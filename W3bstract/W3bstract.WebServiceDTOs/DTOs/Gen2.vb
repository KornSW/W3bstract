Imports System
Imports System.Collections.Generic

Public Class ServiceRequest
  Public Property AuthData As ServiceAuthArguments = Nothing
  Public Property CallArguments As ServiceCallArguments = Nothing
End Class

Public Class ServiceAuthArguments
  Public Property Token As String = Nothing
  Public Property AuthId As String = Nothing
  Public Property AuthHash As String = Nothing
End Class

Public Class ServiceCallArguments
  Public Property MethodName As String = Nothing
  Public Property MethodArguments As CallParameter() = {}
  Public Property AmbientPayload As CallParameter() = Nothing
End Class

Public Class ServiceResponse
  Public Property Status As ResponseStatus
  Public Property ErrorDetails As ServiceErrorDetails = Nothing
  Public Property CallResultData As ServiceCallResults = Nothing
End Class

Public Class ServiceErrorDetails
  Public Property ErrorKey As String
  Public Property Placeholders As String() = {}
  Public Property MessageEN As String
End Class

Public Class ServiceCallResults
  Public Property ReturnValue As Object = Nothing
  Public Property ReturnTypeName As String = Nothing
  Public Property ByRefResults As CallParameter() = {}
  Public Property AmbientPayload As CallParameter() = Nothing
End Class

Public Class CallParameter
  Public Property ParamName As String = ""
  Public Property Value As Object = Nothing
  Public Property TypeName As String = Nothing
End Class

Public Class ItemReferer
  Public Property ItemClass As String
  Public Property Identifier As Long
  Public Property DisplayLabel As String
  Public Property Icon As Byte() = Nothing
End Class

Public Class GuidBasedItemReferer
  Public Property ItemClass As String
  Public Property Identifier As Guid
  Public Property DisplayLabel As String
  Public Property Icon As Byte() = Nothing
End Class