Imports System.Text
Imports System
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports W3bstract.AbstractHosting.InMemory
Imports W3bstract.ServiceCommunication.Connector
Imports W3bstract.ServiceCommunication.DynamicFacade

<TestClass()>
Public Class WebServiceIntegrationTest

#Region " Infrastructure "

  Protected Sub InvokeUsingMock1Infrastucture(testMethod As Action(Of MockService1, WebServiceFacade(Of IMockService1), WebRequestRouter, IMockService1))

    Dim mockService1 As New MockService1

    Using stateMgr As New InMemoryWebSessionStateManager(Function(r) "SHARED")
      Using mockService1Facade As New WebServiceFacade(Of IMockService1)(mockService1)
        Using serverVirtualDirectory As New WebRequestRouter()

          serverVirtualDirectory.RegisterDynamicTarget(mockService1Facade, "mock1")

          Dim serverEndpoint As New RawRequestPocessor(serverVirtualDirectory, AddressOf stateMgr.GetSessionState)

          Dim client As IMockService1 = WebServiceClient(Of IMockService1).CreateInstance(
           "http://localhost/mock1",
            AddressOf serverEndpoint.ProcessRawRequest 'client and server have a direct wire-up without any network transport 
          )

          testMethod.Invoke(mockService1, mockService1Facade, serverVirtualDirectory, client)

        End Using
      End Using
    End Using

  End Sub

#End Region

  <TestMethod()>
  Public Sub InMemoryWebserviceCall()
    Dim result As String = Nothing

    Me.InvokeUsingMock1Infrastucture(
     Sub(service, facade, server, client)

       result = client.Method2("Hallo", 4)

     End Sub
    )

    Assert.AreEqual("Foo Hallo Bar 8", result)

  End Sub

End Class
