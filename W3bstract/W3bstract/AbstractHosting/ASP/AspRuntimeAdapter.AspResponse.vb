Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Web

Namespace AbstractHosting.ASP

  Partial Class AspRuntimeAdapter

    Private Class AspResponse
      Implements IWebResponse

      Private _Response As HttpResponse
      Private _State As AspSessionState
      Private _Writer As StreamWriter = Nothing

      Public Sub New(response As HttpResponse, state As AspSessionState)
        _Response = response
        _State = state
        response.HeaderEncoding = System.Text.Encoding.UTF8

        'EINFACH NOCH NICHT GETESTET!!!
        'response.AppendHeader("Strict-Transport-Security", "max-age=28800; includeSubdomains")
        'response.AppendHeader("X-Frame-Options", "DENY")
        'response.AppendHeader("X-UA-Compatible", "IE=edge") 'suppress IE 'quirks-mode'
        'response.AppendHeader("X-XSS-Protection", "1; mode=block")

      End Sub

      Public Property ContentMimeType As String Implements IWebResponse.ContentMimeType
        Get
          Return _Response.ContentType
        End Get
        Set(value As String)
          _Response.ContentType = value
        End Set
      End Property

      Public ReadOnly Property ContentWriter As TextWriter Implements IWebResponse.ContentWriter
        Get
          If (_Writer Is Nothing) Then
            _Writer = New StreamWriter(_Response.OutputStream)
            _Writer.AutoFlush = True
          End If
          Return _Writer
        End Get
      End Property

      Public ReadOnly Property Stream As Stream Implements IWebResponse.Stream
        Get
          Return _Response.OutputStream
        End Get
      End Property

      Public Property StatusCode As Integer Implements IWebResponse.StatusCode
        Get
          Return _Response.StatusCode
        End Get
        Set(value As Integer)
          _Response.StatusCode = value
        End Set
      End Property

      Public Property Header(name As String) As String Implements IWebResponse.Header
        Get
          Return _Response.Headers.Get(name)
        End Get
        Set(value As String)
          If (value Is Nothing) Then
            _Response.Headers.Remove(name)
          Else
            _Response.Headers.Set(name, value)
          End If
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
            If (_Writer IsNot Nothing) Then
              _Writer.Flush()
              _Writer.Dispose()
              _Writer = Nothing
            End If
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

  End Class

End Namespace
