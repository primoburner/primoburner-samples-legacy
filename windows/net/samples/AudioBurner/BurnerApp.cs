using System;
using System.Windows.Forms;

namespace AudioBurner.NET
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
			try
			{
                Application.EnableVisualStyles();
				Application.Run(new BurnerForm());
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}
	}
}
