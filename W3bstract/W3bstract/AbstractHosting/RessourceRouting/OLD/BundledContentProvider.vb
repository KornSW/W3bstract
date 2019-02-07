Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.CompilerServices

Namespace Web

  Public MustInherit Class BundledContentProvider
    Implements IWebContentProvider

    Private _BundleIndex As New Dictionary(Of String, Action(Of Stream))

    Public Sub New()
      Me.VisitBundle(
        Sub(bundle)
          Dim root = Me.RootDir.Replace("\", "/")
          If (Not root.EndsWith("/")) Then
            root = root & "/"
          End If
          If (root.StartsWith("/")) Then
            root = root.Substring(1, root.Length - 1)
          End If
          For Each entry In bundle.Entries
            Dim fullName As String = root & entry.FullName
            _BundleIndex.Add(fullName, Sub(target) Me.WriteDecompressedContent(entry.FullName, target))
          Next
        End Sub
      )
    End Sub

    Private Sub WriteDecompressedContent(entryName As String, target As Stream)
      Me.VisitBundle(
        Sub(bundle)
          Using stream = bundle.GetEntry(entryName).Open()
            stream.CopyTo(target)
          End Using
        End Sub)
    End Sub

    Private Sub VisitBundle(method As Action(Of ZipArchive))

      Throw New NotImplementedException("GetDefaultNamespace fehlt noch")

      'Dim ass = Me.GetType().Assembly
      'Dim resourceName As String = ass.GetDefaultNamespace() & "." & Me.EmbeddedZipFileName
      'Using stream = ass.GetManifestResourceStream(resourceName)
      '  If (stream Is Nothing) Then
      '    Throw New Exception(String.Format("Resource '{0}' was not found in '{1}'", resourceName, ass.GetName.Name))
      '  End If
      '  Using bundle = New ZipArchive(stream, ZipArchiveMode.Read)
      '    method.Invoke(bundle)
      '  End Using
      'End Using
    End Sub

    Protected MustOverride ReadOnly Property EmbeddedZipFileName As String

    Protected Overridable ReadOnly Property RootDir As String
      Get
        Return String.Empty
      End Get
    End Property

    Public Sub RespondContent(contentName As String, request As IWebRequest, response As IWebResponse, state As IWebSessionState) Implements IWebContentProvider.RespondContent
      _BundleIndex(contentName).Invoke(response.stream)
    End Sub

    Public Function GetContentNames() As String() Implements IWebContentProvider.GetContentNames
      Return _BundleIndex.Keys.ToArray()
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

  Public Class EmbeddedContentProvider
    Implements IWebContentProvider

    Private _Assembly As Assembly

    Public Sub New(assembly As Assembly)
      _Assembly = assembly
    End Sub

    Public Sub RespondContent(contentName As String, request As IWebRequest, response As IWebResponse, state As IWebSessionState) Implements IWebContentProvider.RespondContent

    End Sub

    Public Function GetContentNames() As String() Implements IWebContentProvider.GetContentNames
      Return {}
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

End Namespace
