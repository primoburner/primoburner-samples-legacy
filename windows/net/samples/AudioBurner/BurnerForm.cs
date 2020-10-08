using System;
using System.IO;
using System.Data;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

using System.Windows.Forms;

using PrimoSoftware.Burner;

namespace AudioBurner.NET
{
	public class BurnerForm: System.Windows.Forms.Form
	{
		#region Contruct / Finalize
		public BurnerForm()
		{
			InitializeComponent();

            this.Text += IntPtr.Size == 8 ? " 64-bit" : " 32-bit";

			m_Burner.Status			+= new AudioBurner.NET.BurnerCallback.Status(Burner_Status);
			m_Burner.Continue		+= new AudioBurner.NET.BurnerCallback.Continue(Burner_Continue);
			m_Burner.Progress	    += new AudioBurner.NET.BurnerCallback.Progress(Burner_Progress);
			m_Burner.TrackProgress	+= new AudioBurner.NET.BurnerCallback.TrackProgress(Burner_TrackProgress);
			m_Burner.EraseProgress	+= new AudioBurner.NET.BurnerCallback.EraseProgress(Burner_EraseProgress);

			try
			{
				m_Burner.Open();

				int deviceCount = 0;
				DeviceInfo [] devices = m_Burner.EnumerateDevices();
				for (int i = 0; i < devices.Length; i++) 
				{
					DeviceInfo dev = devices[i];
					if (dev.IsWriter) 
					{
						comboDevices.Items.Add(dev);
						deviceCount++;
					}
				}

				if (0 == deviceCount)
					throw new Exception("No writer devices found.");


				// Device combo
				comboDevices.SelectedIndex = 0;

				// Write Method

                comboBoxRecordingMode.Items.Add(new ListItem(WriteMethod.Sao, "Session-At-Once"));
                comboBoxRecordingMode.Items.Add(new ListItem(WriteMethod.Tao, "Track-At-Once"));
				comboBoxRecordingMode.SelectedIndex = 0;

                // Decode in temp files
                checkDecodeInTempFiles.Checked = false;

				// Write parameters
				checkBoxSimulate.Checked = false;
				checkBoxEjectWhenDone.Checked = false;
				checkBoxCloseDisk.Checked = false;
                checkBoxCDText.Checked = false;
				checkBoxHiddenTrack.Checked = false;
				checkBoxHiddenTrack.Enabled = false;

				// Erasing
				checkBoxQuickErase.Checked = true;

                buttonTrackCDText.Enabled = false;
                buttonAlbumCDText.Enabled = false;

                UpdateDeviceInformation();
			}
			catch (Exception e)
			{
				ShowErrorMessage(e);
			}
		}

		protected override void Dispose( bool disposing)
		{
			if( disposing )
			{
				if (components != null) 
					components.Dispose();

				// Close Burner
				m_Burner.Close();
				m_Burner = null;
			}

			base.Dispose( disposing );
		}
		#endregion

		#region Event Handlers

		protected override void WndProc(ref Message msg)
		{
			if (WM_DEVICECHANGE == msg.Msg)
				UpdateDeviceInformation();

			base.WndProc(ref msg);
		}

		private void buttonAddFiles_Click(object sender, System.EventArgs e)
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
			checkBoxHiddenTrack.Enabled = 1 < lbPlaylist.Items.Count;
   		}

		private void buttonRemoveFiles_Click(object sender, System.EventArgs e)
		{
            ListBox.SelectedIndexCollection selected = lbPlaylist.SelectedIndices;

            int count = selected.Count;

            for (int i = count - 1; i >= 0; i--)
            {
                lbPlaylist.Items.RemoveAt(selected[i]);
            }
			checkBoxHiddenTrack.Enabled = 1 < lbPlaylist.Items.Count;
		}

		private void buttonBurn_Click(object sender, System.EventArgs e)
		{
			Burn();
			UpdateDeviceInformation();
		}

		private void buttonEraseDisc_Click(object sender, System.EventArgs e)
		{
			Erase();
			UpdateDeviceInformation();
		}

		private void buttonEject_Click(object sender, System.EventArgs e)
		{
			if (-1 == comboDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);
					m_Burner.Eject();
				m_Burner.ReleaseDevice();
			}
			catch(Exception bme)
			{
				m_Burner.ReleaseDevice();
				ShowErrorMessage(bme);
			}
		}

		private void buttonCloseTray_Click(object sender, System.EventArgs e)
		{
			if (-1 == comboDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);
					m_Burner.CloseTray();
				m_Burner.ReleaseDevice();
			}
			catch(Exception bme)
			{
				m_Burner.ReleaseDevice();
				ShowErrorMessage(bme);
			}
		}

		private void buttonExit_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void comboBoxDevices_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if(-1 != comboDevices.SelectedIndex)
				UpdateDeviceInformation();
		}

		private void Burner_Status(string message)
		{
			m_progressInfo.Message = message;
			m_progressWindow.UpdateProgress(m_progressInfo);
		}

		private void Burner_Progress(int percent)
		{
			try
			{
				m_progressInfo.Percent = percent;

				// get device internal buffer
                double cached = (double)m_Burner.DeviceCacheUsedSpace * 100.0 / (double)m_Burner.DeviceCacheSize;
                m_progressInfo.UsedCachePercent = (int)cached;

				// get actual write speed (KB/s)
				m_progressInfo.ActualWriteSpeed = m_Burner.WriteTransferKB;
			}
			catch
			{
			}

			m_progressWindow.UpdateProgress(m_progressInfo);
		}

		private void Burner_TrackProgress(int nTrack, int percentCompleted)
		{
			// do nothing
		}

		private void Burner_EraseProgress(int percentCompleted)
		{
			m_progressInfo.Message = "Erasing...";
			m_progressInfo.Percent = percentCompleted;
			m_progressWindow.UpdateProgress(m_progressInfo);
		}

		private bool Burner_Continue()
		{
			return (false == m_progressWindow.Stopped);
		}
		#endregion

		#region Private Methods
		
		private void Burn()
		{
			if (-1 == comboDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);

				CreateProgressWindow();

				BurnSettings settings = new BurnSettings();

                settings.DecodeInTempFiles = checkDecodeInTempFiles.Checked;
                settings.WriteCDText = checkBoxCDText.Checked;
				settings.Simulate = checkBoxSimulate.Checked;
				settings.Eject = checkBoxEjectWhenDone.Checked;
				settings.CloseDisk = checkBoxCloseDisk.Checked;
				settings.CreateHiddenTrack = checkBoxHiddenTrack.Checked;

				SpeedInfo speed = (SpeedInfo)comboBoxRecordingSpeed.SelectedItem;
				settings.WriteSpeedKB = (int)speed.TransferRateKB;

				ListItem item = (ListItem)comboBoxRecordingMode.SelectedItem;
				settings.WriteMethod = (WriteMethod)item.Value;

                settings.CDText.Album = m_albumCDText;

                for (int i=0; i<lbPlaylist.Items.Count; i++)
                {
                    if (settings.Files.Count >= 99)
                        break;

                    item = (ListItem)lbPlaylist.Items[i];
                    settings.Files.Add(item.Description);
                    settings.CDText.Songs[i] = (CDTextEntry)item.Value;
                    
                }

                settings.UseAudioStream = checkUseAudioStream.Checked;

				BurnThreadDelegate thread = new BurnThreadDelegate(BurnThread);
				IAsyncResult ar = thread.BeginInvoke(settings, null, null);

				WaitForThreadToFinish(ar);

				if (THREAD_EXCEPTION == thread.EndInvoke(ar))
					ShowErrorMessage(m_ThreadException);

				DestroyProgressWindow();
				m_Burner.ReleaseDevice();
			}
			catch(BurnerException bme)
			{
				m_Burner.ReleaseDevice();
				ShowErrorMessage(bme);
			}
		}

		private delegate int BurnThreadDelegate(BurnSettings settings);
		private int BurnThread(BurnSettings settings)
		{
			try
			{
				m_Burner.Burn(settings);
			}
			catch(BurnerException bme)
			{
				m_ThreadException = bme;
				return THREAD_EXCEPTION;
			}

			return NOERROR;
		}

		private void Erase()
		{
			if (-1 == comboDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);

				if (m_Burner.MediaIsBlank)
					if (DialogResult.Cancel == MessageBox.Show("Media is already blank. Do you want to erase it again?", this.Text, MessageBoxButtons.OKCancel))
					{
						m_Burner.ReleaseDevice();
						return;
					}

				if (DialogResult.Cancel == MessageBox.Show("Erasing will destroy all information on the disc. Do you want to continue?", this.Text, MessageBoxButtons.OKCancel))
				{
					m_Burner.ReleaseDevice();
					return;
				}


				CreateProgressWindow();
				m_progressWindow.Status = "Erasing. Please wait...";

				EraseSettings settings = new EraseSettings();
				settings.Quick = checkBoxQuickErase.Checked;
				settings.Force = true;
			
				EraseThreadDelegate thread = new EraseThreadDelegate(EraseThread);
				IAsyncResult ar = thread.BeginInvoke(settings, null, null);

				WaitForThreadToFinish(ar);

				if (THREAD_EXCEPTION == thread.EndInvoke(ar))
					ShowErrorMessage(m_ThreadException);

				DestroyProgressWindow();
				m_Burner.ReleaseDevice();
			}
			catch(BurnerException bme)
			{
				m_Burner.ReleaseDevice();
				ShowErrorMessage(bme);
			}
		}

		private delegate int EraseThreadDelegate(EraseSettings settings);
		private int EraseThread(EraseSettings settings)
		{
			try
			{
				m_Burner.Erase(settings);
			}
			catch(BurnerException bme)
			{
				m_ThreadException = bme;
				return THREAD_EXCEPTION;
			}

			return NOERROR;
		}

		private void WaitForThreadToFinish(IAsyncResult ar)
		{
			while (false == ar.IsCompleted) 
			{
				Application.DoEvents();
				Thread.Sleep(50);
			}
		}

		private void CreateProgressWindow()
		{
			this.Enabled = false;
			
			// Clear the progress info
			m_progressInfo = new ProgressInfo();

			// Create a progress window
			m_progressWindow = new ProgressForm();
			m_progressWindow.Owner = this;
			m_progressWindow.Show();
		}

		private void DestroyProgressWindow()
		{
            // Enable the main form before closing the progress window, otherwise it's hidden
            this.Enabled = true;

			if(null != m_progressWindow)
			{
				m_progressWindow.Close();
				m_progressWindow = null;
			}
		}

		private void UpdateDeviceInformation()
		{
			if (-1 == comboDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboDevices.SelectedItem;

				// Select device. Exclusive access is not required.
				m_Burner.SelectDevice(dev.Index, false);

                // Capacity
                long mediaFreeSpace = m_Burner.MediaFreeSpace;

                if (mediaFreeSpace <= 0)
                {
                    labelFreeSpace.Text = "Insert a blank disc.";
                }
                else
                {
                    long min = (mediaFreeSpace) / (75 * 60);
                    string str = string.Empty;

                    if (m_Burner.MediaIsBlank)
                        str = string.Format("{0} min free (a blank disc)", min);
                    else
                        str = string.Format("{0} min free (not a blank disc)", min);

                    labelFreeSpace.Text = str;
                }

                // Burn Button
                buttonBurn.Enabled = (mediaFreeSpace > 0);


				// Speed
				SpeedInfo [] speeds = m_Burner.EnumerateWriteSpeeds();

				// Save speed selection
				string sSelectedSpeed = "";
				if(comboBoxRecordingSpeed.SelectedIndex > -1)
					sSelectedSpeed = ((SpeedInfo)comboBoxRecordingSpeed.SelectedItem).ToString();

                // Update available speeds
				comboBoxRecordingSpeed.Items.Clear();
				for (int i = 0; i < speeds.Length; i++)
					comboBoxRecordingSpeed.Items.Add(speeds[i]);

				// Restore selection
				if (comboBoxRecordingSpeed.Items.Count > 0)
				{
					comboBoxRecordingSpeed.SelectedIndex = comboBoxRecordingSpeed.FindString(sSelectedSpeed);
					if (-1 == comboBoxRecordingSpeed.SelectedIndex)
						comboBoxRecordingSpeed.SelectedIndex = 0;
				}

				// Recording mode
                bool bSaoPossible = m_Burner.SaoPossible;
                bool bTaoPossible = m_Burner.TaoPossible;

                checkBoxQuickErase.Enabled = m_Burner.CanReWrite;
                buttonEraseDisc.Enabled = m_Burner.CanReWrite;

                checkBoxCDText.Enabled = m_Burner.CDTextSupport;

                comboBoxRecordingMode.Enabled = bTaoPossible && bSaoPossible;
                
                if (!bSaoPossible && !bTaoPossible)
                {
                    throw new BurnerException(BurnerErrors.NO_WRITER_DEVICES);
                }
                else if (!bSaoPossible)
                {
                    comboBoxRecordingMode.SelectedIndex = 1; // only TAO is possible
                }
                else if (!bTaoPossible)
                {
                    comboBoxRecordingMode.SelectedIndex = 0; // only SAO is possible
                }
				
				m_Burner.ReleaseDevice();
			}
			catch(BurnerException bme)
			{
				// Ignore the error when it is DEVICE_ALREADY_SELECTED
				if (BurnerErrors.DEVICE_ALREADY_SELECTED == bme.Error)
					return;

				// Report all other errors
				m_Burner.ReleaseDevice();
				ShowErrorMessage(bme);
			}
		}

		#endregion

		#region Private Members
		private const int WM_DEVICECHANGE = 0x0219;
		
		private const int NOERROR = 0;
		private const int THREAD_EXCEPTION = 1;

		private Burner m_Burner = new Burner();
		private BurnerException m_ThreadException;

		private ProgressForm m_progressWindow;
		private ProgressInfo m_progressInfo = new ProgressInfo();

        private CDTextEntry m_albumCDText = new CDTextEntry();

		#endregion

		#region Windows Form Designer generated code
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelFreeSpace;
        private System.Windows.Forms.ComboBox comboDevices;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.ComboBox comboBoxRecordingMode;
		private System.Windows.Forms.ComboBox comboBoxRecordingSpeed;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkBoxCloseDisk;
		private System.Windows.Forms.CheckBox checkBoxSimulate;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.Button buttonEraseDisc;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.Button buttonEject;
        private System.Windows.Forms.Button buttonCloseTray;
        private System.Windows.Forms.Button buttonRemoveFiles;
		private System.Windows.Forms.Button buttonAddFiles;
        private System.Windows.Forms.Button buttonBurn;
        private System.Windows.Forms.CheckBox checkBoxEjectWhenDone;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.CheckBox checkBoxQuickErase;
		private System.Windows.Forms.CheckBox checkDecodeInTempFiles;
        private GroupBox groupBox2;
        private ListBox lbPlaylist;
        private GroupBox groupBox3;
        private CheckBox checkBoxCDText;
        private Button buttonAlbumCDText;
        private Button buttonTrackCDText;
        private CheckBox checkUseAudioStream;
		private GroupBox groupBox1;
		private CheckBox checkBoxHiddenTrack;


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
			this.label1 = new System.Windows.Forms.Label();
			this.comboDevices = new System.Windows.Forms.ComboBox();
			this.labelFreeSpace = new System.Windows.Forms.Label();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.checkDecodeInTempFiles = new System.Windows.Forms.CheckBox();
			this.comboBoxRecordingMode = new System.Windows.Forms.ComboBox();
			this.comboBoxRecordingSpeed = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.checkBoxCloseDisk = new System.Windows.Forms.CheckBox();
			this.checkBoxEjectWhenDone = new System.Windows.Forms.CheckBox();
			this.checkBoxSimulate = new System.Windows.Forms.CheckBox();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.checkBoxQuickErase = new System.Windows.Forms.CheckBox();
			this.buttonEraseDisc = new System.Windows.Forms.Button();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.buttonEject = new System.Windows.Forms.Button();
			this.buttonCloseTray = new System.Windows.Forms.Button();
			this.buttonBurn = new System.Windows.Forms.Button();
			this.buttonAddFiles = new System.Windows.Forms.Button();
			this.buttonRemoveFiles = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.lbPlaylist = new System.Windows.Forms.ListBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.buttonAlbumCDText = new System.Windows.Forms.Button();
			this.buttonTrackCDText = new System.Windows.Forms.Button();
			this.checkBoxCDText = new System.Windows.Forms.CheckBox();
			this.checkUseAudioStream = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.checkBoxHiddenTrack = new System.Windows.Forms.CheckBox();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.groupBox6.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(88, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "CD Writers :";
			// 
			// comboDevices
			// 
			this.comboDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboDevices.Location = new System.Drawing.Point(8, 24);
			this.comboDevices.Name = "comboDevices";
			this.comboDevices.Size = new System.Drawing.Size(288, 21);
			this.comboDevices.TabIndex = 1;
			this.comboDevices.SelectedIndexChanged += new System.EventHandler(this.comboBoxDevices_SelectedIndexChanged);
			// 
			// labelFreeSpace
			// 
			this.labelFreeSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelFreeSpace.Location = new System.Drawing.Point(304, 24);
			this.labelFreeSpace.Name = "labelFreeSpace";
			this.labelFreeSpace.Size = new System.Drawing.Size(176, 21);
			this.labelFreeSpace.TabIndex = 2;
			this.labelFreeSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.checkDecodeInTempFiles);
			this.groupBox4.Controls.Add(this.comboBoxRecordingMode);
			this.groupBox4.Controls.Add(this.comboBoxRecordingSpeed);
			this.groupBox4.Controls.Add(this.label7);
			this.groupBox4.Controls.Add(this.label8);
			this.groupBox4.Controls.Add(this.checkBoxCloseDisk);
			this.groupBox4.Controls.Add(this.checkBoxEjectWhenDone);
			this.groupBox4.Controls.Add(this.checkBoxSimulate);
			this.groupBox4.Location = new System.Drawing.Point(8, 331);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(368, 104);
			this.groupBox4.TabIndex = 16;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Parameters";
			// 
			// checkDecodeInTempFiles
			// 
			this.checkDecodeInTempFiles.Location = new System.Drawing.Point(133, 75);
			this.checkDecodeInTempFiles.Name = "checkDecodeInTempFiles";
			this.checkDecodeInTempFiles.Size = new System.Drawing.Size(170, 24);
			this.checkDecodeInTempFiles.TabIndex = 20;
			this.checkDecodeInTempFiles.Text = "Decode audio in temp files";
			// 
			// comboBoxRecordingMode
			// 
			this.comboBoxRecordingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRecordingMode.ItemHeight = 13;
			this.comboBoxRecordingMode.Location = new System.Drawing.Point(237, 49);
			this.comboBoxRecordingMode.Name = "comboBoxRecordingMode";
			this.comboBoxRecordingMode.Size = new System.Drawing.Size(120, 21);
			this.comboBoxRecordingMode.TabIndex = 19;
			// 
			// comboBoxRecordingSpeed
			// 
			this.comboBoxRecordingSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRecordingSpeed.ItemHeight = 13;
			this.comboBoxRecordingSpeed.Location = new System.Drawing.Point(237, 22);
			this.comboBoxRecordingSpeed.Name = "comboBoxRecordingSpeed";
			this.comboBoxRecordingSpeed.Size = new System.Drawing.Size(71, 21);
			this.comboBoxRecordingSpeed.TabIndex = 18;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(133, 51);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(89, 13);
			this.label7.TabIndex = 17;
			this.label7.Text = "Recording Mode:";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(133, 23);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(93, 13);
			this.label8.TabIndex = 16;
			this.label8.Text = "Recording Speed:";
			// 
			// checkBoxCloseDisk
			// 
			this.checkBoxCloseDisk.Location = new System.Drawing.Point(16, 75);
			this.checkBoxCloseDisk.Name = "checkBoxCloseDisk";
			this.checkBoxCloseDisk.Size = new System.Drawing.Size(103, 24);
			this.checkBoxCloseDisk.TabIndex = 15;
			this.checkBoxCloseDisk.Text = "Close Disc";
			// 
			// checkBoxEjectWhenDone
			// 
			this.checkBoxEjectWhenDone.Location = new System.Drawing.Point(16, 48);
			this.checkBoxEjectWhenDone.Name = "checkBoxEjectWhenDone";
			this.checkBoxEjectWhenDone.Size = new System.Drawing.Size(119, 24);
			this.checkBoxEjectWhenDone.TabIndex = 14;
			this.checkBoxEjectWhenDone.Text = "Eject when done";
			// 
			// checkBoxSimulate
			// 
			this.checkBoxSimulate.Location = new System.Drawing.Point(16, 18);
			this.checkBoxSimulate.Name = "checkBoxSimulate";
			this.checkBoxSimulate.Size = new System.Drawing.Size(104, 24);
			this.checkBoxSimulate.TabIndex = 13;
			this.checkBoxSimulate.Text = "Simulate";
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.checkBoxQuickErase);
			this.groupBox5.Controls.Add(this.buttonEraseDisc);
			this.groupBox5.Location = new System.Drawing.Point(488, 331);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(96, 104);
			this.groupBox5.TabIndex = 26;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Erase";
			// 
			// checkBoxQuickErase
			// 
			this.checkBoxQuickErase.Checked = true;
			this.checkBoxQuickErase.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxQuickErase.Location = new System.Drawing.Point(16, 24);
			this.checkBoxQuickErase.Name = "checkBoxQuickErase";
			this.checkBoxQuickErase.Size = new System.Drawing.Size(56, 25);
			this.checkBoxQuickErase.TabIndex = 25;
			this.checkBoxQuickErase.Text = "Quick";
			// 
			// buttonEraseDisc
			// 
			this.buttonEraseDisc.Location = new System.Drawing.Point(13, 55);
			this.buttonEraseDisc.Name = "buttonEraseDisc";
			this.buttonEraseDisc.Size = new System.Drawing.Size(75, 25);
			this.buttonEraseDisc.TabIndex = 26;
			this.buttonEraseDisc.Text = "E&rase";
			this.buttonEraseDisc.Click += new System.EventHandler(this.buttonEraseDisc_Click);
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.buttonEject);
			this.groupBox6.Controls.Add(this.buttonCloseTray);
			this.groupBox6.Location = new System.Drawing.Point(387, 331);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(93, 104);
			this.groupBox6.TabIndex = 25;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Eject Device";
			// 
			// buttonEject
			// 
			this.buttonEject.Location = new System.Drawing.Point(11, 56);
			this.buttonEject.Name = "buttonEject";
			this.buttonEject.Size = new System.Drawing.Size(75, 24);
			this.buttonEject.TabIndex = 23;
			this.buttonEject.Text = "E&ject";
			this.buttonEject.Click += new System.EventHandler(this.buttonEject_Click);
			// 
			// buttonCloseTray
			// 
			this.buttonCloseTray.Location = new System.Drawing.Point(11, 26);
			this.buttonCloseTray.Name = "buttonCloseTray";
			this.buttonCloseTray.Size = new System.Drawing.Size(75, 23);
			this.buttonCloseTray.TabIndex = 22;
			this.buttonCloseTray.Text = "&Close";
			this.buttonCloseTray.Click += new System.EventHandler(this.buttonCloseTray_Click);
			// 
			// buttonBurn
			// 
			this.buttonBurn.Location = new System.Drawing.Point(496, 23);
			this.buttonBurn.Name = "buttonBurn";
			this.buttonBurn.Size = new System.Drawing.Size(88, 24);
			this.buttonBurn.TabIndex = 28;
			this.buttonBurn.Text = "Burn";
			this.buttonBurn.Click += new System.EventHandler(this.buttonBurn_Click);
			// 
			// buttonAddFiles
			// 
			this.buttonAddFiles.Location = new System.Drawing.Point(8, 59);
			this.buttonAddFiles.Name = "buttonAddFiles";
			this.buttonAddFiles.Size = new System.Drawing.Size(104, 24);
			this.buttonAddFiles.TabIndex = 29;
			this.buttonAddFiles.Text = "Add Audio Files";
			this.buttonAddFiles.Click += new System.EventHandler(this.buttonAddFiles_Click);
			// 
			// buttonRemoveFiles
			// 
			this.buttonRemoveFiles.Location = new System.Drawing.Point(117, 59);
			this.buttonRemoveFiles.Name = "buttonRemoveFiles";
			this.buttonRemoveFiles.Size = new System.Drawing.Size(105, 24);
			this.buttonRemoveFiles.TabIndex = 30;
			this.buttonRemoveFiles.Text = "Remove Files";
			this.buttonRemoveFiles.Click += new System.EventHandler(this.buttonRemoveFiles_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Location = new System.Drawing.Point(8, 52);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(576, 2);
			this.groupBox2.TabIndex = 32;
			this.groupBox2.TabStop = false;
			// 
			// lbPlaylist
			// 
			this.lbPlaylist.FormattingEnabled = true;
			this.lbPlaylist.Location = new System.Drawing.Point(11, 89);
			this.lbPlaylist.Name = "lbPlaylist";
			this.lbPlaylist.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.lbPlaylist.Size = new System.Drawing.Size(573, 160);
			this.lbPlaylist.TabIndex = 33;
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.buttonAlbumCDText);
			this.groupBox3.Controls.Add(this.buttonTrackCDText);
			this.groupBox3.Controls.Add(this.checkBoxCDText);
			this.groupBox3.Location = new System.Drawing.Point(11, 258);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(335, 68);
			this.groupBox3.TabIndex = 34;
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
			// checkUseAudioStream
			// 
			this.checkUseAudioStream.AutoSize = true;
			this.checkUseAudioStream.Location = new System.Drawing.Point(228, 66);
			this.checkUseAudioStream.Name = "checkUseAudioStream";
			this.checkUseAudioStream.Size = new System.Drawing.Size(148, 17);
			this.checkUseAudioStream.TabIndex = 35;
			this.checkUseAudioStream.Text = "Use audio files as streams";
			this.checkUseAudioStream.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.checkBoxHiddenTrack);
			this.groupBox1.Location = new System.Drawing.Point(352, 258);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(232, 68);
			this.groupBox1.TabIndex = 35;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Hidden Track";
			// 
			// checkBoxHiddenTrack
			// 
			this.checkBoxHiddenTrack.AutoSize = true;
			this.checkBoxHiddenTrack.Location = new System.Drawing.Point(16, 29);
			this.checkBoxHiddenTrack.Name = "checkBoxHiddenTrack";
			this.checkBoxHiddenTrack.Size = new System.Drawing.Size(199, 17);
			this.checkBoxHiddenTrack.TabIndex = 0;
			this.checkBoxHiddenTrack.Text = "Place first audio file in a hidden track";
			this.checkBoxHiddenTrack.UseVisualStyleBackColor = true;
			// 
			// BurnerForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(596, 447);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.checkUseAudioStream);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.lbPlaylist);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.buttonRemoveFiles);
			this.Controls.Add(this.buttonAddFiles);
			this.Controls.Add(this.buttonBurn);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox6);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.labelFreeSpace);
			this.Controls.Add(this.comboDevices);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BurnerForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "PrimoBurner(tm) Engine for .NET - Audio Burning Sample Application ";
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.groupBox5.ResumeLayout(false);
			this.groupBox6.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

        private void buttonTrackCDText_Click(object sender, EventArgs e)
        {
            int cursel = lbPlaylist.SelectedIndex;
            if (-1 == cursel || 0 == lbPlaylist.Items.Count)
            {
                MessageBox.Show("To edit the CDText information of a track select the track from the track list and click on this button again.");
                return;
            }

            if (cursel >= 99)
            {
                MessageBox.Show("CDText information is supported for up to 99 tracks only.");
                return;
            }

            CDTextForm dlg = new CDTextForm();
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

        private void buttonAlbumCDText_Click(object sender, EventArgs e)
        {
            CDTextForm dlg = new CDTextForm();
            dlg.EditAlbum = true;
            dlg.CDText = m_albumCDText;

            if (DialogResult.OK == dlg.ShowDialog())
            {
                m_albumCDText = dlg.CDText;
            }
        }

        private void checkBoxCDText_CheckedChanged(object sender, EventArgs e)
        {
            buttonTrackCDText.Enabled = checkBoxCDText.Checked;
            buttonAlbumCDText.Enabled = checkBoxCDText.Checked;
        }

		private void ShowErrorMessage(Exception e)
		{
			if (null != e)
			{
				MessageBox.Show(e.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
