Imports System
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports W3bstract.CefAdapter.JsBridging

<BindToJsObject("fooSvc")>
Public Class FooService

#Region " handle .NET-Event in JS "

  Dim _Ctr As New PerformanceCounter("Processor", "% Processor Time", "_Total")

  Public Sub New()
    Task.Run(
      Sub()
        Do
          Threading.Thread.Sleep(1000)
          If (CpuLoadUpdateEvent IsNot Nothing) Then
            RaiseEvent CpuLoadUpdate(DateTime.Now.ToLongTimeString, CInt(_Ctr.NextValue()))
          End If
        Loop
      End Sub
    )
  End Sub

  <AttachJsEventHandler("onCpuLoadUpdateFromDotNet")>
  Public Event CpuLoadUpdate(arg1 As String, arg2 As Integer)

#End Region

#Region " Call .NET from JS "

  <PublicateAsJsMethod("getMessage")>
  Public Function GetMessage() As String
    Return ".NET Says: " + DateTime.Now.ToLongTimeString
  End Function

  'this will be called from js (see the exit button)
  <PublicateAsJsMethod("exit")>
  Public Sub ExitApplication()
    Task.Run(
      Sub()
        Threading.Thread.Sleep(500)
        System.Windows.Forms.Application.Exit()
      End Sub
    )
  End Sub

#End Region

#Region " Call JS from .NET "

  <InjectJsMethodHandle("changeTheme")>'NOTE: will be injected from outside
  Public Property ChangeThemeMethod As Func(Of String, Boolean)

#End Region

End Class
