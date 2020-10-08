using System;
using System.Windows.Forms;

namespace BluRayBurner.NET
{
	/// <summary>
	/// Summary description for MainClass.
	/// </summary>
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

            // Set license string
            const string license = @"<primoSoftware></primoSoftware>";
            PrimoSoftware.Burner.Library.SetLicense(license);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new BurnerForm());

            // Shutdown the SDK
            PrimoSoftware.Burner.Library.Shutdown();
		}
	}
}
