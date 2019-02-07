Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports Newtonsoft.Json

Public Class BrowserWindowObjectStrapper
  Implements IWebRequestHandler

  Public Sub New(objectName As String, objectToStrap As Object)
    Me.ObjectName = objectName
    Me.ObjectToStrap = objectToStrap
  End Sub

  Public ReadOnly Property ObjectName As String
  Public ReadOnly Property ObjectToStrap As Object = Nothing

  Public Sub ProcessRequest(request As IWebRequest, response As IWebResponse, state As IWebSessionState) Implements IWebRequestHandler.ProcessRequest
    If (request.HttpMethod.Equals("options", StringComparison.CurrentCultureIgnoreCase)) Then
      response.StatusCode = 200 'OK
      Exit Sub
    End If
    If (Me.ObjectToStrap IsNot Nothing) Then
      Dim serializedObject As String = JsonConvert.SerializeObject(Me.ObjectToStrap)
      response.ContentWriter.Write("window." + Me.ObjectName + " = " + serializedObject)
    End If
  End Sub

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
