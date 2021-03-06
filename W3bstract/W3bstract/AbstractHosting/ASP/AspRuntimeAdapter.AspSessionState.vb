﻿Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Web
Imports System.Web.SessionState

Namespace AbstractHosting.ASP

  Partial Class AspRuntimeAdapter

    Private Class AspSessionState
      Implements IWebSessionState

      Private _Server As HttpServerUtility
      Private _SessionState As HttpSessionState
      Private _AppState As HttpApplicationState
      Private _Context As HttpContext

      Public Sub New(server As HttpServerUtility, sessionState As HttpSessionState, appState As HttpApplicationState, context As HttpContext)
        _Server = server
        _SessionState = sessionState
        _AppState = appState
        _Context = context
      End Sub

      Public Property RequestSessionReset As Boolean Implements IWebSessionState.RequestSessionReset

      Friend ReadOnly Property Server As HttpServerUtility
        Get
          Return _Server
        End Get
      End Property

      Friend ReadOnly Property Application As HttpApplicationState
        Get
          Return _AppState
        End Get
      End Property

      Friend ReadOnly Property Context As HttpContext
        Get
          Return _Context
        End Get
      End Property

      Public ReadOnly Property SessionId As String Implements IWebSessionState.SessionId
        Get
          Return _SessionState.SessionID
        End Get
      End Property

      Default Public Property Item(key As String) As Object Implements IDictionary(Of String, Object).Item
        Get
          Return _SessionState.Item(key)
        End Get
        Set(value As Object)
          _SessionState.Item(key) = value
        End Set
      End Property

      Public ReadOnly Property Keys As ICollection(Of String) Implements IDictionary(Of String, Object).Keys
        Get
          Dim items As New List(Of String)
          For i As Integer = 0 To (_SessionState.Count - 1)
            items.Add(_SessionState.Keys.Item(i))
          Next
          Return items
        End Get
      End Property

      Public ReadOnly Property Values As ICollection(Of Object) Implements IDictionary(Of String, Object).Values
        Get
          Dim items As New List(Of Object)
          For i As Integer = 0 To (_SessionState.Count - 1)
            items.Add(_SessionState.Item(i))
          Next
          Return items
        End Get
      End Property

      Public ReadOnly Property Count As Integer Implements ICollection(Of KeyValuePair(Of String, Object)).Count
        Get
          Return _SessionState.Count
        End Get
      End Property

      Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).IsReadOnly
        Get
          Return _SessionState.IsReadOnly
        End Get
      End Property

      Public Function GetItem(Of TStateItem As {New, IDisposable})() As TStateItem Implements IWebSessionState.GetItem
        Dim typeName As String = GetType(TStateItem).FullName
        Dim stateItem As TStateItem
        stateItem = DirectCast(_SessionState.Item(typeName), TStateItem)

        If (stateItem Is Nothing) Then
          stateItem = New TStateItem
          _SessionState.Item(typeName) = stateItem
        End If

        Return stateItem
      End Function

      Public Function ContainsKey(key As String) As Boolean Implements IDictionary(Of String, Object).ContainsKey
        Return _SessionState.Keys.OfType(Of String).Contains(key)
      End Function

      Public Sub Add(key As String, value As Object) Implements IDictionary(Of String, Object).Add
        _SessionState.Add(key, value)
      End Sub

      Public Function Remove(key As String) As Boolean Implements IDictionary(Of String, Object).Remove
        _SessionState.Remove(key)
        Return True
      End Function

      Public Function TryGetValue(key As String, ByRef value As Object) As Boolean Implements IDictionary(Of String, Object).TryGetValue
        Dim results = Me.Enumerate.Where(Function(kvp) kvp.Key = key)
        If (Not results.Any()) Then
          Return False
        End If
        value = results.Single().Value
        Return True
      End Function

      Public Sub Add(item As KeyValuePair(Of String, Object)) Implements ICollection(Of KeyValuePair(Of String, Object)).Add
        _SessionState.Add(item.Key, item.Value)
      End Sub

      Public Sub Clear() Implements ICollection(Of KeyValuePair(Of String, Object)).Clear
        _SessionState.Clear()
      End Sub

      Public Function Contains(item As KeyValuePair(Of String, Object)) As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).Contains
        Return Me.Enumerate.Where(Function(kvp) kvp.Key = item.Key).Any()
      End Function

      Public Sub CopyTo(array() As KeyValuePair(Of String, Object), arrayIndex As Integer) Implements ICollection(Of KeyValuePair(Of String, Object)).CopyTo
        Me.Enumerate.ToList().CopyTo(array, arrayIndex)
      End Sub

      Public Function Remove(item As KeyValuePair(Of String, Object)) As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).Remove
        _SessionState.Remove(item.Key)
        Return True
      End Function

      Public Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of String, Object)) Implements IEnumerable(Of KeyValuePair(Of String, Object)).GetEnumerator
        Return Me.Enumerate.GetEnumerator()
      End Function

      Private Function GetEnumeratorUntyped() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Me.Enumerate.GetEnumerator()
      End Function

      Private Iterator Function Enumerate() As IEnumerable(Of KeyValuePair(Of String, Object))
        For Each k In _SessionState.Keys.OfType(Of String)
          Yield New KeyValuePair(Of String, Object)(k, _SessionState(k))
        Next
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

  End Class

End Namespace
