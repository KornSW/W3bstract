Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Reflection
Imports W3bstract.ServiceCommunication.Connector.Dynamics
Imports W3bstract.ServiceCommunication.Connector.Serialization

Public Class WebServiceClient(Of TServiceContract)
  Implements IDynamicProxyInvoker

  Public ReadOnly Property Url As String
  Public ReadOnly Property Facade As TServiceContract

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AmbienceChannels() As IAmbienceChannel

  Public Shared Function CreateInstance(url As String, ParamArray ambienceChannels() As IAmbienceChannel) As TServiceContract
    Dim inst As New WebServiceClient(Of TServiceContract)(url, AddressOf ProcessHttpCall, ambienceChannels)
    Return inst.Facade
  End Function

  Public Shared Function CreateInstance(url As String, rawRequestProcessingMethod As RawRequestProcessingDelegate, ParamArray ambienceChannels() As IAmbienceChannel) As TServiceContract
    Dim inst As New WebServiceClient(Of TServiceContract)(url, rawRequestProcessingMethod, ambienceChannels)
    Return inst.Facade
  End Function

  Private Sub New(url As String, rawRequestProcessingMethod As RawRequestProcessingDelegate, ambienceChannels() As IAmbienceChannel)
    Me.Url = url
    _RawRequestProcessingMethod = rawRequestProcessingMethod
    _AmbienceChannels = ambienceChannels
    Me.Facade = DynamicProxy.CreateInstance(Of TServiceContract)(Me)
  End Sub

  Private Function CollectAmbientPayload() As CallParameter()
    Dim snapshot As New List(Of CallParameter)
    For Each ambienceChannel As IAmbienceChannel In _AmbienceChannels
      ambienceChannel.InjectOutgoingData(snapshot)
    Next
    Return snapshot.ToArray()
  End Function

  Private Shared _CookieContainer As New CookieContainer()

  Private _RawRequestProcessingMethod As RawRequestProcessingDelegate

  Public Delegate Function RawRequestProcessingDelegate(address As String, methodVerb As String, payload As String) As String

  Protected Shared Function ProcessHttpCall(address As String, methodVerb As String, payload As String) As String
    Dim rawResponse = ExtendedWebClient.GetInstance().UploadString(address, methodVerb, payload)
    Return rawResponse
  End Function

  Public Function InvokeMethod(methodName As String, arguments() As Object, argumentNames As String()) As Object Implements IDynamicProxyInvoker.InvokeMethod
    Dim requestBag As New ServiceRequest
    requestBag.CallArguments = New ServiceCallArguments
    requestBag.CallArguments.MethodName = methodName

    requestBag.CallArguments.MethodArguments = Me.ConvertArgs(arguments, argumentNames).ToArray()
    requestBag.CallArguments.AmbientPayload = Me.CollectAmbientPayload().ToArray()

    'Dim rawResponse = ExtendedWebClient.GetInstance().UploadString(_Url, "POST", Xaml.Serialize(requestBag))
    Dim rawResponse = _RawRequestProcessingMethod.Invoke(_Url, "POST", Xaml.Serialize(requestBag))

    'Dim req = HttpWebRequest.CreateHttp(_Url)
    'req.CookieContainer = _CookieContainer
    'req.Method = "POST"
    'Dim sw As New StreamWriter(req.GetRequestStream())
    'sw.Write(Xaml.Serialize(requestBag))
    'sw.Flush()
    'Dim resp = DirectCast(req.GetResponse, HttpWebResponse)
    'Dim respStr = resp.GetResponseStream
    'Dim rawResponse = respStr.ReadAllText()

    If (String.IsNullOrWhiteSpace(rawResponse)) Then
      Return Nothing
    End If

    Dim responseBag = Xaml.Deserialize(Of ServiceResponse)(rawResponse)

    Return responseBag.CallResultData.ReturnValue
  End Function

  Private Iterator Function ConvertArgs(arguments() As Object, argumentNames As String()) As IEnumerable(Of CallParameter)
    For i As Integer = 0 To arguments.Length - 1
      Yield New CallParameter With {.ParamName = argumentNames(i), .Value = arguments(i)}
    Next
  End Function

End Class
