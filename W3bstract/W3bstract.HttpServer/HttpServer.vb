Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Net
Imports W3bstract.AbstractHosting
Imports W3bstract.AbstractHosting.InMemory

Public Class HttpServer
  Implements IDisposable
  Implements IWebRuntimeHost

#Region " Declarations "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _Config As HttpServerConfig = New HttpServerConfig

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _Sites As New Dictionary(Of WebSiteConfiguration, IWebRequestHandler)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _Listeners As New List(Of HttpListener)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _Sessions As New InMemoryWebSessionStateManager(AddressOf Me.GenerateSessionIdFromRequest)

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _NonAsyncDevMode As Boolean = False

#End Region

#Region " Constructors "

  Public Sub New(Optional nonAsyncDevMode As Boolean = False)
    _Config = New HttpServerConfig
    _NonAsyncDevMode = nonAsyncDevMode
  End Sub

  Public Sub New(site As IWebRequestHandler, Optional nonAsyncDevMode As Boolean = False)
    MyClass.New(site, Nothing,,, nonAsyncDevMode)
  End Sub

  Public Sub New(site As IWebRequestHandler, ipBinding As String, Optional portBinding As Integer = -1, Optional virtualRootDirectory As String = "/", Optional nonAsyncDevMode As Boolean = False)
    _Config = New HttpServerConfig
    _NonAsyncDevMode = nonAsyncDevMode
    Me.InitSite(site, ipBinding, portBinding, virtualRootDirectory)
  End Sub

#End Region

  Public ReadOnly Property ServerRuntime As String Implements IWebRuntimeHost.ServerRuntime
    Get
      Return "W3bstract Micro Webserver"
    End Get
  End Property

  Friend ReadOnly Property NonAsyncDevMode As Boolean
    Get
      Return _NonAsyncDevMode
    End Get
  End Property

#Region " Init "

  Public Sub InitSite(site As IWebRequestHandler, ipBinding As String, ByRef portBinding As Integer, virtualRootDirectory As String)

    If (site Is Nothing) Then
      Throw New ApplicationException("The site-argument mustnot be NULL!")
    End If

    If (String.IsNullOrWhiteSpace(ipBinding)) Then
      Dim adr = Dns.GetHostAddresses(Dns.GetHostName()).Where(Function(a) a.AddressFamily = Sockets.AddressFamily.InterNetwork).FirstOrDefault()
      If (adr IsNot Nothing) Then
        ipBinding = adr.ToString()
      End If
    End If

    Dim cfg As New WebSiteConfiguration
    cfg.Port = portBinding
    cfg.IpV4Binding = ipBinding
    cfg.SiteUid = Guid.Empty
    cfg.VirtualRootDirectory = virtualRootDirectory

    Me.InitSite(site, cfg)

    portBinding = cfg.Port 'falls ein dynamischer port zugewiesen wurde...
  End Sub

  Private Sub InitSite(site As IWebRequestHandler, cfg As WebSiteConfiguration)
    'site.Initialize(Me, cfg)
    _Sites.Add(cfg, site)
    Me.WireUpListener(cfg, site)
  End Sub

#End Region

#Region " Sessions "

  Public ReadOnly Property Sessions As IWebSessionStateManager
    Get
      Return _Sessions
    End Get
  End Property

  Private Function GenerateSessionIdFromRequest(request As IWebRequest) As String
    Dim privateIdentity As String = "_" & request.ClientIpAddress.ToString()
    Dim publicSid As String = Guid.NewGuid().ToString().ToLower()
    If (request.Headers.AllKeys.Contains("Cookie")) Then
      Dim cookie = request.Headers("Cookie").ToString().ToLower()
      If (cookie.StartsWith("jsessionid=")) Then
        publicSid = cookie.Split("="c)(1)
      End If
    End If
    Dim privateSid = publicSid & privateIdentity
    Return privateSid
  End Function

  Public Function GetSessionState(request As HttpRequestScope, response As HttpResponseScope) As IWebSessionState

    Dim sess As IWebSessionState = _Sessions.GetSessionState(request,
      Function(req)
        'this creates a new session
        Dim privateIdentity As String = "_" & request.ClientIpAddress.ToString()
        Dim publicSid As String = Guid.NewGuid().ToString().ToLower()
        Dim privateSid = publicSid & privateIdentity
        response.AddCustomHeader("Set-Cookie", "JSESSIONID=" & publicSid & "")
        Return privateSid
      End Function
    )

    Return sess
  End Function

#End Region

#Region " Listeners "

  Public ReadOnly Property Listeners As HttpListener()
    Get
      Return _Listeners.ToArray()
    End Get
  End Property

  Private Function GetOrCreateListener(ip As IPAddress, ByRef port As Integer) As HttpListener
    Dim exisitingListener As HttpListener = Nothing

    If (port > 0) Then
      Dim portToSearch = port
      exisitingListener = (From l In _Listeners Where l.Ip.Equals(ip) AndAlso l.Port = portToSearch).SingleOrDefault()
    End If

    If (exisitingListener Is Nothing) Then
      exisitingListener = New HttpListener(ip, port, Me)
      If (port < 1) Then
        port = exisitingListener.Port
      End If
      _Listeners.Add(exisitingListener)
    End If

    Return exisitingListener
  End Function

  Private Sub WireUpListener(config As WebSiteConfiguration, site As IWebRequestHandler)

    Dim thisComputer As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())

    For Each localIp In thisComputer.AddressList

      If (localIp.AddressFamily = Sockets.AddressFamily.InterNetwork) Then
        If (localIp.ToString().MatchesMaskOrRegex(config.IpV4Binding)) Then
          Dim l = Me.GetOrCreateListener(localIp, config.Port)
          l.AddSite(config.VirtualRootDirectory, site)
        End If
      ElseIf (localIp.AddressFamily = Sockets.AddressFamily.InterNetworkV6) Then

      End If
    Next

    If (config.IpV4Binding = "127.0.0.1") Then
      Dim l = Me.GetOrCreateListener(IPAddress.Loopback, config.Port)
      l.AddSite(config.VirtualRootDirectory, site)
    End If

  End Sub

  <EditorBrowsable(EditorBrowsableState.Never)>
  Public Sub ProcessRequests()

    If (Not _NonAsyncDevMode) Then
      Throw New Exception("This can only be used in NonAsyncDevMode!")
    End If

    For Each l In _Listeners
      While l.SyncedQueue.Count > 0
        Dim requestProcessingCall As Action
        SyncLock l.SyncedQueue
          requestProcessingCall = l.SyncedQueue.Dequeue
        End SyncLock
        requestProcessingCall.Invoke()
      End While
    Next

  End Sub

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

        For Each listener In _Listeners
          listener.Dispose()
        Next
        _Listeners.Clear()
        _Listeners = Nothing

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
