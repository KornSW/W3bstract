Imports System

Public Interface IRawRequestHook

  Sub BeforeProcess(request As IWebRequest, session As IWebSessionState, response As IWebResponse, ByRef skipProcessing As Boolean)

  Sub AfterProcess(request As IWebRequest, session As IWebSessionState, response As IWebResponse, processingSkipped As Boolean)

  Sub OnCatchingException(request As IWebRequest, session As IWebSessionState, response As IWebResponse, ex As Exception, ByRef catchException As Boolean)

End Interface
