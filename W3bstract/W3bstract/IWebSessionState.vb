Imports System
Imports System.Collections.Generic
Imports System.Diagnostics

Public Interface IWebSessionState
  Inherits IDisposable
  Inherits IDictionary(Of String, Object)

  ReadOnly Property SessionId As String

  Function GetItem(Of T As {New, IDisposable})() As T

  Property RequestSessionReset As Boolean

End Interface

Public Class WebSessionState
  Implements IDisposable

  Friend Shared Function IsCurrently(state As IWebSessionState) As IDisposable
    If (_Current IsNot Nothing) Then
      Throw New Exception("Current Thread is already bound to an WebSessionState")
    End If

    _Current = state
    Return New WebSessionState
  End Function

  Private Sub New()
  End Sub

  <ThreadStatic>
  Private Shared _Current As IWebSessionState = Nothing

  Public Shared ReadOnly Property Current As IWebSessionState
    Get
      Return _Current
    End Get
  End Property

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  ''' Dispose the current object instance
  ''' </summary>
  Protected Overridable Sub Dispose(disposing As Boolean)
    If (Not _AlreadyDisposed) Then
      If (disposing) Then
        _Current = Nothing
      End If
      _AlreadyDisposed = True
    End If
  End Sub

  ''' <summary>
  ''' Dispose the current object instance and suppress the finalizer
  ''' </summary>
  Private Sub Dispose() Implements IDisposable.Dispose
    Me.Dispose(True)
    GC.SuppressFinalize(Me)
  End Sub

#End Region

End Class
