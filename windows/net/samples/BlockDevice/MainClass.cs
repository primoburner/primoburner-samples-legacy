
using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using PrimoSoftware.Burner;

namespace BlockDevice.NET
{

	// Structure to keep the file information
	class TFile
	{
		// full path to the file
		public string sPath;

		// address on the CD/DVD
		public int dwDiscAddress;

		// size on the CD/DVD (in blocks)
		public long ddwDiscSize;
	}

	enum BurnOption
	{
		Unknown	= 0,
		Write,		// write
		ReadDiscID,	// read
		Erase		// erase
	}

	class MainClass
	{
		[DllImport("msvcrt.dll", EntryPoint="_getch")]
		protected static extern int getch();

		private static ArrayList	m_oSourceFiles = new ArrayList();
		private static BurnOption	m_eBurnOption = BurnOption.Unknown;

		// Size must be aligned to 16 blocks
		private const int BLOCKS_PER_WRITE  = 10 * 16;

		/////////////////////////////////////////////
		// Command line handlers 
		//
		static void Usage()
		{
			Console.Write("BlockDevice [-e] [-i <sourcefile>] [-i <sourcefile>] [-i <sourcefile>]\n");
			Console.Write("    -i sourcefile = file to burn, multiple files can be specified.\n");
			Console.Write("    -e            = erase RW disc. \n");
			Console.Write("    -d            = read and display temporary disc ID. \n");
		}

		static int ParseCommandLine(string[] args)
		{
			int		i = 0 ;
			string	sCommand = "";	

			for (i=0; i < args.Length; i++)
			{
				sCommand = args[i];

				//Input file
				if (sCommand == "-i")
				{
					i++;
					if (i == args.Length)
					{
						Usage();
						return -1;
					}

					TFile f = new TFile();
					f.sPath = args[i];

					m_oSourceFiles.Add(f);

					continue;
				}

				//Erase
				if (sCommand == "-e")
				{
					if (m_eBurnOption != BurnOption.Unknown)
					{
						Usage();
						return -1;
					}

					m_eBurnOption = BurnOption.Erase;
					continue;
				}

				// Read Disc ID
				if (sCommand == "-d")
				{
					if (m_eBurnOption != BurnOption.Unknown)
					{
						Usage();
						return -1;
					}

					m_eBurnOption = BurnOption.ReadDiscID;
					continue;
				}
			}

			if (m_eBurnOption == BurnOption.Unknown)
				m_eBurnOption = BurnOption.Write;

			if ((BurnOption.Erase != m_eBurnOption) && (BurnOption.ReadDiscID != m_eBurnOption) && (m_oSourceFiles.Count == 0))
			{
				Usage();
				return -1;
			}

			return 0;
		}

		private static Device SelectDevice(Engine engine)
		{
			int ch;
			int nDevice;

			// A variable to keep the return value
			Device device = null;

			// Get a device enumerator object. At the time of the creation it will enumerate all CD and DVD Writers installed in the system
			DeviceEnumerator devices = engine.CreateDeviceEnumerator();
	
			// Get the number of available devices
			int nCount = devices.Count;

			// If nCount is 0 most likely there are not any CD/DVD writer drives installed in the system 
			if (0 == nCount) 
			{
				Console.WriteLine("No devices available.");

				// Release the enumrator to free any allocated resources
				devices.Dispose();
				return null;
			}

			// Now ask the user to choose a device
			do
			{
				Console.WriteLine("Select a device:");

				// Loop through all the devices and show their name and description
				for (int i = 0; i < nCount; i++) 
				{
					// Get device instance from the enumerator
					device = devices.CreateDevice(i);

					// GetItem may return null if the device was locked
					if (null != device)
					{
						// Get device description
						string strName;
						strName = device.Description;

						// When showing the devices, show also the drive letter
						Console.WriteLine("  ({0}:\\) {1}:", device.DriveLetter, strName);

						device.Dispose();
					}
				}

				Console.WriteLine("Enter drive letter:");
				ch = getch();

				nDevice = Library.GetCDROMIndexFromLetter(Convert.ToChar(ch));
			} 
			while (nDevice < 0 || nDevice > nCount - 1);

			device = devices.CreateDevice(nDevice);
			if (null == device)
			{
				devices.Dispose();
				return null;
			}

			devices.Dispose();
			return device;
		}

		private static void WaitUnitReady(Device oDevice)
		{
			int eError = oDevice.UnitReady;
			while (eError != (int)DeviceError.Success)
			{
				PrintError(oDevice);
				System.Threading.Thread.Sleep(1000);
				eError = oDevice.UnitReady;
			}

			Console.WriteLine("Unit is ready.");
		}

		/////////////
		// Erase
		private static void Erase(Device  oDevice)
		{
			MediaProfile mp = oDevice.MediaProfile;
			switch(mp)
			{
				// DVD+RW (needs to be formatted before the disc can be used)
				case MediaProfile.DvdPlusRw:
				{
					Console.WriteLine("Formatting...");

					BgFormatStatus fmt = oDevice.BgFormatStatus;
					switch(fmt)
					{
						case BgFormatStatus.NotFormatted:
							oDevice.Format(FormatType.DvdPlusRwFull);
							break;
						case BgFormatStatus.Partial:
							oDevice.Format(FormatType.DvdPlusRwRestart);
							break;
					}
				}
				break;

				// DVD-RW, Sequential Recording (default for new DVD-RW)
				case MediaProfile.DvdMinusRwSeq:
					Console.WriteLine("Erasing...");
					oDevice.Erase(EraseType.Minimal);
					break;

				// DVD-RW, Restricted Overwrite (DVD-RW was formatted initially)
				case MediaProfile.DvdMinusRwRo:
					Console.WriteLine("Formatting...\n");
					oDevice.Format(FormatType.DvdMinusRwQuick);
					break;

				case MediaProfile.CdRw:
					Console.WriteLine("Erasing...");
					oDevice.Erase(EraseType.Minimal);
					break;
			}

			// Must be DVD-R, DVD+R or CD-R
		}

		private static DataFile CreateFileSystemTree()
		{
			DataFile oRoot = new DataFile();

			oRoot.IsDirectory = true;
			oRoot.FilePath = "\\";
			oRoot.LongFilename = "\\";

			for (int i = 0; i < m_oSourceFiles.Count; i++)
			{
				TFile file  = (TFile)m_oSourceFiles[i];

				DataFile oDataFile = new DataFile();

					// it is a file
					oDataFile.IsDirectory = false;

					// filename long and short
					oDataFile.LongFilename = Path.GetFileName(file.sPath);
					oDataFile.ShortFilename = "";

					oDataFile.DataSource = DataSourceType.Disc;	// it is already on the cd/dvd
					oDataFile.DiscAddress = file.dwDiscAddress;	// set the disc address
					oDataFile.FileSize = file.ddwDiscSize;		// and the size

					oRoot.Children.Add(oDataFile);
			}

			return oRoot;
		}

		private static string ConstructErrorMessage(int systemError)
		{
			return new System.ComponentModel.Win32Exception(systemError).Message;
		}

		private static string ConstructErrorMessage(Device device)
		{
			string message = string.Empty;
			if (null != device)
			{
                ErrorInfo error = device.Error;
				switch (error.Facility)
				{
					case ErrorFacility.SystemWindows:
						message = ConstructErrorMessage(error.Code);
						break;
					default:
						message = string.Format("Device error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, error.Code, error.Message);
						break;
				}
			}
			return message;
		}

		private static string ConstructErrorMessage(PrimoSoftware.Burner.BlockDevice blockDevice, Device device)
		{
			string message = string.Empty;
			if (null != blockDevice)
			{
                ErrorInfo error = blockDevice.Error;
				switch (error.Facility)
				{
					case ErrorFacility.Device:
						message = ConstructErrorMessage(device);
						break;
					case ErrorFacility.SystemWindows:
						message = ConstructErrorMessage(error.Code);
						break;
					default:
						message = string.Format("BlockDevice error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, (int)error.Code, error.Message);
						break;
				}
			}
			return message;
		}

		private static void PrintError(PrimoSoftware.Burner.BlockDevice blockDevice, Device device)
		{
			string message = ConstructErrorMessage(blockDevice, device);
			Console.WriteLine(message);
		}

		private static void PrintError(Device device)
		{
			string message = ConstructErrorMessage(device);
			Console.WriteLine(message);
		}

		private static bool BurnFile(PrimoSoftware.Burner.BlockDevice oBlockDevice, int iFileIndex)
		{
			// Get the start address at which BlockDevice will start writing.
			int iDiscAddress = oBlockDevice.WriteAddress;

			
			FileStream oFile = null;
			
			try
			{
				// Open file
				oFile = File.OpenRead( ((TFile)m_oSourceFiles[iFileIndex]).sPath);
			}
			catch (IOException ex)
			{
				Debug.WriteLine(ex.Message);
				return false;
			}

			// Set up write progress counters
			long lCurrent = 0;
			long lAll = (long) oFile.Length;

			// Allocate read buffer
			byte[] oBuffer = new byte[(int)BlockSize.Dvd * BLOCKS_PER_WRITE]; 

			// Write the data
			while(lCurrent < lAll)
			{
				int iRead = 0;
				
				iRead = oFile.Read(oBuffer, 0, (int)BlockSize.Dvd * BLOCKS_PER_WRITE);
				if (iRead != 0)
				{
					// Align on 2048 bytes
					if ((iRead % ((int)BlockSize.Dvd)) != 0)
						iRead += (int)BlockSize.Dvd - (iRead % (int)BlockSize.Dvd);  

					// Write the data
					if(!oBlockDevice.Write(oBuffer, 0, (int)iRead))
						break;
				}
					
				// Update current position (bytes)
				lCurrent += (long)iRead;
			}

			// Close file
			oFile.Close();

			// Update TFile structure
			((TFile)m_oSourceFiles[iFileIndex]).dwDiscAddress = iDiscAddress;
			((TFile)m_oSourceFiles[iFileIndex]).ddwDiscSize = lAll;

			return true;
		}

		private static bool Burn(PrimoSoftware.Burner.BlockDevice oBlockDevice)
		{
			// Open block device 
			if (!oBlockDevice.Open())
				return false;

			// Burn all files that the user specified
			for(int i = 0; i < m_oSourceFiles.Count; i++)
				BurnFile(oBlockDevice, i);

			// Close block device
			if (!oBlockDevice.Close())
				return false;

			// Create files system tree
			DataFile oFileSystem = CreateFileSystemTree();

			// Finalize disc
			if (!oBlockDevice.FinalizeDisc(oFileSystem, "HPCDEDISC", true, true, false, true))
				return false;

			return true;
		}

		private static bool Burn(Device oDevice)
		{
			Debug.Assert((oDevice != null));

			// Set write speed
			int iMaxSpeedKB = oDevice.MaxWriteSpeedKB;
			if (oDevice.MediaIsDVD) 
			{
				Console.WriteLine("Setting write speed to {0}x", Math.Round((double)iMaxSpeedKB / Speed1xKB.DVD, 1));
			}
            else if (oDevice.MediaIsBD)
            {
                Console.WriteLine("Setting write speed to {0}x", Math.Round((double)iMaxSpeedKB / Speed1xKB.BD, 1));
            }
            else
			{
				Console.WriteLine("Setting write speed to {0}x", Math.Round((double)iMaxSpeedKB / Speed1xKB.CD));
			}

            oDevice.WriteSpeedKB = iMaxSpeedKB;

			// Create block device object
			PrimoSoftware.Burner.BlockDevice oBlockDevice = new PrimoSoftware.Burner.BlockDevice();
			
			// Set device
			oBlockDevice.Device = oDevice;

			// Set temporary disc ID
			oBlockDevice.TempDiscID = "DISCID";

			// Burn 
			bool bRes = Burn(oBlockDevice);

			// Check for errors
			if (!bRes) 
			{
				PrintError(oBlockDevice, oDevice);
		
				oBlockDevice.Dispose();
				return false;
			}

			oBlockDevice.Dispose();
			
			return true;
		}

		private static void ReadDiscID(Device oDevice)
		{
			//Create block device object
			PrimoSoftware.Burner.BlockDevice oBlockDevice = new PrimoSoftware.Burner.BlockDevice();
			
			//Set device
			oBlockDevice.Device = oDevice;

			// Open block device 
			if (oBlockDevice.Open(BlockDeviceOpenFlags.Read))
			{
				// Show information about the disc in the device
				Console.WriteLine();
				Console.WriteLine("Disc Info");
				Console.WriteLine("---------");

				Console.WriteLine("Finalized: {0}", oBlockDevice.IsFinalized);
				Console.WriteLine("Temporary Disc ID: {0}", oBlockDevice.TempDiscID);
				Console.WriteLine();

				// Close block device
				oBlockDevice.Close();
			}

			oBlockDevice.Dispose();
		}


		[STAThread]
		static int Main(string[] args)
		{
			int iParseResult = ParseCommandLine(args);
			
			if (0 != iParseResult)
				return iParseResult;


			//////////////////////////////////////////////////////////////////////////////////////////
			// 1) Create an engine object

			// Create an instance of IEngine. That is the main interface that can be used to enumerate 
			// all devices in the system
			Library.Initialize();
			Engine oEngine = new Engine();

			///////////////////////////////////////////////////////////////////////////////////////////
			// 2) Initialize the engine object

			// Try to initialize the engine
            oEngine.Initialize();

			Library.EnableTraceLog(null, true);

			///////////////////////////////////////////////////////////////////////////////////////////
			// 3) Select a device (CD/DVD-RW drive)
	
			// The SelectDevice function allows the user to select a device and then 
			// returns an instance of IDevice.
			Device oDevice = SelectDevice(oEngine);
			if (oDevice == null)
			{
				// Something went wrong. Shutdown the engine.
				oEngine.Shutdown();

				// Release the engine instance. That will free any allocated resources
				oEngine.Dispose();

				// We are done.
				Library.Shutdown();
				return -1;
			}

			// Close the device tray and refresh disc information
			if (oDevice.Eject(false))
			{
				// Wait for the device to become ready
				WaitUnitReady(oDevice);

				// Refresh disc information. Need to call this method when media changes
				oDevice.Refresh();
			}

			// Check if disc is present
			if (MediaReady.Present != oDevice.MediaState)
			{
				Console.WriteLine("Please insert a blank disc in the device and try again.");
				oDevice.Dispose();

				oEngine.Shutdown();
				oEngine.Dispose();
				Library.Shutdown();
				return -1;		
			}

			// Do the work now
			switch (m_eBurnOption)
			{
				case BurnOption.Erase:
					Erase(oDevice);
				break;
				case BurnOption.ReadDiscID:
					ReadDiscID(oDevice);
				break;
				default:
				{
					// Check if disc is blank
                    MediaProfile mp = oDevice.MediaProfile;
					if ((MediaProfile.DvdPlusRw != mp) &&
                        (MediaProfile.DvdRam != mp) &&
                        (MediaProfile.BdRe != mp) &&!oDevice.MediaIsBlank)
					{
						Console.WriteLine("Please insert a blank disc in the device and try again.");
						
						oDevice.Dispose();

						oEngine.Shutdown();
						oEngine.Dispose();

						Library.Shutdown();
						return -1;		
					}

					Burn(oDevice);
				}
				break;
			}

            // Dismount the device volume. This forces the operating system to refresh the CD file system.
            oDevice.Dismount();

			// Release IDevice object
			oDevice.Dispose();

			// Shutdown the engine
			oEngine.Shutdown();

			// Release the engine instance
			oEngine.Dispose();

			Library.DisableTraceLog();

			Library.Shutdown();

			return 0;
		}

	}
}
