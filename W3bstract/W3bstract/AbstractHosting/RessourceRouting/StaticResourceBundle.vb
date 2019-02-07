Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Reflection
Imports SmartCoreFx

Public Class StreamResourceHandler
  Implements IWebRequestHandler

  Private _StreamFactory As Func(Of Stream)
  Private _MimeType As String

  Public Sub New(streamFactory As Func(Of Stream), mimeType As String, Optional keepInMemory As Boolean = False)
    _StreamFactory = streamFactory
    _MimeType = mimeType
    If (keepInMemory) Then
      Me.KeepInMemory()
    End If
  End Sub

  Public Sub ProcessRequest(request As IWebRequest, response As IWebResponse, state As IWebSessionState) Implements IWebRequestHandler.ProcessRequest
    If (request.HttpMethod.Equals("options", StringComparison.CurrentCultureIgnoreCase)) Then
      response.StatusCode = 200 'OK
      Exit Sub
    End If
    response.ContentMimeType = _MimeType
    If (_PrefetchedContent Is Nothing) Then
      Using sourceStream = _StreamFactory.Invoke()
        sourceStream.CopyTo(response.Stream)
      End Using
    Else
      response.Stream.Write(_PrefetchedContent, 0, _PrefetchedContent.Length)
    End If
  End Sub

  Public Sub KeepInMemory()
    If (_PrefetchedContent Is Nothing) Then
      _PrefetchedContent = {}
      Using sourceStream = _StreamFactory.Invoke()
        Dim length = CInt(sourceStream.Length)
        ReDim _PrefetchedContent(length - 1)
        sourceStream.Read(_PrefetchedContent, 0, length)
      End Using
    End If
  End Sub

  Private _PrefetchedContent As Byte() = Nothing

  Public Property PrefetchedContent As Byte()
    Get
      Return _PrefetchedContent
    End Get
    Set(value As Byte())
      _PrefetchedContent = value
    End Set
  End Property

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
