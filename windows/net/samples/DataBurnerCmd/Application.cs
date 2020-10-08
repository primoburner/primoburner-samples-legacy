using System;
using System.Collections.Generic;
using System.Text;

namespace DataBurnerCmd.NET
{
	class Application
	{
		#region Private members

		private Burner m_Burner;

		#endregion

		#region Constructors

		public Application()
		{
			m_Burner = new Burner();
		}

		#endregion

		#region Public methods

		public void Run(AppFunctionality functionality)
		{
			if (null == functionality)
			{
				return;
			}
			InitializeBurner();
			try
			{
				switch (functionality.AppOption)
				{
				case AppOption.DeviceList:
					{
						ShowDevices(m_Burner);
					}
					break;
				case AppOption.Clean:
					{
						m_Burner.SelectDevice(functionality.DeviceIndex);
						m_Burner.Clean();
					}
					break;
				case AppOption.Image:
					{
						m_Burner.SelectDevice(functionality.DeviceIndex);
						ImageBurnSettings imageSettings = new ImageBurnSettings(functionality.LayoutSrc);
						m_Burner.BurnImage(imageSettings);
					}
					break;
				case AppOption.Packet:
					{
						m_Burner.SelectDevice(functionality.DeviceIndex);
						PacketBurnSettings packetSettings = new PacketBurnSettings(functionality.LayoutSrc, functionality.PacketOption);
						m_Burner.BurnPacket(packetSettings);
					}
					break;
				case AppOption.Write:
					{
						m_Burner.SelectDevice(functionality.DeviceIndex);
						SimpleBurnSettings simpleSettings = new SimpleBurnSettings(functionality.LayoutSrc, functionality.SimpleOption);
						m_Burner.BurnSimple(simpleSettings);
					}
					break;
				}
			}
			catch (BurnerException ex)
			{
				Console.WriteLine(ex);
			}
			m_Burner.Close();
		}

		#endregion

		#region Private methods

		private void ShowDevices(Burner burner)
		{
			IList<DeviceInfo> devices = burner.EnumerateDevices();
			if (0 == devices.Count)
			{
				throw new BurnerException(BurnerErrors.NoDevices, BurnerErrorMessages.NoDevices, ErrorProvider.Burner);
			}
			else
			{
				Console.WriteLine("Available device:");

				// Loop through all the devices and show their name and description
				Console.WriteLine("Index     Description");
				for (int i = 0; i < devices.Count; i++)
				{
					DeviceInfo info = devices[i];
					Console.WriteLine("  {0}.     {1}:", info.Index, info.Title);
				}
			}
		}
		private string GetProviderName(ErrorProvider provider)
		{
			switch (provider)
			{
				case ErrorProvider.Burner:
					return "Burner object";
				case ErrorProvider.System:
					return "Operating System";
				case ErrorProvider.Engine:
					return "PrimoBurner.Engine object";
				case ErrorProvider.DeviceEnum:
					return "PrimoBurner.DeviceEnum object";
				case ErrorProvider.Device:
					return "PrimoBurner.Device object";
				case ErrorProvider.DataDisc:
					return "PrimoBurner.DataDisc object";

			};
			return "Unknown";
		}
		private void InitializeBurner()
		{
			m_Burner.Open();
			// setup the event handlers
			m_Burner.Continue += new BurnerEventProvider.Continue(m_Burner_Continue);
			m_Burner.EraseProgress += new BurnerEventProvider.EraseProgress(m_Burner_EraseProgress);
			m_Burner.FileProgress += new BurnerEventProvider.FileProgress(m_Burner_FileProgress);
			m_Burner.FormatProgress += new BurnerEventProvider.FormatProgress(m_Burner_FormatProgress);
			m_Burner.Progress += new BurnerEventProvider.Progress(m_Burner_Progress);
			m_Burner.Status += new BurnerEventProvider.Status(m_Burner_Status);
		}

		// DataBurnerCmd.NET.Burner event handlers
		private bool m_Burner_Continue()
		{
			// return false to stop the write operation perormed by the Burner object
			return true;
		}
		private void m_Burner_EraseProgress(double percentCompleted)
		{
			Console.WriteLine(" Erase completed at {0:00.00}%", percentCompleted);
		}
		private void m_Burner_FileProgress(int file, string fileName, int percentCompleted)
		{
			Console.WriteLine(" {0}. File: {1} - Percent complete: {2:00.00}%", file, fileName, percentCompleted);
		}
		private void m_Burner_FormatProgress(double percentCompleted)
		{
			Console.WriteLine(" Format completed at {0:00.00}%", percentCompleted);
		}
		private void m_Burner_Progress(long pos, long all)
		{
			Console.WriteLine(" OnProgress: {0:P}  pos={1} all={2}",  (double)pos / all, pos, all);
		}
		private void m_Burner_Status(string message)
		{
			Console.WriteLine(" Status: {0}",message);
		}

		#endregion
	}
}
