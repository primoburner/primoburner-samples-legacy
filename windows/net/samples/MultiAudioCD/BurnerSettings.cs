using System;
using System.Collections.Generic;
using PrimoSoftware.Burner;


namespace MultiAudioCD
{
    // Burn Settings
    public class BurnSettings
    {
        public PrimoSoftware.Burner.WriteMethod WriteMethod = WriteMethod.Sao;
        public int WriteSpeedKB = 0;

        public bool Simulate = false;
        public bool CloseDisc = true;
        public bool Eject = true;
        public bool WriteCDText = false;
        public bool DecodeInTempFiles = false;
        public List<string> Files = new List<string>(10);
        public CDTextSettings CDText = new CDTextSettings();

        public bool QuickErase = true;
    };

    // CD-Text Record
    public class CDTextEntry
    {
        //NOTE: non-null strings must be passed to PrimoBurner if CD-TEXT is used
        public string Title = string.Empty;
        public string Performer = string.Empty;
        public string SongWriter = string.Empty;
        public string Composer = string.Empty;
        public string Arranger = string.Empty;
        public string Message = string.Empty;
        public string DiskId = string.Empty;
        public int Genre = -1;
        public string GenreText = string.Empty;
        public string UpcIsrc = string.Empty;
    };

    // CD-Text Settings
    public class CDTextSettings
    {
        public CDTextEntry Album = new CDTextEntry();
        public CDTextEntry[] Songs = new CDTextEntry[99];
    }


    class WorkerThreadContext
    {
        public Device device;
        public ProgressForm progressForm;
        public ProgressInfo progressInfo;
        public int burnerIndex;
        public BurnSettings burnerSettings;
        public double WriteRate1xKB = -1;

        public void AudioCD_OnStatus(Object sender, AudioCDStatusEventArgs args)
        {
            progressInfo.Status = TranslateAudioCDStatus(args.Status);
            progressForm.UpdateProgress(progressInfo, burnerIndex);
        }

        public void AudioCD_OnProgress(Object sender, AudioCDProgressEventArgs args)
        {
            if (args.All > 0)
            {
                double progress = 100 * (double)args.Position / (double)args.All;

                if ((progress - progressInfo.Progress) > 0.1)
                {
                    progressInfo.ProgressStr = string.Format("{0:0.0}%", progress);
                    progressInfo.Progress = progress;

                    if (WriteRate1xKB > 0)
                    {
                        progressInfo.WriteSpeed = string.Format("{0:0.0}x", (double)device.WriteTransferRate / WriteRate1xKB);
                    }

                    progressForm.UpdateProgress(progressInfo, burnerIndex);
                }
            }
        }

        public void AudioCD_OnContinueBurn(Object sender, AudioCDContinueEventArgs args)
        {
            args.Continue = !progressForm.Stopped;
        }

        public void Device_OnErase(object sender, DeviceEraseEventArgs e)
        {
            double progress = e.Progress;

            if ((progress - progressInfo.Progress) > 0.1)
            {
                progressInfo.ProgressStr = string.Format("{0:0.0}%", progress);
                progressInfo.Progress = progress;
                progressForm.UpdateProgress(progressInfo, burnerIndex);
            }
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

                case AudioCDStatus.Reading:
                    return "Reading...";

                default:
                    return "unexpected status";
            }
        }
    }


    class ListItem
    {
        public object Value;
        public string Description;

        public ListItem(object nvalue, string description)
        {
            Value = nvalue;
            Description = description;
        }

        public override string ToString()
        {
            return Description;
        }
    }


    public class SpeedInfo
    {
        public int TransferRateKB;
        public double TransferRate1xKB;
        public override string ToString()
        {
            return string.Format("{0}x", Math.Round((double)TransferRateKB / TransferRate1xKB, 1));
        }
    };

    /// <summary>
    /// Container for device information
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Device index in DeviceEnumerator
        /// </summary>
        public int Index;

        public char DriveLetter;

        public string Title;

        public MediaProfile MediaProfile;

        public long MediaFreeSpace;

        public SpeedInfo SelectedWriteSpeed;

        public SpeedInfo MaxWriteSpeed;

        public bool MediaIsBlank;

        public bool MediaIsCD;

        public bool MediaPresent;
    };

    public class ProgressInfo
    {
        public string DeviceTitle = string.Empty;
        public string Status = string.Empty;
        public string ProgressStr = string.Empty;
        public double Progress = 0.0;
        public string WriteSpeed = string.Empty;
    }
}
