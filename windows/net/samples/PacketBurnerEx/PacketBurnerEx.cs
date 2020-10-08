using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using PrimoSoftware.Burner;
using System.Collections.Generic;

namespace PacketBurnerEx.NET
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class PacketBurnerEx : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label lblDevices;
		private System.Windows.Forms.ComboBox cmbDevices;
		private System.Windows.Forms.TextBox txtFreeSpace;
		private System.Windows.Forms.TextBox txtRequiredSpace;
		private System.Windows.Forms.Label lblDeviceInfo;
		private System.Windows.Forms.Label lblPoint1;
		private System.Windows.Forms.Label lblPoint2;
		private System.Windows.Forms.Label lblPoint3;
		private System.Windows.Forms.Label lblWarning;
		private System.Windows.Forms.Label lblSelectFolder;
		private System.Windows.Forms.TextBox txtFolder;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.GroupBox groupParameters;
		private System.Windows.Forms.CheckBox chkEjectWhenDone;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupEjectMedia;
		private System.Windows.Forms.Button btnEject;
		private System.Windows.Forms.Button btnCloseTray;
		private System.Windows.Forms.GroupBox groupErase;
		private System.Windows.Forms.CheckBox chkQuickErase;
		private System.Windows.Forms.Button btnErase;
		private System.Windows.Forms.GroupBox groupFormat;
		private System.Windows.Forms.CheckBox chkQuickFormat;
		private System.Windows.Forms.Button btnFormat;
		private System.Windows.Forms.Button btnStart;
		private System.Windows.Forms.Button btnAppend;
		private System.Windows.Forms.Button btnFinalize;
		private System.Windows.Forms.Button btnExit;
		private System.Windows.Forms.ComboBox	cmbSpeed;

		const int WM_DEVICECHANGE = 0x0219;

		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;

		private long							_capacity;
		private long							_requiredSpace;
		private ArrayList						_deviceIndexes;
		private Engine							_engine;
		private DeviceEnumerator				_deviceEnum;
		private MediaProfile					_mediaProfile;
		private Progress						_progressForm;
		private Device							_device;
		private ShowProgressArgs				_progressArgs;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container	components = null;

		private string RootDir
		{
			get{return txtFolder.Text;}
			set{txtFolder.Text = value;}
		}
		public PacketBurnerEx()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            Library.EnableTraceLog(null, true);

			_deviceIndexes = new ArrayList();
			_mediaProfile = MediaProfile.Unknown;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (null != _deviceEnum)
				_deviceEnum.Dispose();

			if (null != _engine)
			{
				_engine.Shutdown();
				_engine.Dispose();
			}

            Library.DisableTraceLog();

			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.lblDevices = new System.Windows.Forms.Label();
			this.cmbDevices = new System.Windows.Forms.ComboBox();
			this.txtFreeSpace = new System.Windows.Forms.TextBox();
			this.txtRequiredSpace = new System.Windows.Forms.TextBox();
			this.lblDeviceInfo = new System.Windows.Forms.Label();
			this.lblPoint1 = new System.Windows.Forms.Label();
			this.lblPoint2 = new System.Windows.Forms.Label();
			this.lblPoint3 = new System.Windows.Forms.Label();
			this.lblWarning = new System.Windows.Forms.Label();
			this.lblSelectFolder = new System.Windows.Forms.Label();
			this.txtFolder = new System.Windows.Forms.TextBox();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.groupParameters = new System.Windows.Forms.GroupBox();
			this.cmbSpeed = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.chkEjectWhenDone = new System.Windows.Forms.CheckBox();
			this.groupEjectMedia = new System.Windows.Forms.GroupBox();
			this.btnCloseTray = new System.Windows.Forms.Button();
			this.btnEject = new System.Windows.Forms.Button();
			this.groupErase = new System.Windows.Forms.GroupBox();
			this.btnErase = new System.Windows.Forms.Button();
			this.chkQuickErase = new System.Windows.Forms.CheckBox();
			this.groupFormat = new System.Windows.Forms.GroupBox();
			this.btnFormat = new System.Windows.Forms.Button();
			this.chkQuickFormat = new System.Windows.Forms.CheckBox();
			this.btnStart = new System.Windows.Forms.Button();
			this.btnAppend = new System.Windows.Forms.Button();
			this.btnFinalize = new System.Windows.Forms.Button();
			this.btnExit = new System.Windows.Forms.Button();
			this.groupParameters.SuspendLayout();
			this.groupEjectMedia.SuspendLayout();
			this.groupErase.SuspendLayout();
			this.groupFormat.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblDevices
			// 
			this.lblDevices.Location = new System.Drawing.Point(16, 16);
			this.lblDevices.Name = "lblDevices";
			this.lblDevices.Size = new System.Drawing.Size(100, 16);
			this.lblDevices.TabIndex = 0;
			this.lblDevices.Text = "CD / DVD Writers:";
			// 
			// cmbDevices
			// 
			this.cmbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbDevices.Location = new System.Drawing.Point(16, 32);
			this.cmbDevices.Name = "cmbDevices";
			this.cmbDevices.Size = new System.Drawing.Size(240, 21);
			this.cmbDevices.TabIndex = 0;
			this.cmbDevices.SelectedIndexChanged += new System.EventHandler(this.cmbDevices_SelectedIndexChanged);
			// 
			// txtFreeSpace
			// 
			this.txtFreeSpace.Location = new System.Drawing.Point(264, 32);
			this.txtFreeSpace.Name = "txtFreeSpace";
			this.txtFreeSpace.ReadOnly = true;
			this.txtFreeSpace.Size = new System.Drawing.Size(104, 20);
			this.txtFreeSpace.TabIndex = 11;
			this.txtFreeSpace.TabStop = false;
			this.txtFreeSpace.Text = "";
			// 
			// txtRequiredSpace
			// 
			this.txtRequiredSpace.Location = new System.Drawing.Point(376, 32);
			this.txtRequiredSpace.Name = "txtRequiredSpace";
			this.txtRequiredSpace.ReadOnly = true;
			this.txtRequiredSpace.Size = new System.Drawing.Size(128, 20);
			this.txtRequiredSpace.TabIndex = 12;
			this.txtRequiredSpace.TabStop = false;
			this.txtRequiredSpace.Text = "";
			// 
			// lblDeviceInfo
			// 
			this.lblDeviceInfo.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblDeviceInfo.Location = new System.Drawing.Point(16, 56);
			this.lblDeviceInfo.Name = "lblDeviceInfo";
			this.lblDeviceInfo.Size = new System.Drawing.Size(488, 20);
			this.lblDeviceInfo.TabIndex = 13;
			// 
			// lblPoint1
			// 
			this.lblPoint1.Location = new System.Drawing.Point(16, 80);
			this.lblPoint1.Name = "lblPoint1";
			this.lblPoint1.Size = new System.Drawing.Size(488, 32);
			this.lblPoint1.TabIndex = 14;
			this.lblPoint1.Text = "1. Select a folder and click the \'Start\' button to add it\'s content to the disc a" +
				"nd start a new multi-track session.";
			// 
			// lblPoint2
			// 
			this.lblPoint2.Location = new System.Drawing.Point(16, 120);
			this.lblPoint2.Name = "lblPoint2";
			this.lblPoint2.Size = new System.Drawing.Size(488, 16);
			this.lblPoint2.TabIndex = 15;
			this.lblPoint2.Text = "2. Select a folder and click the \'Append\' button to add it\'s content to the disc." +
				"";
			// 
			// lblPoint3
			// 
			this.lblPoint3.Location = new System.Drawing.Point(16, 144);
			this.lblPoint3.Name = "lblPoint3";
			this.lblPoint3.Size = new System.Drawing.Size(488, 32);
			this.lblPoint3.TabIndex = 16;
			this.lblPoint3.Text = "3. Select a folder and click the \'Finaslize Disc\' button to add it\'s content to t" +
				"he disc and finalize the multi-track session.";
			// 
			// lblWarning
			// 
			this.lblWarning.Location = new System.Drawing.Point(16, 184);
			this.lblWarning.Name = "lblWarning";
			this.lblWarning.Size = new System.Drawing.Size(488, 32);
			this.lblWarning.TabIndex = 17;
			this.lblWarning.Text = "WARNING: The operating system will not be able to see any files you burned until " +
				"you finalize the disc. ";
			// 
			// lblSelectFolder
			// 
			this.lblSelectFolder.Location = new System.Drawing.Point(16, 224);
			this.lblSelectFolder.Name = "lblSelectFolder";
			this.lblSelectFolder.Size = new System.Drawing.Size(100, 16);
			this.lblSelectFolder.TabIndex = 18;
			this.lblSelectFolder.Text = "Select a folder:";
			// 
			// txtFolder
			// 
			this.txtFolder.Location = new System.Drawing.Point(96, 224);
			this.txtFolder.Name = "txtFolder";
			this.txtFolder.Size = new System.Drawing.Size(328, 20);
			this.txtFolder.TabIndex = 1;
			this.txtFolder.Text = "";
			// 
			// btnBrowse
			// 
			this.btnBrowse.Location = new System.Drawing.Point(432, 224);
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.Size = new System.Drawing.Size(72, 23);
			this.btnBrowse.TabIndex = 2;
			this.btnBrowse.Text = "Browse";
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			// 
			// groupParameters
			// 
			this.groupParameters.Controls.AddRange(new System.Windows.Forms.Control[] {
																						  this.cmbSpeed,
																						  this.label1,
																						  this.chkEjectWhenDone});
			this.groupParameters.Location = new System.Drawing.Point(16, 248);
			this.groupParameters.Name = "groupParameters";
			this.groupParameters.Size = new System.Drawing.Size(296, 56);
			this.groupParameters.TabIndex = 3;
			this.groupParameters.TabStop = false;
			this.groupParameters.Text = "Parameters";
			// 
			// cmbSpeed
			// 
			this.cmbSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbSpeed.Location = new System.Drawing.Point(240, 22);
			this.cmbSpeed.Name = "cmbSpeed";
			this.cmbSpeed.Size = new System.Drawing.Size(48, 21);
			this.cmbSpeed.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(144, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(96, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "Recording Speed:";
			// 
			// chkEjectWhenDone
			// 
			this.chkEjectWhenDone.Location = new System.Drawing.Point(8, 24);
			this.chkEjectWhenDone.Name = "chkEjectWhenDone";
			this.chkEjectWhenDone.Size = new System.Drawing.Size(112, 16);
			this.chkEjectWhenDone.TabIndex = 0;
			this.chkEjectWhenDone.Text = "Eject when done";
			// 
			// groupEjectMedia
			// 
			this.groupEjectMedia.Controls.AddRange(new System.Windows.Forms.Control[] {
																						  this.btnCloseTray,
																						  this.btnEject});
			this.groupEjectMedia.Location = new System.Drawing.Point(320, 248);
			this.groupEjectMedia.Name = "groupEjectMedia";
			this.groupEjectMedia.Size = new System.Drawing.Size(184, 56);
			this.groupEjectMedia.TabIndex = 4;
			this.groupEjectMedia.TabStop = false;
			this.groupEjectMedia.Text = "Eject Media";
			// 
			// btnCloseTray
			// 
			this.btnCloseTray.Location = new System.Drawing.Point(96, 24);
			this.btnCloseTray.Name = "btnCloseTray";
			this.btnCloseTray.Size = new System.Drawing.Size(72, 23);
			this.btnCloseTray.TabIndex = 1;
			this.btnCloseTray.Text = "Close Tray";
			this.btnCloseTray.Click += new System.EventHandler(this.btnCloseTray_Click);
			// 
			// btnEject
			// 
			this.btnEject.Location = new System.Drawing.Point(16, 24);
			this.btnEject.Name = "btnEject";
			this.btnEject.Size = new System.Drawing.Size(72, 23);
			this.btnEject.TabIndex = 0;
			this.btnEject.Text = "Eject";
			this.btnEject.Click += new System.EventHandler(this.btnEject_Click);
			// 
			// groupErase
			// 
			this.groupErase.Controls.AddRange(new System.Windows.Forms.Control[] {
																					 this.btnErase,
																					 this.chkQuickErase});
			this.groupErase.Location = new System.Drawing.Point(16, 312);
			this.groupErase.Name = "groupErase";
			this.groupErase.Size = new System.Drawing.Size(160, 56);
			this.groupErase.TabIndex = 5;
			this.groupErase.TabStop = false;
			this.groupErase.Text = "Erase";
			// 
			// btnErase
			// 
			this.btnErase.Location = new System.Drawing.Point(80, 21);
			this.btnErase.Name = "btnErase";
			this.btnErase.Size = new System.Drawing.Size(72, 23);
			this.btnErase.TabIndex = 1;
			this.btnErase.Text = "Erase";
			this.btnErase.Click += new System.EventHandler(this.btnErase_Click);
			// 
			// chkQuickErase
			// 
			this.chkQuickErase.Location = new System.Drawing.Point(8, 24);
			this.chkQuickErase.Name = "chkQuickErase";
			this.chkQuickErase.Size = new System.Drawing.Size(56, 16);
			this.chkQuickErase.TabIndex = 0;
			this.chkQuickErase.Text = "Quick";
			// 
			// groupFormat
			// 
			this.groupFormat.Controls.AddRange(new System.Windows.Forms.Control[] {
																					  this.btnFormat,
																					  this.chkQuickFormat});
			this.groupFormat.Location = new System.Drawing.Point(184, 312);
			this.groupFormat.Name = "groupFormat";
			this.groupFormat.Size = new System.Drawing.Size(160, 56);
			this.groupFormat.TabIndex = 6;
			this.groupFormat.TabStop = false;
			this.groupFormat.Text = "Format";
			// 
			// btnFormat
			// 
			this.btnFormat.Location = new System.Drawing.Point(80, 21);
			this.btnFormat.Name = "btnFormat";
			this.btnFormat.Size = new System.Drawing.Size(72, 23);
			this.btnFormat.TabIndex = 1;
			this.btnFormat.Text = "Format";
			this.btnFormat.Click += new System.EventHandler(this.btnFormat_Click);
			// 
			// chkQuickFormat
			// 
			this.chkQuickFormat.Location = new System.Drawing.Point(8, 24);
			this.chkQuickFormat.Name = "chkQuickFormat";
			this.chkQuickFormat.Size = new System.Drawing.Size(56, 16);
			this.chkQuickFormat.TabIndex = 0;
			this.chkQuickFormat.Text = "Quick";
			// 
			// btnStart
			// 
			this.btnStart.Location = new System.Drawing.Point(16, 384);
			this.btnStart.Name = "btnStart";
			this.btnStart.Size = new System.Drawing.Size(88, 23);
			this.btnStart.TabIndex = 7;
			this.btnStart.Text = "1. Start";
			this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
			// 
			// btnAppend
			// 
			this.btnAppend.Location = new System.Drawing.Point(112, 384);
			this.btnAppend.Name = "btnAppend";
			this.btnAppend.TabIndex = 8;
			this.btnAppend.Text = "2. Append";
			this.btnAppend.Click += new System.EventHandler(this.btnAppend_Click);
			// 
			// btnFinalize
			// 
			this.btnFinalize.Location = new System.Drawing.Point(192, 384);
			this.btnFinalize.Name = "btnFinalize";
			this.btnFinalize.Size = new System.Drawing.Size(96, 23);
			this.btnFinalize.TabIndex = 9;
			this.btnFinalize.Text = "3. Finalize Disc";
			this.btnFinalize.Click += new System.EventHandler(this.btnFinalize_Click);
			// 
			// btnExit
			// 
			this.btnExit.Location = new System.Drawing.Point(416, 384);
			this.btnExit.Name = "btnExit";
			this.btnExit.TabIndex = 10;
			this.btnExit.Text = "Exit";
			this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
			// 
			// PacketBurnerEx
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(522, 423);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.btnExit,
																		  this.btnFinalize,
																		  this.btnAppend,
																		  this.btnStart,
																		  this.groupFormat,
																		  this.groupErase,
																		  this.groupEjectMedia,
																		  this.groupParameters,
																		  this.btnBrowse,
																		  this.txtFolder,
																		  this.txtRequiredSpace,
																		  this.txtFreeSpace,
																		  this.lblSelectFolder,
																		  this.lblWarning,
																		  this.lblPoint3,
																		  this.lblPoint2,
																		  this.lblPoint1,
																		  this.lblDeviceInfo,
																		  this.cmbDevices,
																		  this.lblDevices});
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "PacketBurnerEx";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "PacketBurnerEx.NET - Multi-track Incremental Burning";
			this.Load += new System.EventHandler(this.PacketBurnerEx_Load);
			this.groupParameters.ResumeLayout(false);
			this.groupEjectMedia.ResumeLayout(false);
			this.groupErase.ResumeLayout(false);
			this.groupFormat.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
            // Initialize the SDK
            PrimoSoftware.Burner.Library.Initialize();

            // Set license string
            const string license = @"<primoSoftware></primoSoftware>";
            PrimoSoftware.Burner.Library.SetLicense(license);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new PacketBurnerEx());

            // Shutdown the SDK
            PrimoSoftware.Burner.Library.Shutdown();

		}

		private void GetSupportedWriteSpeeds(Device dev)
		{
			string sSelectedSpeed = "";
			if(cmbSpeed.SelectedIndex > -1)
				sSelectedSpeed = ((SpeedListItem)cmbSpeed.SelectedItem).ToString();

			cmbSpeed.Items.Clear();

            IList<SpeedDescriptor> writeSpeeds = dev.GetWriteSpeeds();
			for (int i = 0; i < writeSpeeds.Count; i++)
			{
				SpeedDescriptor desc = writeSpeeds[i];
						
                SpeedListItem item = new SpeedListItem(dev.MediaIsDVD, desc.TransferRateKB);
				cmbSpeed.Items.Add(item);
			}
			
			cmbSpeed.SelectedIndex = cmbSpeed.FindString(sSelectedSpeed);
			if (-1 == cmbSpeed.SelectedIndex)
				cmbSpeed.SelectedIndex = 0;
		}

		private void SetDeviceControls()
		{
			int nDevice = (int)cmbDevices.SelectedIndex;
			if (-1 == nDevice)
				return;

			_device = _deviceEnum.CreateDevice((int)_deviceIndexes[nDevice]);
			if (null == _device) 
			{
				// Auto insert notification
				MessageBox.Show("CPacketBurnerExDlg::SetDeviceControls : Unable to get device object\nPossible reason : auto-insert notification enabled\nwhile writing (or any other) operation is in progress.\nThis is just a warning message - your CD is not damaged.","Error!",MessageBoxButtons.OK,MessageBoxIcon.Error);
				return; 
			}

			// Get and display the media profile
			_mediaProfile = _device.MediaProfile;
			lblDeviceInfo.Text = (GetProfileDescription(_device));

			_capacity = _device.MediaFreeSpace;
			_capacity *= (long)BlockSize.Dvd;

			if (0 == _capacity || _capacity < _requiredSpace) 
			{
				btnStart.Enabled = false;
				btnAppend.Enabled = false;
			}
			else 
			{
				btnStart.Enabled = true;
				btnAppend.Enabled = true;
			}

			GetSupportedWriteSpeeds(_device);

			txtFreeSpace.Text = String.Format("Free space : {0}GB", ((double)_capacity/(1e9)).ToString("0.00"));
			txtRequiredSpace.Text = String.Format("Required space : {0}GB", ((double)_requiredSpace/(1e9)).ToString("0.00"));

			_device.Dispose();
		}

		private void PacketBurnerEx_Load(object sender, System.EventArgs e)
		{
			// Add extra initialization here
			_capacity = 0;
			_requiredSpace = 0;
			
			// Write parameters
			chkEjectWhenDone.Checked = false;

			// Erase/Format parameters
			chkQuickErase.Checked = true;
			chkQuickFormat.Checked = true;

			// 	Initialize _engine / Create Engine
			_engine = new Engine();
			if (!_engine.Initialize()) 
			{
				MessageBox.Show("Unable to initialize _engine.","Error!",MessageBoxButtons.OK,MessageBoxIcon.Error);
				return;
			}

			// Get device enumerator
			_deviceEnum = _engine.CreateDeviceEnumerator();
			int nDevices = _deviceEnum.Count;
			if (0 == nDevices) 
			{
				MessageBox.Show("No devices available.","Error!",MessageBoxButtons.OK,MessageBoxIcon.Error);
				if (null != _deviceEnum) 
				{
					_deviceEnum.Dispose();
					_deviceEnum = null;
				}

				return;
			}

			_deviceIndexes.Clear();

			// Count the CD/DVD writers
			for (int i = 0; i < nDevices;i++) 
			{
				// Get next device
				Device pDevice = _deviceEnum.CreateDevice(i);

				// Always check for null
				if (null == pDevice)
					continue;
		
				// Add only writers
				string sDesc = "";
				char sLetter;
					
				sDesc = pDevice.Description;
				sLetter = pDevice.DriveLetter;
				_deviceIndexes.Add(i);

				string sName = string.Format("({0}:) - {1}", sLetter, sDesc);

				cmbDevices.Items.Add(new ComboItem(i,sName));

				// Release device object
				pDevice.Dispose();
			}

			cmbDevices.ValueMember = "Number";
			cmbDevices.DisplayMember = "Data";

			// Select the first device
			if (_deviceIndexes.Count > 0)
			{
				cmbDevices.SelectedIndex = 0;
				SetDeviceControls();
			}
			else 
			{
				MessageBox.Show("Could not find any CD/DVD writer devices.","Error!",MessageBoxButtons.OK,MessageBoxIcon.Error);

				_deviceEnum.Dispose();
				_deviceEnum = null;

				return;
			}
		}

		private void ProcessInputTree(DataFile pCurrentFile, string sCurrentPath)
		{
			ArrayList FilesandDirs = new ArrayList(Directory.GetFiles(sCurrentPath,"*"));
			FilesandDirs.AddRange(Directory.GetDirectories(sCurrentPath,"*"));

			foreach (string sEntry in FilesandDirs)
			{
				if (Directory.Exists(sEntry))
				{
					// Skip the curent folder . and the parent folder .. 
					if (sEntry == "."  || sEntry == "..")
						continue;

					// Create a folder entry and scan it for the files
					DataFile pDataFile = new DataFile(); 
					DirectoryInfo di = new DirectoryInfo(sEntry);
					pDataFile.IsDirectory = true;
					pDataFile.FilePath = di.Name;
					pDataFile.LongFilename = di.Name;
                    pDataFile.FileTime = di.CreationTime;

                    if ((di.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        pDataFile.HiddenMask = (int)ImageType.Udf;

					// Search for all files
					ProcessInputTree(pDataFile, sEntry);

					// Add this folder to the tree
					pCurrentFile.Children.Add(pDataFile);
				} 
				else
				{
					// File
					DataFile pDataFile = new DataFile(); 
					pDataFile.IsDirectory = false;
					FileInfo fi = new FileInfo(sEntry);
					pDataFile.FilePath = fi.FullName;
					pDataFile.LongFilename = fi.Name;
                    pDataFile.FileTime = fi.CreationTime;

                    if ((fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        pDataFile.HiddenMask = (int)ImageType.Udf;

                    // Add to the tree
					pCurrentFile.Children.Add(pDataFile);
				}
			}
		}

		private bool SetEmptyImageLayout(DataDisc pDataDisc)
		{
			DataFile pDataFile = new DataFile(); 

			// Entry for the root of the image file system
			pDataFile.IsDirectory = true;
			pDataFile.FilePath = "\\";
			pDataFile.LongFilename = "\\";			

			bool bRes = pDataDisc.SetImageLayout(pDataFile);

			return bRes;
		}

		private bool SetImageLayoutFromFolder(DataDisc dataDisc, string fname)
		{
			DataFile pDataFile = new DataFile(); 

			// Entry for the root of the image file system
			pDataFile.IsDirectory = true;
			pDataFile.FilePath = "\\";			
			pDataFile.LongFilename = "\\";

			try
			{
				string sFullPath = fname;
				ProcessInputTree(pDataFile, sFullPath);
			}
			catch(Exception error)
			{
				string sError = string.Format("SetImageLayoutFromFolder Error: {0}" , error.Message);
				MessageBox.Show(sError);

				return false;
			}

			bool bRes = dataDisc.SetImageLayout(pDataFile);
			
			return bRes;
		}

		private bool DirectoryExists()
		{
			return Directory.Exists(RootDir);
		}

		private bool ValidateForm() 
		{
			if (!DirectoryExists()) 
			{
				MessageBox.Show("Please specify a valid directory.","Error!",MessageBoxButtons.OK,MessageBoxIcon.Error);
				txtFolder.Focus();
				return false;
			}

			if ('\\' == RootDir[RootDir.Length-1] || 
				'/' == RootDir[RootDir.Length-1]) 
			{
				RootDir = RootDir.Substring(0,RootDir.Length-1);
			}

			return true;
		}
		
		private void StartProcess(EAction Action)
		{
			if (EAction.ACTION_NONE == Action)
				return;

			this.Enabled = false;
			_progressForm = new Progress();
			_progressForm.Owner = this;
			_progressForm.burningDone = new BurningDoneHandler(BurningDone);
			_progressForm.Show();

			ProcessContext ctx				= new ProcessContext();

			ctx.bEject						= chkEjectWhenDone.Checked;
			if (EAction.ACTION_ERASE == Action)
				ctx.bQuick					= chkQuickErase.Checked;
			else if (EAction.ACTION_FORMAT == Action)
				ctx.bQuick					= chkQuickFormat.Checked;
			ctx.bStopRequest				= false;
			ctx.eAction						= Action;
			ctx.nSpeedKB					= ((SpeedListItem)cmbSpeed.SelectedItem).SpeedKB;
			ctx.sRootDir					= RootDir;

			ProcessDelegate  process = new ProcessDelegate(Process);
			process.BeginInvoke(ctx, null, null);
		}

		delegate void ProcessDelegate(ProcessContext ctx);
		public void BurningDone()
		{
			this.Enabled = true;
			if(null != _progressForm)
			{
				_progressForm.Close();
				_progressForm = null;
			}

			if(null != _device)
			{
				_device.Dispose();
				_device = null;
			}

			this.Focus();
			SetDeviceControls();
		}

		private void Process(ProcessContext ctx)
		{
			_progressArgs = new ShowProgressArgs();
			try
			{
				int nDevice = cmbDevices.SelectedIndex;
				_device = _deviceEnum.CreateDevice((int)_deviceIndexes[nDevice]);

				switch(ctx.eAction)
				{
					case EAction.ACTION_BURN_START:
						Burn(_device, false, false, ctx);
						break;
					case EAction.ACTION_BURN_APPEND:
						Burn(_device, false, true, ctx);
						break;
					case EAction.ACTION_BURN_FINALIZE:
						Burn(_device, true, true, ctx);
						break;
					case EAction.ACTION_ERASE:
						Erase(_device, ctx.bQuick);
						break;
					case EAction.ACTION_FORMAT:
						Format(_device, ctx.bQuick);
						break;
				}
			}
			finally
			{
				_progressArgs.bDone = true;
				_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
			}
		}

		private int GetLastTrack(Device pDevice)
		{
			// Get the last track number from the last session if multisession option was specified
			int nLastTrack = 0;

			// Check for DVD+RW and DVD-RW RO random writable media. 
			MediaProfile mp = pDevice.MediaProfile;
			if (MediaProfile.DvdPlusRw == mp || MediaProfile.DvdMinusRwRo == mp)
			{
				// DVD+RW and DVD-RW RO has only one session with one track
				if (pDevice.MediaFreeSpace > 0)
					nLastTrack = 1;	
			}		
			else
			{
				// All other media are recorded using tracks and sessions and multi-session is no different 
				// than with the CD. 

				// Use the ReadDiskInfo method to get the last track number
                DiscInfo di = pDevice.ReadDiscInfo();
				if (null != di)
					nLastTrack = di.LastTrack;
			}

			return nLastTrack;
		}

		private void SetParameters(DataDisc pDataDisc, bool bCloseSessionAndDisc, ProcessContext ctx)
		{
			pDataDisc.ImageType = ImageType.Udf;

			pDataDisc.UdfVolumeProps.VolumeLabel = "HPCDEDISC";
			
			pDataDisc.OnContinueBurn += OnContinueBurn;
			pDataDisc.OnFileStatus += OnFileStatus;
			pDataDisc.OnProgress += OnProgress;
			pDataDisc.OnStatus += OnStatus;

			pDataDisc.SimulateBurn = false;

			// Packet mode
			pDataDisc.WriteMethod = WriteMethod.Packet;

            DataWriteStrategy writeStrategy = DataWriteStrategy.None;

            switch (ctx.eAction)
            {
                case EAction.ACTION_BURN_START:
                    writeStrategy = DataWriteStrategy.ReserveFileTableTrack;
                    break;
                case EAction.ACTION_BURN_FINALIZE:
                    writeStrategy = DataWriteStrategy.WriteFileTableTrack;
                    break;
            }

			pDataDisc.WriteStrategy = writeStrategy;

            pDataDisc.CloseTrack = (DataWriteStrategy.WriteFileTableTrack == writeStrategy);
			pDataDisc.CloseSession = bCloseSessionAndDisc;
			pDataDisc.CloseDisc = bCloseSessionAndDisc;
		}

		private bool Burn(Device pDevice, bool bFinalize, bool bLoadLastTrack, ProcessContext ctx)
		{
			if (null == pDevice)
				return false;

			// Get disk information
            DiscInfo di = pDevice.ReadDiscInfo();
			if (null == di)
				return false;

			// Create DataDisc object 
			DataDisc pDataDisc = new DataDisc();
			pDataDisc.Device = pDevice;

			// Set burning parameters
			SetParameters(pDataDisc, bFinalize, ctx);

			// Set the session start address. Must do this before intializing the directory structure.
			if (SessionState.Open == di.SessionState)
				pDataDisc.SessionStartAddress = pDevice.NewTrackStartAddress;
			else
				pDataDisc.SessionStartAddress = pDevice.NewSessionStartAddress;

			// Get the last track number from the last session if multi-session option was specified
			int nPrevTrackNumber = bLoadLastTrack ? GetLastTrack(pDevice) : 0;
			pDataDisc.LoadTrackLayout = nPrevTrackNumber;

			// Set image layout
			bool bRes = SetImageLayoutFromFolder(pDataDisc, ctx.sRootDir);
			if (!bRes) 
			{
				ShowErrorMessage(pDataDisc, _device);

				pDataDisc.Dispose();
				return false;
			}

			// Set speed
			pDevice.WriteSpeedKB = ctx.nSpeedKB;

			// Burn 
			while (true)
			{
				// Try to write the image
				bRes = pDataDisc.WriteToDisc(false);
				if (!bRes)
				{
                    ErrorInfo error = pDataDisc.Error;

					// Check if the error is: Cannot load image layout. 
					// If so most likely it is an empty formatted DVD+RW or empty formatted DVD-RW RO with one track. 
					if (ErrorFacility.DataDisc == error.Facility &&
                        DataDiscError.CannotLoadImageLayout == (DataDiscError)error.Code)
					{
						// Set to 0 to disable previous data session loading
						pDataDisc.LoadTrackLayout = 0;

						// try to write it again
						continue;
					}
				}

				break;
			}

			// Check for errors
			if (!bRes) 
			{
				ShowErrorMessage(pDataDisc, _device);
				pDataDisc.Dispose();
				return false;
			}

			if (ctx.bEject)
				pDevice.Eject(true);

			pDataDisc.Dispose();

			return true;
		}

		private bool Format(Device device, bool quick)
		{
			_progressArgs.status = "Formatting disc. Please wait...";
			_progressForm.StopButtonEnabled = false;
			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);

			MediaProfile mp = device.MediaProfile;
			if((MediaProfile.DvdMinusRwSeq != mp) && 
				(MediaProfile.DvdMinusRwRo != mp) && 
				(MediaProfile.DvdPlusRw != mp))
			{
				MessageBox.Show("Formatting is supported on DVD-RW and DVD+RW media only.");
				return false;
			}

			if(MessageBox.Show("Formatting will destroy all the information on the disc. Do you want to continue?", "", MessageBoxButtons.OKCancel) != DialogResult.OK)
			{
				return false;
			}

			bool bRes = false;
			device.OnFormat += OnFormatProgress;

			switch(mp)
			{
				case MediaProfile.DvdMinusRwRo:
					bRes = device.Format(quick ? FormatType.DvdMinusRwQuick : FormatType.DvdMinusRwFull, 0, false);
					break;

				case MediaProfile.DvdMinusRwSeq:
					bRes = device.Format(quick ? FormatType.DvdMinusRwQuick : FormatType.DvdMinusRwFull, 0, false);
					break;
					
				case MediaProfile.DvdPlusRw:
				{
					BgFormatStatus fmt = device.BgFormatStatus;
					switch(fmt)
					{
						case BgFormatStatus.NotFormatted:
							bRes = device.Format(FormatType.DvdPlusRwFull, 0, !quick);
							break;
						case BgFormatStatus.Partial:
							bRes = device.Format(FormatType.DvdPlusRwRestart, 0, !quick);
							break;
					}
				}
					break;
			}
			device.OnFormat -= OnFormatProgress;
			return bRes;
		}

		private bool Erase(Device device, bool quick)
		{
			_progressArgs.status = "Erasing disc. Please wait...";
			_progressForm.StopButtonEnabled = false;

			MediaProfile mp = device.MediaProfile;
			if((MediaProfile.DvdMinusRwSeq != mp) && 
				(MediaProfile.DvdMinusRwRo != mp) && 
				(MediaProfile.CdRw != mp))
			{
				MessageBox.Show("Erasing is supported on CD-RW and DVD-RW media only.");
				return false;
			}

			if(MessageBox.Show("Erasing will destroy all the information on the disc. Do you want to continue?", "", MessageBoxButtons.OKCancel) != DialogResult.OK)
			{
				return false;
			}

			if ((MediaProfile.DvdMinusRwRo == mp) && quick)
			{
				if(MessageBox.Show("The media is DVD-RW in restricted overwrite mode and quick erase " +
					"cannot be performed. Do you want to perform a full erase instead?"
					, "", MessageBoxButtons.OKCancel) != DialogResult.OK)
				{
					return true;
				}

				quick = false;
			}
	
			device.OnErase += OnEraseProgress;
			
			bool bRes = device.Erase(quick ? EraseType.Minimal : EraseType.Disc);
			
			device.OnErase -= OnEraseProgress;

			return bRes;
		}

		#region Burning event handlers
		public void OnContinueBurn(Object sender, DataDiscContinueEventArgs eArgs)
		{
			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
			eArgs.Continue = !_progressArgs.bStopRequest;
		}

		public void OnProgress(Object sender, DataDiscProgressEventArgs eArgs)
		{
			long dwPos = eArgs.Position;
			long dwAll = eArgs.All;
			if (dwPos > dwAll)
				dwPos = dwAll;

			_progressArgs.progressPos = (int)((double)dwPos * 100.0 / (double)dwAll);

			if(null != _device)
			{
				_progressArgs.bufferPos = (int)((100 * (double)(_device.InternalCacheUsedSpace)) / ((double)_device.InternalCacheCapacity));
				_progressArgs.actualWriteSpeed = _device.WriteTransferRate;
			}
			else
			{
				_progressArgs.bufferPos = 0;
				_progressArgs.actualWriteSpeed = 0;
			}

			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
		}

		public void OnFileStatus(Object sender, DataDiscFileStatusEventArgs eArgs)
		{
			// nothing to do
		}

		public void OnStatus(Object sender, DataDiscStatusEventArgs eArgs)
		{
			_progressArgs.status = GetTextStatus(eArgs.Status);
			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
		}

		#endregion

		#region Erase / Format event handlers
		public void OnEraseProgress(Object sender, DeviceEraseEventArgs eArgs)
		{
			_progressArgs.progressPos = Convert.ToInt32(eArgs.Progress);
			_progressArgs.bufferPos = 0;
			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
		}

		public void OnFormatProgress(Object sender, DeviceFormatEventArgs eArgs)
		{
			_progressArgs.progressPos = Convert.ToInt32(eArgs.Progress);
			_progressArgs.bufferPos = 0;
			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
		}
		#endregion

		#region Controls' event handlers
		private void btnBrowse_Click(object sender, System.EventArgs e)
		{
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				RootDir = folderBrowserDialog.SelectedPath;

                DataDisc dataCD = new DataDisc();
                dataCD.ImageType = ImageType.Udf;

                Cursor.Current = Cursors.WaitCursor;

				if(!dataCD.SetImageLayoutFromFolder(folderBrowserDialog.SelectedPath))
				{
					MessageBox.Show(String.Format("A problem occured when trying to set the source directory to:\n{0}", RootDir));
					RootDir = "";
				}
				else
				{
					_requiredSpace = dataCD.ImageSizeInBytes;
				}

				Cursor.Current = Cursors.Arrow;
				dataCD.Dispose();
				SetDeviceControls();
			}
		}

		private void cmbDevices_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int nCurSel = cmbDevices.SelectedIndex;
			if (-1 != nCurSel)
				SetDeviceControls();
		}

		private void btnStart_Click(object sender, System.EventArgs e)
		{
			if (!ValidateForm())
				return;

			StartProcess(EAction.ACTION_BURN_START);
		}
		
		private void btnAppend_Click(object sender, System.EventArgs e)
		{
			if (!ValidateForm())
				return;

			StartProcess(EAction.ACTION_BURN_APPEND);
		}

		private void btnFinalize_Click(object sender, System.EventArgs e)
		{
			StartProcess(EAction.ACTION_BURN_FINALIZE);
		}


		private void btnEject_Click(object sender, System.EventArgs e)
		{
			int nDevice = cmbDevices.SelectedIndex;
			_device = _deviceEnum.CreateDevice((int)_deviceIndexes[nDevice]);
			if (null != _device)
			{
				_device.Eject(true);
				_device.Dispose();
			}
		}

		private void btnCloseTray_Click(object sender, System.EventArgs e)
		{
			int nDevice = cmbDevices.SelectedIndex;
			_device = _deviceEnum.CreateDevice((int)_deviceIndexes[nDevice]);
			if (null != _device)
			{
				_device.Eject(false);
				_device.Dispose();
			}
		}

		private void btnErase_Click(object sender, System.EventArgs e)
		{
			StartProcess(EAction.ACTION_ERASE);
		}

		private void btnFormat_Click(object sender, System.EventArgs e)
		{
			StartProcess(EAction.ACTION_FORMAT);
		}

		private void btnExit_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		#endregion

		private string CreateErrorMessage(int systemError)
		{
			return new System.ComponentModel.Win32Exception(systemError).Message;
		}

		private string CreateErrorMessage(Device device)
		{
			string message = string.Empty;
			if (null != device)
			{
                ErrorInfo error = device.Error;
				switch (error.Facility)
				{
					case ErrorFacility.SystemWindows:
						message = CreateErrorMessage(error.Code);
						break;
					default:
                        message = string.Format("Device error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, error.Code, error.Message);
						break;
				}
			}
			return message;
		}

		private string CreateErrorMessage(DataDisc dataDisc, Device device)
		{
			string message = string.Empty;
			if (null != dataDisc)
			{
                ErrorInfo error = dataDisc.Error;
                switch (error.Facility)
                {
                    case ErrorFacility.SystemWindows:
                        message = CreateErrorMessage(error.Code);
                        break;
                    case ErrorFacility.Device:
						message = CreateErrorMessage(device);
						break;
					default:
						message = string.Format("DataDisc error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, error.Code, error.Message);
						break;
				}
			}
			return message;
		}

		void ShowErrorMessage(DataDisc dataDisc, Device device)
		{
			string message = CreateErrorMessage(dataDisc, device);
			MessageBox.Show(message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public string GetSystemErrorMessage(int iErrorCode)
		{
			StringBuilder sb = new StringBuilder(512);
			int iReturn;

			iReturn = Kernel32.FormatMessage(
				(int)Kernel32.FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM,
				IntPtr.Zero,
				iErrorCode, 
				0, 
				sb, 
				sb.Capacity, 
				IntPtr.Zero);

			return sb.ToString();
		}

		private string GetProfileDescription( Device pDevice)
		{
			switch(_mediaProfile)
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
					return "DVD-R Sequential Recording. Write once DVD using sequential recording.";

				case MediaProfile.DvdMinusRDLSeq:
					return "DVD-R DL 8.54GB for Sequential Recording. Write once DVD.";

				case MediaProfile.DvdMinusRDLJump:
					return "DVD-R DL 8.54GB for Layer Jump Recording. Write once DVD.";

				case MediaProfile.DvdRam:
					return "DVD-RAM ReWritable DVD.";

				case MediaProfile.DvdMinusRwRo:
					return "DVD-RW Restricted Overwrite ReWritable. ReWritable DVD using restricted overwrite.";

				case MediaProfile.DvdMinusRwSeq:
					return "DVD-RW Sequential Recording ReWritable. ReWritable DVD using sequential recording.";

				case MediaProfile.DvdPlusRw:
				{
					BgFormatStatus fmt = pDevice.BgFormatStatus;
					switch(fmt)
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
					return "DVD+R Write once DVD.";

				case MediaProfile.DvdPlusRDL:
					return "DVD+R DL Double Layer Write once DVD+R.";

				default:
					return "Unknown Profile.";
			}
		}

		string GetTextStatus(DataDiscStatus eStatus)
		{
			switch(eStatus)
			{
				case DataDiscStatus.BuildingFileSystem:
					return "Building filesystem...";

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
					return "Initializing...";

				case DataDiscStatus.Writing:
					return "Writing...";

				case DataDiscStatus.WritingLeadOut:
					return "Writing...";

				case DataDiscStatus.LoadingImageLayout:
					return "Loading image layout from last track...";
			}

			return "Unknown status...";
		}

		protected override void WndProc(ref Message msg)
		{
			if (WM_DEVICECHANGE == msg.Msg)
			{
				if (cmbDevices.Items.Count > 0)
					SetDeviceControls();
			}

			base.WndProc(ref msg);
		}
	}
	public enum EAction
	{
		ACTION_NONE	= 0,	
		ACTION_BURN_START,
		ACTION_BURN_APPEND,
		ACTION_BURN_FINALIZE,
		ACTION_ERASE,
		ACTION_FORMAT,
	};
	public class Kernel32
	{
		public enum FormatMessageFlags : int
		{
			FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
			FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
			FORMAT_MESSAGE_FROM_STRING = 0x00000400,
			FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
			FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
			FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000
		}

		[DllImport("kernel32.dll")]
		public static extern int FormatMessage(
			int lFlags,
			IntPtr lSource,
			int lMessageId,
			int lLanguageId,
			StringBuilder sBuffer,
			int lSize,
			IntPtr lArguments);
	}

	public struct ComboItem
	{
		int iNum;
		object oData;

		public int Number
		{
			get {return iNum;}
		}

		public object Data
		{
			get {return oData;}
		}

		public ComboItem(int Num,object Data)
		{
			iNum = Num;
			oData = Data;
		}
	};
	public struct ProcessContext 
	{
		public EAction	eAction;		// current action
		public bool		bQuick;			// indicates quick format or erase

		public bool		bEject;			// eject device after burning
		public int		nSpeedKB;			// burning speed // not used
		public string	sRootDir;			// directory to add

		public bool		bStopRequest;	// indicates whether user pressed the stop button on the progress dialog
	};

	internal class ShowProgressArgs: EventArgs
	{
		public ShowProgressArgs()
		{
			bStopRequest = false;
			bDone		 = false;
			progressPos  = 0;
			bufferPos	 = 0;
		}

		public bool bStopRequest;
		public bool bDone;
		public string status;
		public int progressPos;
		public int bufferPos;
		public int actualWriteSpeed;
	}

	internal class SpeedListItem
	{
		public bool IsDVD;
		public int SpeedKB;

		public SpeedListItem(bool _isDVD, int _speedKB)
		{
			IsDVD = _isDVD;
			SpeedKB = _speedKB;
		}

		public override string ToString()
		{
			if (IsDVD)
				return string.Format("{0}x", Math.Round((double)SpeedKB / Speed1xKB.DVD, 1));

			return string.Format("{0}x", Math.Round((double)SpeedKB / Speed1xKB.CD));
		}
	}

}
