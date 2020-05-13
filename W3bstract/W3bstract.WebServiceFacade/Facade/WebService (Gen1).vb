Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Web
Imports W3bstract.ServiceCommunication.Serialization

Namespace DynamicFacade

  <Obsolete("please use WebServiceFacade")>
  Public Class WebService(Of TServiceContract)
    Implements IWebRequestHandler

    Private _WrappedImplementation As TServiceContract
    Private _AmbienceChannels() As IAmbienceChannel
    Private _MethodsPerNameAndSignature As New Dictionary(Of String, Dictionary(Of Type(), Func(Of Object(), Object)))
    Private _Contract As New List(Of WebMethodDescriptor)

    Public Sub New(wrappedImplementation As TServiceContract, ParamArray ambienceChannels() As IAmbienceChannel)
      _WrappedImplementation = wrappedImplementation
      _AmbienceChannels = ambienceChannels
      Me.BuildMethodIndex()
    End Sub

    Private Sub BuildMethodIndex()
      For Each mi In GetType(TServiceContract).GetMethods()
        If (mi.IsPublic) Then


          'TODO: keine statischen!!!!

          'TODO: keine systemmethoden Tostring Gethash....


          If (Not _MethodsPerNameAndSignature.ContainsKey(mi.Name)) Then
            _MethodsPerNameAndSignature.Add(mi.Name, New Dictionary(Of Type(), Func(Of Object(), Object)))
          End If
          Dim signautes = _MethodsPerNameAndSignature(mi.Name)
          signautes.Add(mi.GetParameters().Select(Function(p) p.ParameterType).ToArray(),
          Function(args As Object())
            Return mi.Invoke(_WrappedImplementation, args)
          End Function
        )
          Dim methodDesc As New WebMethodDescriptor
          methodDesc.MethodName = mi.Name
          If (Not mi.ReturnType.Name = "System.Void") Then
            methodDesc.ReturnTypeName = mi.ReturnType.Name
          End If
          methodDesc.Arguments = mi.GetParameters().Select(
          Function(p)
            Return New WebArgumentDescriptor With {
              .ArgumentName = p.Name,
              .ArgumentTypeName = p.ParameterType.Name,
              .IsByRef = p.IsOut
            }
          End Function
        ).ToArray()

          _Contract.Add(methodDesc)
        End If
      Next
    End Sub

    'Public Sub ProcessRequest(request As HttpRequest, response As HttpResponse)
    '  Dim streamReader As New StreamReader(request.InputStream)

    '  Dim rawRequest = streamReader.ReadToEnd()
    '  If (rawRequest.Length > 5) Then


    '    Dim requestBag = Xaml.Deserialize(Of WebServiceRequest)(rawRequest)
    '    Dim responseBag = Me.ProcessRequest(requestBag)
    '    Dim serializedResponse As String = Xaml.Serialize(responseBag)
    '    response.Write(serializedResponse)
    '  End If
    'End Sub

    Public Sub ProcessRequest(request As IWebRequest, response As IWebResponse, state As IWebSessionState) Implements IWebRequestHandler.ProcessRequest

      Dim urlParams = HttpUtility.ParseQueryString(request.Url.Query.ToLower())
      Dim streamReader As New StreamReader(request.InputStream)
      Dim rawRequest = streamReader.ReadToEnd()
      Dim method As String = ""

      Dim communicationFormat As String = "xaml"
      If (urlParams.AllKeys.Contains("format")) Then
        communicationFormat = urlParams("format")
      End If
      If (urlParams.AllKeys.Contains("method")) Then
        method = urlParams("method")
      End If

      Dim requestBag As WebServiceRequest
      If (String.IsNullOrWhiteSpace(rawRequest)) Then
        requestBag = New WebServiceRequest
      Else
        Try
          Select Case communicationFormat
            Case "xaml"
              requestBag = Xaml.Deserialize(Of WebServiceRequest)(rawRequest)
            Case "json"

              'TODO: json.NET     mit typinormationen
              'Dim settings As New JsonSerializerSettings()
              'settings.TypeNameHandling = TypeNameHandling.All

              'Dim s = JsonSerializer.CreateDefault()
              'requestBag =
            Case Else
              response.ContentWriter.Write($"Unknown format '{communicationFormat}'")
              Exit Sub
          End Select
        Catch ex As Exception
          response.ContentWriter.Write($"Cannot parse the POST-Data beacause it is not in a compatible {communicationFormat}-Format!")
          Exit Sub
        End Try
      End If

      Dim serializedResponse As String

      If (String.IsNullOrEmpty(method)) Then
        'serializedResponse = Xaml.Serialize(_Contract.ToArray())
        'response.ContentWriter.Write(serializedResponse)
        'Exit Sub
        method = request.HttpMethod
      End If

      If (rawRequest.Length < 6) Then
        requestBag = New WebServiceRequest

        Dim args As New List(Of WebServiceArgument)
        For Each pKey In urlParams.AllKeys
          If (pKey = "method") Then
            requestBag.MethodName = urlParams.Get(pKey)
          Else
            args.Add(New WebServiceArgument With {.ParamName = pKey, .Value = urlParams.Get(pKey)})
          End If
        Next
        requestBag.RequestArguments = args.ToArray()
      Else

      End If

      Dim responseBag = Me.ProcessRequest(requestBag)
      serializedResponse = Xaml.Serialize(responseBag)
      response.ContentWriter.Write(serializedResponse)

      ' response.ContentWriter.Write("'WebServiceRequest'-Structure missing!!!")

    End Sub

    Public Function ProcessRequest(request As WebServiceRequest) As WebServiceResponse
      Dim response As New WebServiceResponse
      Dim successfullyRedirected As Boolean = False
      Try

        If (Not _MethodsPerNameAndSignature.ContainsKey(request.MethodName)) Then
          Throw New NotImplementedException()
        End If

        Dim signatures = _MethodsPerNameAndSignature(request.MethodName)
        For Each signature As Type() In signatures.Keys
          If (signature.Length = request.RequestArguments.Length) Then
            Dim argumentValues = request.RequestArguments.Select(Function(a) a.Value).ToArray()
            Dim match As Boolean = True
            For i As Integer = 0 To request.RequestArguments.Length - 1
              If (argumentValues(i) IsNot Nothing AndAlso Not signature(i).IsAssignableFrom(argumentValues(i).GetType())) Then
                match = False
              End If
            Next
            If (match) Then
              response.ResultValue = signatures(signature).Invoke(argumentValues)
              If (response.ResultValue IsNot Nothing) Then
                response.ResultTypeName = response.ResultValue.GetType().Name
              End If
              successfullyRedirected = True
            End If
          End If
        Next

        If (Not successfullyRedirected) Then
          Throw New NotImplementedException()
        End If

      Catch ex As Exception
        response.FaultMessage = ex.Message
      End Try

      Return response
    End Function

#Region " IDisposable "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AlreadyDisposed As Boolean = False

    ''' <summary>
    ''' Dispose the current object instance and suppress the finalizer
    ''' </summary>
    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public Sub Dispose() Implements IDisposable.Dispose
      If (Not _AlreadyDisposed) Then
        Me.Disposing()
        _AlreadyDisposed = True
      End If
      GC.SuppressFinalize(Me)
    End Sub

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Protected Overridable Sub Disposing()


    End Sub

    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Protected Sub DisposedGuard()
      If (_AlreadyDisposed) Then
        Throw New ObjectDisposedException(Me.GetType.Name)
      End If
    End Sub

#End Region

  End Class

  Public Class WebMethodDescriptor

    Public Property MethodName As String
    Public Property ReturnTypeName As String
    Public Property Arguments As WebArgumentDescriptor()

  End Class

  Public Class WebArgumentDescriptor
    Public Property ArgumentName As String
    Public Property ArgumentTypeName As String
    Public Property IsByRef As Boolean = False

  End Class

End Namespace
