Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Web

Namespace AbstractHosting.ASP

  Partial Class AspRuntimeAdapter

    Private Class AspRequest
      Implements IWebRequest

      Private _Request As HttpRequest
      Private _State As AspSessionState
      Private _Cookies As Dictionary(Of String, String) = Nothing

      Public Sub New(request As HttpRequest, state As AspSessionState)
        _Request = request
        _State = state
      End Sub

#Region " Properties "

      'Public ReadOnly Property PostData As NameValueCollection Implements IWebRequest.PostData
      '  Get
      '    Return _Request.Form
      '  End Get
      'End Property

      Public ReadOnly Property InputStream As Stream Implements IWebRequest.InputStream
        Get
          Return _Request.InputStream
        End Get
      End Property

      Public ReadOnly Property Url As Uri Implements IWebRequest.Url
        Get
          Return _Request.Url
        End Get
      End Property

      Public ReadOnly Property ClientHostname As String Implements IWebRequest.ClientHostname
        Get
          Select Case (_Request.UserHostName)
            Case "::1" : Return "localhost"
            Case "127.0.0.1" : Return "localhost"
            Case Else
              Return _Request.UserHostName
          End Select
        End Get
      End Property

      Public ReadOnly Property ClientIpAddress As IPAddress Implements IWebRequest.ClientIpAddress
        Get
          Return IPAddress.Parse(_Request.UserHostAddress)
        End Get
      End Property

      Public ReadOnly Property HttpMethod As String Implements IWebRequest.HttpMethod
        Get
          Return _Request.HttpMethod
        End Get
      End Property

      Public ReadOnly Property Headers As NameValueCollection Implements IWebRequest.Headers
        Get
          Return _Request.Headers
        End Get
      End Property

      Public ReadOnly Property Cookies As Dictionary(Of String, String) Implements IWebRequest.Cookies
        Get
          SyncLock _Cookies

            If (_Cookies Is Nothing) Then

              _Cookies = New Dictionary(Of String, String)
              For Each cookieName As String In _Request.Cookies.AllKeys
                Dim cookie = _Request.Cookies.Item(cookieName)
                If (cookie.HasKeys) Then
                  For Each entryName As String In cookie.Values.Keys
                    _Cookies.Add(cookieName + "." + entryName, cookie(entryName))
                  Next
                Else
                  Dim i As Integer = 0
                  For Each entry As String In cookie.Value
                    _Cookies.Add(cookieName + "." + i.ToString(), entry)
                    i += 1
                  Next
                End If
              Next

            End If

            Return _Cookies
          End SyncLock
        End Get
      End Property

      Public ReadOnly Property Browser As String Implements IWebRequest.Browser
        Get
          Return _Request.Browser.Browser
        End Get
      End Property

#End Region

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

  End Class

End Namespace
