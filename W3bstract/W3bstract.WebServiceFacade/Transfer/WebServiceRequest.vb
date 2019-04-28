Imports System

<Obsolete()>
Public Class WebServiceRequest

  Public Property MethodName As String
  Public Property RequestArguments As WebServiceArgument()
  Public Property GenericArgumentTypeNames As String()
  Public Property AmbientData As AmbienceDataBag

End Class

<Obsolete()>
Public Class WebServiceArgument

  Public Property ParamName As String
  Public Property TypeName As String
  Public Property Value As Object

End Class
