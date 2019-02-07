Imports System
Imports System.Collections.Generic

Public Interface IWebSessionState
  Inherits IDisposable
  Inherits IDictionary(Of String, Object)

  ReadOnly Property SessionId As String

  Function GetItem(Of T As {New, IDisposable})() As T








  Property RequestSessionReset As Boolean

End Interface
