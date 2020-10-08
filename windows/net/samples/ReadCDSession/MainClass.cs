using System;
using System.Threading;
using System.Runtime.InteropServices;
using PrimoSoftware.Burner;

namespace ReadCDSession.NET
{

	class MainClass
	{
		[DllImport("msvcrt.dll", EntryPoint="_getch")]
		protected static extern int getch();

		static string TypeToMode(TrackType tt)
		{
			switch(tt)
			{
					/** Audio Track */
				case TrackType.Audio:
					return "Audio";

					/** Mode 0 Track */
				case TrackType.Mode0: 
					return "Mode0";

					/** Mode 1, Data Track */
				case TrackType.Mode1: 
					return "Mode1";
				
					/** Mode 2 Formless */
				case TrackType.Mode2Formless: 
					return "Mode2";

					/** Mode 2, Form 1 Data Track */
				case TrackType.Mode2Form1: 
					return "Mode2 Form1";

					/** Mode 2, Form 2 Data Track */
				case TrackType.Mode2Form2:
					return "Mode2 Form2";

					/** Mode 2, Mixed Form Data Track */
				case TrackType.Mode2Mixed:
					return "Mode2 Mixed";
			}

			return "";
		}

		static void OnRead(Object sender, DeviceReadEventArgs eArgs)
		{
			long lStartLba = eArgs.StartLba;
			long lEndLba = eArgs.EndLba;
			long lCurrentLba = eArgs.CurrentLba;
			int dwBlocks = eArgs.Blocks;
		
			// simple progress implementation
			int nPercentage = (int) ((double) (lCurrentLba - lStartLba) * 100 / (double) (lEndLba - lStartLba)); 
			nPercentage /= 2;

			Console.Write("\r");
			while (nPercentage-- > 0)
				Console.Write("|"); 
		}

		static void OnContinueRead(Object sender, DeviceContinueEventArgs eArgs)
		{
			eArgs.Continue =  true;
		}

		[STAThread]
		static void Main(string[] args)
		{
			int ch;
			int nDeviceIndex = -1;
			
			// Initialize the library.
			Library.Initialize();

            Library.EnableTraceLog(null, true);

			// Create the engine object
			Engine engine = new Engine();

			// Initialize the engine
			engine.Initialize();

			// Enumerate all devices
			DeviceEnumerator devs = engine.CreateDeviceEnumerator();
			if (0 == devs.Count) 
			{
				devs.Dispose();
				
				engine.Shutdown();
				engine.Dispose();

				Library.Shutdown();
				
				Console.WriteLine("No devices available.");
				return;
			}

			Console.WriteLine("Select a device.");

			Device device = null;
			do
			{
				for(int i = 0; i < devs.Count; i++)
				{
					// Items are actually instances of Device
					device = devs.CreateDevice(i);
					Console.Write("  ({1}:\\) {2}:\n", i+1, device.DriveLetter, device.Description);
					device.Dispose();
				}

				Console.Write("\nEnter drive letter:\n");
				ch = getch();

				nDeviceIndex = Library.GetCDROMIndexFromLetter(Convert.ToChar(ch));
			}
			while ((nDeviceIndex < 0) || (nDeviceIndex > devs.Count - 1));

			// Get device
			device = devs.CreateDevice(nDeviceIndex);
			if (null == device)
			{
				devs.Dispose();

				engine.Shutdown();
				engine.Dispose();

				return;
			}

			Console.Write("\nScanning first session.\nThis could take a few minutes ...\n");
			Console.Write("0%                       50%                  100%\n");

			device.OnContinueRead += OnContinueRead;
			device.OnRead += OnRead;

			// select max read speed; 
			device.ReadSpeedKB = device.MaxReadSpeedKB;

			// read the first session layout
			CDSession se = device.ReadCDSessionLayout(1, true, true, 300);
            if (se!=null)
			{
				Console.Write("\n\n");
				Console.Write("MCN (UPC): {0}\n", se.Mcn);

				// list tracks
				int nTrackCount = se.Tracks.Count;
				for (int nTrack = 0; nTrack < nTrackCount; nTrack++)
				{
					CDTrack te = se.Tracks[nTrack];
					if (null == te)
						continue;

					// track
					Console.Write("\nTNO: {0}, {1} ISRC: {2} PS: {3} TS: {4} TE: {5} PE: {6}\n", 
						((int)(nTrack + 1)).ToString("D2"),
						TypeToMode(te.Type),
						te.Isrc,
						te.PregapStart.ToString("D6"),
						te.Start.ToString("D6"),
						te.End.ToString("D6"),
						te.PostgapEnd.ToString("D6"));

					// Indexes
					if (te.Indexes.Count > 0)
					{
                        for (int nIndex = 0; nIndex < te.Indexes.Count; nIndex++)
							Console.Write("Index: {0}, Position: {1}\n", ((int)(nIndex + 2)).ToString(), te.Indexes[nIndex].ToString());
					}

					// modes
					if (te.Modes.Count > 0)
					{
						for (int nSubTypeIndex = 0; nSubTypeIndex < te.Modes.Count; nSubTypeIndex++)
						{
							CDMode tti = te.Modes[nSubTypeIndex];
							Console.Write("Mode: {0}, Position: {1}\n", TypeToMode(tti.Type), tti.Position.ToString());
						}	
					}
				}

				Console.Write("Press any key...\n");
				ch = getch();
			}

			device.Dispose();
			devs.Dispose();

			engine.Shutdown();
			engine.Dispose();

            Library.DisableTraceLog();

		}
	}
}
