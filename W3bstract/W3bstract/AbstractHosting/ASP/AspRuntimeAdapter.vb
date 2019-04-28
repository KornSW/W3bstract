Imports System
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Web
Imports System.Web.SessionState

Namespace AbstractHosting.ASP

  ''' <summary>
  ''' This class will be dynamically instanciated (via reflection+activator)
  ''' by the asp websites and services.
  ''' We have to do so because this assembly should not be
  ''' shadow-copied by asp to avoid issues with multiple instances of assemblies
  ''' within one appdomain (caused by multiple load contexts when dynamically loaded
  ''' plugin-assemblies) ...
  ''' </summary>
  Partial Public Class AspRuntimeAdapter
    Implements IWebRuntimeHost

    Private _Handler As IWebRequestHandler

    Public Sub New(handler As IWebRequestHandler)
      _Handler = handler
    End Sub

    Public ReadOnly Property Handler As IWebRequestHandler
      Get
        Return _Handler
      End Get
    End Property

    Public ReadOnly Property ServerRuntime As String Implements IWebRuntimeHost.ServerRuntime
      Get
        Return "ASP.NET"
      End Get
    End Property

    Public Sub ProcessRequest(ByVal context As HttpContext)

      Using ses As New AspSessionState(context.Server, context.Session, context.Application, context),
          req As New AspRequest(context.Request, ses),
          res As New AspResponse(context.Response, ses)

#If DEBUG Then
      Dim start As DateTime = DateTime.Now
#End If

        _Handler.ProcessRequest(req, res, ses)

#If DEBUG Then
      Dim ms = DateTime.Now.Subtract(start).TotalMilliseconds
      If (ms > 200) Then
        Debug.WriteLine($"WARNING SLOW REQUEST ({ms}ms): '{req.Url}'")
      End If
#End If

        If (ses.RequestSessionReset) Then
          context.Session.Abandon()
        End If

      End Using

    End Sub

  End Class

End Namespace
