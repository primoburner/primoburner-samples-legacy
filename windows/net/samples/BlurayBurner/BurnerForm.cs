using System;
using System.IO;
using System.Data;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

using System.Windows.Forms;

using PrimoSoftware.Burner;

namespace BluRayBurner.NET
{
	public class BurnerForm: System.Windows.Forms.Form
	{
		#region Contruct / Finalize
		public BurnerForm()
		{
			InitializeComponent();

			m_Burner.Status			+= new BluRayBurner.NET.BurnerCallback.Status(Burner_Status);
			m_Burner.Continue		+= new BluRayBurner.NET.BurnerCallback.Continue(Burner_Continue);
			m_Burner.ImageProgress	+= new BluRayBurner.NET.BurnerCallback.ImageProgress(Burner_ImageProgress);
			m_Burner.FileProgress	+= new BluRayBurner.NET.BurnerCallback.FileProgress(Burner_FileProgress);
			m_Burner.FormatProgress += new BluRayBurner.NET.BurnerCallback.FormatProgress(Burner_FormatProgress);
			m_Burner.EraseProgress	+= new BluRayBurner.NET.BurnerCallback.EraseProgress(Burner_EraseProgress);

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
						comboBoxDevices.Items.Add(dev);
						deviceCount++;
					}
				}

				if (0 == deviceCount)
					throw new Exception("No writer devices found.");

				// Required space
				m_ddwRequiredSpace = 0;

				// Device combo
				comboBoxDevices.SelectedIndex = 0;
				UpdateDeviceInformation();

				// Image Types
                comboBoxImageType.Items.Add(new ListItem(UdfRevision.Revision102, "UDF 1.02"));
                comboBoxImageType.Items.Add(new ListItem(UdfRevision.Revision201, "UDF 2.01"));
                comboBoxImageType.Items.Add(new ListItem(UdfRevision.Revision250, "UDF 2.50"));
                comboBoxImageType.SelectedIndex = comboBoxImageType.Items.Count - 1;

				// Write parameters
				checkBoxEjectWhenDone.Checked = false;
				checkBoxCloseDisc.Checked = false;

				// Erasing / Formatting
				checkBoxQuickFormat.Checked = true;

				// Multi-session
				checkBoxLoadLastTrack.Checked = false;
			}
			catch (Exception ex)
			{
				ShowErrorMessage(ex);
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

		private void buttonBrowse_Click(object sender, System.EventArgs e)
		{
			string sourceFolder = ShowFolderBrowser();
			if ("" == sourceFolder)
				return;
			
			Cursor.Current = Cursors.WaitCursor;

			try
			{
				ListItem item = (ListItem)comboBoxImageType.SelectedItem;
				PrimoSoftware.Burner.UdfRevision udfRevision = (PrimoSoftware.Burner.UdfRevision)item.Value;

                m_ddwRequiredSpace = m_Burner.CalculateImageSize(sourceFolder, PrimoSoftware.Burner.ImageType.Udf, udfRevision);
				UpdateDeviceInformation();

				textBoxRootDir.Text = sourceFolder;
			}
			catch (BurnerException bme)
			{
				ShowErrorMessage(bme);
			}
			
			Cursor.Current = Cursors.Arrow;
		}

		private void buttonCreateImage_Click(object sender, System.EventArgs e)
		{
			if (!ValidateForm())
				return;

			saveFileDialog.Filter = "Image File (*.iso)|*.iso";
			if (DialogResult.OK != saveFileDialog.ShowDialog())
				return;

			CreateImage(saveFileDialog.FileName);
		}

		private void buttonBurnImage_Click(object sender, System.EventArgs e)
		{
			openFileDialog.Filter = "Image File (*.iso)|*.iso";
			if(DialogResult.OK != openFileDialog.ShowDialog())
				return;
			
			string imageFile = openFileDialog.FileName;
			try
			{
				FileInfo fi = new FileInfo(imageFile);
				if(fi.Length > m_ddwCapacity)
				{
					MessageBox.Show(String.Format("Cannot write image file {0}.\nThe file is too big.", imageFile));
					return;
				}
			}
			catch (Exception ex)
			{
				ShowErrorMessage(ex);
				return;
			}

			BurnImage(imageFile);
			UpdateDeviceInformation();
		}

		private void buttonBurn_Click(object sender, System.EventArgs e)
		{
			if (!ValidateForm())
				return;

			Burn();
			UpdateDeviceInformation();
		}

		private void buttonFormatDisc_Click(object sender, System.EventArgs e)
		{
			Format();
			UpdateDeviceInformation();
		}

		private void buttonEject_Click(object sender, System.EventArgs e)
		{
			if (-1 == comboBoxDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboBoxDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);
					m_Burner.Eject();
				m_Burner.ReleaseDevice();
			}
			catch(BurnerException bme)
			{
				m_Burner.ReleaseDevice();
				ShowErrorMessage(bme);
			}
		}

		private void buttonCloseTray_Click(object sender, System.EventArgs e)
		{
			if (-1 == comboBoxDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboBoxDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);
					m_Burner.CloseTray();
				m_Burner.ReleaseDevice();
			}
			catch(BurnerException bme)
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
			if(-1 != comboBoxDevices.SelectedIndex)
				UpdateDeviceInformation();
		}

        private void checkBoxBDVideo_CheckStateChanged(object sender, EventArgs e)
        {
            
            if (checkBoxBDVideo.Checked)
            {
                // UDF 2.50;
                comboBoxImageType.SelectedIndex = 2;

                // Write parameters
                checkBoxCloseDisc.Checked = true;

                // Disable multi-session
                checkBoxLoadLastTrack.Checked = false;
            }

            comboBoxImageType.Enabled = !checkBoxBDVideo.Checked;
            checkBoxCloseDisc.Enabled = !checkBoxBDVideo.Checked;
            checkBoxLoadLastTrack.Enabled = !checkBoxBDVideo.Checked;
        }

		private void Burner_Status(string message)
		{
			m_progressInfo.Message = message;
			m_progressWindow.UpdateProgress(m_progressInfo);
		}

		private void Burner_ImageProgress(long position, long all)
		{
			try
			{
				double dd = (double)position * 100.0 / (double)all;
				m_progressInfo.Percent = (int)dd;

				// get device internal buffer
				dd = (double)m_Burner.DeviceCacheUsedSize;
				dd = dd * 100.0 / (double)m_Burner.DeviceCacheSize;
				m_progressInfo.UsedCachePercent = (int)dd;

				// get actual write speed (KB/s)
				m_progressInfo.ActualWriteSpeed = m_Burner.WriteTransferKB;
			}
			catch
			{
			}

			m_progressWindow.UpdateProgress(m_progressInfo);
		}

		private void Burner_FileProgress(int file, string fileName, int percentCompleted)
		{
			// do nothing
		}

		private void Burner_FormatProgress(double percentCompleted)
		{
			m_progressInfo.Message = "Formatting...";
			m_progressInfo.Percent = (int)percentCompleted;
			m_progressWindow.UpdateProgress(m_progressInfo);
		}

		private void Burner_EraseProgress(double percentCompleted)
		{
			m_progressInfo.Message = "Erasing...";
			m_progressInfo.Percent = (int)percentCompleted;
			m_progressWindow.UpdateProgress(m_progressInfo);
		}

		private bool Burner_Continue()
		{
			return (false == m_progressWindow.Stopped);
		}
		#endregion

		#region Private Methods
		private void CreateImage(string imageFile)
		{
			CreateProgressWindow();

			CreateImageSettings settings = new CreateImageSettings();
			settings.ImageFile = imageFile;
			settings.SourceFolder = textBoxRootDir.Text;

			ListItem item = (ListItem)comboBoxImageType.SelectedItem;
			settings.ImageType = ImageType.Udf;
            settings.UdfRevision = (PrimoSoftware.Burner.UdfRevision)item.Value;
            settings.BDVideo = checkBoxBDVideo.Checked;

			settings.VolumeLabel = textBoxVolumeName.Text;

			CreateImageThreadDelegate thread = new CreateImageThreadDelegate(CreateImageThread);
			IAsyncResult ar = thread.BeginInvoke(settings, null, null);

			WaitForThreadToFinish(ar);

			if (THREAD_EXCEPTION == thread.EndInvoke(ar))
				ShowErrorMessage(m_ThreadException);

			DestroyProgressWindow();
		}

		private delegate int CreateImageThreadDelegate(CreateImageSettings settings);
		private int CreateImageThread(CreateImageSettings settings)
		{
			try
			{
				m_Burner.CreateImage(settings);
			}
			catch(BurnerException bme)
			{
				m_ThreadException = bme;
				return THREAD_EXCEPTION;
			}

			return NOERROR;
		}


		private void BurnImage(string imageFile)
		{
			if (-1 == comboBoxDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboBoxDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);

				CreateProgressWindow();

				BurnImageSettings settings = new BurnImageSettings();
				settings.ImageFile = imageFile;

				settings.Eject = checkBoxEjectWhenDone.Checked;
				settings.CloseDisc = checkBoxCloseDisc.Checked;

				SpeedInfo speed = (SpeedInfo)comboBoxRecordingSpeed.SelectedItem;
				settings.WriteSpeedKB = (int)speed.TransferRateKB;

				BurnImageThreadDelegate thread = new BurnImageThreadDelegate(BurnImageThread);
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

		private delegate int BurnImageThreadDelegate(BurnImageSettings settings);
		private int BurnImageThread(BurnImageSettings settings)
		{
			try
			{
				m_Burner.BurnImage(settings);
			}
			catch(BurnerException bme)
			{
				m_ThreadException = bme;
				return THREAD_EXCEPTION;
			}

			return NOERROR;
		}

		private void Burn()
		{
			if (-1 == comboBoxDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboBoxDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);

				CreateProgressWindow();

				BurnSettings settings = new BurnSettings();

				settings.SourceFolder = textBoxRootDir.Text;

				ListItem item = (ListItem)comboBoxImageType.SelectedItem;
				settings.ImageType = ImageType.Udf;
                settings.UdfRevision = (PrimoSoftware.Burner.UdfRevision)item.Value;
                settings.BDVideo = checkBoxBDVideo.Checked;

				settings.VolumeLabel = textBoxVolumeName.Text;
				
				settings.Eject = checkBoxEjectWhenDone.Checked;
				settings.CloseDisc = checkBoxCloseDisc.Checked;
				settings.LoadLastTrack = checkBoxLoadLastTrack.Checked;

				SpeedInfo speed = (SpeedInfo)comboBoxRecordingSpeed.SelectedItem;
				settings.WriteSpeedKB = (int)speed.TransferRateKB;

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

		private void Format()
		{
			if (-1 == comboBoxDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboBoxDevices.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true);

				if (m_Burner.MediaIsFullyFormatted)
					if (DialogResult.Cancel == MessageBox.Show("Media is already formatted. Do you want to format it again?", this.Text, MessageBoxButtons.OKCancel))
					{
						m_Burner.ReleaseDevice();
						return;
					}

				if (DialogResult.Cancel == MessageBox.Show("Formatting will destroy all the information on the disc. Do you want to continue?", this.Text, MessageBoxButtons.OKCancel))
				{
					m_Burner.ReleaseDevice();
					return;
				}

				CreateProgressWindow();

				m_progressWindow.EnableStop = false;
				m_progressWindow.Status = "Formatting. Please wait...";

				FormatSettings settings = new FormatSettings();
				settings.Quick = checkBoxQuickFormat.Checked;
				settings.Force = true;

				FormatThreadDelegate thread = new FormatThreadDelegate(FormatThread);
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

		private delegate int FormatThreadDelegate(FormatSettings settings);
		private int FormatThread(FormatSettings settings)
		{
			try
			{
				m_Burner.Format(settings);
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

		private void ShowErrorMessage(Exception e)
		{
			if (null != e)
			{
				MessageBox.Show(e.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
			if(null != m_progressWindow)
			{
				m_progressWindow.Close();
				m_progressWindow = null;
			}

			this.Enabled = true;
		}

		private void UpdateDeviceInformation()
		{
			if (-1 == comboBoxDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboBoxDevices.SelectedItem;

				// Select device. Exclusive access is not required.
				m_Burner.SelectDevice(dev.Index, false);

				// Get and display the media profile
				m_ddwCapacity = m_Burner.MediaFreeSpace * (int)BlockSize.Dvd;

				// Media profile
				labelMediaType.Text = m_Burner.MediaProfileString;

				// Required space
				labelRequiredSpace.Text = String.Format("Required space : {0}GB", ((double)m_ddwRequiredSpace / (1e9)).ToString("0.00"));

				// Capacity
				labelFreeSpace.Text = String.Format("Free space : {0}GB", ((double)m_ddwCapacity / (1e9)).ToString("0.00"));

				// Speed
				SpeedInfo [] speeds = m_Burner.EnumerateWriteSpeeds();

				// Save speed selection
				string sSelectedSpeed = "";
				if(comboBoxRecordingSpeed.SelectedIndex > -1)
					sSelectedSpeed = ((SpeedInfo)comboBoxRecordingSpeed.SelectedItem).ToString();

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

				// Burn Button
				buttonBurn.Enabled = (m_ddwCapacity > 0 && m_ddwCapacity >= m_ddwRequiredSpace);

				m_Burner.ReleaseDevice();
			}
			catch(BurnerException bme)
			{
				// Ignore the error when it is DEVICE_ALREADY_SELECTED
				if (BurnerErrors.DEVICE_ALREADY_SELECTED == bme.ErrorCode)
					return;

				// Report all other errors
				m_Burner.ReleaseDevice();
				ShowErrorMessage(bme);
			}
		}

		private bool ValidateForm()
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

		private string ShowFolderBrowser()
		{
			System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			if (DialogResult.OK == folderBrowserDialog.ShowDialog())
				return folderBrowserDialog.SelectedPath;
			return "";
		}
		#endregion

		#region Private Members
		private const int WM_DEVICECHANGE = 0x0219;
		
		private const int NOERROR = 0;
		private const int THREAD_EXCEPTION = 1;

		private Burner m_Burner = new Burner();
		private BurnerException m_ThreadException;

		private long	m_ddwCapacity = 0;
		private long	m_ddwRequiredSpace = 0;

		private ProgressForm m_progressWindow;
		private ProgressInfo m_progressInfo = new ProgressInfo();

		#endregion

		#region Windows Form Designer generated code
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label labelFreeSpace;
		private System.Windows.Forms.Label labelRequiredSpace;
		private System.Windows.Forms.ComboBox comboBoxDevices;
		private System.Windows.Forms.TextBox textBoxVolumeName;
		private System.Windows.Forms.ComboBox comboBoxImageType;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.ComboBox comboBoxRecordingSpeed;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkBoxCloseDisc;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.Button buttonEject;
		private System.Windows.Forms.Button buttonCloseTray;
		private System.Windows.Forms.GroupBox groupBox7;
		private System.Windows.Forms.Button buttonBurnImage;
		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Button buttonCreateImage;
		private System.Windows.Forms.Button buttonBurn;
		private System.Windows.Forms.TextBox textBoxRootDir;
		private System.Windows.Forms.CheckBox checkBoxLoadLastTrack;
		private System.Windows.Forms.CheckBox checkBoxEjectWhenDone;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Label labelMediaType;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonFormatDisc;
		private System.Windows.Forms.CheckBox checkBoxQuickFormat;
        private CheckBox checkBoxBDVideo;


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
            this.comboBoxDevices = new System.Windows.Forms.ComboBox();
            this.labelFreeSpace = new System.Windows.Forms.Label();
            this.labelRequiredSpace = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxRootDir = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxVolumeName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxImageType = new System.Windows.Forms.ComboBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.comboBoxRecordingSpeed = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.checkBoxCloseDisc = new System.Windows.Forms.CheckBox();
            this.checkBoxEjectWhenDone = new System.Windows.Forms.CheckBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.buttonEject = new System.Windows.Forms.Button();
            this.buttonCloseTray = new System.Windows.Forms.Button();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.checkBoxLoadLastTrack = new System.Windows.Forms.CheckBox();
            this.buttonBurn = new System.Windows.Forms.Button();
            this.buttonCreateImage = new System.Windows.Forms.Button();
            this.buttonBurnImage = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.labelMediaType = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxQuickFormat = new System.Windows.Forms.CheckBox();
            this.buttonFormatDisc = new System.Windows.Forms.Button();
            this.checkBoxBDVideo = new System.Windows.Forms.CheckBox();
            this.groupBox4.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Blu-ray Writers :";
            // 
            // comboBoxDevices
            // 
            this.comboBoxDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDevices.Location = new System.Drawing.Point(8, 24);
            this.comboBoxDevices.Name = "comboBoxDevices";
            this.comboBoxDevices.Size = new System.Drawing.Size(288, 21);
            this.comboBoxDevices.TabIndex = 1;
            this.comboBoxDevices.SelectedIndexChanged += new System.EventHandler(this.comboBoxDevices_SelectedIndexChanged);
            // 
            // labelFreeSpace
            // 
            this.labelFreeSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelFreeSpace.Location = new System.Drawing.Point(304, 24);
            this.labelFreeSpace.Name = "labelFreeSpace";
            this.labelFreeSpace.Size = new System.Drawing.Size(136, 21);
            this.labelFreeSpace.TabIndex = 2;
            this.labelFreeSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelRequiredSpace
            // 
            this.labelRequiredSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelRequiredSpace.Location = new System.Drawing.Point(448, 24);
            this.labelRequiredSpace.Name = "labelRequiredSpace";
            this.labelRequiredSpace.Size = new System.Drawing.Size(136, 21);
            this.labelRequiredSpace.TabIndex = 3;
            this.labelRequiredSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(8, 88);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 16);
            this.label4.TabIndex = 4;
            this.label4.Text = "Source Folder:";
            // 
            // textBoxRootDir
            // 
            this.textBoxRootDir.Location = new System.Drawing.Point(96, 88);
            this.textBoxRootDir.Name = "textBoxRootDir";
            this.textBoxRootDir.ReadOnly = true;
            this.textBoxRootDir.Size = new System.Drawing.Size(400, 20);
            this.textBoxRootDir.TabIndex = 5;
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(504, 88);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(80, 24);
            this.buttonBrowse.TabIndex = 6;
            this.buttonBrowse.Text = "Browse";
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(8, 120);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 16);
            this.label5.TabIndex = 7;
            this.label5.Text = "Volume name :";
            // 
            // textBoxVolumeName
            // 
            this.textBoxVolumeName.Location = new System.Drawing.Point(96, 120);
            this.textBoxVolumeName.MaxLength = 16;
            this.textBoxVolumeName.Name = "textBoxVolumeName";
            this.textBoxVolumeName.Size = new System.Drawing.Size(136, 20);
            this.textBoxVolumeName.TabIndex = 8;
            this.textBoxVolumeName.Text = "DATABD";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(256, 120);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(72, 16);
            this.label6.TabIndex = 9;
            this.label6.Text = "Image Type:";
            // 
            // comboBoxImageType
            // 
            this.comboBoxImageType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxImageType.Location = new System.Drawing.Point(336, 120);
            this.comboBoxImageType.Name = "comboBoxImageType";
            this.comboBoxImageType.Size = new System.Drawing.Size(160, 21);
            this.comboBoxImageType.TabIndex = 10;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.checkBoxBDVideo);
            this.groupBox4.Controls.Add(this.comboBoxRecordingSpeed);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.checkBoxCloseDisc);
            this.groupBox4.Controls.Add(this.checkBoxEjectWhenDone);
            this.groupBox4.Location = new System.Drawing.Point(8, 152);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(576, 64);
            this.groupBox4.TabIndex = 16;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Parameters";
            // 
            // comboBoxRecordingSpeed
            // 
            this.comboBoxRecordingSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRecordingSpeed.ItemHeight = 13;
            this.comboBoxRecordingSpeed.Location = new System.Drawing.Point(128, 24);
            this.comboBoxRecordingSpeed.Name = "comboBoxRecordingSpeed";
            this.comboBoxRecordingSpeed.Size = new System.Drawing.Size(72, 21);
            this.comboBoxRecordingSpeed.TabIndex = 18;
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(16, 26);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(96, 16);
            this.label8.TabIndex = 16;
            this.label8.Text = "Recording Speed:";
            // 
            // checkBoxCloseDisc
            // 
            this.checkBoxCloseDisc.Location = new System.Drawing.Point(220, 23);
            this.checkBoxCloseDisc.Name = "checkBoxCloseDisc";
            this.checkBoxCloseDisc.Size = new System.Drawing.Size(78, 26);
            this.checkBoxCloseDisc.TabIndex = 15;
            this.checkBoxCloseDisc.Text = "Close Disc";
            // 
            // checkBoxEjectWhenDone
            // 
            this.checkBoxEjectWhenDone.Location = new System.Drawing.Point(458, 23);
            this.checkBoxEjectWhenDone.Name = "checkBoxEjectWhenDone";
            this.checkBoxEjectWhenDone.Size = new System.Drawing.Size(112, 26);
            this.checkBoxEjectWhenDone.TabIndex = 14;
            this.checkBoxEjectWhenDone.Text = "Eject When Done";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.buttonEject);
            this.groupBox6.Controls.Add(this.buttonCloseTray);
            this.groupBox6.Location = new System.Drawing.Point(392, 224);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(192, 64);
            this.groupBox6.TabIndex = 25;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Device Tray";
            // 
            // buttonEject
            // 
            this.buttonEject.Location = new System.Drawing.Point(16, 24);
            this.buttonEject.Name = "buttonEject";
            this.buttonEject.Size = new System.Drawing.Size(75, 24);
            this.buttonEject.TabIndex = 23;
            this.buttonEject.Text = "E&ject";
            this.buttonEject.Click += new System.EventHandler(this.buttonEject_Click);
            // 
            // buttonCloseTray
            // 
            this.buttonCloseTray.Location = new System.Drawing.Point(96, 24);
            this.buttonCloseTray.Name = "buttonCloseTray";
            this.buttonCloseTray.Size = new System.Drawing.Size(75, 24);
            this.buttonCloseTray.TabIndex = 22;
            this.buttonCloseTray.Text = "&Close";
            this.buttonCloseTray.Click += new System.EventHandler(this.buttonCloseTray_Click);
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.checkBoxLoadLastTrack);
            this.groupBox7.Location = new System.Drawing.Point(184, 224);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(200, 64);
            this.groupBox7.TabIndex = 27;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Multi-session";
            // 
            // checkBoxLoadLastTrack
            // 
            this.checkBoxLoadLastTrack.Location = new System.Drawing.Point(16, 24);
            this.checkBoxLoadLastTrack.Name = "checkBoxLoadLastTrack";
            this.checkBoxLoadLastTrack.Size = new System.Drawing.Size(168, 24);
            this.checkBoxLoadLastTrack.TabIndex = 0;
            this.checkBoxLoadLastTrack.Text = "Append Data To Disc";
            // 
            // buttonBurn
            // 
            this.buttonBurn.Location = new System.Drawing.Point(8, 296);
            this.buttonBurn.Name = "buttonBurn";
            this.buttonBurn.Size = new System.Drawing.Size(104, 24);
            this.buttonBurn.TabIndex = 28;
            this.buttonBurn.Text = "Burn";
            this.buttonBurn.Click += new System.EventHandler(this.buttonBurn_Click);
            // 
            // buttonCreateImage
            // 
            this.buttonCreateImage.Location = new System.Drawing.Point(120, 296);
            this.buttonCreateImage.Name = "buttonCreateImage";
            this.buttonCreateImage.Size = new System.Drawing.Size(104, 24);
            this.buttonCreateImage.TabIndex = 29;
            this.buttonCreateImage.Text = "Create ISO Image";
            this.buttonCreateImage.Click += new System.EventHandler(this.buttonCreateImage_Click);
            // 
            // buttonBurnImage
            // 
            this.buttonBurnImage.Location = new System.Drawing.Point(232, 296);
            this.buttonBurnImage.Name = "buttonBurnImage";
            this.buttonBurnImage.Size = new System.Drawing.Size(104, 24);
            this.buttonBurnImage.TabIndex = 30;
            this.buttonBurnImage.Text = "Burn ISO Image";
            this.buttonBurnImage.Click += new System.EventHandler(this.buttonBurnImage_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(480, 296);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(104, 24);
            this.buttonExit.TabIndex = 31;
            this.buttonExit.Text = "Exit";
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // labelMediaType
            // 
            this.labelMediaType.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelMediaType.Location = new System.Drawing.Point(8, 56);
            this.labelMediaType.Name = "labelMediaType";
            this.labelMediaType.Size = new System.Drawing.Size(576, 21);
            this.labelMediaType.TabIndex = 32;
            this.labelMediaType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxQuickFormat);
            this.groupBox1.Controls.Add(this.buttonFormatDisc);
            this.groupBox1.Location = new System.Drawing.Point(8, 224);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(168, 64);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Format";
            // 
            // checkBoxQuickFormat
            // 
            this.checkBoxQuickFormat.Checked = true;
            this.checkBoxQuickFormat.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxQuickFormat.Location = new System.Drawing.Point(16, 24);
            this.checkBoxQuickFormat.Name = "checkBoxQuickFormat";
            this.checkBoxQuickFormat.Size = new System.Drawing.Size(56, 24);
            this.checkBoxQuickFormat.TabIndex = 25;
            this.checkBoxQuickFormat.Text = "Quick";
            // 
            // buttonFormatDisc
            // 
            this.buttonFormatDisc.Location = new System.Drawing.Point(80, 24);
            this.buttonFormatDisc.Name = "buttonFormatDisc";
            this.buttonFormatDisc.Size = new System.Drawing.Size(75, 24);
            this.buttonFormatDisc.TabIndex = 26;
            this.buttonFormatDisc.Text = "&Format";
            this.buttonFormatDisc.Click += new System.EventHandler(this.buttonFormatDisc_Click);
            // 
            // checkBoxBDVideo
            // 
            this.checkBoxBDVideo.Location = new System.Drawing.Point(307, 23);
            this.checkBoxBDVideo.Name = "checkBoxBDVideo";
            this.checkBoxBDVideo.Size = new System.Drawing.Size(145, 26);
            this.checkBoxBDVideo.TabIndex = 21;
            this.checkBoxBDVideo.Text = "BD-Video Compatible";
            this.checkBoxBDVideo.CheckStateChanged += new System.EventHandler(this.checkBoxBDVideo_CheckStateChanged);
            // 
            // BurnerForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(591, 335);
            this.Controls.Add(this.labelMediaType);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonBurnImage);
            this.Controls.Add(this.buttonCreateImage);
            this.Controls.Add(this.buttonBurn);
            this.Controls.Add(this.groupBox7);
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
            this.Controls.Add(this.labelFreeSpace);
            this.Controls.Add(this.comboBoxDevices);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BurnerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PrimoBurner(tm) Engine for .NET - BluRayBurner.NET - Blu-ray Disc (BD) Burning S" +
                "ample";
            this.groupBox4.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.groupBox7.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion
	}
}
