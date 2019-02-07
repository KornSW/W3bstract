Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Reflection
Imports System.Web
Imports System.Web.SessionState

Public Class RequestHandler
#Region "..."
  Implements IHttpHandler
  Implements IRequiresSessionState
  Implements IDisposable

  Private WithEvents _AppDom As AppDomain
  Private _ApplicationAssembly As Assembly
  Private _RuntimeAssembly As Assembly

  'these fields are untyped because the objects are loaded from outside
  '(via assembly resolving). We have to do so because the target assembly which  
  'contains the "W3bstract.AspRuntimeAdapter" should not be shadow-copied by
  'asp to avoid issues with multiple instances of assemblies within one appdomain
  '(caused by multiple load contexts when dynamically loaded plugin-assemblies)...
  Private _ApplicationHandler As Object
  Private _AspRuntimeAdapter As Object
  Private _ProcessRequestMethod As MethodInfo

  Public Sub New()

    'required for assembly resolving
    _AppDom = AppDomain.CurrentDomain

    _ApplicationAssembly = Assembly.Load(ApplicationAssemblyName)
    Dim applicationHandlerType = _ApplicationAssembly.GetType(ApplicationHandlerTypeName)
    Dim entryPointConstrcutorArguments As Object() = Me.GetApplicationHandlerConstructorArgs()
    _ApplicationHandler = Activator.CreateInstance(applicationHandlerType, entryPointConstrcutorArguments)

    _RuntimeAssembly = Assembly.Load("W3bstract")
    Dim aspRuntimeAdapterType = _RuntimeAssembly.GetType("W3bstract.AspRuntimeAdapter")
    _ProcessRequestMethod = aspRuntimeAdapterType.GetMethod("ProcessRequest")
    _AspRuntimeAdapter = Activator.CreateInstance(aspRuntimeAdapterType, {_ApplicationHandler})

  End Sub

  Private Function AppDom_AssemblyResolve(sender As Object, args As ResolveEventArgs) As Assembly Handles _AppDom.AssemblyResolve
    Dim assName As New AssemblyName(args.Name)
    Dim targetPath As String

    targetPath = Path.Combine(My.Settings.AssemblyLookupDirectory, assName.Name + ".dll")
    If (File.Exists(targetPath)) Then
      Return Assembly.LoadFile(targetPath)
    End If

    targetPath = Path.Combine(My.Settings.AssemblyLookupDirectory, assName.Name + ".exe")
    If (File.Exists(targetPath)) Then
      Return Assembly.LoadFile(targetPath)
    End If

    Return Nothing
  End Function

  Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
    Get
      Return True
    End Get
  End Property

  Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
    Try
      _ProcessRequestMethod.Invoke(_AspRuntimeAdapter, {context})
    Catch ex As Exception
      'HACK: SOLLTE RAUS ODER NUR BEI LOCALHOST
      context.Response.Write(ex.Message)
    End Try
  End Sub

#End Region

  Private Const ApplicationAssemblyName As String =
    "W3bstract.DemoApp.BackendApi"

  Private Const ApplicationHandlerTypeName As String =
    "W3bstract.DemoApp.BackendApi.HttpServerEntryPoint"

  Private Function GetApplicationHandlerConstructorArgs() As Object()
    Return {
      My.Settings.BaseAddress,
      My.Settings.ApiServiceUrl
    }
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
    If (_AspRuntimeAdapter IsNot Nothing AndAlso TypeOf (_AspRuntimeAdapter) Is IDisposable) Then
      Try
        DirectCast(_AspRuntimeAdapter, IDisposable).Dispose()
      Catch
      End Try
    End If
    If (_ApplicationHandler IsNot Nothing AndAlso TypeOf (_ApplicationHandler) Is IDisposable) Then
      Try
        DirectCast(_ApplicationHandler, IDisposable).Dispose()
      Catch
      End Try
    End If
  End Sub

  <EditorBrowsable(EditorBrowsableState.Advanced)>
  Protected Sub DisposedGuard()
    If (_AlreadyDisposed) Then
      Throw New ObjectDisposedException(Me.GetType.Name)
    End If
  End Sub

#End Region

End Class
