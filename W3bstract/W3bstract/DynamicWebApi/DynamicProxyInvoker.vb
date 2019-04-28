Imports System
Imports System.Collections.Generic

Public Interface IDynamicProxyInvoker

  Function InvokeMethod(methodName As String, arguments As Object()) As Object

  'Function GetPropertyValue(propertyName As String, indexArguments As Object()) As Object
  'Sub SetPropertyValue(propertyName As String, value As Object, indexArguments As Object())
  'Event Raise(eventName As String, arguments As Object())

End Interface

Public Class DynamicProxyInvoker
  Implements IDynamicProxyInvoker

  Dim methodsPerNameAndSignature As New Dictionary(Of String, Dictionary(Of Type(), Func(Of Object(), Object)))

  Public Sub DefineMethod(methodName As String, [method] As Action)
    If (Not methodsPerNameAndSignature.ContainsKey(methodName)) Then
      methodsPerNameAndSignature.Add(methodName, New Dictionary(Of Type(), Func(Of Object(), Object)))
    End If
    methodsPerNameAndSignature(methodName).Add({},
    Function(args As Object())
      [method].Invoke()
      Return Nothing
    End Function
  )
  End Sub

  Public Sub DefineMethod(Of TArg1)(methodName As String, [method] As Action(Of TArg1))
    If (Not methodsPerNameAndSignature.ContainsKey(methodName)) Then
      methodsPerNameAndSignature.Add(methodName, New Dictionary(Of Type(), Func(Of Object(), Object)))
    End If
    methodsPerNameAndSignature(methodName).Add({GetType(TArg1)},
    Function(args As Object())
      [method].Invoke(DirectCast(args(0), TArg1))
      Return Nothing
    End Function
 )
  End Sub

  Public Sub DefineMethod(Of TArg1, TArg2)(methodName As String, [method] As Action(Of TArg1, TArg2))
    If (Not methodsPerNameAndSignature.ContainsKey(methodName)) Then
      methodsPerNameAndSignature.Add(methodName, New Dictionary(Of Type(), Func(Of Object(), Object)))
    End If
    methodsPerNameAndSignature(methodName).Add({GetType(TArg1), GetType(TArg2)},
    Function(args As Object())
      [method].Invoke(DirectCast(args(0), TArg1), DirectCast(args(1), TArg2))
      Return Nothing
    End Function
  )
  End Sub

  Public Sub DefineMethod(Of TResult)(methodName As String, [method] As Func(Of TResult))
    Dim signature = methodName + "()"
    If (Not methodsPerNameAndSignature.ContainsKey(methodName)) Then
      methodsPerNameAndSignature.Add(methodName, New Dictionary(Of Type(), Func(Of Object(), Object)))
    End If
    methodsPerNameAndSignature(methodName).Add({},
    Function(args As Object())
      Return [method].Invoke()
    End Function
  )
  End Sub

  Public Sub DefineMethod(Of TArg1, TResult)(methodName As String, [method] As Func(Of TArg1, TResult))
    Dim argTypeNames As String() = {GetType(TArg1).Name}
    Dim signature = methodName + "(" + String.Join(",", argTypeNames) + ")"
    If (Not methodsPerNameAndSignature.ContainsKey(methodName)) Then
      methodsPerNameAndSignature.Add(methodName, New Dictionary(Of Type(), Func(Of Object(), Object)))
    End If
    methodsPerNameAndSignature(methodName).Add({GetType(TArg1)},
    Function(args As Object())
      Return [method].Invoke(DirectCast(args(0), TArg1))
    End Function
  )
  End Sub

  Public Sub DefineMethod(Of TArg1, TArg2, TResult)(methodName As String, [method] As Func(Of TArg1, TArg2, TResult))
    If (Not methodsPerNameAndSignature.ContainsKey(methodName)) Then
      methodsPerNameAndSignature.Add(methodName, New Dictionary(Of Type(), Func(Of Object(), Object)))
    End If
    methodsPerNameAndSignature(methodName).Add({GetType(TArg1), GetType(TArg2)},
    Function(args As Object())
      Return [method].Invoke(DirectCast(args(0), TArg1), DirectCast(args(1), TArg2))
    End Function
  )
  End Sub

  Public Function InvokeMethod(methodName As String, arguments() As Object) As Object Implements IDynamicProxyInvoker.InvokeMethod

    If (methodsPerNameAndSignature.ContainsKey(methodName)) Then
      Dim signatures = methodsPerNameAndSignature(methodName)
      For Each signature As Type() In signatures.Keys
        If (signature.Length = arguments.Length) Then
          Dim match As Boolean = True
          For i As Integer = 0 To arguments.Length - 1
            If (arguments(i) IsNot Nothing AndAlso Not signature(i).IsAssignableFrom(arguments(i).GetType())) Then
              match = False
            End If
          Next
          If (match) Then
            Return signatures(signature).Invoke(arguments)
          End If
        End If
      Next
    End If

    Throw New NotImplementedException
  End Function

End Class
