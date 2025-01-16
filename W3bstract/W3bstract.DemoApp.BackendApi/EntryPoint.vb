Imports W3bstract
Imports W3bstract.ServiceCommunication.DynamicFacade
Imports W3bstract.ServiceCommunication.OpenApiSupport

Public Class WebApiEntryPoint
  Inherits WebRequestRouter

  Public Sub Initialize()

    Dim service = New DataService
    Dim facade = New WebServiceFacade(Of DataService)(service)
    Dim oapiGen = New OpenApiDefinitionGenerator(facade)
    Dim swaggerUi = New SwaggerUiBundle("v1/oapi.json")

    Me.RegisterDynamicTarget(facade, "v1/datasvc")
    Me.RegisterDynamicTarget(oapiGen, "v1/oapi.json")
    Me.RegisterDynamicTarget(swaggerUi, "v1/swagger")

  End Sub

End Class

Public Class DataService

  Public Function MyMethod1(arg1 As String, arg2 As Integer) As String
    Return arg1 + " " + (arg2 * 2).ToString()
  End Function

  Public Function MyMethod2(arg1 As String, arg2 As Integer) As String
    Return arg1 + " " + (arg2 * 2).ToString()
  End Function

End Class
