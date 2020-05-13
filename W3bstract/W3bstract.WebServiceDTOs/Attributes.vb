Imports System
Imports System.Collections.Generic

<AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=False)>
Public Class PostBodyAttribute
  Inherits Attribute
  Public Sub New()
  End Sub
End Class

<AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=False)>
Public Class SessionStateAttribute
  Inherits Attribute
  Public Sub New()
  End Sub
End Class

<AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=False)>
Public Class UrlParamAttribute
  Inherits Attribute
  Public Sub New(Optional key As String = Nothing)
    Me.Key = key
  End Sub

  Public ReadOnly Property Key As String

End Class

<AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=False)>
Public Class RequestHeaderAttribute
  Inherits Attribute
  Public Sub New(Optional key As String = Nothing)
    Me.Key = key
  End Sub

  Public ReadOnly Property Key As String

End Class

<AttributeUsage(AttributeTargets.Method, AllowMultiple:=True)>
Public Class RequireSecurityRoleAttribute
  Inherits Attribute

  Public Sub New(ParamArray andLinkedRoleNames() As String)
    Me.RoleNames = andLinkedRoleNames
  End Sub

  Public ReadOnly Property RoleNames As String()

End Class

<AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=False)>
Public Class CoockieValueAttribute
  Inherits Attribute
  Public Sub New(Optional key As String = Nothing)
    Me.Key = key
  End Sub

  Public ReadOnly Property Key As String

End Class
