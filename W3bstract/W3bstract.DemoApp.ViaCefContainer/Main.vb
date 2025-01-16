Imports System
Imports System.Windows.Forms
Imports W3bstract.AbstractHosting.InMemory
Imports W3bstract.CefAdapter.JsBridging
Imports W3bstract.CefAdapter.WinForms
Imports W3bstract.DemoApp.AngularSpaBundle
Imports W3bstract.DemoApp.BackendApi

Public Module Main

  Public Sub Main()

    Application.EnableVisualStyles()

    Using root As New WebRequestRouter(), stateMgr As New InMemoryWebSessionStateManager(Function(r) "SHARED")

      Dim backendApi As New WebApiEntryPoint()
      root.RegisterDynamicTarget(backendApi, "/api")

      Dim fontendSpa As New SpaBundleEntryPoint(backendApiUrl:="/api")
      root.RegisterDynamicTarget(fontendSpa)

      root.SetDefaultResource("index.html")

      Dim svc As New FooService

      Dim pxy As New JsInMemoryHttpClient(root, stateMgr)

      DialogLauncher.ShowDialog(root, stateMgr, Application.ProductName, My.Resources.AppIcon, False, svc)

    End Using

  End Sub

End Module
