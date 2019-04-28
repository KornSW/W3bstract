Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

'Public Class HttpSessionState
'  Implements IWebSessionState

'  Private _Items As New Dictionary(Of Type, IDisposable)
'  Private _SessionId As String

'  Public Sub New(sessionId As String)
'    _SessionId = sessionId
'  End Sub

'  Public Property LastAccess As DateTime = DateTime.Now

'  Public ReadOnly Property SessionId As String Implements IWebSessionState.SessionId
'    Get
'      Return _SessionId
'    End Get
'  End Property

'  Public Function GetItem(Of T As {New, IDisposable})() As T Implements IWebSessionState.GetItem
'    Dim tKey As Type = GetType(T)
'    If (_Items.ContainsKey(tKey)) Then
'      Return DirectCast(_Items(tKey), T)
'    Else
'      Dim newInstance As New T
'      _Items.Add(tKey, newInstance)
'      Return newInstance
'    End If
'  End Function

'  Public Property RequestSessionReset As Boolean = False Implements IWebSessionState.RequestSessionReset

'#Region " IDisposable "

'  Public ReadOnly Property IsDisposed As Boolean
'    Get
'      Return _AlreadyDisposed
'    End Get
'  End Property

'  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
'  Private _AlreadyDisposed As Boolean = False

'  ''' <summary>
'  ''' Dispose the current object instance
'  ''' </summary>
'  Protected Overridable Sub Dispose(disposing As Boolean)
'    If (Not _AlreadyDisposed) Then
'      If (disposing) Then
'        For Each tKey In _Items.Keys
'          _Items(tKey).TryDispose()
'        Next
'        _Items.Clear()
'        _Items = Nothing
'      End If
'      _AlreadyDisposed = True
'    End If
'  End Sub

'  ''' <summary>
'  ''' Dispose the current object instance and suppress the finalizer
'  ''' </summary>
'  Public Sub Dispose() Implements IDisposable.Dispose
'    Me.Dispose(True)
'    GC.SuppressFinalize(Me)
'  End Sub

'#End Region

'End Class
