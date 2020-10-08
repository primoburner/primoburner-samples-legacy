Imports System
Imports System.IO
Imports System.Data
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Threading
Imports System.Diagnostics
Imports System.Windows.Forms

Imports PrimoSoftware.Burner

Namespace MultiDataDisc
	Public Class BurnerForm
		Inherits System.Windows.Forms.Form

		Private m_engine As Engine
		Private m_devicesInfo As New List(Of DeviceInfo)()

		Private m_workers As List(Of WorkerThreadContext)

		Private Const WM_DEVICECHANGE As Integer = &H219
		Private m_RequiredSpace As Long = 0
		Private m_mainWorkerThread As System.Threading.Thread

		Private m_progressWindow As ProgressForm

		Private Function IsBurning() As Boolean
			Return (Nothing IsNot m_mainWorkerThread)
		End Function

		Public Sub New()
			InitializeComponent()

			m_RequiredSpace = 0

			' Write Method
			comboBoxRecordingMode.Items.Add(New ListItem(WriteMethod.DvdDao, "Disc-At-Once"))
			comboBoxRecordingMode.Items.Add(New ListItem(WriteMethod.DvdIncremental, "Incremental"))
			comboBoxRecordingMode.SelectedIndex = 1

			' Image Types
			comboBoxImageType.Items.Add(New ListItem(ImageType.Iso9660, "ISO9660"))
			comboBoxImageType.Items.Add(New ListItem(ImageType.Joliet, "Joliet"))
			comboBoxImageType.Items.Add(New ListItem(ImageType.Udf, "UDF"))
			comboBoxImageType.Items.Add(New ListItem(ImageType.UdfIso, "UDF & ISO9660"))
			comboBoxImageType.Items.Add(New ListItem(ImageType.UdfJoliet, "UDF & Joliet"))
			comboBoxImageType.SelectedIndex = 2

			' Write parameters
			checkBoxSimulate.Checked = False
			checkBoxEjectWhenDone.Checked = False
			checkBoxCloseDisc.Checked = False
		End Sub

		Private Sub BurnerForm_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			m_engine = New Engine()

			If Not m_engine.Initialize() Then
				ShowError(m_engine.Error, "Engine.Initialize() failed.")
				Me.Close()
				Return
			End If

			UpdateRequiredSpace()
			UpdateDevicesInformation()
			UpdateUI()
		End Sub

		Protected Overrides Sub Dispose(ByVal disposing As Boolean)
			If disposing Then
				If components IsNot Nothing Then
					components.Dispose()
				End If


				If Nothing IsNot m_engine Then
					m_engine.Shutdown()
					m_engine.Dispose()
				End If

				m_engine = Nothing
			End If

			MyBase.Dispose(disposing)
		End Sub

		Protected Overrides Sub WndProc(ByRef msg As Message)
			If WM_DEVICECHANGE = msg.Msg Then
				If Not IsBurning() Then
					'do not update device info while burning
					UpdateDevicesInformation()
				End If
			End If

			MyBase.WndProc(msg)
		End Sub

		Private Sub lvDevices_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles lvDevices.SelectedIndexChanged
			UpdateUI()
		End Sub

		Private Sub UpdateUI()
			Dim deviceSelected As Boolean = lvDevices.SelectedItems.Count > 0
			buttonChangeWriteSpeed.Enabled = deviceSelected
			buttonCloseTray.Enabled = deviceSelected
			buttonEject.Enabled = deviceSelected
		End Sub

		Private Sub UpdateDevicesInformation()
			Dim selectedWriteSpeeds As New Dictionary(Of Integer, SpeedInfo)()
			For Each di As DeviceInfo In m_devicesInfo
				selectedWriteSpeeds(AscW(di.DriveLetter)) = di.SelectedWriteSpeed
			Next di

			m_devicesInfo.Clear()

			Using enumerator As DeviceEnumerator = m_engine.CreateDeviceEnumerator()
				For i As Integer = 0 To enumerator.Count - 1
					Dim device As Device = enumerator.CreateDevice(i, False)
					If Nothing IsNot device Then
						If IsWriterDevice(device) Then
							Dim dev As New DeviceInfo()
							dev.Index = i
							dev.DriveLetter = device.DriveLetter
							dev.Title = String.Format("({0}:) - {1}", device.DriveLetter, device.Description)
							dev.MediaFreeSpace = device.MediaFreeSpace * CLng(BlockSize.Dvd)
							dev.MediaProfile = device.MediaProfile
							dev.MediaProfileString = GetMediaProfileString(device)

							Dim writeSpeeds As List(Of SpeedInfo) = GetWriteSpeeds(device)

							If writeSpeeds.Count > 0 Then
								dev.MaxWriteSpeed = writeSpeeds(0)
							End If

							Dim selectedSpeed As SpeedInfo = Nothing

							If selectedWriteSpeeds.ContainsKey(AscW(dev.DriveLetter)) Then
								selectedSpeed = selectedWriteSpeeds(AscW(dev.DriveLetter))
							End If

							If (Nothing IsNot selectedSpeed) AndAlso ContainsSpeed(writeSpeeds, selectedSpeed) Then
								dev.SelectedWriteSpeed = selectedSpeed
							End If

							m_devicesInfo.Add(dev)
						End If

						device.Dispose()
					End If
				Next i
			End Using

			UpdateDevicesView()
		End Sub

		Private Sub UpdateDevicesView()
			If lvDevices.Items.Count <> m_devicesInfo.Count Then
				lvDevices.Items.Clear()
				For i As Integer = 0 To m_devicesInfo.Count - 1
					Dim lvi As New ListViewItem()
					lvi.SubItems.Add(String.Empty)
					lvi.SubItems.Add(String.Empty)
					lvi.SubItems.Add(String.Empty)
					lvDevices.Items.Add(lvi)
				Next i
			End If

			For i As Integer = 0 To m_devicesInfo.Count - 1
				Dim devInfo As DeviceInfo = m_devicesInfo(i)
				Dim liv As ListViewItem = lvDevices.Items(i)
				liv.Tag = devInfo
				liv.SubItems(0).Text = devInfo.Title
				liv.SubItems(1).Text = devInfo.MediaProfileString
				liv.SubItems(2).Text = String.Format("{0}GB", (CDbl(devInfo.MediaFreeSpace) / (1e9)).ToString("0.00"))

				Dim writeSpeed As String = String.Empty
				If devInfo.SelectedWriteSpeed IsNot Nothing Then
					writeSpeed = devInfo.SelectedWriteSpeed.ToString()
				ElseIf devInfo.MaxWriteSpeed IsNot Nothing Then
					writeSpeed = devInfo.MaxWriteSpeed.ToString() & " (default to Max)"
				End If

				liv.SubItems(3).Text = writeSpeed
			Next i
		End Sub

		Private Sub UpdateRequiredSpace()
			labelRequiredSpace.Text = String.Format("Required space : {0}GB", (CDbl(m_RequiredSpace) / (1e9)).ToString("0.00"))
		End Sub

		Private Sub BurnerForm_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
			If IsBurning() Then
				e.Cancel = True
				ShowError("Burning is in progress. The program cannot be closed.")
				Return
			End If
		End Sub

		Private Sub buttonExit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonExit.Click
			Close()
		End Sub

		Private Sub comboBoxRecordingMode_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles comboBoxRecordingMode.SelectedIndexChanged
			SelectedRecordingModeChanged()
		End Sub

		Private Sub SelectedRecordingModeChanged()
			If -1 = comboBoxRecordingMode.SelectedIndex Then
				Return
			End If

			Dim item As ListItem = CType(comboBoxRecordingMode.SelectedItem, ListItem)
			Dim writeMethod As PrimoSoftware.Burner.WriteMethod = CType(item.Value, PrimoSoftware.Burner.WriteMethod)

			If PrimoSoftware.Burner.WriteMethod.DvdDao = writeMethod Then
				checkBoxCloseDisc.Checked = True
				checkBoxCloseDisc.Enabled = False
			ElseIf PrimoSoftware.Burner.WriteMethod.DvdIncremental = writeMethod Then
				checkBoxCloseDisc.Enabled = True
			End If
		End Sub

		Private Sub buttonEject_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonEject.Click
			If lvDevices.SelectedIndices.Count = 0 Then
				Return
			End If

			Dim deviceIndex As Integer = m_devicesInfo(lvDevices.SelectedIndices(0)).Index

			Using devEnum As DeviceEnumerator = m_engine.CreateDeviceEnumerator()
				Using dev As Device = devEnum.CreateDevice(deviceIndex, False)
					If dev IsNot Nothing Then
						If Not dev.Eject(True) Then
							ShowError(dev.Error, "Failed to eject device tray.")
						End If
					Else
						ShowError(devEnum.Error, "Failed to create device.")
					End If
				End Using
			End Using
		End Sub

		Private Sub buttonCloseTray_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonCloseTray.Click
			If lvDevices.SelectedIndices.Count = 0 Then
				Return
			End If

			Dim deviceIndex As Integer = m_devicesInfo(lvDevices.SelectedIndices(0)).Index

			Using devEnum As DeviceEnumerator = m_engine.CreateDeviceEnumerator()
				Using dev As Device = devEnum.CreateDevice(deviceIndex, False)
					If dev IsNot Nothing Then
						If Not dev.Eject(False) Then
							ShowError(dev.Error, "Failed to close device tray.")
						End If
					Else
						ShowError(devEnum.Error, "Failed to create device.")
					End If
				End Using
			End Using
		End Sub

		Private Sub buttonChangeWriteSpeed_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonChangeWriteSpeed.Click
			If lvDevices.SelectedIndices.Count = 0 Then
				Return
			End If

			Dim di As DeviceInfo = m_devicesInfo(lvDevices.SelectedIndices(0))

			Using devEnum As DeviceEnumerator = m_engine.CreateDeviceEnumerator()
				Using dev As Device = devEnum.CreateDevice(di.Index, False)
					If dev IsNot Nothing Then
						Using dlg As New WriteSpeedsForm()
							dlg.WriterTitle = di.Title
							dlg.WriteSpeeds = GetWriteSpeeds(dev)
							dlg.SelectedWriteSpeed = di.SelectedWriteSpeed
							If dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
								di.SelectedWriteSpeed = dlg.SelectedWriteSpeed
								UpdateDevicesView()
							End If
						End Using
					Else
						ShowError(devEnum.Error, "Failed to create device.")
					End If
				End Using
			End Using
		End Sub

		Private Sub buttonBrowse_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonBrowse.Click
			Dim selectedPath As String

			Using folderBrowserDialog As New FolderBrowserDialog()
				If System.Windows.Forms.DialogResult.OK <> folderBrowserDialog.ShowDialog() Then
					Return
				End If

				selectedPath = folderBrowserDialog.SelectedPath
			End Using

			Dim item As ListItem = CType(comboBoxImageType.SelectedItem, ListItem)
			Dim imageType As PrimoSoftware.Burner.ImageType = CType(item.Value, PrimoSoftware.Burner.ImageType)

			Using dataDisc As New DataDisc()
				dataDisc.ImageType = imageType

				If Not dataDisc.SetImageLayoutFromFolder(selectedPath) Then
					ShowError(dataDisc.Error, "Failed to SetImageLayoutFromFolder.")
					Return
				End If

				m_RequiredSpace = dataDisc.ImageSizeInBytes
			End Using

			textBoxRootDir.Text = selectedPath
			UpdateRequiredSpace()
		End Sub

		Private Sub buttonBurn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles buttonBurn.Click
			If Not ValidateSourceDirectory() Then
				Return
			End If

			Dim selectedDevices As New List(Of DeviceInfo)()

			For Each lvi As ListViewItem In lvDevices.Items
				If lvi.Checked Then
					selectedDevices.Add(m_devicesInfo(lvi.Index))
				End If
			Next lvi

			If selectedDevices.Count = 0 Then
				ShowError("No device(s) selected for burning.")
				Return
			End If

			For Each di As DeviceInfo In selectedDevices
				If di.MediaFreeSpace < m_RequiredSpace Then
					ShowError("Required space is greater than the free space on device: " & di.Title)
					Return
				End If
			Next di

			CreateProgressWindow()

			If Not PrepareWorkerThreadsResourses(selectedDevices) Then
				DestroyProgressWindow()
				ReleaseWorkerThreadsResources()
				Return
			End If

			m_mainWorkerThread = New Thread(AddressOf MainWorkerThreadProc)
			m_mainWorkerThread.Start()
		End Sub

		Private Function PrepareWorkerThreadsResourses(ByVal selectedDevices As List(Of DeviceInfo)) As Boolean
			m_workers = New List(Of WorkerThreadContext)()

			Using devEnum As DeviceEnumerator = m_engine.CreateDeviceEnumerator()
				For Each di As DeviceInfo In selectedDevices
					Dim ctx As New WorkerThreadContext()
					ctx.burnerIndex = m_workers.Count

					m_workers.Add(ctx)

					ctx.device = devEnum.CreateDevice(di.Index, True)
					If Nothing Is ctx.device Then
						ShowError(devEnum.Error, "Failed to create device")
						Return False
					End If

					ctx.progressForm = m_progressWindow
					ctx.progressInfo = New ProgressInfo()
					ctx.progressInfo.DeviceTitle = di.Title
					ctx.burnerSettings = New BurnSettings()

					' burn settings
						ctx.burnerSettings.SourceFolder = textBoxRootDir.Text
						ctx.burnerSettings.VolumeLabel = textBoxVolumeName.Text

						Dim item As ListItem = CType(comboBoxImageType.SelectedItem, ListItem)
						ctx.burnerSettings.ImageType = CType(item.Value, PrimoSoftware.Burner.ImageType)

						If ctx.device.MediaIsBD Then
							ctx.burnerSettings.WriteMethod = WriteMethod.BluRay
						Else
							item = CType(comboBoxRecordingMode.SelectedItem, ListItem)
							ctx.burnerSettings.WriteMethod = CType(item.Value, WriteMethod)
						End If

						If di.SelectedWriteSpeed IsNot Nothing Then
							ctx.burnerSettings.WriteSpeedKB = di.SelectedWriteSpeed.TransferRateKB
						ElseIf di.MaxWriteSpeed IsNot Nothing Then
							ctx.burnerSettings.WriteSpeedKB = di.MaxWriteSpeed.TransferRateKB
						End If

						ctx.burnerSettings.Simulate = checkBoxSimulate.Checked
						ctx.burnerSettings.CloseDisc = checkBoxCloseDisc.Checked
						ctx.burnerSettings.Eject = checkBoxEjectWhenDone.Checked
				Next di
			End Using

			Return True
		End Function

		Private Sub ReleaseWorkerThreadsResources()
			For Each ctx As WorkerThreadContext In m_workers
				If ctx.device IsNot Nothing Then
					ctx.device.Dispose()
					ctx.device = Nothing
				End If
			Next ctx

			m_workers = Nothing
		End Sub

		Private Sub MainWorkerThreadProc()
			Dim threads As New List(Of Thread)()

			' start burn threads 
			For Each ctx As WorkerThreadContext In m_workers
				Dim thread As New Thread(AddressOf BurnWorkerThreadProc)
				thread.Start(ctx)
				threads.Add(thread)
			Next ctx

			' wait all threads to finish
			For Each thread As Thread In threads
				thread.Join()
			Next thread

			' On thread complete
				m_mainWorkerThread = Nothing

				Dim del As MethodInvoker = Function() AnonymousMethod1()

				UIThread(del)
		End Sub
		
		Private Function AnonymousMethod1() As Object
			ReleaseWorkerThreadsResources()
			DestroyProgressWindow()
			UpdateDevicesInformation()
			Return Nothing
		End Function

		Private Sub BurnWorkerThreadProc(ByVal obj As Object)
			Dim ctx As WorkerThreadContext = TryCast(obj, WorkerThreadContext)

			Using dataDisc As New DataDisc()
				AddHandler dataDisc.OnStatus, AddressOf ctx.DataDisc_OnStatus
				AddHandler dataDisc.OnProgress, AddressOf ctx.DataDisc_OnProgress
				AddHandler dataDisc.OnContinueBurn, AddressOf ctx.DataDisc_OnContinueBurn

				ctx.WriteRate1xKB = GetTransferRate1xKB(ctx.device)

				ctx.device.WriteSpeedKB = ctx.burnerSettings.WriteSpeedKB

				FormatMedia(ctx.device)

				dataDisc.Device = ctx.device
				dataDisc.SimulateBurn = ctx.burnerSettings.Simulate
				dataDisc.WriteMethod = ctx.burnerSettings.WriteMethod
				dataDisc.CloseDisc = ctx.burnerSettings.CloseDisc

				dataDisc.SessionStartAddress = ctx.device.NewSessionStartAddress

				' Set burning parameters
				dataDisc.ImageType = ctx.burnerSettings.ImageType

				SetVolumeProperties(dataDisc, ctx.burnerSettings.VolumeLabel, Date.Now)

				If Not dataDisc.SetImageLayoutFromFolder(ctx.burnerSettings.SourceFolder) Then
					ShowError(dataDisc.Error, "Failed to SetImageLayoutFromFolder")

					ctx.progressInfo.Status = "ERROR"
					ctx.progressForm.UpdateProgress(ctx.progressInfo, ctx.burnerIndex)
					Return
				End If

				If Not dataDisc.WriteToDisc(True) Then
					ShowError(dataDisc.Error, "WriteToDisc failed.")

					ctx.progressInfo.Status = "ERROR"
					ctx.progressForm.UpdateProgress(ctx.progressInfo, ctx.burnerIndex)
					Return
				End If

				ctx.progressInfo.Status = "SUCCESS"
				ctx.progressForm.UpdateProgress(ctx.progressInfo, ctx.burnerIndex)

				If ctx.burnerSettings.Eject Then
					ctx.device.Eject(True)
				End If
			End Using
		End Sub

		Private Sub CreateProgressWindow()
			Me.Enabled = False

			' Create a progress window
			m_progressWindow = New ProgressForm()
			m_progressWindow.Owner = Me
			m_progressWindow.Show()
		End Sub

		Private Sub DestroyProgressWindow()
			If Nothing IsNot m_progressWindow Then
				m_progressWindow.Close()
				m_progressWindow = Nothing
			End If

			Me.Enabled = True
		End Sub

		Private Function ValidateSourceDirectory() As Boolean
			Dim strRootDir As String = textBoxRootDir.Text

			If Not Directory.Exists(strRootDir) Then
				MessageBox.Show("Please specify a valid source directory.")
				textBoxRootDir.Focus()
				Return False
			End If

			If "\"c = strRootDir.Chars(strRootDir.Length -1) OrElse "/"c = strRootDir.Chars(strRootDir.Length - 1) Then
				strRootDir = strRootDir.Substring(0, strRootDir.Length - 1)
				textBoxRootDir.Text = strRootDir
			End If

			Return True
		End Function

		Private Sub ShowError(ByVal errorInfo As ErrorInfo, ByVal description As String)
			Dim message As String = String.Empty

			If Not String.IsNullOrEmpty(description) Then
				message = description & vbCrLf
			End If

			Select Case errorInfo.Facility
				Case ErrorFacility.SystemWindows
					message = (New System.ComponentModel.Win32Exception(errorInfo.Code)).Message

				Case ErrorFacility.Success
					message = "Success"

				Case ErrorFacility.DataDisc
					message = String.Format("DataDisc error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message)

				Case ErrorFacility.Device
					message = String.Format("Device error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message)

				Case Else
					message = String.Format("Facility:{0} error :0x{1:x8}: {2}", errorInfo.Facility, errorInfo.Code, errorInfo.Message)
			End Select

			ShowError(message)
		End Sub

		Private Sub ShowError(ByVal message As String)
			Dim del As MethodInvoker = Function() AnonymousMethod2(message)

			UIThread(del)
		End Sub
		
		Private Function AnonymousMethod2(ByVal message As String) As Object
			MessageBox.Show(message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error)
			Return Nothing
		End Function

		Private Sub UIThread(ByVal code As MethodInvoker)
			If InvokeRequired Then
				Invoke(code)
				Return
			End If

			code.Invoke()
		End Sub

		#Region "Helpers"
		Private Shared Function IsWriterDevice(ByVal device As Device) As Boolean
			Return device.DVDFeatures.CanWriteDVDMinusR OrElse device.DVDFeatures.CanWriteDVDPlusR OrElse device.CDFeatures.CanWriteCDR OrElse device.BDFeatures.CanWriteBDR
		End Function

		Private Shared Function ContainsSpeed(ByVal speeds As List(Of SpeedInfo), ByVal speed As SpeedInfo) As Boolean
			For Each si As SpeedInfo In speeds
				If (si.TransferRateKB = speed.TransferRateKB) AndAlso (Math.Abs(si.TransferRate1xKB - speed.TransferRate1xKB) < 0.01) Then
					Return True
				End If
			Next si

			Return False
		End Function

		Private Shared Function GetWriteSpeeds(ByVal device As Device) As List(Of SpeedInfo)
			Dim speedInfos As New List(Of SpeedInfo)()

            Dim speedDescriptors As IList(Of SpeedDescriptor) = device.GetWriteSpeeds()

			Dim speed1xKB As Double

			speed1xKB = GetTransferRate1xKB(device)

			For Each speed As SpeedDescriptor In speedDescriptors
				Dim speedInfo As New SpeedInfo()
				speedInfo.TransferRateKB = speed.TransferRateKB
				speedInfo.TransferRate1xKB = speed1xKB

				speedInfos.Add(speedInfo)
			Next speed

			Return speedInfos
		End Function

		Private Shared Function GetTransferRate1xKB(ByVal device As Device) As Double
			If device.MediaIsBD Then
				Return Speed1xKB.BD
			ElseIf device.MediaIsDVD Then
				Return Speed1xKB.DVD
			Else
				Return Speed1xKB.CD
			End If
		End Function

		Private Shared Function GetMediaProfileString(ByVal device As Device) As String
			Dim profile As PrimoSoftware.Burner.MediaProfile = device.MediaProfile
			Select Case profile
				Case MediaProfile.CdRom
					Return "CD-ROM. Read only CD."

				Case MediaProfile.CdR
					Return "CD-R. Write once CD."

				Case MediaProfile.CdRw
					Return "CD-RW. Re-writable CD."

				Case MediaProfile.DvdRom
					Return "DVD-ROM. Read only DVD."

				Case MediaProfile.DvdMinusRSeq
					Return "DVD-R Sequential Recording. Write once DVD."

				Case MediaProfile.DvdMinusRDLSeq
					Return "DVD-R DL 8.54GB for Sequential Recording. Write once DVD."

				Case MediaProfile.DvdMinusRDLJump
					Return "DVD-R DL 8.54GB for Layer Jump Recording. Write once DVD."

				Case MediaProfile.DvdRam
					Return "DVD-RAM ReWritable DVD."

				Case MediaProfile.DvdMinusRwRo
					Return "DVD-RW Restricted Overwrite ReWritable."

				Case MediaProfile.DvdMinusRwSeq
					Return "DVD-RW Sequential Recording ReWritable."

				Case MediaProfile.DvdPlusRw
						Dim fmt As BgFormatStatus = device.BgFormatStatus
						Select Case fmt
							Case BgFormatStatus.NotFormatted
								Return "DVD+RW ReWritable DVD. Not formatted."
							Case BgFormatStatus.Partial
								Return "DVD+RW ReWritable DVD. Partially formatted."
							Case BgFormatStatus.Pending
								Return "DVD+RW ReWritable DVD. Background formatting is pending ..."
							Case BgFormatStatus.Completed
								Return "DVD+RW ReWritable DVD. Formatted."
						End Select
						Return "DVD+RW ReWritable DVD."

				Case MediaProfile.DvdPlusR
					Return "DVD+R. Write once DVD."

				Case MediaProfile.DvdPlusRDL
					Return "DVD+R DL 8.5GB. Write once DVD."

				Case MediaProfile.BdRom
					Return "BD-ROM Read only Blu-ray Disc."

				Case MediaProfile.BdRSrm
					Return "BD-R for Sequential Recording."

				Case MediaProfile.BdRSrmPow
					Return "BD-R for Sequential Recording with Pseudo-Overwrite."

				Case MediaProfile.BdRRrm
					Return "BD-R Random Recording Mode (RRM)."

				Case MediaProfile.BdRe
						If device.MediaIsFormatted Then
							Return "BD-RE ReWritable Blu-ray Disc. Formatted."
						End If

						Return "BD-RE ReWritable Blu-ray Disc. Blank. Not formatted."

				Case Else
					Return "Unknown Profile."
			End Select
		End Function

		Private Function FormatMedia(ByVal dev As Device) As Boolean
			Select Case dev.MediaProfile
				' DVD+RW (needs to be formatted before the disc can be used)
				Case MediaProfile.DvdPlusRw
						Select Case dev.BgFormatStatus
							Case BgFormatStatus.NotFormatted
								dev.Format(FormatType.DvdPlusRwFull)

							Case BgFormatStatus.Partial
								dev.Format(FormatType.DvdPlusRwRestart)
						End Select

				' BD-RE (needs to be formatted before the disc can be used)
				Case MediaProfile.BdRe
						If Not dev.MediaIsFormatted Then
							dev.FormatBD(BDFormatType.BdFull, BDFormatSubType.BdReQuickReformat)
						End If
			End Select

			Return True
		End Function

		Private Shared Sub SetVolumeProperties(ByVal data As DataDisc, ByVal volumeLabel As String, ByVal creationTime As Date)
			' Sample settings. Replace with your own data or leave empty
			data.IsoVolumeProps.VolumeLabel = volumeLabel
			data.IsoVolumeProps.VolumeSet = "SET"
			data.IsoVolumeProps.SystemID = "WINDOWS"
			data.IsoVolumeProps.Publisher = "PUBLISHER"
			data.IsoVolumeProps.DataPreparer = "PREPARER"
			data.IsoVolumeProps.Application = "DVDBURNER"
			data.IsoVolumeProps.CopyrightFile = "COPYRIGHT.TXT"
			data.IsoVolumeProps.AbstractFile = "ABSTRACT.TXT"
			data.IsoVolumeProps.BibliographicFile = "BIBLIO.TXT"
			data.IsoVolumeProps.CreationTime = creationTime

			data.JolietVolumeProps.VolumeLabel = volumeLabel
			data.JolietVolumeProps.VolumeSet = "SET"
			data.JolietVolumeProps.SystemID = "WINDOWS"
			data.JolietVolumeProps.Publisher = "PUBLISHER"
			data.JolietVolumeProps.DataPreparer = "PREPARER"
			data.JolietVolumeProps.Application = "DVDBURNER"
			data.JolietVolumeProps.CopyrightFile = "COPYRIGHT.TXT"
			data.JolietVolumeProps.AbstractFile = "ABSTRACT.TXT"
			data.JolietVolumeProps.BibliographicFile = "BIBLIO.TXT"
			data.JolietVolumeProps.CreationTime = creationTime

			data.UdfVolumeProps.VolumeLabel = volumeLabel
			data.UdfVolumeProps.VolumeSet = "SET"
			data.UdfVolumeProps.CopyrightFile = "COPYRIGHT.TXT"
			data.UdfVolumeProps.AbstractFile = "ABSTRACT.TXT"
			data.UdfVolumeProps.CreationTime = creationTime
		End Sub
		#End Region

		#Region "Windows Form Designer generated code"

		Private label4 As System.Windows.Forms.Label
		Private label5 As System.Windows.Forms.Label
		Private label6 As System.Windows.Forms.Label
		Private labelRequiredSpace As System.Windows.Forms.Label
		Private textBoxVolumeName As System.Windows.Forms.TextBox
		Private comboBoxImageType As System.Windows.Forms.ComboBox
		Private WithEvents buttonBrowse As System.Windows.Forms.Button
		Private groupBox4 As System.Windows.Forms.GroupBox
		Private WithEvents comboBoxRecordingMode As System.Windows.Forms.ComboBox
		Private label7 As System.Windows.Forms.Label
		Private checkBoxCloseDisc As System.Windows.Forms.CheckBox
		Private checkBoxSimulate As System.Windows.Forms.CheckBox
		Private groupBox6 As System.Windows.Forms.GroupBox
		Private WithEvents buttonEject As System.Windows.Forms.Button
		Private WithEvents buttonCloseTray As System.Windows.Forms.Button
		Private WithEvents buttonExit As System.Windows.Forms.Button
		Private WithEvents buttonBurn As System.Windows.Forms.Button
		Private textBoxRootDir As System.Windows.Forms.TextBox
		Private checkBoxEjectWhenDone As System.Windows.Forms.CheckBox
		Private WithEvents lvDevices As ListView
		Private columnHeaderDeviceTitle As ColumnHeader
		Private columnHeaderMedia As ColumnHeader
		Private columnHeaderFreeSpace As ColumnHeader
		Private columnHeaderWriteSpeed As ColumnHeader
		Private WithEvents buttonChangeWriteSpeed As System.Windows.Forms.Button

		''' <summary>
		''' Required designer variable.
		''' </summary>
		Private components As System.ComponentModel.Container = Nothing

		''' <summary>
		''' Required method for Designer support - do not modify
		''' the contents of this method with the code editor.
		''' </summary>
		Private Sub InitializeComponent()
			Me.labelRequiredSpace = New System.Windows.Forms.Label()
			Me.label4 = New System.Windows.Forms.Label()
			Me.textBoxRootDir = New System.Windows.Forms.TextBox()
			Me.buttonBrowse = New System.Windows.Forms.Button()
			Me.label5 = New System.Windows.Forms.Label()
			Me.textBoxVolumeName = New System.Windows.Forms.TextBox()
			Me.label6 = New System.Windows.Forms.Label()
			Me.comboBoxImageType = New System.Windows.Forms.ComboBox()
			Me.groupBox4 = New System.Windows.Forms.GroupBox()
			Me.comboBoxRecordingMode = New System.Windows.Forms.ComboBox()
			Me.label7 = New System.Windows.Forms.Label()
			Me.checkBoxCloseDisc = New System.Windows.Forms.CheckBox()
			Me.checkBoxEjectWhenDone = New System.Windows.Forms.CheckBox()
			Me.checkBoxSimulate = New System.Windows.Forms.CheckBox()
			Me.groupBox6 = New System.Windows.Forms.GroupBox()
			Me.buttonEject = New System.Windows.Forms.Button()
			Me.buttonCloseTray = New System.Windows.Forms.Button()
			Me.buttonBurn = New System.Windows.Forms.Button()
			Me.buttonExit = New System.Windows.Forms.Button()
			Me.lvDevices = New System.Windows.Forms.ListView()
			Me.columnHeaderDeviceTitle = (CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader))
			Me.columnHeaderMedia = (CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader))
			Me.columnHeaderFreeSpace = (CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader))
			Me.columnHeaderWriteSpeed = (CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader))
			Me.buttonChangeWriteSpeed = New System.Windows.Forms.Button()
			Me.groupBox4.SuspendLayout()
			Me.groupBox6.SuspendLayout()
			Me.SuspendLayout()
			' 
			' labelRequiredSpace
			' 
			Me.labelRequiredSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
			Me.labelRequiredSpace.Location = New System.Drawing.Point(7, 223)
			Me.labelRequiredSpace.Name = "labelRequiredSpace"
			Me.labelRequiredSpace.Size = New System.Drawing.Size(488, 21)
			Me.labelRequiredSpace.TabIndex = 3
			Me.labelRequiredSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
			' 
			' label4
			' 
			Me.label4.Location = New System.Drawing.Point(7, 259)
			Me.label4.Name = "label4"
			Me.label4.Size = New System.Drawing.Size(80, 16)
			Me.label4.TabIndex = 4
			Me.label4.Text = "Source Folder:"
			' 
			' textBoxRootDir
			' 
			Me.textBoxRootDir.Location = New System.Drawing.Point(95, 259)
			Me.textBoxRootDir.Name = "textBoxRootDir"
			Me.textBoxRootDir.ReadOnly = True
			Me.textBoxRootDir.Size = New System.Drawing.Size(400, 20)
			Me.textBoxRootDir.TabIndex = 5
			' 
			' buttonBrowse
			' 
			Me.buttonBrowse.Location = New System.Drawing.Point(503, 259)
			Me.buttonBrowse.Name = "buttonBrowse"
			Me.buttonBrowse.Size = New System.Drawing.Size(80, 24)
			Me.buttonBrowse.TabIndex = 6
			Me.buttonBrowse.Text = "Browse"
'			Me.buttonBrowse.Click += New System.EventHandler(Me.buttonBrowse_Click)
			' 
			' label5
			' 
			Me.label5.Location = New System.Drawing.Point(7, 297)
			Me.label5.Name = "label5"
			Me.label5.Size = New System.Drawing.Size(80, 16)
			Me.label5.TabIndex = 7
			Me.label5.Text = "Volume name :"
			' 
			' textBoxVolumeName
			' 
			Me.textBoxVolumeName.Location = New System.Drawing.Point(95, 295)
			Me.textBoxVolumeName.MaxLength = 16
			Me.textBoxVolumeName.Name = "textBoxVolumeName"
			Me.textBoxVolumeName.Size = New System.Drawing.Size(136, 20)
			Me.textBoxVolumeName.TabIndex = 8
			Me.textBoxVolumeName.Text = "DATADVD"
			' 
			' label6
			' 
			Me.label6.Location = New System.Drawing.Point(255, 297)
			Me.label6.Name = "label6"
			Me.label6.Size = New System.Drawing.Size(72, 16)
			Me.label6.TabIndex = 9
			Me.label6.Text = "Image Type:"
			' 
			' comboBoxImageType
			' 
			Me.comboBoxImageType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.comboBoxImageType.Location = New System.Drawing.Point(335, 295)
			Me.comboBoxImageType.Name = "comboBoxImageType"
			Me.comboBoxImageType.Size = New System.Drawing.Size(160, 21)
			Me.comboBoxImageType.TabIndex = 10
			' 
			' groupBox4
			' 
			Me.groupBox4.Controls.Add(Me.comboBoxRecordingMode)
			Me.groupBox4.Controls.Add(Me.label7)
			Me.groupBox4.Controls.Add(Me.checkBoxCloseDisc)
			Me.groupBox4.Controls.Add(Me.checkBoxEjectWhenDone)
			Me.groupBox4.Controls.Add(Me.checkBoxSimulate)
			Me.groupBox4.Location = New System.Drawing.Point(7, 323)
			Me.groupBox4.Name = "groupBox4"
			Me.groupBox4.Size = New System.Drawing.Size(576, 75)
			Me.groupBox4.TabIndex = 16
			Me.groupBox4.TabStop = False
			Me.groupBox4.Text = "Parameters"
			' 
			' comboBoxRecordingMode
			' 
			Me.comboBoxRecordingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
			Me.comboBoxRecordingMode.ItemHeight = 13
			Me.comboBoxRecordingMode.Location = New System.Drawing.Point(105, 30)
			Me.comboBoxRecordingMode.Name = "comboBoxRecordingMode"
			Me.comboBoxRecordingMode.Size = New System.Drawing.Size(121, 21)
			Me.comboBoxRecordingMode.TabIndex = 19
'			Me.comboBoxRecordingMode.SelectedIndexChanged += New System.EventHandler(Me.comboBoxRecordingMode_SelectedIndexChanged)
			' 
			' label7
			' 
			Me.label7.Location = New System.Drawing.Point(10, 31)
			Me.label7.Name = "label7"
			Me.label7.Size = New System.Drawing.Size(100, 16)
			Me.label7.TabIndex = 17
			Me.label7.Text = "Recording Mode:"
			' 
			' checkBoxCloseDisc
			' 
			Me.checkBoxCloseDisc.Location = New System.Drawing.Point(245, 31)
			Me.checkBoxCloseDisc.Name = "checkBoxCloseDisc"
			Me.checkBoxCloseDisc.Size = New System.Drawing.Size(76, 24)
			Me.checkBoxCloseDisc.TabIndex = 15
			Me.checkBoxCloseDisc.Text = "Close Disc"
			' 
			' checkBoxEjectWhenDone
			' 
			Me.checkBoxEjectWhenDone.Location = New System.Drawing.Point(334, 31)
			Me.checkBoxEjectWhenDone.Name = "checkBoxEjectWhenDone"
			Me.checkBoxEjectWhenDone.Size = New System.Drawing.Size(121, 24)
			Me.checkBoxEjectWhenDone.TabIndex = 14
			Me.checkBoxEjectWhenDone.Text = "Eject When Done"
			' 
			' checkBoxSimulate
			' 
			Me.checkBoxSimulate.Location = New System.Drawing.Point(464, 31)
			Me.checkBoxSimulate.Name = "checkBoxSimulate"
			Me.checkBoxSimulate.Size = New System.Drawing.Size(104, 24)
			Me.checkBoxSimulate.TabIndex = 13
			Me.checkBoxSimulate.Text = "Simulate"
			' 
			' groupBox6
			' 
			Me.groupBox6.Controls.Add(Me.buttonEject)
			Me.groupBox6.Controls.Add(Me.buttonCloseTray)
			Me.groupBox6.Location = New System.Drawing.Point(7, 149)
			Me.groupBox6.Name = "groupBox6"
			Me.groupBox6.Size = New System.Drawing.Size(196, 59)
			Me.groupBox6.TabIndex = 25
			Me.groupBox6.TabStop = False
			Me.groupBox6.Text = "Device Tray"
			' 
			' buttonEject
			' 
			Me.buttonEject.Location = New System.Drawing.Point(22, 19)
			Me.buttonEject.Name = "buttonEject"
			Me.buttonEject.Size = New System.Drawing.Size(75, 24)
			Me.buttonEject.TabIndex = 23
			Me.buttonEject.Text = "E&ject"
'			Me.buttonEject.Click += New System.EventHandler(Me.buttonEject_Click)
			' 
			' buttonCloseTray
			' 
			Me.buttonCloseTray.Location = New System.Drawing.Point(109, 19)
			Me.buttonCloseTray.Name = "buttonCloseTray"
			Me.buttonCloseTray.Size = New System.Drawing.Size(75, 24)
			Me.buttonCloseTray.TabIndex = 22
			Me.buttonCloseTray.Text = "&Close"
'			Me.buttonCloseTray.Click += New System.EventHandler(Me.buttonCloseTray_Click)
			' 
			' buttonBurn
			' 
			Me.buttonBurn.Location = New System.Drawing.Point(7, 410)
			Me.buttonBurn.Name = "buttonBurn"
			Me.buttonBurn.Size = New System.Drawing.Size(104, 24)
			Me.buttonBurn.TabIndex = 28
			Me.buttonBurn.Text = "Burn"
'			Me.buttonBurn.Click += New System.EventHandler(Me.buttonBurn_Click)
			' 
			' buttonExit
			' 
			Me.buttonExit.Location = New System.Drawing.Point(479, 410)
			Me.buttonExit.Name = "buttonExit"
			Me.buttonExit.Size = New System.Drawing.Size(104, 24)
			Me.buttonExit.TabIndex = 31
			Me.buttonExit.Text = "Exit"
'			Me.buttonExit.Click += New System.EventHandler(Me.buttonExit_Click)
			' 
			' lvDevices
			' 
			Me.lvDevices.CheckBoxes = True
			Me.lvDevices.Columns.AddRange(New System.Windows.Forms.ColumnHeader() { Me.columnHeaderDeviceTitle, Me.columnHeaderMedia, Me.columnHeaderFreeSpace, Me.columnHeaderWriteSpeed})
			Me.lvDevices.FullRowSelect = True
			Me.lvDevices.GridLines = True
			Me.lvDevices.HideSelection = False
			Me.lvDevices.Location = New System.Drawing.Point(8, 12)
			Me.lvDevices.MultiSelect = False
			Me.lvDevices.Name = "lvDevices"
			Me.lvDevices.Size = New System.Drawing.Size(575, 132)
			Me.lvDevices.TabIndex = 33
			Me.lvDevices.UseCompatibleStateImageBehavior = False
			Me.lvDevices.View = System.Windows.Forms.View.Details
'			Me.lvDevices.SelectedIndexChanged += New System.EventHandler(Me.lvDevices_SelectedIndexChanged)
			' 
			' columnHeaderDeviceTitle
			' 
			Me.columnHeaderDeviceTitle.Text = "Device"
			Me.columnHeaderDeviceTitle.Width = 222
			' 
			' columnHeaderMedia
			' 
			Me.columnHeaderMedia.Text = "Media"
			Me.columnHeaderMedia.Width = 108
			' 
			' columnHeaderFreeSpace
			' 
			Me.columnHeaderFreeSpace.Text = "Free Space"
			Me.columnHeaderFreeSpace.Width = 107
			' 
			' columnHeaderWriteSpeed
			' 
			Me.columnHeaderWriteSpeed.Text = "Write Speed"
			Me.columnHeaderWriteSpeed.Width = 108
			' 
			' buttonChangeWriteSpeed
			' 
			Me.buttonChangeWriteSpeed.Location = New System.Drawing.Point(449, 168)
			Me.buttonChangeWriteSpeed.Name = "buttonChangeWriteSpeed"
			Me.buttonChangeWriteSpeed.Size = New System.Drawing.Size(134, 24)
			Me.buttonChangeWriteSpeed.TabIndex = 34
			Me.buttonChangeWriteSpeed.Text = "Change Write Speed"
'			Me.buttonChangeWriteSpeed.Click += New System.EventHandler(Me.buttonChangeWriteSpeed_Click)
			' 
			' BurnerForm
			' 
			Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
			Me.ClientSize = New System.Drawing.Size(597, 450)
			Me.Controls.Add(Me.buttonChangeWriteSpeed)
			Me.Controls.Add(Me.lvDevices)
			Me.Controls.Add(Me.buttonExit)
			Me.Controls.Add(Me.buttonBurn)
			Me.Controls.Add(Me.groupBox6)
			Me.Controls.Add(Me.groupBox4)
			Me.Controls.Add(Me.comboBoxImageType)
			Me.Controls.Add(Me.label6)
			Me.Controls.Add(Me.textBoxVolumeName)
			Me.Controls.Add(Me.textBoxRootDir)
			Me.Controls.Add(Me.label5)
			Me.Controls.Add(Me.buttonBrowse)
			Me.Controls.Add(Me.label4)
			Me.Controls.Add(Me.labelRequiredSpace)
			Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
			Me.MaximizeBox = False
			Me.MinimizeBox = False
			Me.Name = "BurnerForm"
			Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
			Me.Text = "PrimoBurner(tm) Engine for .NET - MultiDataDisc - Burning Sample Application"
'			Me.FormClosing += New System.Windows.Forms.FormClosingEventHandler(Me.BurnerForm_FormClosing)
'			Me.Load += New System.EventHandler(Me.BurnerForm_Load)
			Me.groupBox4.ResumeLayout(False)
			Me.groupBox6.ResumeLayout(False)
			Me.ResumeLayout(False)
			Me.PerformLayout()

		End Sub
		#End Region
	End Class
End Namespace
