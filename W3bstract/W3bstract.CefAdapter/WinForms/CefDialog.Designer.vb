
Namespace CefAdapter.WinForms

  <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
  Partial Class CefDialog
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
      Try
        If disposing AndAlso components IsNot Nothing Then
          components.Dispose()
        End If
      Finally
        MyBase.Dispose(disposing)
      End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
      Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(CefDialog))
      Me.CefControl = New W3bstract.CefAdapter.WinForms.CefControl()
      Me.SuspendLayout()
      '
      'CefControl
      '
      Me.CefControl.BackColor = System.Drawing.Color.White
      Me.CefControl.BrowserConsoleErrorLoggingMethod = Nothing
      Me.CefControl.BrowserConsoleInfoLoggingMethod = Nothing
      Me.CefControl.BrowserConsoleWarningLoggingMethod = Nothing
      Me.CefControl.BrowserDevToolsVisible = False
      Me.CefControl.Dock = System.Windows.Forms.DockStyle.Fill
      Me.CefControl.Location = New System.Drawing.Point(0, 0)
      Me.CefControl.Name = "CefControl"
      Me.CefControl.Size = New System.Drawing.Size(880, 638)
      Me.CefControl.TabIndex = 0
      '
      'CefDialog
      '
      Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
      Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
      Me.ClientSize = New System.Drawing.Size(880, 638)
      Me.Controls.Add(Me.CefControl)
      Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
      Me.Name = "CefDialog"
      Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
      Me.Text = "Application"
      Me.ResumeLayout(False)

    End Sub

    Friend WithEvents CefControl As CefControl
  End Class

End Namespace
