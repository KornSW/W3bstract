Imports System

Namespace CefAdapter.JsBridging

  ''' <summary>
  ''' Note: This works ONLY FOR VALUE-TYPE PARAMETERS! (and byRef arguments are currently not supported!!!)
  ''' </summary>
  <AttributeUsage(AttributeTargets.Property, AllowMultiple:=False)>
  Public Class InjectJsMethodHandleAttribute
    Inherits Attribute

    Public Sub New(jsMethodName As String)
      Me.JsMethodName = jsMethodName
    End Sub

    Public ReadOnly Property JsMethodName As String

  End Class

  ''' <summary>
  ''' Note: This works ONLY FOR VALUE-TYPE PARAMETERS!
  ''' </summary>
  <AttributeUsage(AttributeTargets.Event, AllowMultiple:=False)>
  Public Class AttachJsEventHandlerAttribute
    Inherits Attribute

    Public Sub New(jsMethodName As String)
      Me.JsMethodName = jsMethodName
    End Sub

    Public ReadOnly Property JsMethodName As String

  End Class

  ''' <summary>
  ''' Note: This works ONLY FOR VALUE-TYPE PARAMETERS!
  ''' </summary>
  <AttributeUsage(AttributeTargets.Method, AllowMultiple:=False)>
  Public Class PublicateAsJsMethodAttribute
    Inherits Attribute

    Public Sub New(jsMethodName As String)
      Me.JsMethodName = jsMethodName
    End Sub

    Public ReadOnly Property JsMethodName As String

  End Class

  ''' <summary>
  ''' Note: This works ONLY FOR VALUE-TYPE PARAMETERS!
  ''' </summary>
  <AttributeUsage(AttributeTargets.Class, AllowMultiple:=False)>
  Public Class BindToJsObject
    Inherits Attribute

    ''' <summary>
    ''' </summary>
    ''' <param name="jsObjectName">The given object name must be available under 'window.objName'</param>
    Public Sub New(jsObjectName As String)
      Me.JsObjectName = jsObjectName
    End Sub

    Public ReadOnly Property JsObjectName As String

  End Class

End Namespace
