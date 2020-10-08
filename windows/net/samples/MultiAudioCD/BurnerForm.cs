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

namespace MultiAudioCD
{
	public class BurnerForm: System.Windows.Forms.Form
	{
        Engine m_engine;
        List<DeviceInfo> m_devicesInfo = new List<DeviceInfo>();
        List<WorkerThreadContext> m_workers;
        private CDTextEntry m_albumCDText = new CDTextEntry();
        private const int WM_DEVICECHANGE = 0x0219;
        System.Threading.Thread m_mainWorkerThread;
        private ProgressForm m_progressWindow;

        #region Windows Form Designer generated code

        private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.Button buttonEject;
        private System.Windows.Forms.Button buttonCloseTray;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Button buttonBurn;
        private ListView lvDevices;
        private ColumnHeader columnHeaderDeviceTitle;
        private ColumnHeader columnHeaderMedia;
        private ColumnHeader columnHeaderWriteSpeed;
        private System.Windows.Forms.Button buttonChangeWriteSpeed;
        private ComboBox comboBoxRecordingMode;
        private CheckBox checkDecodeInTempFiles;
        private Label label1;
        private GroupBox groupBox3;
        private Button buttonAlbumCDText;
        private Button buttonTrackCDText;
        private CheckBox checkBoxCDText;
        private ListBox lbPlaylist;
        private CheckBox checkBoxCloseDisc;
        private CheckBox checkBoxQuickErase;
        private Button buttonRemoveFiles;
        private CheckBox checkBoxEjectWhenDone;
        private GroupBox groupBoxErase;
        private Button buttonEraseDisc;
        private CheckBox checkBoxSimulate;
        private Button buttonAddFiles;
        private GroupBox groupBox2;

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
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.buttonEject = new System.Windows.Forms.Button();
            this.buttonCloseTray = new System.Windows.Forms.Button();
            this.buttonBurn = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.lvDevices = new System.Windows.Forms.ListView();
            this.columnHeaderDeviceTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderMedia = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderWriteSpeed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonChangeWriteSpeed = new System.Windows.Forms.Button();
            this.comboBoxRecordingMode = new System.Windows.Forms.ComboBox();
            this.checkDecodeInTempFiles = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.buttonAlbumCDText = new System.Windows.Forms.Button();
            this.buttonTrackCDText = new System.Windows.Forms.Button();
            this.checkBoxCDText = new System.Windows.Forms.CheckBox();
            this.lbPlaylist = new System.Windows.Forms.ListBox();
            this.checkBoxCloseDisc = new System.Windows.Forms.CheckBox();
            this.checkBoxQuickErase = new System.Windows.Forms.CheckBox();
            this.buttonRemoveFiles = new System.Windows.Forms.Button();
            this.checkBoxEjectWhenDone = new System.Windows.Forms.CheckBox();
            this.groupBoxErase = new System.Windows.Forms.GroupBox();
            this.buttonEraseDisc = new System.Windows.Forms.Button();
            this.checkBoxSimulate = new System.Windows.Forms.CheckBox();
            this.buttonAddFiles = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox6.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBoxErase.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
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
            this.buttonBurn.Location = new System.Drawing.Point(9, 621);
            this.buttonBurn.Name = "buttonBurn";
            this.buttonBurn.Size = new System.Drawing.Size(104, 24);
            this.buttonBurn.TabIndex = 28;
            this.buttonBurn.Text = "Burn";
            this.buttonBurn.Click += new System.EventHandler(this.buttonBurn_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(481, 621);
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
            this.columnHeaderMedia.Width = 223;
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
            // comboBoxRecordingMode
            // 
            this.comboBoxRecordingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRecordingMode.ItemHeight = 13;
            this.comboBoxRecordingMode.Location = new System.Drawing.Point(229, 22);
            this.comboBoxRecordingMode.Name = "comboBoxRecordingMode";
            this.comboBoxRecordingMode.Size = new System.Drawing.Size(120, 21);
            this.comboBoxRecordingMode.TabIndex = 19;
            // 
            // checkDecodeInTempFiles
            // 
            this.checkDecodeInTempFiles.Location = new System.Drawing.Point(125, 48);
            this.checkDecodeInTempFiles.Name = "checkDecodeInTempFiles";
            this.checkDecodeInTempFiles.Size = new System.Drawing.Size(170, 24);
            this.checkDecodeInTempFiles.TabIndex = 20;
            this.checkDecodeInTempFiles.Text = "Decode audio in temp files";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(125, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 13);
            this.label1.TabIndex = 17;
            this.label1.Text = "Recording Mode:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.buttonAlbumCDText);
            this.groupBox3.Controls.Add(this.buttonTrackCDText);
            this.groupBox3.Controls.Add(this.checkBoxCDText);
            this.groupBox3.Location = new System.Drawing.Point(11, 435);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(335, 68);
            this.groupBox3.TabIndex = 41;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "CD-TEXT";
            // 
            // buttonAlbumCDText
            // 
            this.buttonAlbumCDText.Location = new System.Drawing.Point(223, 23);
            this.buttonAlbumCDText.Name = "buttonAlbumCDText";
            this.buttonAlbumCDText.Size = new System.Drawing.Size(95, 28);
            this.buttonAlbumCDText.TabIndex = 2;
            this.buttonAlbumCDText.Text = "Album CD-Text";
            this.buttonAlbumCDText.UseVisualStyleBackColor = true;
            this.buttonAlbumCDText.Click += new System.EventHandler(this.buttonAlbumCDText_Click);
            // 
            // buttonTrackCDText
            // 
            this.buttonTrackCDText.Location = new System.Drawing.Point(122, 23);
            this.buttonTrackCDText.Name = "buttonTrackCDText";
            this.buttonTrackCDText.Size = new System.Drawing.Size(96, 28);
            this.buttonTrackCDText.TabIndex = 1;
            this.buttonTrackCDText.Text = "Track CD-Text";
            this.buttonTrackCDText.UseVisualStyleBackColor = true;
            this.buttonTrackCDText.Click += new System.EventHandler(this.buttonTrackCDText_Click);
            // 
            // checkBoxCDText
            // 
            this.checkBoxCDText.AutoSize = true;
            this.checkBoxCDText.Location = new System.Drawing.Point(16, 29);
            this.checkBoxCDText.Name = "checkBoxCDText";
            this.checkBoxCDText.Size = new System.Drawing.Size(100, 17);
            this.checkBoxCDText.TabIndex = 0;
            this.checkBoxCDText.Text = "Write CD-TEXT";
            this.checkBoxCDText.UseVisualStyleBackColor = true;
            this.checkBoxCDText.CheckedChanged += new System.EventHandler(this.checkBoxCDText_CheckedChanged);
            // 
            // lbPlaylist
            // 
            this.lbPlaylist.FormattingEnabled = true;
            this.lbPlaylist.Location = new System.Drawing.Point(11, 266);
            this.lbPlaylist.Name = "lbPlaylist";
            this.lbPlaylist.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbPlaylist.Size = new System.Drawing.Size(573, 160);
            this.lbPlaylist.TabIndex = 40;
            // 
            // checkBoxCloseDisc
            // 
            this.checkBoxCloseDisc.Checked = true;
            this.checkBoxCloseDisc.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCloseDisc.Location = new System.Drawing.Point(393, 20);
            this.checkBoxCloseDisc.Name = "checkBoxCloseDisc";
            this.checkBoxCloseDisc.Size = new System.Drawing.Size(103, 24);
            this.checkBoxCloseDisc.TabIndex = 15;
            this.checkBoxCloseDisc.Text = "Close Disc";
            // 
            // checkBoxQuickErase
            // 
            this.checkBoxQuickErase.Checked = true;
            this.checkBoxQuickErase.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxQuickErase.Location = new System.Drawing.Point(13, 18);
            this.checkBoxQuickErase.Name = "checkBoxQuickErase";
            this.checkBoxQuickErase.Size = new System.Drawing.Size(56, 25);
            this.checkBoxQuickErase.TabIndex = 25;
            this.checkBoxQuickErase.Text = "Quick";
            // 
            // buttonRemoveFiles
            // 
            this.buttonRemoveFiles.Location = new System.Drawing.Point(117, 236);
            this.buttonRemoveFiles.Name = "buttonRemoveFiles";
            this.buttonRemoveFiles.Size = new System.Drawing.Size(105, 24);
            this.buttonRemoveFiles.TabIndex = 39;
            this.buttonRemoveFiles.Text = "Remove Files";
            this.buttonRemoveFiles.Click += new System.EventHandler(this.buttonRemoveFiles_Click);
            // 
            // checkBoxEjectWhenDone
            // 
            this.checkBoxEjectWhenDone.Location = new System.Drawing.Point(16, 48);
            this.checkBoxEjectWhenDone.Name = "checkBoxEjectWhenDone";
            this.checkBoxEjectWhenDone.Size = new System.Drawing.Size(119, 24);
            this.checkBoxEjectWhenDone.TabIndex = 14;
            this.checkBoxEjectWhenDone.Text = "Eject when done";
            // 
            // groupBoxErase
            // 
            this.groupBoxErase.Controls.Add(this.checkBoxQuickErase);
            this.groupBoxErase.Controls.Add(this.buttonEraseDisc);
            this.groupBoxErase.Location = new System.Drawing.Point(228, 150);
            this.groupBoxErase.Name = "groupBoxErase";
            this.groupBoxErase.Size = new System.Drawing.Size(172, 58);
            this.groupBoxErase.TabIndex = 37;
            this.groupBoxErase.TabStop = false;
            this.groupBoxErase.Text = "Erase";
            // 
            // buttonEraseDisc
            // 
            this.buttonEraseDisc.Location = new System.Drawing.Point(78, 17);
            this.buttonEraseDisc.Name = "buttonEraseDisc";
            this.buttonEraseDisc.Size = new System.Drawing.Size(75, 25);
            this.buttonEraseDisc.TabIndex = 26;
            this.buttonEraseDisc.Text = "E&rase";
            this.buttonEraseDisc.Click += new System.EventHandler(this.buttonEraseDisc_Click);
            // 
            // checkBoxSimulate
            // 
            this.checkBoxSimulate.Location = new System.Drawing.Point(16, 20);
            this.checkBoxSimulate.Name = "checkBoxSimulate";
            this.checkBoxSimulate.Size = new System.Drawing.Size(104, 24);
            this.checkBoxSimulate.TabIndex = 13;
            this.checkBoxSimulate.Text = "Simulate";
            // 
            // buttonAddFiles
            // 
            this.buttonAddFiles.Location = new System.Drawing.Point(8, 236);
            this.buttonAddFiles.Name = "buttonAddFiles";
            this.buttonAddFiles.Size = new System.Drawing.Size(104, 24);
            this.buttonAddFiles.TabIndex = 38;
            this.buttonAddFiles.Text = "Add Audio Files";
            this.buttonAddFiles.Click += new System.EventHandler(this.buttonAddFiles_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkDecodeInTempFiles);
            this.groupBox2.Controls.Add(this.comboBoxRecordingMode);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.checkBoxCloseDisc);
            this.groupBox2.Controls.Add(this.checkBoxEjectWhenDone);
            this.groupBox2.Controls.Add(this.checkBoxSimulate);
            this.groupBox2.Location = new System.Drawing.Point(8, 508);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(575, 86);
            this.groupBox2.TabIndex = 36;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Parameters";
            // 
            // BurnerForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(597, 666);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.lbPlaylist);
            this.Controls.Add(this.buttonRemoveFiles);
            this.Controls.Add(this.groupBoxErase);
            this.Controls.Add(this.buttonAddFiles);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.buttonChangeWriteSpeed);
            this.Controls.Add(this.lvDevices);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonBurn);
            this.Controls.Add(this.groupBox6);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BurnerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PrimoBurner(tm) Engine for .NET - MultiAudioCD - Burning Sample Application";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BurnerForm_FormClosing);
            this.Load += new System.EventHandler(this.BurnerForm_Load);
            this.groupBox6.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBoxErase.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

		}
		#endregion

        #region Helpers
        static bool IsWriterDevice(Device device)
        {
            return device.CDFeatures.CanWriteCDR || device.CDFeatures.CanWriteCDRW;
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

            double speed1xKB = Speed1xKB.CD;

            foreach (SpeedDescriptor speed in speedDescriptors)
            {
                SpeedInfo speedInfo = new SpeedInfo();
                speedInfo.TransferRateKB = speed.TransferRateKB;
                speedInfo.TransferRate1xKB = speed1xKB;

                speedInfos.Add(speedInfo);
            }

            return speedInfos;
        }
        #endregion

        bool IsBurningInProgress()
        {
            return (null != m_mainWorkerThread);
        }

        public BurnerForm()
        {
            InitializeComponent();

            this.Text += IntPtr.Size == 8 ? " 64-bit" : " 32-bit";

            // Write Method
            comboBoxRecordingMode.Items.Add(new ListItem(WriteMethod.Sao, "Session-At-Once"));
            comboBoxRecordingMode.Items.Add(new ListItem(WriteMethod.Tao, "Track-At-Once"));
            comboBoxRecordingMode.SelectedIndex = 0;
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

            UpdateDevicesInformation();
            UpdateUI();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
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

            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message msg)
        {
            if (WM_DEVICECHANGE == msg.Msg)
            {
                if (!IsBurningInProgress())
                {
                    //do not update device info while burning
                    UpdateDevicesInformation();
                }
            }

            base.WndProc(ref msg);
        }

        private void checkBoxCDText_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void buttonAlbumCDText_Click(object sender, EventArgs e)
        {
            using (CDTextForm dlg = new CDTextForm())
            {
                dlg.EditAlbum = true;
                dlg.CDText = m_albumCDText;

                if (DialogResult.OK == dlg.ShowDialog())
                {
                    m_albumCDText = dlg.CDText;
                }
            }
        }

        private void buttonTrackCDText_Click(object sender, EventArgs e)
        {
            int cursel = lbPlaylist.SelectedIndex;
            if ((-1 == cursel) || (0 == lbPlaylist.Items.Count))
            {
                MessageBox.Show("To edit the CDText information of a track select the track from the track list and click on this button again.");
                return;
            }

            if (cursel >= 99)
            {
                MessageBox.Show("CDText information is supported for up to 99 tracks only.");
                return;
            }

            using (CDTextForm dlg = new CDTextForm())
            {
                dlg.EditAlbum = false;
                ListItem item = (ListItem)lbPlaylist.Items[cursel];
                CDTextEntry cdt = (CDTextEntry)item.Value;
                if (cdt != null)
                    dlg.CDText = cdt;

                if (DialogResult.OK == dlg.ShowDialog())
                {
                    item.Value = dlg.CDText;
                }
            }
        }

        private void buttonAddFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Wave Files (*.wav)|*.wav|All Files (*.*)|*.*";
                openFileDialog.Title = "Add audio files";
                openFileDialog.CheckFileExists = true;
                openFileDialog.Multiselect = true;

                if (DialogResult.OK == openFileDialog.ShowDialog())
                {
                    foreach (string filename in openFileDialog.FileNames)
                    {
                        if (lbPlaylist.Items.Count >= 99)
                            break;

                        ListItem item = new ListItem(new CDTextEntry(), filename);
                        lbPlaylist.Items.Add(item);
                    }
                }
            }
        }

        private void buttonRemoveFiles_Click(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection selected = lbPlaylist.SelectedIndices;

            int count = selected.Count;

            for (int i = count - 1; i >= 0; i--)
            {
                lbPlaylist.Items.RemoveAt(selected[i]);
            }
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

            buttonTrackCDText.Enabled = checkBoxCDText.Checked;
            buttonAlbumCDText.Enabled = checkBoxCDText.Checked;

            groupBoxErase.Enabled = deviceSelected;
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
            if (null != m_progressWindow)
            {
                m_progressWindow.Close();
                m_progressWindow = null;
            }

            this.Enabled = true;
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

                case ErrorFacility.AudioCD:
                    message = string.Format("AudioCD error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message);
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
                            dev.MediaFreeSpace = device.MediaFreeSpace;
                            dev.MediaProfile = device.MediaProfile;
                            dev.MediaIsBlank = device.MediaIsBlank;
                            dev.MediaIsCD = device.MediaIsCD;
                            dev.MediaPresent = device.MediaState == MediaReady.Present;

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

                string strMedia = string.Empty;

                if (!devInfo.MediaPresent)
                {
                    strMedia = "media not present";
                }
                else
                {
                    if (!devInfo.MediaIsCD)
                    {
                        strMedia = "media is not CD";
                    }
                    else
                    {
                        long min = (devInfo.MediaFreeSpace) / (75 * 60);

                        if (devInfo.MediaIsBlank)
                            strMedia = string.Format("{0} min free (a blank disc)", min);
                        else
                            strMedia = string.Format("{0} min free (not a blank disc)", min);
                    }
                }

                liv.SubItems[1].Text = strMedia;

                string writeSpeed = string.Empty;
                if (devInfo.SelectedWriteSpeed != null)
                {
                    writeSpeed = devInfo.SelectedWriteSpeed.ToString();
                }
                else if (devInfo.MaxWriteSpeed != null)
                {
                    writeSpeed = devInfo.MaxWriteSpeed.ToString() + " (default to Max)";
                }

                liv.SubItems[2].Text = writeSpeed;
            }
        }

        private void BurnerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsBurningInProgress())
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

        private void buttonBurn_Click(object sender, System.EventArgs e)
        {
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

            if (lbPlaylist.Items.Count == 0)
            {
                ShowError("No files(s) selected for burning.");
                return;
            }

            CreateProgressWindow();

            if (!PrepareWorkerThreadsResourses(selectedDevices))
            {
                DestroyProgressWindow();
                ReleaseWorkerThreadsResources();
                return;
            }

            m_mainWorkerThread = new Thread(MainBurnWorkerThreadProc);
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
                    ListItem item = (ListItem)comboBoxRecordingMode.SelectedItem;
                    ctx.burnerSettings.WriteMethod = (WriteMethod)item.Value;

                    ctx.burnerSettings.Simulate = checkBoxSimulate.Checked;
                    ctx.burnerSettings.CloseDisc = checkBoxCloseDisc.Checked;
                    ctx.burnerSettings.Eject = checkBoxEjectWhenDone.Checked;
                    ctx.burnerSettings.WriteCDText = checkBoxCDText.Checked;
                    ctx.burnerSettings.DecodeInTempFiles = checkDecodeInTempFiles.Checked;

                    if (di.SelectedWriteSpeed != null)
                    {
                        ctx.burnerSettings.WriteSpeedKB = di.SelectedWriteSpeed.TransferRateKB;
                    }
                    else if (di.MaxWriteSpeed != null)
                    {
                        ctx.burnerSettings.WriteSpeedKB = di.MaxWriteSpeed.TransferRateKB;
                    }

                    ctx.burnerSettings.CDText.Album = m_albumCDText;

                    for (int i = 0; i < lbPlaylist.Items.Count; i++)
                    {
                        if (ctx.burnerSettings.Files.Count >= 99)
                            break;

                        item = (ListItem)lbPlaylist.Items[i];
                        ctx.burnerSettings.Files.Add(item.Description);
                        ctx.burnerSettings.CDText.Songs[i] = (CDTextEntry)item.Value;

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

        void MainBurnWorkerThreadProc()
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

        static void SetCDText(PrimoSoftware.Burner.CDText cdtext, CDTextEntry cdt, int track)
        {
            if (cdtext == null)
                return;

            CDTextItem item = new CDTextItem()
            {
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
                item.Genre = (CDTextGenreCode)cdt.Genre;
                item.GenreText = cdt.GenreText;
            }

            item.UpcIsrc = cdt.UpcIsrc;

            cdtext.Items.Add(track, item);
        }

        void BurnWorkerThreadProc(object obj)
        {
            WorkerThreadContext ctx = obj as WorkerThreadContext;

            using (AudioCD audioCD = new AudioCD())
            {
                audioCD.OnWriteStatus += new EventHandler<AudioCDStatusEventArgs>(ctx.AudioCD_OnStatus);
                audioCD.OnWriteProgress += new EventHandler<AudioCDProgressEventArgs>(ctx.AudioCD_OnProgress);
                audioCD.OnContinueWrite += new EventHandler<AudioCDContinueEventArgs>(ctx.AudioCD_OnContinueBurn);

                ctx.device.WriteSpeedKB = ctx.burnerSettings.WriteSpeedKB;
                ctx.WriteRate1xKB = Speed1xKB.CD;

                audioCD.Device = ctx.device;

                if (ctx.burnerSettings.DecodeInTempFiles)
                    audioCD.AudioDecodingMethod = AudioDecodingMethod.TempFile;
                else
                    audioCD.AudioDecodingMethod = AudioDecodingMethod.Memory;


                IList<AudioInput> audioInputs = audioCD.AudioInputs;

                for (int i = 0; i < ctx.burnerSettings.Files.Count; i++)
                {
                    AudioInput ai = new AudioInput();
                    ai.FilePath = ctx.burnerSettings.Files[i];
                    audioInputs.Add(ai);
                }


                // Setup CDText Properties
                if (ctx.device.CDFeatures.CanReadCDText && ctx.burnerSettings.WriteCDText)
                {
                    CDText cdtext = new CDText();

                    SetCDText(cdtext, ctx.burnerSettings.CDText.Album, 0);

                    int cdTextItemN = 0;
                    for (int i = 0; i < ctx.burnerSettings.Files.Count; i++)
                    {
                        SetCDText(cdtext, ctx.burnerSettings.CDText.Songs[i], cdTextItemN + 1);
                        cdTextItemN++;
                    }

                    audioCD.CDText = cdtext;
                }

                audioCD.SimulateBurn = ctx.burnerSettings.Simulate;
                audioCD.CloseDisc = ctx.burnerSettings.CloseDisc;
                audioCD.WriteMethod = ctx.burnerSettings.WriteMethod;

                if (!audioCD.WriteToCD())
                {
                    ShowError(audioCD.Error, "WriteToCD failed.");

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

        void EraseWorkerThreadProc(object obj)
        {
            WorkerThreadContext ctx = obj as WorkerThreadContext;

            Device device = ctx.device;

            ctx.progressInfo.Status = "Erasing ...";
            ctx.progressForm.UpdateProgress(ctx.progressInfo, ctx.burnerIndex);

            EventHandler<DeviceEraseEventArgs> handler = new EventHandler<DeviceEraseEventArgs>(ctx.Device_OnErase);
            device.OnErase += handler;

            bool bRes = device.Erase(ctx.burnerSettings.QuickErase ? EraseType.Minimal : EraseType.Disc);

            device.OnErase -= handler;

            if (!bRes)
            {
                ShowError(device.Error, "Erase failed.");
                ctx.progressInfo.Status = "ERROR";
            }
            else
            {
                ctx.progressInfo.Status = "SUCCESS";
            }

            device.Refresh();
        }

        void MainEraseWorkerThreadProc()
        {
            List<Thread> threads = new List<Thread>();

            // start burn threads 
            foreach (WorkerThreadContext ctx in m_workers)
            {
                Thread thread = new Thread(EraseWorkerThreadProc);
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

        private void buttonEraseDisc_Click(object sender, EventArgs e)
        {
            if (DialogResult.Cancel == MessageBox.Show("Erasing will destroy all information on the disc. Do you want to continue?", this.Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation))
                return;

            if (lvDevices.SelectedIndices.Count == 0)
                return;

            DeviceInfo deviceInfo = m_devicesInfo[lvDevices.SelectedIndices[0]];
            int deviceIndex = deviceInfo.Index;

            using (DeviceEnumerator devEnum = m_engine.CreateDeviceEnumerator())
            {
                Device dev = devEnum.CreateDevice(deviceIndex, false);
                if (dev == null)
                {
                    ShowError(devEnum.Error, "Failed to create device.");
                    return;
                }

                if(dev.MediaProfile != MediaProfile.CdRw)
                {
                    dev.Dispose();
                    ShowError("Erasing is supported only for CD-RW media.");
                    return;
                }

                CreateProgressWindow();

                m_workers = new List<WorkerThreadContext>();
                WorkerThreadContext ctx = new WorkerThreadContext();
                ctx.burnerIndex = m_workers.Count;
                m_workers.Add(ctx);
                ctx.device = dev;

                ctx.progressForm = m_progressWindow;
                ctx.progressInfo = new ProgressInfo();
                ctx.progressInfo.DeviceTitle = deviceInfo.Title;
                ctx.burnerSettings = new BurnSettings();
                ctx.burnerSettings.QuickErase = checkBoxQuickErase.Checked;
            }

            m_mainWorkerThread = new Thread(MainEraseWorkerThreadProc);
            m_mainWorkerThread.Start();
        }
	}
}
