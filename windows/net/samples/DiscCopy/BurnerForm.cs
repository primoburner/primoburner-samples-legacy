using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

using System.Windows.Forms;

using PrimoSoftware.Burner;

namespace DiscCopy.NET
{
	public class BurnerForm: System.Windows.Forms.Form
	{
		#region Contruct / Finalize
		public BurnerForm()
		{
			InitializeComponent();

			m_Burner.Status			+= new DiscCopy.NET.BurnerCallback.Status(Burner_Status);
			m_Burner.Continue		+= new DiscCopy.NET.BurnerCallback.Continue(Burner_Continue);
			m_Burner.CopyProgress	+= new DiscCopy.NET.BurnerCallback.CopyProgress(Burner_CopyProgress);
			m_Burner.TrackStatus	+= new DiscCopy.NET.BurnerCallback.TrackStatus(Burner_TrackStatus);
			m_Burner.FormatProgress += new DiscCopy.NET.BurnerCallback.FormatProgress(Burner_FormatProgress);
			m_Burner.EraseProgress	+= new DiscCopy.NET.BurnerCallback.EraseProgress(Burner_EraseProgress);

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
						comboBoxSrcDevice.Items.Add(dev);
						comboBoxDstDevice.Items.Add(dev);
						deviceCount++;
					}
				}

				if (0 == deviceCount)
					throw new Exception("No writer devices found.");

				// Device combo
				comboBoxSrcDevice.SelectedIndex = 0;
				comboBoxDstDevice.SelectedIndex = 0;
				checkBoxUseTemporaryFiles.Checked = true;
				UpdateDeviceInformation();
			}
			catch (Exception e)
			{
				ShowErrorMessage(e);
			}
			CopyMode = CopyMode.Simple;
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
			if (NewDiscWaitForm.WM_DEVICECHANGE == msg.Msg)
				UpdateDeviceInformation();

			base.WndProc(ref msg);
		}

		private void buttonBrowse_Click(object sender, System.EventArgs e)
		{
			string sourceFolder = ShowFolderBrowser();
			if ("" == sourceFolder)
				return;
			
			textBoxRootDir.Text = sourceFolder;
		}

		private void buttonCopy_Click(object sender, EventArgs e)
		{
			if (!ValidateForm())
			{
				return;
			}
			m_bInProcess = true;
			switch(this.CopyMode)
			{
				case CopyMode.Simple:
					RunSimpleCopy();
					break;
				case CopyMode.Direct:
					RunDirectCopy();
					break;
			}
			m_bInProcess = false;

			UpdateDeviceInformation();
		}

		private void buttonExit_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void comboBoxSrcDevice_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if(-1 != comboBoxSrcDevice.SelectedIndex)
				UpdateDeviceInformation();
		}

		private void rbtnCopyMode_CheckedChanged(object sender, EventArgs e)
		{
			bool enableDst = CopyMode.Direct == this.CopyMode;
			comboBoxDstDevice.Enabled = enableDst;
			checkBoxUseTemporaryFiles.Enabled = enableDst;
			buttonBrowse.Enabled = !enableDst;
		}

		private void Burner_Status(string message)
		{
			m_progressInfo.Message = message;
			m_progressWindow.UpdateProgress(m_progressInfo);
		}

		private void Burner_CopyProgress(long position, long all)
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

		private void Burner_TrackStatus(int track, int percentCompleted)
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
		private int CreateImage(string imageFolder)
		{
			try
			{
				if (-1 == comboBoxSrcDevice.SelectedIndex)
				{
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);
				}

				DeviceInfo dev = (DeviceInfo)comboBoxSrcDevice.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true, false);

				CreateProgressWindow();

				CreateImageSettings settings = new CreateImageSettings();
				settings.ImageFolderPath = imageFolder;
				settings.ReadSubChannel = checkBoxReadSubChannel.Checked;

				CreateImageThreadDelegate thread = new CreateImageThreadDelegate(CreateImageThread);
				IAsyncResult ar = thread.BeginInvoke(settings, null, null);

				WaitForThreadToFinish(ar);

				int res = thread.EndInvoke(ar);
				if (THREAD_EXCEPTION == res)
				{
					ShowErrorMessage(m_ThreadException);
				}

				DestroyProgressWindow();
				m_Burner.ReleaseDevices();
				return res;
			}
			catch (BurnerException bme)
			{
				m_Burner.ReleaseDevices();
				ShowErrorMessage(bme);
				return THREAD_EXCEPTION;
			}
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

		private int BurnImage(string imageFolder)
		{
			try
			{
				if (-1 == comboBoxSrcDevice.SelectedIndex)
				{
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);
				}
				DeviceInfo dev = (DeviceInfo)comboBoxSrcDevice.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true, false);

				CreateProgressWindow();

				BurnImageSettings settings = new BurnImageSettings();
				settings.ImageFolderPath = imageFolder;

				BurnImageThreadDelegate thread = new BurnImageThreadDelegate(BurnImageThread);
				IAsyncResult ar = thread.BeginInvoke(settings, null, null);

				WaitForThreadToFinish(ar);
				int res = thread.EndInvoke(ar);
				if (THREAD_EXCEPTION == res)
				{
					ShowErrorMessage(m_ThreadException);
				}

				DestroyProgressWindow();
				m_Burner.ReleaseDevices();
				return res;
			}
			catch(BurnerException bme)
			{
				m_Burner.ReleaseDevices();
				ShowErrorMessage(bme);
				return THREAD_EXCEPTION;
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

		private int PrepareNewMedium()
		{
			try
			{
				if (-1 == comboBoxSrcDevice.SelectedIndex)
				{
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);
				}
				DeviceInfo dev = (DeviceInfo)comboBoxSrcDevice.SelectedItem;
				m_Burner.SelectDevice(dev.Index, false, false);

				NewDiscWaitForm dlg = new NewDiscWaitForm();
				dlg.SetBurner(m_Burner);
				DialogResult res = dlg.ShowDialog();
				if (DialogResult.OK == res)
				{
					if (m_Burner.MediaIsRewritable || m_Burner.BDRFormatAllowed)
					{
						CleanMediaSettings settings = new CleanMediaSettings();
						{
							settings.Quick = dlg.Quick;
							settings.MediaCleanMethod = dlg.SelectedCleanMethod;
						}
						m_Burner.ReleaseDevices();
						return CleanMedia(settings);
					}
					else
					{
						bool isBlank = m_Burner.MediaIsBlank;
						m_Burner.ReleaseDevices();
						return isBlank ? NOERROR : BurnerErrors.MEDIA_NOT_REWRITABLE;
					}
				}
				else
				{
					m_Burner.ReleaseDevices();
					return DISCCOPY_ONNEWDISCWAIT_CANCEL;
				}
			}
			catch (BurnerException bme)
			{
				m_Burner.ReleaseDevices();
				ShowErrorMessage(bme);
				return OPERATION_FAILED;
			}
		}

		private int CleanMedia(CleanMediaSettings settings)
		{
			try
			{
				if (-1 == comboBoxSrcDevice.SelectedIndex)
				{
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);
				}
				DeviceInfo dev = (DeviceInfo)comboBoxSrcDevice.SelectedItem;
				m_Burner.SelectDevice(dev.Index, true, false);

				if (m_Burner.MediaIsBlank)
				{
					string msg = string.Empty;
					switch (settings.MediaCleanMethod)
					{
						case CleanMethod.Erase:
							msg = "Media is already blank. Do you still want to erase it?";
							break;
						case CleanMethod.Format:
							msg = "Media is already blank. Do you still want to format it?";
							break;
						default:
							return NOERROR;
					}
					if (DialogResult.Cancel == MessageBox.Show(msg, "Warning!", MessageBoxButtons.OKCancel))
					{
						m_Burner.ReleaseDevices();
						return NOERROR;
					}
				}

				if (DialogResult.Cancel == MessageBox.Show("Continuing will destroy all information on the medium. Do you still wish to proceed?", "Warning!", MessageBoxButtons.OKCancel))
				{
					m_Burner.ReleaseDevices();
					return DISCCOPY_MEDIA_CLEAN_CANCEL;
				}

				CreateProgressWindow();
				m_progressWindow.Status = "Erasing. Please wait...";
				m_progressWindow.EnableStop = false;

				CleanMediaThreadDelegate thread = new CleanMediaThreadDelegate(CleanMediaThread);
				IAsyncResult ar = thread.BeginInvoke(settings, null, null);

				WaitForThreadToFinish(ar);
				int res = thread.EndInvoke(ar);
				if (THREAD_EXCEPTION == res)
				{
					ShowErrorMessage(m_ThreadException);
				}

				DestroyProgressWindow();
				m_Burner.ReleaseDevices();
				return res;
			}
			catch (BurnerException bme)
			{
				m_Burner.ReleaseDevices();
				ShowErrorMessage(bme);
				return OPERATION_FAILED;
			}
		}
		private delegate int CleanMediaThreadDelegate(CleanMediaSettings settings);
		private int CleanMediaThread(CleanMediaSettings settings)
		{
			try
			{
				m_Burner.CleanMedia(settings);
			}
			catch (BurnerException bme)
			{
				m_ThreadException = bme;
				return THREAD_EXCEPTION;
			}

			return NOERROR;
		}

		private delegate int DirectCopyThreadDelegate(DirectCopySettings settings);
		private int DirectCopyThread(DirectCopySettings settings)
		{
			try
			{
				m_Burner.DirectCopy(settings);
			}
			catch (BurnerException bme)
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
			MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
			if (m_bInProcess)
				return;
			if (-1 == comboBoxSrcDevice.SelectedIndex)
				return;

			try
			{
				DeviceInfo dev = (DeviceInfo)comboBoxSrcDevice.SelectedItem;

				// Select device. Exclusive access is not required.
				m_Burner.SelectDevice(dev.Index, false, false);

				bool copyAllowed = m_Burner.MediaIsValidProfile;
				buttonCopy.Enabled = copyAllowed;

				bool showReadSettings = false;
				bool showWriteSettings = m_Burner.MediaIsCD;
				if (copyAllowed)
				{
					showReadSettings = m_Burner.MediaCanReadSubChannel;
				}
				checkBoxReadSubChannel.Enabled = showReadSettings;
				comboBoxWriteMethods.Enabled = showWriteSettings;

				CDCopyWriteMethod method = GetWriteMethod();
				comboBoxWriteMethods.Items.Clear();
				int selectedItm = 0;
				if (m_Burner.RawDaoPossible)
				{
					comboBoxWriteMethods.Items.Add(new ListItem(CDCopyWriteMethod.CdRaw, "Disc-At-Once"));
					if (CDCopyWriteMethod.CdRaw == method)
					{
						selectedItm = comboBoxWriteMethods.Items.Count - 1;
					}
					comboBoxWriteMethods.Items.Add(new ListItem(CDCopyWriteMethod.CdRaw2352, "Disc-At-Once 2352 Bytes/sector"));
					if (CDCopyWriteMethod.CdRaw2352 == method)
					{
						selectedItm = comboBoxWriteMethods.Items.Count - 1;
					}
					comboBoxWriteMethods.Items.Add(new ListItem(CDCopyWriteMethod.CdFullRaw, "Disc-At-Once + SUB 2448 Bytes/sector"));
					if (CDCopyWriteMethod.CdFullRaw == method)
					{
						selectedItm = comboBoxWriteMethods.Items.Count - 1;
					}
				}
				comboBoxWriteMethods.Items.Add(new ListItem(CDCopyWriteMethod.CdCooked, "SAO/TAO/Packet"));
				if (CDCopyWriteMethod.CdCooked == method)
				{
					selectedItm = comboBoxWriteMethods.Items.Count - 1;
				}
				comboBoxWriteMethods.SelectedIndex = selectedItm;

				m_Burner.ReleaseDevices();
			}
			catch(BurnerException bme)
			{
				// Ignore the error when it is DEVICE_ALREADY_SELECTED
				if (BurnerErrors.DEVICE_ALREADY_SELECTED == bme.Error)
					return;

				// Report all other errors
				m_Burner.ReleaseDevices();
				ShowErrorMessage(bme);
			}
		}

		private bool ValidateForm()
		{
			if (CopyMode.Simple == this.CopyMode)
			{
				string strRootDir = textBoxRootDir.Text;

				if (!Directory.Exists(strRootDir))
				{
					MessageBox.Show("Please specify a valid source directory.");
					textBoxRootDir.Focus();
					return false;
				}
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

		private CDCopyWriteMethod GetWriteMethod()
		{
			ListItem sel = comboBoxWriteMethods.SelectedItem as ListItem;
			if (null != sel && sel.Value is CDCopyWriteMethod)
			{
				return (CDCopyWriteMethod)sel.Value;
			}
			return CDCopyWriteMethod.CdCooked;
		}

		private void RunSimpleCopy()
		{
			string imageFolder = textBoxRootDir.Text;
			if (NOERROR == CreateImage(imageFolder))
			{
				if (NOERROR == PrepareNewMedium())
				{
					BurnImage(imageFolder);
				}
			}
		}

		private void RunDirectCopy()
		{
			try
			{
				if (-1 == comboBoxSrcDevice.SelectedIndex)
				{
					throw BurnerException.CreateBurnerException(BurnerErrors.NO_DEVICE);
				}

				DeviceInfo devSrc = (DeviceInfo)comboBoxSrcDevice.SelectedItem;
				DeviceInfo devDst = (DeviceInfo)comboBoxDstDevice.SelectedItem;
				m_Burner.SelectDevice(devSrc.Index, true, false);
				m_Burner.SelectDevice(devDst.Index, true, true);

				CreateProgressWindow();

				DirectCopySettings settings = new DirectCopySettings();
				settings.ReadSubChannel = checkBoxReadSubChannel.Checked;
				settings.UseTemporaryFiles = checkBoxUseTemporaryFiles.Checked;
				settings.WriteMethod = GetWriteMethod();

				DirectCopyThreadDelegate thread = new DirectCopyThreadDelegate(DirectCopyThread);
				IAsyncResult ar = thread.BeginInvoke(settings, null, null);

				WaitForThreadToFinish(ar);

				int res = thread.EndInvoke(ar);
				if (THREAD_EXCEPTION == res)
				{
					ShowErrorMessage(m_ThreadException);
				}

				DestroyProgressWindow();
				m_Burner.ReleaseDevices();
			}
			catch (BurnerException bme)
			{
				m_Burner.ReleaseDevices();
				ShowErrorMessage(bme);
			}
		}

		private CopyMode CopyMode
		{
			get
			{
				if (rbtnSimpleCopy.Checked)
				{
					return CopyMode.Simple;
				}
				else if (rbtnDirectCopy.Checked)
				{
					return CopyMode.Direct;
				}
				return CopyMode.None;
			}
			set
			{
				switch (value)
				{
					case CopyMode.Simple:
						rbtnSimpleCopy.Checked = true;
						rbtnDirectCopy.Checked = false;
						break;
					case CopyMode.Direct:
						rbtnSimpleCopy.Checked = false;
						rbtnDirectCopy.Checked = true;
						break;
					default:
						rbtnSimpleCopy.Checked = false;
						rbtnDirectCopy.Checked = false;
						break;
				}
			}
		}

		#endregion

		#region Private Members
		private const int NOERROR = 0;
		private const int THREAD_EXCEPTION = 1;
		private const int OPERATION_FAILED = 2;
		private const int DISCCOPY_ONNEWDISCWAIT_CANCEL = 3;
		private const int DISCCOPY_MEDIA_CLEAN_CANCEL = 4;

		private Burner m_Burner = new Burner();
		private BurnerException m_ThreadException;

		private ProgressForm m_progressWindow;
		private ProgressInfo m_progressInfo = new ProgressInfo();

		private bool m_bInProcess = false;
            
		#endregion

		#region Windows Form Designer generated code
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox comboBoxSrcDevice;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Button buttonCopy;
		private System.Windows.Forms.TextBox textBoxRootDir;
		private GroupBox groupBoxReadSettings;
		private GroupBox groupBoxWriteSettings;
		private CheckBox checkBoxReadSubChannel;
		private Label label2;
		private ComboBox comboBoxWriteMethods;
		private Panel panel1;
		private Label label3;
		private ComboBox comboBoxDstDevice;
		private GroupBox groupBox1;
		private RadioButton rbtnDirectCopy;
		private RadioButton rbtnSimpleCopy;
		private CheckBox checkBoxUseTemporaryFiles;


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
			this.comboBoxSrcDevice = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxRootDir = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.buttonCopy = new System.Windows.Forms.Button();
			this.buttonExit = new System.Windows.Forms.Button();
			this.groupBoxReadSettings = new System.Windows.Forms.GroupBox();
			this.checkBoxUseTemporaryFiles = new System.Windows.Forms.CheckBox();
			this.checkBoxReadSubChannel = new System.Windows.Forms.CheckBox();
			this.groupBoxWriteSettings = new System.Windows.Forms.GroupBox();
			this.comboBoxWriteMethods = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxDstDevice = new System.Windows.Forms.ComboBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.rbtnDirectCopy = new System.Windows.Forms.RadioButton();
			this.rbtnSimpleCopy = new System.Windows.Forms.RadioButton();
			this.groupBoxReadSettings.SuspendLayout();
			this.groupBoxWriteSettings.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(115, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Select source device:";
			// 
			// comboBoxSrcDevice
			// 
			this.comboBoxSrcDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxSrcDevice.Location = new System.Drawing.Point(8, 27);
			this.comboBoxSrcDevice.Name = "comboBoxSrcDevice";
			this.comboBoxSrcDevice.Size = new System.Drawing.Size(367, 21);
			this.comboBoxSrcDevice.TabIndex = 0;
			this.comboBoxSrcDevice.SelectedIndexChanged += new System.EventHandler(this.comboBoxSrcDevice_SelectedIndexChanged);
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label4.Location = new System.Drawing.Point(8, 96);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(154, 16);
			this.label4.TabIndex = 4;
			this.label4.Text = "Select image output folder:";
			// 
			// textBoxRootDir
			// 
			this.textBoxRootDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBoxRootDir.Location = new System.Drawing.Point(8, 115);
			this.textBoxRootDir.Name = "textBoxRootDir";
			this.textBoxRootDir.ReadOnly = true;
			this.textBoxRootDir.Size = new System.Drawing.Size(367, 20);
			this.textBoxRootDir.TabIndex = 5;
			this.textBoxRootDir.TabStop = false;
			// 
			// buttonBrowse
			// 
			this.buttonBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.buttonBrowse.Location = new System.Drawing.Point(379, 113);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(92, 24);
			this.buttonBrowse.TabIndex = 3;
			this.buttonBrowse.Text = "Browse";
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			// 
			// buttonCopy
			// 
			this.buttonCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonCopy.Location = new System.Drawing.Point(8, 244);
			this.buttonCopy.Name = "buttonCopy";
			this.buttonCopy.Size = new System.Drawing.Size(88, 24);
			this.buttonCopy.TabIndex = 6;
			this.buttonCopy.Text = "Copy";
			this.buttonCopy.Click += new System.EventHandler(this.buttonCopy_Click);
			// 
			// buttonExit
			// 
			this.buttonExit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.buttonExit.Location = new System.Drawing.Point(362, 244);
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.Size = new System.Drawing.Size(109, 24);
			this.buttonExit.TabIndex = 7;
			this.buttonExit.Text = "Exit";
			this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
			// 
			// groupBoxReadSettings
			// 
			this.groupBoxReadSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBoxReadSettings.Controls.Add(this.checkBoxUseTemporaryFiles);
			this.groupBoxReadSettings.Controls.Add(this.checkBoxReadSubChannel);
			this.groupBoxReadSettings.Location = new System.Drawing.Point(8, 154);
			this.groupBoxReadSettings.Name = "groupBoxReadSettings";
			this.groupBoxReadSettings.Size = new System.Drawing.Size(166, 71);
			this.groupBoxReadSettings.TabIndex = 4;
			this.groupBoxReadSettings.TabStop = false;
			this.groupBoxReadSettings.Text = "Read settings:";
			// 
			// checkBoxUseTemporaryFiles
			// 
			this.checkBoxUseTemporaryFiles.AutoSize = true;
			this.checkBoxUseTemporaryFiles.Location = new System.Drawing.Point(27, 42);
			this.checkBoxUseTemporaryFiles.Name = "checkBoxUseTemporaryFiles";
			this.checkBoxUseTemporaryFiles.Size = new System.Drawing.Size(115, 17);
			this.checkBoxUseTemporaryFiles.TabIndex = 1;
			this.checkBoxUseTemporaryFiles.Text = "Use temporary files";
			this.checkBoxUseTemporaryFiles.UseVisualStyleBackColor = true;
			// 
			// checkBoxReadSubChannel
			// 
			this.checkBoxReadSubChannel.AutoSize = true;
			this.checkBoxReadSubChannel.Location = new System.Drawing.Point(28, 19);
			this.checkBoxReadSubChannel.Name = "checkBoxReadSubChannel";
			this.checkBoxReadSubChannel.Size = new System.Drawing.Size(113, 17);
			this.checkBoxReadSubChannel.TabIndex = 0;
			this.checkBoxReadSubChannel.Text = "Read sub-channel";
			this.checkBoxReadSubChannel.UseVisualStyleBackColor = true;
			// 
			// groupBoxWriteSettings
			// 
			this.groupBoxWriteSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxWriteSettings.Controls.Add(this.comboBoxWriteMethods);
			this.groupBoxWriteSettings.Controls.Add(this.label2);
			this.groupBoxWriteSettings.Location = new System.Drawing.Point(203, 154);
			this.groupBoxWriteSettings.Name = "groupBoxWriteSettings";
			this.groupBoxWriteSettings.Size = new System.Drawing.Size(268, 71);
			this.groupBoxWriteSettings.TabIndex = 5;
			this.groupBoxWriteSettings.TabStop = false;
			this.groupBoxWriteSettings.Text = "Write settings:";
			// 
			// comboBoxWriteMethods
			// 
			this.comboBoxWriteMethods.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxWriteMethods.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxWriteMethods.FormattingEnabled = true;
			this.comboBoxWriteMethods.Location = new System.Drawing.Point(12, 38);
			this.comboBoxWriteMethods.Name = "comboBoxWriteMethods";
			this.comboBoxWriteMethods.Size = new System.Drawing.Size(245, 21);
			this.comboBoxWriteMethods.TabIndex = 0;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(9, 19);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(88, 16);
			this.label2.TabIndex = 1;
			this.label2.Text = "Write method:";
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Location = new System.Drawing.Point(6, 233);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(465, 1);
			this.panel1.TabIndex = 34;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(8, 51);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(141, 16);
			this.label3.TabIndex = 35;
			this.label3.Text = "Select destination device:";
			// 
			// comboBoxDstDevice
			// 
			this.comboBoxDstDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxDstDevice.Location = new System.Drawing.Point(8, 70);
			this.comboBoxDstDevice.Name = "comboBoxDstDevice";
			this.comboBoxDstDevice.Size = new System.Drawing.Size(367, 21);
			this.comboBoxDstDevice.TabIndex = 2;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.rbtnDirectCopy);
			this.groupBox1.Controls.Add(this.rbtnSimpleCopy);
			this.groupBox1.Location = new System.Drawing.Point(379, 9);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(92, 98);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Copy Mode:";
			// 
			// rbtnDirectCopy
			// 
			this.rbtnDirectCopy.AutoSize = true;
			this.rbtnDirectCopy.Location = new System.Drawing.Point(13, 62);
			this.rbtnDirectCopy.Name = "rbtnDirectCopy";
			this.rbtnDirectCopy.Size = new System.Drawing.Size(53, 17);
			this.rbtnDirectCopy.TabIndex = 1;
			this.rbtnDirectCopy.Text = "Direct";
			this.rbtnDirectCopy.UseVisualStyleBackColor = true;
			this.rbtnDirectCopy.CheckedChanged += new System.EventHandler(this.rbtnCopyMode_CheckedChanged);
			// 
			// rbtnSimpleCopy
			// 
			this.rbtnSimpleCopy.AutoSize = true;
			this.rbtnSimpleCopy.Location = new System.Drawing.Point(13, 19);
			this.rbtnSimpleCopy.Name = "rbtnSimpleCopy";
			this.rbtnSimpleCopy.Size = new System.Drawing.Size(56, 17);
			this.rbtnSimpleCopy.TabIndex = 0;
			this.rbtnSimpleCopy.Text = "Simple";
			this.rbtnSimpleCopy.UseVisualStyleBackColor = true;
			this.rbtnSimpleCopy.CheckedChanged += new System.EventHandler(this.rbtnCopyMode_CheckedChanged);
			// 
			// BurnerForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(479, 275);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.comboBoxDstDevice);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.groupBoxWriteSettings);
			this.Controls.Add(this.groupBoxReadSettings);
			this.Controls.Add(this.buttonExit);
			this.Controls.Add(this.buttonCopy);
			this.Controls.Add(this.textBoxRootDir);
			this.Controls.Add(this.buttonBrowse);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.comboBoxSrcDevice);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BurnerForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "PrimoBurner(tm) Engine for .NET - DiscCopy.NET - Disc Copy Sample Application ";
			this.groupBoxReadSettings.ResumeLayout(false);
			this.groupBoxReadSettings.PerformLayout();
			this.groupBoxWriteSettings.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

	}
}
