Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Reflection

Public Interface IWebServiceFacade
  Inherits IWebRequestHandler

  'Property ExceptionMessageDisarmingMethod As Func(Of IWebRequest, Exception, String)
  Property ExceptionHandlingStrategy As IExceptionHandlingStrategy

End Interface

Partial Public Class WebServiceFacade(Of TServiceContract)
  Implements IWebServiceFacade

  Private _Service As TServiceContract
  Private _RequstHooks As IWebServiceRequestHook()
  Private _Methods As Dictionary(Of String, MethodInvokationAdapter)
  Private _LockObj As New Object

  Public Sub New(serviceInstance As TServiceContract, ParamArray requstHooks() As IWebServiceRequestHook)
    _Service = serviceInstance
    _RequstHooks = requstHooks
  End Sub

  Private Function GetMethodByName(methodName As String) As MethodInvokationAdapter
    SyncLock _LockObj
      methodName = methodName.ToLower()

      If (_Methods Is Nothing) Then
        'build the index...
        _Methods = New Dictionary(Of String, MethodInvokationAdapter)
        For Each m In GetType(TServiceContract).GetMethods()
          If (m.IsPublic) Then
            _Methods.Add(m.Name.ToLower(), New MethodInvokationAdapter(_Service, m, _RequstHooks))
          End If
        Next
      End If

      If (_Methods.ContainsKey(methodName)) Then
        Return _Methods(methodName)
      Else
        Return Nothing
      End If

    End SyncLock
  End Function

  Public Sub ProcessRequest(request As IWebRequest, response As IWebResponse, session As IWebSessionState) Implements IWebRequestHandler.ProcessRequest
    Dim serializer As IWebSerializer = Nothing
    Dim requestDto As ServiceRequest = Nothing '<<< NEW WAY!!!
    Dim methodName As String = String.Empty

    Try

      If (request.HttpMethod.Equals("options", StringComparison.CurrentCultureIgnoreCase)) Then
        response.StatusCode = 200 'OK
        Exit Sub
      End If

      Dim result As Object = Nothing
      Dim writer As New StreamWriter(response.Stream)
      Dim urlValiables = request.ParseQuery()

      Dim communicationFormatName = "xaml"
      If (request.Headers.Item("Content-Type") = "application/json") Then
        communicationFormatName = "json"
      End If
      urlValiables.TryGetItem("Format", True, communicationFormatName)

      Try
        serializer = WebSerializer.GetInstanceByFormatName(communicationFormatName)
      Catch ex As Exception
        writer.Write("Unknown Format!")
        Exit Sub
      End Try


      urlValiables.TryGetItem("Method", True, methodName)


      If (String.IsNullOrWhiteSpace(methodName)) Then
        'FALLBACK TO NEW MODE
        Dim postBody As String = Nothing
        If (request.HttpMethod = "POST" OrElse request.HttpMethod = "PUT") Then
          Using sr As New StreamReader(request.InputStream)
            postBody = sr.ReadToEnd()
          End Using
        End If
        If (Not String.IsNullOrWhiteSpace(postBody)) Then
          requestDto = DirectCast(serializer.Deserialize(postBody, GetType(ServiceRequest)), ServiceRequest)
          methodName = requestDto.CallArguments.MethodName
        End If
      End If

      Dim method = Me.GetMethodByName(methodName)
      If (method Is Nothing) Then
        response.StatusCode = 501 'not implmented
        writer.Write("Unknown Method!")
        Exit Sub
      End If

      result = method.Invoke(request, session, response, serializer, requestDto)

      If (requestDto Is Nothing) Then
        '##### BEG OLD WAY ################################################

        If (result Is Nothing) Then
          Exit Sub 'idr. bei OPTIONS COMMANDO!!!

        ElseIf (TypeOf (result) Is String) Then
          response.ContentWriter.Write(DirectCast(result, String))

        ElseIf (TypeOf (result) Is Byte()) Then
          Dim resultBa = DirectCast(result, Byte())
          response.Stream.Write(resultBa, 0, resultBa.Length)

        ElseIf (TypeOf (result) Is Stream) Then
          If (TypeOf (result) Is MimeTaggedStream) Then
            response.ContentMimeType = DirectCast(result, MimeTaggedStream).MimeType
          Else
            response.ContentMimeType = "application/octet-stream"
          End If
          DirectCast(result, Stream).CopyTo(response.Stream)

        ElseIf (TypeOf (result) Is Drawing.Image) Then
          response.ContentMimeType = "image/png"
          DirectCast(result, Drawing.Image).Save(response.Stream, Drawing.Imaging.ImageFormat.Png)

        ElseIf (TypeOf (result) Is FileInfo) Then
          With DirectCast(result, FileInfo)

            If (Not .Exists) Then
              response.ContentMimeType = "text/plain"
              response.ContentWriter.Write("File not found!")
              Exit Sub
            End If

            response.Header("Content-Disposition") = $"attachment; filename=""{ .Name }"""

            Select Case .Extension.ToLower()
              Case ".htm", ".html" : response.ContentMimeType = "text/html"
              Case ".jpg", ".jpeg" : response.ContentMimeType = "image/jpeg"
              Case ".png" : response.ContentMimeType = "image/png"
              Case ".xml" : response.ContentMimeType = "application/xml"
              Case ".pdf" : response.ContentMimeType = "application/pdf"
              Case Else : response.ContentMimeType = "application/octet-stream"
            End Select

            Using fs As New FileStream(.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)
              fs.CopyTo(response.Stream)
            End Using

          End With

        Else 'REGULAR RESULT OBJECT

          'response.HeaderEncoding = System.Text.Encoding.UTF8
          'response.Headers.Add("Content-Type", "application/json")

          'If (TypeOf (result) Is Array) Then
          '    result = New DataSetResponse With {.Data = DirectCast(result, Array)}
          '    'result = New ArrayResponse With {.Data = DirectCast(result, Array)}
          '  End If

          'HACK: automatic wrapping into DataResponse Classes
          If (TypeOf (result) Is Array) Then
            result = New DataSetResponse With {.Data = DirectCast(result, Array)}
            'result = New ArrayResponse With {.Data = DirectCast(result, Array)}
          ElseIf (Not TypeOf (result) Is DataResponse) Then
            result = New ScalarDataResponse With {.Data = result}
          End If
          Dim sr As String = serializer.Serialize(result)
          response.ContentMimeType = serializer.MimeType
          response.ContentWriter.Write(sr)

        End If

        '##### END OLD WAY ################################################
      Else
        '##### BEG NEW WAY ################################################



        'TODO: Sonderfälle dass returnvalue Stream, Image oder FileInfo ist!!!!



        'wrap into a responseCaplse
        Dim responseCaplse = New ServiceResponse With {
            .CallResultData = New ServiceCallResults With {.ReturnValue = result},
            .Status = ResponseStatus.OK
        }

        If (result IsNot Nothing) Then
          responseCaplse.CallResultData.ReturnTypeName = result.GetType().Name
        End If

        Dim serializedResult As String = serializer.Serialize(responseCaplse)
        response.ContentMimeType = serializer.MimeType
        response.ContentWriter.Write(serializedResult)

        '##### END NEW WAY ################################################

      End If

    Catch ex As Exception

      If (TypeOf ex Is TargetInvocationException AndAlso ex.InnerException IsNot Nothing) Then
        ex = ex.InnerException
      End If

      'Me.HandleError(ex, request, response, session)

      Dim responseDto As New ServiceResponse With {.ErrorDetails = New ServiceErrorDetails}
      responseDto.Status = ResponseStatus.InternalServerError
      responseDto.ErrorDetails.MessageEN = "internal server error"
      responseDto.ErrorDetails.ErrorKey = "INTERNAL_SERVER_ERROR"

      Me.GetExceptionHandlingStrategy(methodName).HandleException(methodName, ex, request, session, requestDto, responseDto)

      If (serializer Is Nothing) Then
        response.StatusCode = responseDto.Status
        response.ContentMimeType = "text/plain"
        response.ContentWriter.Write(responseDto.ErrorDetails.MessageEN)
        Exit Sub
      End If

      '##### BEG OLD WAY ################################################
      If (requestDto Is Nothing) Then
        Dim errResponse As New ErrorResponse
        errResponse.MessageEN = responseDto.ErrorDetails.MessageEN
        errResponse.Key = responseDto.ErrorDetails.ErrorKey
        errResponse.Placeholders = responseDto.ErrorDetails.Placeholders
        Try
          Dim serializedResult As String = serializer.Serialize(errResponse)
          response.ContentMimeType = serializer.MimeType
          response.ContentWriter.Write(serializedResult)
        Catch
        End Try

        Exit Sub '<<<<<<<<<<<
      End If
      '##### END OLD WAY ################################################

      Try
        Dim serializedResult As String = serializer.Serialize(responseDto)
        response.ContentMimeType = serializer.MimeType
        response.ContentWriter.Write(serializedResult)
      Catch
      End Try

    End Try

  End Sub

  'HACK: Umbauen auf konfigurativ
  Public Property ExceptionHandlingStrategy As IExceptionHandlingStrategy = New DefaultExceptionHandlingStrategy Implements IWebServiceFacade.ExceptionHandlingStrategy

  Private Function GetExceptionHandlingStrategy(methodName As String) As IExceptionHandlingStrategy
    Return Me.ExceptionHandlingStrategy
  End Function

  'Public Property ExceptionMessageDisarmingMethod As Func(Of IWebRequest, Exception, String) = Function(r, ex) "processing error" Implements IWebServiceFacade.ExceptionMessageDisarmingMethod

  'Protected Overridable Sub HandleError(ex As Exception, request As IWebRequest, response As IWebResponse, session As IWebSessionState)
  '  Me.ExceptionLoggingMethod.Invoke(ex, request, session)
  'End Sub

  'Public Property ExceptionLoggingMethod As Action(Of Exception, IWebRequest, IWebSessionState) = (
  '  Sub(ex As Exception, request As IWebRequest, session As IWebSessionState)
  '    System.Diagnostics.Trace.TraceError("{0} {1}", ex.Message, ex.StackTrace, request.Url)
  '  End Sub
  ')

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  ''' Dispose the current object instance and suppress the finalizer
  ''' </summary>
  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Public Sub Dispose() Implements IDisposable.Dispose
    If (Not _AlreadyDisposed) Then
      Me.Disposing()
      _AlreadyDisposed = True
    End If
    GC.SuppressFinalize(Me)
  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Protected Overridable Sub Disposing()


  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Protected Sub DisposedGuard()
    If (_AlreadyDisposed) Then
      Throw New ObjectDisposedException(Me.GetType.Name)
    End If
  End Sub

#End Region

  'Public Sub Configure(configurationMethod As Action(Of ServiceFacadeConfigurator(Of TServiceContract)))
  '  configurationMethod.Invoke(New ServiceFacadeConfigurator(Of TServiceContract)(Me))
  'End Sub

End Class

#Region " Experimental "

Public Class ServiceFacadeConfigurator(Of TServiceContract)

  Private _Facade As WebServiceFacade(Of TServiceContract)

  Public Sub New(facade As WebServiceFacade(Of TServiceContract))
    _Facade = facade
  End Sub

  Public Function AllPublicMethods() As IServiceFacadeMethodConfigurator(Of TServiceContract)

  End Function

  Public Function [Method](methodName As String) As IServiceFacadeMethodConfigurator(Of TServiceContract)

  End Function


End Class

Public Interface IServiceFacadeMethodConfigurator(Of TServiceContract)

  Sub Ignore()
  Sub Expose()
  Sub ExposeAs(methodName As String)


  'Sub SetPreparationMethod(preparationMethod As Action(Of ))

  Sub SetExceptionHandlingStrategy(strategy As IExceptionHandlingStrategy)

  ' Sub SetResultProcessingMethod(preparationMethod As Action(Of ))

End Interface

#End Region
