Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection

Public Class WebRequestRouter
  Implements IWebRequestHandler

  'TODO: ERROR PAGES
  'TODO: REDIRECTORS

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _BaseAddress As String

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _DefaultResourceIsBoundToUnregisteredSuburls As Boolean

  Public ReadOnly Property BaseAddress As String
    Get
      Return _BaseAddress
    End Get
  End Property

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _DefaultResource As String = Nothing

  Public Overridable ReadOnly Property DefaultResource As String
    Get
      Return _DefaultResource
    End Get
  End Property

  Public Sub SetDefaultResource(name As String)
    _DefaultResource = name
  End Sub

  Public Sub SetDefaultResource(name As String, alsoForSuburls As Boolean)
    _DefaultResource = name
    If (alsoForSuburls) Then
      Me.BindDefaultResourceForUnregisteredSuburls()
    End If
  End Sub

  Public Sub BindDefaultResourceForUnregisteredSuburls()
    _DefaultResourceIsBoundToUnregisteredSuburls = True
  End Sub

  Public Sub New(Optional baseAddress As String = "/")
    _BaseAddress = baseAddress
    If (Not _BaseAddress.EndsWith("/")) Then
      _BaseAddress = _BaseAddress + "/"
    End If
    If (Not _BaseAddress.StartsWith("/")) Then
      _BaseAddress = "/" + _BaseAddress
    End If
  End Sub

  Private _Childs As New Dictionary(Of String, IWebRequestHandler)
  Private _Extenders As New List(Of WebRequestRouter)
  Private _RequestHooks As New Dictionary(Of String, IRawRequestHook())

  Public ReadOnly Property ChildNames As String()
    Get
      Return _Childs.Keys.ToArray()
    End Get
  End Property

  Public Function GetChildHandler(childName As String) As IWebRequestHandler
    Return _Childs(childName)
  End Function

  Public Property ErrorPageHandler As IWebRequestHandler = Nothing  'default is HTTP 404

#Region " Configuration "

  Protected Shared Function GetMimeTypeFromFileExtension(ext As String) As String
    Select Case (ext.ToLower())

      Case ".ico" : Return "image/ico"
      Case ".png" : Return "image/png"
      Case ".gif" : Return "image/gif"
      Case ".jpg", ".jpeg" : Return "image/jpeg"

      Case ".xml" : Return "application/xml"
      Case ".xaml" : Return "application/xml"
      Case ".json" : Return "application/json"

      Case ".txt", ".log" : Return "text/plain"
      Case ".css" : Return "text/css"
      Case ".htm", ".html" : Return "text/html"

      Case ".pdf" : Return "application/pdf"
      Case Else : Return "application/octet-stream "

    End Select
  End Function

  Public Sub RegisterFile(fileFullName As String, Optional subUrl As String = "/", Optional mimeType As String = Nothing, Optional setAsDefault As Boolean = False, Optional keepInMemory As Boolean = False)
    If (mimeType Is Nothing) Then
      mimeType = GetMimeTypeFromFileExtension(IO.Path.GetExtension(fileFullName))
    End If
    If (String.IsNullOrWhiteSpace(subUrl)) Then
      subUrl = "/"
    End If
    If (subUrl.EndsWith("/")) Then
      subUrl = subUrl + IO.Path.GetFileName(fileFullName)
    End If
    Dim handler As New StreamResourceHandler(Function() New IO.FileStream(fileFullName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read), mimeType, keepInMemory)
    Me.RegisterDynamicTarget(handler, subUrl, setAsDefault)
  End Sub

  Private _FileSystemWatchers As New Dictionary(Of String, FileSystemWatcher)

  Public Sub RegisterFilesFromDirectory(directoryName As String, Optional fileSearchPattern As String = "*.*", Optional subUrl As String = "/", Optional mimeType As String = Nothing, Optional defaultName As String = Nothing, Optional enableAutoReload As Boolean = False)
    If (String.IsNullOrWhiteSpace(subUrl)) Then
      subUrl = "/"
    End If
    If (Not subUrl.EndsWith("/")) Then
      Throw New Exception("The subUrl must end with '/'")
    End If

    Dim di As New DirectoryInfo(directoryName)

    Dim addFileMethod = (
      Sub(fileFullName As String)
        Dim name = IO.Path.GetFileName(fileFullName)
        Dim asDefault = (defaultName IsNot Nothing AndAlso name = defaultName)
        Me.RegisterFile(fileFullName, subUrl + name, mimeType, asDefault)
      End Sub
    )

    For Each file In di.GetFiles(fileSearchPattern)
      addFileMethod.Invoke(file.FullName)
    Next

    Dim watcherName = directoryName + "@" + fileSearchPattern
    If (enableAutoReload AndAlso Not _FileSystemWatchers.ContainsKey(watcherName)) Then

      Dim w As New FileSystemWatcher(directoryName)
      w.Filter = fileSearchPattern
      w.NotifyFilter = NotifyFilters.CreationTime Or NotifyFilters.LastWrite Or NotifyFilters.FileName
      w.IncludeSubdirectories = False
      _FileSystemWatchers.Add(watcherName, w)

      AddHandler w.Created, (
        Sub(sender As Object, e As FileSystemEventArgs)
          addFileMethod.Invoke(e.FullPath)
        End Sub
       )

      AddHandler w.Deleted, (
        Sub(sender As Object, e As FileSystemEventArgs)
          Me.UnRegisterDynamicTarget(subUrl + e.Name)
        End Sub
       )

      AddHandler w.Renamed, (
        Sub(sender As Object, e As RenamedEventArgs)
          Me.UnRegisterDynamicTarget(subUrl + e.OldName)
          addFileMethod.Invoke(e.FullPath)
        End Sub
       )

      w.EnableRaisingEvents = True

    End If

  End Sub

  Public Sub RegisterDirectoryTree(directoryName As String, Optional fileSearchPattern As String = "*.*", Optional subUrl As String = "/", Optional mimeType As String = Nothing, Optional enableAutoReload As Boolean = False)
    If (String.IsNullOrWhiteSpace(subUrl)) Then
      subUrl = "/"
    End If
    If (Not subUrl.EndsWith("/")) Then
      Throw New Exception("The subUrl must end with '/'")
    End If
    Me.RegisterFilesFromDirectory(directoryName, fileSearchPattern, subUrl, mimeType,, enableAutoReload)

    Dim di As New DirectoryInfo(directoryName)
    For Each subDir In di.GetDirectories()
      Me.RegisterDirectoryTree(subDir.FullName, fileSearchPattern, subUrl + subDir.Name + "/", mimeType, enableAutoReload)
    Next
  End Sub

  Public Sub RegisterEmbeddedFile(assembly As Assembly, fullResourceName As String, Optional subUrl As String = "/", Optional mimeType As String = Nothing, Optional setAsDefault As Boolean = False, Optional keepInMemory As Boolean = False)
    Dim defaultNamespace As String = assembly.GetDefaultNamespace()
    If (mimeType Is Nothing) Then
      mimeType = GetMimeTypeFromFileExtension(IO.Path.GetExtension(fullResourceName))
    End If
    If (String.IsNullOrWhiteSpace(subUrl)) Then
      subUrl = "/"
    End If
    If (subUrl.EndsWith("/")) Then
      subUrl = subUrl + fullResourceName.Substring(defaultNamespace.Length + 1)
    End If
    Dim handler As New StreamResourceHandler(Function() assembly.GetManifestResourceStream(fullResourceName), mimeType, keepInMemory)
    Me.RegisterDynamicTarget(handler, subUrl, setAsDefault)
  End Sub

  Public Sub RegisterEmbeddedFiles(assembly As Assembly, Optional resourceNameSearchPattern As String = "*.*", Optional subUrl As String = "/", Optional mimeType As String = Nothing, Optional keepInMemory As Boolean = False)
    If (String.IsNullOrWhiteSpace(subUrl)) Then
      subUrl = "/"
    End If
    If (Not subUrl.EndsWith("/")) Then
      Throw New Exception("The subUrl must end with '/'")
    End If
    For Each resourceName In assembly.GetManifestResourceNames()
      If (Not resourceName.EndsWith(".resources") AndAlso resourceName.MatchesWildcardMask(resourceNameSearchPattern)) Then
        Me.RegisterEmbeddedFile(assembly, resourceName, subUrl,,, keepInMemory)
      End If
    Next
  End Sub

  Public Sub RegisterDynamicTarget(handler As IWebRequestHandler, subUrl As String, ParamArray requestHooks() As IRawRequestHook)
    Me.RegisterDynamicTarget(handler, subUrl, False, requestHooks)
  End Sub

  Public Sub UnRegisterDynamicTarget(subUrl As String)
    Me.RegisterDynamicTarget(Nothing, subUrl)
  End Sub

  ''' <summary>
  ''' Extends the current level with another Router (Join)
  ''' </summary>
  Public Sub RegisterDynamicTarget(router As WebRequestRouter)
    _Extenders.Add(router)
  End Sub

  Public Sub RegisterDynamicTarget(handler As IWebRequestHandler, subUrl As String, setAsDefault As Boolean, ParamArray requestHooks() As IRawRequestHook)

    If (String.IsNullOrWhiteSpace(subUrl)) Then
      Throw New Exception("The subUrl cannot be empty")
    End If

    If (subUrl.EndsWith("/")) Then
      Throw New Exception("The subUrl must not end with '/'")
    End If
    If (subUrl.StartsWith("/")) Then
      subUrl = subUrl.Substring(1)
    End If

    If (subUrl.Contains("/")) Then
      Dim directSubdirName As String
      directSubdirName = subUrl.Substring(0, subUrl.IndexOf("/"))
      subUrl = subUrl.Substring(directSubdirName.Length)

      Dim subRouter As WebRequestRouter
      If (_Childs.ContainsKey(directSubdirName)) Then
        Dim child As IWebRequestHandler = _Childs(directSubdirName)
        If (Not TypeOf (child) Is WebRequestRouter) Then
          Throw New Exception($"The route '{_BaseAddress}{directSubdirName}' is alredy registered!")
        End If
        subRouter = DirectCast(child, WebRequestRouter)
      Else

        If (handler Is Nothing) Then
          'beim remove muss kein neuer unter-router erstellt werden
          Exit Sub
        Else
          subRouter = New WebRequestRouter(_BaseAddress + directSubdirName + "/")
          _Childs.Add(directSubdirName, subRouter)
        End If

      End If

      subRouter.RegisterDynamicTarget(handler, subUrl, setAsDefault)
      Exit Sub
    End If

    If (_Childs.ContainsKey(subUrl)) Then

      'beim remove nicht knallen
      If (handler IsNot Nothing) Then
        Throw New Exception($"The route '{_BaseAddress}{subUrl}' is alredy registered!")
      End If

    Else

      'WIR SIND ZUSTÄNDIG!!!

      If (handler Is Nothing) Then

        'UNREGISTER

        If (_Childs.ContainsKey(subUrl)) Then
          Dim oldHandler = _Childs(subUrl)
          _Childs.Remove(subUrl)
          oldHandler.TryDispose()
        End If

        If (_RequestHooks.ContainsKey(subUrl)) Then
          _RequestHooks.Remove(subUrl)
        End If

      Else
        'REGISTER

        _Childs.Add(subUrl, handler)
        If (requestHooks.Length > 0) Then
          _RequestHooks.Add(subUrl, requestHooks)
        End If

      End If

    End If

  End Sub

#End Region

#Region " Operation "

  Public Sub ProcessRequest(request As IWebRequest, response As IWebResponse, state As IWebSessionState) Implements IWebRequestHandler.ProcessRequest
    response.StatusCode = 200 'OK

    Dim absolutePath As String = request.Url.AbsolutePath
    If (Not absolutePath.StartsWith("/")) Then
      absolutePath = "/" + absolutePath
    End If

    If (Not absolutePath.StartsWith(_BaseAddress, StringComparison.CurrentCultureIgnoreCase)) Then
      If ((absolutePath + "/").Equals(_BaseAddress, StringComparison.CurrentCultureIgnoreCase)) Then
        absolutePath = absolutePath + "/"
      Else
        response.StatusCode = 404
        Exit Sub 'must be a wire-up issue
      End If
    End If

    Dim urlPartToHandle = absolutePath.Substring(_BaseAddress.Length)
    Dim childFragment As String

    If (urlPartToHandle.Contains("/")) Then
      childFragment = urlPartToHandle.Substring(0, urlPartToHandle.IndexOf("/"))
    Else
      childFragment = urlPartToHandle
    End If

    If (childFragment = "" AndAlso Me.DefaultResource IsNot Nothing) Then
      childFragment = Me.DefaultResource
    End If

    Dim target As IWebRequestHandler = Nothing
    target = Me.FindTarget(childFragment)

    If (target IsNot Nothing) Then
      target.ProcessRequest(request, response, state)
    ElseIf (_DefaultResourceIsBoundToUnregisteredSuburls AndAlso Me.DefaultResource IsNot Nothing) Then
      target = Me.FindTarget(Me.DefaultResource)
      target.ProcessRequest(request, response, state)

    ElseIf (Me.ErrorPageHandler IsNot Nothing) Then
      Me.ErrorPageHandler.ProcessRequest(request, response, state)
    Else
      response.StatusCode = 404 'NOT FOUND
    End If

  End Sub

  Protected Function FindTarget(childFragment As String) As IWebRequestHandler

    SyncLock _Childs
      If (_Childs.ContainsKey(childFragment)) Then
        Return _Childs(childFragment)
      End If
    End SyncLock

    SyncLock _Extenders
      For Each ext In _Extenders
        Dim target As IWebRequestHandler
        target = ext.FindTarget(childFragment)
        If (target IsNot Nothing) Then
          Return target
        End If
      Next
    End SyncLock

    Return Nothing
  End Function

#End Region

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
