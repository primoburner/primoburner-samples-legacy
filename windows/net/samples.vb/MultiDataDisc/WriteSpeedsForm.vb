Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Namespace MultiDataDisc
	Partial Public Class WriteSpeedsForm
		Inherits Form

		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub buttonOK_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonOK.Click
			_selectedWriteSpeed = TryCast(cbWriteSpeed.SelectedItem, SpeedInfo)
			DialogResult = System.Windows.Forms.DialogResult.OK
		End Sub

		Private Sub buttonCancel_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonCancel.Click
			DialogResult = System.Windows.Forms.DialogResult.Cancel
		End Sub

		Public Property WriterTitle() As String
			Get
				Return lblWriter.Text
			End Get

			Set(ByVal value As String)
				lblWriter.Text = value
			End Set
		End Property

		Private _writeSpeeds As List(Of SpeedInfo)
		Public Property WriteSpeeds() As List(Of SpeedInfo)
			Get
				Return _writeSpeeds
			End Get
			Set(ByVal value As List(Of SpeedInfo))
				_writeSpeeds = value
				UpdateWriteSpeeds()
			End Set
		End Property

		Private _selectedWriteSpeed As SpeedInfo
		Public Property SelectedWriteSpeed() As SpeedInfo
			Get
				Return _selectedWriteSpeed
			End Get
			Set(ByVal value As SpeedInfo)
				_selectedWriteSpeed = value
				UpdateWriteSpeeds()
			End Set
		End Property

		Private Sub UpdateWriteSpeeds()
			cbWriteSpeed.Items.Clear()

			If _writeSpeeds IsNot Nothing Then
				For i As Integer = 0 To _writeSpeeds.Count - 1
					cbWriteSpeed.Items.Add(_writeSpeeds(i))
				Next i
			End If

			If _selectedWriteSpeed IsNot Nothing Then
				If cbWriteSpeed.Items.Count > 0 Then
					cbWriteSpeed.SelectedIndex = cbWriteSpeed.FindString(_selectedWriteSpeed.ToString())

					If -1 = cbWriteSpeed.SelectedIndex Then
						cbWriteSpeed.SelectedIndex = 0
					End If
				End If
			End If
		End Sub
	End Class
End Namespace
