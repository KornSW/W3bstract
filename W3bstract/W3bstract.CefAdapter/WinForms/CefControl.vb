Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports CefSharp
Imports W3bstract.AbstractHosting.CEF
Imports W3bstract.CefAdapter.JsBridging

Namespace CefAdapter.WinForms

  Public Class CefControl

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private WithEvents _Browser As CefSharp.WinForms.ChromiumWebBrowser = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private WithEvents _RuntimeAdapter As CefRuntimeAdapter = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _JsBridgedObjects As New List(Of JsHookingAdapter)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _FirstFrameLoaded As Boolean = False

    Public Event BrowserInitialized()

    Public Sub New()
      Me.InitializeComponent()
    End Sub

    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
    Public ReadOnly Property JsBridgedObjects As JsHookingAdapter()
      Get
        Return _JsBridgedObjects.ToArray()
      End Get
    End Property

    Public ReadOnly Property Browser As CefSharp.WinForms.ChromiumWebBrowser
      Get
        Return _Browser
      End Get
    End Property

    Public ReadOnly Property RuntimeAdapter As CefRuntimeAdapter
      Get
        Return _RuntimeAdapter
      End Get
    End Property

    Public Sub InitializeBrowser(requestHandler As IWebRequestHandler, webSessionStateManager As IWebSessionStateManager, ParamArray objectsToBridgeIntoJs() As Object)

      If (_Browser IsNot Nothing) Then
        Exit Sub
      End If

      Dim rootUrl = "http://inMemory.local/"
      _Browser = New CefSharp.WinForms.ChromiumWebBrowser(rootUrl)

      If (objectsToBridgeIntoJs IsNot Nothing) Then

        CefSharpSettings.LegacyJavascriptBindingEnabled = True

        For Each jsBridgedObject In objectsToBridgeIntoJs
          Dim adapter As New JsHookingAdapter(jsBridgedObject)
          _JsBridgedObjects.Add(adapter)
          adapter.RegisterHook(_Browser)
        Next

      End If

      Me.Controls.Add(_Browser)

      _Browser.Name = "CefBrowser"
      _Browser.Location = New System.Drawing.Point(247, 167)
      _Browser.Size = New System.Drawing.Size(39, 13)
      _Browser.AutoSize = True
      _Browser.Dock = DockStyle.Fill
      _Browser.BackColor = Color.White
      _Browser.TabIndex = 0

      _Browser.Visible = True
      _Browser.Show()
      _Browser.Select()

      'initialize an adapter as interactor to serve the bundle directly into the browser control 
      _RuntimeAdapter = New CefRuntimeAdapter(_Browser, requestHandler, webSessionStateManager, rootUrl)

      _RuntimeAdapter.Run()

    End Sub

#Region " FrameLoad "

    Private Sub CefBrowser_FrameLoadEnd(sender As Object, e As FrameLoadEndEventArgs) Handles _Browser.FrameLoadEnd

      If (Me.InvokeRequired) Then
        Me.Invoke(
          Sub()
            Me.InjectJsHooks()
            If (BrowserInitializedEvent IsNot Nothing AndAlso Not _FirstFrameLoaded) Then
              RaiseEvent BrowserInitialized()
            End If
            _FirstFrameLoaded = True
          End Sub
        )
      Else
        Me.InjectJsHooks()
        If (BrowserInitializedEvent IsNot Nothing AndAlso Not _FirstFrameLoaded) Then
          RaiseEvent BrowserInitialized()
        End If
        _FirstFrameLoaded = True
      End If
    End Sub

#End Region

    Private Sub InjectJsHooks()
      For Each obj In _JsBridgedObjects
        For Each adapter In _JsBridgedObjects
          adapter.ActivateHook(_Browser.GetMainFrame())
        Next
      Next
    End Sub

    Private Sub CefBrowser_Initialized() Handles Me.BrowserInitialized
      If (_BrowserDevToolsVisible) Then
        _Browser.ShowDevTools()
      End If
    End Sub

#Region " Developer Tools "

    Private _BrowserDevToolsVisible As Boolean = False

    Public Property BrowserDevToolsVisible As Boolean
      Get
        Return _BrowserDevToolsVisible
      End Get
      Set(value As Boolean)

        If (_BrowserDevToolsVisible = value) Then
          Exit Property
        End If

        If (_BrowserDevToolsVisible) Then
          If (_FirstFrameLoaded) Then
            _Browser.CloseDevTools
          End If
          _BrowserDevToolsVisible = False
        Else
          If (_FirstFrameLoaded) Then
            _Browser.ShowDevTools
          End If
          _BrowserDevToolsVisible = True
        End If

      End Set
    End Property

#End Region

#Region " Console Logging "

    Public Property BrowserConsoleErrorLoggingMethod As Action(Of String) = Nothing
    Public Property BrowserConsoleWarningLoggingMethod As Action(Of String) = Nothing
    Public Property BrowserConsoleInfoLoggingMethod As Action(Of String) = Nothing

    Private Sub CefBrowser_ConsoleMessage(sender As Object, e As ConsoleMessageEventArgs) Handles _Browser.ConsoleMessage
      Select Case e.Level
        Case LogSeverity.Error
          Trace.TraceError("    JAVA-SCRIPT:| " + e.Message)
          If (Me.BrowserConsoleErrorLoggingMethod IsNot Nothing) Then
            Me.BrowserConsoleErrorLoggingMethod.Invoke(e.Message)
          End If
        Case LogSeverity.Warning
          Trace.TraceWarning("    JAVA-SCRIPT:| " + e.Message)
          If (Me.BrowserConsoleWarningLoggingMethod IsNot Nothing) Then
            Me.BrowserConsoleWarningLoggingMethod.Invoke(e.Message)
          End If
        Case Else
          If (Me.BrowserConsoleInfoLoggingMethod IsNot Nothing) Then
            Me.BrowserConsoleInfoLoggingMethod.Invoke(e.Message)
          End If
      End Select
    End Sub

#End Region

  End Class

End Namespace
