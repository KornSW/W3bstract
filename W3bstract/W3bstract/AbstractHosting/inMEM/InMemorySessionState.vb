Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Namespace AbstractHosting.InMemory

  <DebuggerDisplay("{SessionId}")>
  Public Class InMemorySessionState
    Implements IWebSessionState

#Region " Fields & Constructor "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _SessionId As String = Guid.NewGuid().ToString()

    Public Sub New(sessionId As String)
      _SessionId = sessionId
    End Sub

    Public Sub New()
      _SessionId = Guid.NewGuid().ToString()
    End Sub

#End Region

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Public ReadOnly Property SessionId As String Implements IWebSessionState.SessionId
      Get
        Return _SessionId
      End Get
    End Property

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _Items As New Dictionary(Of String, Object)

    Public Property LastAccess As DateTime = DateTime.Now

    Public Property RequestSessionReset As Boolean = False Implements IWebSessionState.RequestSessionReset

    Default Public Property Item(key As String) As Object Implements IDictionary(Of String, Object).Item
      Get
        SyncLock _Items
          Return _Items(key)
        End SyncLock
      End Get
      Set(value As Object)
        SyncLock _Items
          _Items(key) = value
        End SyncLock
      End Set
    End Property

    Public ReadOnly Property Keys As ICollection(Of String) Implements IDictionary(Of String, Object).Keys
      Get
        SyncLock _Items
          Return _Items.Keys
        End SyncLock
      End Get
    End Property

    Public ReadOnly Property Values As ICollection(Of Object) Implements IDictionary(Of String, Object).Values
      Get
        SyncLock _Items
          Return _Items.Values
        End SyncLock
      End Get
    End Property

    Public Function ContainsKey(key As String) As Boolean Implements IDictionary(Of String, Object).ContainsKey
      SyncLock _Items
        Return _Items.ContainsKey(key)
      End SyncLock
    End Function

    Public Sub Add(key As String, value As Object) Implements IDictionary(Of String, Object).Add
      SyncLock _Items
        _Items.Add(key, value)
      End SyncLock
    End Sub

    Public Function Remove(key As String) As Boolean Implements IDictionary(Of String, Object).Remove
      SyncLock _Items
        Return _Items.Remove(key)
      End SyncLock
    End Function

    Public Function TryGetValue(key As String, ByRef value As Object) As Boolean Implements IDictionary(Of String, Object).TryGetValue
      SyncLock _Items
        Return _Items.TryGetValue(key, value)
      End SyncLock
    End Function

    Public Sub Add(item As KeyValuePair(Of String, Object)) Implements ICollection(Of KeyValuePair(Of String, Object)).Add
      Throw New NotImplementedException()
    End Sub

    Public Sub Clear() Implements ICollection(Of KeyValuePair(Of String, Object)).Clear
      SyncLock _Items
        _Items.Clear()
      End SyncLock
    End Sub

    Public Function Contains(item As KeyValuePair(Of String, Object)) As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).Contains
      Throw New NotImplementedException()
    End Function

    Public Sub CopyTo(array() As KeyValuePair(Of String, Object), arrayIndex As Integer) Implements ICollection(Of KeyValuePair(Of String, Object)).CopyTo
      Throw New NotImplementedException()
    End Sub

    Public Function Remove(item As KeyValuePair(Of String, Object)) As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).Remove
      Throw New NotImplementedException()
    End Function

    Public Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of String, Object)) Implements IEnumerable(Of KeyValuePair(Of String, Object)).GetEnumerator
      Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
      Throw New NotImplementedException()
    End Function

    Public ReadOnly Property Count As Integer Implements ICollection(Of KeyValuePair(Of String, Object)).Count
      Get
        SyncLock _Items
          Return _Items.Count
        End SyncLock
      End Get
    End Property

    Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).IsReadOnly
      Get
        Return False
      End Get
    End Property

    Public Function GetItem(Of TStateItem As {New, IDisposable})() As TStateItem Implements IWebSessionState.GetItem
      SyncLock _Items

        Dim typeName As String = GetType(TStateItem).FullName
        Dim stateItem As TStateItem

        If (_Items.ContainsKey(typeName)) Then
          stateItem = DirectCast(_Items(typeName), TStateItem)
        Else
          stateItem = New TStateItem
          _Items.Add(typeName, stateItem)
        End If

        Return stateItem
      End SyncLock
    End Function

#Region " IDisposable "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AlreadyDisposed As Boolean = False

    ''' <summary>
    ''' Dispose the current object instance
    ''' </summary>
    Protected Overridable Sub Dispose(disposing As Boolean)
      If (Not _AlreadyDisposed) Then
        If (disposing) Then
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
