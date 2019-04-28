Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.CompilerServices
Imports System.Threading

Public Class HttpResponseScope
  Implements IWebResponse

  Private _Request As HttpRequestScope
  Private _OutputWriter As StreamWriter
  Private _HeadersWritten As Boolean = False
  Private _HttpHeaders As New Hashtable()

  Public Sub New(request As HttpRequestScope, outputStreamWriter As StreamWriter)
    _Request = request
    _OutputWriter = outputStreamWriter
  End Sub

  Private _ContentMimeType As String = "text/html"
  Public Property ContentMimeType As String Implements IWebResponse.ContentMimeType
    Get
      Return _ContentMimeType
    End Get
    Set(value As String)
      If (_HeadersWritten) Then
        Throw New InvalidOperationException("ContentMimeType can only be changed white the Header have not been written to the replystream")
      End If
      _ContentMimeType = value
    End Set
  End Property

  Public ReadOnly Property HeadersWritten As Boolean
    Get
      Return _HeadersWritten
    End Get
  End Property

  Public ReadOnly Property ContentWriter As TextWriter Implements IWebResponse.ContentWriter
    Get
      If (Not _HeadersWritten) Then
        Me.WriteHeaders()
        'Throw New InvalidOperationException("StreamWriter is only available after WriteHeaders has been called")
      End If

      Return _OutputWriter
    End Get
  End Property

  Public Sub AddCustomHeader(key As String, rawValue As String)
    _HttpHeaders.Add(key, rawValue)
  End Sub

  Public ReadOnly Property Stream As Stream Implements IWebResponse.Stream
    Get
      Return _OutputWriter.BaseStream
    End Get
  End Property

  Private _StatusCode As Integer = 200
  Public Property StatusCode As Integer Implements IWebResponse.StatusCode
    Get
      Return _StatusCode
    End Get
    Set(value As Integer)
      If (_HeadersWritten) Then
        Throw New InvalidOperationException("StatusCode can only be changed white the Header have not been written to the replystream")
      End If
      _StatusCode = value
    End Set
  End Property

  Public Property Header(name As String) As String Implements IWebResponse.Header
    Get
      Return _HttpHeaders(name).ToString()
    End Get
    Set(value As String)
      _HttpHeaders(name) = value
    End Set
  End Property

  Public Sub WriteHeaders()
    If (Not _HeadersWritten) Then
      _OutputWriter.WriteLine($"HTTP/1.0 {Me.StatusCode} OK")
      For Each key In _HttpHeaders.Keys
        _OutputWriter.WriteLine(String.Format("{0}: {1}", key, _HttpHeaders(key)))
      Next
      _OutputWriter.WriteLine(Convert.ToString("Content-Type: ") & _ContentMimeType)
      _OutputWriter.WriteLine("Connection: close")
      _OutputWriter.WriteLine("")
    End If
  End Sub

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  ''' Dispose the current object instance and suppress the finalizer
  ''' </summary>
  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Sub Dispose() Implements IDisposable.Dispose
    If (Not _AlreadyDisposed) Then
      Me.Disposing()
      _AlreadyDisposed = True
    End If
    GC.SuppressFinalize(Me)
  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Protected Overridable Sub Disposing()


  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Protected Sub DisposedGuard()
    If (_AlreadyDisposed) Then
      Throw New ObjectDisposedException(Me.GetType.Name)
    End If
  End Sub

#End Region

End Class
