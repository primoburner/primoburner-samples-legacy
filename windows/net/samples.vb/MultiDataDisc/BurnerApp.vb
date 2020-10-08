Imports System
Imports System.Windows.Forms

Namespace MultiDataDisc
	Public Class BurnerApp
		''' <summary>
		''' The main entry point for the application.
		''' </summary>
		<STAThread> _
		Shared Sub Main()
			' Initialize the SDK
			PrimoSoftware.Burner.Library.Initialize()
			' Library.EnableTraceLog(null, true);

			' Set license string
			'PrimoSoftware.Burner.Library.SetLicense("PRIMO-LICENSE");

			Application.EnableVisualStyles()
			Application.SetCompatibleTextRenderingDefault(False)

			Try
				Application.Run(New BurnerForm())
			Catch e As Exception
				MessageBox.Show(e.Message)
			End Try

			' Library.DisableTraceLog();
			' Shutdown the SDK
			PrimoSoftware.Burner.Library.Shutdown()
		End Sub
	End Class
End Namespace
