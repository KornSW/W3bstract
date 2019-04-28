Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Reflection
Imports CefSharp
Imports CefSharp.WinForms
Imports W3bstract.AbstractHosting.InMemory

Namespace AbstractHosting.CEF

  Public Class CefRuntimeAdapter
    Implements IResourceHandlerFactory

#Region " Fields & Constructor "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _CurrentRequests As New List(Of CefRequestContext)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _WebRequestHandler As IWebRequestHandler

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _WebSessionStateManager As IWebSessionStateManager

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private WithEvents _BrowserControl As ChromiumWebBrowser

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _VirtualRootUrl As String

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _PageTitle As String = "loading..."

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _FavIcon As Icon = Nothing

    Public Sub New(browserControl As ChromiumWebBrowser, webRequestHandler As IWebRequestHandler, webSessionStateManager As IWebSessionStateManager, Optional virtualRootUrl As String = "http://inMemory.local/")
      _WebRequestHandler = webRequestHandler
      _WebSessionStateManager = webSessionStateManager
      _BrowserControl = browserControl
      _BrowserControl.ResourceHandlerFactory = Me
      _VirtualRootUrl = virtualRootUrl.ToLower()
      If (Not _VirtualRootUrl.EndsWith("/")) Then
        _VirtualRootUrl = _VirtualRootUrl + "/"
      End If
    End Sub

#End Region

#Region " Info Properties "

    Public ReadOnly Property WebRequestHandler As IWebRequestHandler
      Get
        Return _WebRequestHandler
      End Get
    End Property

    Public ReadOnly Property WebSessionStateManager As IWebSessionStateManager
      Get
        Return _WebSessionStateManager
      End Get
    End Property

    Private ReadOnly Property HasHandlers As Boolean Implements IResourceHandlerFactory.HasHandlers
      Get
        Return True
      End Get
    End Property

    Public ReadOnly Property PageTitle As String
      Get
        Return _PageTitle
      End Get
    End Property

    Public ReadOnly Property FavIcon As Icon
      Get
        Return _FavIcon
      End Get
    End Property

#End Region

#Region " Registration "

    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
    Public ReadOnly Property CurrentRequests As CefRequestContext()
      Get
        SyncLock _CurrentRequests
          Return _CurrentRequests.ToArray()
        End SyncLock
      End Get
    End Property

    Private Sub NotifyRequestBegin(context As CefRequestContext)
      SyncLock _CurrentRequests
        _CurrentRequests.Add(context)
      End SyncLock
    End Sub

    Private Sub NotifyRequestEnd(context As CefRequestContext)
      SyncLock _CurrentRequests
        _CurrentRequests.Remove(context)
      End SyncLock
    End Sub

#End Region

    Private Sub BrowserControl_TitleChanged(sender As Object, e As TitleChangedEventArgs) Handles _BrowserControl.TitleChanged
      _PageTitle = e.Title
    End Sub

    Private Function GetResourceHandler(browserControl As IWebBrowser, browser As IBrowser, frame As IFrame, request As IRequest) As IResourceHandler Implements IResourceHandlerFactory.GetResourceHandler
      If (Not request.Url.StartsWith(_VirtualRootUrl)) Then
        Return Nothing
      Else
        Return New CefRequestContext(Me, request, _WebRequestHandler)
      End If
    End Function

    Public Sub Run()
      _BrowserControl.Load(_VirtualRootUrl)

      Using imReq As New InMemoryWebRequest("GET", _VirtualRootUrl + "favicon.ico"),
        imSess As New InMemorySessionState, imRes As New InMemoryWebResponse

        _WebRequestHandler.ProcessRequest(imReq, imRes, imSess)

        If (imRes.Stream.Position > 0) Then
          imRes.Stream.Position = 0
        End If

        If (imRes.StatusCode = 200) Then
          _FavIcon = New Icon(imRes.Stream)
        End If
      End Using

    End Sub

  End Class

End Namespace
