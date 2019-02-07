Imports System
Imports System.IO
Imports System.ComponentModel

Public Interface IWebResponse
  Inherits IDisposable

  Property ContentMimeType As String
  ReadOnly Property ContentWriter As TextWriter

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  ReadOnly Property Stream As Stream

  Property StatusCode As Integer

  Property Header(name As String) As String

End Interface
