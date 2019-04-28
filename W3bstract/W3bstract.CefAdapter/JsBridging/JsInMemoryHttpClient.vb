Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Threading.Tasks
Imports W3bstract.AbstractHosting.InMemory

Namespace CefAdapter.JsBridging

  <BindToJsObject("w3bstractHttpClient")>
  Public Class JsInMemoryHttpClient

#Region " nested Class 'RequestProcessingContext' "

    Public Class RequestProcessingContext

      Public Sub New(webRequestHandler As IWebRequestHandler, webSessionStateManager As IWebSessionStateManager, handle As Integer, httpMethod As String, url As String, payload As String, callback As Action(Of RequestProcessingContext, Integer, String))
        Me.Handle = handle

        Task.Run(
        Sub()
          Dim response As String = Nothing
          Dim statusCode As Integer = 500 'internal server error

          Try
            Using reqStream As New MemoryStream

              If (Not String.IsNullOrWhiteSpace(payload)) Then
                Dim sw As New StreamWriter(reqStream)
                sw.Write(payload)
                sw.Flush()
                reqStream.Position = 0
              End If

              Using req As New InMemoryWebRequest(httpMethod, url, reqStream), res As New InMemoryWebResponse

                Dim webSessionState = webSessionStateManager.GetSessionState(req)

                webRequestHandler.ProcessRequest(req, res, webSessionState)
                statusCode = res.StatusCode

                If (webSessionState.RequestSessionReset) Then
                  webSessionStateManager.ResetSession(webSessionState)
                End If

                If (res.Stream.Position > 0 AndAlso res.Stream.CanSeek) Then
                  res.Stream.Position = 0
                End If
                Using sr As New StreamReader(res.Stream)
                  response = sr.ReadToEnd()
                End Using

              End Using

            End Using
          Finally
            callback.Invoke(Me, statusCode, response)
          End Try
        End Sub
      )

      End Sub

      Public ReadOnly Property Handle As Integer

    End Class

#End Region

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _LastHandle As Integer = 0

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _HandleLock As New Object

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _Requests As New List(Of RequestProcessingContext)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _WebRequestHandler As IWebRequestHandler

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _WebSessionStateManager As IWebSessionStateManager

    Public Sub New(webRequestHandler As IWebRequestHandler, webSessionStateManager As IWebSessionStateManager)
      _WebRequestHandler = webRequestHandler
      _WebSessionStateManager = webSessionStateManager
    End Sub

    Public ReadOnly Property WebRequestHandler As IWebRequestHandler
      Get
        Return _WebRequestHandler
      End Get
    End Property

    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
    Public ReadOnly Property RequestQueue As RequestProcessingContext()
      Get
        SyncLock _Requests
          Return _Requests.ToArray()
        End SyncLock
      End Get
    End Property

    <PublicateAsJsMethod("queueRequest")>
    Public Function QueueRequest(httpMethod As String, url As String, payload As String) As Integer

      Dim currentHandle As Integer
      SyncLock _HandleLock
        If (_LastHandle = Integer.MaxValue) Then
          _LastHandle = 1
        Else
          _LastHandle += 1
        End If
        currentHandle = _LastHandle
      End SyncLock

      Dim context As New RequestProcessingContext(_WebRequestHandler, _WebSessionStateManager, currentHandle, httpMethod, url, payload, AddressOf Me.HandleResponse)
      SyncLock _Requests
        _Requests.Add(context)
      End SyncLock

      Return currentHandle
    End Function

    Private Sub HandleResponse(context As RequestProcessingContext, httpStatus As Integer, response As String)

      If (Me.ResponseCallback IsNot Nothing) Then
        Task.Run(
        Sub()
          Try
            Me.ResponseCallback.Invoke(context.Handle, httpStatus, response)
          Catch
          End Try
        End Sub
      )
      End If

      SyncLock _Requests
        _Requests.Remove(context)
      End SyncLock

    End Sub

    Private _ResponseCallback As Action(Of Integer, Integer, String)

    <InjectJsMethodHandle("handleResponse")> 'NOTE: will be injected from outside by the JsHoockingAdapter
    Public Property ResponseCallback As Action(Of Integer, Integer, String)
      Get
        Return _ResponseCallback
      End Get
      Set(value As Action(Of Integer, Integer, String))
        _ResponseCallback = value

        'convention -> this is to inform the js representation about the fact, that is has been hoocked!
        _ResponseCallback.Invoke(-1, 200, "")

      End Set
    End Property

#Region " Push "

    <AttachJsEventHandler("onPushReceived")>
    Public Event Push(message As String)

    Public Sub SendPush(message As String)
      If (PushEvent IsNot Nothing) Then
        RaiseEvent Push(message)
      End If
    End Sub

#End Region

  End Class

End Namespace
