using System;
using System.IO;
using System.Collections;

using PrimoSoftware.Burner;
using System.Collections.Generic;

namespace DVDBurner.NET
{
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

    /// <summary>
    /// Container for speed information
    /// </summary>
	public struct SpeedInfo
	{
		public double TransferRateKB;
		public double TransferRate1xKB;
		public override string ToString() 
		{ 
			return string.Format("{0}x", Math.Round((double)TransferRateKB / TransferRate1xKB, 1));
		}
	};

	public class Burner
	{
		#region Public Events
		public event BurnerCallback.Status Status;
		public event BurnerCallback.ImageProgress ImageProgress;
		public event BurnerCallback.FileProgress FileProgress;
		public event BurnerCallback.FormatProgress FormatProgress;
		public event BurnerCallback.EraseProgress EraseProgress;
		public event BurnerCallback.Continue Continue;
		#endregion
		
		#region Public Properties
		public bool IsOpen 
		{ 
			get 
			{ 
					return m_isOpen; 
			}	
		}

		public bool MediaIsBlank
		{
			get
			{
				if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

				return m_device.MediaIsBlank;
			}
		}

		public bool MediaIsFullyFormatted
		{
			get
			{
				if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

				// Get media profile
				MediaProfile mp = m_device.MediaProfile;

				// DVD+RW
				if (MediaProfile.DvdPlusRw == mp)
					return (BgFormatStatus.Completed == m_device.BgFormatStatus);

				// DVD-RW for Restricted Overwrite
				if (MediaProfile.DvdMinusRwRo == mp)
					return (m_device.MediaFreeSpace == m_device.MediaCapacity);

				return false;
			}
		}

		public int DeviceCacheSize
		{
			get
			{
				if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

				return m_device.InternalCacheCapacity;
			}
		}

		public int DeviceCacheUsedSpace
		{
			get
			{
				if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

				return m_device.InternalCacheUsedSpace;
			}
		}

		public int WriteTransferKB
		{
			get
			{
				if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

				return m_device.WriteTransferRate;
			}
		}

		public long MediaFreeSpace
		{
			get
			{
				if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

				return m_device.MediaFreeSpace;
			}
		}

		public int MaxWriteSpeedKB
		{
			get
			{
				if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

				return m_device.MaxWriteSpeedKB;
			}
		}

		public PrimoSoftware.Burner.MediaProfile MediaProfile
		{
			get
			{
				if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

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
						return "CD-ROM. Read only CD.";

					case MediaProfile.CdR:
						return "CD-R. Write once CD.";

					case MediaProfile.CdRw:
						return "CD-RW. Re-writable CD.";

					case MediaProfile.DvdRom:
						return "DVD-ROM. Read only DVD.";

					case MediaProfile.DvdMinusRSeq:
						return "DVD-R Sequential Recording. Write once DVD.";

					case MediaProfile.DvdMinusRDLSeq:
						return "DVD-R DL 8.54GB for Sequential Recording. Write once DVD.";

					case MediaProfile.DvdMinusRDLJump:
						return "DVD-R DL 8.54GB for Layer Jump Recording. Write once DVD.";

					case MediaProfile.DvdRam:
						return "DVD-RAM ReWritable DVD.";

					case MediaProfile.DvdMinusRwRo:
						return "DVD-RW Restricted Overwrite ReWritable. ReWritable DVD using restricted overwrite.";

					case MediaProfile.DvdMinusRwSeq:
						return "DVD-RW Sequential Recording ReWritable. ReWritable DVD using sequential recording.";

					case MediaProfile.DvdPlusRw:
					{
						BgFormatStatus fmt = m_device.BgFormatStatus;
						switch(fmt)
						{
							case BgFormatStatus.NotFormatted:
								return "DVD+RW ReWritable DVD. Not formatted.";
							case BgFormatStatus.Partial:
								return "DVD+RW ReWritable DVD. Partially formatted.";
							case BgFormatStatus.Pending:
								return "DVD+RW ReWritable DVD. Background formatting is pending ...";
							case BgFormatStatus.Completed:
								return "DVD+RW ReWritable DVD. Formatted.";
						}
						return "DVD+RW ReWritable DVD.";
					}

					case MediaProfile.DvdPlusR:
						return "DVD+R. Write once DVD.";

					case MediaProfile.DvdPlusRDL:
						return "DVD+R DL 8.5GB. Write once DVD.";

					default:
						return "Unknown Profile.";
				}
			}
		}

		#endregion

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

                throw new BurnerException(BurnerErrors.ENGINE_INITIALIZATION, BurnerErrors.ENGINE_INITIALIZATION_TEXT);
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

		public DeviceInfo[] EnumerateDevices()
		{
			if (!m_isOpen)
                throw new BurnerException(BurnerErrors.BURNER_NOT_OPEN, BurnerErrors.BURNER_NOT_OPEN_TEXT);

			m_deviceArray.Clear();

			DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator();
				int devices = enumerator.Count;
				if (0 == devices) 
				{
					enumerator.Dispose();
                    throw new BurnerException(BurnerErrors.NO_DEVICES, BurnerErrors.NO_DEVICES_TEXT);
				}

				for (int i = 0; i < devices; i++) 
				{
					Device device = enumerator.CreateDevice(i);
					if (null != device)
					{
						DeviceInfo dev = new DeviceInfo();
						dev.Index = i;
						dev.Title = GetDeviceTitle(device);

						m_deviceArray.Add(dev);
						device.Dispose();
					}
				}
			enumerator.Dispose();

			return (DeviceInfo[])m_deviceArray.ToArray(typeof(DeviceInfo));
		}

		public void SelectDevice(int deviceIndex, bool exclusive)
		{
			if (null != m_device)
                throw new BurnerException(BurnerErrors.DEVICE_ALREADY_SELECTED, BurnerErrors.DEVICE_ALREADY_SELECTED_TEXT);

			DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator();
			Device dev = enumerator.CreateDevice(deviceIndex, exclusive);
			if (null == dev)
			{
				enumerator.Dispose();
                throw new BurnerException(BurnerErrors.INVALID_DEVICE_INDEX, BurnerErrors.INVALID_DEVICE_INDEX_TEXT);
			}

			m_device = dev;
			enumerator.Dispose();
		}

		public void ReleaseDevice()
		{
			if (null != m_device)
				m_device.Dispose();

			m_device = null;
		}

		public SpeedInfo [] EnumerateWriteSpeeds()
		{
			if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

			m_speedArray.Clear();

            IList<SpeedDescriptor> speeds = m_device.GetWriteSpeeds();
            for (int i = 0; i < speeds.Count; i++)
            {
                SpeedDescriptor speed = speeds[i];
                if (null != speed)
                {
                    SpeedInfo speedInfo = new SpeedInfo();
                    speedInfo.TransferRateKB = speed.TransferRateKB;
                    speedInfo.TransferRate1xKB = m_device.MediaIsDVD ? Speed1xKB.DVD : Speed1xKB.CD;

                    m_speedArray.Add(speedInfo);
                }
            }

			return (SpeedInfo[])m_speedArray.ToArray(typeof(SpeedInfo));
		}

		public long CalculateImageSize(string sourceFolder, PrimoSoftware.Burner.ImageType imageType)
		{
			DataDisc data = new DataDisc();
			data.ImageType = imageType;

			long imageSize = 0;
			try
			{
				SetImageLayoutFromFolder(data, false, sourceFolder); 
				imageSize = data.ImageSizeInBytes;
			}
			catch
			{
				data.Dispose();
				throw;
			}

			data.Dispose();
			return imageSize;
		}

		public void Eject()
		{
			if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

			m_device.Eject(true);
		}

		public void CloseTray()
		{
			if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

			m_device.Eject(false);
		}

		public void CreateImage(CreateImageSettings settings)
		{
			DataDisc data = new DataDisc();

			// Add event handlers
            data.OnStatus += new EventHandler<DataDiscStatusEventArgs>(DataDisc_OnStatus);
            data.OnFileStatus += new EventHandler<DataDiscFileStatusEventArgs>(DataDisc_OnFileStatus);
            data.OnProgress += new EventHandler<DataDiscProgressEventArgs>(DataDisc_OnProgress);
            data.OnContinueBurn += new EventHandler<DataDiscContinueEventArgs>(DataDisc_OnContinueBurn);

			try
			{
				SetVolumeProperties(data, settings.VolumeLabel);
				data.ImageType = settings.ImageType;

				// Create image file system
				SetImageLayoutFromFolder(data, settings.VideoDVD, settings.SourceFolder);

				// Create the image file
				if (!data.CreateImageFile(settings.ImageFile)) 
					throw new BurnerException(data.Error);

			}
			finally
			{
				data.Dispose();
			}
		}

		public void BurnImage(BurnImageSettings settings) 
		{
			if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

			DataDisc data = new DataDisc();

			// Add event handlers
            data.OnStatus += new EventHandler<DataDiscStatusEventArgs>(DataDisc_OnStatus);
            data.OnFileStatus += new EventHandler<DataDiscFileStatusEventArgs>(DataDisc_OnFileStatus);
            data.OnProgress += new EventHandler<DataDiscProgressEventArgs>(DataDisc_OnProgress);
            data.OnContinueBurn += new EventHandler<DataDiscContinueEventArgs>(DataDisc_OnContinueBurn);

			try
			{
				m_device.WriteSpeedKB = settings.WriteSpeedKB;
				FormatMedia(m_device);

				data.Device = m_device;
				data.SimulateBurn = settings.Simulate;
				data.WriteMethod = settings.WriteMethod;
				data.CloseDisc = settings.CloseDisc;

				// Write the image to the DVD
				if (!data.WriteImageToDisc(settings.ImageFile))
					throw new BurnerException(data.Error);

				if (settings.Eject)
					m_device.Eject(true);
			}
			finally
			{
				data.Dispose();
			}
		}

		public void Burn(BurnSettings settings) 
		{
			if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

			DataDisc data = new DataDisc();

			// Add event handlers
            data.OnStatus += new EventHandler<DataDiscStatusEventArgs>(DataDisc_OnStatus);
            data.OnFileStatus += new EventHandler<DataDiscFileStatusEventArgs>(DataDisc_OnFileStatus);
            data.OnProgress += new EventHandler<DataDiscProgressEventArgs>(DataDisc_OnProgress);
            data.OnContinueBurn += new EventHandler<DataDiscContinueEventArgs>(DataDisc_OnContinueBurn);

			try
			{
				FormatMedia(m_device);

				m_device.WriteSpeedKB = settings.WriteSpeedKB;

				data.Device = m_device;
				data.SimulateBurn = settings.Simulate;
				data.WriteMethod = settings.WriteMethod;
				data.CloseDisc = settings.CloseDisc;

				// Set the session start address. This must be done before intializing the file system.
				data.SessionStartAddress = m_device.NewSessionStartAddress;

				// Multi-session. Find the last complete track number from the last session.
				if (settings.LoadLastTrack)
				{
					int nPrevTrackNumber = GetLastCompleteTrack(m_device);
					if (nPrevTrackNumber > 0)
					{
						// Set the track number here. DataDisc will load it later.
						data.LoadTrackLayout = nPrevTrackNumber;
						data.DataOverwrite = DataOverwriteFlags.Overwrite;
					}
				}

				// Set burning parameters
				data.ImageType = settings.ImageType;

				SetVolumeProperties(data, settings.VolumeLabel);

				// Set image layout
				SetImageLayoutFromFolder(data, settings.VideoDVD, settings.SourceFolder);

				// Burn 
				bool bRes = false;
				while (true)
				{
					// Try to write the image
					bRes = data.WriteToDisc(true);
					if (!bRes)
					{
						// When error is: Cannot load image layout, most likely it is an empty formatted DVD+RW 
						// or empty formatted DVD-RW RO with one track.
						if (data.Error.Facility == ErrorFacility.DataDisc &&
                            data.Error.Code == (int)DataDiscError.CannotLoadImageLayout)
						{
							// Set to 0 to disable loading filesystem from previous track
							data.LoadTrackLayout = 0;

							// retry writing
							continue;
						}
					}

					break;
				}

				// Check result and show error message
				if (!bRes)
					throw new BurnerException(data.Error);

				if (settings.Eject)
					m_device.Eject(true);

			}
			finally
			{
				data.Dispose();
			}
		}

		public void Erase(EraseSettings settings) 
		{
			if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

			MediaProfile mp = m_device.MediaProfile;

			if (MediaProfile.DvdMinusRwSeq != mp && MediaProfile.DvdMinusRwRo != mp && MediaProfile.CdRw != mp)
                throw new BurnerException(BurnerErrors.ERASE_NOT_SUPPORTED, BurnerErrors.ERASE_NOT_SUPPORTED_TEXT);
		
			if (m_device.MediaIsBlank && !settings.Force)
				return;

			m_device.OnErase += Device_Erase;

			bool bRes = m_device.Erase(settings.Quick ? EraseType.Minimal : EraseType.Disc);

			m_device.OnErase -= Device_Erase;

			if (!bRes)
				throw new BurnerException(m_device.Error);

			// Refresh to reload disc information
			m_device.Refresh();
		}

		public void Format(FormatSettings settings) 
		{
			if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE, BurnerErrors.NO_DEVICE_TEXT);

			MediaProfile mp = m_device.MediaProfile;

			if (MediaProfile.DvdMinusRwSeq != mp && MediaProfile.DvdMinusRwRo != mp && MediaProfile.DvdPlusRw != mp)
                throw new BurnerException(BurnerErrors.FORMAT_NOT_SUPPORTED, BurnerErrors.FORMAT_NOT_SUPPORTED_TEXT);

			m_device.OnFormat += Device_Format;

			bool bRes = true;
			switch(mp)
			{
				case MediaProfile.DvdMinusRwRo:
					bRes = m_device.Format(settings.Quick ? FormatType.DvdMinusRwQuick : FormatType.DvdMinusRwFull);
				break;
				case MediaProfile.DvdMinusRwSeq:
					bRes = m_device.Format(settings.Quick ? FormatType.DvdMinusRwQuick : FormatType.DvdMinusRwFull);
				break;

				case MediaProfile.DvdPlusRw:
				{
					BgFormatStatus fmt = m_device.BgFormatStatus;
					switch(fmt)
					{
						case BgFormatStatus.Completed:
							if (settings.Force)
								bRes = m_device.Format(FormatType.DvdPlusRwFull, 0, !settings.Quick);
						break;
						case BgFormatStatus.NotFormatted:
							bRes = m_device.Format(FormatType.DvdPlusRwFull, 0, !settings.Quick);
						break;
						case BgFormatStatus.Partial:
							bRes = m_device.Format(FormatType.DvdPlusRwRestart, 0, !settings.Quick);
						break;
					}
				}
				break;
			}

			m_device.OnFormat -= Device_Format;

			if (!bRes)
				throw new BurnerException(m_device.Error);

			// Refresh to reload disc information
			m_device.Refresh();
		}
		#endregion
		
		#region Device Event Handlers

		public void Device_Format(Object sender, DeviceFormatEventArgs args)
		{
			if (null != FormatProgress)
				FormatProgress(args.Progress);
		}

		public void Device_Erase(Object sender, DeviceEraseEventArgs args)
		{
			if (null != EraseProgress)
				EraseProgress(args.Progress);
		}

		#endregion

		#region DataDisc Event Handlers
		public void DataDisc_OnStatus(Object sender, DataDiscStatusEventArgs args)
		{
			if (null == Status)
				return;

			Status(GetDataDiscStatusString(args.Status));
		}

		public void DataDisc_OnFileStatus(Object sender, DataDiscFileStatusEventArgs args)
		{
			if (null == FileProgress)
				return;

			FileProgress(args.FileNumber, args.FileName, args.PercentWritten);
		}

		public void DataDisc_OnProgress(Object sender, DataDiscProgressEventArgs args)
		{
			if (null == ImageProgress)
				return;

			ImageProgress(args.Position, args.All);
		}

        public void DataDisc_OnContinueBurn(object sender, DataDiscContinueEventArgs e)
		{
			if (null == Continue)
				return;

            e.Continue = Continue();
		}
		#endregion

		#region Private Methods
			private string GetDeviceTitle(Device device)
			{
				return String.Format("({0}:) - {1}", device.DriveLetter, device.Description);
			}


			private void CreateFileTree(DataFile currentDirectory, string currentPath)
			{
                const ImageType allImageTypes = (ImageType.Iso9660 | ImageType.Joliet | ImageType.Udf);

				ArrayList filesAndDirs = new ArrayList();
				filesAndDirs.AddRange(Directory.GetFiles(currentPath, "*"));
				filesAndDirs.AddRange(Directory.GetDirectories(currentPath, "*"));

				foreach (string path in filesAndDirs)
				{
					if (Directory.Exists(path))
					{
						// Get directory information
						DirectoryInfo di = new DirectoryInfo(path);

						// Create a new directory
						DataFile newDirectory = new DataFile(); 

							newDirectory.IsDirectory = true;
							newDirectory.LongFilename = di.Name;
							
							newDirectory.FilePath = di.Name;
							newDirectory.FileTime = di.CreationTime;

                            if ((di.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                newDirectory.HiddenMask = (int)allImageTypes;
				
							// Call CreateFileTree recursively to process all the files from the new directory
							CreateFileTree(newDirectory, di.FullName);

							// Add the new directory to the image tree
							currentDirectory.Children.Add(newDirectory);
					} 
					else
					{
						// Get file information
						FileInfo fi = new FileInfo(path);
						// Create a new file
						DataFile newFile = new DataFile(); 
							newFile.IsDirectory = false;
							newFile.LongFilename = fi.Name;
							
							newFile.FilePath = fi.FullName;
							newFile.FileTime = fi.CreationTime;

                            if ((fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                newFile.HiddenMask = (int)allImageTypes;

							// Add the new file to the image tree
                            currentDirectory.Children.Add(newFile);
					}
				}
			}

			private void SetImageLayoutFromFolder(DataDisc data, bool isVideoDVD, string sourceFolder)
            {
                if (isVideoDVD)
                {
                    data.DvdVideo = true;
                    data.CloseDisc = true;
                }

                /* manually create layout from folder
                 
                DataFile fileSystemRoot = new DataFile();

                fileSystemRoot.IsDirectory = true;
                fileSystemRoot.LongFilename = "\\";
                fileSystemRoot.FilePath = "\\";

                CreateFileTree(fileSystemRoot, sourceFolder);
                 */

                // Set image layout
                if (isVideoDVD)
                {
                    DataFile dvdFileSystemRoot = null;
                    VideoDVD dvd = new VideoDVD();
                    try
                    {
                        // pass the manually created layout to PrimoBurner
                        //if (!dvd.SetImageLayout(fileSystemRoot)) 

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
                    finally
                    {
                        dvd.Dispose();
                    }
                }
                else
                {
                    // pass the manually created layout to PrimoBurner
                    //if (!data.SetImageLayout(fileSystemRoot))

                    // pass the source folder, the layout will be created by PrimoBurner
                    if (!data.SetImageLayoutFromFolder(sourceFolder))
                        throw new BurnerException(data.Error);
                }

            }

			private void SetVolumeProperties(DataDisc data, string volumeLabel)
			{
                DateTime creationTime = DateTime.Now;

				// Sample settings. Replace with your own data or leave empty
				data.IsoVolumeProps.VolumeLabel = volumeLabel;
                data.IsoVolumeProps.VolumeSet = "SET";
                data.IsoVolumeProps.SystemID = "WINDOWS";
                data.IsoVolumeProps.Publisher = "PUBLISHER";
                data.IsoVolumeProps.DataPreparer = "PREPARER";
                data.IsoVolumeProps.Application = "DVDBURNER";
                data.IsoVolumeProps.CopyrightFile = "COPYRIGHT.TXT";
                data.IsoVolumeProps.AbstractFile = "ABSTRACT.TXT";
                data.IsoVolumeProps.BibliographicFile = "BIBLIO.TXT";
                data.IsoVolumeProps.CreationTime = creationTime;

                data.JolietVolumeProps.VolumeLabel = volumeLabel;
                data.JolietVolumeProps.VolumeSet = "SET";
                data.JolietVolumeProps.SystemID = "WINDOWS";
                data.JolietVolumeProps.Publisher = "PUBLISHER";
                data.JolietVolumeProps.DataPreparer = "PREPARER";
                data.JolietVolumeProps.Application = "DVDBURNER";
                data.JolietVolumeProps.CopyrightFile = "COPYRIGHT.TXT";
                data.JolietVolumeProps.AbstractFile = "ABSTRACT.TXT";
                data.JolietVolumeProps.BibliographicFile = "BIBLIO.TXT";
                data.JolietVolumeProps.CreationTime = creationTime;

                data.UdfVolumeProps.VolumeLabel = volumeLabel;
                data.UdfVolumeProps.VolumeSet = "SET";
                data.UdfVolumeProps.CopyrightFile = "COPYRIGHT.TXT";
                data.UdfVolumeProps.AbstractFile = "ABSTRACT.TXT";
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

			bool FormatMedia(Device dev)
			{
				dev.OnErase += Device_Erase;
				dev.OnFormat += Device_Format;

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

				dev.OnErase -= Device_Erase;
				dev.OnFormat -= Device_Format;

				// Must be DVD-R, DVD+R
				return true;
			}

			int GetLastCompleteTrack(Device dev)
			{
				// Get the last track number from the last session if multisession option was specified
				int lastTrack = 0;

				// Check for DVD+RW and DVD-RW RO random writable media. 
				MediaProfile mp = dev.MediaProfile;
				if (MediaProfile.DvdPlusRw == mp || MediaProfile.DvdMinusRwRo == mp)
				{
					// DVD+RW and DVD-RW RO has only one session with one track
					if (dev.MediaFreeSpace > 0)
						lastTrack = 1;	
				}		
				else
				{
					// All other media is recorded using tracks and sessions and multi-session is no different than with the CD. 
					// Use the ReadDiskInfo method to get the last track number
                    DiscInfo di = dev.ReadDiscInfo();
					if (di != null)
					{
						lastTrack = di.LastTrack;
				
						// ReadDiskInfo reports the empty space as a last track .
						if (DiscStatus.Open == di.DiscStatus || DiscStatus.Empty == di.DiscStatus)
							lastTrack--;
					}
				}

				return lastTrack;
			}

		#endregion
	
		#region Private Property Members
			private bool m_isOpen = false;
			private ArrayList m_deviceArray = new ArrayList();
			private ArrayList m_speedArray = new ArrayList();
		#endregion
	 
		#region Private Members
			private Engine m_engine = null;
			private Device m_device = null;
		#endregion

	}
}
