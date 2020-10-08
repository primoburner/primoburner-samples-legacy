Imports System
Imports System.Drawing
Imports System.Collections
Imports System.ComponentModel
Imports System.Windows.Forms
Imports PrimoSoftware.Burner


Namespace MultiDataDisc
	Public Class ProgressForm
		Inherits System.Windows.Forms.Form

		Public Sub New()
			InitializeComponent()
		End Sub

		Protected Overrides Sub Dispose(ByVal disposing As Boolean)
			If disposing Then
				If components IsNot Nothing Then
					components.Dispose()
				End If
			End If

			MyBase.Dispose(disposing)
		End Sub

		Public ReadOnly Property Stopped() As Boolean
			Get
				Return m_stopped
			End Get
		End Property

		Private Sub UIThread(ByVal code As MethodInvoker)
			If InvokeRequired Then
				Invoke(code)
				Return
			End If

			code.Invoke()
		End Sub

		Public Sub UpdateProgress(ByVal info As ProgressInfo, ByVal rowIndex As Integer)
			Dim del As MethodInvoker = Function() AnonymousMethod1(rowIndex, info)

			UIThread(del)
		End Sub
		
		Private Function AnonymousMethod1(ByVal rowIndex As Integer, ByVal info As MultiDataDisc.ProgressInfo) As Object
			Do While lvDevices.Items.Count <= rowIndex
				Dim lviNewEntry As New ListViewItem()
				lviNewEntry.SubItems.Add(String.Empty)
				lviNewEntry.SubItems.Add(String.Empty)
				lviNewEntry.SubItems.Add(String.Empty)
				lvDevices.Items.Add(lviNewEntry)
			Loop
			Dim lvi As ListViewItem = lvDevices.Items(rowIndex)
			lvi.SubItems(0).Text = info.DeviceTitle
			lvi.SubItems(1).Text = info.Status
			lvi.SubItems(2).Text = info.ProgressStr
			lvi.SubItems(3).Text = info.WriteSpeed
			Return Nothing
		End Function

		Private Sub buttonStop_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonStop.Click
			buttonStop.Enabled = False
			m_stopped = True
		End Sub

		Private m_stopped As Boolean = False

		#Region "Windows Form Designer generated code"
		Private WithEvents buttonStop As System.Windows.Forms.Button
		Private lvDevices As ListView
		Private columnHeaderDevice As ColumnHeader
		Private columnHeaderStatus As ColumnHeader
		Private columnHeaderProgress As ColumnHeader
		Private columnHeaderWriteSpeed As ColumnHeader

		''' <summary>
		''' Required designer variable.
		''' </summary>
		Private components As System.ComponentModel.Container = Nothing

		''' <summary>
		''' Required method for Designer support - do not modify
		''' the contents of this method with the code editor.
		''' </summary>
		Private Sub InitializeComponent()
			Me.buttonStop = New System.Windows.Forms.Button()
			Me.lvDevices = New System.Windows.Forms.ListView()
			Me.columnHeaderDevice = (CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader))
			Me.columnHeaderStatus = (CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader))
			Me.columnHeaderProgress = (CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader))
			Me.columnHeaderWriteSpeed = (CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader))
			Me.SuspendLayout()
			' 
			' buttonStop
			' 
			Me.buttonStop.Location = New System.Drawing.Point(687, 169)
			Me.buttonStop.Name = "buttonStop"
			Me.buttonStop.Size = New System.Drawing.Size(71, 20)
			Me.buttonStop.TabIndex = 1
			Me.buttonStop.Text = "Stop"
'			Me.buttonStop.Click += New System.EventHandler(Me.buttonStop_Click)
			' 
			' lvDevices
			' 
			Me.lvDevices.Columns.AddRange(New System.Windows.Forms.ColumnHeader() { Me.columnHeaderDevice, Me.columnHeaderStatus, Me.columnHeaderProgress, Me.columnHeaderWriteSpeed})
			Me.lvDevices.FullRowSelect = True
			Me.lvDevices.GridLines = True
			Me.lvDevices.Location = New System.Drawing.Point(7, 5)
			Me.lvDevices.Name = "lvDevices"
			Me.lvDevices.Size = New System.Drawing.Size(757, 148)
			Me.lvDevices.TabIndex = 2
			Me.lvDevices.UseCompatibleStateImageBehavior = False
			Me.lvDevices.View = System.Windows.Forms.View.Details
			' 
			' columnHeaderDevice
			' 
			Me.columnHeaderDevice.Text = "Device"
			Me.columnHeaderDevice.Width = 363
			' 
			' columnHeaderStatus
			' 
			Me.columnHeaderStatus.Text = "Status"
			Me.columnHeaderStatus.Width = 218
			' 
			' columnHeaderProgress
			' 
			Me.columnHeaderProgress.Text = "Progress"
			Me.columnHeaderProgress.Width = 77
			' 
			' columnHeaderWriteSpeed
			' 
			Me.columnHeaderWriteSpeed.Text = "Write Speed"
			Me.columnHeaderWriteSpeed.Width = 88
			' 
			' ProgressForm
			' 
			Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
			Me.ClientSize = New System.Drawing.Size(770, 203)
			Me.ControlBox = False
			Me.Controls.Add(Me.lvDevices)
			Me.Controls.Add(Me.buttonStop)
			Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
			Me.MaximizeBox = False
			Me.MinimizeBox = False
			Me.Name = "ProgressForm"
			Me.ShowInTaskbar = False
			Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
			Me.Text = "Working ..."
			Me.ResumeLayout(False)

		End Sub
		#End Region
	End Class
End Namespace
