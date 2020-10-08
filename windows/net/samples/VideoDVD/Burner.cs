using System;
using System.IO;
using System.Collections;

using PrimoSoftware.Burner;
using System.Collections.Generic;

namespace VideoDVD
{
    public class BurnerCallback
    {
        public delegate void Status(string message);
        public delegate void ImageProgress(long pos, long all);
        public delegate bool Continue();
    }

    public class ProgressInfo
    {
        public string Message = "";
        public int Percent = 0;
        public int UsedCachePercent = 0;
        public int ActualWriteSpeed = 0;
    }

    // Burn Settings
    public class BurnSettings
    {
        public string SourceFolder;
        public string VolumeLabel;
    };

    /// <summary>
    /// Container for device information
    /// </summary>
	public struct DeviceInfo
	{
        /// <summary>
        /// Device index
        /// </summary>
		public int Index;

        /// <summary>
        /// Device description
        /// </summary>
		public string Title;

        /// <summary>
        /// Returns string representation of this object
        /// </summary>
		public override string ToString() 
		{ 
			return Title; 
		}
	};


	public class Burner
	{
		public event BurnerCallback.Status Status;
		public event BurnerCallback.ImageProgress ImageProgress;
		public event BurnerCallback.Continue Continue;
		
		public bool IsOpen 
		{ 
			get 
			{ 
					return m_isOpen; 
			}	
		}

        private void CheckDevice()
        {
            if (null == m_device)
                throw new Exception(BurnerErrors.NO_DEVICE_TEXT);
        }


		public int DeviceCacheSize
		{
			get
			{
                CheckDevice();
				return m_device.InternalCacheCapacity;
			}
		}

		public int DeviceCacheUsedSpace
		{
			get
			{
                CheckDevice();
				return m_device.InternalCacheUsedSpace;
			}
		}

		public int WriteTransferKB
		{
			get
            {
                CheckDevice();
                return m_device.WriteTransferRate;
			}
		}

		public long MediaFreeSpace
		{
			get
			{
                CheckDevice();
				return m_device.MediaFreeSpace;
			}
		}

		public PrimoSoftware.Burner.MediaProfile MediaProfile
		{
			get
			{
                CheckDevice();
				return m_device.MediaProfile;
			}
		}

		public string MediaProfileString
		{
			get
			{
				PrimoSoftware.Burner.MediaProfile profile = this.MediaProfile;
				switch(profile)
				{
					case MediaProfile.CdRom:
						return "CD-ROM";

					case MediaProfile.CdR:
						return "CD-R";

					case MediaProfile.CdRw:
						return "CD-RW";

					case MediaProfile.DvdRom:
						return "DVD-ROM";

					case MediaProfile.DvdMinusRSeq:
						return "DVD-R Sequential Recording.";

					case MediaProfile.DvdMinusRDLSeq:
						return "DVD-R DL 8.54GB for Sequential Recording.";

					case MediaProfile.DvdMinusRDLJump:
						return "DVD-R DL 8.54GB for Layer Jump Recording.";

					case MediaProfile.DvdRam:
						return "DVD-RAM";

					case MediaProfile.DvdMinusRwRo:
						return "DVD-RW Restricted Overwrite.";

					case MediaProfile.DvdMinusRwSeq:
						return "DVD-RW Sequential Recording.";

					case MediaProfile.DvdPlusRw:
					{
						BgFormatStatus fmt = m_device.BgFormatStatus;
						switch(fmt)
						{
							case BgFormatStatus.NotFormatted:
								return "DVD+RW. Not formatted.";
							case BgFormatStatus.Partial:
								return "DVD+RW. Partially formatted.";
							case BgFormatStatus.Pending:
								return "DVD+RW. Background formatting is pending ...";
							case BgFormatStatus.Completed:
								return "DVD+RW. Formatted.";
						}
						return "DVD+RW.";
					}

					case MediaProfile.DvdPlusR:
						return "DVD+R";

					case MediaProfile.DvdPlusRDL:
						return "DVD+R DL 8.5GB";

					default:
						return "Unknown Profile.";
				}
			}
		}


		#region Public Methods
		public void Open()
		{	
			if (m_isOpen)
				return;

			// Enable trace log
			Library.EnableTraceLog(null, true);

			m_engine = new Engine();
			if (!m_engine.Initialize()) 
			{
				m_engine.Dispose();
				m_engine = null;

                throw new BurnerException(m_engine.Error);
			}

			m_isOpen = true;
		}

		public void Close()
		{
			if (null != m_device)
				m_device.Dispose();
			m_device = null;

			if (null != m_engine)
			{
				m_engine.Shutdown();
				m_engine.Dispose();
			}
			m_engine = null;

			Library.DisableTraceLog();

			m_isOpen = false;
		}

		public List<DeviceInfo> EnumerateDevices()
		{
            if (!m_isOpen)
                return null;

			m_devices.Clear();

            using (DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator())
            {
                int devices = enumerator.Count;

                for (int i = 0; i < devices; i++)
                {
                    Device device = enumerator.CreateDevice(i);
                    if (null != device)
                    {
                        DeviceInfo dev = new DeviceInfo();
                        dev.Index = i;
                        dev.Title = GetDeviceTitle(device);

                        m_devices.Add(dev);

                        device.Dispose();
                    }
                }
            }

			return m_devices;
		}

		public bool SelectDevice(int deviceIndex, bool exclusive)
		{
            if (m_device != null)
                return false;

            using (DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator())
            {
                Device dev = enumerator.CreateDevice(deviceIndex, exclusive);
                if (null == dev)
                    throw new BurnerException(enumerator.Error);

                m_device = dev;
                
            }

            return true;
		}

		public void ReleaseDevice()
		{
			if (null != m_device)
				m_device.Dispose();

			m_device = null;
		}


		public long CalculateImageSize(string sourceFolder)
		{
            using (DataDisc data = new DataDisc())
            {
                data.DvdVideo = true;
                SetImageLayoutFromFolder(data, sourceFolder);
                return data.ImageSizeInBytes;
            }
		}

		public void Burn(BurnSettings settings)
        {
            if (null == m_device)
                return;

            using (DataDisc data = new DataDisc())
            {
                // Add event handlers
                data.OnStatus += new EventHandler<DataDiscStatusEventArgs>(DataDisc_OnStatus);
                data.OnProgress += new EventHandler<DataDiscProgressEventArgs>(DataDisc_OnProgress);
                data.OnContinueBurn += new EventHandler<DataDiscContinueEventArgs>(DataDisc_OnContinueBurn);

                // Format if DVD+RW
                FormatMedia(m_device);

                m_device.WriteSpeedKB = m_device.MaxWriteSpeedKB;

                data.Device = m_device;
                data.WriteMethod = WriteMethod.DvdDao;
                data.CloseDisc = true;

                // Set the session start address. This must be done before intializing the file system.
                data.SessionStartAddress = m_device.NewSessionStartAddress;

                data.DvdVideo = true;

                SetVolumeProperties(data, settings.VolumeLabel);

                SetImageLayoutFromFolder(data, settings.SourceFolder);

                // Burn. Dismount after burning is complete
                if (!data.WriteToDisc(true))
                    throw new BurnerException(data.Error);

                m_device.Eject(true);
            }

        }

		#endregion
		
		#region DataDisc Event Handlers
		public void DataDisc_OnStatus(Object sender, DataDiscStatusEventArgs args)
		{
			if (null == Status)
				return;

			Status(GetDataDiscStatusString(args.Status));
		}
		

		public void DataDisc_OnProgress(Object sender, DataDiscProgressEventArgs args)
		{
			if (null == ImageProgress)
				return;

			ImageProgress(args.Position, args.All);
		}

		public void DataDisc_OnContinueBurn(Object sender, DataDiscContinueEventArgs args)
		{
            if (null == Continue)
            {
                args.Continue = true;
            }
			
            args.Continue = Continue();
		}
		#endregion

		#region Private Methods
			private string GetDeviceTitle(Device device)
			{
				return String.Format("({0}:) - {1}", device.DriveLetter, device.Description);
			}


			private void SetImageLayoutFromFolder(DataDisc data, string sourceFolder)
            {
                DataFile dvdFileSystemRoot = null;
                using (PrimoSoftware.Burner.VideoDVD dvd = new PrimoSoftware.Burner.VideoDVD())
                {
                    // pass the source folder, the layout will be created by PrimoBurner
                    if (!dvd.SetImageLayoutFromFolder(sourceFolder))
                    {
                        throw new BurnerException(dvd.Error);
                    }

                    // Get the correct dvd layout 
                    dvdFileSystemRoot = dvd.ImageLayout;
                    if (!data.SetImageLayout(dvdFileSystemRoot))
                    {
                        throw new BurnerException(data.Error);
                    }
                }
            }

			private void SetVolumeProperties(DataDisc data, string volumeLabel)
			{
                DateTime creationTime = DateTime.Now;

                // DVD-Video uses UDF and ISO filesystems
				data.IsoVolumeProps.VolumeLabel = volumeLabel;
                data.IsoVolumeProps.CreationTime = creationTime;
                data.UdfVolumeProps.VolumeLabel = volumeLabel;
                data.UdfVolumeProps.CreationTime = creationTime;
			}

			private string GetDataDiscStatusString(DataDiscStatus status)
			{
				switch(status)
				{
					case DataDiscStatus.BuildingFileSystem:
						return "Building filesystem...";
					case DataDiscStatus.LoadingImageLayout:
						return "Loading image layout...";
					case DataDiscStatus.WritingFileSystem:
						return "Writing filesystem...";
					case DataDiscStatus.WritingImage:
						return "Writing image...";
					case DataDiscStatus.CachingSmallFiles:
						return "Caching small files...";
					case DataDiscStatus.CachingNetworkFiles:
						return "Caching network files...";
					case DataDiscStatus.CachingCDRomFiles:
						return "Caching CDROM files...";
					case DataDiscStatus.Initializing:
						return "Initializing and writing lead-in...";
					case DataDiscStatus.Writing:
						return "Writing...";
					case DataDiscStatus.WritingLeadOut:
						return "Writing lead-out and flushing cache...";
				}

				return "Unknown status...";
			}

			void FormatMedia(Device dev)
			{
				switch(dev.MediaProfile)
				{
					// DVD+RW (needs to be formatted before the disc can be used)
					case MediaProfile.DvdPlusRw:
					{
						if (null != Status)
							Status("Formatting...");

						switch(dev.BgFormatStatus)
						{
							case BgFormatStatus.NotFormatted:
								dev.Format(FormatType.DvdPlusRwFull);
							break;

							case BgFormatStatus.Partial:
								dev.Format(FormatType.DvdPlusRwRestart);
							break;
						}
					}
					break;
				}
			}

		#endregion
	

			private bool m_isOpen = false;
            private List<DeviceInfo> m_devices = new List<DeviceInfo>();

			private Engine m_engine = null;
			private Device m_device = null;
	}
}
