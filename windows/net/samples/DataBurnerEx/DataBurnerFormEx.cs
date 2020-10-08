using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using PrimoSoftware.Burner;
using System.Collections.Generic;

namespace DataBurnerEx.NET
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class DataBurnerExForm : System.Windows.Forms.Form
	{

		const int IDC_MYCOMMAND = 0x0013;
		const int WM_SYSCOMMAND = 0x0112;
		const int WM_DEVICECHANGE = 0x0219;

		private System.Windows.Forms.ComboBox cmbDevices;
		private System.Windows.Forms.Label lblCDWriters;
		private System.Windows.Forms.TextBox txtFreeSpace;
		private System.Windows.Forms.TextBox txtRequiredSpace;
		private System.Windows.Forms.Label lblFolderToBurn;
		private System.Windows.Forms.Button cmdBrowse;
		private System.Windows.Forms.Label lblVolumeName;
		private System.Windows.Forms.TextBox txtVolumeName;
		private System.Windows.Forms.Label lblImageType;
		private System.Windows.Forms.ComboBox cmbImageType;
		private System.Windows.Forms.GroupBox grpISO;
		private System.Windows.Forms.GroupBox grpJoliet;
		private System.Windows.Forms.RadioButton rdbISOAll;
		private System.Windows.Forms.RadioButton rdbISOLevel2;
		private System.Windows.Forms.RadioButton rdbISOLevel1;
		private System.Windows.Forms.CheckBox chkTreeDepth;
		private System.Windows.Forms.CheckBox chkTranslateName;
		private System.Windows.Forms.RadioButton rdbJolietAll;
		private System.Windows.Forms.RadioButton rdbJolietShortNames;
		private System.Windows.Forms.TextBox txtRoot;
		private System.Windows.Forms.GroupBox grpParameters;
		private System.Windows.Forms.CheckBox chkTest;
		private System.Windows.Forms.CheckBox chkEject;
		private System.Windows.Forms.CheckBox chkCloseDisc;
		private System.Windows.Forms.Label lblRecordingSpeed;
		private System.Windows.Forms.ComboBox cmbSpeed;
		private System.Windows.Forms.Label lblRecordingMode;
		private System.Windows.Forms.ComboBox cmbRecordingMode;
		private System.Windows.Forms.CheckBox chkRaw;
		private System.Windows.Forms.GroupBox grpEjectDevice;
		private System.Windows.Forms.Button cmdEjectIn;
		private System.Windows.Forms.Button cmdEjectOut;
		private System.Windows.Forms.GroupBox grpErase;
		private System.Windows.Forms.CheckBox chkQuick;
		private System.Windows.Forms.GroupBox grpMultisession;
		private System.Windows.Forms.CheckBox chkLoadLastTrack;
		private System.Windows.Forms.Button cmdBurn;
		private System.Windows.Forms.Button cmdCreateImage;
		private System.Windows.Forms.Button cmdBurnImage;
		private System.Windows.Forms.Button cmdExit;
		private System.Windows.Forms.Button cmdErase;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private Engine				m_Engine = null;
		private DeviceEnumerator	m_Enumerator = null;
		private int					m_iDevicesCount = 0;
		private long				m_nCapacity = 0;
		private long				m_nRequiredSpace = 0;
		private bool				m_bRawDao = false;
		
        private ArrayList			m_Streams = null;
		private OperationContext	m_ctx;
		private Device			m_Device = null;

		private Progress			dlgProgress = null;
		private ShowProgressArgs	progressArgs = null;

		string RootDir
		{
			get {return this.txtRoot.Text.Trim();}
			set {this.txtRoot.Text = value;}
		}

		string VolumeName
		{
			get {return this.txtVolumeName.Text.Trim();}
			set {this.txtVolumeName.Text = value;}
		}

		ImageType SelectedImageType
		{
			get 
			{
				switch(cmbImageType.SelectedIndex)
				{
					case 0:
						return ImageType.Iso9660;
					case 1:
						return ImageType.Joliet;
					case 2:
						return ImageType.Udf;
					case 3:
						return ImageType.UdfIso;
					case 4:
						return ImageType.UdfJoliet;
					default:
						return ImageType.Joliet;
				}
			}
		}


		public DataBurnerExForm()
		{
			InitializeComponent();

			IntPtr hSysMenu = Kernel32.GetSystemMenu(this.Handle, 0);
			Kernel32.AppendMenu(hSysMenu,MenuFlags.MF_SEPARATOR,0,null);
			Kernel32.AppendMenu(hSysMenu,
				MenuFlags.MF_BYCOMMAND, IDC_MYCOMMAND,
				"About DataBurnerEx...");

            Library.EnableTraceLog(null, true);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			DestroyStreams();

			if (null != m_Enumerator)
				m_Enumerator.Dispose();

			if (null != m_Engine)
			{
				m_Engine.Shutdown();
				m_Engine.Dispose();
			}

            Library.DisableTraceLog();

			if (disposing)
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		
		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.cmbDevices = new System.Windows.Forms.ComboBox();
			this.lblCDWriters = new System.Windows.Forms.Label();
			this.txtFreeSpace = new System.Windows.Forms.TextBox();
			this.txtRequiredSpace = new System.Windows.Forms.TextBox();
			this.lblFolderToBurn = new System.Windows.Forms.Label();
			this.txtRoot = new System.Windows.Forms.TextBox();
			this.cmdBrowse = new System.Windows.Forms.Button();
			this.lblVolumeName = new System.Windows.Forms.Label();
			this.txtVolumeName = new System.Windows.Forms.TextBox();
			this.lblImageType = new System.Windows.Forms.Label();
			this.cmbImageType = new System.Windows.Forms.ComboBox();
			this.grpISO = new System.Windows.Forms.GroupBox();
			this.chkTranslateName = new System.Windows.Forms.CheckBox();
			this.chkTreeDepth = new System.Windows.Forms.CheckBox();
			this.rdbISOLevel1 = new System.Windows.Forms.RadioButton();
			this.rdbISOLevel2 = new System.Windows.Forms.RadioButton();
			this.rdbISOAll = new System.Windows.Forms.RadioButton();
			this.grpJoliet = new System.Windows.Forms.GroupBox();
			this.rdbJolietShortNames = new System.Windows.Forms.RadioButton();
			this.rdbJolietAll = new System.Windows.Forms.RadioButton();
			this.grpParameters = new System.Windows.Forms.GroupBox();
			this.chkRaw = new System.Windows.Forms.CheckBox();
			this.cmbRecordingMode = new System.Windows.Forms.ComboBox();
			this.lblRecordingMode = new System.Windows.Forms.Label();
			this.cmbSpeed = new System.Windows.Forms.ComboBox();
			this.lblRecordingSpeed = new System.Windows.Forms.Label();
			this.chkCloseDisc = new System.Windows.Forms.CheckBox();
			this.chkEject = new System.Windows.Forms.CheckBox();
			this.chkTest = new System.Windows.Forms.CheckBox();
			this.grpEjectDevice = new System.Windows.Forms.GroupBox();
			this.cmdEjectOut = new System.Windows.Forms.Button();
			this.cmdEjectIn = new System.Windows.Forms.Button();
			this.grpErase = new System.Windows.Forms.GroupBox();
			this.cmdErase = new System.Windows.Forms.Button();
			this.chkQuick = new System.Windows.Forms.CheckBox();
			this.grpMultisession = new System.Windows.Forms.GroupBox();
			this.chkLoadLastTrack = new System.Windows.Forms.CheckBox();
			this.cmdBurn = new System.Windows.Forms.Button();
			this.cmdCreateImage = new System.Windows.Forms.Button();
			this.cmdBurnImage = new System.Windows.Forms.Button();
			this.cmdExit = new System.Windows.Forms.Button();
			this.grpISO.SuspendLayout();
			this.grpJoliet.SuspendLayout();
			this.grpParameters.SuspendLayout();
			this.grpEjectDevice.SuspendLayout();
			this.grpErase.SuspendLayout();
			this.grpMultisession.SuspendLayout();
			this.SuspendLayout();
			// 
			// cmbDevices
			// 
			this.cmbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbDevices.Location = new System.Drawing.Point(13, 30);
			this.cmbDevices.Name = "cmbDevices";
			this.cmbDevices.Size = new System.Drawing.Size(336, 24);
			this.cmbDevices.TabIndex = 0;
			this.cmbDevices.SelectedIndexChanged += new System.EventHandler(this.cmbDevices_SelectedIndexChanged);
			// 
			// lblCDWriters
			// 
			this.lblCDWriters.Location = new System.Drawing.Point(13, 7);
			this.lblCDWriters.Name = "lblCDWriters";
			this.lblCDWriters.Size = new System.Drawing.Size(76, 23);
			this.lblCDWriters.TabIndex = 1;
			this.lblCDWriters.Text = "CD Writers:";
			this.lblCDWriters.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// txtFreeSpace
			// 
			this.txtFreeSpace.Location = new System.Drawing.Point(356, 30);
			this.txtFreeSpace.Name = "txtFreeSpace";
			this.txtFreeSpace.ReadOnly = true;
			this.txtFreeSpace.Size = new System.Drawing.Size(156, 23);
			this.txtFreeSpace.TabIndex = 2;
			this.txtFreeSpace.Text = "";
			// 
			// txtRequiredSpace
			// 
			this.txtRequiredSpace.Location = new System.Drawing.Point(520, 30);
			this.txtRequiredSpace.Name = "txtRequiredSpace";
			this.txtRequiredSpace.ReadOnly = true;
			this.txtRequiredSpace.Size = new System.Drawing.Size(152, 23);
			this.txtRequiredSpace.TabIndex = 3;
			this.txtRequiredSpace.Text = "";
			// 
			// lblFolderToBurn
			// 
			this.lblFolderToBurn.Location = new System.Drawing.Point(13, 60);
			this.lblFolderToBurn.Name = "lblFolderToBurn";
			this.lblFolderToBurn.Size = new System.Drawing.Size(138, 22);
			this.lblFolderToBurn.TabIndex = 4;
			this.lblFolderToBurn.Text = "Select a folder to burn:";
			this.lblFolderToBurn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// txtRoot
			// 
			this.txtRoot.Location = new System.Drawing.Point(151, 60);
			this.txtRoot.Name = "txtRoot";
			this.txtRoot.ReadOnly = true;
			this.txtRoot.Size = new System.Drawing.Size(370, 23);
			this.txtRoot.TabIndex = 5;
			this.txtRoot.Text = "";
			// 
			// cmdBrowse
			// 
			this.cmdBrowse.Location = new System.Drawing.Point(569, 60);
			this.cmdBrowse.Name = "cmdBrowse";
			this.cmdBrowse.Size = new System.Drawing.Size(75, 22);
			this.cmdBrowse.TabIndex = 6;
			this.cmdBrowse.Text = "Browse";
			this.cmdBrowse.Click += new System.EventHandler(this.cmdBrowse_Click);
			// 
			// lblVolumeName
			// 
			this.lblVolumeName.Location = new System.Drawing.Point(13, 90);
			this.lblVolumeName.Name = "lblVolumeName";
			this.lblVolumeName.Size = new System.Drawing.Size(96, 22);
			this.lblVolumeName.TabIndex = 7;
			this.lblVolumeName.Text = "Volume name :";
			this.lblVolumeName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// txtVolumeName
			// 
			this.txtVolumeName.Location = new System.Drawing.Point(109, 90);
			this.txtVolumeName.Name = "txtVolumeName";
			this.txtVolumeName.Size = new System.Drawing.Size(166, 23);
			this.txtVolumeName.TabIndex = 8;
			this.txtVolumeName.Text = "";
			// 
			// lblImageType
			// 
			this.lblImageType.Location = new System.Drawing.Point(307, 90);
			this.lblImageType.Name = "lblImageType";
			this.lblImageType.Size = new System.Drawing.Size(87, 22);
			this.lblImageType.TabIndex = 9;
			this.lblImageType.Text = "Image Type:";
			this.lblImageType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// cmbImageType
			// 
			this.cmbImageType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbImageType.Items.AddRange(new object[] {
															  "ISO9660",
															  "Joliet",
															  "UDF",
															  "UDF & ISO9660",
															  "UDF & Joliet"});
			this.cmbImageType.Location = new System.Drawing.Point(397, 90);
			this.cmbImageType.Name = "cmbImageType";
			this.cmbImageType.Size = new System.Drawing.Size(172, 24);
			this.cmbImageType.TabIndex = 10;
			this.cmbImageType.SelectedIndexChanged += new System.EventHandler(this.cmbImageType_SelectedIndexChanged);
			// 
			// grpISO
			// 
			this.grpISO.Controls.Add(this.chkTranslateName);
			this.grpISO.Controls.Add(this.chkTreeDepth);
			this.grpISO.Controls.Add(this.rdbISOLevel1);
			this.grpISO.Controls.Add(this.rdbISOLevel2);
			this.grpISO.Controls.Add(this.rdbISOAll);
			this.grpISO.Location = new System.Drawing.Point(13, 121);
			this.grpISO.Name = "grpISO";
			this.grpISO.Size = new System.Drawing.Size(443, 82);
			this.grpISO.TabIndex = 11;
			this.grpISO.TabStop = false;
			this.grpISO.Text = "ISO9660";
			// 
			// chkTranslateName
			// 
			this.chkTranslateName.Enabled = false;
			this.chkTranslateName.Location = new System.Drawing.Point(260, 53);
			this.chkTranslateName.Name = "chkTranslateName";
			this.chkTranslateName.Size = new System.Drawing.Size(143, 22);
			this.chkTranslateName.TabIndex = 4;
			this.chkTranslateName.Text = "Translate File Names";
			// 
			// chkTreeDepth
			// 
			this.chkTreeDepth.Enabled = false;
			this.chkTreeDepth.Location = new System.Drawing.Point(13, 53);
			this.chkTreeDepth.Name = "chkTreeDepth";
			this.chkTreeDepth.Size = new System.Drawing.Size(199, 22);
			this.chkTreeDepth.TabIndex = 3;
			this.chkTreeDepth.Text = "Restrict Tree Depth to 8 levels";
			// 
			// rdbISOLevel1
			// 
			this.rdbISOLevel1.Enabled = false;
			this.rdbISOLevel1.Location = new System.Drawing.Point(323, 22);
			this.rdbISOLevel1.Name = "rdbISOLevel1";
			this.rdbISOLevel1.Size = new System.Drawing.Size(109, 24);
			this.rdbISOLevel1.TabIndex = 2;
			this.rdbISOLevel1.Text = "Level1(8.3)";
			// 
			// rdbISOLevel2
			// 
			this.rdbISOLevel2.Enabled = false;
			this.rdbISOLevel2.Location = new System.Drawing.Point(188, 22);
			this.rdbISOLevel2.Name = "rdbISOLevel2";
			this.rdbISOLevel2.Size = new System.Drawing.Size(108, 24);
			this.rdbISOLevel2.TabIndex = 1;
			this.rdbISOLevel2.Text = "Level2 (31)";
			// 
			// rdbISOAll
			// 
			this.rdbISOAll.Enabled = false;
			this.rdbISOAll.Location = new System.Drawing.Point(13, 22);
			this.rdbISOAll.Name = "rdbISOAll";
			this.rdbISOAll.Size = new System.Drawing.Size(147, 24);
			this.rdbISOAll.TabIndex = 0;
			this.rdbISOAll.Text = "Level 2 Long (212)";
			// 
			// grpJoliet
			// 
			this.grpJoliet.Controls.Add(this.rdbJolietShortNames);
			this.grpJoliet.Controls.Add(this.rdbJolietAll);
			this.grpJoliet.Location = new System.Drawing.Point(464, 121);
			this.grpJoliet.Name = "grpJoliet";
			this.grpJoliet.Size = new System.Drawing.Size(208, 82);
			this.grpJoliet.TabIndex = 12;
			this.grpJoliet.TabStop = false;
			this.grpJoliet.Text = "Joliet";
			// 
			// rdbJolietShortNames
			// 
			this.rdbJolietShortNames.Location = new System.Drawing.Point(13, 53);
			this.rdbJolietShortNames.Name = "rdbJolietShortNames";
			this.rdbJolietShortNames.Size = new System.Drawing.Size(144, 22);
			this.rdbJolietShortNames.TabIndex = 1;
			this.rdbJolietShortNames.Text = "Standard Names (64)";
			// 
			// rdbJolietAll
			// 
			this.rdbJolietAll.Location = new System.Drawing.Point(13, 22);
			this.rdbJolietAll.Name = "rdbJolietAll";
			this.rdbJolietAll.Size = new System.Drawing.Size(144, 24);
			this.rdbJolietAll.TabIndex = 0;
			this.rdbJolietAll.Text = "Long Names (107)";
			// 
			// grpParameters
			// 
			this.grpParameters.Controls.Add(this.chkRaw);
			this.grpParameters.Controls.Add(this.cmbRecordingMode);
			this.grpParameters.Controls.Add(this.lblRecordingMode);
			this.grpParameters.Controls.Add(this.cmbSpeed);
			this.grpParameters.Controls.Add(this.lblRecordingSpeed);
			this.grpParameters.Controls.Add(this.chkCloseDisc);
			this.grpParameters.Controls.Add(this.chkEject);
			this.grpParameters.Controls.Add(this.chkTest);
			this.grpParameters.Location = new System.Drawing.Point(13, 208);
			this.grpParameters.Name = "grpParameters";
			this.grpParameters.Size = new System.Drawing.Size(443, 113);
			this.grpParameters.TabIndex = 16;
			this.grpParameters.TabStop = false;
			this.grpParameters.Text = "Parameters";
			// 
			// chkRaw
			// 
			this.chkRaw.Enabled = false;
			this.chkRaw.Location = new System.Drawing.Point(164, 82);
			this.chkRaw.Name = "chkRaw";
			this.chkRaw.Size = new System.Drawing.Size(89, 24);
			this.chkRaw.TabIndex = 7;
			this.chkRaw.Text = "Raw Mode";
			this.chkRaw.CheckedChanged += new System.EventHandler(this.chkRaw_CheckedChanged);
			// 
			// cmbRecordingMode
			// 
			this.cmbRecordingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbRecordingMode.Items.AddRange(new object[] {
																  "Disk-At-Once",
																  "Session-At-Once",
																  "Track-At-Once",
																  "Packet"});
			this.cmbRecordingMode.Location = new System.Drawing.Point(304, 53);
			this.cmbRecordingMode.Name = "cmbRecordingMode";
			this.cmbRecordingMode.Size = new System.Drawing.Size(129, 24);
			this.cmbRecordingMode.TabIndex = 6;
			this.cmbRecordingMode.SelectedIndexChanged += new System.EventHandler(this.cmbRecordingMode_SelectedIndexChanged);
			// 
			// lblRecordingMode
			// 
			this.lblRecordingMode.Location = new System.Drawing.Point(164, 53);
			this.lblRecordingMode.Name = "lblRecordingMode";
			this.lblRecordingMode.Size = new System.Drawing.Size(132, 21);
			this.lblRecordingMode.TabIndex = 5;
			this.lblRecordingMode.Text = "Recording Mode:";
			this.lblRecordingMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// cmbSpeed
			// 
			this.cmbSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbSpeed.Location = new System.Drawing.Point(304, 22);
			this.cmbSpeed.MaxDropDownItems = 4;
			this.cmbSpeed.Name = "cmbSpeed";
			this.cmbSpeed.Size = new System.Drawing.Size(68, 24);
			this.cmbSpeed.TabIndex = 4;
			// 
			// lblRecordingSpeed
			// 
			this.lblRecordingSpeed.Location = new System.Drawing.Point(164, 22);
			this.lblRecordingSpeed.Name = "lblRecordingSpeed";
			this.lblRecordingSpeed.Size = new System.Drawing.Size(132, 21);
			this.lblRecordingSpeed.TabIndex = 3;
			this.lblRecordingSpeed.Text = "Recording Speed:";
			this.lblRecordingSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// chkCloseDisc
			// 
			this.chkCloseDisc.Checked = true;
			this.chkCloseDisc.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkCloseDisc.Enabled = false;
			this.chkCloseDisc.Location = new System.Drawing.Point(13, 82);
			this.chkCloseDisc.Name = "chkCloseDisc";
			this.chkCloseDisc.Size = new System.Drawing.Size(90, 24);
			this.chkCloseDisc.TabIndex = 2;
			this.chkCloseDisc.Text = "Close Disc";
			// 
			// chkEject
			// 
			this.chkEject.Location = new System.Drawing.Point(13, 53);
			this.chkEject.Name = "chkEject";
			this.chkEject.Size = new System.Drawing.Size(118, 22);
			this.chkEject.TabIndex = 1;
			this.chkEject.Text = "Eject when done";
			// 
			// chkTest
			// 
			this.chkTest.Location = new System.Drawing.Point(13, 22);
			this.chkTest.Name = "chkTest";
			this.chkTest.Size = new System.Drawing.Size(99, 24);
			this.chkTest.TabIndex = 0;
			this.chkTest.Text = "Simulate";
			// 
			// grpEjectDevice
			// 
			this.grpEjectDevice.Controls.Add(this.cmdEjectOut);
			this.grpEjectDevice.Controls.Add(this.cmdEjectIn);
			this.grpEjectDevice.Location = new System.Drawing.Point(464, 208);
			this.grpEjectDevice.Name = "grpEjectDevice";
			this.grpEjectDevice.Size = new System.Drawing.Size(102, 113);
			this.grpEjectDevice.TabIndex = 17;
			this.grpEjectDevice.TabStop = false;
			this.grpEjectDevice.Text = "Eject Device";
			// 
			// cmdEjectOut
			// 
			this.cmdEjectOut.Location = new System.Drawing.Point(13, 53);
			this.cmdEjectOut.Name = "cmdEjectOut";
			this.cmdEjectOut.Size = new System.Drawing.Size(76, 21);
			this.cmdEjectOut.TabIndex = 1;
			this.cmdEjectOut.Text = "Eject";
			this.cmdEjectOut.Click += new System.EventHandler(this.cmdEjectOut_Click);
			// 
			// cmdEjectIn
			// 
			this.cmdEjectIn.Location = new System.Drawing.Point(13, 22);
			this.cmdEjectIn.Name = "cmdEjectIn";
			this.cmdEjectIn.Size = new System.Drawing.Size(76, 22);
			this.cmdEjectIn.TabIndex = 0;
			this.cmdEjectIn.Text = "Close Tray";
			this.cmdEjectIn.Click += new System.EventHandler(this.cmdEjectIn_Click);
			// 
			// grpErase
			// 
			this.grpErase.Controls.Add(this.cmdErase);
			this.grpErase.Controls.Add(this.chkQuick);
			this.grpErase.Location = new System.Drawing.Point(568, 208);
			this.grpErase.Name = "grpErase";
			this.grpErase.Size = new System.Drawing.Size(104, 113);
			this.grpErase.TabIndex = 18;
			this.grpErase.TabStop = false;
			this.grpErase.Text = "Erase";
			// 
			// cmdErase
			// 
			this.cmdErase.Location = new System.Drawing.Point(13, 53);
			this.cmdErase.Name = "cmdErase";
			this.cmdErase.Size = new System.Drawing.Size(76, 21);
			this.cmdErase.TabIndex = 1;
			this.cmdErase.Text = "Erase disc";
			this.cmdErase.Click += new System.EventHandler(this.cmdErase_Click);
			// 
			// chkQuick
			// 
			this.chkQuick.Location = new System.Drawing.Point(13, 22);
			this.chkQuick.Name = "chkQuick";
			this.chkQuick.Size = new System.Drawing.Size(76, 24);
			this.chkQuick.TabIndex = 0;
			this.chkQuick.Text = "Quick";
			// 
			// grpMultisession
			// 
			this.grpMultisession.Controls.Add(this.chkLoadLastTrack);
			this.grpMultisession.Location = new System.Drawing.Point(13, 328);
			this.grpMultisession.Name = "grpMultisession";
			this.grpMultisession.Size = new System.Drawing.Size(443, 53);
			this.grpMultisession.TabIndex = 19;
			this.grpMultisession.TabStop = false;
			this.grpMultisession.Text = "Multisession";
			// 
			// chkLoadLastTrack
			// 
			this.chkLoadLastTrack.Checked = true;
			this.chkLoadLastTrack.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkLoadLastTrack.Location = new System.Drawing.Point(13, 22);
			this.chkLoadLastTrack.Name = "chkLoadLastTrack";
			this.chkLoadLastTrack.Size = new System.Drawing.Size(124, 24);
			this.chkLoadLastTrack.TabIndex = 0;
			this.chkLoadLastTrack.Text = "Load last track ";
			// 
			// cmdBurn
			// 
			this.cmdBurn.Location = new System.Drawing.Point(13, 392);
			this.cmdBurn.Name = "cmdBurn";
			this.cmdBurn.Size = new System.Drawing.Size(90, 31);
			this.cmdBurn.TabIndex = 20;
			this.cmdBurn.Text = "Burn";
			this.cmdBurn.Click += new System.EventHandler(this.cmdBurn_Click);
			// 
			// cmdCreateImage
			// 
			this.cmdCreateImage.Location = new System.Drawing.Point(124, 392);
			this.cmdCreateImage.Name = "cmdCreateImage";
			this.cmdCreateImage.Size = new System.Drawing.Size(88, 31);
			this.cmdCreateImage.TabIndex = 21;
			this.cmdCreateImage.Text = "Create Image";
			this.cmdCreateImage.Click += new System.EventHandler(this.cmdCreateImage_Click);
			// 
			// cmdBurnImage
			// 
			this.cmdBurnImage.Location = new System.Drawing.Point(233, 392);
			this.cmdBurnImage.Name = "cmdBurnImage";
			this.cmdBurnImage.Size = new System.Drawing.Size(90, 31);
			this.cmdBurnImage.TabIndex = 22;
			this.cmdBurnImage.Text = "Burn Image";
			this.cmdBurnImage.Click += new System.EventHandler(this.cmdBurnImage_Click);
			// 
			// cmdExit
			// 
			this.cmdExit.Location = new System.Drawing.Point(584, 392);
			this.cmdExit.Name = "cmdExit";
			this.cmdExit.Size = new System.Drawing.Size(89, 31);
			this.cmdExit.TabIndex = 23;
			this.cmdExit.Text = "Exit";
			this.cmdExit.Click += new System.EventHandler(this.cmdExit_Click);
			// 
			// DataBurnerExForm
			// 
			//this.AutoScaleBaseSize = new System.Drawing.Size(6, 16);
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(682, 436);
			this.Controls.Add(this.cmdExit);
			this.Controls.Add(this.cmdBurnImage);
			this.Controls.Add(this.cmdCreateImage);
			this.Controls.Add(this.cmdBurn);
			this.Controls.Add(this.grpMultisession);
			this.Controls.Add(this.grpErase);
			this.Controls.Add(this.grpEjectDevice);
			this.Controls.Add(this.grpParameters);
			this.Controls.Add(this.grpJoliet);
			this.Controls.Add(this.grpISO);
			this.Controls.Add(this.cmbImageType);
			this.Controls.Add(this.lblImageType);
			this.Controls.Add(this.txtVolumeName);
			this.Controls.Add(this.txtRoot);
			this.Controls.Add(this.txtRequiredSpace);
			this.Controls.Add(this.txtFreeSpace);
			this.Controls.Add(this.lblVolumeName);
			this.Controls.Add(this.cmdBrowse);
			this.Controls.Add(this.lblFolderToBurn);
			this.Controls.Add(this.lblCDWriters);
			this.Controls.Add(this.cmbDevices);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DataBurnerExForm";
			this.Text = "DataBurnerEx.NET - Stream Based Burner";
			this.Load += new System.EventHandler(this.DataBurnerForm_Load);
			this.Validating += new System.ComponentModel.CancelEventHandler(this.DataBurnerExForm_Validating);
			this.grpISO.ResumeLayout(false);
			this.grpJoliet.ResumeLayout(false);
			this.grpParameters.ResumeLayout(false);
			this.grpEjectDevice.ResumeLayout(false);
			this.grpErase.ResumeLayout(false);
			this.grpMultisession.ResumeLayout(false);
			this.ResumeLayout(false);
			this.ResumeLayout();
		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
            try
            {
                // Initialize the SDK
                PrimoSoftware.Burner.Library.Initialize();

                // Set license string
                const string license = @"<primoSoftware></primoSoftware>";
                PrimoSoftware.Burner.Library.SetLicense(license);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.Run(new DataBurnerExForm());

                // Shutdown the SDK
                PrimoSoftware.Burner.Library.Shutdown();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
		}


		protected override void WndProc(ref Message msg)
		{
			if (msg.Msg==WM_SYSCOMMAND) 
			{
				if (msg.WParam.ToInt32() == IDC_MYCOMMAND) 
				{
					AboutForm frmAbout = new AboutForm();
					frmAbout.ShowDialog();
					return;
				} 
			}
			else if (WM_DEVICECHANGE==msg.Msg)
			{
				if (cmbDevices.Items.Count > 0)
				{
					SetDeviceControls();
				}
			}
			base.WndProc(ref msg);
		}


		private void cmdExit_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void DataBurnerForm_Load(object sender, System.EventArgs e)
		{
			m_Device = null;

			m_nRequiredSpace = 0;
			VolumeName = "DATADISC";
			// Session Mode
			cmbRecordingMode.SelectedIndex = 2;

			// Image Types
			cmbImageType.SelectedIndex = 1;

			// Write parameters
			chkTest.Checked = false;
			chkEject.Checked = false;
			chkCloseDisc.Checked = false;
			chkRaw.Checked = false;
			chkQuick.Checked = true;
			rdbISOAll.Checked = true;
			rdbJolietAll.Checked = true;

			m_ctx.bStopRequest = false;
			m_ctx.nMode = 0;
			m_ctx.operation = Operations.Invalid;
			m_ctx.nSpeedKB = 0;
			m_ctx.strImageFile = "";

			// Multisession
			chkLoadLastTrack.Checked = false;

			m_Engine = new Engine();
			if (m_Engine == null)
				return;

			bool bResult = m_Engine.Initialize();
			if (!bResult) 
			{
				MessageBox.Show("Unable to initialize PrimoBurner engine.");
				if (m_Enumerator!=null) 
				{
					m_Enumerator.Dispose();
					m_Enumerator=null;
				}

				return;
			}

			m_Enumerator = m_Engine.CreateDeviceEnumerator();
			
            int iDevices = m_Enumerator.Count;
			if (iDevices <= 0)
			{
				MessageBox.Show("No devices available.");
				
                if (m_Enumerator!=null)
				{
					m_Enumerator.Dispose();
					m_Enumerator = null;
				}

				return;
			}

			m_iDevicesCount = 0;
			for (int i=0; i < iDevices;i++) 
			{
				Device pDevice=m_Enumerator.CreateDevice(i);
				if (pDevice==null)
					continue;
				
				string sDesc = "";
				char sLetter;
					
				sDesc = pDevice.Description;
				sLetter = pDevice.DriveLetter;
				m_iDevicesCount++;

				string sName = string.Format("({0}:) - {1}", sLetter, sDesc);

				cmbDevices.Items.Add(new ComboItem(i,sName));

				pDevice.Dispose();
			}

			cmbDevices.ValueMember = "Num";
			cmbDevices.DisplayMember = "Data";

			if (m_iDevicesCount>0)
			{
				cmbDevices.SelectedIndex = 0;
				SetDeviceControls();
			}
			else 
			{
				MessageBox.Show("Could not find any CD-R devices.");
				if (null != m_Enumerator) 
				{
					m_Enumerator.Dispose();
					m_Enumerator = null;
				}

				return;
			}

			m_Streams = new ArrayList();
		}

		private void cmbDevices_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if ((cmbDevices.SelectedItem!=null)&&(cmbDevices.SelectedIndex!=-1))
				SetDeviceControls();
		}

		private void cmbRecordingMode_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (m_bRawDao && 0 == cmbRecordingMode.SelectedIndex)
			{
				chkRaw.Enabled = true;
				chkCloseDisc.Checked = true;
				chkCloseDisc.Enabled = false;
			}
				// SAO
			else if (1 == cmbRecordingMode.SelectedIndex)
			{
                chkCloseDisc.Enabled = true;

                chkRaw.Checked = false;
                chkRaw.Enabled = false;
            }
				// TAO
			else
			{
				chkCloseDisc.Enabled = true;

				chkRaw.Checked = false;
				chkRaw.Enabled = false;
			}

			chkRaw_CheckedChanged(chkRaw,null);
		}

		private void chkRaw_CheckedChanged(object sender, System.EventArgs e)
		{
			if (0 == cmbRecordingMode.SelectedIndex)
			{
				if (chkRaw.Checked)
					chkCloseDisc.Checked = true;

				chkCloseDisc.Enabled = !chkRaw.Checked;
			}
		}

		private void cmbImageType_SelectedIndexChanged(object sender, System.EventArgs e)
		{
            ImageType imgType = SelectedImageType;
			EnableIsoGroup((imgType & ImageType.Iso9660) == ImageType.Iso9660);
			EnableJolietGroup((imgType & ImageType.Joliet) == ImageType.Joliet);
		}

		private void cmdErase_Click(object sender, System.EventArgs e)
		{
			RunOperation(Operations.Erase);
		}

		private void cmdEjectOut_Click(object sender, System.EventArgs e)
		{
			int nDevice = ((ComboItem)cmbDevices.SelectedItem).Number;
			Device pDevice=m_Enumerator.CreateDevice(nDevice);
			if (pDevice!=null)
			{	
				pDevice.Eject(true);
				pDevice.Dispose();
			}
		}

		private void cmdEjectIn_Click(object sender, System.EventArgs e)
		{
			int nDevice = ((ComboItem)cmbDevices.SelectedItem).Number;
			Device pDevice=m_Enumerator.CreateDevice(nDevice);
			if (pDevice!=null)
			{	
				pDevice.Eject(false);
				pDevice.Dispose();
			}
		}

		private void cmdCreateImage_Click(object sender, System.EventArgs e)
		{
			if (!this.ValidateForm())
				return;

			SaveFileDialog oFileDialog = new SaveFileDialog();
			oFileDialog.InitialDirectory = RootDir ;
			oFileDialog.Filter = "Image File (*.iso)|*.iso";
			oFileDialog.FilterIndex = 2 ;
			oFileDialog.RestoreDirectory = true ;
			oFileDialog.AddExtension = true;

			if (DialogResult.OK!=oFileDialog.ShowDialog())
				return;
			m_ctx.strImageFile = oFileDialog.FileName.Trim();
			RunOperation(Operations.CreateImage);
		}

		private void DataBurnerExForm_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!DirectoryExists()) 
			{
				MessageBox.Show("Please specify a valid root directory");
				txtRoot.Focus();
				e.Cancel = true;
				return;
			}
			if (txtVolumeName.Text.Trim().Length>16) 
			{
				MessageBox.Show("Volume name can contain maximum 16 characters");
				txtVolumeName.Focus();
				e.Cancel = true;
				return;
			}
			if (RootDir[RootDir.Length-1]=='\\' || RootDir[RootDir.Length-1]=='/') 
			{
				RootDir = RootDir.Substring(0,RootDir.Length-1);
			}
		}
		
		private void cmdBurnImage_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog oFileDialog = new OpenFileDialog();
			oFileDialog.InitialDirectory = "c:\\" ;
			oFileDialog.Filter = "Image File (*.iso)|*.iso";
			oFileDialog.FilterIndex = 2 ;
			oFileDialog.RestoreDirectory = true ;

			if(DialogResult.OK!=oFileDialog.ShowDialog())
				return;

			m_ctx.strImageFile = oFileDialog.FileName.Trim();
			FileStream fileStream = null;
			try
			{
				fileStream = File.Open(m_ctx.strImageFile,
					FileMode.Open,FileAccess.Read,FileShare.Read);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return;
			}

			if (fileStream == null) 
			{
				string str = string.Format("Unable to open file {0}", m_ctx.strImageFile);
				MessageBox.Show(str);
				return;
			}

			long lFileSize = fileStream.Length;
			fileStream.Close();

			if (lFileSize > m_nCapacity) 
			{
				string str = string.Format("Cannot write image file {0}.\nThe file is too big.", m_ctx.strImageFile);
				MessageBox.Show(str);
				return;
			}

			RunOperation(Operations.BurnImage);
		}

		private void cmdBurn_Click(object sender, System.EventArgs e)
		{
			if (!ValidateForm())
				return;
			RunOperation(Operations.BurnOnTheFly);
		}

		private void cmdBrowse_Click(object sender, System.EventArgs e)
		{
			System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
			folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				RootDir = folderBrowserDialog.SelectedPath;
				Cursor.Current = Cursors.WaitCursor;

				DataDisc dataDisc = new DataDisc();
				dataDisc.ImageType = SelectedImageType;
				
				bool bRes = SetImageLayoutFromFolder(dataDisc,RootDir);
				if (!bRes) 
				{
					string str = string.Format("A problem occured when trying to set directory:\n{0}", RootDir);
					MessageBox.Show(str);

					RootDir = "";
				} 
				else 
				{
					m_nRequiredSpace = dataDisc.ImageSizeInBytes;
				}

				Cursor.Current = Cursors.Arrow;
				dataDisc.Dispose();
				SetDeviceControls();
			}
		}


		// Methods
		private string GetTextStatus(DataDiscStatus eStatus)
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
					return "Initializing and writing lead-in...";

				case DataDiscStatus.Writing:
					return "Writing...";

				case DataDiscStatus.WritingLeadOut:
					return "Writing lead-out and flushing cache...";
			}
			return "Unknown status...";
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
			int nDevice = ((ComboItem)cmbDevices.SelectedItem).Number;
			if (-1 == nDevice)
				return;

			Device pDevice = m_Enumerator.CreateDevice(nDevice);
			if (pDevice == null) 
			{
				MessageBox.Show("Function CDataBurnerDlg::SetDeviceControls : Unable to get device object\nPossible reason : auto-insert notification enabled\nwhile writing (or any other) operation is in progress.\nThis is just a warning message - your CD is not damaged.");
				return; // Protection from AI notification...
			}

			m_nCapacity = pDevice.MediaFreeSpace;
			m_nCapacity *= Convert.ToInt64(PrimoSoftware.Burner.BlockSize.CdRom);

			if (0 == m_nCapacity || m_nCapacity < m_nRequiredSpace) 
			{
				cmdBurn.Enabled = false;
			} 
			else 
			{
				cmdBurn.Enabled = true;
			}

			GetSupportedWriteSpeeds(pDevice);
			
			bool bRW, bRWDisk;
			bRW		= ReWritePossible(pDevice);
			bRWDisk = pDevice.MediaIsReWritable;
	
			if (bRW && bRWDisk) 
			{
				chkQuick.Enabled = true;
				cmdErase.Enabled = true;
			} 
			else 
			{
				chkQuick.Enabled = false;
				cmdErase.Enabled = false;
			}

			m_bRawDao = pDevice.CDFeatures.CanWriteRawDao;

			cmbRecordingMode_SelectedIndexChanged(cmbRecordingMode,null);

			string str;
			str = string.Format("Required space : {0}M", m_nRequiredSpace/(1024*1024));
			txtRequiredSpace.Text = str;

			str = string.Format("Free space : {0}M", m_nCapacity/(1024*1024));
			txtFreeSpace.Text = str;
			
			pDevice.Dispose();
		}

        private bool ReWritePossible(Device device)
        {
            CDFeatures cdfeatures = device.CDFeatures;
            DVDFeatures dvdfeatures = device.DVDFeatures;
            BDFeatures bdfeatures = device.BDFeatures;

            bool cdRewritePossible = cdfeatures.CanWriteCDRW;
            bool dvdRewritePossible = dvdfeatures.CanReadDVDMinusRW || dvdfeatures.CanReadDVDPlusRW || dvdfeatures.CanWriteDVDRam;
            bool bdRewritePossible = bdfeatures.CanReadBDRE;

            return cdRewritePossible || dvdRewritePossible || bdRewritePossible;
        }

		private void EnableJolietGroup(bool bEnable)
		{
			grpJoliet.Enabled = bEnable;
			rdbJolietAll.Enabled = bEnable;
			rdbJolietShortNames.Enabled = bEnable;
		}

		private void EnableIsoGroup(bool bEnable)
		{
			grpISO.Enabled = bEnable;
			
			rdbISOAll.Enabled = bEnable;
			rdbISOLevel2.Enabled = bEnable;
			rdbISOLevel1.Enabled = bEnable;

			chkTreeDepth.Enabled = bEnable;
			chkTranslateName.Enabled = bEnable;
		}

		private bool DirectoryExists()
		{
			return Directory.Exists(RootDir);
		}

		private int GetConstraints(OperationContext ctx)
		{
			int dwRes = 0;
			switch(ctx.iso.nLimits)
			{
				case 0:
					dwRes |= (int)ImageConstraints.IsoLongLevel2;
					break;
				case 1:
					dwRes |= (int)ImageConstraints.IsoLevel2;
					break;
				case 2:
					dwRes |= (int)ImageConstraints.IsoLevel1;
					break;
			}

			if (!ctx.iso.bTreeDepth)
				dwRes = dwRes & ~(int)ImageConstraints.IsoTreeDepth;
			return dwRes;
		}

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

		string GetShortLogicalName(string sFullName)
		{
			StringBuilder sbObj = new StringBuilder(255);
			Kernel32.GetShortPathName(sFullName, sbObj, sbObj.Capacity);
			return sbObj.ToString().Substring(sbObj.ToString().LastIndexOf("\\") + 1);
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
					
					string strShortname = GetShortLogicalName(di.FullName);
					pDataFile.ShortFilename = strShortname == di.Name ? "" : strShortname; 
					
					bool bHide = 0 != (di.Attributes & FileAttributes.Hidden);
                    if (bHide)
					    pDataFile.HiddenMask = (int)m_ctx.imageType;
						
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

					// Use IDataFileStream instead of a physical file name 
					pDataFile.DataSource = DataSourceType.Stream;

					// create a data stream instance
					Stream pStream = new FileStream(sEntry, FileMode.Open);
					m_Streams.Add(pStream);

					// Our implementation of a IDataFileStream is for illustration purposes only and simply reads the data 
					// from the real file;
					pDataFile.Stream = pStream;
					
					pDataFile.LongFilename = fi.Name;

					string strShortname = GetShortLogicalName(fi.FullName);
					pDataFile.ShortFilename = strShortname == fi.Name ? "" : strShortname;

					bool bHide = 0 != (fi.Attributes & FileAttributes.Hidden);
                    if (bHide)
                        pDataFile.HiddenMask = (int)m_ctx.imageType;

					// Add to the tree
					pCurrentFile.Children.Add(pDataFile);
				}

			}
		}

		private bool SetImageLayoutFromFolder(DataDisc dataDisc, string fname)
		{
			DataFile pDataFile = new DataFile(); 

			// Entry for the root of the image file system
			pDataFile.IsDirectory = true;
			pDataFile.FilePath = "\\";			
			pDataFile.LongFilename = "\\";

			DestroyStreams();
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

		private bool Burn(Device pDevice, int nPrevTrackNumber) 
		{
			bool bRes = false;
			if (pDevice == null)
				return false;

			// this one we will use to burn
			DataDisc dataDisc = new DataDisc();
			dataDisc.Device = pDevice;

			// We need to tell DataDisc which address we start from so it adjusts the session start address
			// This must be done before calling any of the LoadFromDisc, Merge, SetImageLayoutFromFolder methods
			dataDisc.SessionStartAddress = (pDevice.NewSessionStartAddress);

			// Set burning parameters
			dataDisc.ImageType = m_ctx.imageType;
			dataDisc.ImageConstraints = GetConstraints(m_ctx);
			dataDisc.TranslateFilenames = (m_ctx.iso.bTraslateNames);
			
            dataDisc.UdfVolumeProps.VolumeLabel = VolumeName;
            dataDisc.IsoVolumeProps.VolumeLabel = VolumeName;
            dataDisc.JolietVolumeProps.VolumeLabel = VolumeName;

			dataDisc.SimulateBurn = m_ctx.bSimulate;

			// Set write mode
			if (!m_ctx.bRaw && (0 == m_ctx.nMode || 1 == m_ctx.nMode))
				// Session-At-Once (also called Disc-At-Once)
				dataDisc.WriteMethod = WriteMethod.Sao;
			else if (m_ctx.bRaw)
				// RAW Disc-At-Once
				dataDisc.WriteMethod = WriteMethod.RawDao;
			else if (2 == m_ctx.nMode)
				// Track-At-Once
				dataDisc.WriteMethod = WriteMethod.Tao;
			else
				// Packet
				dataDisc.WriteMethod = WriteMethod.Packet;

			// CloseDisc controls multi-session. Disk must be left open when multi-sessions are desired. 
			dataDisc.CloseDisc = m_ctx.bCloseDisc;

			// Set event handlers
			dataDisc.OnContinueBurn += DataDisc_OnContinueBurn;
			dataDisc.OnFileStatus += DataDisc_OnFileStatus;
			dataDisc.OnProgress += DataDisc_OnProgress;
			dataDisc.OnStatus += DataDisc_OnStatus;


			// Create a second instance to load the layout of the existing track 
			DataDisc pNew = new DataDisc();

			// As of version 2.0 we must set the ImageType before calling DataDisc.SetImageLayout
			pNew.ImageType = m_ctx.imageType;

			// We need to tell this one about the session start address too.
			pNew.SessionStartAddress = pDevice.NewSessionStartAddress;

			if (nPrevTrackNumber > 0)
			{
				// LoadFromDisc loads only the path tables. So it is quick.
				if (!dataDisc.LoadFromDisc(nPrevTrackNumber))
				{
					string strMessage = string.Format("Could not load track {0} from the last session. Do you want to continue?", nPrevTrackNumber);
					if (DialogResult.No == MessageBox.Show(strMessage,"", MessageBoxButtons.YesNo))
					{
						pNew.Dispose();
						dataDisc.Dispose();
						return false;
					}
				}
			}

			if (nPrevTrackNumber > 0)
			{
				// Load the new folder into pNew 
				bRes = SetImageLayoutFromFolder(pNew, RootDir);
				if (!bRes) 
				{
					ShowErrorMessage(pNew,pDevice);

					pNew.Dispose();
					dataDisc.Dispose();
					return false;
				}

				// Merge the new layout with the old one
				if (!dataDisc.Merge(pNew))
				{
					string strMessage = string.Format("Could not merge with track {0} from the last session. Do you want to continue?", nPrevTrackNumber);
					if (DialogResult.No == MessageBox.Show(strMessage,"",MessageBoxButtons.YesNo))
					{
						pNew.Dispose();
						dataDisc.Dispose();
						return false;
					}
				}
			}
			else
			{
				// Load the layout directly into pDataCD 
				bRes = SetImageLayoutFromFolder(dataDisc, RootDir);
				if (!bRes) 
				{
					ShowErrorMessage(dataDisc,pDevice);

					pNew.Dispose();
					dataDisc.Dispose();
					return false;
				}
			}

			// Set speed
			pDevice.WriteSpeedKB = m_ctx.nSpeedKB;

			// Burn 
			bRes = dataDisc.WriteToDisc(true);
			if (!bRes) 
			{
				ShowErrorMessage(dataDisc,pDevice);

				pNew.Dispose();
				dataDisc.Dispose();
				return false;
			}

			if (m_ctx.bEject)
				pDevice.Eject(true);

			pNew.Dispose();
			dataDisc.Dispose();
			return true;
		}

		private bool ImageBurn(Device pDevice, int nPrevTrackNumber) 
		{
			DataDisc dataDisc = new DataDisc();

			dataDisc.OnContinueBurn += DataDisc_OnContinueBurn;
			dataDisc.OnFileStatus += DataDisc_OnFileStatus;
			dataDisc.OnProgress += DataDisc_OnProgress;
			dataDisc.OnStatus += DataDisc_OnStatus;

			dataDisc.Device = pDevice;
			dataDisc.SessionStartAddress = pDevice.NewSessionStartAddress;

			dataDisc.SimulateBurn = m_ctx.bSimulate;
			
			// Set write mode
			if (!m_ctx.bRaw && (0 == m_ctx.nMode || 1 == m_ctx.nMode))
				// Session-At-Once (also called Disc-At-Once)
				dataDisc.WriteMethod = WriteMethod.Sao;
			else if (m_ctx.bRaw)
				// RAW Disc-At-Once
				dataDisc.WriteMethod = WriteMethod.RawDao;
			else if (2 == m_ctx.nMode)
				// Track-At-Once
				dataDisc.WriteMethod = WriteMethod.Tao;
			else
				// Packet
				dataDisc.WriteMethod = WriteMethod.Packet;

			dataDisc.CloseDisc = m_ctx.bCloseDisc;

			// Set write speed
			pDevice.WriteSpeedKB = m_ctx.nSpeedKB;

			bool bRes = dataDisc.WriteImageToDisc(m_ctx.strImageFile, true);
			if (!bRes) 
			{
				ShowErrorMessage(dataDisc,pDevice);

				dataDisc.Dispose();
				return false;
			}

			dataDisc.Dispose();
			return true;
		}

		private bool ImageCreate(Device pDevice, int nPrevTrackNumber) 
		{
			DataDisc pDataCD = new DataDisc();

			pDataCD.UdfVolumeProps.VolumeLabel = VolumeName;
            pDataCD.IsoVolumeProps.VolumeLabel = VolumeName;
            pDataCD.JolietVolumeProps.VolumeLabel = VolumeName;

			pDataCD.ImageType = m_ctx.imageType;
			pDataCD.ImageConstraints = GetConstraints(m_ctx);
			pDataCD.TranslateFilenames = (m_ctx.iso.bTraslateNames);

			pDataCD.SessionStartAddress = 0;

			pDataCD.OnContinueBurn += DataDisc_OnContinueBurn;
            pDataCD.OnFileStatus += DataDisc_OnFileStatus;
            pDataCD.OnProgress += DataDisc_OnProgress;
            pDataCD.OnStatus += DataDisc_OnStatus;

			bool bRes = SetImageLayoutFromFolder(pDataCD,RootDir);
			if (!bRes) 
			{
				ShowErrorMessage(pDataCD,pDevice);

				pDataCD.Dispose();
				return false;
			}

			bRes = pDataCD.CreateImageFile(m_ctx.strImageFile);
			if (!bRes) 
			{
				ShowErrorMessage(pDataCD,pDevice);

				pDataCD.Dispose();
				return false;
			}

			if (m_ctx.bEject)
				pDevice.Eject(true);

			pDataCD.Dispose();
			return true;
		}

		private void DestroyStreams() 
		{
			m_Streams.Clear();
		}

		private void RunOperation(Operations operation)
		{
			this.Enabled = false;
			dlgProgress = new Progress();
			dlgProgress.Owner = this;
			dlgProgress.burningDone = new BurningDoneHandler(BurningDone);
			dlgProgress.Show();
			
			m_ctx.bCloseDisc = chkCloseDisc.Checked;
			m_ctx.bEject = chkEject.Checked;
			m_ctx.bErasing = operation==Operations.Erase?true:false;
			m_ctx.bLoadLastTrack = chkLoadLastTrack.Checked;
			m_ctx.bQuick = chkQuick.Checked;
			m_ctx.bRaw = chkRaw.Checked;
			m_ctx.bSimulate = chkTest.Checked;
			m_ctx.bStopRequest = false;
			m_ctx.imageType = SelectedImageType;
			m_ctx.iso.nLimits = rdbISOAll.Checked?0:(rdbISOLevel2.Checked?1:2);
			m_ctx.iso.bTreeDepth = chkTreeDepth.Checked;
			m_ctx.iso.bTraslateNames = chkTranslateName.Checked;
			m_ctx.joliet.nLimits = rdbJolietAll.Checked?0:1;
			m_ctx.nMode = cmbRecordingMode.SelectedIndex;
			m_ctx.nSpeedKB = ((SpeedListItem)cmbSpeed.SelectedItem).SpeedKB;
			m_ctx.operation = operation;

			if (m_ctx.bErasing)
			{
				dlgProgress.labelStatus.Text = "Erasing disc. Please wait...";
			}
			ProcessDelegate  process = new ProcessDelegate(Process);
			process.BeginInvoke(m_ctx, null, null);
		}

		delegate void ProcessDelegate(OperationContext ctx);
		public void BurningDone()
		{
			if(null != dlgProgress)
			{
				dlgProgress.Close();
				dlgProgress = null;
			}
			this.Invalidate();
			this.BringToFront();
			if(null != m_Device)
			{
				m_Device.Dispose();
				m_Device = null;
			}
			this.Enabled = true;
			if (m_ctx.bEject)
			{
				int nDevice = ((ComboItem)cmbDevices.SelectedItem).Number;
				Device pDevice=m_Enumerator.CreateDevice(nDevice);
				if (pDevice!=null)
				{	
					pDevice.Eject(true);
					pDevice.Dispose();
				}
			}
			SetDeviceControls();
		}
		private void Process(OperationContext ctx)
		{
			progressArgs = new ShowProgressArgs();

			try
			{
				int nDevice = ((ComboItem)cmbDevices.SelectedItem).Number;
				// Get a device 
				if (-1 == nDevice && Operations.CreateImage == ctx.operation)
				{	
					ImageCreate(null, 0);
					return;
				}

				// No devices and not an OPERATION_IMAGE_CREATE - nothing to do here
				if (-1 == nDevice)
					return;

				m_Device = m_Enumerator.CreateDevice(nDevice);

				if (ctx.bErasing) 
				{
					m_Device.WriteSpeedKB = ctx.nSpeedKB;
					m_Device.Erase(ctx.bQuick);

					ctx.bErasing = false;
					return;
				}

				// Get the last track number from the last session if multisession option was specified
				int nPrevTrackNumber = 0;
				if (m_ctx.bLoadLastTrack)
				{
					// ReadSessionInfo method will not detect last session if it is open, so we need to use ReadDiskInfo 
                    DiscInfo di = m_Device.ReadDiscInfo();
                    if (null != di)
                    {
                        if (DiscStatus.Open == di.DiscStatus)
                        {
                            // ReadDiskInfo will report the empty space as a track too
                            // that's why we need to go back one track to get the last completed track
                            nPrevTrackNumber = di.LastTrack - 1;
                        }
                    }
				}
				
				switch(ctx.operation)
				{
					case Operations.Invalid:
						break;

					case Operations.CreateImage:
						ImageCreate(m_Device, 0);
						break;

					case Operations.BurnImage:
						ImageBurn(m_Device, 0);
						break;

					case Operations.BurnOnTheFly:
						Burn(m_Device, nPrevTrackNumber);
						break;
				}
			}
			finally
			{
				progressArgs.bDone = true;
				BurningDone();
				dlgProgress.ShowProgress(System.Threading.Thread.CurrentThread, progressArgs);
			}
		}

		private bool ValidateForm()
		{
			if (!Directory.Exists(RootDir))
			{
				MessageBox.Show("Please specify a valid root directory");
				txtRoot.Focus();
				return false;
			}

			if(VolumeName.Length > 16)
			{
				MessageBox.Show("Volume name can contain maximum 16 characters");
				txtVolumeName.Focus();
				return false;
			}

			if((RootDir[RootDir.Length -1] == '\\') || (RootDir[RootDir.Length -1] == '/'))
			{
				RootDir = RootDir.Substring(0, RootDir.Length-1);
				txtRoot.Text = RootDir;
			}
		
			return true;
		}

		private void DataDisc_OnContinueBurn(object Sender, DataDiscContinueEventArgs eArgs)
		{
			dlgProgress.ShowProgress(System.Threading.Thread.CurrentThread, progressArgs);
			eArgs.Continue = !progressArgs.bStopRequest;
		}

		private void DataDisc_OnFileStatus(object Sender, DataDiscFileStatusEventArgs eArgs)
		{

		}

		private void DataDisc_OnProgress(object Sender, DataDiscProgressEventArgs eArgs)
		{
			progressArgs.progressPos = (int)((double)eArgs.Position * 100.0 / (double)eArgs.All);

			if(null != m_Device)
				progressArgs.bufferPos = (int)((100 * (double)(m_Device.InternalCacheUsedSpace)) / ((double)m_Device.InternalCacheCapacity));
			else
				progressArgs.bufferPos = 0;

			dlgProgress.ShowProgress(System.Threading.Thread.CurrentThread, progressArgs);
		}

		private void DataDisc_OnStatus(object Sender, DataDiscStatusEventArgs eArgs)
		{
			progressArgs.status = GetTextStatus(eArgs.Status);
			dlgProgress.ShowProgress(System.Threading.Thread.CurrentThread, progressArgs);
		}
	};

	public class Kernel32
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern int GetShortPathName(
			[MarshalAs(UnmanagedType.LPTStr)]
			string path,
			[MarshalAs(UnmanagedType.LPTStr)]
			StringBuilder shortPath,
			int shortPathLength);

		[DllImport("user32.dll")] 
		public static extern IntPtr GetSystemMenu(IntPtr hwnd, int bRevert);
		[DllImport("user32.dll")] 
		public static extern bool AppendMenu(IntPtr hMenu,MenuFlags uFlags, uint uIDNewItem, string lpNewItem);

	}

	
	internal enum Operations
	{
		Invalid      = -1,
		CreateImage  = 1,
		BurnImage    = 2,
		BurnOnTheFly = 3,
		Erase        = 4,
		Format       = 5
	};

	internal struct OperationContext
	{
		public Operations	operation;
		public bool		bQuick;       // indicates quick format or erase
		public bool		bSimulate;
		public bool		bEject;
		public int		nMode;
		public bool		bCloseDisc;
		public int		nSpeedKB;
		public ImageType imageType;
		public string   strImageFile;
		public bool     bLoadLastTrack;
		public bool		bErasing;
		public bool		bRaw;
		public bool		bStopRequest;
		public isostr	iso;
		public jolietstr joliet;
		
	};

	internal struct isostr
	{
		public int  nLimits;
		public bool bTreeDepth;
		public bool bTraslateNames;
	};

	internal struct jolietstr
	{
		public int nLimits;
	};

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
	};
	
	public enum MenuFlags 
	{
		MF_BYCOMMAND = 0x00000000,
		MF_SEPARATOR = 0x00000800
	};

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
