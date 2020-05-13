Imports System
Imports System.Collections.Generic

<Obsolete>
Public Class AmbienceDataBag
  Inherits List(Of CallParameter)
End Class

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

<Obsolete()>
Public Class WebServiceResponse

  Public Property ByRefArguments As WebServiceArgument()
  Public Property ResultValue As Object
  Public Property ResultTypeName As String
  Public Property FaultMessage As Object

End Class

<Obsolete()>
Public Class ErrorResponse

  Public Property MessageEN As String = "server error"
  Public Property Key As String = "ERROR"
  Public Property Placeholders As String() = {}

End Class

<Obsolete()>
Public Class ArrayResponse
  Public Property Data As Array
End Class

<Obsolete()>
Public Class DataRequest
  Inherits DataRequest(Of Object)

End Class

<Obsolete()>
Public Class DataRequest(Of TArgument)

  Public Property RequestArgument As TArgument = Nothing

End Class

<Obsolete()>
Public Class DataSetRequest
  Inherits DataSetRequest(Of Object)
End Class

<Obsolete()>
Public Class ConstraintFilter

  Public Property Name As String
  Public Property Value As String

End Class

<Obsolete()>
Public Class DataSetRequest(Of TArgument)
  Inherits DataRequest(Of TArgument)

  Public Property SortByFieldName As String
  Public Property SortReverse As Boolean

  Public Property FilterColumnRef As String = "*"
  Public Property FilterValue As String

  Public Property ConstraintFilters As ConstraintFilter()

  Public Property ItemsPerPage As Integer = 100
  Public Property PageNumber As Integer = 1

End Class

<Obsolete()>
Public Class DataResponse
  Public Property Status As ResponseStatus = ResponseStatus.OK
  Public Message As String

#Region " Convenience "

  Public Shared Function OK() As DataResponse
    Return New DataResponse()
  End Function

  Public Shared Function BadRequest(Optional message As String = Nothing) As DataResponse
    Dim r As New DataResponse
    r.SetStatusBadRequest(message)
    Return r
  End Function

  Public Shared Function Forbidden(Optional message As String = Nothing) As DataResponse
    Dim r As New DataResponse
    r.SetStatusForbidden(message)
    Return r
  End Function

  Public Shared Function InternalServerError(Optional message As String = Nothing) As DataResponse
    Dim r As New DataResponse
    r.SetStatusInternalServerError(message)
    Return r
  End Function

  Public Shared Function Unauthorized(Optional message As String = Nothing) As DataResponse
    Dim r As New DataResponse
    r.SetStatusUnauthorized(message)
    Return r
  End Function

  Public Sub SetStatusBadRequest(Optional message As String = Nothing)
    Me.Status = ResponseStatus.BadRequest
    If (message Is Nothing) Then
      message = "Bad Request!"
    End If
    Me.Message = message
  End Sub

  Public Sub SetStatusForbidden(Optional message As String = Nothing)
    Me.Status = ResponseStatus.Forbidden
    If (message Is Nothing) Then
      message = "Forbidden!"
    End If
    Me.Message = message
  End Sub

  Public Sub SetStatusInternalServerError(Optional message As String = Nothing)
    Me.Status = ResponseStatus.InternalServerError
    If (message Is Nothing) Then
      message = "Internal Server Error!"
    End If
    Me.Message = message
  End Sub

  Public Sub SetStatusUnauthorized(Optional message As String = Nothing)
    Me.Status = ResponseStatus.Unauthorized
    If (message Is Nothing) Then
      message = "Unauthorized"
    End If
    Me.Message = message
  End Sub

#End Region

End Class

Public Class NameAndId

  Public Property Name As String
  Public Property Id As Guid

End Class

<Obsolete()>
Public Class ScalarDataResponse
  Inherits DataResponse
  Public Property Data As Object = Nothing

End Class

<Obsolete()>
Public Class ScalarDataResponse(Of TData)
  Inherits DataResponse
  Public Property Data As TData = Nothing

End Class

<Obsolete()>
Public Class DataSetResponse
  Inherits DataResponse

  Public Property Data As Array = New Object() {}

  Public Property PageNumber As Integer
  Public Property TotalPageCount As Integer
  Public Property FromItemNumber As Integer
  Public Property ToItemNumber As Integer
  Public Property TotalItemCount As Integer

End Class

<Obsolete()>
Public Class DataSetResponse(Of TData)
  Inherits DataResponse

  Public Property Data As TData() = {}

  Public Property PageNumber As Integer
  Public Property TotalPageCount As Integer
  Public Property FromItemNumber As Integer
  Public Property ToItemNumber As Integer
  Public Property TotalItemCount As Integer

End Class
