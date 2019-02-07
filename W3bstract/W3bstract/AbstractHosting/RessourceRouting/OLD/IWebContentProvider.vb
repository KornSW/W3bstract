Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Public Interface IWebContentProvider
  Inherits IDisposable

  Function GetContentNames() As String()

  Sub RespondContent(contentName As String, request As IWebRequest, response As IWebResponse, state As IWebSessionState)

End Interface
