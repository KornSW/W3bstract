Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Namespace AbstractHosting.InMemory

  Public Class InMemoryWebSessionStateManager
    Implements IWebSessionStateManager

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _Sessions As New Dictionary(Of String, InMemorySessionState)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _SessionLimit As Integer

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _SessionIdInspector As Func(Of IWebRequest, String)

    Public Sub New(sessionIdInspector As Func(Of IWebRequest, String), Optional sessionTimeoutMinutes As Integer = 30, Optional sessionLimit As Integer = 50)
      _SessionIdInspector = sessionIdInspector
      _SessionLimit = sessionLimit
      Me.SessionTimeoutMinutes = sessionTimeoutMinutes
    End Sub

    Public ReadOnly Property SessionTimeoutMinutes As Integer

    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
    Public ReadOnly Property Sessions As IWebSessionState()
      Get
        SyncLock _Sessions
          Return _Sessions.Values.ToArray()
        End SyncLock
      End Get
    End Property

    Public Sub ResetSession(sessionState As IWebSessionState) Implements IWebSessionStateManager.ResetSession
      SyncLock _Sessions
        If (_Sessions.ContainsKey(sessionState.SessionId)) Then
          _Sessions.Remove(sessionState.SessionId)
        End If
      End SyncLock
      sessionState.Dispose()
    End Sub

    Public Sub RemoveTimedoutSessions()
      SyncLock _Sessions
        For Each k In _Sessions.Keys.ToArray()
          Dim s = _Sessions(k)
          If (s.LastAccess.AddMinutes(Me.SessionTimeoutMinutes) < DateTime.Now) Then
            s.Dispose()
            _Sessions.Remove(k)
          End If
        Next
      End SyncLock
    End Sub

    Public Function GetSessionState(request As IWebRequest) As IWebSessionState Implements IWebSessionStateManager.GetSessionState
      Return Me.GetSessionState(request, _SessionIdInspector)
    End Function

    Public Function GetSessionState(request As IWebRequest, newSessionIdFactory As Func(Of IWebRequest, String)) As IWebSessionState Implements IWebSessionStateManager.GetSessionState
      Dim isNew As Boolean = False
      Dim sessionId As String = _SessionIdInspector.Invoke(request)
      Dim state As InMemorySessionState = Nothing

      SyncLock _Sessions

        If (_Sessions.ContainsKey(sessionId)) Then
          state = _Sessions(sessionId)
        End If

        If (state IsNot Nothing AndAlso state.LastAccess.AddMinutes(Me.SessionTimeoutMinutes) < DateTime.Now) Then
          state.Dispose()
          state = Nothing
        End If

        If (state Is Nothing) Then

          If (_Sessions.Count >= _SessionLimit) Then
            Throw New Exception($"Session Limit ({_SessionLimit}) reached!")
          End If

          isNew = True
          sessionId = newSessionIdFactory.Invoke(request)
          state = New InMemorySessionState(sessionId)
          _Sessions.Add(sessionId, state)
        End If

        state.LastAccess = DateTime.Now
      End SyncLock

      If (isNew) Then
        'its a simple way to do the cleanups...
        Me.RemoveTimedoutSessions()
      End If

      Return state
    End Function

    Public Sub ResetAllSessions() Implements IWebSessionStateManager.ResetAllSessions
      SyncLock _Sessions
        For Each s In _Sessions.Values
          s.Dispose()
        Next
        _Sessions.Clear()
      End SyncLock
    End Sub

#Region " IDisposable "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AlreadyDisposed As Boolean = False

    ''' <summary>
    ''' Dispose the current object instance
    ''' </summary>
    Protected Overridable Sub Dispose(disposing As Boolean)
      If (Not _AlreadyDisposed) Then
        If (disposing) Then
          Me.ResetAllSessions()
        End If
        _AlreadyDisposed = True
      End If
    End Sub

    ''' <summary>
    ''' Dispose the current object instance and suppress the finalizer
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
      Me.Dispose(True)
      GC.SuppressFinalize(Me)
    End Sub

#End Region

  End Class

End Namespace
