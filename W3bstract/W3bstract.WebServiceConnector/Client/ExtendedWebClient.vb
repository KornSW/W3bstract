Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Security
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Security
Imports System.Text
Imports System.Web

Friend Class ExtendedWebClient
  Inherits WebClient

#Region " Singleton "

  Private Shared _Instance As ExtendedWebClient = Nothing

  Public Shared Function GetInstance() As ExtendedWebClient
    If (_Instance Is Nothing) Then
      _Instance = New ExtendedWebClient()
      _Instance.Encoding = System.Text.Encoding.UTF8
    End If

    Return _Instance
  End Function

#End Region

  Private _CookieContainer As New CookieContainer
  Private _CachePolicy As New Cache.RequestCachePolicy(Cache.RequestCacheLevel.BypassCache)

  Public Sub New()
    MyBase.New()
    Me.ConfigureProxy()
  End Sub

  Public Sub New(basicAuthUserName As String, basicAuthPassword As String)
    MyBase.New()
    Me.ConfigureProxy()
    Me.Credentials = New NetworkCredential(basicAuthUserName, basicAuthPassword)
  End Sub

  Public Sub New(basicAuthUserName As String, basicAuthPassword As SecureString)
    MyBase.New()
    Me.ConfigureProxy()
    Me.Credentials = New NetworkCredential(basicAuthUserName, basicAuthPassword)
  End Sub

  Private _ProxyCredentials As ICredentials = Nothing
  Public Property ProxyCredentials As ICredentials
    Get
      Return _ProxyCredentials
    End Get
    Set(value As ICredentials)
      _ProxyCredentials = value
      Me.ConfigureProxy()
    End Set
  End Property

  Protected Sub ConfigureProxy()
    Dim defaultProxy As IWebProxy = WebRequest.DefaultWebProxy
    If (defaultProxy IsNot Nothing) Then
      If (_ProxyCredentials Is Nothing) Then
        defaultProxy.Credentials = CredentialCache.DefaultCredentials
      Else
        defaultProxy.Credentials = _ProxyCredentials
      End If
      Me.Proxy = defaultProxy
    Else
      Me.Proxy = Nothing
    End If
  End Sub

  Public Property CacheLevel As Cache.RequestCacheLevel
    Get
      Return _CachePolicy.Level
    End Get
    Set(value As Cache.RequestCacheLevel)
      _CachePolicy = New Cache.RequestCachePolicy(value)
    End Set
  End Property

  Public Property CookieContainer As CookieContainer
    Get
      Return _CookieContainer
    End Get
    Set(value As CookieContainer)
      _CookieContainer = value
    End Set
  End Property

  Public Overloads Sub UploadData(address As String, data As Stream, responseReceiver As Stream)
    Dim bytes As Integer = CInt(data.Length - data.Position)
    Dim sendBuffer(bytes - 1) As Byte
    data.Read(sendBuffer, CInt(data.Position), bytes)
    Dim receiveBuffer = MyBase.UploadData(address, sendBuffer)
    responseReceiver.Write(receiveBuffer, 0, receiveBuffer.Length)
  End Sub

  Public Property IgnoreSslCertificateErrors As Boolean = True

  'client.Credentials = CredentialCache.DefaultCredentials;

  Protected Function ValidateCertificate(sender As Object, certificate As System.Security.Cryptography.X509Certificates.X509Certificate, chain As System.Security.Cryptography.X509Certificates.X509Chain, sslPolicyErrors As System.Net.Security.SslPolicyErrors) As Boolean
    Return True
  End Function

  Protected Overrides Function GetWebResponse(request As WebRequest) As WebResponse
    Try
      Dim resp As WebResponse = MyBase.GetWebResponse(request)
      Dim h = resp.Headers
      Return resp
    Catch ex As Exception
      Throw
    End Try
  End Function

  Protected Overrides Function GetWebRequest(address As Uri) As WebRequest
    Dim request = DirectCast(MyBase.GetWebRequest(address), HttpWebRequest)
    Dim originalCallback As RemoteCertificateValidationCallback = Nothing
    If (Me.IgnoreSslCertificateErrors) Then
      originalCallback = ServicePointManager.ServerCertificateValidationCallback
      ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf Me.ValidateCertificate)
    End If
    ' request.PreAuthenticate = True
    request.UserAgent = "Company - Device - 0.01"
    Try
      request.CookieContainer = _CookieContainer
      request.CachePolicy = _CachePolicy
      request.Credentials = Me.Credentials
      Return request
    Finally
      If (Me.IgnoreSslCertificateErrors) Then
        ServicePointManager.ServerCertificateValidationCallback = originalCallback
      End If
    End Try
  End Function

End Class

