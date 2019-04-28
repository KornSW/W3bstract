Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Reflection
Imports Newtonsoft.Json

Public Interface IExceptionHandlingStrategy

  Sub HandleException(methodName As String, ex As Exception, rawRequest As IWebRequest, session As IWebSessionState, svcRequest As ServiceRequest, svcResponse As ServiceResponse)

End Interface

Public Class DefaultExceptionHandlingStrategy
  Implements IExceptionHandlingStrategy

  Protected Enum HandlingMode
    ErrorHttpResponse = 0
    ErrorServiceResponse = 1
    ResultServiceResponse = 2
  End Enum

  Protected Overridable Function SelectHandlingMode(methodName As String, ex As Exception) As HandlingMode
    Return HandlingMode.ErrorServiceResponse
  End Function

  Protected Overridable Sub GenerateErrorDetails(
        methodName As String, ex As Exception, rawRequest As IWebRequest, session As IWebSessionState,
        localLoopback As Boolean, details As ServiceErrorDetails, ByRef statusCode As ResponseStatus)

    If (TypeOf ex Is ArgumentException) Then
      statusCode = ResponseStatus.BadRequest
      details.MessageEN = "Bad Request!"
      details.ErrorKey = "BAD_REQUEST"

    ElseIf (TypeOf ex Is Security.SecurityException) Then
      statusCode = ResponseStatus.Forbidden
      details.MessageEN = "Forbidden!"
      details.ErrorKey = "FORBIDDEN"

    End If

  End Sub

  Public Sub HandleException(methodName As String, ex As Exception, rawRequest As IWebRequest, session As IWebSessionState, svcRequest As ServiceRequest, svcResponse As ServiceResponse) Implements IExceptionHandlingStrategy.HandleException
    Dim localLoopback = Net.IPAddress.IsLoopback(rawRequest.ClientIpAddress)
    svcResponse.Status = ResponseStatus.InternalServerError

    Select Case Me.SelectHandlingMode(methodName, ex)
      Case HandlingMode.ErrorHttpResponse
        Throw New NotImplementedException

      Case HandlingMode.ErrorServiceResponse
        If (localLoopback) Then
          svcResponse.ErrorDetails.MessageEN = ex.Message
          svcResponse.ErrorDetails.ErrorKey = "EXCEPTION"
        Else
          svcResponse.ErrorDetails.MessageEN = "internal server error"
          svcResponse.ErrorDetails.ErrorKey = "ERROR"
        End If
        Me.GenerateErrorDetails(methodName, ex, rawRequest, session, localLoopback, svcResponse.ErrorDetails, svcResponse.Status)

      Case HandlingMode.ResultServiceResponse
        Throw New NotImplementedException

    End Select
  End Sub

End Class
