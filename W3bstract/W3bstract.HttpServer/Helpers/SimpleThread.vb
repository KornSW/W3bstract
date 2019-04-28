Imports System

Friend Class SimpleThread

  Public Delegate Function AsyncOperation(params() As Object, ByRef cancelRequested As Boolean) As Object
  Public Delegate Sub AsyncResultHandler(result As Object)

  Private _AsyncOperation As AsyncOperation
  Private _AsyncResultHandler As AsyncResultHandler
  Private _CancelRequested As Boolean = False

  Private WithEvents _Bgw As New System.ComponentModel.BackgroundWorker

  Private Sub New(asyncOperation As AsyncOperation, asyncResultHandler As AsyncResultHandler, ParamArray params() As Object)
    _AsyncOperation = asyncOperation
    _AsyncResultHandler = asyncResultHandler
    _Bgw.RunWorkerAsync(params)
  End Sub

  Public Shared Function RunAsync(operation As AsyncOperation, ParamArray params() As Object) As SimpleThread
    Return New SimpleThread(operation, Nothing, params)
  End Function

  Public Shared Function RunAsync(operation As AsyncOperation, resultHandler As AsyncResultHandler, ParamArray params() As Object) As SimpleThread
    Return New SimpleThread(operation, resultHandler, params)
  End Function

  Public Sub Cancel()
    _CancelRequested = True
  End Sub

  Public Function Cancel(waitMs As Integer) As Boolean
    _CancelRequested = True
    Dim timeout As DateTime = DateTime.Now.AddMilliseconds(waitMs)

    While (DateTime.Now < timeout)
      Threading.Thread.Sleep(10)
      If (Not Me.IsBusy) Then
        Return True
      End If
    End While

    Return False
  End Function

  Public ReadOnly Property IsBusy As Boolean
    Get
      Return _Bgw.IsBusy
    End Get
  End Property

  Private Sub Bgw_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles _Bgw.DoWork
    e.Result = _AsyncOperation.Invoke(DirectCast(e.Argument, Object()), _CancelRequested)
  End Sub

  Private Sub Bgw_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles _Bgw.RunWorkerCompleted
    If (Not _CancelRequested AndAlso _AsyncResultHandler IsNot Nothing) Then
      _AsyncResultHandler.Invoke(e.Result)
    End If
  End Sub

End Class
