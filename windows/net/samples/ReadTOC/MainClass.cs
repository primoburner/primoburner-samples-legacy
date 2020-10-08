using System;
using PrimoSoftware.Burner;

namespace ReadTOC.NET
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
			// Initialize the hpCDE library.
			Library.Initialize();

            Library.EnableTraceLog(null, true);

			// Create the engine object
			Engine eng = new Engine();

			// Initialize the engine
			eng.Initialize();

			// Enumerate all devices
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
				// Items are actually new instances of Device
				dev = devs.CreateDevice(i);
				Console.WriteLine("{0}. {1}: {2}", i+1, dev.DriveLetter, dev.Description);
				dev.Dispose();
			}

			int iDeviceIndex = -1;
			while (true)
			{
				try 
				{
					// Read user input 
					iDeviceIndex = int.Parse(Console.ReadLine());

					// Check range, if ok exit this loop
					if (iDeviceIndex > 0 && iDeviceIndex <= devs.Count)
						break;
				}
				catch {} 
			}

			dev  = devs.CreateDevice(iDeviceIndex - 1);
			if (null != dev)
			{
				SessionInfo si = dev.ReadSessionInfo();

                if (null != si)
                {
                    for (byte bSessionNumber = si.FirstSession; bSessionNumber <= si.LastSession; bSessionNumber++)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Session {0}", bSessionNumber);
                        Console.WriteLine();

                        // Read and print the Table Of Content of the disk
                        Toc toc = dev.ReadTocFromSession(bSessionNumber);
                        if (null != toc)
                        {
                            int iIndex; long lAddr;
                            for (byte bTrackNumber = toc.FirstTrack; bTrackNumber <= toc.LastTrack; bTrackNumber++)
                            {
                                iIndex = bTrackNumber - toc.FirstTrack;
                                lAddr = toc.Tracks[iIndex].Address;

                                if (toc.Tracks[iIndex].IsData)
                                    Console.WriteLine("\t{0:0#} Data  LBA: {1:0#####}. Time: ({2:0#}:{3:0#}) (2048 bytes per block)", iIndex + 1, lAddr, lAddr / 4500, (lAddr % 4500) / 75);
                                else
                                    Console.WriteLine("\t{0:0#} Audio LBA: {1:0#####}. Time: ({2:0#}:{3:0#}) (2352 bytes per block)", iIndex + 1, lAddr, lAddr / 4500, (lAddr % 4500) / 75);
                            }

                            iIndex = toc.LastTrack - toc.FirstTrack + 1;
                            lAddr = toc.Tracks[iIndex].Address;
                            Console.WriteLine("\t{0:0#} Lead-out LBA: {1:0#####}. Time: ({2:0#}:{3:0#}) (2352 bytes per block)", iIndex + 1, lAddr, lAddr / 4500, (lAddr % 4500) / 75);
                        }
                    }
                }
			}
	
			Console.WriteLine("Press any key...");
			Console.Read();

			// Free device instance
			if(null != dev)
				dev.Dispose();

			devs.Dispose();

			// Shut down the engine
			eng.Shutdown();

			eng.Dispose();

            Library.DisableTraceLog();

			Library.Shutdown();
		}
	}
}

