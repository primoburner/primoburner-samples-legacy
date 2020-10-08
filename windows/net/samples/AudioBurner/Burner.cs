using System;
using System.IO;
using System.Collections;

using PrimoSoftware.Burner;
using System.Collections.Generic;

namespace AudioBurner.NET
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
        #region Construct / Finalize
        public Burner()
        {
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
        public event BurnerCallback.Progress Progress;
        public event BurnerCallback.TrackProgress TrackProgress;
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
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.MediaIsBlank;
            }
        }

        public int DeviceCacheSize
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.InternalCacheCapacity;
            }
        }

        public int DeviceCacheUsedSpace
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.InternalCacheUsedSpace;
            }
        }

        public int WriteTransferKB
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.WriteTransferRate;
            }
        }

        public long MediaFreeSpace
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.MediaFreeSpace;
            }
        }

        public int MaxWriteSpeedKB
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.MaxWriteSpeedKB;
            }
        }

        public bool CDTextSupport
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.CDFeatures.CanReadCDText;
            }
        }

        public bool CanReWrite
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.CDFeatures.CanWriteCDRW && m_device.MediaIsReWritable;
            }
        }

        public bool SaoPossible
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.CDFeatures.CanWriteSao;
            }
        }

        public bool TaoPossible
        {
            get
            {
                if (null == m_device)
                    throw new BurnerException(BurnerErrors.NO_DEVICE);

                return m_device.CDFeatures.CanWriteTao;
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

                throw new BurnerException(BurnerErrors.ENGINE_INITIALIZATION);
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
                throw new BurnerException(BurnerErrors.BURNER_NOT_OPEN);

            m_deviceArray.Clear();

            DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator();
            int devices = enumerator.Count;
            if (0 == devices)
            {
                enumerator.Dispose();
                throw new BurnerException(BurnerErrors.NO_DEVICES);
            }

            for (int i = 0; i < devices; i++)
            {
                Device device = enumerator.CreateDevice(i);
                if (null != device)
                {
                    DeviceInfo dev = new DeviceInfo();
                    dev.Index = i;
                    dev.Title = GetDeviceTitle(device);
                    dev.IsWriter = device.CDFeatures.CanWriteCDR || device.CDFeatures.CanWriteCDRW;

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
                throw new BurnerException(BurnerErrors.DEVICE_ALREADY_SELECTED);

            DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator();
            Device dev = enumerator.CreateDevice(deviceIndex, exclusive);
            if (null == dev)
            {
                enumerator.Dispose();
                throw new BurnerException(BurnerErrors.INVALID_DEVICE_INDEX);
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

        public SpeedInfo[] EnumerateWriteSpeeds()
        {
            if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE);

            m_speedArray.Clear();

            IList<SpeedDescriptor> speeds = m_device.GetWriteSpeeds();
            for (int i = 0; i < speeds.Count; i++)
            {
                SpeedDescriptor speed = speeds[i];
                if (null != speed)
                {
                    SpeedInfo speedInfo = new SpeedInfo();
                    speedInfo.TransferRateKB = speed.TransferRateKB;
                    speedInfo.TransferRate1xKB = Speed1xKB.CD;

                    m_speedArray.Add(speedInfo);
                }
            }

            return (SpeedInfo[])m_speedArray.ToArray(typeof(SpeedInfo));
        }

        public void Eject()
        {
            if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE);

            m_device.Eject(true);
        }

        public void CloseTray()
        {
            if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE);

            m_device.Eject(false);
        }

        public void Burn(BurnSettings settings)
        {

            if (null == m_device)
				throw new BurnerException(BurnerErrors.NO_DEVICE);

            AudioCD audio = new AudioCD();

            // Add event handlers
            audio.OnWriteStatus += new EventHandler<AudioCDStatusEventArgs>(AudioCD_OnStatus);
            audio.OnWriteTrack += new EventHandler<AudioCDTrackStatusEventArgs>(AudioCD_OnTrackProgress);
            audio.OnWriteProgress += new EventHandler<AudioCDProgressEventArgs>(AudioCD_OnProgress);
            audio.OnContinueWrite += new EventHandler<AudioCDContinueEventArgs>(AudioCD_OnContinueBurn);

            try
            {
                audio.Device = m_device;

                if (settings.DecodeInTempFiles)
                    audio.AudioDecodingMethod = AudioDecodingMethod.TempFile;
                else
                    audio.AudioDecodingMethod = AudioDecodingMethod.Memory;

                if (0 == settings.Files.Count)
                    throw new BurnerException(BurnerErrors.NO_AUDIO_TRACKS);

                IList<AudioInput> audioInputs = audio.AudioInputs;

                for (int i = 0; i < settings.Files.Count; i++)
                {

                    AudioInput ai = new AudioInput();

                    if (settings.UseAudioStream)
                    {
                        ai.Stream = new FileStream(settings.Files[i], FileMode.Open);
                        ai.Storage = AudioStorageType.Stream;
                    }
                    else
                    {
                        ai.FilePath = settings.Files[i];
                    }

                    audioInputs.Add(ai);
                }

				bool willCreateHiddenTrack = false;

				// The following code will setup the AudioCD object to write a hidden track using the entire data from the first audio
				// input as the hidden content.
				if (settings.CreateHiddenTrack)
				{
					// Until this moment on the target medium there would be an audio track for each audio input loaded in the AudioCD
					// object. To create a hidden track first of all the CDSession describing the current audio CD  will be retrieved.
					CDSession session = audio.CreateCDSession();
				
                    // Since the first audio input will be set in the hidden track it is necessary to ensure that there is at least one
					// other audio input
					if (1 < session.Tracks.Count)
					{
						willCreateHiddenTrack = true;
					
                        // A hidden track contains the sectors from the start of the audio session (0) till the start of the first track
						// in that session (CDTrack::Start). When these are equal there is no hidden track. Now the first track in
						// the CDSession object will be removed - thus the second track will become first track for the AudioCD.
						session.Tracks.RemoveAt(0);
						
                        // The pregap start of the new first track must be set to 0 (the pregap start value of the original first track)
						// thus expanding the hidden section to the size fo the original first track
						session.Tracks[0].PregapStart = 0;
						
                        // After that all that is needed is to send the reconstructed CDSession to the AudioCD object
						audio.CDSession = session;
					}
				}

                // Setup CDText Properties
                if (m_device.CDFeatures.CanReadCDText && settings.WriteCDText)
                {
                    CDText cdtext = new CDText();

                    SetCDText(cdtext, settings.CDText.Album, 0);

					int cdTextItemN = 0;
                    for (int i = 0; i < settings.Files.Count; i++)
					{
						//If hidden track will be written on the target medium then the CD text might neeed to be updated - in this case,
						// since the entire first audio input will become the hidden track its CD text entry is ignored
						if (willCreateHiddenTrack && 0 == i)
						{
							continue;
						}
						SetCDText(cdtext, settings.CDText.Songs[i], cdTextItemN + 1);
						cdTextItemN++;
					}

                    audio.CDText = cdtext;
                }

                m_device.WriteSpeedKB = settings.WriteSpeedKB;
                audio.SimulateBurn = settings.Simulate;
                audio.CloseDisc = settings.CloseDisk;
                audio.WriteMethod = settings.WriteMethod;

                if (audio.WriteMethod != settings.WriteMethod)
                {
                    throw new BurnerException(BurnerErrors.INVALID_RECORDING_MODE);
                }

                bool res = audio.WriteToCD();
				if (!res)
                {
					throw new BurnerException(audio.Error);
                }

                m_device.Eject(settings.Eject);
            }
            finally
            {
                audio.Dispose();
            }
        }

        public void Erase(EraseSettings settings)
        {
            if (null == m_device)
                throw new BurnerException(BurnerErrors.NO_DEVICE);

            MediaProfile mp = m_device.MediaProfile;

            if (MediaProfile.DvdMinusRwSeq != mp && MediaProfile.DvdMinusRwRo != mp && MediaProfile.CdRw != mp)
                throw new BurnerException(BurnerErrors.ERASE_NOT_SUPPORTED);

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

        #endregion

        #region Device Event Handlers

        public void Device_Erase(Object sender, DeviceEraseEventArgs args)
        {
            if (null != EraseProgress)
                EraseProgress((int)args.Progress);
        }

        #endregion

        #region AudioCD Event Handlers
        public void AudioCD_OnStatus(Object sender, AudioCDStatusEventArgs args)
        {
            if (null == Status)
                return;

            Status(TranslateAudioCDStatus(args.Status));
        }

        public void AudioCD_OnTrackProgress(Object sender, AudioCDTrackStatusEventArgs args)
        {
            if (null == TrackProgress)
                return;

            TrackProgress(args.Track, args.PercentWritten);
        }

        public void AudioCD_OnProgress(Object sender, AudioCDProgressEventArgs args)
        {
            if (null == Progress)
                return;

            Progress(args.Position * 100 / args.All);
        }

        public void AudioCD_OnContinueBurn(Object sender, AudioCDContinueEventArgs args)
        {
            if (null == Continue)
                return;

            args.Continue = Continue();
        }
        #endregion

        #region Private Methods
        private string GetDeviceTitle(Device device)
        {
            return String.Format("({0}:) - {1}", device.DriveLetter, device.Description);
        }

        string TranslateAudioCDStatus(AudioCDStatus status)
        {

            switch (status)
            {
                case AudioCDStatus.Initializing:
                    return "Initializing...";

                case AudioCDStatus.InitializingDevice:
                    return "Initializing device...";

                case AudioCDStatus.DecodingAudio:
                    return "Decoding audio...";

                case AudioCDStatus.Writing:
                    return "Writing...";

                case AudioCDStatus.WritingLeadOut:
                    return "Writing lead-out...";

                default:
                    return "unexpected status";
            }


        }

        void SetCDText(PrimoSoftware.Burner.CDText cdtext, CDTextEntry cdt, int track)
        {
            if (cdtext == null)
                return;

            CDTextItem item = new CDTextItem() {
                Title = cdt.Title,
                Performer = cdt.Performer,
                Songwriter = cdt.SongWriter,
                Composer = cdt.Composer,
                Arranger = cdt.Arranger,
                Message = cdt.Message
            };

            if (0 == track) // album
            {
                item.DiskId = cdt.DiskId;
                item.Genre = (CDTextGenreCode) cdt.Genre;
				item.GenreText = cdt.GenreText;
            }

            item.UpcIsrc = cdt.UpcIsrc;

            cdtext.Items.Add(track, item);
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
