Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Reflection

Public Class WebServiceClient(Of TServiceContract)
  Implements IDynamicProxyInvoker

  Public ReadOnly Property Url As String
  Public ReadOnly Property Facade As TServiceContract

  Private _AmbienceChannels() As IAmbienceChannel
  Private _WebClient As New WebClient

  Public Shared Function CreateInstance(url As String, ParamArray ambienceChannels() As IAmbienceChannel) As TServiceContract
    Dim inst As New WebServiceClient(Of TServiceContract)(url, ambienceChannels)
    Return inst.Facade
  End Function

  Private Sub New(url As String, ambienceChannels() As IAmbienceChannel)
    Me.Url = url
    _AmbienceChannels = ambienceChannels
    Me.Facade = DynamicProxy.CreateInstance(Of TServiceContract)(Me)
  End Sub

  Public Function InvokeMethod(methodName As String, arguments() As Object) As Object Implements IDynamicProxyInvoker.InvokeMethod
    Dim requestBag As New WebServiceRequest
    requestBag.MethodName = methodName
    requestBag.RequestArguments = arguments.Select(Function(a) New WebServiceArgument With {.Value = a}).ToArray()

    Dim rawResponse = _WebClient.UploadString(_Url, Xaml.Serialize(requestBag))
    Dim responseBag = Xaml.Deserialize(Of WebServiceResponse)(rawResponse)

    Return responseBag.ResultValue
  End Function

End Class
