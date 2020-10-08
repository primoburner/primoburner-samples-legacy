using System;
using System.IO;
using System.Collections;

using PrimoSoftware.Burner;

namespace DiscCopy.NET
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
        /// Indicates whether this device is a writer
        /// </summary>
		public bool IsWriter;

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
		const string IMAGE_DESCRIPTION_FILE_NAME = "image.sdi";

		#region Construct / Finalize
		public Burner()
		{
			m_MediaType = MediaType.None;
			// Initialize the SDK
			Library.Initialize();
		}

		~Burner()
		{
			// Close
			if (m_isOpen)
				Close();

			// Shutdown the SDK
			Library.Shutdown();
		}
		#endregion

		#region Public Events
		public event BurnerCallback.Status Status;
		public event BurnerCallback.TrackStatus TrackStatus;
		public event BurnerCallback.CopyProgress CopyProgress;
		public event BurnerCallback.Continue Continue;

		public event BurnerCallback.FormatProgress FormatProgress;
		public event BurnerCallback.EraseProgress EraseProgress;
		
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
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				return m_SrcDevice.MediaIsBlank;
			}
		}

		public bool MediaIsFullyFormatted
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				// Get media profile
				MediaProfile mp = m_SrcDevice.MediaProfile;

				// DVD+RW
				if (MediaProfile.DvdPlusRw == mp)
					return (BgFormatStatus.Completed == m_SrcDevice.BgFormatStatus);

				// DVD-RW for Restricted Overwrite
				if (MediaProfile.DvdMinusRwRo == mp)
					return (m_SrcDevice.MediaFreeSpace == m_SrcDevice.MediaCapacity);

				return false;
			}
		}

		public int DeviceCacheSize
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				return m_SrcDevice.InternalCacheCapacity;
			}
		}

		public int DeviceCacheUsedSpace
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				return m_SrcDevice.InternalCacheUsedSpace;
			}
		}

		public int WriteTransferKB
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				return m_SrcDevice.WriteTransferRate;
			}
		}

		public PrimoSoftware.Burner.MediaProfile MediaProfile
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				return m_SrcDevice.MediaProfile;
			}
		}

		public bool MediaIsValidProfile
		{
			get
			{
				// Only CDs and DVDs are being processed by DiscCopy interface so any other media types (currently only BDs)
				// are invalid
				return MediaIsCD || MediaIsDVD || MediaIsBD;
			}
		}

		public bool MediaIsCD
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

                if (MediaReady.Present == m_SrcDevice.MediaState)
					return m_SrcDevice.MediaIsCD;

				return false;
			}
		}

		public bool MediaIsDVD
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

                if (MediaReady.Present == m_SrcDevice.MediaState)
					return m_SrcDevice.MediaIsDVD;

				return false;
			}
		}

		public bool MediaIsBD
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				if (MediaReady.Present == m_SrcDevice.MediaState)
					return m_SrcDevice.MediaIsBD;

				return false;
			}
		}

		public bool MediaCanReadSubChannel
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				if (MediaIsCD)
				{
					CDFeatures cdFeatures = m_SrcDevice.CDFeatures;
					return cdFeatures.CanReadRawRW || cdFeatures.CanReadPackedRW;
				}
				return false;
			}
		}

		public bool RawDaoPossible
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				return m_SrcDevice.CDFeatures.CanWriteRawDao;
			}
		}

		public bool MediaIsRewritable
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				return m_SrcDevice.MediaIsReWritable;
			}
		}

		public bool BDRFormatAllowed
		{
			get
			{
				if (null == m_SrcDevice)
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

				return (MediaProfile.BdRSrmPow == m_OriginalMediaProfile) && (MediaProfile.BdRSrm == m_SrcDevice.MediaProfile);
			}
		}

		public MediaProfile OriginalMediaProfile
		{
			get { return m_OriginalMediaProfile; }
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

				throw BurnerException.CreateBurnerException(BurnerErrors.ENGINE_INITIALIZATION);
			}

			m_isOpen = true;
		}

		public void Close()
		{
			m_MediaType = MediaType.None;
			ReleaseDevices();

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
				throw BurnerException.CreateBurnerException(BurnerErrors.BURNER_NOT_OPEN);

			ArrayList deviceArray = new ArrayList();

			DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator();
			int devices = enumerator.Count;
			if (0 == devices)
			{
				enumerator.Dispose();
				throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICES);
			}

			for (int i = 0; i < devices; i++)
			{
				Device device = enumerator.CreateDevice(i);
				if (null != device)
				{
					DeviceInfo dev = new DeviceInfo();
					dev.Index = i;
					dev.Title = GetDeviceTitle(device);

					deviceArray.Add(dev);
					device.Dispose();
				}
			}
			enumerator.Dispose();

			return (DeviceInfo[])deviceArray.ToArray(typeof(DeviceInfo));
		}

		public void SelectDevice(int deviceIndex, bool exclusive, bool dstDevice)
		{
			Device dev = dstDevice ? m_DstDevice : m_SrcDevice;
			if (null != dev)
				throw BurnerException.CreateBurnerException(BurnerErrors.DEVICE_ALREADY_SELECTED);

			DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator();
			dev = enumerator.CreateDevice(deviceIndex, exclusive);
			if (null == dev)
			{
				enumerator.Dispose();
				throw BurnerException.CreateBurnerException(BurnerErrors.INVALID_DEVICE_INDEX);
			}
			if (!dstDevice)
			{
				m_SrcDevice = dev;
			}
			else
			{
				m_DstDevice = dev;
			}
			enumerator.Dispose();
		}

		public void ReleaseDevices()
		{
			if (null != m_SrcDevice)
				m_SrcDevice.Dispose();

			m_SrcDevice = null;

			if (null != m_DstDevice)
				m_DstDevice.Dispose();

			m_DstDevice = null;
		}

		public void CreateImage(CreateImageSettings settings)
		{
			Device srcDevice = m_SrcDevice;
			m_MediaType = MediaType.None;
			if (null == srcDevice)
				throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

            PrimoSoftware.Burner.DiscCopy discCopy = new PrimoSoftware.Burner.DiscCopy();

			// Add event handlers
			discCopy.OnStatus += DiscCopy_OnStatus;
			discCopy.OnTrackStatus += DiscCopy_OnTrackStatus;
			discCopy.OnProgress += DiscCopy_OnProgress;
			discCopy.OnContinueCopy += DiscCopy_OnContinueCopy;

			string imageFile = Path.Combine(settings.ImageFolderPath, IMAGE_DESCRIPTION_FILE_NAME);

			if (MediaReady.Present == srcDevice.MediaState)
			{
				m_OriginalMediaProfile = srcDevice.MediaProfile;
				// Create an image for the selected medium
				if (srcDevice.MediaIsCD)
				{
					m_MediaType = MediaType.CD;
					CDCopyReadMethod readMethod = settings.ReadSubChannel ? CDCopyReadMethod.CdFullRaw : CDCopyReadMethod.CdRaw2352;
					if (!discCopy.CreateImageFromCD(imageFile, srcDevice, readMethod))
					{
						throw BurnerException.CreateDiscCopyException(srcDevice, null, discCopy);
					}
				}
				else if (srcDevice.MediaIsDVD)
				{
					m_MediaType = MediaType.DVD;
					if (!discCopy.CreateImageFromDVD(imageFile, srcDevice))
					{
						throw BurnerException.CreateDiscCopyException(srcDevice, null, discCopy);
					}
				}
				else if (srcDevice.MediaIsBD)
				{
					m_MediaType = MediaType.BD;
					if (!discCopy.CreateImageFromBD(imageFile, srcDevice))
					{
						throw BurnerException.CreateDiscCopyException(srcDevice, null, discCopy);
					}
				}
				else
				{
					m_OriginalMediaProfile = MediaProfile.Unknown;
					throw BurnerException.CreateBurnerException(BurnerErrors.MEDIA_TYPE_NOT_SUPPORTED);
				}
			}
			else
			{
				throw BurnerException.CreateBurnerException(BurnerErrors.DEVICE_ALREADY_SELECTED);
			}
		}

		public void BurnImage(BurnImageSettings settings)
		{
			Device dstDevice = m_SrcDevice;
			if (null == dstDevice)
				throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

			PrimoSoftware.Burner.DiscCopy discCopy = new PrimoSoftware.Burner.DiscCopy();

			// Add event handlers
			discCopy.OnStatus += DiscCopy_OnStatus;
			discCopy.OnTrackStatus += DiscCopy_OnTrackStatus;
			discCopy.OnProgress += DiscCopy_OnProgress;
			discCopy.OnContinueCopy += DiscCopy_OnContinueCopy;

			string imageFile = Path.Combine(settings.ImageFolderPath, IMAGE_DESCRIPTION_FILE_NAME);

			if (MediaReady.Present == dstDevice.MediaState)
			{
				// Write the image to the CD/DVD/BD
				if (MediaType.CD == m_MediaType)
				{
					if (!discCopy.WriteImageToCD(dstDevice, imageFile, settings.WriteMethod)) 
					{
						throw BurnerException.CreateDiscCopyException(null, dstDevice, discCopy);
					}
				}
				else if (MediaType.DVD == m_MediaType)
				{
					if (!discCopy.WriteImageToDVD(dstDevice, imageFile)) 
					{
						throw BurnerException.CreateDiscCopyException(null, dstDevice, discCopy);
					}
				}
				else if (MediaType.BD == m_MediaType)
				{
					if (!discCopy.WriteImageToBD(dstDevice, imageFile))
					{
						throw BurnerException.CreateDiscCopyException(null, dstDevice, discCopy);
					}
				}
			}
			else
			{
				throw BurnerException.CreateBurnerException(BurnerErrors.DEVICE_NOT_READY);
			}
		}

		public void CleanMedia(CleanMediaSettings settings)
		{
			if (null == m_SrcDevice)
				throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

			m_SrcDevice.OnErase += Device_Erase;
			m_SrcDevice.OnFormat += Device_Format;

			bool bRes = true;
			MediaProfile mp = m_SrcDevice.MediaProfile;
			if (CleanMethod.Erase == settings.MediaCleanMethod)
			{
				if (MediaProfile.DvdMinusRwSeq != mp && MediaProfile.DvdMinusRwRo != mp &&
					MediaProfile.CdRw != mp)
				{
					throw BurnerException.CreateBurnerException(BurnerErrors.ERASE_NOT_SUPPORTED);
				}

				bRes = m_SrcDevice.Erase(settings.Quick ? EraseType.Minimal : EraseType.Disc);
			}
			else if (CleanMethod.Format == settings.MediaCleanMethod)
			{
				switch(mp)
				{
					case MediaProfile.DvdMinusRwSeq:
					case MediaProfile.DvdMinusRwRo:
						bRes = m_SrcDevice.Format(settings.Quick ? FormatType.DvdMinusRwQuick : FormatType.DvdMinusRwFull);
					break;

					case MediaProfile.DvdPlusRw:
					{
						BgFormatStatus fmt = m_SrcDevice.BgFormatStatus;
						switch(fmt)
						{
							case BgFormatStatus.NotFormatted:
								bRes = m_SrcDevice.Format(FormatType.DvdPlusRwFull, 0, !settings.Quick);
								break;
							case BgFormatStatus.Partial:
								bRes = m_SrcDevice.Format(FormatType.DvdPlusRwRestart, 0, !settings.Quick);
								break;
							case BgFormatStatus.Completed:
								bRes = true;
								break;
							case BgFormatStatus.Pending:
								bRes = false;
								break;
						}
						break;
					}
					case MediaProfile.DvdRam:
					{
						if(!m_SrcDevice.MediaIsFormatted) 
						{ 
							bRes = m_SrcDevice.Format(FormatType.DvdRamFull);
						} 
						break;
					}
					case MediaProfile.BdRe:
					{
						bRes = m_SrcDevice.FormatBD(BDFormatType.BdFull, BDFormatSubType.BdReQuickReformat);
						break;
					}
					case MediaProfile.BdRSrm:
					{
						bRes = m_SrcDevice.FormatBD(BDFormatType.BdFull, BDFormatSubType.BdRSrmPow);
						break;
					}
					default:
						throw BurnerException.CreateBurnerException(BurnerErrors.FORMAT_NOT_SUPPORTED);
				}
			}
			m_SrcDevice.OnErase -= Device_Erase;
			m_SrcDevice.OnFormat -= Device_Format;

			if (!bRes)
			{
				throw BurnerException.CreateDeviceException(m_SrcDevice, true);
			}
			// Refresh to reload disc information
			m_SrcDevice.Refresh();
		}

		public void DirectCopy(DirectCopySettings settings)
		{
			if (null == m_SrcDevice || null == m_DstDevice)
				throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

            PrimoSoftware.Burner.DiscCopy discCopy = new PrimoSoftware.Burner.DiscCopy();

			// Add event handlers
			discCopy.OnStatus += DiscCopy_OnStatus;
			discCopy.OnTrackStatus += DiscCopy_OnTrackStatus;
			discCopy.OnProgress += DiscCopy_OnProgress;
			discCopy.OnContinueCopy += DiscCopy_OnContinueCopy;

			try
			{
				if (MediaReady.Present == m_SrcDevice.MediaState &&
					MediaReady.Present == m_DstDevice.MediaState)
				{
					// Write the image to the CD/DVD/BD
					if (m_SrcDevice.MediaIsCD)
					{
						CDCopyReadMethod readMethod = settings.ReadSubChannel ? CDCopyReadMethod.CdFullRaw : CDCopyReadMethod.CdRaw2352;
						if (!discCopy.CopyCD(m_DstDevice, m_SrcDevice, settings.UseTemporaryFiles, readMethod, settings.WriteMethod))
						{
							throw BurnerException.CreateDiscCopyException(m_SrcDevice, m_DstDevice, discCopy);
						}
					}
					else if (m_SrcDevice.MediaIsDVD)
					{
						if (!discCopy.CopyDVD(m_DstDevice, m_SrcDevice, settings.UseTemporaryFiles))
						{
							throw BurnerException.CreateDiscCopyException(m_SrcDevice, m_DstDevice, discCopy);
						}
					}
					else if (m_SrcDevice.MediaIsBD)
					{
						if (!discCopy.CopyBD(m_DstDevice, m_SrcDevice, settings.UseTemporaryFiles))
						{
							throw BurnerException.CreateDiscCopyException(m_SrcDevice, m_DstDevice, discCopy);
						}
					}
					else
					{
						throw BurnerException.CreateBurnerException(BurnerErrors.MEDIA_TYPE_NOT_SUPPORTED);
					}
				}
				else
				{
					throw BurnerException.CreateBurnerException(BurnerErrors.DEVICE_NOT_READY);
				}

			}
			catch
			{
				throw;
			}
		}

		public void RefreshDevice()
		{
			if (null == m_SrcDevice)
				throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);

			m_SrcDevice.Refresh();
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

		#region DiscCopy Event Handlers
		public void DiscCopy_OnStatus(Object sender, DiscCopyStatusEventArgs args)
		{
			BurnerCallback.Status handler = Status;
			if (null != handler)
			{
				handler(GetDiscCopyStatusString(args.Status));
			}
		}

		public void DiscCopy_OnTrackStatus(object sender, DiscCopyTrackStatusEventArgs args)
		{
			BurnerCallback.TrackStatus handler = TrackStatus;
			if (null != handler)
			{
				handler(args.Track, args.Percent);
			}
		}

		public void DiscCopy_OnProgress(Object sender, DiscCopyProgressEventArgs args)
		{
			BurnerCallback.CopyProgress handler = CopyProgress;
			if (null != handler)
			{
				handler(args.Position, args.All);
			}
		}

		public void DiscCopy_OnContinueCopy(Object sender, DiscCopyContinueCopyEventArgs eArgs)
		{
			BurnerCallback.Continue handler = Continue;
			if (null != handler)
			{
				eArgs.Continue =  handler();
			}

			eArgs.Continue = true;
		}
		#endregion

		#region Private Methods
		private string GetDeviceTitle(Device device)
		{
			return String.Format("({0}:) - {1}", device.DriveLetter, device.Description);
		}

		private string GetDiscCopyStatusString(DiscCopyStatus status)
		{
			switch (status)
			{
				case DiscCopyStatus.ReadingData:
					return "Reading disc...";

				case DiscCopyStatus.ReadingToc:
					return "Reading disc TOC...";

				case DiscCopyStatus.WritingData:
					return "Writing image...";

				case DiscCopyStatus.WritingLeadOut:
					return "Flushing device cache and writing lead-out...";
			}

			return "Unknown status...";
		}

		#endregion

		#region Private Property Members
		private bool m_isOpen = false;
		//private ArrayList m_SrcDeviceArray = new ArrayList();
		#endregion

		#region Private Members
		private Engine m_engine = null;
		private Device m_SrcDevice = null;
		private Device m_DstDevice = null;
		private MediaType m_MediaType = MediaType.None;
		private MediaProfile m_OriginalMediaProfile = MediaProfile.Unknown;
		#endregion

	}
}
