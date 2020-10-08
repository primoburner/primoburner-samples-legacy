using System;
using System.IO;
using System.Data;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

using System.Windows.Forms;

using PrimoSoftware.Burner;

namespace DVDBurner.NET
{
	public class BurnerForm: System.Windows.Forms.Form
	{
		#region Contruct / Finalize
		public BurnerForm()
		{
			InitializeComponent();

			m_Burner.Status			+= new DVDBurner.NET.BurnerCallback.Status(Burner_Status);
			m_Burner.Continue		+= new DVDBurner.NET.BurnerCallback.Continue(Burner_Continue);
			m_Burner.ImageProgress	+= new DVDBurner.NET.BurnerCallback.ImageProgress(Burner_ImageProgress);
			m_Burner.FileProgress	+= new DVDBurner.NET.BurnerCallback.FileProgress(Burner_FileProgress);
			m_Burner.FormatProgress += new DVDBurner.NET.BurnerCallback.FormatProgress(Burner_FormatProgress);
			m_Burner.EraseProgress	+= new DVDBurner.NET.BurnerCallback.EraseProgress(Burner_EraseProgress);

			try
			{
				m_Burner.Open();

				int deviceCount = 0;
				DeviceInfo [] devices = m_Burner.EnumerateDevices();
				for (int i = 0; i < devices.Length; i++) 
				{
					DeviceInfo dev = devices[i];
					
					comboBoxDevices.Items.Add(dev);
					deviceCount++;
				}

				if (0 == deviceCount)
					throw new Exception("No writer devices found.");

				// Required space
				m_ddwRequiredSpace = 0;

				// Device combo
				comboBoxDevices.SelectedIndex = 0;
				UpdateDeviceInformation();

				// Write Method
				comboBoxRecordingMode.Items.Add(new ListItem(WriteMethod.DvdDao,			"Disk-At-Once"));
				comboBoxRecordingMode.Items.Add(new ListItem(WriteMethod.DvdIncremental,	"Incremental"));
				comboBoxRecordingMode.SelectedIndex = 1;

				// Image Types
				comboBoxImageType.Items.Add(new ListItem(ImageType.Iso9660,		"ISO9660"));
				comboBoxImageType.Items.Add(new ListItem(ImageType.Joliet,		"Joliet"));
				comboBoxImageType.Items.Add(new ListItem(ImageType.Udf,			"UDF"));
				comboBoxImageType.Items.Add(new ListItem(ImageType.UdfIso,		"UDF & ISO9660"));
				comboBoxImageType.Items.Add(new ListItem(ImageType.UdfJoliet,	"UDF & Joliet"));
				comboBoxImageType.SelectedIndex = 2;

				// Write parameters
				checkBoxSimulate.Checked = false;
				checkBoxEjectWhenDone.Checked = false;
				checkBoxCloseDisc.Checked = false;

				// Erasing / Formatting
				checkBoxQuickErase.Checked = true;
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
				PrimoSoftware.Burner.ImageType imageType = (PrimoSoftware.Burner.ImageType)item.Value;

				m_ddwRequiredSpace = m_Burner.CalculateImageSize(sourceFolder, imageType);
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

		private void buttonEraseDisc_Click(object sender, System.EventArgs e)
		{
			Erase();
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

		private void comboBoxRecordingMode_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			SelectedRecordingModeChanged();
		}

		private void checkBoxVideoDVD_CheckStateChanged(object sender, System.EventArgs e)
		{
			if (checkBoxVideoDVD.Checked)
			{
				// UDF & ISO9660;
				comboBoxImageType.SelectedIndex = 3;

				// Write parameters
				checkBoxCloseDisc.Checked = true;

				// Disable multi-session
				checkBoxLoadLastTrack.Checked = false;
			}

			comboBoxImageType.Enabled = !checkBoxVideoDVD.Checked;
			checkBoxCloseDisc.Enabled = !checkBoxVideoDVD.Checked;
			checkBoxLoadLastTrack.Enabled = !checkBoxVideoDVD.Checked;
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
				dd = (double)m_Burner.DeviceCacheUsedSpace;
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
			settings.ImageType = (PrimoSoftware.Burner.ImageType)item.Value;

			settings.VolumeLabel = textBoxVolumeName.Text;
			settings.VideoDVD = checkBoxVideoDVD.Checked;

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

				settings.Simulate = checkBoxSimulate.Checked;
				settings.Eject = checkBoxEjectWhenDone.Checked;
				settings.CloseDisc = checkBoxCloseDisc.Checked;

				SpeedInfo speed = (SpeedInfo)comboBoxRecordingSpeed.SelectedItem;
				settings.WriteSpeedKB = (int)speed.TransferRateKB;

				ListItem item = (ListItem)comboBoxRecordingMode.SelectedItem;
				settings.WriteMethod = (WriteMethod)item.Value;

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
				settings.ImageType = (PrimoSoftware.Burner.ImageType)item.Value;

				settings.VolumeLabel = textBoxVolumeName.Text;
				settings.VideoDVD = checkBoxVideoDVD.Checked;
				
				settings.Simulate = checkBoxSimulate.Checked;
				settings.Eject = checkBoxEjectWhenDone.Checked;
				settings.CloseDisc = checkBoxCloseDisc.Checked;
				settings.LoadLastTrack = checkBoxLoadLastTrack.Checked;

				SpeedInfo speed = (SpeedInfo)comboBoxRecordingSpeed.SelectedItem;
				settings.WriteSpeedKB = (int)speed.TransferRateKB;

				item = (ListItem)comboBoxRecordingMode.SelectedItem;
				settings.WriteMethod = (WriteMethod)item.Value;

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
			if (-1 == comboBoxDevices.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboBoxDevices.SelectedItem;
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

		private void ShowErrorMessage(Exception ex)
		{
			if (null != ex)
			{
				MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

				// Recording mode
				SelectedRecordingModeChanged();

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

		private void SelectedRecordingModeChanged()
		{
			if (-1 == comboBoxRecordingMode.SelectedIndex)
				return;

			ListItem item = (ListItem)comboBoxRecordingMode.SelectedItem;
			PrimoSoftware.Burner.WriteMethod writeMethod = (PrimoSoftware.Burner.WriteMethod)item.Value;

			if(PrimoSoftware.Burner.WriteMethod.DvdDao == writeMethod)
			{
				checkBoxCloseDisc.Checked = true;
				checkBoxCloseDisc.Enabled = false;
			}
			else if(PrimoSoftware.Burner.WriteMethod.DvdIncremental == writeMethod)
			{
				if(!checkBoxCloseDisc.Enabled)
					checkBoxCloseDisc.Checked = true;

				checkBoxCloseDisc.Enabled = true;
			}
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
		private System.Windows.Forms.ComboBox comboBoxRecordingMode;
		private System.Windows.Forms.ComboBox comboBoxRecordingSpeed;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkBoxCloseDisc;
		private System.Windows.Forms.CheckBox checkBoxSimulate;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.Button buttonEraseDisc;
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
		private System.Windows.Forms.CheckBox checkBoxQuickErase;
		private System.Windows.Forms.CheckBox checkBoxQuickFormat;
		private System.Windows.Forms.CheckBox checkBoxVideoDVD;


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
            this.checkBoxVideoDVD = new System.Windows.Forms.CheckBox();
            this.comboBoxRecordingMode = new System.Windows.Forms.ComboBox();
            this.comboBoxRecordingSpeed = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.checkBoxCloseDisc = new System.Windows.Forms.CheckBox();
            this.checkBoxEjectWhenDone = new System.Windows.Forms.CheckBox();
            this.checkBoxSimulate = new System.Windows.Forms.CheckBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.checkBoxQuickErase = new System.Windows.Forms.CheckBox();
            this.buttonEraseDisc = new System.Windows.Forms.Button();
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
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(10, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "DVD Writers :";
            // 
            // comboBoxDevices
            // 
            this.comboBoxDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDevices.Location = new System.Drawing.Point(10, 28);
            this.comboBoxDevices.Name = "comboBoxDevices";
            this.comboBoxDevices.Size = new System.Drawing.Size(345, 24);
            this.comboBoxDevices.TabIndex = 1;
            this.comboBoxDevices.SelectedIndexChanged += new System.EventHandler(this.comboBoxDevices_SelectedIndexChanged);
            // 
            // labelFreeSpace
            // 
            this.labelFreeSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelFreeSpace.Location = new System.Drawing.Point(365, 28);
            this.labelFreeSpace.Name = "labelFreeSpace";
            this.labelFreeSpace.Size = new System.Drawing.Size(163, 24);
            this.labelFreeSpace.TabIndex = 2;
            this.labelFreeSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelRequiredSpace
            // 
            this.labelRequiredSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelRequiredSpace.Location = new System.Drawing.Point(531, 28);
            this.labelRequiredSpace.Name = "labelRequiredSpace";
            this.labelRequiredSpace.Size = new System.Drawing.Size(174, 24);
            this.labelRequiredSpace.TabIndex = 3;
            this.labelRequiredSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(10, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 18);
            this.label4.TabIndex = 4;
            this.label4.Text = "Source Folder:";
            // 
            // textBoxRootDir
            // 
            this.textBoxRootDir.Location = new System.Drawing.Point(115, 102);
            this.textBoxRootDir.Name = "textBoxRootDir";
            this.textBoxRootDir.ReadOnly = true;
            this.textBoxRootDir.Size = new System.Drawing.Size(480, 22);
            this.textBoxRootDir.TabIndex = 5;
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(605, 102);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(96, 27);
            this.buttonBrowse.TabIndex = 6;
            this.buttonBrowse.Text = "Browse";
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(10, 138);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 19);
            this.label5.TabIndex = 7;
            this.label5.Text = "Volume name :";
            // 
            // textBoxVolumeName
            // 
            this.textBoxVolumeName.Location = new System.Drawing.Point(115, 138);
            this.textBoxVolumeName.MaxLength = 16;
            this.textBoxVolumeName.Name = "textBoxVolumeName";
            this.textBoxVolumeName.Size = new System.Drawing.Size(163, 22);
            this.textBoxVolumeName.TabIndex = 8;
            this.textBoxVolumeName.Text = "DATADVD";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(307, 138);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(87, 19);
            this.label6.TabIndex = 9;
            this.label6.Text = "Image Type:";
            // 
            // comboBoxImageType
            // 
            this.comboBoxImageType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxImageType.Location = new System.Drawing.Point(403, 138);
            this.comboBoxImageType.Name = "comboBoxImageType";
            this.comboBoxImageType.Size = new System.Drawing.Size(192, 24);
            this.comboBoxImageType.TabIndex = 10;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.checkBoxVideoDVD);
            this.groupBox4.Controls.Add(this.comboBoxRecordingMode);
            this.groupBox4.Controls.Add(this.comboBoxRecordingSpeed);
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.checkBoxCloseDisc);
            this.groupBox4.Controls.Add(this.checkBoxEjectWhenDone);
            this.groupBox4.Controls.Add(this.checkBoxSimulate);
            this.groupBox4.Location = new System.Drawing.Point(10, 175);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(547, 120);
            this.groupBox4.TabIndex = 16;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Parameters";
            // 
            // checkBoxVideoDVD
            // 
            this.checkBoxVideoDVD.Location = new System.Drawing.Point(19, 55);
            this.checkBoxVideoDVD.Name = "checkBoxVideoDVD";
            this.checkBoxVideoDVD.Size = new System.Drawing.Size(173, 28);
            this.checkBoxVideoDVD.TabIndex = 20;
            this.checkBoxVideoDVD.Text = "DVD-Video Compatible";
            this.checkBoxVideoDVD.CheckStateChanged += new System.EventHandler(this.checkBoxVideoDVD_CheckStateChanged);
            // 
            // comboBoxRecordingMode
            // 
            this.comboBoxRecordingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRecordingMode.ItemHeight = 16;
            this.comboBoxRecordingMode.Location = new System.Drawing.Point(394, 28);
            this.comboBoxRecordingMode.Name = "comboBoxRecordingMode";
            this.comboBoxRecordingMode.Size = new System.Drawing.Size(145, 24);
            this.comboBoxRecordingMode.TabIndex = 19;
            this.comboBoxRecordingMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxRecordingMode_SelectedIndexChanged);
            // 
            // comboBoxRecordingSpeed
            // 
            this.comboBoxRecordingSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRecordingSpeed.ItemHeight = 16;
            this.comboBoxRecordingSpeed.Location = new System.Drawing.Point(154, 28);
            this.comboBoxRecordingSpeed.Name = "comboBoxRecordingSpeed";
            this.comboBoxRecordingSpeed.Size = new System.Drawing.Size(86, 24);
            this.comboBoxRecordingSpeed.TabIndex = 18;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(259, 28);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(120, 18);
            this.label7.TabIndex = 17;
            this.label7.Text = "Recording Mode:";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(19, 28);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(115, 18);
            this.label8.TabIndex = 16;
            this.label8.Text = "Recording Speed:";
            // 
            // checkBoxCloseDisc
            // 
            this.checkBoxCloseDisc.Location = new System.Drawing.Point(394, 55);
            this.checkBoxCloseDisc.Name = "checkBoxCloseDisc";
            this.checkBoxCloseDisc.Size = new System.Drawing.Size(124, 28);
            this.checkBoxCloseDisc.TabIndex = 15;
            this.checkBoxCloseDisc.Text = "Close Disc";
            // 
            // checkBoxEjectWhenDone
            // 
            this.checkBoxEjectWhenDone.Location = new System.Drawing.Point(394, 83);
            this.checkBoxEjectWhenDone.Name = "checkBoxEjectWhenDone";
            this.checkBoxEjectWhenDone.Size = new System.Drawing.Size(145, 28);
            this.checkBoxEjectWhenDone.TabIndex = 14;
            this.checkBoxEjectWhenDone.Text = "Eject When Done";
            // 
            // checkBoxSimulate
            // 
            this.checkBoxSimulate.Location = new System.Drawing.Point(19, 83);
            this.checkBoxSimulate.Name = "checkBoxSimulate";
            this.checkBoxSimulate.Size = new System.Drawing.Size(125, 28);
            this.checkBoxSimulate.TabIndex = 13;
            this.checkBoxSimulate.Text = "Simulate";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.checkBoxQuickErase);
            this.groupBox5.Controls.Add(this.buttonEraseDisc);
            this.groupBox5.Location = new System.Drawing.Point(10, 305);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(201, 55);
            this.groupBox5.TabIndex = 26;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Erase";
            // 
            // checkBoxQuickErase
            // 
            this.checkBoxQuickErase.Checked = true;
            this.checkBoxQuickErase.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxQuickErase.Location = new System.Drawing.Point(19, 18);
            this.checkBoxQuickErase.Name = "checkBoxQuickErase";
            this.checkBoxQuickErase.Size = new System.Drawing.Size(67, 28);
            this.checkBoxQuickErase.TabIndex = 25;
            this.checkBoxQuickErase.Text = "Quick";
            // 
            // buttonEraseDisc
            // 
            this.buttonEraseDisc.Location = new System.Drawing.Point(96, 18);
            this.buttonEraseDisc.Name = "buttonEraseDisc";
            this.buttonEraseDisc.Size = new System.Drawing.Size(90, 28);
            this.buttonEraseDisc.TabIndex = 26;
            this.buttonEraseDisc.Text = "E&rase";
            this.buttonEraseDisc.Click += new System.EventHandler(this.buttonEraseDisc_Click);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.buttonEject);
            this.groupBox6.Controls.Add(this.buttonCloseTray);
            this.groupBox6.Location = new System.Drawing.Point(576, 175);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(125, 120);
            this.groupBox6.TabIndex = 25;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Device Tray";
            // 
            // buttonEject
            // 
            this.buttonEject.Location = new System.Drawing.Point(19, 28);
            this.buttonEject.Name = "buttonEject";
            this.buttonEject.Size = new System.Drawing.Size(90, 27);
            this.buttonEject.TabIndex = 23;
            this.buttonEject.Text = "E&ject";
            this.buttonEject.Click += new System.EventHandler(this.buttonEject_Click);
            // 
            // buttonCloseTray
            // 
            this.buttonCloseTray.Location = new System.Drawing.Point(19, 65);
            this.buttonCloseTray.Name = "buttonCloseTray";
            this.buttonCloseTray.Size = new System.Drawing.Size(90, 27);
            this.buttonCloseTray.TabIndex = 22;
            this.buttonCloseTray.Text = "&Close";
            this.buttonCloseTray.Click += new System.EventHandler(this.buttonCloseTray_Click);
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.checkBoxLoadLastTrack);
            this.groupBox7.Location = new System.Drawing.Point(461, 305);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(240, 55);
            this.groupBox7.TabIndex = 27;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Multi-session";
            // 
            // checkBoxLoadLastTrack
            // 
            this.checkBoxLoadLastTrack.Location = new System.Drawing.Point(19, 18);
            this.checkBoxLoadLastTrack.Name = "checkBoxLoadLastTrack";
            this.checkBoxLoadLastTrack.Size = new System.Drawing.Size(202, 28);
            this.checkBoxLoadLastTrack.TabIndex = 0;
            this.checkBoxLoadLastTrack.Text = "Append Data To Disc";
            // 
            // buttonBurn
            // 
            this.buttonBurn.Location = new System.Drawing.Point(10, 378);
            this.buttonBurn.Name = "buttonBurn";
            this.buttonBurn.Size = new System.Drawing.Size(124, 28);
            this.buttonBurn.TabIndex = 28;
            this.buttonBurn.Text = "Burn";
            this.buttonBurn.Click += new System.EventHandler(this.buttonBurn_Click);
            // 
            // buttonCreateImage
            // 
            this.buttonCreateImage.Location = new System.Drawing.Point(144, 378);
            this.buttonCreateImage.Name = "buttonCreateImage";
            this.buttonCreateImage.Size = new System.Drawing.Size(125, 28);
            this.buttonCreateImage.TabIndex = 29;
            this.buttonCreateImage.Text = "Create ISO Image";
            this.buttonCreateImage.Click += new System.EventHandler(this.buttonCreateImage_Click);
            // 
            // buttonBurnImage
            // 
            this.buttonBurnImage.Location = new System.Drawing.Point(278, 378);
            this.buttonBurnImage.Name = "buttonBurnImage";
            this.buttonBurnImage.Size = new System.Drawing.Size(125, 28);
            this.buttonBurnImage.TabIndex = 30;
            this.buttonBurnImage.Text = "Burn ISO Image";
            this.buttonBurnImage.Click += new System.EventHandler(this.buttonBurnImage_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(576, 378);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(125, 28);
            this.buttonExit.TabIndex = 31;
            this.buttonExit.Text = "Exit";
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // labelMediaType
            // 
            this.labelMediaType.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelMediaType.Location = new System.Drawing.Point(10, 65);
            this.labelMediaType.Name = "labelMediaType";
            this.labelMediaType.Size = new System.Drawing.Size(695, 24);
            this.labelMediaType.TabIndex = 32;
            this.labelMediaType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxQuickFormat);
            this.groupBox1.Controls.Add(this.buttonFormatDisc);
            this.groupBox1.Location = new System.Drawing.Point(235, 305);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(202, 55);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Format";
            // 
            // checkBoxQuickFormat
            // 
            this.checkBoxQuickFormat.Checked = true;
            this.checkBoxQuickFormat.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxQuickFormat.Location = new System.Drawing.Point(19, 18);
            this.checkBoxQuickFormat.Name = "checkBoxQuickFormat";
            this.checkBoxQuickFormat.Size = new System.Drawing.Size(67, 28);
            this.checkBoxQuickFormat.TabIndex = 25;
            this.checkBoxQuickFormat.Text = "Quick";
            // 
            // buttonFormatDisc
            // 
            this.buttonFormatDisc.Location = new System.Drawing.Point(96, 18);
            this.buttonFormatDisc.Name = "buttonFormatDisc";
            this.buttonFormatDisc.Size = new System.Drawing.Size(90, 28);
            this.buttonFormatDisc.TabIndex = 26;
            this.buttonFormatDisc.Text = "&Format";
            this.buttonFormatDisc.Click += new System.EventHandler(this.buttonFormatDisc_Click);
            // 
            // BurnerForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
            this.ClientSize = new System.Drawing.Size(724, 424);
            this.Controls.Add(this.labelMediaType);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonBurnImage);
            this.Controls.Add(this.buttonCreateImage);
            this.Controls.Add(this.buttonBurn);
            this.Controls.Add(this.groupBox7);
            this.Controls.Add(this.groupBox5);
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
            this.Text = "PrimoBurner(tm) Engine for .NET - DVDBurner.NET - DVD Burning Sample Application " +
    "";
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.groupBox7.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion
	}
}
