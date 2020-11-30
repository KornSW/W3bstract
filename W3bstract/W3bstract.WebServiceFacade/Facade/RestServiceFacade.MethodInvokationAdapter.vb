Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports W3bstract.ServiceCommunication.Serialization

Namespace DynamicFacade

  Partial Class RestServiceFacade(Of TServiceContract)

    Friend Class MethodInvokationAdapter

      Private _Service As TServiceContract
      Private _RequstHooks As IWebServiceRequestHook()
      Private _Method As MethodInfo

      Private _ParamterFactories As New List(Of Func(Of IWebRequest, IWebSessionState, IWebResponse, IWebSerializer, ServiceRequest, Object))
      'Private _RequiredSecurityRoleSets As New List(Of String())

      Public Sub New(service As TServiceContract, method As MethodInfo, ParamArray requstHooks() As IWebServiceRequestHook)
        _Service = service
        _Method = method
        _RequstHooks = requstHooks

        'For Each a In _Method.GetCustomAttributes(True)
        '  Select Case a.GetType()
        '    Case GetType(RequireSecurityRoleAttribute)
        '      _RequiredSecurityRoleSets.Add(DirectCast(a, RequireSecurityRoleAttribute).RoleNames)
        '  End Select
        'Next

        For Each p In _Method.GetParameters()
          _ParamterFactories.Add(Me.BuildParameterValueGetterDelegate(p))
        Next

      End Sub

      Public ReadOnly Property Method As MethodInfo
        Get
          Return _Method
        End Get
      End Property

      Private Function BuildParameterValueGetterDelegate(parameter As ParameterInfo) As Func(Of IWebRequest, IWebSessionState, IWebResponse, IWebSerializer, ServiceRequest, Object)
        Dim attributes = parameter.GetCustomAttributes(True)
        Dim name = parameter.Name
        Dim lowerName = name.ToLower()
        Dim paramtype As Type = parameter.ParameterType

        For Each a In attributes
          Select Case a.GetType()
            Case GetType(PostBodyAttribute)
              Return (
              Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object
                Dim postBody As String = Nothing
                If (request.HttpMethod = "POST" OrElse request.HttpMethod = "PUT") Then
                  Using sr As New StreamReader(request.InputStream)
                    postBody = sr.ReadToEnd()
                  End Using
                End If
                If (String.IsNullOrWhiteSpace(postBody)) Then
                  Return Nothing
                Else
                  Return serializer.Deserialize(postBody, paramtype)
                End If
              End Function
            )
            Case GetType(SessionStateAttribute)
              Return (
              Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object

                Dim itm As Object = Nothing
                If (session.ContainsKey(name)) Then
                  itm = session.Item(name)
                End If

                If (itm Is Nothing AndAlso paramtype.HasParameterlessConstructor()) Then
                  itm = Activator.CreateInstance(paramtype)
                  session.Item(name) = itm
                  'session.CookieMode = HttpCookieMode.UseCookies
                  'response.SetCookie(session.CookieMode = HttpCookieMode.UseCookies)
                  'response.SetCookie(New HttpCookie("Session","")
                End If
                Return itm
              End Function
            )
            Case GetType(UrlParamAttribute)
              Dim key = DirectCast(a, UrlParamAttribute).Key
              Return (
              Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object
                Return request.ParseQuery().Item(key)
              End Function
            )
            Case GetType(RequestHeaderAttribute)
              Dim key = DirectCast(a, RequestHeaderAttribute).Key
              Return (
              Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object
                Return request.Headers.Item(key)
              End Function
            )
            Case GetType(CoockieValueAttribute)
              Dim key = DirectCast(a, CoockieValueAttribute).Key
              Return (
              Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object

                Throw New NotImplementedException

                'Dim c As HttpCookie = request.Cookies.Get(key)
                'If (c Is Nothing) Then
                '  c = New HttpCookie(key)
                'End If
                'response.SetCookie(c)
                'Return c
              End Function
            )
          End Select
        Next

        Select Case (parameter.ParameterType)
          Case GetType(IWebRequest)
            Return (
            Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object
              Return request
            End Function
          )
          Case GetType(IWebResponse)
            Return (
            Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object
              Return response
            End Function
          )
          Case GetType(IWebSessionState)
            Return (
            Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object
              Return session
            End Function
          )
          Case GetType(ServiceRequest)
            Return (
            Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object
              Return requestCapsle
            End Function
         )
        End Select

        Return (
         Function(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object

           If (requestCapsle Is Nothing) Then
             Throw New NotImplementedException
           End If

           Dim arg = requestCapsle.CallArguments.MethodArguments.Where(Function(a) a.ParamName.ToLower() = lowerName).SingleOrDefault()
           If (arg IsNot Nothing) Then

             If (arg.Value IsNot Nothing) Then
               'HACK: UGLY CONVERSION!!!
               Dim reTypedArgValue As Object = JsonTypeConversion.MakeTyped(parameter.ParameterType, arg.Value)
               Return reTypedArgValue
             End If

           End If

           Return Nothing
         End Function
      )

      End Function

      Public Function Invoke(request As IWebRequest, session As IWebSessionState, response As IWebResponse, serializer As IWebSerializer, requestCapsle As ServiceRequest) As Object

        If (request.HttpMethod.Equals("options", StringComparison.CurrentCultureIgnoreCase)) Then
          Return Nothing
        End If

        Dim resultObject As Object = Nothing
        Dim parameterValues As New List(Of Object)

        Dim decideExceptionCatch As Func(Of Exception, Boolean) = (
          Function(ex)
            Dim catchException As Boolean = False
            For Each hook In _RequstHooks
              hook.OnException(request, session, response, _Service, _Method, ex, catchException, resultObject)
            Next
            Return catchException
          End Function
         )

        'this an ambient publication using a ThreadStatic variable
        'TODO: umbau auf AsyncLocal
        Using WebSessionState.IsCurrently(session)

          Try

            Dim skipInvoke As Boolean = False
            For Each hook In _RequstHooks
              hook.BeforeInvoke(request, session, response, _Service, _Method, skipInvoke)
            Next

            If (Not skipInvoke) Then

              SyncLock _ParamterFactories
                For Each pf In _ParamterFactories
                  parameterValues.Add(pf.Invoke(request, session, response, serializer, requestCapsle))
                Next
              End SyncLock

              Dim params = _Method.GetParameters()
              For i As Integer = 0 To params.Length - 1
                If (TypeOf (parameterValues.Item(i)) Is String) Then
                  Dim pType = params(i).ParameterType
                  If (Not pType = GetType(String)) Then
                    Dim input = DirectCast(parameterValues.Item(i), String)
                    If (Not pType.TryParse(input, parameterValues.Item(i))) Then
                      Throw New Exception($"Cannot Parse input '{input}' into a '{pType.Name}' for Parameter '{params(i).Name}' of Method '{_Method.Name}'.")
                    End If
                  End If
                End If
              Next

              resultObject = _Method.Invoke(_Service, parameterValues.ToArray())

            End If

            For Each hook In _RequstHooks
              hook.AfterInvoke(request, session, response, _Service, _Method, skipInvoke, resultObject)
            Next

          Catch ex As Exception When decideExceptionCatch.Invoke(ex)
          End Try

        End Using
        'here (on dispose) the ambient publication will be cleared!

        Return resultObject
      End Function

    End Class

  End Class

End Namespace
