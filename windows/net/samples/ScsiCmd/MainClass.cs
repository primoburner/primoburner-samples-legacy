using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;

using PrimoSoftware.Burner;

namespace ScsiCmd.NET
{
	class MainClass
	{
		static bool Inquiry(ScsiInterface scsi)
		{
			// INQUIRY = 0x12
			byte [] cmd = new byte[6];
			cmd[0] = 0x12;	
			cmd[4] = 36;

			byte [] buffer = new byte[36];
	
			bool ret = scsi.SendCommand(cmd, ScsiCommandDirection.Read, buffer, (int)ScsiCommandTimeout.Long);
			if (!ret)
			{
				Console.WriteLine("Inquiry failed.");	
				return false;
			}
	
			// Check if it is a CD/DVD/BD/HD-DVD
			if ((buffer[0] & 0x1F) != 5)
			{
				Console.WriteLine("Not an MMC unit!.");	
				return false;
			}

			string vendorID = Encoding.ASCII.GetString(buffer, 8, 8);
			string productID = Encoding.ASCII.GetString(buffer, 16, 16);
			string productRevisionLevel = Encoding.ASCII.GetString(buffer, 32, 4);

			Console.WriteLine("INQUIRY: [{0,-8}] [{1,-16}][{2,-4}]", vendorID, productID, productRevisionLevel);
			return true;
		}

		static bool TestUnitReady(ScsiInterface scsi) 
		{
			// TestUnitReady = 0x00
			byte [] cmd = new byte[6];

			bool ret = scsi.SendCommand(cmd);
			if (!ret)
			{
				Console.WriteLine("Unit is not ready.");	
		
				ScsiCommandSense sense = scsi.Sense;
				Console.WriteLine("SCSI Sense -> Key: 0x{0:X2} ASC: 0x{1:X2} ASCQ: 0x{2:X2}", sense.Key, sense.ASC, sense.ASCQ);

				return false;
			}
	
			Console.WriteLine("Unit is ready.");	
			return true;
		}

		static bool StartStopUnit(ScsiInterface scsi, bool eject)
		{
			byte [] cmd = new byte[6];

			// START/STOP UNIT 0x1B;
			cmd[0] = 0x1B; 
			cmd[4] = (byte)(eject ? 0x02 : 0x03); // LoUnlo = 1, Start = 0 : LoUnlo = 1, Start = 1

			bool ret = scsi.SendCommand(cmd);
			if (!ret)
			{
				Console.WriteLine("StartStopUnit failed.");	

				ScsiCommandSense sense = scsi.Sense;
				Console.WriteLine("SCSI Sense -> Key: 0x{0:X2} ASC: 0x{1:X2} ASCQ: 0x{2:X2}", sense.Key, sense.ASC, sense.ASCQ);

				return false;
			}
	
			Console.WriteLine("StartStopUnit success.");	
			return true;
		}

		[STAThread]
		static void Main(string[] args)
		{
			// Initialize the hpCDE library.
			Library.Initialize();

            Library.EnableTraceLog(null, true);

			//////////////////////////////////////////////////////////////////////////////////////////
			// 1) Create an engine object

			// Create an instance of Engine. That is the main iterface that can be used to enumerate 
			// all devices in the system
			Engine eng = new Engine();

			///////////////////////////////////////////////////////////////////////////////////////////
			// 2) Inititialize the engine object

			// Try to initialize the engine
            eng.Initialize();

			///////////////////////////////////////////////////////////////////////////////////////////
			// 3) Select a device (CD/DVD-RW drive)
	
			// The SelectDevice function allows the user to select a device and then 
			// returns an instance of IDevice.
			Device oDevice = SelectDevice(eng);

			if (null == oDevice)
			{
				// Something went wrong. Shutdown the engine.
				eng.Shutdown();

				// Release the engine instance. That will free any allocated resources
				eng.Dispose();

				// We are done.
				return;
			}

			///////////////////////////////////////////////////////////////////////////////////////////
			// 4) Create IScsiInterface instance and execute some commands.
	
			ScsiInterface scsi = oDevice.GetScsiInterface();

			// SCSI inquiry
			Inquiry(scsi);

			// Open the device tray 
			StartStopUnit(scsi, true);

			// See if the unit is ready. If the tray has ejected unit will not be ready and 
			// ScsiInterface will report back SCSI sense data:
			//		Scsi Sense -> Key: 0x02 ASC: 0x3a ASCQ: 0x00 - MEDIUM NOT PRESENT Or
			//		Scsi Sense -> Key: 0x02 ASC: 0x3a ASCQ: 0x02 - MEDIUM NOT PRESENT - TRAY OPEN 
			TestUnitReady(scsi);

			Console.WriteLine("Please load disc and press enter.");
			Console.ReadLine();

			// Close the device tray 
			StartStopUnit(scsi, false);

			// See if the unit is ready
			TestUnitReady(scsi);

			Console.WriteLine("Press enter.");
			Console.ReadLine();

			// Dispose ScsiInterface object
			scsi.Dispose();

			// Dispose Device object
			oDevice.Dispose();

			// Shut down the engine
			eng.Shutdown();

			// Dispose the engine
			eng.Dispose();

			Library.DisableTraceLog();

			Library.Shutdown();
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

				Console.Write("Enter drive letter and press enter: ");
				ch = Console.Read(); Console.ReadLine();


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
	}
}
