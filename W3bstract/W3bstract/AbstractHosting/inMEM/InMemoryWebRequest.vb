Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Web
Imports System.Web.SessionState
Imports SmartCoreFx
Imports SmartCoreFx.Web

Public Class InMemoryWebRequest
  Implements IWebRequest

  Public Sub New(httpMethod As String, url As String, Optional inputStream As Stream = Nothing)
    Me.HttpMethod = httpMethod
    Me.Url = New Uri(url)
    Me.InputStream = inputStream
  End Sub

#Region " Properties "

  Public ReadOnly Property InputStream As Stream Implements IWebRequest.InputStream

  Public ReadOnly Property Url As Uri Implements IWebRequest.Url

  Public ReadOnly Property ClientHostname As String Implements IWebRequest.ClientHostname
    Get
      Return "localhost"
    End Get
  End Property

  Public ReadOnly Property ClientIpAddress As IPAddress Implements IWebRequest.ClientIpAddress
    Get
      Return IPAddress.Parse("127.0.0.1")
    End Get
  End Property

  Public ReadOnly Property HttpMethod As String Implements IWebRequest.HttpMethod

  Public ReadOnly Property Headers As New NameValueCollection Implements IWebRequest.Headers

  Public ReadOnly Property Cookies As Dictionary(Of String, String) Implements IWebRequest.Cookies
    Get
      Throw New NotImplementedException()
    End Get
  End Property

  Public ReadOnly Property Browser As String Implements IWebRequest.Browser
    Get
      Return "in-memory"
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
