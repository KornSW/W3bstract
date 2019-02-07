Imports System
Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.CompilerServices

Public Module ExtensionMethods

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function ApplyMimeTag(extendee As Stream, mimeType As String) As MimeTaggedStream
    If (TypeOf (extendee) Is MimeTaggedStream) Then
      Throw New Exception("The Stream is already Tagged")
    End If
    Return New MimeTaggedStream(extendee, mimeType)
  End Function

End Module
