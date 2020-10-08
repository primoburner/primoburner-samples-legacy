using System;
using System.Windows.Forms;

namespace MultiAudioCD
{
	public class BurnerApp
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void  Main() 
		{
            // Initialize the SDK
            PrimoSoftware.Burner.Library.Initialize();
            // Library.EnableTraceLog(null, true);

            // Set license string
            // PrimoSoftware.Burner.Library.SetLicense("PRIMO-LICENSE");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			try
			{
				Application.Run(new BurnerForm());
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}

            // Library.DisableTraceLog();
            // Shutdown the SDK
            PrimoSoftware.Burner.Library.Shutdown();
		}
	}
}
