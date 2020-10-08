using System;
using System.IO;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

using PrimoSoftware.Burner;

namespace MultiDataDisc
{
	public class BurnerForm: System.Windows.Forms.Form
	{
        Engine m_engine;
        List<DeviceInfo> m_devicesInfo = new List<DeviceInfo>();

        List<WorkerThreadContext> m_workers;

        private const int WM_DEVICECHANGE = 0x0219;
        private long m_RequiredSpace = 0;
        System.Threading.Thread m_mainWorkerThread;

        private ProgressForm m_progressWindow;

        bool IsBurning()
        {
            return (null != m_mainWorkerThread);
        }

		public BurnerForm()
		{
			InitializeComponent();

            m_RequiredSpace = 0;

            // Write Method
            comboBoxRecordingMode.Items.Add(new ListItem(WriteMethod.DvdDao, "Disc-At-Once"));
            comboBoxRecordingMode.Items.Add(new ListItem(WriteMethod.DvdIncremental, "Incremental"));
            comboBoxRecordingMode.SelectedIndex = 1;

            // Image Types
            comboBoxImageType.Items.Add(new ListItem(ImageType.Iso9660, "ISO9660"));
            comboBoxImageType.Items.Add(new ListItem(ImageType.Joliet, "Joliet"));
            comboBoxImageType.Items.Add(new ListItem(ImageType.Udf, "UDF"));
            comboBoxImageType.Items.Add(new ListItem(ImageType.UdfIso, "UDF & ISO9660"));
            comboBoxImageType.Items.Add(new ListItem(ImageType.UdfJoliet, "UDF & Joliet"));
            comboBoxImageType.SelectedIndex = 2;

            // Write parameters
            checkBoxSimulate.Checked = false;
            checkBoxEjectWhenDone.Checked = false;
            checkBoxCloseDisc.Checked = false;
		}

        private void BurnerForm_Load(object sender, EventArgs e)
        {
            m_engine = new Engine();

            if (!m_engine.Initialize())
            {
                ShowError(m_engine.Error, "Engine.Initialize() failed.");
                this.Close();
                return;
            }

            UpdateRequiredSpace();
            UpdateDevicesInformation();
            UpdateUI();
        }

		protected override void Dispose( bool disposing)
		{
			if( disposing )
			{
				if (components != null) 
					components.Dispose();


                if (null != m_engine)
                {
                    m_engine.Shutdown();
                    m_engine.Dispose();
                }

                m_engine = null;
			}

			base.Dispose( disposing );
		}

		protected override void WndProc(ref Message msg)
		{
            if (WM_DEVICECHANGE == msg.Msg)
            {
                if (!IsBurning())
                {
                    //do not update device info while burning
                    UpdateDevicesInformation();
                }
            }

			base.WndProc(ref msg);
		}

        private void lvDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        void UpdateUI()
        {
            bool deviceSelected = lvDevices.SelectedItems.Count > 0;
            buttonChangeWriteSpeed.Enabled = deviceSelected;
            buttonCloseTray.Enabled = deviceSelected;
            buttonEject.Enabled = deviceSelected;
        }

        private void UpdateDevicesInformation()
        {
            Dictionary<int, SpeedInfo> selectedWriteSpeeds = new Dictionary<int, SpeedInfo>();
            foreach (DeviceInfo di in m_devicesInfo)
            {
                selectedWriteSpeeds[(int)di.DriveLetter] = di.SelectedWriteSpeed;
            }

            m_devicesInfo.Clear();

            using (DeviceEnumerator enumerator = m_engine.CreateDeviceEnumerator())
            {
                for (int i = 0; i < enumerator.Count; i++)
                {
                    Device device = enumerator.CreateDevice(i, false);
                    if (null != device)
                    {
                        if (IsWriterDevice(device))
                        {
                            DeviceInfo dev = new DeviceInfo();
                            dev.Index = i;
                            dev.DriveLetter = device.DriveLetter;
                            dev.Title = String.Format("({0}:) - {1}", device.DriveLetter, device.Description);
                            dev.MediaFreeSpace = device.MediaFreeSpace * (long)BlockSize.Dvd;
                            dev.MediaProfile = device.MediaProfile;
                            dev.MediaProfileString = GetMediaProfileString(device);

                            List<SpeedInfo> writeSpeeds = GetWriteSpeeds(device);

                            if (writeSpeeds.Count > 0)
                            {
                                dev.MaxWriteSpeed = writeSpeeds[0];
                            }

                            SpeedInfo selectedSpeed = null;

                            if (selectedWriteSpeeds.ContainsKey((int)dev.DriveLetter))
                                selectedSpeed = selectedWriteSpeeds[(int)dev.DriveLetter];

                            if ((null != selectedSpeed) && ContainsSpeed(writeSpeeds, selectedSpeed))
                            {
                                dev.SelectedWriteSpeed = selectedSpeed;
                            }

                            m_devicesInfo.Add(dev);
                        }

                        device.Dispose();
                    }
                }
            }

            UpdateDevicesView();
        }

        private void UpdateDevicesView()
        {
            if (lvDevices.Items.Count != m_devicesInfo.Count)
            {
                lvDevices.Items.Clear();
                for (int i = 0; i < m_devicesInfo.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvDevices.Items.Add(lvi);
                }
            }

            for (int i = 0; i < m_devicesInfo.Count; i++)
            {
                DeviceInfo devInfo = m_devicesInfo[i];
                ListViewItem liv = lvDevices.Items[i];
                liv.Tag = devInfo;
                liv.SubItems[0].Text = devInfo.Title;
                liv.SubItems[1].Text = devInfo.MediaProfileString;
                liv.SubItems[2].Text = String.Format("{0}GB", ((double)devInfo.MediaFreeSpace / (1e9)).ToString("0.00"));

                string writeSpeed = string.Empty;
                if (devInfo.SelectedWriteSpeed != null)
                {
                    writeSpeed = devInfo.SelectedWriteSpeed.ToString();
                }
                else if (devInfo.MaxWriteSpeed != null)
                {
                    writeSpeed = devInfo.MaxWriteSpeed.ToString() + " (default to Max)";
                }

                liv.SubItems[3].Text = writeSpeed;
            }
        }

        void UpdateRequiredSpace()
        {
            labelRequiredSpace.Text = String.Format("Required space : {0}GB", ((double)m_RequiredSpace / (1e9)).ToString("0.00"));
        }

        private void BurnerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsBurning())
            {
                e.Cancel = true;
                ShowError("Burning is in progress. The program cannot be closed.");
                return;
            }
        }

        private void buttonExit_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void comboBoxRecordingMode_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            SelectedRecordingModeChanged();
        }

        private void SelectedRecordingModeChanged()
        {
            if (-1 == comboBoxRecordingMode.SelectedIndex)
                return;

            ListItem item = (ListItem)comboBoxRecordingMode.SelectedItem;
            PrimoSoftware.Burner.WriteMethod writeMethod = (PrimoSoftware.Burner.WriteMethod)item.Value;

            if (PrimoSoftware.Burner.WriteMethod.DvdDao == writeMethod)
            {
                checkBoxCloseDisc.Checked = true;
                checkBoxCloseDisc.Enabled = false;
            }
            else if (PrimoSoftware.Burner.WriteMethod.DvdIncremental == writeMethod)
            {
                checkBoxCloseDisc.Enabled = true;
            }
        }

        private void buttonEject_Click(object sender, System.EventArgs e)
        {
            if (lvDevices.SelectedIndices.Count == 0)
                return;

            int deviceIndex = m_devicesInfo[lvDevices.SelectedIndices[0]].Index;

            using (DeviceEnumerator devEnum = m_engine.CreateDeviceEnumerator())
            {
                using (Device dev = devEnum.CreateDevice(deviceIndex, false))
                {
                    if (dev != null)
                    {
                        if (!dev.Eject(true))
                        {
                            ShowError(dev.Error, "Failed to eject device tray.");
                        }
                    }
                    else
                    {
                        ShowError(devEnum.Error, "Failed to create device.");
                    }
                }
            }
        }

        private void buttonCloseTray_Click(object sender, System.EventArgs e)
        {
            if (lvDevices.SelectedIndices.Count == 0)
                return;

            int deviceIndex = m_devicesInfo[lvDevices.SelectedIndices[0]].Index;

            using (DeviceEnumerator devEnum = m_engine.CreateDeviceEnumerator())
            {
                using (Device dev = devEnum.CreateDevice(deviceIndex, false))
                {
                    if (dev != null)
                    {
                        if (!dev.Eject(false))
                        {
                            ShowError(dev.Error, "Failed to close device tray.");
                        }
                    }
                    else
                    {
                        ShowError(devEnum.Error, "Failed to create device.");
                    }
                }
            }
        }

        private void buttonChangeWriteSpeed_Click(object sender, EventArgs e)
        {
            if (lvDevices.SelectedIndices.Count == 0)
                return;

            DeviceInfo di = m_devicesInfo[lvDevices.SelectedIndices[0]];

            using (DeviceEnumerator devEnum = m_engine.CreateDeviceEnumerator())
            {
                using (Device dev = devEnum.CreateDevice(di.Index, false))
                {
                    if (dev != null)
                    {
                        using (WriteSpeedsForm dlg = new WriteSpeedsForm())
                        {
                            dlg.WriterTitle = di.Title;
                            dlg.WriteSpeeds = GetWriteSpeeds(dev);
                            dlg.SelectedWriteSpeed = di.SelectedWriteSpeed;
                            if (dlg.ShowDialog() == DialogResult.OK)
                            {
                                di.SelectedWriteSpeed = dlg.SelectedWriteSpeed;
                                UpdateDevicesView();
                            }
                        }
                    }
                    else
                    {
                        ShowError(devEnum.Error, "Failed to create device.");
                    }
                }
            }
        }

		private void buttonBrowse_Click(object sender, System.EventArgs e)
		{
            string selectedPath;

            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (DialogResult.OK != folderBrowserDialog.ShowDialog())
                    return;

                selectedPath = folderBrowserDialog.SelectedPath;
            }

            ListItem item = (ListItem)comboBoxImageType.SelectedItem;
            PrimoSoftware.Burner.ImageType imageType = (PrimoSoftware.Burner.ImageType)item.Value;

            using(DataDisc dataDisc = new DataDisc())
            {
                dataDisc.ImageType = imageType;

                if(!dataDisc.SetImageLayoutFromFolder(selectedPath))
                {
                    ShowError(dataDisc.Error, "Failed to SetImageLayoutFromFolder.");
                    return;
                }

                m_RequiredSpace = dataDisc.ImageSizeInBytes;
            }

            textBoxRootDir.Text = selectedPath;
            UpdateRequiredSpace();
		}

		private void buttonBurn_Click(object sender, System.EventArgs e)
		{
			if (!ValidateSourceDirectory())
				return;

            List<DeviceInfo> selectedDevices = new List<DeviceInfo>();

            foreach (ListViewItem lvi in lvDevices.Items)
            {
                if (lvi.Checked)
                    selectedDevices.Add(m_devicesInfo[lvi.Index]);
            }

            if (selectedDevices.Count == 0)
            {
                ShowError("No device(s) selected for burning.");
                return;
            }

            foreach (DeviceInfo di in selectedDevices)
            {
                if (di.MediaFreeSpace < m_RequiredSpace)
                {
                    ShowError("Required space is greater than the free space on device: " + di.Title);
                    return;
                }
            }

            CreateProgressWindow();

            if (!PrepareWorkerThreadsResourses(selectedDevices))
            {
                DestroyProgressWindow();
                ReleaseWorkerThreadsResources();
                return;
            }

            m_mainWorkerThread = new Thread(MainWorkerThreadProc);
            m_mainWorkerThread.Start();
		}

        bool PrepareWorkerThreadsResourses(List<DeviceInfo> selectedDevices)
        {
            m_workers = new List<WorkerThreadContext>();

            using (DeviceEnumerator devEnum = m_engine.CreateDeviceEnumerator())
            {
                foreach (DeviceInfo di in selectedDevices)
                {
                    WorkerThreadContext ctx = new WorkerThreadContext();
                    ctx.burnerIndex = m_workers.Count;

                    m_workers.Add(ctx);

                    ctx.device = devEnum.CreateDevice(di.Index, true);
                    if (null == ctx.device)
                    {
                        ShowError(devEnum.Error, "Failed to create device");
                        return false;
                    }

                    ctx.progressForm = m_progressWindow;
                    ctx.progressInfo = new ProgressInfo();
                    ctx.progressInfo.DeviceTitle = di.Title;
                    ctx.burnerSettings = new BurnSettings();

                    // burn settings
                    {
                        ctx.burnerSettings.SourceFolder = textBoxRootDir.Text;
                        ctx.burnerSettings.VolumeLabel = textBoxVolumeName.Text;

                        ListItem item = (ListItem)comboBoxImageType.SelectedItem;
                        ctx.burnerSettings.ImageType = (PrimoSoftware.Burner.ImageType)item.Value;

                        if (ctx.device.MediaIsBD)
                        {
                            ctx.burnerSettings.WriteMethod = WriteMethod.BluRay;
                        }
                        else
                        {
                            item = (ListItem)comboBoxRecordingMode.SelectedItem;
                            ctx.burnerSettings.WriteMethod = (WriteMethod)item.Value;
                        }

                        if (di.SelectedWriteSpeed != null)
                        {
                            ctx.burnerSettings.WriteSpeedKB = di.SelectedWriteSpeed.TransferRateKB;
                        }
                        else if (di.MaxWriteSpeed != null)
                        {
                            ctx.burnerSettings.WriteSpeedKB = di.MaxWriteSpeed.TransferRateKB;
                        }

                        ctx.burnerSettings.Simulate = checkBoxSimulate.Checked;
                        ctx.burnerSettings.CloseDisc = checkBoxCloseDisc.Checked;
                        ctx.burnerSettings.Eject = checkBoxEjectWhenDone.Checked;
                    }
                }
            }

            return true;
        }

        void ReleaseWorkerThreadsResources()
        {
            foreach (WorkerThreadContext ctx in m_workers)
            {
                if (ctx.device != null)
                {
                    ctx.device.Dispose();
                    ctx.device = null;
                }
            }

            m_workers = null;
        }

        void MainWorkerThreadProc()
        {
            List<Thread> threads = new List<Thread>();

            // start burn threads 
            foreach (WorkerThreadContext ctx in m_workers)
            {
                Thread thread = new Thread(BurnWorkerThreadProc);
                thread.Start(ctx);
                threads.Add(thread);
            }

            // wait all threads to finish
            foreach (Thread thread in threads)
                thread.Join();

            // On thread complete
            {
                m_mainWorkerThread = null;

                MethodInvoker del = delegate
                {
                    ReleaseWorkerThreadsResources();
                    DestroyProgressWindow();
                    UpdateDevicesInformation();
                };

                UIThread(del);
            }
        }

        void BurnWorkerThreadProc(object obj)
        {
            WorkerThreadContext ctx = obj as WorkerThreadContext;

            using (DataDisc dataDisc = new DataDisc())
            {
                dataDisc.OnStatus += new EventHandler<DataDiscStatusEventArgs>(ctx.DataDisc_OnStatus);
                dataDisc.OnProgress += new EventHandler<DataDiscProgressEventArgs>(ctx.DataDisc_OnProgress);
                dataDisc.OnContinueBurn += new EventHandler<DataDiscContinueEventArgs>(ctx.DataDisc_OnContinueBurn);

                ctx.WriteRate1xKB = GetTransferRate1xKB(ctx.device);

                ctx.device.WriteSpeedKB = ctx.burnerSettings.WriteSpeedKB;

                FormatMedia(ctx.device);

                dataDisc.Device = ctx.device;
                dataDisc.SimulateBurn = ctx.burnerSettings.Simulate;
                dataDisc.WriteMethod = ctx.burnerSettings.WriteMethod;
                dataDisc.CloseDisc = ctx.burnerSettings.CloseDisc;

                dataDisc.SessionStartAddress = ctx.device.NewSessionStartAddress;

                // Set burning parameters
                dataDisc.ImageType = ctx.burnerSettings.ImageType;

                SetVolumeProperties(dataDisc, ctx.burnerSettings.VolumeLabel, DateTime.Now);

                if (!dataDisc.SetImageLayoutFromFolder(ctx.burnerSettings.SourceFolder))
                {
                    ShowError(dataDisc.Error, "Failed to SetImageLayoutFromFolder");

                    ctx.progressInfo.Status = "ERROR";
                    ctx.progressForm.UpdateProgress(ctx.progressInfo, ctx.burnerIndex);
                    return;
                }

                if (!dataDisc.WriteToDisc(true))
                {
                    ShowError(dataDisc.Error, "WriteToDisc failed.");

                    ctx.progressInfo.Status = "ERROR";
                    ctx.progressForm.UpdateProgress(ctx.progressInfo, ctx.burnerIndex);
                    return;
                }

                ctx.progressInfo.Status = "SUCCESS";
                ctx.progressForm.UpdateProgress(ctx.progressInfo, ctx.burnerIndex);

                if (ctx.burnerSettings.Eject)
                    ctx.device.Eject(true);
            }
        }

		private void CreateProgressWindow()
		{
			this.Enabled = false;
			
			// Create a progress window
			m_progressWindow = new ProgressForm();
			m_progressWindow.Owner = this;
			m_progressWindow.Show();
		}

		private void DestroyProgressWindow()
		{
			if(null != m_progressWindow)
			{
				m_progressWindow.Close();
				m_progressWindow = null;
			}

			this.Enabled = true;
		}

		private bool ValidateSourceDirectory()
		{
			string strRootDir = textBoxRootDir.Text;

			if (!Directory.Exists(strRootDir))
			{
				MessageBox.Show("Please specify a valid source directory.");
				textBoxRootDir.Focus();
				return false;
			}

			if ('\\' == strRootDir[strRootDir.Length -1] || 
				'/' == strRootDir[strRootDir.Length - 1])
			{
				strRootDir = strRootDir.Substring(0, strRootDir.Length - 1);
				textBoxRootDir.Text = strRootDir;
			}
		
			return true;
		}

        void ShowError(ErrorInfo errorInfo, string description)
        {
            string message = string.Empty;

            if (!string.IsNullOrEmpty(description))
            {
                message = description + "\r\n";
            }

            switch (errorInfo.Facility)
            {
                case ErrorFacility.SystemWindows:
                    message = new System.ComponentModel.Win32Exception(errorInfo.Code).Message;
                    break;

                case ErrorFacility.Success:
                    message = "Success";
                    break;

                case ErrorFacility.DataDisc:
                    message = string.Format("DataDisc error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message);
                    break;

                case ErrorFacility.Device:
                    message = string.Format("Device error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message);
                    break;

                default:
                    message = string.Format("Facility:{0} error :0x{1:x8}: {2}", errorInfo.Facility, errorInfo.Code, errorInfo.Message);
                    break;
            }

            ShowError(message);
        }

        void ShowError(string message)
        {
            MethodInvoker del = delegate
            {
                MessageBox.Show(message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            UIThread(del);
        }

        void UIThread(MethodInvoker code)
        {
            if (InvokeRequired)
            {
                Invoke(code);
                return;
            }

            code.Invoke();
        }

        #region Helpers
        static bool IsWriterDevice(Device device)
        {
            return device.DVDFeatures.CanWriteDVDMinusR || device.DVDFeatures.CanWriteDVDPlusR ||
                device.CDFeatures.CanWriteCDR || device.BDFeatures.CanWriteBDR;
        }

        static bool ContainsSpeed(List<SpeedInfo> speeds, SpeedInfo speed)
        {
            foreach (SpeedInfo si in speeds)
            {
                if ((si.TransferRateKB == speed.TransferRateKB) &&
                    (Math.Abs(si.TransferRate1xKB - speed.TransferRate1xKB) < 0.01))
                {
                    return true;
                }
            }

            return false;
        }

        static List<SpeedInfo> GetWriteSpeeds(Device device)
        {
            List<SpeedInfo> speedInfos = new List<SpeedInfo>();
            IList<SpeedDescriptor> speedDescriptors = device.GetWriteSpeeds();

            double speed1xKB;

            speed1xKB = GetTransferRate1xKB(device);

            foreach (SpeedDescriptor speed in speedDescriptors)
            {
                SpeedInfo speedInfo = new SpeedInfo();
                speedInfo.TransferRateKB = speed.TransferRateKB;
                speedInfo.TransferRate1xKB = speed1xKB;

                speedInfos.Add(speedInfo);
            }

            return speedInfos;
        }

        private static double GetTransferRate1xKB(Device device)
        {
            if (device.MediaIsBD)
            {
                return Speed1xKB.BD;
            }
            else if (device.MediaIsDVD)
            {
                return Speed1xKB.DVD;
            }
            else
            {
                return Speed1xKB.CD;
            }
        }

        static string GetMediaProfileString(Device device)
        {
            PrimoSoftware.Burner.MediaProfile profile = device.MediaProfile;
            switch (profile)
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
                    return "DVD-RW Restricted Overwrite ReWritable.";

                case MediaProfile.DvdMinusRwSeq:
                    return "DVD-RW Sequential Recording ReWritable.";

                case MediaProfile.DvdPlusRw:
                    {
                        BgFormatStatus fmt = device.BgFormatStatus;
                        switch (fmt)
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

                case MediaProfile.BdRom:
                    return "BD-ROM Read only Blu-ray Disc.";

                case MediaProfile.BdRSrm:
                    return "BD-R for Sequential Recording.";

                case MediaProfile.BdRSrmPow:
                    return "BD-R for Sequential Recording with Pseudo-Overwrite.";

                case MediaProfile.BdRRrm:
                    return "BD-R Random Recording Mode (RRM).";

                case MediaProfile.BdRe:
                    {
                        if (device.MediaIsFormatted)
                            return "BD-RE ReWritable Blu-ray Disc. Formatted.";

                        return "BD-RE ReWritable Blu-ray Disc. Blank. Not formatted.";
                    }

                default:
                    return "Unknown Profile.";
            }
        }

        bool FormatMedia(Device dev)
        {
            switch (dev.MediaProfile)
            {
                // DVD+RW (needs to be formatted before the disc can be used)
                case MediaProfile.DvdPlusRw:
                    {
                        switch (dev.BgFormatStatus)
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

                // BD-RE (needs to be formatted before the disc can be used)
                case MediaProfile.BdRe:
                    {
                        if(!dev.MediaIsFormatted)
                            dev.FormatBD(BDFormatType.BdFull, BDFormatSubType.BdReQuickReformat);
                    }
                    break;
            }

            return true;
        }

        static void SetVolumeProperties(DataDisc data, string volumeLabel, DateTime creationTime)
        {
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
        #endregion

        #region Windows Form Designer generated code

        private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label labelRequiredSpace;
		private System.Windows.Forms.TextBox textBoxVolumeName;
		private System.Windows.Forms.ComboBox comboBoxImageType;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ComboBox comboBoxRecordingMode;
        private System.Windows.Forms.Label label7;
		private System.Windows.Forms.CheckBox checkBoxCloseDisc;
        private System.Windows.Forms.CheckBox checkBoxSimulate;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.Button buttonEject;
        private System.Windows.Forms.Button buttonCloseTray;
        private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Button buttonBurn;
        private System.Windows.Forms.TextBox textBoxRootDir;
        private System.Windows.Forms.CheckBox checkBoxEjectWhenDone;
        private ListView lvDevices;
        private ColumnHeader columnHeaderDeviceTitle;
        private ColumnHeader columnHeaderMedia;
        private ColumnHeader columnHeaderFreeSpace;
        private ColumnHeader columnHeaderWriteSpeed;
        private System.Windows.Forms.Button buttonChangeWriteSpeed;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.labelRequiredSpace = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxRootDir = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxVolumeName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxImageType = new System.Windows.Forms.ComboBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.comboBoxRecordingMode = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.checkBoxCloseDisc = new System.Windows.Forms.CheckBox();
            this.checkBoxEjectWhenDone = new System.Windows.Forms.CheckBox();
            this.checkBoxSimulate = new System.Windows.Forms.CheckBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.buttonEject = new System.Windows.Forms.Button();
            this.buttonCloseTray = new System.Windows.Forms.Button();
            this.buttonBurn = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.lvDevices = new System.Windows.Forms.ListView();
            this.columnHeaderDeviceTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderMedia = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderFreeSpace = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderWriteSpeed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonChangeWriteSpeed = new System.Windows.Forms.Button();
            this.groupBox4.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelRequiredSpace
            // 
            this.labelRequiredSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelRequiredSpace.Location = new System.Drawing.Point(7, 223);
            this.labelRequiredSpace.Name = "labelRequiredSpace";
            this.labelRequiredSpace.Size = new System.Drawing.Size(488, 21);
            this.labelRequiredSpace.TabIndex = 3;
            this.labelRequiredSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(7, 259);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 16);
            this.label4.TabIndex = 4;
            this.label4.Text = "Source Folder:";
            // 
            // textBoxRootDir
            // 
            this.textBoxRootDir.Location = new System.Drawing.Point(95, 259);
            this.textBoxRootDir.Name = "textBoxRootDir";
            this.textBoxRootDir.ReadOnly = true;
            this.textBoxRootDir.Size = new System.Drawing.Size(400, 20);
            this.textBoxRootDir.TabIndex = 5;
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(503, 259);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(80, 24);
            this.buttonBrowse.TabIndex = 6;
            this.buttonBrowse.Text = "Browse";
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(7, 297);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 16);
            this.label5.TabIndex = 7;
            this.label5.Text = "Volume name :";
            // 
            // textBoxVolumeName
            // 
            this.textBoxVolumeName.Location = new System.Drawing.Point(95, 295);
            this.textBoxVolumeName.MaxLength = 16;
            this.textBoxVolumeName.Name = "textBoxVolumeName";
            this.textBoxVolumeName.Size = new System.Drawing.Size(136, 20);
            this.textBoxVolumeName.TabIndex = 8;
            this.textBoxVolumeName.Text = "DATADVD";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(255, 297);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(72, 16);
            this.label6.TabIndex = 9;
            this.label6.Text = "Image Type:";
            // 
            // comboBoxImageType
            // 
            this.comboBoxImageType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxImageType.Location = new System.Drawing.Point(335, 295);
            this.comboBoxImageType.Name = "comboBoxImageType";
            this.comboBoxImageType.Size = new System.Drawing.Size(160, 21);
            this.comboBoxImageType.TabIndex = 10;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.comboBoxRecordingMode);
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.checkBoxCloseDisc);
            this.groupBox4.Controls.Add(this.checkBoxEjectWhenDone);
            this.groupBox4.Controls.Add(this.checkBoxSimulate);
            this.groupBox4.Location = new System.Drawing.Point(7, 323);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(576, 75);
            this.groupBox4.TabIndex = 16;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Parameters";
            // 
            // comboBoxRecordingMode
            // 
            this.comboBoxRecordingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRecordingMode.ItemHeight = 13;
            this.comboBoxRecordingMode.Location = new System.Drawing.Point(105, 30);
            this.comboBoxRecordingMode.Name = "comboBoxRecordingMode";
            this.comboBoxRecordingMode.Size = new System.Drawing.Size(121, 21);
            this.comboBoxRecordingMode.TabIndex = 19;
            this.comboBoxRecordingMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxRecordingMode_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(10, 31);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 16);
            this.label7.TabIndex = 17;
            this.label7.Text = "Recording Mode:";
            // 
            // checkBoxCloseDisc
            // 
            this.checkBoxCloseDisc.Location = new System.Drawing.Point(245, 31);
            this.checkBoxCloseDisc.Name = "checkBoxCloseDisc";
            this.checkBoxCloseDisc.Size = new System.Drawing.Size(76, 24);
            this.checkBoxCloseDisc.TabIndex = 15;
            this.checkBoxCloseDisc.Text = "Close Disc";
            // 
            // checkBoxEjectWhenDone
            // 
            this.checkBoxEjectWhenDone.Location = new System.Drawing.Point(334, 31);
            this.checkBoxEjectWhenDone.Name = "checkBoxEjectWhenDone";
            this.checkBoxEjectWhenDone.Size = new System.Drawing.Size(121, 24);
            this.checkBoxEjectWhenDone.TabIndex = 14;
            this.checkBoxEjectWhenDone.Text = "Eject When Done";
            // 
            // checkBoxSimulate
            // 
            this.checkBoxSimulate.Location = new System.Drawing.Point(464, 31);
            this.checkBoxSimulate.Name = "checkBoxSimulate";
            this.checkBoxSimulate.Size = new System.Drawing.Size(104, 24);
            this.checkBoxSimulate.TabIndex = 13;
            this.checkBoxSimulate.Text = "Simulate";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.buttonEject);
            this.groupBox6.Controls.Add(this.buttonCloseTray);
            this.groupBox6.Location = new System.Drawing.Point(7, 149);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(196, 59);
            this.groupBox6.TabIndex = 25;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Device Tray";
            // 
            // buttonEject
            // 
            this.buttonEject.Location = new System.Drawing.Point(22, 19);
            this.buttonEject.Name = "buttonEject";
            this.buttonEject.Size = new System.Drawing.Size(75, 24);
            this.buttonEject.TabIndex = 23;
            this.buttonEject.Text = "E&ject";
            this.buttonEject.Click += new System.EventHandler(this.buttonEject_Click);
            // 
            // buttonCloseTray
            // 
            this.buttonCloseTray.Location = new System.Drawing.Point(109, 19);
            this.buttonCloseTray.Name = "buttonCloseTray";
            this.buttonCloseTray.Size = new System.Drawing.Size(75, 24);
            this.buttonCloseTray.TabIndex = 22;
            this.buttonCloseTray.Text = "&Close";
            this.buttonCloseTray.Click += new System.EventHandler(this.buttonCloseTray_Click);
            // 
            // buttonBurn
            // 
            this.buttonBurn.Location = new System.Drawing.Point(7, 410);
            this.buttonBurn.Name = "buttonBurn";
            this.buttonBurn.Size = new System.Drawing.Size(104, 24);
            this.buttonBurn.TabIndex = 28;
            this.buttonBurn.Text = "Burn";
            this.buttonBurn.Click += new System.EventHandler(this.buttonBurn_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(479, 410);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(104, 24);
            this.buttonExit.TabIndex = 31;
            this.buttonExit.Text = "Exit";
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // lvDevices
            // 
            this.lvDevices.CheckBoxes = true;
            this.lvDevices.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderDeviceTitle,
            this.columnHeaderMedia,
            this.columnHeaderFreeSpace,
            this.columnHeaderWriteSpeed});
            this.lvDevices.FullRowSelect = true;
            this.lvDevices.GridLines = true;
            this.lvDevices.HideSelection = false;
            this.lvDevices.Location = new System.Drawing.Point(8, 12);
            this.lvDevices.MultiSelect = false;
            this.lvDevices.Name = "lvDevices";
            this.lvDevices.Size = new System.Drawing.Size(575, 132);
            this.lvDevices.TabIndex = 33;
            this.lvDevices.UseCompatibleStateImageBehavior = false;
            this.lvDevices.View = System.Windows.Forms.View.Details;
            this.lvDevices.SelectedIndexChanged += new System.EventHandler(this.lvDevices_SelectedIndexChanged);
            // 
            // columnHeaderDeviceTitle
            // 
            this.columnHeaderDeviceTitle.Text = "Device";
            this.columnHeaderDeviceTitle.Width = 222;
            // 
            // columnHeaderMedia
            // 
            this.columnHeaderMedia.Text = "Media";
            this.columnHeaderMedia.Width = 108;
            // 
            // columnHeaderFreeSpace
            // 
            this.columnHeaderFreeSpace.Text = "Free Space";
            this.columnHeaderFreeSpace.Width = 107;
            // 
            // columnHeaderWriteSpeed
            // 
            this.columnHeaderWriteSpeed.Text = "Write Speed";
            this.columnHeaderWriteSpeed.Width = 108;
            // 
            // buttonChangeWriteSpeed
            // 
            this.buttonChangeWriteSpeed.Location = new System.Drawing.Point(449, 168);
            this.buttonChangeWriteSpeed.Name = "buttonChangeWriteSpeed";
            this.buttonChangeWriteSpeed.Size = new System.Drawing.Size(134, 24);
            this.buttonChangeWriteSpeed.TabIndex = 34;
            this.buttonChangeWriteSpeed.Text = "Change Write Speed";
            this.buttonChangeWriteSpeed.Click += new System.EventHandler(this.buttonChangeWriteSpeed_Click);
            // 
            // BurnerForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(597, 450);
            this.Controls.Add(this.buttonChangeWriteSpeed);
            this.Controls.Add(this.lvDevices);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonBurn);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.comboBoxImageType);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBoxVolumeName);
            this.Controls.Add(this.textBoxRootDir);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.labelRequiredSpace);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BurnerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PrimoBurner(tm) Engine for .NET - MultiDataDisc - Burning Sample Application";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BurnerForm_FormClosing);
            this.Load += new System.EventHandler(this.BurnerForm_Load);
            this.groupBox4.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion
	}
}
