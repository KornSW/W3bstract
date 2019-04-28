Imports System.Reflection
Imports W3bstract

Public Class SpaBundleEntryPoint
  Inherits WebRequestRouter

  Public Sub New(backendApiUrl As String)

    Me.RegisterEmbeddedFiles(Assembly.GetExecutingAssembly())

  End Sub

End Class
