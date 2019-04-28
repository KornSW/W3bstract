Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Linq.Expressions
Imports System.Reflection
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports CefSharp
Imports CefSharp.WinForms

Namespace CefAdapter.JsBridging

  Public Class JsHookingAdapter
#Region "..."

    Private _JsObjectNameForDotNetClass As String
    Private _JsObjectNameToHook As String
    Private _Frame As IFrame = Nothing
    Private _IncludedMethods As Dictionary(Of String, MethodInfo) = Nothing

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="obj"></param>
    ''' <param name="jsObjectToHook">The given object name must be available under 'window.objName'</param>
    Friend Sub New(obj As Object, Optional jsObjectToHook As String = Nothing)
      Me.DotNetObject = obj

      If (String.IsNullOrWhiteSpace(jsObjectToHook)) Then
        Dim attr = obj.GetType().GetCustomAttribute(Of BindToJsObject)
        If (attr Is Nothing) Then
          Me.ObjectName = obj.GetType().Name
        Else
          Me.ObjectName = attr.JsObjectName
        End If
      Else
        Me.ObjectName = jsObjectToHook
      End If

      _JsObjectNameToHook = "window." + Me.ObjectName
      _JsObjectNameForDotNetClass = Me.ObjectName + "Hook"

    End Sub

    Public ReadOnly Property DotNetObject As Object
    Public ReadOnly Property ObjectName As String

    Friend Sub RegisterHook(browser As ChromiumWebBrowser)
      'browser.RegisterJsObject(_JsObjectNameForDotNetClass, Me.DotNetObject)
      browser.RegisterJsObject(_JsObjectNameForDotNetClass, Me)
    End Sub

    Private ReadOnly Property IncludedMethods() As MethodInfo()
      Get
        If (_IncludedMethods Is Nothing) Then
          _IncludedMethods = New Dictionary(Of String, MethodInfo)

          Dim excludedMethods As String() = {
            NameOf(ToString),
            NameOf([GetType]),
            NameOf(InvokeMethodOnJsObject),
            NameOf(InvokeMethodOnNetObject),
            NameOf(Equals),
            NameOf(Me.GetHashCode),
            NameOf(ResultCallback),
            NameOf(ActivateHook)
          }

          For Each m In Me.DotNetObject.GetType().GetMethods().Where(Function(mi) (Not excludedMethods.Contains(mi.Name))).ToArray()
            _IncludedMethods.Add(m.Name, m)
          Next

        End If
        SyncLock _IncludedMethods
          Return _IncludedMethods.Values.ToArray()
        End SyncLock
      End Get
    End Property

    Private Function MakeFirstLetterLCase(name As String) As String
      Return name.Substring(0, 1).ToLower() + name.Substring(1)
    End Function

#End Region

    Private _ApplicationIsRunning As Boolean = True

    Friend Sub ActivateHook(frame As IFrame)

      If (frame Is Nothing) Then
        Throw New ArgumentException()
      End If

      If (_Frame IsNot Nothing) Then
        Exit Sub
      End If

      _Frame = frame

      Me.HoockMethods()

      Me.WireUpDelegates()

      Me.WireUpEvents()

      AddHandler Application.ApplicationExit, Sub() _ApplicationIsRunning = False

    End Sub

#Region " Call JS -> .NET (method-redirection) "

    Private Sub HoockMethods()

      For Each method In Me.IncludedMethods()

        Dim attr = method.GetCustomAttribute(Of PublicateAsJsMethodAttribute)
        If (attr IsNot Nothing) Then

          Dim jsMethodNameToRedirect As String = attr.JsMethodName  'Me.MakeFirstLetterLCase(method.Name)

          'NOTE: when injecting a .net object into CEF, the name of .net method-names will automatically
          'change their fist character into lcase!!!
          'Dim jsMethodNameOnDotNetClass As String = Me.MakeFirstLetterLCase(method.Name)
          Dim jsMethodNameOnDotNetClass As String = Me.MakeFirstLetterLCase(NameOf(InvokeMethodOnNetObject))

          Dim parameterNames As String() = method.GetParameters().Select(Function(p) p.Name).ToArray()
          Dim parameterSignature As String = String.Join(", ", parameterNames)

          Dim optionalReturn As String = ""
          If (Not method.ReturnType = GetType(System.Void)) Then
            optionalReturn = "return "
          End If

          'Dim hoockingJsCommand As String = (
          '  $"{_JsObjectNameToHook}.{jsMethodNameToRedirect} = function({parameterSignature}) " +
          '  "{" +
          '     $"{optionalReturn}{_JsObjectNameForDotNetClass}.{jsMethodNameOnDotNetClass}({parameterSignature});" +
          '  "}"
          ')

          Dim hoockingJsCommand As String = (
            $"{_JsObjectNameToHook}.{jsMethodNameToRedirect} = function({parameterSignature}) " +
            "{" +
               $"{optionalReturn}{_JsObjectNameForDotNetClass}.{jsMethodNameOnDotNetClass}('{method.Name}', [{parameterSignature}]);" +
            "}"
          )

          _Frame.ExecuteJavaScriptAsync(hoockingJsCommand)

        End If

      Next
    End Sub

    Public Function InvokeMethodOnNetObject(methodName As String, args() As Object) As Object
      Dim targetMethod As MethodInfo = Nothing
      SyncLock _IncludedMethods
        If (Not _IncludedMethods.ContainsKey(methodName)) Then
          Return Nothing
        End If
        targetMethod = _IncludedMethods(methodName)
      End SyncLock

      Try
        'TODO: umbau auf über task.run ASYNC damit JS keines freeze erlebt! hierzu muss ein resukt-handle zurück gegeben werden!!
        'und dann in der js welt gepollt werden bis das reulst da ist ->> evtl RxJS obserbale!!!
        Return targetMethod.Invoke(Me.DotNetObject, args)

      Catch ex As Exception
        Return ex
      End Try

    End Function

#End Region

#Region " Call .NET -> JS (Delegates) "

    Private Sub WireUpDelegates()

      For Each prp In Me.DotNetObject.GetType().GetProperties().Where(Function(p) p.CanWrite AndAlso GetType([Delegate]).IsAssignableFrom(p.PropertyType))

        Dim attr = prp.GetCustomAttribute(Of InjectJsMethodHandleAttribute)
        If (attr IsNot Nothing) Then

          Dim invoker As [Delegate] = BuildDynamicDelegate(
            prp.PropertyType,
            Function(args) Me.InvokeMethodOnJsObject(attr.JsMethodName, args)
          )

          prp.SetValue(Me.DotNetObject, invoker)

        End If

      Next

    End Sub

    Private _CallbackBuffer As New Dictionary(Of String, Object)

    Protected Function InvokeMethodOnJsObject(methodName As String, ParamArray args() As Object) As Object

      If (Not _ApplicationIsRunning) Then
        Return Nothing
      End If

      Dim argStrings() As String = args.Select(
        Function(a)

          If (a Is Nothing) Then
            Return "null"
          End If

          'TODO: richtiger serializer!!!!
          If TypeOf (a) Is Integer OrElse TypeOf (a) Is Long OrElse TypeOf (a) Is Decimal OrElse TypeOf (a) Is Boolean Then
            Return a.ToString()
          Else
            Return "'" + a.ToString() + "'"
          End If

        End Function
      ).ToArray()

      Dim callId = Guid.NewGuid().ToString()
      Dim hoockingJsCommand As String =
        "{ var result = " + _JsObjectNameToHook & "." + methodName + "(" + String.Join(", ", argStrings) + "); " +
        _JsObjectNameForDotNetClass + ".resultCallback('" + callId + "', result); }"
      _Frame.ExecuteJavaScriptAsync(hoockingJsCommand)


      Do While _ApplicationIsRunning
        SyncLock _CallbackBuffer
          If (_CallbackBuffer.ContainsKey(callId)) Then
            Dim result As Object = _CallbackBuffer(callId)
            _CallbackBuffer.Remove(callId)
            Return result
          End If
        End SyncLock
        Threading.Thread.Sleep(20)
        Application.DoEvents()
      Loop

      Return Nothing
    End Function

    Public Sub ResultCallback(callId As String, result As Object)
      Task.Run(
      Sub()
        SyncLock _CallbackBuffer
          _CallbackBuffer.Add(callId, result)
        End SyncLock
      End Sub
    )
    End Sub

#End Region

#Region " Events .NET -> JS (naming-convention based) "

    'https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/how-to-hook-up-a-delegate-using-reflection

    Private Sub WireUpEvents()

      For Each evt In Me.DotNetObject.GetType().GetEvents()

        Dim attr = evt.GetCustomAttribute(Of AttachJsEventHandlerAttribute)
        If (attr IsNot Nothing) Then

          Dim handler As [Delegate] = Me.BuildDynamicDelegate(
            evt,
            Sub(args) Me.InvokeMethodOnJsObject(attr.JsMethodName, args),
            parameterFilter:=Function(p) Not (p.Type.IsClass OrElse p.Type.IsInterface OrElse p.Name = "sender")
          )

          evt.AddEventHandler(Me.DotNetObject, handler)

        End If

      Next
    End Sub

    Private Function BuildDynamicDelegate(evt As EventInfo, innerHandler As Action(Of Object()), Optional parameterFilter As Func(Of ParameterExpression, Boolean) = Nothing) As [Delegate]
      Return BuildDynamicDelegate(evt.EventHandlerType, innerHandler, parameterFilter)
    End Function

    Private Function BuildDynamicDelegate(outerDelegateType As Type, innerHandler As Action(Of Object()), Optional parameterFilter As Func(Of ParameterExpression, Boolean) = Nothing) As [Delegate]
      Return BuildDynamicDelegate(
      outerDelegateType,
      Function(args)
        innerHandler.Invoke(args)
        Return Nothing
      End Function,
      parameterFilter
    )
    End Function

    Private Function BuildDynamicDelegate(outerDelegateType As Type, innerHandler As Func(Of Object(), Object), Optional parameterFilter As Func(Of ParameterExpression, Boolean) = Nothing) As [Delegate]

      Dim protectedInnerHandlerHandler = innerHandler
      Dim parameterExpressions As New List(Of ParameterExpression)
      Dim outerDelegateMethod = outerDelegateType.GetMethod("Invoke")

      'Protection to avoid a NullReferenceException when Nothing is returned by the 
      'inner Handler and the lambda tries to convert it into a value type
      If (Not outerDelegateMethod.ReturnType = GetType(System.Void)) Then
        protectedInnerHandlerHandler = (
        Function(args)
          Dim result As Object = innerHandler.Invoke(args)

          If (result IsNot Nothing AndAlso Not outerDelegateMethod.ReturnType.IsAssignableFrom(result.GetType())) Then
            result = Nothing
          End If

          If (result Is Nothing AndAlso outerDelegateMethod.ReturnType.IsValueType) Then
            'default value of the value type
            Return Activator.CreateInstance(outerDelegateMethod.ReturnType)
          End If

          Return result
        End Function
      )
      End If

      If (parameterFilter Is Nothing) Then
        parameterFilter = Function(p) True
      End If

      For Each prm In outerDelegateMethod.GetParameters()
        parameterExpressions.Add(Expression.Parameter(prm.ParameterType, prm.Name))
      Next

      Dim castedParameterExpressions = parameterExpressions.Where(Function(pe) parameterFilter.Invoke(pe)).Select(Function(p) Expression.Convert(p, GetType(Object)))

      Dim objArryBuilderExpresssion = Expression.NewArrayInit(GetType(Object), castedParameterExpressions)


      Dim callResult = Expression.Call(Expression.Constant(protectedInnerHandlerHandler.Target), protectedInnerHandlerHandler.Method, objArryBuilderExpresssion)

      Dim body As Expression
      If (outerDelegateMethod.ReturnType = GetType(System.Void)) Then
        body = callResult
      Else
        body = Expression.Convert(callResult, outerDelegateMethod.ReturnType)
      End If


      Dim lambda = Expression.Lambda(outerDelegateType, body, parameterExpressions)

      Return lambda.Compile()
    End Function

#End Region

  End Class

End Namespace
