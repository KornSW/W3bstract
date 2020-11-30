Imports System

Public Class MockService1
  Implements IMockService1

  Public Sub Method1() Implements IMockService1.Method1
    Throw New Exception("Method1 says: BOOOOM!")
  End Sub

  Public Function Method2(arg1 As String, arg2 As Integer) As String Implements IMockService1.Method2
    Return $"Foo {arg1} Bar {(arg2 * 2)}"
  End Function

End Class
