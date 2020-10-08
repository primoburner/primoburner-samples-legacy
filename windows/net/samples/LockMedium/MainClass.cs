using System;
using PrimoSoftware.Burner;
using System.Runtime.InteropServices;

namespace LockMedium.NET
{
	class MainClass
	{
		[DllImport("msvcrt.dll", EntryPoint="_getch")]
		protected static extern int getch();

		[STAThread]
		static void Main(string[] args)
		{
			Library.Initialize();	//Initalize the library
            Library.EnableTraceLog(null, true);

			Engine eng = new Engine();
			eng.Initialize();	//try to initalize the engine

			DeviceEnumerator devs = eng.CreateDeviceEnumerator();	//Enumerate all devices
			int nCount = devs.Count;
			if (nCount == 0)
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
			for (int i = 0; i < nCount; i++)
			{
				// Items are actually instances of CDDevice12
				dev = devs.CreateDevice(i);
				Console.WriteLine("{0}. {1}: {2}", i+1, dev.DriveLetter, dev.Description);
				dev.Dispose();
			}

			int nDeviceIndex = -1;
			int ch;

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
				if (dev.MediaState== MediaReady.NotPresent)
					Console.WriteLine("Media not present");
				if (dev.LockMedia(true))	//Lock the device
				{
					Console.WriteLine("Medium Locked.");
					Console.WriteLine("Press any key to unlock the medium");
					ch = getch();
					if (dev.LockMedia(false))	//unlock the device
						Console.WriteLine("Medium unlocked.");
					else
						Console.WriteLine("Could not unlock the medium.");
				}
				else
					Console.WriteLine("Could not lock the medium.");

				// print available blocks
				Console.WriteLine("{0} blocks available on the disc", dev.MediaFreeSpace);
				
				// Eject
				Console.WriteLine("Press any key to eject the device");
                ch = getch();
				dev.Eject(true);

				// Close tray
				Console.WriteLine("Insert a blank disc. Press any key to close the device tray.");
				ch = getch();
				dev.Eject(false);

				//Refresh status
				dev.Refresh();

				//print available blocks again
				Console.WriteLine("{0} blocks available on the disc", dev.MediaFreeSpace);

				//Exit
				Console.WriteLine("Press any key to exit");
				ch = getch();

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
}
