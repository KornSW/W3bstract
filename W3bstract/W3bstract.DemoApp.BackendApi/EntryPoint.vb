Imports W3bstract
Imports W3bstract.WebServiceFacade

Public Class WebApiEntryPoint
  Inherits WebRequestRouter

  Public Sub Initialize()


    Me.RegisterDynamicTarget(New WebServiceFacade(Of DataService)(New DataService), "datasvc")



  End Sub

End Class

Public Class DataService









End Class