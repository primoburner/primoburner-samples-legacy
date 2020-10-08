using System;
using PrimoSoftware.Burner;


namespace MultiDataDisc
{
	// Burn Settings
	public class BurnSettings
	{
		public string SourceFolder;
		public string VolumeLabel;

		public PrimoSoftware.Burner.ImageType ImageType = ImageType.None;
		public PrimoSoftware.Burner.WriteMethod WriteMethod = WriteMethod.DvdIncremental; 
		public int WriteSpeedKB = 0;
	
		public bool Simulate = false;
		public bool CloseDisc = true;
		public bool Eject = true;
	};

    class WorkerThreadContext
    {
        public Device device;
        public ProgressForm progressForm;
        public ProgressInfo progressInfo;
        public int burnerIndex;
        public BurnSettings burnerSettings;
        public double WriteRate1xKB = -1;

        public void DataDisc_OnStatus(Object sender, DataDiscStatusEventArgs args)
        {
            progressInfo.Status = GetDataDiscStatusString(args.Status);
            progressForm.UpdateProgress(progressInfo, burnerIndex);
        }

        public void DataDisc_OnProgress(Object sender, DataDiscProgressEventArgs args)
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

        public void DataDisc_OnContinueBurn(object sender, DataDiscContinueEventArgs e)
        {
            e.Continue = !progressForm.Stopped;
        }

        static string GetDataDiscStatusString(DataDiscStatus status)
        {
            switch (status)
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

        public string MediaProfileString;

        public long MediaFreeSpace;

        public SpeedInfo SelectedWriteSpeed;

        public SpeedInfo MaxWriteSpeed;
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
