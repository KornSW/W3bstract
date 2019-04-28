Imports System.ComponentModel
Imports System.IO
Imports System.Net
Imports System
Imports System.Diagnostics
Imports System.Reflection
Imports CefSharp
Imports CefSharp.WinForms
Imports System.Collections.Generic
Imports System.Drawing

Namespace AbstractHosting.CEF

  Partial Class CefRuntimeAdapter

    <DebuggerDisplay("CefRequestContext {Identifier}")>
    Public NotInheritable Class CefRequestContext
      Inherits ResourceHandler

      'https://stackoverflow.com/questions/28697613/working-with-locally-built-web-page-in-cefsharp

#Region " Fields & Constructor "

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _Owner As CefRuntimeAdapter

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _WebRequestHandler As IWebRequestHandler

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _Request As CefWebRequestWrapper

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _Response As CefWebResponseWrapper = Nothing

      <DebuggerBrowsable(DebuggerBrowsableState.Never)>
      Private _Url As String

      Public Sub New(owner As CefRuntimeAdapter, cefRequest As IRequest, webRequestHandler As IWebRequestHandler)
        _Owner = owner
        _Url = cefRequest.Url
        _WebRequestHandler = webRequestHandler
      End Sub

#End Region

#Region " Info Properties "

      Public ReadOnly Property Identifier As ULong
        Get
          Return _Request.WrappedCefRequest.Identifier
        End Get
      End Property

      Public ReadOnly Property Request As IWebRequest
        Get
          Return _Request
        End Get
      End Property

      Public ReadOnly Property Response As IWebResponse
        Get
          Return _Response
        End Get
      End Property

      Public ReadOnly Property Url As String
        Get
          Return _Url
        End Get
      End Property

#End Region

#Region " Processing "

      Public Overrides Function ProcessRequestAsync(cefRequest As IRequest, callback As ICallback) As Boolean
        If (cefRequest.Url = _Url) Then
          callback.Continue()
          _Request = New CefWebRequestWrapper(cefRequest)
          _Owner.NotifyRequestBegin(Me)
          Return True
        Else
          callback.Cancel()
          Return False
        End If
      End Function

      Public Overrides Function GetResponse(cefResponse As IResponse, ByRef responseLength As Long, ByRef redirectUrl As String) As Stream

        Dim sessionState = _Owner.WebSessionStateManager.GetSessionState(_Request)

        _Response = New CefWebResponseWrapper(cefResponse, sessionState)
        _WebRequestHandler.ProcessRequest(_Request, _Response, sessionState)
        responseLength = _Response.Stream.Length
        If (_Response.Stream.Position > 0) Then
          _Response.Stream.Position = 0
        End If
        If (sessionState.RequestSessionReset) Then
          _Owner.WebSessionStateManager.ResetSession(sessionState)
        End If
        _Owner.NotifyRequestEnd(Me)
        'Me.AutoDisposeStream = True
        Return _Response.Stream
      End Function

#End Region

    End Class

  End Class

End Namespace
