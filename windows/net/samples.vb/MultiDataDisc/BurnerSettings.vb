Imports System
Imports PrimoSoftware.Burner


Namespace MultiDataDisc
	' Burn Settings
	Public Class BurnSettings
		Public SourceFolder As String
		Public VolumeLabel As String

		Public ImageType As PrimoSoftware.Burner.ImageType = ImageType.None
		Public WriteMethod As PrimoSoftware.Burner.WriteMethod = WriteMethod.DvdIncremental
		Public WriteSpeedKB As Integer = 0

		Public Simulate As Boolean = False
		Public CloseDisc As Boolean = True
		Public Eject As Boolean = True
	End Class

	Friend Class WorkerThreadContext
		Public device As Device
		Public progressForm As ProgressForm
		Public progressInfo As ProgressInfo
		Public burnerIndex As Integer
		Public burnerSettings As BurnSettings
		Public WriteRate1xKB As Double = -1

		Public Sub DataDisc_OnStatus(ByVal sender As Object, ByVal args As DataDiscStatusEventArgs)
			progressInfo.Status = GetDataDiscStatusString(args.Status)
			progressForm.UpdateProgress(progressInfo, burnerIndex)
		End Sub

		Public Sub DataDisc_OnProgress(ByVal sender As Object, ByVal args As DataDiscProgressEventArgs)
			If args.All > 0 Then
				Dim progress As Double = 100 * CDbl(args.Position) / CDbl(args.All)

				If (progress - progressInfo.Progress) > 0.1 Then
					progressInfo.ProgressStr = String.Format("{0:0.0}%", progress)
					progressInfo.Progress = progress

					If WriteRate1xKB > 0 Then
						progressInfo.WriteSpeed = String.Format("{0:0.0}x", CDbl(device.WriteTransferRate) / WriteRate1xKB)
					End If

					progressForm.UpdateProgress(progressInfo, burnerIndex)
				End If
			End If
		End Sub

		Public Sub DataDisc_OnContinueBurn(ByVal sender As Object, ByVal e As DataDiscContinueEventArgs)
			e.Continue = Not progressForm.Stopped
		End Sub

		Private Shared Function GetDataDiscStatusString(ByVal status As DataDiscStatus) As String
			Select Case status
				Case DataDiscStatus.BuildingFileSystem
					Return "Building filesystem..."
				Case DataDiscStatus.LoadingImageLayout
					Return "Loading image layout..."
				Case DataDiscStatus.WritingFileSystem
					Return "Writing filesystem..."
				Case DataDiscStatus.WritingImage
					Return "Writing image..."
				Case DataDiscStatus.CachingSmallFiles
					Return "Caching small files..."
				Case DataDiscStatus.CachingNetworkFiles
					Return "Caching network files..."
				Case DataDiscStatus.CachingCDRomFiles
					Return "Caching CDROM files..."
				Case DataDiscStatus.Initializing
					Return "Initializing and writing lead-in..."
				Case DataDiscStatus.Writing
					Return "Writing..."
				Case DataDiscStatus.WritingLeadOut
					Return "Writing lead-out and flushing cache..."
			End Select

			Return "Unknown status..."
		End Function
	End Class


	Friend Class ListItem
		Public Value As Object
		Public Description As String

		Public Sub New(ByVal nvalue As Object, ByVal description As String)
			Value = nvalue
			Me.Description = description
		End Sub

		Public Overrides Function ToString() As String
			Return Description
		End Function
	End Class


	Public Class SpeedInfo
		Public TransferRateKB As Integer
		Public TransferRate1xKB As Double
		Public Overrides Function ToString() As String
			Return String.Format("{0}x", Math.Round(CDbl(TransferRateKB) / TransferRate1xKB, 1))
		End Function
	End Class

	''' <summary>
	''' Container for device information
	''' </summary>
	Public Class DeviceInfo
		''' <summary>
		''' Device index in DeviceEnumerator
		''' </summary>
		Public Index As Integer

		Public DriveLetter As Char

		Public Title As String

		Public MediaProfile As MediaProfile

		Public MediaProfileString As String

		Public MediaFreeSpace As Long

		Public SelectedWriteSpeed As SpeedInfo

		Public MaxWriteSpeed As SpeedInfo
	End Class

	Public Class ProgressInfo
		Public DeviceTitle As String = String.Empty
		Public Status As String = String.Empty
		Public ProgressStr As String = String.Empty
		Public Progress As Double = 0.0
		Public WriteSpeed As String = String.Empty
	End Class
End Namespace
