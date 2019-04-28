Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Public Module ExtensionsForIWebRequest

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function ParseQuery(extendee As IWebRequest) As Dictionary(Of String, String)
    Return extendee.Url.ParseQuery()
  End Function

End Module
