Imports System
Imports System.Drawing
Imports W3bstract

Namespace CefAdapter.WinForms

  Public Class DialogLauncher

    Private Sub New()
    End Sub

    Public Shared Sub ShowDialog(requestHandler As IWebRequestHandler, webSessionStateManager As IWebSessionStateManager, windowTitle As String, icon As Icon, browserDevToolsVisible As Boolean, ParamArray objectsToBridgeIntoJs() As Object)

      'TODO: icon und title

      Using dialog As New CefDialog
        dialog.Text = windowTitle
        dialog.Icon = icon
        dialog.CefControl.InitializeBrowser(requestHandler, webSessionStateManager, objectsToBridgeIntoJs)
        dialog.CefControl.BrowserDevToolsVisible = browserDevToolsVisible
        dialog.ShowDialog()
      End Using

    End Sub

  End Class

End Namespace
