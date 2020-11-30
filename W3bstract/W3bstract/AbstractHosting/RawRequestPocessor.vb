
Imports System
Imports System.IO
Imports W3bstract.AbstractHosting.InMemory

Public Class RawRequestPocessor

  Private _SessionStateLookupMethod As Func(Of IWebRequest, IWebSessionState)

  Public Sub New(requestHandler As IWebRequestHandler, sessionStateLookupMethod As Func(Of IWebRequest, IWebSessionState))
    _SessionStateLookupMethod = sessionStateLookupMethod
    Me.RequestHandler = requestHandler
  End Sub

  Public ReadOnly Property RequestHandler As IWebRequestHandler

  Public Function ProcessRawRequest(address As String, methodVerb As String, payload As String) As String

    Dim request As New InMemoryWebRequest(methodVerb, address, payload)
    Using response As New InMemoryWebResponse()

      Dim session = _SessionStateLookupMethod.Invoke(request)

      RequestHandler.ProcessRequest(request, response, session)

      Dim rawResponse As String = response.ToRawString()

      Return rawResponse
    End Using
  End Function

End Class
