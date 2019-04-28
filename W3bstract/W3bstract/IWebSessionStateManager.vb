Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.IO
Imports System.Net

Public Interface IWebSessionStateManager
  Inherits IDisposable

  Function GetSessionState(request As IWebRequest) As IWebSessionState
  Function GetSessionState(request As IWebRequest, newSessionIdFactory As Func(Of IWebRequest, String)) As IWebSessionState

  Sub ResetSession(sessionState As IWebSessionState)
  Sub ResetAllSessions()

End Interface
