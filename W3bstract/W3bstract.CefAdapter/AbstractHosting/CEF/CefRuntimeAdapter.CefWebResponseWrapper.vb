Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Reflection
Imports System.Text
Imports System.Web
Imports CefSharp
Imports CefSharp.WinForms
Imports W3bstract

Namespace AbstractHosting.CEF

  Partial Class CefRuntimeAdapter

    <DebuggerDisplay("Response: {ContentMimeType}")>
    Public Class CefWebResponseWrapper
      Implements IWebResponse

#Region " Fields & Constructor "

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _WrappedCefResponse As IResponse

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _State As IWebSessionState

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _Writer As StreamWriter = Nothing

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _OutputStream As New MemoryStream(4096)

      Public Sub New(wrappedCefResponse As IResponse, state As IWebSessionState)
        _State = state
        _WrappedCefResponse = wrappedCefResponse
      End Sub

#End Region

      Public ReadOnly Property WrappedCefResponse As IResponse
        Get
          Return _WrappedCefResponse
        End Get
      End Property

      Public Property ContentMimeType As String Implements IWebResponse.ContentMimeType
        Get
          Return _WrappedCefResponse.MimeType
        End Get
        Set(value As String)
          _WrappedCefResponse.MimeType = value
        End Set
      End Property

      Public Property StatusCode As Integer Implements IWebResponse.StatusCode
        Get
          Return _WrappedCefResponse.StatusCode
        End Get
        Set(value As Integer)
          _WrappedCefResponse.StatusCode = value
        End Set
      End Property

      Public ReadOnly Property ContentWriter As TextWriter Implements IWebResponse.ContentWriter
        Get
          If (_Writer Is Nothing) Then
            _Writer = New StreamWriter(_OutputStream)
            _Writer.AutoFlush = True
          End If
          Return _Writer
        End Get
      End Property

      Public ReadOnly Property Stream As Stream Implements IWebResponse.Stream
        Get
          Return _OutputStream
        End Get
      End Property

      Public Property Header(name As String) As String Implements IWebResponse.Header
        Get
          Return _WrappedCefResponse.Headers().Item(name)
        End Get
        Set(value As String)
          _WrappedCefResponse.Headers().Item(name) = value
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

  End Class

End Namespace
