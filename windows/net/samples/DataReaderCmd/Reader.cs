using System;
using System.Collections.Generic;
using System.IO;

using PrimoSoftware.Burner;

namespace DataReaderCmd.NET
{
	class DeviceInfo
	{
		private int		m_Index;
		private string	m_Title;
		private char	m_DriveLetter;

		public int Index
		{
			get { return m_Index; }
		}
		public string Title
		{
			get { return m_Title; }
		}
		public char DriveLetter
		{
			get { return m_DriveLetter; }
		}

		public DeviceInfo(int index, string title, char driveLetter)
		{
			m_Index = index;
			m_Title = title;
			m_DriveLetter = driveLetter;
		}
	};

	class TrackDetails
	{
		private int m_TrackIndex;
		private string m_DisplayTitle;

		public int TrackIndex
		{
			get { return m_TrackIndex; }
		}
		public string DisplayTitle
		{
			get { return m_DisplayTitle; }
		}

		public TrackDetails(int trackIndex, string displayTitle)
		{
			m_TrackIndex = trackIndex;
			m_DisplayTitle = displayTitle;
		}
	};

	class TrackSegment
	{
		private int m_StartAddress;
		private int m_RecordedSize;
		private int m_EndAddress;

		public int StartAddress
		{
			get { return m_StartAddress; }
		}
		public int RecordedSize
		{
			get { return m_RecordedSize; }
		}
		public int EndAddress
		{
			get { return m_EndAddress; }
		}

		public TrackSegment(int startAddress, int recordedSize)
		{
			m_StartAddress = startAddress;
			m_RecordedSize = recordedSize;
			m_EndAddress = startAddress + recordedSize - 1;
		}
	};

	class FileSegment
	{
		int m_StartAddress;
		int m_EndAddress;
		Int64 m_FileSize;

		public int StartAddress
		{
			get { return m_StartAddress; }
		}
		public int EndAddress
		{
			get { return m_EndAddress; }
		}
		public Int64 FileSize
		{
			get { return m_FileSize; }
		}

		public FileSegment(int address, Int64 fileSize)
		{
			m_StartAddress = address;
			m_FileSize = fileSize;
			m_EndAddress = address + (int)(fileSize / (int)BlockSize.CdRom);
			if (0 < fileSize % (int)BlockSize.CdRom)
				m_EndAddress += 1;
		}
	};

	struct LayoutItem
	{
		public DateTime FileTime;
		public string FileName;
		public bool IsDirectory;
		public int Address;
		public long SizeInBytes;
	};

	class Reader
	{
		#region Private members

		bool m_isOpen;
		bool m_UseImage;
		int m_TrackIndex;

		string m_ImageFile;

		Engine m_Engine;
		DeviceEnumerator m_Enumerator;
		Device m_Device;
		DataDisc m_DataDisc;

		#endregion

		#region Public Events
		public event ReaderEventProvider.BlockSizeChange BlockSizeChange;
		public event ReaderEventProvider.ReaderNotificationMessage ReaderNotificationMessage;
		public event ReaderEventProvider.ReadProgress ReadProgress;
		#endregion

		#region Constructors
		public Reader()
		{
			m_Engine = null;
			m_Enumerator = null;
			m_Device = null;
			m_DataDisc = null;

			m_isOpen = false;
			m_UseImage = false;
			m_TrackIndex = 0;
		}
		#endregion

		#region Public methods
		public bool Open()
		{
			Library.Initialize();

            // Set license string
            const string license = @"<primoSoftware></primoSoftware>";
            Library.SetLicense(license);

			if (m_isOpen)
				return true;

			// Enable trace log
			Library.EnableTraceLog(null, true);

			m_Engine = new Engine();
			if (!m_Engine.Initialize()) 
			{
				throw new ReaderException(m_Engine);
			}

			m_isOpen = true;
			return true;
		}

		public void Close()
		{
			CleanupDataDisc();
			CleanupDevice();
			CleanupDeviceEnum();
			CleanupEngine();

			Library.DisableTraceLog();

			m_isOpen = false;
			Library.Shutdown();
		}

		public List<DeviceInfo> EnumerateDevices()
		{
			List<DeviceInfo> devices = new List<DeviceInfo>();
			if (!m_isOpen)
			{
				throw new ReaderException(ReaderError.ReaderNotOpen, ReaderErrorMessages.ReaderNotOpen, ErrorProvider.Reader);
			}

			CleanupDeviceEnum();
			m_Enumerator = m_Engine.CreateDeviceEnumerator();

			int deviceCount = m_Enumerator.Count;
			if (0 == deviceCount) 
			{
				throw new ReaderException(ReaderError.NoDevices, ReaderErrorMessages.NoDevices, ErrorProvider.Reader);
			}

			for (int i = 0; i < deviceCount; i++) 
			{
				Device device = m_Enumerator.CreateDevice(i);
				if (null != device)
				{
					devices.Add(new DeviceInfo(i, CreateDeviceTitle(device), device.DriveLetter));

					device.Dispose();
				}
				else
				{
					throw new ReaderException(m_Enumerator);
				}
			}

			return devices;
		}

		public List<TrackDetails> EnumerateTracks(int deviceIndex)
		{
			List<TrackDetails> tracks = null;
			if (!m_isOpen)
			{
				throw new ReaderException(ReaderError.ReaderNotOpen, ReaderErrorMessages.ReaderNotOpen, ErrorProvider.Reader);
			}

			SelectDevice(deviceIndex, false);

			if (null != m_Device)
			{
				if (m_Device.MediaIsCD)
				{
					tracks = PrepareCDTracks(m_Device);
				}
				else if (m_Device.MediaIsDVD || m_Device.MediaIsBD)
				{
					tracks = PrepareDVDBDTracks(m_Device);
				}
			}
			else
			{
				throw new ReaderException(ReaderError.DeviceNotSet, ReaderErrorMessages.DeviceNotSet, ErrorProvider.Reader);
			}
			if (null == tracks)
			{
				tracks = new List<TrackDetails>();
			}
			return tracks;
		}
		public void ReadTrackUserData(TrackRipSettings settings)
		{
			if (!m_isOpen)
			{
				throw new ReaderException(ReaderError.ReaderNotOpen, ReaderErrorMessages.ReaderNotOpen, ErrorProvider.Reader);
			}

			SelectDevice(settings.DeviceIndex, false);

			if (null != m_Device)
			{
				if (m_Device.MediaIsCD)
				{
					RipCDTrack(m_Device, settings);
				}
				else if (m_Device.MediaIsDVD || m_Device.MediaIsBD)
				{
					RipDVDBDTrack(m_Device, settings);
				}
			}
			else
			{
				throw new ReaderException(ReaderError.DeviceNotSet, ReaderErrorMessages.DeviceNotSet, ErrorProvider.Reader);
			}
		}
		
        public void PrepareSource(int deviceIndex, int trackIndex)
		{
			SelectDevice(deviceIndex, false);
			m_TrackIndex = trackIndex;
			m_UseImage = false;
		}
		
        public void PrepareSource(string imageFile)
		{
			m_ImageFile = imageFile;
			m_UseImage = true;
		}

		public void ReadFileFromSource(string filePath, string destinationFolder)
		{
			DataFile dataFile = FindItem(filePath);
			if (null == dataFile)
			{
				throw new ReaderException(ReaderError.ItemNotFound, ReaderErrorMessages.ItemNotFound, ErrorProvider.Reader);
			}
			try
			{
				if (m_UseImage)
				{
					ReadFileFromImage(dataFile, destinationFolder);
				}
				else
				{
					ReadFileFromDisc(dataFile, destinationFolder);
				}
			}
			catch (ReaderException ex)
			{
				throw ex;
			}
		}
		
        public List<LayoutItem> GetFolderContentFromLayout(string folderPath)
		{
			List<LayoutItem> content = new List<LayoutItem>();

			DataFile dataFile = FindItem(folderPath);
			if (null == dataFile)
				throw new ReaderException(ReaderError.ItemNotFound, ReaderErrorMessages.ItemNotFound, ErrorProvider.Reader);

			if (!dataFile.IsDirectory)
				throw new ReaderException(ReaderError.ItemNotFolder, ReaderErrorMessages.ItemNotFolder, ErrorProvider.Reader);

			DataFileList children = dataFile.Children;
			for (int i = 0; i < children.Count; i++)
			{
				DataFile item = children[i];

				// Get file creation time. It will be UTC(GMT)

				LayoutItem layoutItem;
				layoutItem.Address = item.DiscAddress;
				layoutItem.FileName = item.LongFilename;
				layoutItem.FileTime = item.FileTime;
				layoutItem.IsDirectory = item.IsDirectory;
				layoutItem.SizeInBytes = item.FileSize;
				content.Add(layoutItem);
			}

			return content;
		}

		#endregion
		
		#region Protected Methods
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

		protected void SelectDevice(int deviceIndex, bool exclusive)
		{
			CleanupDeviceEnum();
			CleanupDevice();
			m_Enumerator = m_Engine.CreateDeviceEnumerator();

			m_Device = m_Enumerator.CreateDevice(deviceIndex, exclusive);

			if (null == m_Device)
			{
				throw new ReaderException(m_Enumerator);
			}
		}

		protected string CreateDeviceTitle(Device device)
		{
			return string.Format("({0}:) - {1}", device.DriveLetter, device.Description);
		}

		protected List<TrackSegment> BuildTrackkSegmentList(Device device, TrackInfoEx trackInfo)
		{
			List<TrackSegment> segments = new List<TrackSegment>();

			if (LayerJumpRecordingStatus.NoLayerJump != trackInfo.LayerJumpRecordingStatus)
			{
				// This case should be reached only when processing DVD-R DL LJ media
				long l0Size = device.MediaLayerCapacity;
				if (0 == l0Size)
				{
					throw new ReaderException(device);
				}
				if (trackInfo.IsLRAValid)
				{
					// The start addres of a track in layer jump recording mode would be on layer 0 and the last
					// possible address of a track in layer jump recording would be on layer 1. To calculate the
					// size of such a track TRACKINFOEX::LastLayerJumpAddress value must be used. The formula
					// states that the address of the first block after the jump on layer 1 would be
					// '2M - 1 - LJA' where M is the start LBA of the data area on layer 1 and LJA is the layer
					// jump address on layer 0 (address at which the jump was performed from layer 0 to layer 1)
					// Note that DVD-R DL are OTP (Opposite Track Path) discs. Furthermore layer 0 is slightly
					// larger than layer 1.
					if (l0Size <= trackInfo.LastRecordedAddress)
					{
						// add segment on layer0
						segments.Add(new TrackSegment(trackInfo.Address, trackInfo.LastLayerJumpAddress - trackInfo.Address + 1));
						// add segment on layer1
						int layer1StartAddress = 2 * (int)l0Size - 1 - trackInfo.LastLayerJumpAddress;
						int layer1RecordedSize = trackInfo.LastRecordedAddress - layer1StartAddress + 1;
						segments.Add(new TrackSegment(layer1StartAddress, layer1RecordedSize));
					}
					else
					{
						segments.Add(new TrackSegment(trackInfo.Address, trackInfo.LastRecordedAddress - trackInfo.Address + 1));
					}
				}
				else
				{
					throw new ReaderException(ReaderError.TrackLRAInvalid, ReaderErrorMessages.TrackLRAInvalid, ErrorProvider.Reader);
				}
			}
			else
			{
				segments.Add(new TrackSegment(trackInfo.Address, trackInfo.RecordedSize));
			}
			return segments;
		}
		
        protected int GetLastCompleteTrack(Device device)
		{
			// Get the last track number from the last session if multisession option was specified
			int lastTrackIndex = 0;

			// Check for DVD+RW, DVD-RW RO, MP_DVD_RAM_RW and MP_BD_RE random writable media. 
			MediaProfile mp = device.MediaProfile;
			if (MediaProfile.DvdPlusRw == mp || MediaProfile.DvdMinusRwRo == mp || MediaProfile.DvdRam == mp || MediaProfile.BdRe == mp)
			{
				// DVD+RW, DVD-RW RO, MP_DVD_RAM_RW and MP_BD_RE has only one session with one track
				if (device.MediaFreeSpace > 0)
					lastTrackIndex = 1;	
			}		
			else
			{
				// All other media is recorded using tracks and sessions and multi-session is no different 
				// than with the CD. 

				// Use the ReadDiskInfo method to get the last track number
                DiscInfo di = device.ReadDiscInfo();
				if (null != di)
				{
					lastTrackIndex = di.LastTrack;
					
					// ReadDiskInfo reports the empty space as a track too
					// That's why we need to go back one track to get the last completed track
					if (DiscStatus.Open == di.DiscStatus || DiscStatus.Empty == di.DiscStatus)
						lastTrackIndex--;
				}
				else
				{
					throw new ReaderException(device);
				}
			}

			return lastTrackIndex;
		}

		protected List<TrackDetails> PrepareCDTracks(Device device)
		{
			List<TrackDetails> tracks = new List<TrackDetails>();
			if (null != device)
			{
				// Read the table of content
                Toc toc = m_Device.ReadToc();
				if (null != toc)
					throw new ReaderException(m_Device);

				// Last entry is the start address of lead-out
				for (int i = toc.FirstTrack; i <= toc.LastTrack; i++) 
				{
					int nIndex = i - toc.FirstTrack;
					int nAddr = toc.Tracks[nIndex].Address;
					string title = string.Empty;
					if (toc.Tracks[nIndex].IsData)
						title = string.Format("{0,2} Data  LBA: {1,5}. Time: ({2,2}:{3,2})", nIndex + 1, nAddr, nAddr / 4500, (nAddr % 4500) / 75);
					else
						title = string.Format("{0,2} Audio LBA: {1,5}. Time: ({2,2}:{3,2})", nIndex + 1, nAddr, nAddr / 4500, (nAddr % 4500) / 75);

					tracks.Add(new TrackDetails(nIndex + 1, title));
				}
			}
			else
			{
				throw new ReaderException(ReaderError.DeviceNotSet, ReaderErrorMessages.DeviceNotSet, ErrorProvider.Reader);
			}
			return tracks;
		}

		protected List<TrackDetails> PrepareDVDBDTracks(Device device)
		{
			List<TrackDetails> tracks = new List<TrackDetails>();
			if (null != device)
			{
				int lastTrackIndex = GetLastCompleteTrack(device);
				if (0 == lastTrackIndex)
					return tracks;

				for (int trackIndex = 1; trackIndex <= lastTrackIndex; trackIndex++) 
				{
                    TrackInfoEx ti = device.ReadTrackInfoEx(trackIndex);
					if (null != ti)
					{
						List<TrackSegment> segments = BuildTrackkSegmentList(device, ti);
						int trackSize = 0;
						for (int j = 0; j < segments.Count; j++)
						{
							TrackSegment segment = segments[j];
							trackSize += segment.RecordedSize;
						}

						string title = string.Format("{0,2} Packet: {1} Start: {2,6} Track Size: {3,6} Recorded Size: {4,6}",
							ti.TrackNumber, ti.IsPacket,  ti.Address, ti.TrackSize, trackSize);

						tracks.Add(new TrackDetails(trackIndex, title));
					}
					else
					{
						throw new ReaderException(device);
					}
				}
			}
			else
			{
				throw new ReaderException(ReaderError.DeviceNotSet, ReaderErrorMessages.DeviceNotSet, ErrorProvider.Reader);
			}
			return tracks;
		}

		protected bool ReadFileSegmentFromDevice(FileSegment segment, TrackBuffer buffer, System.IO.FileStream destinationFile)
		{
			int blocksAtOnce = (int)DefaultBufferSize.Cd;
			int currentBlockSize = (int)BlockSize.CdRom;
			if (m_Device.MediaIsDVD)
			{
				blocksAtOnce = (int)DefaultBufferSize.Dvd;
				currentBlockSize = (int)BlockSize.Dvd;
			}
			else if (m_Device.MediaIsBD)
			{
				blocksAtOnce = (int)DefaultBufferSize.Bd;
				currentBlockSize = (int)BlockSize.Bd;
			}

			int startFrame = segment.StartAddress;
			int endFrame = segment.EndAddress;

            while (startFrame < endFrame) 
			{
                int numFrames = Math.Min(blocksAtOnce, endFrame - startFrame);
				if (!m_Device.ReadData(buffer, startFrame, numFrames))
				{
                    return false;
				}

				CallOnReadProgress(startFrame, buffer.Blocks, buffer.BlockSize); 

				// Detect changes of block size
				if (currentBlockSize != buffer.BlockSize)
				{
					CallOnBlockSizeChanged(currentBlockSize, buffer.BlockSize, startFrame);
					currentBlockSize = buffer.BlockSize;
				}

				// Update buffer start address in frames (blocks)
				startFrame += buffer.Blocks;

				if (startFrame < endFrame)
                    destinationFile.Write(buffer.Buffer, 0, buffer.Blocks * buffer.BlockSize);
				else
				{
					// The real file size may not align on BlockSize.CdRom.
					// We should not write the padding bytes
					int padding = (int)BlockSize.CdRom - (int)(segment.FileSize % (long)BlockSize.CdRom);
					if ((int)BlockSize.CdRom == padding)
					{
						// if the remainder of (segment.FileSize % (long)BlockSize.CdRom) is 0 then there should be no padding
						padding = 0;
					}

                    destinationFile.Write(buffer.Buffer, 0, (buffer.Blocks * buffer.BlockSize) - padding);
				}
			}

			return true;
		}

		protected void ReadFileFromDisc(DataFile dataFile, string destinationFolder)
		{
			if (null == m_Device)
			{
				throw new ReaderException(ReaderError.DeviceNotSet, ReaderErrorMessages.DeviceNotSet, ErrorProvider.Reader);
			}
			if (null != dataFile)
			{
				if (!dataFile.IsDirectory)
				{
					List<FileSegment> segments = new List<FileSegment>();
					// Use Udf file extents to track the location of the file segments on the medium. 
					IList<UdfExtent> extents = dataFile.UdfFileProps.Extents;
					int cnt = extents.Count;
					if (0 < cnt)
					{
						for (int i = 0; i < cnt; i++)
						{
							UdfExtent extent = extents[i];
							segments.Add(new FileSegment(extent.Address, extent.Length));
						}
					}
					else
					{
						segments.Add(new FileSegment(dataFile.DiscAddress, dataFile.FileSize));
					}
					// Make the full path
					string destinationFilePath = System.IO.Path.Combine(destinationFolder, dataFile.LongFilename);

					// Open the file and write all the data
					FileStream destinationFile = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);
					if (null != destinationFile)
					{
						int blocksAtOnce = (int)DefaultBufferSize.Cd;
						int currentBlockSize = (int)BlockSize.CdRom;
						if (m_Device.MediaIsDVD)
						{
							blocksAtOnce = (int)DefaultBufferSize.Dvd;
							currentBlockSize = (int)BlockSize.Dvd;
						}
						else if (m_Device.MediaIsBD)
						{
							blocksAtOnce = (int)DefaultBufferSize.Bd;
							currentBlockSize = (int)BlockSize.Bd;
						}

						bool deviceError = false;

						TrackBuffer buffer = new TrackBuffer(currentBlockSize, blocksAtOnce);
						for (int si = 0; si < segments.Count; si ++)
						{
							FileSegment segment = segments[si];
							deviceError = ReadFileSegmentFromDevice(segment, buffer, destinationFile);
							if (deviceError)
							{
								break;
							}
						}

						destinationFile.Flush();
						destinationFile.Close();

						if (deviceError)
						{
							throw new ReaderException(m_Device);
						}
					}
				}
				else
				{
					throw new ReaderException(ReaderError.ItemNotFound, ReaderErrorMessages.ItemNotFound, ErrorProvider.Reader);
				}
			}
			else
			{
				throw new ReaderException(ReaderError.ItemNotFound, ReaderErrorMessages.ItemNotFound, ErrorProvider.Reader);
			}
		}

		protected void ReadFileFromImage(DataFile dataFile, string destinationFolder)
		{
			if (null != dataFile)
			{
				int startFrame = dataFile.DiscAddress;
				int endFrame = startFrame + (int)(dataFile.FileSize / (int)BlockSize.CdRom);
				if (0 < dataFile.FileSize % (int)BlockSize.CdRom)
					endFrame += 1;

				int currentBlockSize = (int)BlockSize.CdRom;
				int blocksAtOnce = (int)DefaultBufferSize.Cd;

				if (!dataFile.IsDirectory)
				{
					// Make the full path
					string destinationFilePath = System.IO.Path.Combine(destinationFolder, dataFile.LongFilename);

					// Open the files
					FileStream sourceFile = new FileStream(m_ImageFile, FileMode.Open, FileAccess.Read);

					FileStream destinationFile = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);

					// Copy the file from the image
					if (null != destinationFile && null != sourceFile)
					{
						int numFrames = 0;
						int frameSize = currentBlockSize;
						
						// allocate data buffer
						byte[] buffer = new byte[frameSize * blocksAtOnce];

						// Seek to the correct offset in the image file
						sourceFile.Seek(startFrame * frameSize, SeekOrigin.Begin);
						
						// Rip the file
						while (startFrame < endFrame)
						{
							numFrames = Math.Min(blocksAtOnce, endFrame - startFrame);

							int len = (int)sourceFile.Read(buffer, 0, frameSize * numFrames);
							if (0 == len)
								break;

							CallOnReadProgress(startFrame, numFrames, frameSize); 

							// Update buffer start address in frames (blocks)
							startFrame += len / frameSize;

							if (startFrame < endFrame)
								destinationFile.Write(buffer, 0, len);
							else
							{
								// The real file size may not align on BlockSize.CdRom. 
								// We should not write the padding bytes
								int padding = (int)BlockSize.CdRom - (int)(dataFile.FileSize % (int)BlockSize.CdRom);
								if ((int)BlockSize.CdRom == padding)
								{
									// if the remainder of (dataFile.FileSize % (int)BlockSize.CdRom) is 0 then there should be no padding
									padding = 0;
								}
								destinationFile.Write(buffer, 0, len - padding);
							}
						}

						destinationFile.Flush();
						destinationFile.Close();

						sourceFile.Close();
					}
				}
				else
				{
					throw new ReaderException(ReaderError.ItemNotFile, ReaderErrorMessages.ItemNotFile, ErrorProvider.Reader);
				}
			}
			else
			{
				throw new ReaderException(ReaderError.ItemNotFound, ReaderErrorMessages.ItemNotFound, ErrorProvider.Reader);
			}
		}

		protected DataFile FindItem(string path)
		{
			DataFile item = null;
			CleanupDataDisc();
			m_DataDisc = new DataDisc();

			if (!m_UseImage)
			{
				// Load image layout
				m_DataDisc.Device = m_Device;
				if (!m_DataDisc.LoadFromDisc(m_TrackIndex))
				{
					throw new ReaderException(m_DataDisc, m_Device);
				}
			}
			else
			{
				// Load image layout
				if (!m_DataDisc.LoadFromFile(m_ImageFile))
				{
					throw new ReaderException(m_DataDisc, null);
				}
			}

			// Get image layout in a tree structure of DataFile objects
			DataFile root = m_DataDisc.ImageLayout;

			if (0 == path.Length || 0 == path.CompareTo("/") || 0 == path.CompareTo("\\"))
			{
				// only if the path parameter is an empty string would it be assumed that the root layout is
				// to be returned, otherwise it is assumed that the path parameter specifies a specific item
				// within the layout representd by the root object
				item = root;
			}
			else
			{
				item = root.Find(path, false);
			}

			return item;
		}

		protected void RipCDTrack(Device device, TrackRipSettings settings)
		{
            Toc toc = device.ReadToc();
			if (null == toc)
			{
				throw new ReaderException(m_Device);
			}
			
			device.ReadSpeedKB = device.MaxReadSpeedKB;
			TocTrack track = toc.Tracks[settings.TrackIndex - 1];
			TocTrack nextTrack = toc.Tracks[settings.TrackIndex];
			if (track.IsAudio)
			{
				throw new ReaderException(ReaderError.TrackIsAudio, ReaderErrorMessages.TrackIsAudio, ErrorProvider.Reader);
			}

			//  Last entry is the start address of lead-out
			int startFrame = track.Address;
			int endFrame = nextTrack.Address;

			// Detect the track type. 
			// Scan 100 blocks to see if the track is a mode2 form1, form2 or mixed mode track
			// This sample does not support mixed block tracks.
			TrackType tt = device.DetectTrackType(startFrame, 100);
			if (TrackType.Mode2Mixed == tt || TrackType.Mode2Form1 == tt || TrackType.Mode2Form2 == tt)
			{
				CallShowMessage("Mode 2, Form1, Form2 or Mixed form data detected.");
			}

			// Determine the block size of the chosen track
			int blocksAtOnce = (int)DefaultBufferSize.Cd;
			
			int currentBlockSize = CDSector.GetTrackBlockSize(tt);

			string destinationFilePath = System.IO.Path.Combine(settings.DestinationFolder, settings.UserDataFileName);
			FileStream file = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);

			string destinationRawFilePath = System.IO.Path.Combine(settings.DestinationFolder, settings.RawDataFileName);
			FileStream rawFile = new FileStream(destinationRawFilePath, FileMode.Create, FileAccess.Write);

			if (null != file || null != rawFile)
			{
				string message = string.Format("Saving data image to {0} and {1} using {2} byte blocks...", destinationFilePath, destinationRawFilePath, currentBlockSize);
				CallShowMessage(message);

				bool deviceError = false;

				TrackBuffer buffer = new TrackBuffer(currentBlockSize, blocksAtOnce);
				while (startFrame < endFrame) 
				{
					int numberOfSectorsToRead = Math.Min(blocksAtOnce, endFrame - startFrame);
                    if (!device.ReadData(buffer, startFrame, numberOfSectorsToRead))
					{
						deviceError = true;
						break;
					}

					CallOnReadProgress(startFrame, buffer.Blocks, buffer.BlockSize);

					// Detect changes of block size
					if (currentBlockSize != buffer.BlockSize)
					{
						CallOnBlockSizeChanged(currentBlockSize, buffer.BlockSize, startFrame);
						currentBlockSize = buffer.BlockSize;
					}

					// Write just the user data
					WriteToFile(file, buffer);

					// Write the user data and the system information like EDC and ECC
					WriteToRawFile(rawFile, tt, buffer, startFrame);

					if (numberOfSectorsToRead > buffer.Blocks)
					{
						// At the end of the track several sectors might not be readable.
						// Such is the case with Track-At-Once tracks since the lead-out start there is
						// shifted with 2 blocks and that prevents some devices from reading all blocks
						// requested in buffer.NumFrames.
						break;
					}

					// Update buffer start address in frames (blocks)
					startFrame += buffer.Blocks;
				}

				file.Flush();
				rawFile.Flush();

				file.Close();
				rawFile.Close();

				if (deviceError)
				{
					throw new ReaderException(device);
				}
			}
			else
			{
				string message = string.Format("Cannot open {0} or {1} for writing.", destinationFilePath, destinationRawFilePath);
				CallShowMessage(message);
			}
		}

		protected void RipDVDBDTrack(Device device, TrackRipSettings settings)
		{
			// Get track informations
            TrackInfoEx ti = device.ReadTrackInfoEx(settings.TrackIndex);
			if (null == ti)
			{
				throw new ReaderException(device);
			}

			string destinationFilePath = System.IO.Path.Combine(settings.DestinationFolder, settings.UserDataFileName);
			FileStream file = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write);
			if (null != file)
			{
				device.ReadSpeedKB = device.MaxReadSpeedKB;

				bool deviceError = false;
				TrackBuffer buffer = null;
				List<TrackSegment> segments = BuildTrackkSegmentList(device, ti);

				// Determine the block size of the chosen track
				int currentBlockSize = (int)BlockSize.Dvd;
				if (device.MediaIsBD)
				{
					currentBlockSize = (int)BlockSize.Bd;
				}
				int blocksAtOnce = 32;

				string message = string.Format("Saving data image to {0} using {1} byte blocks...", destinationFilePath, currentBlockSize);
				CallShowMessage(message);

				for (int pos = 0; pos < segments.Count; pos++)
				{
					TrackSegment segment = segments[pos];
					//  Last entry is the start address of lead-out
					int startFrame = segment.StartAddress;
					int endFrame = segment.EndAddress;

					buffer = new TrackBuffer(currentBlockSize, blocksAtOnce);
					while (startFrame < endFrame) 
					{
						int numFrames = Math.Min(blocksAtOnce, endFrame - startFrame + 1);
						if (!device.ReadData(buffer, startFrame, numFrames))
						{
							deviceError = true;
							break;
						}

						CallOnReadProgress(startFrame, buffer.Blocks, buffer.BlockSize); 

						// Write the user data to a file
						WriteToFile(file, buffer);

						// Update buffer start address in frames (blocks)
						startFrame += buffer.Blocks;
					}
					if (deviceError)
					{
						break;
					}
				}

				buffer = null;

				file.Flush();
				file.Close();
				
                if (deviceError)
				{
					throw new ReaderException(device);
				}
			}
			else
			{
				string message = string.Format("Cannot open {0} for writing.", destinationFilePath);
				CallShowMessage(message);
			}
		}

		protected void WriteToFile(FileStream file, TrackBuffer buffer)
		{
			file.Write(buffer.Buffer, 0, buffer.Blocks * buffer.BlockSize);
		}

		protected void WriteToRawFile(FileStream file, TrackType tt, TrackBuffer srcBuffer, int startFrame)
		{
			int bufferSize = (int)BlockSize.Cdda;
			byte[] rawBuf = new byte[bufferSize];
			byte[] srcBufferSection = new byte[bufferSize];
			for (int dw = 0; dw < srcBuffer.Blocks; dw++)
			{
				Array.Copy(srcBuffer.Buffer, dw * srcBuffer.BlockSize, srcBufferSection, 0, srcBuffer.BlockSize);
				CDSector.EncodeRawCDBlock(tt, startFrame + dw, true, rawBuf, srcBufferSection);
				file.Write(rawBuf, 0, bufferSize);
			}

		}

		protected void CallOnReadProgress(int startFrame, int numberOfFrames, int frameSize)
		{
			ReaderEventProvider.ReadProgress handler = this.ReadProgress;
			if (null != handler)
			{
				handler(startFrame, numberOfFrames, frameSize);
			}
		}

		protected void CallOnBlockSizeChanged(int currentBlockSize, int newBlockSize, int startFrame)
		{
			
			ReaderEventProvider.BlockSizeChange handler = this.BlockSizeChange;
			if (null != handler)
			{
				handler(currentBlockSize, newBlockSize, startFrame);
			}
		}

		protected void CallShowMessage(string message)
		{
			ReaderEventProvider.ReaderNotificationMessage handler = this.ReaderNotificationMessage;
			if (null != handler)
			{
				handler(message);
			}
		}
		#endregion
	}
}
