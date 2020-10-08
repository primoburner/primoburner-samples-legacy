using System;
using System.Collections.Generic;
using System.Text;

namespace DataReaderCmd.NET
{
	class Application
	{
		#region Private members

		private Reader m_Reader;

		#endregion

		#region Constructors

		public Application()
		{
			m_Reader = new Reader();
		}

		#endregion

		#region Public methods

		public void Run(AppFunctionality functionality)
		{
			if (null != functionality)
			{
				InitializeReader();
				try
				{
					if (null != functionality)
					{
						switch (functionality.AppOption)
						{
							case AppOption.DeviceList:
								{
									ShowDevices(m_Reader);
								}
								break;
							case AppOption.DeviceTrackList:
								{
									ShowDeviceTracks(m_Reader, functionality.DeviceIndex);
								}
								break;
							case AppOption.ViewContent:
								{
									ShowLayoutContent(m_Reader, functionality);
								}
								break;
							case AppOption.ReadContent:
								{
									ReadLayoutItem(m_Reader, functionality);
								}
								break;
						}
					}
				}
				catch (ReaderException ex)
				{
					Console.WriteLine(ex);
				}
				m_Reader.Close();
			}

			Console.WriteLine("\n\nPress any key to exit.");
			Console.ReadKey();
		}

		#endregion

		#region Private methods

		private void ShowDevices(Reader reader)
		{
			IList<DeviceInfo> devices = reader.EnumerateDevices();
			if (0 == devices.Count)
			{
				throw new ReaderException(ReaderError.NoDevices, ReaderErrorMessages.NoDevices, ErrorProvider.Reader);
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
		private void ShowDeviceTracks(Reader reader, int deviceIndex)
		{
			List<TrackDetails> tracks = reader.EnumerateTracks(deviceIndex);
			if (0 == tracks.Count)
			{
				throw new ReaderException(ReaderError.NoTracksOnDisc, ReaderErrorMessages.NoTracksOnDisc, ErrorProvider.Reader);
			}
			else
			{
				// Display a list of tracks
				Console.WriteLine("\nFirst track, Last Track: {0}, {1}\n", tracks[0].TrackIndex, tracks[tracks.Count - 1].TrackIndex);
				for (int i = 0; i < tracks.Count; i++)
				{
					Console.WriteLine("\t{0}", tracks[i].DisplayTitle);
				}
			}
		}
		private void ShowLayoutContent(Reader reader, AppFunctionality functionality)
		{
			if (SourceType.DiscTrackLayout == functionality.SourceType)
			{
				reader.PrepareSource(functionality.DeviceIndex, functionality.TrackIndex);
			}
			else if (SourceType.ImageLayout == functionality.SourceType)
			{
				reader.PrepareSource(functionality.ImageSource);
			}
			else
			{
				Console.WriteLine("Unexpected reader option");
				return;
			}
			List<LayoutItem> layoutItems = reader.GetFolderContentFromLayout(functionality.ItemPath);

			Console.WriteLine("Content of {0} :\n", 0 < functionality.ItemPath.Length ? functionality.ItemPath : "<root>");
			for (int i = 0; i < layoutItems.Count; i++)
			{
				LayoutItem item = layoutItems[i];
				DateTime st = item.FileTime.ToLocalTime();
				bool isDirectory = item.IsDirectory;
				Console.WriteLine("{0,2}. Addr: {1,8} Size: {2,8} {3} {4} {5}\n",
					i + 1,
					item.Address,
					item.SizeInBytes, st,
					isDirectory ? "<DIR> " : "<FILE>",
					item.FileName);
			}
		}
		void ReadLayoutItem(Reader reader, AppFunctionality functionality)
		{
			if (SourceType.DiscTrackLayout == functionality.SourceType)
			{
				reader.PrepareSource(functionality.DeviceIndex ,functionality.TrackIndex);
			}
			else if (SourceType.ImageLayout == functionality.SourceType)
			{
				reader.PrepareSource(functionality.ImageSource);
			}
			else if (SourceType.DiscTrackUserData == functionality.SourceType)
			{
				TrackRipSettings settings = new TrackRipSettings(functionality.DeviceIndex, functionality.TrackIndex, functionality.DestinationFolder);
				reader.ReadTrackUserData(settings);
				return;
			}
			else
			{
				Console.WriteLine("Unexpected reader option");
				return;
			}
			reader.ReadFileFromSource(functionality.ItemPath, functionality.DestinationFolder);
		}
		private string GetProviderName(ErrorProvider provider)
		{
			switch (provider)
			{
				case ErrorProvider.Reader:
					return "Reader object";
				case ErrorProvider.System:
					return "Operating System";
				case ErrorProvider.Engine:
					return "PrimoSoftware.Burner.Engine object";
				case ErrorProvider.DeviceEnum:
					return "PrimoSoftware.Burner.DeviceEnum object";
				case ErrorProvider.Device:
					return "PrimoSoftware.Burner.Device object";
				case ErrorProvider.DataDisc:
					return "PrimoSoftware.Burner.DataDisc object";

			};
			return "Unknown";
		}
		private void InitializeReader()
		{
			m_Reader.Open();
			// setup the event handlers
			m_Reader.BlockSizeChange += new ReaderEventProvider.BlockSizeChange(m_Reader_BlockSizeChange);
			m_Reader.ReadProgress += new ReaderEventProvider.ReadProgress(m_Reader_ReadProgress);
			m_Reader.ReaderNotificationMessage += new ReaderEventProvider.ReaderNotificationMessage(m_Reader_ReaderNotificationMessage);
		}

		void m_Reader_BlockSizeChange(int oldBlockSize, int newBlockSize, int startFrame)
		{
			Console.WriteLine("\t\tBlock size changed from {0} to: {1}  bytes at lba: {2}.\n", oldBlockSize, newBlockSize, startFrame);
		}
		void m_Reader_ReadProgress(int startFrame, int numberOfFrames, int frameSize)
		{
			Console.WriteLine("LBA: {0}, Blocks: {1}, Block Size: {2}", startFrame, numberOfFrames, frameSize);
		}
		void m_Reader_ReaderNotificationMessage(string message)
		{
			Console.WriteLine(message);
		}

		#endregion
	}
}
