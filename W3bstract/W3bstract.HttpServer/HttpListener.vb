Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

Public Class HttpListener
  Implements IDisposable

  Private _Port As Integer
  Private _Ip As IPAddress
  Private _Listener As TcpListener
  Private _Thread As SimpleThread
  Private _Server As HttpServer
  Private _Sites As New Dictionary(Of String, IWebRequestHandler)

  Public Sub New(ipAddress As IPAddress, port As Integer, server As HttpServer)
    If (port < 0) Then
      port = 0
    End If
    _Ip = ipAddress
    _Port = port
    _Server = server
    _Thread = SimpleThread.RunAsync(AddressOf Me.Thread)
    _Listener = New TcpListener(_Ip, _Port)
    _Listener.Start()
    _Port = DirectCast(_Listener.LocalEndpoint, IPEndPoint).Port
  End Sub

  Public ReadOnly Property Ip As IPAddress
    Get
      Return _Ip
    End Get
  End Property

  Public ReadOnly Property Port As Integer
    Get
      Return _Port
    End Get
  End Property

  Public Sub AddSite(virtualDirectory As String, site As IWebRequestHandler)
    SyncLock _Sites
      _Sites.Add(virtualDirectory, site)
    End SyncLock
  End Sub

  Friend Property SyncedQueue As New Queue(Of Action)

  Private Function Thread(params() As Object, ByRef cancelRequested As Boolean) As Object
    While (Not cancelRequested)
      If (_Listener.Pending()) Then
        Dim incommingConnection As TcpClient = _Listener.AcceptTcpClient()
        Dim scope As New HttpRequestScope(incommingConnection, Me, _Server)

        Dim foundSite As IWebRequestHandler = Nothing
        SyncLock _Sites
          For Each siteVDir In _Sites.Keys
            If (scope.HttpUrl.ToLower().StartsWith(siteVDir.ToLower())) Then
              foundSite = _Sites(siteVDir)
              Exit For
            End If
          Next
        End SyncLock

        If (foundSite IsNot Nothing) Then
          If (_Server.NonAsyncDevMode) Then
            SyncLock SyncedQueue
              SyncedQueue.Enqueue(Sub() scope.Process(foundSite))
            End SyncLock
          Else
            Dim processorThread As New Thread(New ThreadStart(Sub() scope.Process(foundSite)))
            processorThread.Start()
          End If
        End If

      End If
      Threading.Thread.Sleep(5)
    End While
    Return Nothing
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
        If (_Thread IsNot Nothing) Then
          _Thread.Cancel(2000)
          _Thread.TryDispose(True)
          _Thread = Nothing
        End If
        If (_Listener IsNot Nothing) Then
          Try
            _Listener.Stop()
          Catch
          End Try
          _Listener.TryDispose(True)
          _Listener = Nothing
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
