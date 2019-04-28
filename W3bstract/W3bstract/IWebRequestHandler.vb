Imports System

Public Interface IWebRequestHandler
  Inherits IDisposable

  'Sub Initialize(host As IWebRuntimeHost, config As IWebSiteConfig)

  Sub ProcessRequest(request As IWebRequest, response As IWebResponse, state As IWebSessionState)

End Interface
