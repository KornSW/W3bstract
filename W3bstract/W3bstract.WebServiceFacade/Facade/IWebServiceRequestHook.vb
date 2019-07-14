Imports System
Imports System.Reflection

Namespace DynamicFacade

  Public Interface IWebServiceRequestHook

    Sub BeforeInvoke(request As IWebRequest, session As IWebSessionState, response As IWebResponse, service As Object, method As MethodInfo, ByRef skipInvoke As Boolean)
    Sub AfterInvoke(request As IWebRequest, session As IWebSessionState, response As IWebResponse, service As Object, method As MethodInfo, invokeSkipped As Boolean, ByRef result As Object)
    Sub OnException(request As IWebRequest, session As IWebSessionState, response As IWebResponse, service As Object, method As MethodInfo, ex As Exception, ByRef handled As Boolean, ByRef result As Object)

  End Interface

End Namespace
