using System;
//using System.Collections;
using System.Collections.Generic;
using System.Text;

using PrimoSoftware.Burner;
using System.IO;

namespace DataBurnerCmd.NET
{
	struct DeviceInfo
	{
		public int		Index;
		public string	Title;
		public char		DriveLetter;
	};

	class Burner
	{
		#region Private members

		private bool m_IsOpen;
		private Engine m_Engine;
		private DeviceEnumerator m_Enumerator;
		private Device m_Device;
		private DataDisc m_DataDisc;

		#endregion

		#region Public Events

		public event BurnerEventProvider.Status Status;
		public event BurnerEventProvider.Progress Progress;
		public event BurnerEventProvider.FileProgress FileProgress;
		public event BurnerEventProvider.FormatProgress FormatProgress;
		public event BurnerEventProvider.EraseProgress EraseProgress;
		public event BurnerEventProvider.Continue Continue;

		#endregion

		#region Constructors

		public Burner()
		{
			m_Engine = null;
			m_Enumerator = null;
			m_Device = null;
			m_DataDisc = null;

			m_IsOpen = false;
		}

		#endregion

		#region Public methods

		public bool Open()
		{
			Library.Initialize();

            // Set license string
            const string license = @"<primoSoftware></primoSoftware>";
            Library.SetLicense(license);

			if (m_IsOpen)
				return true;

			// Enable trace log
			Library.EnableTraceLog(null, true);

			m_Engine = new Engine();
			if (!m_Engine.Initialize()) 
			{
				throw new BurnerException(m_Engine);
			}

			m_IsOpen = true;
			return true;
		}

		public void Close()
		{
			CleanupDataDisc();
			CleanupDevice();
			CleanupDeviceEnum();
			CleanupEngine();

			Library.DisableTraceLog();

			m_IsOpen = false;
			Library.Shutdown();
		}

		public void SelectDevice(int deviceIndex)
		{
			SelectDevice(deviceIndex, false);
		}

		public void SelectDevice(int deviceIndex, bool exclusive)
		{
			if (!m_IsOpen)
			{
				throw new BurnerException(BurnerErrors.BurnerNotOpen, BurnerErrorMessages.BurnerNotOpen, ErrorProvider.Burner);
			}

			CleanupDeviceEnum();
			CleanupDevice();
			m_Enumerator = m_Engine.CreateDeviceEnumerator();

			m_Device = m_Enumerator.CreateDevice(deviceIndex, exclusive);

			if (null != m_Device)
			{
				// set event handlers
				m_Device.OnErase += m_Device_OnErase;
				m_Device.OnFormat += m_Device_OnFormat;
			}
			else
			{
				throw new BurnerException(m_Enumerator);
			}
		}

		public List<DeviceInfo> EnumerateDevices()
		{
			List<DeviceInfo> deviceVector = new List<DeviceInfo>();
			if (!m_IsOpen)
			{
				throw new BurnerException(BurnerErrors.BurnerNotOpen, BurnerErrorMessages.BurnerNotOpen, ErrorProvider.Burner);
			}

			deviceVector.Clear();

			CleanupDeviceEnum();
			m_Enumerator = m_Engine.CreateDeviceEnumerator();

			int nDevices = m_Enumerator.Count;
			if (0 == nDevices)
			{
				throw new BurnerException(BurnerErrors.NoDevices, BurnerErrorMessages.NoDevices, ErrorProvider.Burner);
			}

			for (int i = 0; i < nDevices; i++)
			{
				Device device = m_Enumerator.CreateDevice(i);
				if (null != device)
				{
					DeviceInfo dev;
					dev.Index = i;
					dev.Title = CreateDeviceTitle(device);
					dev.DriveLetter = device.DriveLetter;

					deviceVector.Add(dev);

					device.Dispose();
				}
				else
				{
					throw new BurnerException(m_Enumerator);
				}
			}

			return deviceVector;
		}

		public void BurnImage(ImageBurnSettings settings)
		{
			if (!m_IsOpen)
			{
				throw new BurnerException(BurnerErrors.BurnerNotOpen, BurnerErrorMessages.BurnerNotOpen, ErrorProvider.Burner);
			}

			if (null != m_Device)
			{
				PrepareMedium();

				SetupDataDisc();

				// Set the write method
				if (m_Device.MediaIsBD)
				{
					m_DataDisc.WriteMethod = WriteMethod.BluRay;
				}
				else if (m_Device.MediaIsDVD)
				{
					// The best for DVD Media is DVD Incremental
					m_DataDisc.WriteMethod = WriteMethod.DvdIncremental;
				}
				else
				{
					// The best for CD Media is Track-At-Once (TAO). 
					m_DataDisc.WriteMethod = WriteMethod.Tao;
				}

				// Perform a real burn
				m_DataDisc.SimulateBurn = false;

				// Burn it
				Stream dataStream = new FileStream(settings.ImageFile, FileMode.Open);
				if (!m_DataDisc.WriteImageToDisc(dataStream))
				{
					throw new BurnerException(m_DataDisc, m_Device);
				}
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		public void BurnPacket(PacketBurnSettings settings)
		{
			if (!m_IsOpen)
			{
				throw new BurnerException(BurnerErrors.BurnerNotOpen, BurnerErrorMessages.BurnerNotOpen, ErrorProvider.Burner);
			}
			if (null != m_Device)
			{
				PrepareMedium();

				// Get next writable address
				int lStartAddress = GetStartAddress(m_Device);
				if (-1 == lStartAddress)
				{
					throw new BurnerException(m_Device);
				}

				// indicates whether we should load layout from the last track or not.
				bool fLoadLastTrack  = PacketBurnOption.Append == settings.Option || PacketBurnOption.Finalize == settings.Option;

				// Create and initialize DataDisc object 
				SetupDataDisc();

				// Set other parameters
				SetPacketParameters(m_DataDisc, settings);

				// Get the last track number from the last session if multi-session option was specified
				int nPrevTrackNumber = fLoadLastTrack ? GetLastTrackNumber(m_Device, false) : 0;
				m_DataDisc.LoadTrackLayout = nPrevTrackNumber;

				// Set write speed
				int maxWriteSpeedKB = m_Device.MaxWriteSpeedKB;
				m_Device.WriteSpeedKB = maxWriteSpeedKB;

				// Set the session start address. Must do this before initializing the directory structure.
				m_DataDisc.SessionStartAddress = lStartAddress;

				// Set image layout
				bool bRes = m_DataDisc.SetImageLayoutFromFolder(settings.FolderSrc);
				if (!bRes) 
				{
					throw new BurnerException(m_DataDisc, m_Device);
				}

				// Burn 
				while (true)
				{
					// Try to write the image
					bRes = m_DataDisc.WriteToDisc();
					if (!bRes)
					{
						// Check if the error is: Cannot load image layout. 
						// If so most likely it is an empty formatted DVD+RW or empty formatted DVD-RW RO with one track. 

                        ErrorInfo error = m_DataDisc.Error;

						if (ErrorFacility.DataDisc == error.Facility && 
                            DataDiscError.CannotLoadImageLayout == (DataDiscError)error.Code)
						{
							// Set to 0 to disable previous data session loading
							m_DataDisc.LoadTrackLayout = 0;

							// try to write it again
							continue;
						}
					}

					break;
				}

				if (!bRes) 
				{
					throw new BurnerException(m_DataDisc, m_Device);
				}
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		public void BurnSimple(SimpleBurnSettings settings)
		{
			if (!m_IsOpen)
			{
				throw new BurnerException(BurnerErrors.BurnerNotOpen, BurnerErrorMessages.BurnerNotOpen, ErrorProvider.Burner);
			}
			if (null != m_Device)
			{
				PrepareMedium();

				// Clean previous recordings
				if (SimpleBurnOption.Merge != settings.Option)
					Clean();

				SetupDataDisc();

				// Set data image volume label
				m_DataDisc.UdfVolumeProps.VolumeLabel = settings.VolumeLabel;
                m_DataDisc.IsoVolumeProps.VolumeLabel = settings.VolumeLabel;
                m_DataDisc.JolietVolumeProps.VolumeLabel = settings.VolumeLabel;

				// Set image type. Must be done before calling SetImageLayoutFromFolder.
				m_DataDisc.ImageType = settings.ImageType;

				// Do not cache the small files. Not needed on a fast computer
				m_DataDisc.CachePolicy.CacheSmallFiles = false;

				// Set the session start address. Must do this before intializing the directory structure.
				m_DataDisc.SessionStartAddress = m_Device.NewSessionStartAddress;

				// Load last complete track multi-session
				if (SimpleBurnOption.Merge == settings.Option)
				{
					int nLastCompleteTrack = GetLastTrackNumber(m_Device, true);
					m_DataDisc.LoadTrackLayout = nLastCompleteTrack;
				}

				// The easiest is to use the SetImageLayoutFromFolder method to specify a folder the content of which should be recorded to the disc.
				if (!m_DataDisc.SetImageLayoutFromFolder(settings.FolderSrc))
				{
					throw new BurnerException(m_DataDisc, m_Device);
				}

				// Set the write method
				if (m_Device.MediaIsBD)
				{
					m_DataDisc.WriteMethod = WriteMethod.BluRay;
				}
				else if (m_Device.MediaIsDVD)
				{
					// The best for DVD Media is DVD Incremental
					m_DataDisc.WriteMethod = WriteMethod.DvdIncremental;
				}
				else
				{
					// The best for CD Media is Track-At-Once (TAO). 
					m_DataDisc.WriteMethod = WriteMethod.Tao;
				}

				// Perform a real burn
				m_DataDisc.SimulateBurn = false;

				// Do not close disc to allow multisession
				m_DataDisc.CloseDisc = false;

				// Set write speed
				int maxWriteSpeedKB = m_Device.MaxWriteSpeedKB;
				m_Device.WriteSpeedKB = maxWriteSpeedKB;

				// Burn it
				if (!m_DataDisc.WriteToDisc())
				{
					throw new BurnerException(m_DataDisc, m_Device);
				}
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		public void Erase()
		{
			if (!m_IsOpen)
			{
				throw new BurnerException(BurnerErrors.BurnerNotOpen, BurnerErrorMessages.BurnerNotOpen, ErrorProvider.Burner);
			}
			if (null != m_Device)
			{
				bool cleanResult = true;
				MediaProfile mp = m_Device.MediaProfile;
				switch (mp)
				{
					// DVD-RW, Sequential Recording (default for new DVD-RW)
					case MediaProfile.DvdMinusRwSeq:
						cleanResult = m_Device.Erase(EraseType.Minimal);
						break;

					case MediaProfile.CdRw:
						cleanResult = m_Device.Erase(EraseType.Minimal);
						break;

				}
				if (!cleanResult)
				{
					throw new BurnerException(m_Device);
				}
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		public void Format()
		{
			if (!m_IsOpen)
			{
				throw new BurnerException(BurnerErrors.BurnerNotOpen, BurnerErrorMessages.BurnerNotOpen, ErrorProvider.Burner);
			}
			if (null != m_Device)
			{
				bool cleanResult = true;
				MediaProfile mp = m_Device.MediaProfile;
				switch (mp)
				{
					// DVD+RW (needs to be formatted before the disc can be used)
					case MediaProfile.DvdPlusRw:
						cleanResult = FormatDVDPlusRW();
						break;

					// DVD-RW, Restricted Overwrite (DVD-RW was formatted initially)
					case MediaProfile.DvdMinusRwRo:
						cleanResult = m_Device.Format(FormatType.DvdMinusRwQuick);
						break;

					// BD-RE
					case MediaProfile.BdRe:
						cleanResult = m_Device.FormatBD(BDFormatType.BdFull, BDFormatSubType.BdReQuickReformat);
						break;

					// Format for Pseudo-Overwrite (POW)
					case MediaProfile.BdRSrm:
						cleanResult = m_Device.FormatBD(BDFormatType.BdFull, BDFormatSubType.BdRSrmPow);
						break;

				}
				if (!cleanResult)
				{
					throw new BurnerException(m_Device);
				}
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		bool FormatDVDPlusRW()
		{
			bool cleanResult = true;
			if (null != m_Device)
			{
				BgFormatStatus fmt = m_Device.BgFormatStatus;
				switch (fmt)
				{
					case BgFormatStatus.NotFormatted:
						cleanResult = m_Device.Format(FormatType.DvdPlusRwFull);
						break;
					case BgFormatStatus.Partial:
						cleanResult = m_Device.Format(FormatType.DvdPlusRwRestart);
						break;
				}
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
			return cleanResult;
		}

		void PrepareMedium()
		{
			if (null != m_Device)
			{
				MediaProfile mp = m_Device.MediaProfile;
				switch(mp)
				{
					// DVD+RW (needs to be formatted before the disc can be used)
					case MediaProfile.DvdPlusRw:
						FormatDVDPlusRW();
						break;
				}
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		public void Clean()
		{
			if (!m_IsOpen)
			{
				throw new BurnerException(BurnerErrors.BurnerNotOpen, BurnerErrorMessages.BurnerNotOpen, ErrorProvider.Burner);
			}
			if (null != m_Device)
			{
				MediaProfile mp = m_Device.MediaProfile;
				switch (mp)
				{
					case MediaProfile.DvdPlusRw:
					case MediaProfile.DvdMinusRwRo:
					case MediaProfile.BdRe:
					case MediaProfile.BdRSrm:
						Format();
						break;

					case MediaProfile.DvdMinusRwSeq:
					case MediaProfile.CdRw:
						Erase();
						break;
				}
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		#endregion

		#region Protected methods

		protected void CleanupEngine()
		{
			if (null != m_Engine)
			{
				m_Engine.Shutdown();
				m_Engine.Dispose();
			}
			m_Engine = null;
		}

		protected void CleanupDeviceEnum()
		{
			if (null != m_Enumerator)
			{
				m_Enumerator.Dispose();
			}
			m_Enumerator = null;
		}

		protected void CleanupDevice()
		{
			if (null != m_Device)
			{
				m_Device.Dispose();
			}
			m_Device = null;
		}

		protected void CleanupDataDisc()
		{
			if (null != m_DataDisc)
			{
				m_DataDisc.Dispose();
			}
			m_DataDisc = null;
		}

		protected string CreateDeviceTitle(Device device)
		{
			return string.Format("({0}:) - {1}", device.DriveLetter, device.Description);
		}

		protected void SetPacketParameters(DataDisc dataDisc, PacketBurnSettings settings)
		{
			dataDisc.ImageType = settings.ImageType;

			dataDisc.UdfVolumeProps.VolumeLabel = settings.VolumeLabel;
            dataDisc.IsoVolumeProps.VolumeLabel = settings.VolumeLabel;
            dataDisc.JolietVolumeProps.VolumeLabel = settings.VolumeLabel;

			dataDisc.SimulateBurn = false;

			// Do not cache the small files. Not needed on a fast computer.
			dataDisc.CachePolicy.CacheSmallFiles = false;

			// Packet mode
			dataDisc.WriteMethod = WriteMethod.Packet;

			// Reserve the path table
            DataWriteStrategy writeStrategy = DataWriteStrategy.None;

			switch (settings.Option)
			{
				case PacketBurnOption.Start:
					writeStrategy = DataWriteStrategy.ReserveFileTableTrack;
					break;
				case PacketBurnOption.Finalize:
					writeStrategy = DataWriteStrategy.WriteFileTableTrack;
					break;
			}
			dataDisc.WriteStrategy = writeStrategy;

			dataDisc.CloseTrack = PacketBurnOption.Finalize == settings.Option;
			dataDisc.CloseSession = PacketBurnOption.Finalize == settings.Option;
			dataDisc.CloseDisc = PacketBurnOption.Finalize == settings.Option;
		}

		protected int GetStartAddress(Device device)
		{
			if (null != device)
			{
				// Get disk information
				DiscInfo di = device.ReadDiscInfo();
				if (null == di)
					return -1;

				// Use NewTrackStartAddress if the last session is open. That will give 
				// us next available address for writing in the open session.
				if (SessionState.Open == di.SessionState)
					return device.NewTrackStartAddress;

				return device.NewSessionStartAddress;
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		protected int GetLastTrackNumber(Device device, bool completeOnly)
		{
			if (null != device)
			{
				// Get the last track number from the last session if multisession option was specified
				int lastTrack = 0;

				// Check for DVD+RW and DVD-RW RO random writable media. 
				MediaProfile mp = device.MediaProfile;
				if (MediaProfile.DvdPlusRw == mp || MediaProfile.DvdMinusRwRo == mp)
				{
					// DVD+RW and DVD-RW RO have only one session with one track
					if (device.MediaFreeSpace > 0)
						lastTrack = 1;
				}
				else
				{
					// All other media is recorded using tracks and sessions and multi-session discs are similar to CD. 
					// Use the ReadDiskInfo method to get the last track number
                    DiscInfo di = device.ReadDiscInfo();
					if (null != di)
					{
						lastTrack = di.LastTrack;

						if (completeOnly)
						{
							// ReadDiskInfo reports the empty space as a track too
							// That's why we need to go back one track to get the last completed track
							if (DiscStatus.Open == di.DiscStatus || DiscStatus.Empty == di.DiscStatus)
								lastTrack--;
						}
					}
				}

				return lastTrack;
			}
			else
			{
				throw new BurnerException(BurnerErrors.DeviceNotSet, BurnerErrorMessages.DeviceNotSet, ErrorProvider.Burner);
			}
		}

		protected string GetDataDiscStatusString(DataDiscStatus status)
		{
			switch(status)
			{
				case DataDiscStatus.BuildingFileSystem:
					return "Building filesystem...";
				
				case DataDiscStatus.WritingFileSystem:
					return "Writing filesystem...";

				case DataDiscStatus.WritingImage:
					return "Writing image...";

				case DataDiscStatus.CachingSmallFiles:
					return "Caching small files ...";

				case DataDiscStatus.CachingNetworkFiles:
					return "Caching network files ...";

				case DataDiscStatus.CachingCDRomFiles:
					return "Caching CD-ROM files ...";

				case DataDiscStatus.Initializing:
					return "Initializing...";

				case DataDiscStatus.Writing:
					return "Writing...";

				case DataDiscStatus.WritingLeadOut:
					return "Flushing device cache and writing lead-out...";

				case DataDiscStatus.LoadingImageLayout:
					return "Loading image layout from last track...";
			}

			return "Unknown status...";
		}
		#endregion

		#region Private methods

		private void SetupDataDisc()
		{
			CleanupDataDisc();

			// Create the DataDisc object 
			m_DataDisc = new DataDisc();

			// Tell DataDisc which device it should use. Here we just pass the device the user selected.
			m_DataDisc.Device = m_Device;

			// Set DataDisc event handlers
			m_DataDisc.OnContinueBurn += m_DataDisc_OnContinueBurn;
			m_DataDisc.OnFileStatus += m_DataDisc_OnFileStatus;
			m_DataDisc.OnProgress += m_DataDisc_OnProgress;
			m_DataDisc.OnStatus += m_DataDisc_OnStatus;
		}

		// DataDisc event handlers
		void m_DataDisc_OnStatus(object sender, DataDiscStatusEventArgs e)
		{
			BurnerEventProvider.Status callback = Status;
			if (null != callback)
			{
				callback(GetDataDiscStatusString(e.Status));
			}
		}

		void m_DataDisc_OnProgress(object sender, DataDiscProgressEventArgs e)
		{
			BurnerEventProvider.Progress callback = Progress;
			if (null != callback)
			{
				callback(e.Position, e.All);
			}
		}

		void m_DataDisc_OnFileStatus(object sender, DataDiscFileStatusEventArgs e)
		{
			BurnerEventProvider.FileProgress callback = FileProgress;
			if (null != callback)
			{
				callback(e.FileNumber, e.FileName, e.PercentWritten);
			}
		}

		void m_DataDisc_OnContinueBurn(object sender, DataDiscContinueEventArgs e)
		{
			BurnerEventProvider.Continue callback = Continue;
			if (null != callback)
			{
				e.Continue = callback();
                return;
			}

			e.Continue = true;
		}

		// Device event handlers
		void m_Device_OnFormat(object sender, DeviceFormatEventArgs e)
		{
			BurnerEventProvider.FormatProgress callback = FormatProgress;
			if (null != callback)
			{
				callback(e.Progress);
			}
		}

		void m_Device_OnErase(object sender, DeviceEraseEventArgs e)
		{
			BurnerEventProvider.EraseProgress callback = EraseProgress;
			if (null != callback)
			{
				callback(e.Progress);
			}
		}

		#endregion
	}
}