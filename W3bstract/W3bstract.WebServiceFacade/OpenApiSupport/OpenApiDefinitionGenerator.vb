Imports W3bstract.ServiceCommunication.DynamicFacade

Namespace OpenApiSupport

  Public Class OpenApiDefinitionGenerator
    Implements IWebRequestHandler

    Private disposedValue As Boolean

    Public Sub New(facade As IWebServiceFacade)








    End Sub

    Public Sub ProcessRequest(request As IWebRequest, response As IWebResponse, state As IWebSessionState) Implements IWebRequestHandler.ProcessRequest








    End Sub

    Public Sub Dispose() Implements System.IDisposable.Dispose
    End Sub

  End Class

End Namespace
