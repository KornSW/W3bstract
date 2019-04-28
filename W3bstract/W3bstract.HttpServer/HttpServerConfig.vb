Imports System
Imports System.Collections.Generic
Imports W3bstract.AbstractHosting

Public Class HttpServerConfig

  Public Property LogFileName As String = ""

  Public Property SessionTimeoutMinutes As Integer = 60

  Private _Sites As New List(Of WebSiteConfiguration)
  Public ReadOnly Property Sites As IList(Of WebSiteConfiguration)
    Get
      Return _Sites
    End Get
  End Property

End Class

Public Class WebSiteConfiguration
  Implements IWebSiteConfig

  Public Property SiteUid As Guid = Nothing
  Public Property Port As Integer = 8088
  Public Property IpV4Binding As String = "*"
  Public Property VirtualRootDirectory As String = "/"

  Private ReadOnly Property BaseAddress As String Implements IWebSiteConfig.BaseAddress
    Get
      Return Me.VirtualRootDirectory
    End Get
  End Property

End Class
