Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Public Module ExtensionsForIWebSessionState

  '<ThreadStatic> 'a little bit of ambience...
  'Private _Current As IWebSessionState = Nothing

  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  'Public Sub SetAsCurrent(extendee As IWebSessionState)
  '  _Current = extendee
  'End Sub

  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  'Public Sub BindCurrent(ByRef extendee As IWebSessionState)
  '  extendee = _Current
  'End Sub

End Module
