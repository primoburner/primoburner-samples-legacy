Namespace MultiDataDisc
	Partial Public Class WriteSpeedsForm
		''' <summary>
		''' Required designer variable.
		''' </summary>
		Private components As System.ComponentModel.IContainer = Nothing

		''' <summary>
		''' Clean up any resources being used.
		''' </summary>
		''' <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		Protected Overrides Sub Dispose(ByVal disposing As Boolean)
			If disposing AndAlso (components IsNot Nothing) Then
				components.Dispose()
			End If
			MyBase.Dispose(disposing)
		End Sub

		#Region "Windows Form Designer generated code"

		''' <summary>
		''' Required method for Designer support - do not modify
		''' the contents of this method with the code editor.
		''' </summary>
		Private Sub InitializeComponent()
			Me.buttonOK = New System.Windows.Forms.Button()
			Me.buttonCancel = New System.Windows.Forms.Button()
			Me.lblWriter = New System.Windows.Forms.Label()
			Me.label1 = New System.Windows.Forms.Label()
			Me.cbWriteSpeed = New System.Windows.Forms.ComboBox()
			Me.label2 = New System.Windows.Forms.Label()
			Me.SuspendLayout()
			' 
			' buttonOK
			' 
			Me.buttonOK.Location = New System.Drawing.Point(286, 108)
			Me.buttonOK.Name = "buttonOK"
			Me.buttonOK.Size = New System.Drawing.Size(75, 23)
			Me.buttonOK.TabIndex = 0
			Me.buttonOK.Text = "OK"
			Me.buttonOK.UseVisualStyleBackColor = True
'			Me.buttonOK.Click += New System.EventHandler(Me.buttonOK_Click)
			' 
			' buttonCancel
			' 
			Me.buttonCancel.Location = New System.Drawing.Point(376, 108)
			Me.buttonCancel.Name = "buttonCancel"
			Me.buttonCancel.Size = New System.Drawing.Size(75, 23)
			Me.buttonCancel.TabIndex = 1
			Me.buttonCancel.Text = "Cancel"
			Me.buttonCancel.UseVisualStyleBackColor = True
'			Me.buttonCancel.Click += New System.EventHandler(Me.buttonCancel_Click)
			' 
			' lblWriter
			' 
			Me.lblWriter.Location = New System.Drawing.Point(92, 20)
			Me.lblWriter.Name = "lblWriter"
			Me.lblWriter.Size = New System.Drawing.Size(359, 23)
			Me.lblWriter.TabIndex = 2
			Me.lblWriter.Text = "WRITER"
			' 
			' label1
			' 
			Me.label1.AutoSize = True
			Me.label1.Location = New System.Drawing.Point(12, 61)
			Me.label1.Name = "label1"
			Me.label1.Size = New System.Drawing.Size(69, 13)
			Me.label1.TabIndex = 3
			Me.label1.Text = "Write Speed:"
			' 
			' cbWriteSpeed
			' 
			Me.cbWriteSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.cbWriteSpeed.ItemHeight = 13
			Me.cbWriteSpeed.Location = New System.Drawing.Point(92, 58)
			Me.cbWriteSpeed.Name = "cbWriteSpeed"
			Me.cbWriteSpeed.Size = New System.Drawing.Size(107, 21)
			Me.cbWriteSpeed.TabIndex = 19
			' 
			' label2
			' 
			Me.label2.AutoSize = True
			Me.label2.Location = New System.Drawing.Point(12, 20)
			Me.label2.Name = "label2"
			Me.label2.Size = New System.Drawing.Size(44, 13)
			Me.label2.TabIndex = 20
			Me.label2.Text = "Device:"
			' 
			' WriteSpeedsForm
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(463, 144)
			Me.Controls.Add(Me.label2)
			Me.Controls.Add(Me.cbWriteSpeed)
			Me.Controls.Add(Me.label1)
			Me.Controls.Add(Me.lblWriter)
			Me.Controls.Add(Me.buttonCancel)
			Me.Controls.Add(Me.buttonOK)
			Me.MaximizeBox = False
			Me.MinimizeBox = False
			Me.Name = "WriteSpeedsForm"
			Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
			Me.Text = "Write Speeds"
			Me.ResumeLayout(False)
			Me.PerformLayout()

		End Sub

		#End Region

		Private WithEvents buttonOK As System.Windows.Forms.Button
		Private WithEvents buttonCancel As System.Windows.Forms.Button
		Private lblWriter As System.Windows.Forms.Label
		Private label1 As System.Windows.Forms.Label
		Private cbWriteSpeed As System.Windows.Forms.ComboBox
		Private label2 As System.Windows.Forms.Label
	End Class
End Namespace