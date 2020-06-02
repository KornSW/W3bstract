Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Web

Public Class HttpRequestScope
  Implements IWebRequest

#Region " Declarations "

  Private Const _BufferSize As Integer = 4096

  Private _TcpClient As TcpClient
  Private _Listener As HttpListener
  Private _Server As HttpServer
  Private _InputStream As Stream
  Private _OutputStream As StreamWriter
  Private _HttpMethod As [String]
  Private _HttpUrl As [String]
  Private _HttpProtocolVersionString As [String]
  Private _HttpHeaders As New NameValueCollection
  Private _PostData As New NameValueCollection
  Private _MaxPostSizeBytes As Integer = 10 * 1024 * 1024 ' 10MB
  Private _RemoteIP As IPAddress
  Private _RemoteHostName As String = Nothing 'on demand

#End Region

  Public Sub New(s As TcpClient, listener As HttpListener, server As HttpServer)

    _TcpClient = s
    _Listener = listener
    _Server = server
    _RemoteIP = _TcpClient.GetRemoteIpAddress()

    Dim networkStream As NetworkStream = _TcpClient.GetStream()

    ' we can't use a StreamReader for input, because it buffers up extra data on us inside it's
    ' "processed" view of the world, and we want the data raw after the headers
    _InputStream = New BufferedStream(networkStream)

    ' we probably shouldn't be using a streamwriter for all output from handlers either
    _OutputStream = New StreamWriter(New BufferedStream(_TcpClient.GetStream()))

    Me.ParseRequestsFirstLine()
    Me.ReadHeaders()

  End Sub

  Public ReadOnly Property ClientHostname As String Implements IWebRequest.ClientHostname
    Get
      If (_RemoteHostName Is Nothing) Then
        _RemoteHostName = _RemoteIP.ResolveDnsName()
      End If
      Return _RemoteHostName
    End Get
  End Property

  Public ReadOnly Property ClientIpAddress As IPAddress Implements IWebRequest.ClientIpAddress
    Get
      Return _RemoteIP
    End Get
  End Property

  Public ReadOnly Property Headers As NameValueCollection Implements IWebRequest.Headers
    Get
      Return _HttpHeaders
    End Get
  End Property

  Public ReadOnly Property HttpMethod As String Implements IWebRequest.HttpMethod
    Get
      Return _HttpMethod
    End Get
  End Property

  Public ReadOnly Property InputStream As Stream Implements IWebRequest.InputStream
    Get
      Return _InputStream
    End Get
  End Property

  Public ReadOnly Property Cookies As Dictionary(Of String, String) Implements IWebRequest.Cookies
    Get
      Throw New NotImplementedException()
    End Get
  End Property

  Public ReadOnly Property Browser As String Implements IWebRequest.Browser
    Get
      Return String.Empty
    End Get
  End Property

  Private Function StreamReadLine(inputStream As Stream) As String
    Dim next_char As Integer
    Dim data As String = ""

    While True

      next_char = inputStream.ReadByte()

      If Convert.ToChar(next_char) = Microsoft.VisualBasic.ControlChars.Lf Then
        Exit While
      End If
      If Convert.ToChar(next_char) = Microsoft.VisualBasic.ControlChars.Cr Then
        Continue While
      End If
      If next_char = -1 Then
        Thread.Sleep(1)
        Continue While
      End If

      data += Convert.ToChar(next_char)

    End While

    Return data
  End Function

  Public ReadOnly Property HttpUrl As String
    Get
      Return _HttpUrl
    End Get
  End Property

  Public ReadOnly Property Url As Uri Implements IWebRequest.Url
    Get
      Dim uriString As String = String.Format("http://{0}:{1}{2}", _Listener.Ip, _Listener.Port, _HttpUrl)
      Return New Uri(uriString)
    End Get
  End Property

  Public Sub Process(site As IWebRequestHandler)

    If (_HttpMethod = "POST") Then
      Me.HandlePOSTRequest()
    End If

    Try

      Using response As New HttpResponseScope(Me, _OutputStream)
        Dim state As IWebSessionState = _Server.GetSessionState(Me, response)
        site.ProcessRequest(Me, response, state)
      End Using

    Catch ex As Exception 'When ex.Catching
      If (_RemoteIP.ToString = "127.0.0.1") Then
        _OutputStream.WriteLine("HTTP/1.0 200 OK")
        _OutputStream.WriteLine(Convert.ToString("Content-Type: "))
        _OutputStream.WriteLine("Connection: close")
        _OutputStream.WriteLine(ex.ToFullString(True))
      End If
      Me.ReplyFailure404()
    End Try

    _OutputStream.Flush()
    ' bs.Flush(); // flush any remaining output
    _InputStream = Nothing
    _OutputStream = Nothing
    ' bs = null;            
    _TcpClient.Close()

  End Sub

  Public Sub ParseRequestsFirstLine()
    Dim request As [String] = StreamReadLine(_InputStream)
    Dim tokens As String() = request.Split(" "c)

    If tokens.Length <> 3 Then
      Throw New Exception("invalid http request line")
    End If

    _HttpMethod = tokens(0).ToUpper()
    _HttpUrl = tokens(1)
    If (Not _HttpUrl.StartsWith("/")) Then
      _HttpUrl = "/" & _HttpUrl
    End If
    _HttpProtocolVersionString = tokens(2)

    Console.WriteLine("starting: " + request)
  End Sub

  Public Sub ReadHeaders()
    Console.WriteLine("readHeaders()")
    Dim line As String = Nothing
    While (InlineAssignHelper(line, StreamReadLine(_InputStream))) IsNot Nothing
      If line.Equals("") Then
        Console.WriteLine("got headers")
        Return
      End If

      Dim separator As Integer = line.IndexOf(":"c)
      If separator = -1 Then
        Throw New Exception("invalid http header line: " + line)
      End If
      Dim name As [String] = line.Substring(0, separator)
      Dim pos As Integer = separator + 1
      While (pos < line.Length) AndAlso (line(pos) = " "c)
        ' strip any spaces
        pos += 1
      End While

      Dim value As String = line.Substring(pos, line.Length - pos)
      Console.WriteLine("header: {0}:{1}", name, value)
      _HttpHeaders(name) = value
    End While
  End Sub

  Public ReadOnly Property PostData As NameValueCollection 'Implements IWebRequest.PostData
    Get
      Return _PostData
    End Get
  End Property

  Public Sub HandlePOSTRequest()
    ' this post data processing just reads everything into a memory stream.
    ' this is fine for smallish things, but for large stuff we should really
    ' hand an input stream to the request processor. However, the input stream 
    ' we hand him needs to let him see the "end of the stream" at this content 
    ' length, because otherwise he won't know when he's seen it all! 

    Console.WriteLine("get post data start")
    Dim content_len As Integer = 0

    Dim ms As New MemoryStream()

    If (_HttpHeaders.AllKeys.Contains("Content-Length")) Then

      content_len = Convert.ToInt32(Me._HttpHeaders("Content-Length"))
      If content_len > _MaxPostSizeBytes Then
        Throw New Exception([String].Format("POST Content-Length({0}) too big for this simple server", content_len))
      End If
      Dim buf As Byte() = New Byte(_BufferSize - 1) {}
      Dim to_read As Integer = content_len
      While to_read > 0
        Console.WriteLine("starting Read, to_read={0}", to_read)

        Dim numread As Integer = Me._InputStream.Read(buf, 0, Math.Min(_BufferSize, to_read))
        Console.WriteLine("read finished, numread={0}", numread)
        If numread = 0 Then
          If to_read = 0 Then
            Exit While
          Else
            Throw New Exception("client disconnected during post")
          End If
        End If
        to_read -= numread
        ms.Write(buf, 0, numread)
      End While
      ms.Seek(0, SeekOrigin.Begin)
    End If
    Console.WriteLine("get post data end")
    Using sr As New StreamReader(ms)
      _PostData = HttpUtility.ParseQueryString(sr.ReadToEnd())
    End Using

  End Sub

  Public Sub WriteSuccessMime(Optional contentMimeType As String = "text/html")
    _OutputStream.WriteLine("HTTP/1.0 200 OK")
    _OutputStream.WriteLine(Convert.ToString("Content-Type: ") & contentMimeType)
    _OutputStream.WriteLine("Connection: close")
    _OutputStream.WriteLine("")
  End Sub

  Public Sub ReplyFailure404()
    _OutputStream.WriteLine("HTTP/1.0 404 File not found")
    _OutputStream.WriteLine("Connection: close")
    _OutputStream.WriteLine("")
  End Sub

  Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
    target = value
    Return value
  End Function

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  ''' Dispose the current object instance
  ''' </summary>
  Protected Overridable Sub Dispose(disposing As Boolean)
    If (Not _AlreadyDisposed) Then
      If (disposing) Then



      End If
      _AlreadyDisposed = True
    End If
  End Sub

  ''' <summary>
  ''' Dispose the current object instance and suppress the finalizer
  ''' </summary>
  Public Sub Dispose() Implements IDisposable.Dispose
    Me.Dispose(True)
    GC.SuppressFinalize(Me)
  End Sub

#End Region

End Class
