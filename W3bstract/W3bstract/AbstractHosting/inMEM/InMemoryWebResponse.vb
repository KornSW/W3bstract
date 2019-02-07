Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Web
Imports System.Web.SessionState
Imports SmartCoreFx
Imports SmartCoreFx.Web

Public Class InMemoryWebResponse
  Implements IWebResponse

  Private _Writer As StreamWriter = Nothing
  Private _Headers As New Dictionary(Of String, String)

  Public Sub New()
    _Headers.Add("ContentMimeType", "text/html")
  End Sub

  Public Property ContentMimeType As String Implements IWebResponse.ContentMimeType
    Get
      Return Me.Header("ContentMimeType")
    End Get
    Set(value As String)
      Me.Header("ContentMimeType") = value
    End Set
  End Property

  Public ReadOnly Property ContentWriter As TextWriter Implements IWebResponse.ContentWriter
    Get
      If (_Writer Is Nothing) Then
        _Writer = New StreamWriter(Me.Stream)
        _Writer.AutoFlush = True
      End If
      Return _Writer
    End Get
  End Property

  Public ReadOnly Property Stream As Stream = New MemoryStream Implements IWebResponse.Stream

  Public Property StatusCode As Integer = 200 Implements IWebResponse.StatusCode

  Public Property Header(name As String) As String Implements IWebResponse.Header
    Get
      If (_Headers.ContainsKey(name)) Then
        Return _Headers(name)
      End If
      Return Nothing
    End Get
    Set(value As String)
      If (_Headers.ContainsKey(name)) Then
        If (value Is Nothing) Then
          _Headers.Remove(name)
        Else
          _Headers(name) = value
        End If
      Else
        If (value IsNot Nothing) Then
          _Headers.Add(name, value)
        End If
      End If
    End Set
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
        If (_Writer IsNot Nothing) Then
          _Writer.Flush()
          _Writer.Dispose()
          _Writer = Nothing
        End If
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
