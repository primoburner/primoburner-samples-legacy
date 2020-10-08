using System;
using PrimoSoftware.Burner;

namespace ReadCDText.NET
{
	/// <summary>
	/// Summary description for MainClass.
	/// </summary>
	class MainClass
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			// Initialize the library.
			Library.Initialize();

            Library.EnableTraceLog(null, true);

			// Create the engine object
			Engine eng = new Engine();

			// Initialize the engine
			eng.Initialize();

			// Enumerate all CD/DVD devices
			DeviceEnumerator devs = eng.CreateDeviceEnumerator();
			if (0 == devs.Count)
			{
				devs.Dispose();
				
				eng.Shutdown();
				eng.Dispose();

				Library.Shutdown();

				Console.WriteLine("No devices available.");
				return;
			}

			Console.WriteLine("Please select a device.");
			Console.WriteLine();

			Device dev = null;
			for (int i = 0; i < devs.Count; i++)
			{
				// Items are actually instances of Device
				dev = devs.CreateDevice(i);
				Console.WriteLine("{0}. {1}: {2}", i+1, dev.DriveLetter, dev.Description);
				dev.Dispose();
			}

			int nDeviceIndex = -1;
			while (true)
			{
				try 
				{
					// Read user input 
					nDeviceIndex = int.Parse(Console.ReadLine());

					// Check range, if ok exit this loop
					if (nDeviceIndex > 0 && nDeviceIndex <= devs.Count)
						break;
				}
				catch {}
			}

			dev  = devs.CreateDevice(nDeviceIndex - 1);

			if(null != dev)
			{
				// Display the CD Text information	
                CDText cdText = dev.ReadCDText();
				if(null != cdText)
				{
					// Track 0 contains the album information 
					Console.WriteLine("\nAlbum");
					Console.WriteLine("------------------------------------");
					Console.WriteLine("Name: {0}", cdText.Items[0].Title);
                    Console.WriteLine("Performer: {0}", cdText.Items[0].Performer);
					Console.WriteLine("Song Writer: {0}", cdText.Items[0].Songwriter);
					Console.WriteLine("Composer: {0}", cdText.Items[0].Composer);
					Console.WriteLine("Arranger: {0}", cdText.Items[0].Arranger);
					Console.WriteLine("Message: {0}", cdText.Items[0].Message);
					Console.WriteLine("Disk ID: {0}", cdText.Items[0].DiskId);
					Console.WriteLine("Genre: {0}", cdText.Items[0].Genre);
					Console.WriteLine("Genre Text: {0}", cdText.Items[0].GenreText);
                    Console.WriteLine("UPC/EAN: {0}", cdText.Items[0].UpcIsrc);

					Console.WriteLine("------------------------------------");
					Console.WriteLine("Tracks ");
					Console.WriteLine("------------------------------------");

					for (int i = 1; i < cdText.Items.Count; i++) 
					{
						Console.WriteLine();
						Console.WriteLine("Track {0}", i);
						Console.WriteLine();
                        Console.WriteLine("Name: {0}", cdText.Items[i].Title);
                        Console.WriteLine("Performer: {0}", cdText.Items[i].Performer);
                        Console.WriteLine("Song Writer: {0}", cdText.Items[i].Songwriter);
                        Console.WriteLine("Composer: {0}", cdText.Items[i].Composer);
                        Console.WriteLine("Arranger: {0}", cdText.Items[i].Arranger);
                        Console.WriteLine("Message: {0}", cdText.Items[i].Message);
                        Console.WriteLine("ISRC: {0}", cdText.Items[i].UpcIsrc);
						Console.WriteLine("------------------------------------");
					}
				}
				else
				{
					Console.WriteLine("No CD-Text on the disk or CD-Text unaware drive.");
				}
			}

			Console.WriteLine("Press any key...");
			Console.Read();

			if(null != dev)
				dev.Dispose();

			devs.Dispose();

			eng.Shutdown();
			eng.Dispose();

            Library.DisableTraceLog();

			Library.Shutdown();
		}
	}
}
