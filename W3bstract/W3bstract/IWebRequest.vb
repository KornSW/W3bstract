Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.IO
Imports System.Net

Public Interface IWebRequest
  Inherits IDisposable

  ReadOnly Property Url As Uri
  ReadOnly Property HttpMethod As String
  ReadOnly Property ClientIpAddress As IPAddress
  ReadOnly Property ClientHostname As String
  ReadOnly Property InputStream As Stream
  ReadOnly Property Headers As NameValueCollection
  ReadOnly Property Cookies As Dictionary(Of String, String)
  ReadOnly Property Browser As String

End Interface
