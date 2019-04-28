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

    <DebuggerDisplay("Request: {HttpMethod} {Url}")>
    Public Class CefWebRequestWrapper
      Implements IWebRequest

#Region " Fields & Constructor "

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _WrappedCefRequest As IRequest

      Public Sub New(wrappedCefRequest As IRequest)
        _WrappedCefRequest = wrappedCefRequest
      End Sub

#End Region

#Region " Properties "

      Public ReadOnly Property WrappedCefRequest As IRequest
        Get
          Return _WrappedCefRequest
        End Get
      End Property

      Public ReadOnly Property InputStream As Stream Implements IWebRequest.InputStream
        Get
          Dim postDataLength As Integer = 0
          Dim postData = _WrappedCefRequest.PostData

          For Each postDataElement In postData.Elements
            postDataLength += postDataElement.Bytes.Length
          Next

          Dim postDataStream As New MemoryStream(postDataLength)
          For Each postDataElement In postData.Elements
            postDataStream.Write(postDataElement.Bytes, 0, postDataElement.Bytes.Length)
          Next
          postDataStream.Seek(0, SeekOrigin.Begin)

          Return postDataStream
        End Get
      End Property

      Public ReadOnly Property Url As Uri Implements IWebRequest.Url
        Get
          Return New Uri(_WrappedCefRequest.Url)
        End Get
      End Property

      Public ReadOnly Property ClientHostname As String Implements IWebRequest.ClientHostname
        Get
          Return "localhost"
        End Get
      End Property

      Public ReadOnly Property ClientIpAddress As IPAddress Implements IWebRequest.ClientIpAddress
        Get
          Return IPAddress.Parse("127.0.0.1")
        End Get
      End Property

      Public ReadOnly Property HttpMethod As String Implements IWebRequest.HttpMethod
        Get
          Return _WrappedCefRequest.Method
        End Get
      End Property

      Public ReadOnly Property Headers As NameValueCollection Implements IWebRequest.Headers
        Get
          Return _WrappedCefRequest.Headers
        End Get
      End Property

      Private _Cookies As New Dictionary(Of String, String)
      Public ReadOnly Property Cookies As Dictionary(Of String, String) Implements IWebRequest.Cookies
        Get
          'TODO: not implemented!!!
          Return _Cookies
        End Get
      End Property

      Public ReadOnly Property Browser As String Implements IWebRequest.Browser
        Get
          Return "Chromium (CEF)"
        End Get
      End Property

#End Region

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
