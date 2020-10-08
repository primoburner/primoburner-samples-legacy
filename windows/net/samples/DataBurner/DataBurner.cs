using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Diagnostics;
using PrimoSoftware.Burner;
using System.Collections.Generic;

namespace DataBurner.NET
{
	/// <summary>
	/// Summary description for DataBurner.
	/// </summary>
	public class DataBurner : System.Windows.Forms.Form
	{
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
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textBoxSmallFilesTreshold;
		private System.Windows.Forms.TextBox textBoxCacheLimit;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.CheckBox checkBoxRawMode;
		private System.Windows.Forms.ComboBox comboBoxRecordingMode;
		private System.Windows.Forms.ComboBox comboBoxRecordingSpeed;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkBoxCloseDisc;
		private System.Windows.Forms.CheckBox checkBoxSimulate;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.CheckBox checkBoxQuick;
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
		private System.Windows.Forms.CheckBox checkBoxRestrictTreeDepth;
		private System.Windows.Forms.CheckBox checkBoxTranslateFilenames;
		private System.Windows.Forms.CheckBox checkBoxCacheSmallFiles;
		private System.Windows.Forms.CheckBox checkBoxLoadLastTrack;
		private System.Windows.Forms.GroupBox groupBoxISO;
		private System.Windows.Forms.GroupBox groupBoxJoliet;
		private System.Windows.Forms.RadioButton radioButtonISOLevel2_212;
		private System.Windows.Forms.RadioButton radioButtonISOLevel1;
		private System.Windows.Forms.RadioButton radioButtonISOLevel2_31;
		private System.Windows.Forms.RadioButton radioButtonJolietStandardNames;
		private System.Windows.Forms.RadioButton radioButtonJolietLongNames;
		private System.Windows.Forms.CheckBox checkBoxCacheCDROMFiles;
		private System.Windows.Forms.CheckBox checkBoxCacheNetworkFiles;
		private System.Windows.Forms.CheckBox checkBoxEjectWhenDone;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		
		const int WM_DEVICECHANGE = 0x0219;

		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;

		private System.Windows.Forms.GroupBox groupBoxCahceSmallFiles;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private DeviceEnumerator _deviceEnum;
		private Engine _engine;
		private ArrayList _deviceNames;
		private ArrayList _deviceIndexes;
		private int _deviceIndex;
		private bool _rawDao;
		private long _capacity;
		private long _requiredSpace;
		private string  _imageFile;
		private BurningForm _progressForm;
		private ShowProgressArgs _progressArgs;
		private Device _device;

		const int SMALL_FILES_CACHE_LIMIT = 20000;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TextBox txtBootImage;
		private System.Windows.Forms.Button btnBrowseBootImage;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.CheckBox chkBootable;
		private System.Windows.Forms.ComboBox cmbEmulation;
		private System.Windows.Forms.CheckBox chkCdXa;
		private System.Windows.Forms.GroupBox grpBoot;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.TextBox textBoxBootLoadSegment;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.CheckBox checkBoxAddVersionToIsoNames;
		private System.Windows.Forms.TextBox textBoxBootSectorCount;
		private System.Windows.Forms.CheckBox checkBoxSortByFilename;
		const int SMALL_FILE_SECTORS = 10;

		public DataBurner()
		{
			InitializeComponent();

			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			
			_deviceNames = new ArrayList();
			_deviceIndexes = new ArrayList();
			_progressArgs = new ShowProgressArgs();
			_capacity = 0;
			_requiredSpace = 0;

			comboBoxRecordingMode.Items.Add("Disk-At-Once");
			comboBoxRecordingMode.Items.Add("Session-At-Once");
			comboBoxRecordingMode.Items.Add("Track-At-Once");
			comboBoxRecordingMode.Items.Add("Packet");
			comboBoxRecordingMode.SelectedIndex = 2;

			comboBoxImageType.Items.Add("ISO9660");
			comboBoxImageType.Items.Add("Joliet");
			comboBoxImageType.Items.Add("UDF");
			comboBoxImageType.Items.Add("UDF & ISO9660");
			comboBoxImageType.Items.Add("UDF & Joliet");
			comboBoxImageType.SelectedIndex = 1;

			textBoxSmallFilesTreshold.Text = String.Format("{0}", SMALL_FILE_SECTORS);
			textBoxCacheLimit.Text = String.Format("{0}", SMALL_FILES_CACHE_LIMIT);

			InitDevices();
		}

		private void InitDevices()
		{
            Library.EnableTraceLog(null, true);

            _engine = new Engine();
			if(!_engine.Initialize())
				throw new Exception("Unable to initialize CD writing _engine.");

			_deviceEnum = _engine.CreateDeviceEnumerator();

			if(0 == _deviceEnum.Count) 
			{
				throw new Exception("No devices available.");
			}
			else
			{
				for(int i = 0; i < _deviceEnum.Count; i++)
				{
					Device device = _deviceEnum.CreateDevice(i);

                    string str = String.Format("({0}:) {1}", device.DriveLetter, device.Description);

                    _deviceNames.Add(str);
					_deviceIndexes.Add(i);
					
                    comboBoxDevices.Items.Add(str);

					device.Dispose();
				}

				if(_deviceIndexes.Count > 0)
				{
					comboBoxDevices.SelectedIndex = 0;
					SetDeviceControls(0);
				}
				else
				{
					throw new Exception("Unable ot open any CD writer device.");
				}
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}

				if(_deviceEnum != null)
				{
					_deviceEnum.Dispose();
					_deviceEnum = null;
				}

				if(_engine != null)
				{
					_engine.Shutdown();
					_engine.Dispose();
					_engine = null;

                    Library.DisableTraceLog();
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
			this.groupBoxISO = new System.Windows.Forms.GroupBox();
			this.checkBoxTranslateFilenames = new System.Windows.Forms.CheckBox();
			this.checkBoxRestrictTreeDepth = new System.Windows.Forms.CheckBox();
			this.radioButtonISOLevel1 = new System.Windows.Forms.RadioButton();
			this.radioButtonISOLevel2_31 = new System.Windows.Forms.RadioButton();
			this.radioButtonISOLevel2_212 = new System.Windows.Forms.RadioButton();
			this.groupBoxJoliet = new System.Windows.Forms.GroupBox();
			this.radioButtonJolietStandardNames = new System.Windows.Forms.RadioButton();
			this.radioButtonJolietLongNames = new System.Windows.Forms.RadioButton();
			this.groupBoxCahceSmallFiles = new System.Windows.Forms.GroupBox();
			this.textBoxCacheLimit = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.textBoxSmallFilesTreshold = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.checkBoxCacheSmallFiles = new System.Windows.Forms.CheckBox();
			this.checkBoxCacheCDROMFiles = new System.Windows.Forms.CheckBox();
			this.checkBoxCacheNetworkFiles = new System.Windows.Forms.CheckBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.chkCdXa = new System.Windows.Forms.CheckBox();
			this.checkBoxRawMode = new System.Windows.Forms.CheckBox();
			this.comboBoxRecordingMode = new System.Windows.Forms.ComboBox();
			this.comboBoxRecordingSpeed = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.checkBoxCloseDisc = new System.Windows.Forms.CheckBox();
			this.checkBoxEjectWhenDone = new System.Windows.Forms.CheckBox();
			this.checkBoxSimulate = new System.Windows.Forms.CheckBox();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.checkBoxQuick = new System.Windows.Forms.CheckBox();
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
			this.grpBoot = new System.Windows.Forms.GroupBox();
			this.textBoxBootSectorCount = new System.Windows.Forms.TextBox();
			this.label12 = new System.Windows.Forms.Label();
			this.textBoxBootLoadSegment = new System.Windows.Forms.TextBox();
			this.label11 = new System.Windows.Forms.Label();
			this.cmbEmulation = new System.Windows.Forms.ComboBox();
			this.label10 = new System.Windows.Forms.Label();
			this.btnBrowseBootImage = new System.Windows.Forms.Button();
			this.txtBootImage = new System.Windows.Forms.TextBox();
			this.label9 = new System.Windows.Forms.Label();
			this.chkBootable = new System.Windows.Forms.CheckBox();
			this.checkBoxAddVersionToIsoNames = new System.Windows.Forms.CheckBox();
			this.checkBoxSortByFilename = new System.Windows.Forms.CheckBox();
			this.groupBoxISO.SuspendLayout();
			this.groupBoxJoliet.SuspendLayout();
			this.groupBoxCahceSmallFiles.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.groupBox6.SuspendLayout();
			this.groupBox7.SuspendLayout();
			this.grpBoot.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(72, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "CD Writers :";
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
			this.labelFreeSpace.Size = new System.Drawing.Size(136, 24);
			this.labelFreeSpace.TabIndex = 2;
			this.labelFreeSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// labelRequiredSpace
			// 
			this.labelRequiredSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelRequiredSpace.Location = new System.Drawing.Point(448, 24);
			this.labelRequiredSpace.Name = "labelRequiredSpace";
			this.labelRequiredSpace.Size = new System.Drawing.Size(136, 24);
			this.labelRequiredSpace.TabIndex = 3;
			this.labelRequiredSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 56);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(120, 16);
			this.label4.TabIndex = 4;
			this.label4.Text = "Select a folder to burn:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// textBoxRootDir
			// 
			this.textBoxRootDir.Location = new System.Drawing.Point(128, 56);
			this.textBoxRootDir.Name = "textBoxRootDir";
			this.textBoxRootDir.ReadOnly = true;
			this.textBoxRootDir.Size = new System.Drawing.Size(368, 20);
			this.textBoxRootDir.TabIndex = 5;
			this.textBoxRootDir.Text = "";
			// 
			// buttonBrowse
			// 
			this.buttonBrowse.Location = new System.Drawing.Point(512, 56);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(72, 24);
			this.buttonBrowse.TabIndex = 6;
			this.buttonBrowse.Text = "Browse";
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(8, 88);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(80, 16);
			this.label5.TabIndex = 7;
			this.label5.Text = "Volume name :";
			// 
			// textBoxVolumeName
			// 
			this.textBoxVolumeName.Location = new System.Drawing.Point(96, 88);
			this.textBoxVolumeName.Name = "textBoxVolumeName";
			this.textBoxVolumeName.Size = new System.Drawing.Size(136, 20);
			this.textBoxVolumeName.TabIndex = 8;
			this.textBoxVolumeName.Text = "DATADISC";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(256, 88);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(72, 16);
			this.label6.TabIndex = 9;
			this.label6.Text = "Image Type:";
			// 
			// comboBoxImageType
			// 
			this.comboBoxImageType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxImageType.Location = new System.Drawing.Point(336, 88);
			this.comboBoxImageType.Name = "comboBoxImageType";
			this.comboBoxImageType.Size = new System.Drawing.Size(160, 21);
			this.comboBoxImageType.TabIndex = 10;
			this.comboBoxImageType.SelectedIndexChanged += new System.EventHandler(this.comboBoxImageType_SelectedIndexChanged);
			// 
			// groupBoxISO
			// 
			this.groupBoxISO.Controls.Add(this.checkBoxTranslateFilenames);
			this.groupBoxISO.Controls.Add(this.checkBoxRestrictTreeDepth);
			this.groupBoxISO.Controls.Add(this.radioButtonISOLevel1);
			this.groupBoxISO.Controls.Add(this.radioButtonISOLevel2_31);
			this.groupBoxISO.Controls.Add(this.radioButtonISOLevel2_212);
			this.groupBoxISO.Location = new System.Drawing.Point(8, 120);
			this.groupBoxISO.Name = "groupBoxISO";
			this.groupBoxISO.Size = new System.Drawing.Size(368, 80);
			this.groupBoxISO.TabIndex = 11;
			this.groupBoxISO.TabStop = false;
			this.groupBoxISO.Text = "ISO9660";
			// 
			// checkBoxTranslateFilenames
			// 
			this.checkBoxTranslateFilenames.Location = new System.Drawing.Point(208, 48);
			this.checkBoxTranslateFilenames.Name = "checkBoxTranslateFilenames";
			this.checkBoxTranslateFilenames.Size = new System.Drawing.Size(128, 24);
			this.checkBoxTranslateFilenames.TabIndex = 4;
			this.checkBoxTranslateFilenames.Text = "Translate Filenames";
			// 
			// checkBoxRestrictTreeDepth
			// 
			this.checkBoxRestrictTreeDepth.Location = new System.Drawing.Point(8, 48);
			this.checkBoxRestrictTreeDepth.Name = "checkBoxRestrictTreeDepth";
			this.checkBoxRestrictTreeDepth.Size = new System.Drawing.Size(184, 24);
			this.checkBoxRestrictTreeDepth.TabIndex = 3;
			this.checkBoxRestrictTreeDepth.Text = "Restrict Tree Depth to 8 levels";
			// 
			// radioButtonISOLevel1
			// 
			this.radioButtonISOLevel1.Location = new System.Drawing.Point(256, 16);
			this.radioButtonISOLevel1.Name = "radioButtonISOLevel1";
			this.radioButtonISOLevel1.TabIndex = 2;
			this.radioButtonISOLevel1.Text = "Level1(8.3)";
			// 
			// radioButtonISOLevel2_31
			// 
			this.radioButtonISOLevel2_31.Location = new System.Drawing.Point(152, 16);
			this.radioButtonISOLevel2_31.Name = "radioButtonISOLevel2_31";
			this.radioButtonISOLevel2_31.Size = new System.Drawing.Size(88, 24);
			this.radioButtonISOLevel2_31.TabIndex = 1;
			this.radioButtonISOLevel2_31.Text = "Level2 (31)";
			// 
			// radioButtonISOLevel2_212
			// 
			this.radioButtonISOLevel2_212.Checked = true;
			this.radioButtonISOLevel2_212.Location = new System.Drawing.Point(8, 16);
			this.radioButtonISOLevel2_212.Name = "radioButtonISOLevel2_212";
			this.radioButtonISOLevel2_212.Size = new System.Drawing.Size(120, 24);
			this.radioButtonISOLevel2_212.TabIndex = 0;
			this.radioButtonISOLevel2_212.TabStop = true;
			this.radioButtonISOLevel2_212.Text = "Level 2 Long (212)";
			// 
			// groupBoxJoliet
			// 
			this.groupBoxJoliet.Controls.Add(this.radioButtonJolietStandardNames);
			this.groupBoxJoliet.Controls.Add(this.radioButtonJolietLongNames);
			this.groupBoxJoliet.Location = new System.Drawing.Point(392, 120);
			this.groupBoxJoliet.Name = "groupBoxJoliet";
			this.groupBoxJoliet.Size = new System.Drawing.Size(192, 80);
			this.groupBoxJoliet.TabIndex = 12;
			this.groupBoxJoliet.TabStop = false;
			this.groupBoxJoliet.Text = "Joliet";
			// 
			// radioButtonJolietStandardNames
			// 
			this.radioButtonJolietStandardNames.Location = new System.Drawing.Point(16, 48);
			this.radioButtonJolietStandardNames.Name = "radioButtonJolietStandardNames";
			this.radioButtonJolietStandardNames.Size = new System.Drawing.Size(136, 24);
			this.radioButtonJolietStandardNames.TabIndex = 1;
			this.radioButtonJolietStandardNames.Text = "Standard Names (64)";
			// 
			// radioButtonJolietLongNames
			// 
			this.radioButtonJolietLongNames.Checked = true;
			this.radioButtonJolietLongNames.Location = new System.Drawing.Point(16, 16);
			this.radioButtonJolietLongNames.Name = "radioButtonJolietLongNames";
			this.radioButtonJolietLongNames.Size = new System.Drawing.Size(136, 24);
			this.radioButtonJolietLongNames.TabIndex = 0;
			this.radioButtonJolietLongNames.TabStop = true;
			this.radioButtonJolietLongNames.Text = "Long Names (107)";
			// 
			// groupBoxCahceSmallFiles
			// 
			this.groupBoxCahceSmallFiles.Controls.Add(this.textBoxCacheLimit);
			this.groupBoxCahceSmallFiles.Controls.Add(this.label3);
			this.groupBoxCahceSmallFiles.Controls.Add(this.textBoxSmallFilesTreshold);
			this.groupBoxCahceSmallFiles.Controls.Add(this.label2);
			this.groupBoxCahceSmallFiles.Location = new System.Drawing.Point(8, 216);
			this.groupBoxCahceSmallFiles.Name = "groupBoxCahceSmallFiles";
			this.groupBoxCahceSmallFiles.Size = new System.Drawing.Size(576, 48);
			this.groupBoxCahceSmallFiles.TabIndex = 13;
			this.groupBoxCahceSmallFiles.TabStop = false;
			// 
			// textBoxCacheLimit
			// 
			this.textBoxCacheLimit.Location = new System.Drawing.Point(496, 16);
			this.textBoxCacheLimit.Name = "textBoxCacheLimit";
			this.textBoxCacheLimit.Size = new System.Drawing.Size(64, 20);
			this.textBoxCacheLimit.TabIndex = 4;
			this.textBoxCacheLimit.Text = "";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(312, 16);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(176, 24);
			this.label3.TabIndex = 3;
			this.label3.Text = "Cache limit (blocks, 2048 bytes):";
			// 
			// textBoxSmallFilesTreshold
			// 
			this.textBoxSmallFilesTreshold.Location = new System.Drawing.Point(232, 16);
			this.textBoxSmallFilesTreshold.Name = "textBoxSmallFilesTreshold";
			this.textBoxSmallFilesTreshold.Size = new System.Drawing.Size(64, 20);
			this.textBoxSmallFilesTreshold.TabIndex = 2;
			this.textBoxSmallFilesTreshold.Text = "";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(16, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(216, 24);
			this.label2.TabIndex = 1;
			this.label2.Text = "Small file threshold (blocks, 2048 bytes):";
			// 
			// checkBoxCacheSmallFiles
			// 
			this.checkBoxCacheSmallFiles.Checked = true;
			this.checkBoxCacheSmallFiles.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxCacheSmallFiles.Location = new System.Drawing.Point(24, 208);
			this.checkBoxCacheSmallFiles.Name = "checkBoxCacheSmallFiles";
			this.checkBoxCacheSmallFiles.Size = new System.Drawing.Size(112, 24);
			this.checkBoxCacheSmallFiles.TabIndex = 0;
			this.checkBoxCacheSmallFiles.Text = "Cache small files";
			this.checkBoxCacheSmallFiles.CheckedChanged += new System.EventHandler(this.checkBoxCacheSmallFiles_CheckedChanged);
			// 
			// checkBoxCacheCDROMFiles
			// 
			this.checkBoxCacheCDROMFiles.Location = new System.Drawing.Point(16, 272);
			this.checkBoxCacheCDROMFiles.Name = "checkBoxCacheCDROMFiles";
			this.checkBoxCacheCDROMFiles.Size = new System.Drawing.Size(128, 24);
			this.checkBoxCacheCDROMFiles.TabIndex = 14;
			this.checkBoxCacheCDROMFiles.Text = "Cache CDROM files";
			// 
			// checkBoxCacheNetworkFiles
			// 
			this.checkBoxCacheNetworkFiles.Location = new System.Drawing.Point(144, 272);
			this.checkBoxCacheNetworkFiles.Name = "checkBoxCacheNetworkFiles";
			this.checkBoxCacheNetworkFiles.Size = new System.Drawing.Size(128, 24);
			this.checkBoxCacheNetworkFiles.TabIndex = 15;
			this.checkBoxCacheNetworkFiles.Text = "Cache network files";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.chkCdXa);
			this.groupBox4.Controls.Add(this.checkBoxRawMode);
			this.groupBox4.Controls.Add(this.comboBoxRecordingMode);
			this.groupBox4.Controls.Add(this.comboBoxRecordingSpeed);
			this.groupBox4.Controls.Add(this.label7);
			this.groupBox4.Controls.Add(this.label8);
			this.groupBox4.Controls.Add(this.checkBoxCloseDisc);
			this.groupBox4.Controls.Add(this.checkBoxEjectWhenDone);
			this.groupBox4.Controls.Add(this.checkBoxSimulate);
			this.groupBox4.Location = new System.Drawing.Point(8, 304);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(368, 96);
			this.groupBox4.TabIndex = 16;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Parameters";
			// 
			// chkCdXa
			// 
			this.chkCdXa.Location = new System.Drawing.Point(240, 64);
			this.chkCdXa.Name = "chkCdXa";
			this.chkCdXa.TabIndex = 21;
			this.chkCdXa.Text = "CD-ROM XA";
			// 
			// checkBoxRawMode
			// 
			this.checkBoxRawMode.Location = new System.Drawing.Point(128, 64);
			this.checkBoxRawMode.Name = "checkBoxRawMode";
			this.checkBoxRawMode.TabIndex = 20;
			this.checkBoxRawMode.Text = "Raw Mode";
			this.checkBoxRawMode.CheckedChanged += new System.EventHandler(this.checkBoxRawMode_CheckedChanged);
			// 
			// comboBoxRecordingMode
			// 
			this.comboBoxRecordingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRecordingMode.ItemHeight = 13;
			this.comboBoxRecordingMode.Location = new System.Drawing.Point(240, 40);
			this.comboBoxRecordingMode.Name = "comboBoxRecordingMode";
			this.comboBoxRecordingMode.Size = new System.Drawing.Size(121, 21);
			this.comboBoxRecordingMode.TabIndex = 19;
			this.comboBoxRecordingMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxRecordingMode_SelectedIndexChanged);
			// 
			// comboBoxRecordingSpeed
			// 
			this.comboBoxRecordingSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRecordingSpeed.ItemHeight = 13;
			this.comboBoxRecordingSpeed.Location = new System.Drawing.Point(240, 16);
			this.comboBoxRecordingSpeed.Name = "comboBoxRecordingSpeed";
			this.comboBoxRecordingSpeed.Size = new System.Drawing.Size(72, 21);
			this.comboBoxRecordingSpeed.TabIndex = 18;
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(128, 40);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(100, 16);
			this.label7.TabIndex = 17;
			this.label7.Text = "Recording Mode:";
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(128, 16);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(96, 16);
			this.label8.TabIndex = 16;
			this.label8.Text = "Recording Speed:";
			// 
			// checkBoxCloseDisc
			// 
			this.checkBoxCloseDisc.Checked = true;
			this.checkBoxCloseDisc.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxCloseDisc.Location = new System.Drawing.Point(8, 64);
			this.checkBoxCloseDisc.Name = "checkBoxCloseDisc";
			this.checkBoxCloseDisc.TabIndex = 15;
			this.checkBoxCloseDisc.Text = "Close Disc";
			// 
			// checkBoxEjectWhenDone
			// 
			this.checkBoxEjectWhenDone.Location = new System.Drawing.Point(8, 40);
			this.checkBoxEjectWhenDone.Name = "checkBoxEjectWhenDone";
			this.checkBoxEjectWhenDone.Size = new System.Drawing.Size(112, 24);
			this.checkBoxEjectWhenDone.TabIndex = 14;
			this.checkBoxEjectWhenDone.Text = "Eject when done";
			// 
			// checkBoxSimulate
			// 
			this.checkBoxSimulate.Location = new System.Drawing.Point(8, 16);
			this.checkBoxSimulate.Name = "checkBoxSimulate";
			this.checkBoxSimulate.TabIndex = 13;
			this.checkBoxSimulate.Text = "Simulate";
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.checkBoxQuick);
			this.groupBox5.Controls.Add(this.buttonEraseDisc);
			this.groupBox5.Location = new System.Drawing.Point(488, 304);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(96, 96);
			this.groupBox5.TabIndex = 26;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Erase";
			// 
			// checkBoxQuick
			// 
			this.checkBoxQuick.Checked = true;
			this.checkBoxQuick.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxQuick.Location = new System.Drawing.Point(16, 24);
			this.checkBoxQuick.Name = "checkBoxQuick";
			this.checkBoxQuick.Size = new System.Drawing.Size(56, 24);
			this.checkBoxQuick.TabIndex = 25;
			this.checkBoxQuick.Text = "Quick";
			// 
			// buttonEraseDisc
			// 
			this.buttonEraseDisc.Location = new System.Drawing.Point(8, 56);
			this.buttonEraseDisc.Name = "buttonEraseDisc";
			this.buttonEraseDisc.TabIndex = 26;
			this.buttonEraseDisc.Text = "E&rase disc";
			this.buttonEraseDisc.Click += new System.EventHandler(this.buttonEraseDisc_Click);
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.buttonEject);
			this.groupBox6.Controls.Add(this.buttonCloseTray);
			this.groupBox6.Location = new System.Drawing.Point(384, 304);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(96, 96);
			this.groupBox6.TabIndex = 25;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Eject Device";
			// 
			// buttonEject
			// 
			this.buttonEject.Location = new System.Drawing.Point(8, 56);
			this.buttonEject.Name = "buttonEject";
			this.buttonEject.TabIndex = 23;
			this.buttonEject.Text = "E&ject";
			this.buttonEject.Click += new System.EventHandler(this.buttonEject_Click);
			// 
			// buttonCloseTray
			// 
			this.buttonCloseTray.Location = new System.Drawing.Point(8, 24);
			this.buttonCloseTray.Name = "buttonCloseTray";
			this.buttonCloseTray.TabIndex = 22;
			this.buttonCloseTray.Text = "&Close Tray";
			this.buttonCloseTray.Click += new System.EventHandler(this.buttonCloseTray_Click);
			// 
			// groupBox7
			// 
			this.groupBox7.Controls.Add(this.checkBoxLoadLastTrack);
			this.groupBox7.Location = new System.Drawing.Point(464, 408);
			this.groupBox7.Name = "groupBox7";
			this.groupBox7.Size = new System.Drawing.Size(120, 96);
			this.groupBox7.TabIndex = 27;
			this.groupBox7.TabStop = false;
			this.groupBox7.Text = "Multisession";
			// 
			// checkBoxLoadLastTrack
			// 
			this.checkBoxLoadLastTrack.Location = new System.Drawing.Point(8, 16);
			this.checkBoxLoadLastTrack.Name = "checkBoxLoadLastTrack";
			this.checkBoxLoadLastTrack.Size = new System.Drawing.Size(104, 32);
			this.checkBoxLoadLastTrack.TabIndex = 0;
			this.checkBoxLoadLastTrack.Text = "Load last track";
			// 
			// buttonBurn
			// 
			this.buttonBurn.Location = new System.Drawing.Point(8, 512);
			this.buttonBurn.Name = "buttonBurn";
			this.buttonBurn.Size = new System.Drawing.Size(88, 23);
			this.buttonBurn.TabIndex = 28;
			this.buttonBurn.Text = "Burn";
			this.buttonBurn.Click += new System.EventHandler(this.buttonBurn_Click);
			// 
			// buttonCreateImage
			// 
			this.buttonCreateImage.Location = new System.Drawing.Point(112, 512);
			this.buttonCreateImage.Name = "buttonCreateImage";
			this.buttonCreateImage.Size = new System.Drawing.Size(88, 23);
			this.buttonCreateImage.TabIndex = 29;
			this.buttonCreateImage.Text = "Create Image";
			this.buttonCreateImage.Click += new System.EventHandler(this.buttonCreateImage_Click);
			// 
			// buttonBurnImage
			// 
			this.buttonBurnImage.Location = new System.Drawing.Point(216, 512);
			this.buttonBurnImage.Name = "buttonBurnImage";
			this.buttonBurnImage.Size = new System.Drawing.Size(88, 23);
			this.buttonBurnImage.TabIndex = 30;
			this.buttonBurnImage.Text = "Burn Image";
			this.buttonBurnImage.Click += new System.EventHandler(this.buttonBurnImage_Click);
			// 
			// buttonExit
			// 
			this.buttonExit.Location = new System.Drawing.Point(512, 512);
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.TabIndex = 31;
			this.buttonExit.Text = "Exit";
			this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
			// 
			// grpBoot
			// 
			this.grpBoot.Controls.Add(this.textBoxBootSectorCount);
			this.grpBoot.Controls.Add(this.label12);
			this.grpBoot.Controls.Add(this.textBoxBootLoadSegment);
			this.grpBoot.Controls.Add(this.label11);
			this.grpBoot.Controls.Add(this.cmbEmulation);
			this.grpBoot.Controls.Add(this.label10);
			this.grpBoot.Controls.Add(this.btnBrowseBootImage);
			this.grpBoot.Controls.Add(this.txtBootImage);
			this.grpBoot.Controls.Add(this.label9);
			this.grpBoot.Enabled = false;
			this.grpBoot.Location = new System.Drawing.Point(8, 408);
			this.grpBoot.Name = "grpBoot";
			this.grpBoot.Size = new System.Drawing.Size(448, 96);
			this.grpBoot.TabIndex = 32;
			this.grpBoot.TabStop = false;
			// 
			// textBoxBootSectorCount
			// 
			this.textBoxBootSectorCount.Location = new System.Drawing.Point(408, 56);
			this.textBoxBootSectorCount.MaxLength = 3;
			this.textBoxBootSectorCount.Name = "textBoxBootSectorCount";
			this.textBoxBootSectorCount.Size = new System.Drawing.Size(32, 20);
			this.textBoxBootSectorCount.TabIndex = 8;
			this.textBoxBootSectorCount.Text = "4";
			this.textBoxBootSectorCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(320, 56);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(80, 32);
			this.label12.TabIndex = 7;
			this.label12.Text = "Sector Count (HEX)";
			// 
			// textBoxBootLoadSegment
			// 
			this.textBoxBootLoadSegment.Location = new System.Drawing.Point(280, 56);
			this.textBoxBootLoadSegment.MaxLength = 3;
			this.textBoxBootLoadSegment.Name = "textBoxBootLoadSegment";
			this.textBoxBootLoadSegment.Size = new System.Drawing.Size(32, 20);
			this.textBoxBootLoadSegment.TabIndex = 6;
			this.textBoxBootLoadSegment.Text = "0";
			this.textBoxBootLoadSegment.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(192, 56);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(80, 32);
			this.label11.TabIndex = 5;
			this.label11.Text = "Load Segment (HEX)";
			// 
			// cmbEmulation
			// 
			this.cmbEmulation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbEmulation.Enabled = false;
			this.cmbEmulation.Items.AddRange(new object[] {
															  "No Emulation",
															  "Floppy Emulation 1.20MB",
															  "Floppy Emulation 1.44MB",
															  "Floppy Emulation 2.88MB"});
			this.cmbEmulation.Location = new System.Drawing.Point(70, 54);
			this.cmbEmulation.Name = "cmbEmulation";
			this.cmbEmulation.Size = new System.Drawing.Size(114, 21);
			this.cmbEmulation.TabIndex = 4;
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(8, 56);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(72, 16);
			this.label10.TabIndex = 3;
			this.label10.Text = "Emulation";
			// 
			// btnBrowseBootImage
			// 
			this.btnBrowseBootImage.Enabled = false;
			this.btnBrowseBootImage.Location = new System.Drawing.Point(368, 24);
			this.btnBrowseBootImage.Name = "btnBrowseBootImage";
			this.btnBrowseBootImage.TabIndex = 2;
			this.btnBrowseBootImage.Text = "Browse";
			this.btnBrowseBootImage.Click += new System.EventHandler(this.btnBrowseBootImage_Click);
			// 
			// txtBootImage
			// 
			this.txtBootImage.Location = new System.Drawing.Point(56, 24);
			this.txtBootImage.Name = "txtBootImage";
			this.txtBootImage.ReadOnly = true;
			this.txtBootImage.Size = new System.Drawing.Size(304, 20);
			this.txtBootImage.TabIndex = 1;
			this.txtBootImage.Text = "";
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(8, 24);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(48, 23);
			this.label9.TabIndex = 0;
			this.label9.Text = "Image";
			// 
			// chkBootable
			// 
			this.chkBootable.Location = new System.Drawing.Point(24, 408);
			this.chkBootable.Name = "chkBootable";
			this.chkBootable.Size = new System.Drawing.Size(72, 16);
			this.chkBootable.TabIndex = 33;
			this.chkBootable.Text = "Bootable";
			this.chkBootable.CheckedChanged += new System.EventHandler(this.chkBootable_CheckedChanged);
			// 
			// checkBoxAddVersionToIsoNames
			// 
			this.checkBoxAddVersionToIsoNames.Location = new System.Drawing.Point(272, 272);
			this.checkBoxAddVersionToIsoNames.Name = "checkBoxAddVersionToIsoNames";
			this.checkBoxAddVersionToIsoNames.Size = new System.Drawing.Size(176, 24);
			this.checkBoxAddVersionToIsoNames.TabIndex = 34;
			this.checkBoxAddVersionToIsoNames.Text = "Add version to ISO filenames";
			// 
			// checkBoxSortByFilename
			// 
			this.checkBoxSortByFilename.Checked = true;
			this.checkBoxSortByFilename.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxSortByFilename.Location = new System.Drawing.Point(456, 272);
			this.checkBoxSortByFilename.Name = "checkBoxSortByFilename";
			this.checkBoxSortByFilename.Size = new System.Drawing.Size(112, 24);
			this.checkBoxSortByFilename.TabIndex = 35;
			this.checkBoxSortByFilename.Text = "Sort by filename";
			// 
			// DataBurner
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(592, 543);
			this.Controls.Add(this.checkBoxSortByFilename);
			this.Controls.Add(this.checkBoxAddVersionToIsoNames);
			this.Controls.Add(this.chkBootable);
			this.Controls.Add(this.grpBoot);
			this.Controls.Add(this.checkBoxCacheSmallFiles);
			this.Controls.Add(this.buttonExit);
			this.Controls.Add(this.buttonBurnImage);
			this.Controls.Add(this.buttonCreateImage);
			this.Controls.Add(this.buttonBurn);
			this.Controls.Add(this.groupBox7);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox6);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.checkBoxCacheNetworkFiles);
			this.Controls.Add(this.checkBoxCacheCDROMFiles);
			this.Controls.Add(this.groupBoxCahceSmallFiles);
			this.Controls.Add(this.groupBoxJoliet);
			this.Controls.Add(this.groupBoxISO);
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
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DataBurner";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "DataBurner.NET";
			this.Load += new System.EventHandler(this.DataBurner_Load);
			this.groupBoxISO.ResumeLayout(false);
			this.groupBoxJoliet.ResumeLayout(false);
			this.groupBoxCahceSmallFiles.ResumeLayout(false);
			this.groupBox4.ResumeLayout(false);
			this.groupBox5.ResumeLayout(false);
			this.groupBox6.ResumeLayout(false);
			this.groupBox7.ResumeLayout(false);
			this.grpBoot.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void  Main() 
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

                Application.Run(new DataBurner());

                // Shutdown the SDK
                PrimoSoftware.Burner.Library.Shutdown();

			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}

		private void GetSupportedWriteSpeeds(Device dev)
		{
			string sSelectedSpeed = "";
			if(comboBoxRecordingSpeed.SelectedIndex > -1)
				sSelectedSpeed = ((SpeedListItem)comboBoxRecordingSpeed.SelectedItem).ToString();

			comboBoxRecordingSpeed.Items.Clear();

            IList<SpeedDescriptor> speeds = dev.GetWriteSpeeds();
            for (int i = 0; i < speeds.Count; i++)
            {
				SpeedDescriptor desc = speeds[i];

                SpeedListItem item = new SpeedListItem(dev.MediaIsDVD, desc.TransferRateKB);
				comboBoxRecordingSpeed.Items.Add(item);
			}
			
			comboBoxRecordingSpeed.SelectedIndex = comboBoxRecordingSpeed.FindString(sSelectedSpeed);
			if (-1 == comboBoxRecordingSpeed.SelectedIndex)
				comboBoxRecordingSpeed.SelectedIndex = 0;
		}

		private void SetDeviceControls(int nIndex)
		{
			_deviceIndex = nIndex;
			Device device = _deviceEnum.CreateDevice((int)_deviceIndexes[_deviceIndex]);
			if (null == device) 
			{
				MessageBox.Show("Function SetDeviceControls : Unable to get device object\nPossible reason : auto-insert notification enabled\nwhile writing (or any other) operation is in progress.\nThis is just a warning message - your CD is not damaged.");
				return;
			}

			// Capacity
			_capacity = device.MediaFreeSpace * (int)BlockSize.CdRom;
			buttonBurn.Enabled = (0 != _capacity) && (_capacity >= _requiredSpace);

			// Write speed
			GetSupportedWriteSpeeds(device);

			// Other properties
			bool bRW, bRWDisk;
			bRW		= ReWritePossible(device);
			bRWDisk = device.MediaIsReWritable;

			checkBoxQuick.Enabled = bRW && bRWDisk;
			buttonEraseDisc.Enabled = bRW && bRWDisk;

			_rawDao = device.CDFeatures.CanWriteRawDao;

			comboBoxRecordingModeSelectedChanged();

			labelFreeSpace.Text = String.Format("Free space : {0}M", _capacity/(1024*1024));
			labelRequiredSpace.Text = String.Format("Required space : {0}M", _requiredSpace/(1024*1024));

			device.Dispose();
		}

        bool ReWritePossible(Device device)
        {
            CDFeatures cdfeatures = device.CDFeatures;
            DVDFeatures dvdfeatures = device.DVDFeatures;
            BDFeatures bdfeatures = device.BDFeatures;

            bool cdRewritePossible = cdfeatures.CanWriteCDRW;
            bool dvdRewritePossible = dvdfeatures.CanReadDVDMinusRW || dvdfeatures.CanReadDVDPlusRW || dvdfeatures.CanWriteDVDRam;
            bool bdRewritePossible = bdfeatures.CanReadBDRE;

            return cdRewritePossible || dvdRewritePossible || bdRewritePossible;
        }

		string GetShortLogicalName(string sFullName)
		{
			StringBuilder sbObj = new StringBuilder(255);
			Kernel32.GetShortPathName(sFullName, sbObj, sbObj.Capacity);
			return sbObj.ToString().Substring(sbObj.ToString().LastIndexOf("\\") + 1);
		}

		void CreateFileTree(ImageType dwImageType, DataFile currentFile, string sCurrentPath)
		{
			ArrayList DirectoryItems = new ArrayList();
			DirectoryItems.AddRange(Directory.GetDirectories(sCurrentPath + "\\"));
			DirectoryItems.AddRange(Directory.GetFiles(sCurrentPath + "\\"));

			foreach(string sItemName in DirectoryItems)
			{
				bool bDir = Directory.Exists(sItemName);
				if (bDir)
				{
					DirectoryInfo di = new DirectoryInfo(sItemName);
					if (("." == di.Name) || (".." == di.Name))
						continue;
					
					// Create a folder entry and scan it for the files
					DataFile dataFile = new DataFile();

					dataFile.IsDirectory = true;
					dataFile.FilePath = di.Name;
					dataFile.LongFilename = di.Name;
					
					string strShortName = GetShortLogicalName(di.FullName);
					dataFile.ShortFilename = strShortName == di.Name ? "" : strShortName;
					
					bool bHide = 0 != (di.Attributes & FileAttributes.Hidden);
                    if (bHide)
					    dataFile.HiddenMask = (int)dwImageType;

					// Search for all files in the directory
					CreateFileTree(dwImageType, dataFile, sItemName);

					// Add this folder to the tree
					currentFile.Children.Add(dataFile);
				}
				else
				{
					FileInfo fi = new FileInfo(sItemName);

					// File
                    DataFile dataFile = new DataFile();

					dataFile.IsDirectory = false;
					dataFile.FilePath = fi.FullName;
					dataFile.LongFilename = fi.Name;

					string strShortName = GetShortLogicalName(fi.FullName);
					dataFile.ShortFilename = strShortName == fi.Name ? "" : strShortName;

					bool bHide = 0 != (fi.Attributes & FileAttributes.Hidden);
                    if (bHide)
                        dataFile.HiddenMask = (int)dwImageType;

					// Add this file to the tree
					currentFile.Children.Add(dataFile);
				}
			}

			DirectoryItems.Clear();
		}

		bool SetImageLayoutFromFolder(DataDisc dataDisc, string fname, bool sortByFilename)
		{
            DataFile dataFile = new DataFile();

			// Entry for the root of the image file system
			dataFile.IsDirectory = true;
			dataFile.FilePath = "\\";			
			dataFile.LongFilename = "\\";

			CreateFileTree(dataDisc.ImageType, dataFile, fname);

			bool bRes = dataDisc.SetImageLayout(dataFile, sortByFilename);

			return bRes;
		}

		private string ConstructErrorMessage(int systemError)
		{
			return new System.ComponentModel.Win32Exception(systemError).Message;
		}

		private string ConstructErrorMessage(Device device)
		{
			string message = string.Empty;
			if (null != device)
			{
                ErrorInfo error = device.Error;
				switch (error.Facility)
				{
					case ErrorFacility.SystemWindows:
						message = ConstructErrorMessage(error.Code);
						break;
					default:
						message = string.Format("Device error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, error.Code, error.Message);
						break;
				}
			}
			return message;
		}

		private string ConstructErrorMessage(DataDisc dataDisc, Device device)
		{
			string message = string.Empty;
			if (null != dataDisc)
			{
                ErrorInfo error = device.Error;
				switch (error.Facility)
				{
                    case ErrorFacility.SystemWindows:
                        message = ConstructErrorMessage(error.Code);
                        break;
                    case ErrorFacility.Device:
						message = ConstructErrorMessage(device);
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
			string message = ConstructErrorMessage(dataDisc, device);
			MessageBox.Show(message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void buttonBrowse_Click(object sender, System.EventArgs e)
		{
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxRootDir.Text = folderBrowserDialog.SelectedPath;
				if ( '\\' != textBoxRootDir.Text[textBoxRootDir.Text.Length-1] )
				{
					textBoxRootDir.Text += "\\";
				}
				Cursor.Current = Cursors.WaitCursor;
				
				DataDisc dataDisc = new DataDisc();
				dataDisc.ImageType = GetImageType();

				bool bRes = SetImageLayoutFromFolder(dataDisc, textBoxRootDir.Text, false);
				if (!bRes) 
				{
					string str = string.Format("A problem occured when trying to set directory:\n{0}", textBoxRootDir.Text);
					MessageBox.Show(str);

					textBoxRootDir.Text = "";
				} 
				else 
				{
					_requiredSpace = dataDisc.ImageSizeInBytes;
				}

				Cursor.Current = Cursors.Arrow;

				dataDisc.Dispose();

				SetDeviceControls(_deviceIndex);
			}
		}

		private void buttonBurn_Click(object sender, System.EventArgs e)
		{
			if(!ValidateForm())
				return;

			RunOperation(Operations.BurnOnTheFly);
		}

		private void buttonCreateImage_Click(object sender, System.EventArgs e)
		{
			if (!ValidateForm())
				return;

			saveFileDialog.Filter = "Image File (*.iso)|*.iso";
			if(saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				_imageFile = saveFileDialog.FileName;
				RunOperation(Operations.CreateImage);
			}
		}

		private void buttonBurnImage_Click(object sender, System.EventArgs e)
		{
			openFileDialog.Filter = "Image File (*.iso)|*.iso";
			if(openFileDialog.ShowDialog() == DialogResult.OK )
			{
				_imageFile = openFileDialog.FileName;
				try
				{
					FileStream fs = new FileStream(_imageFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					long fileSize = fs.Length;
					fs.Close();
					if(fileSize > _capacity)
					{
						MessageBox.Show(String.Format("Cannot write image file {0}\n The file is too big. ",_imageFile));
						return;
					}
				}
				catch
				{
					MessageBox.Show(String.Format("Unable to open file {0}",_imageFile));
					return;
				}

				RunOperation(Operations.BurnImage);
			}
		}

		private void buttonExit_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void buttonCloseTray_Click(object sender, System.EventArgs e)
		{
			Device device = _deviceEnum.CreateDevice((int)_deviceIndexes[_deviceIndex]);
			device.Eject(false);
			device.Dispose();
		}

		private void buttonEject_Click(object sender, System.EventArgs e)
		{
			Device device = _deviceEnum.CreateDevice((int)_deviceIndexes[_deviceIndex]);
			device.Eject(true);
			device.Dispose();
		}

		private void buttonEraseDisc_Click(object sender, System.EventArgs e)
		{
			this.Enabled = false;
			_progressForm = new BurningForm();
			_progressForm.Owner = this;
			_progressForm.burningDone = new BurningDoneHandler(BurningDone);
			_progressForm.StopButtonEnabled = false;
			_progressForm.Show();

			OperationContext ctx = new OperationContext();
			ctx.bErasing = true;
			ctx.bQuick = checkBoxQuick.Checked;
			ctx.nSpeedKB = ((SpeedListItem)comboBoxRecordingSpeed.SelectedItem).SpeedKB;

			ProcessDelegate  process = new ProcessDelegate(Process);
			process.BeginInvoke(ctx, null, null);
		}

		private bool ValidateForm()
		{
			string strRootDir = textBoxRootDir.Text;

			if (!Directory.Exists(strRootDir))
			{
				MessageBox.Show("Please specify a valid root directory");
				textBoxRootDir.Focus();
				return false;
			}

			if(textBoxVolumeName.Text.Length > 16)
			{
				MessageBox.Show("Volume name can contain maximum 16 characters");
				textBoxVolumeName.Focus();
				return false;
			}

			if(checkBoxCacheSmallFiles.Checked)
			{
				try
				{
					Int32.Parse(textBoxSmallFilesTreshold.Text);
				}
				catch
				{
					MessageBox.Show("Invalid value for 'Small file threshold'");
					textBoxSmallFilesTreshold.Focus();
					return false;
				}

				try
				{
					Int32.Parse(textBoxCacheLimit.Text);
				}
				catch
				{
					MessageBox.Show("Invalid value for 'Cache limit'");
					textBoxCacheLimit.Focus();
					return false;
				}
			}


			if (chkBootable.Checked)
			{
				try
				{
					Int32.Parse(textBoxBootLoadSegment.Text, System.Globalization.NumberStyles.HexNumber);
				}
				catch
				{
					MessageBox.Show("Invalid value for 'Boot Load Segment'");
					textBoxBootLoadSegment.Focus();
					return false;
				}

				try
				{
					Int32.Parse(textBoxBootSectorCount.Text, System.Globalization.NumberStyles.HexNumber);
				}
				catch
				{
					MessageBox.Show("Invalid value for 'Boot Sector Count'");
					textBoxBootSectorCount.Focus();
					return false;
				}
			}
			
			return true;
		}

		private BootEmulation GetBootEmulationType()
		{
			switch (cmbEmulation.SelectedIndex)
			{
				case 0:
					return BootEmulation.NoEmulation;
				case 1:
					return BootEmulation.Diskette120;
				case 2:
					return BootEmulation.Diskette144;
				case 3:
					return BootEmulation.Diskette288;
				default:
					return BootEmulation.NoEmulation;
			}
		}

		private void RunOperation(Operations operation)
		{
			this.Enabled = false;
			_progressForm = new BurningForm();
			_progressForm.Owner = this;
			_progressForm.burningDone = new BurningDoneHandler(BurningDone);
			_progressForm.Show();

			OperationContext ctx = new OperationContext();

			ctx.bStopRequest		  = false;
			ctx.bErasing			  = false;
			
			ctx.bSimulate			  = checkBoxSimulate.Checked;
			ctx.bEject				  = checkBoxEjectWhenDone.Checked;

			ctx.bCacheSmallFiles	  = checkBoxCacheSmallFiles.Checked;
			ctx.nSmallFilesCacheLimit = int.Parse(textBoxCacheLimit.Text);
			ctx.nSmallFileSize		  = int.Parse(textBoxSmallFilesTreshold.Text); 
			
			ctx.nSpeedKB			  = ((SpeedListItem)comboBoxRecordingSpeed.SelectedItem).SpeedKB;
			ctx.operation			  = operation;

			ctx.nMode				  = comboBoxRecordingMode.SelectedIndex;
			ctx.bCloseDisc			  = checkBoxCloseDisc.Checked;
			ctx.bRaw				  = checkBoxRawMode.Checked;

			ctx.imageType			  = GetImageType();

			ctx.iso.nLimits			  = GetIsoConstraints();
			ctx.iso.bTreeDepth		  = checkBoxRestrictTreeDepth.Checked;
			ctx.iso.bTraslateNames	  = checkBoxTranslateFilenames.Checked;

			ctx.joliet.nLimits		  = GetJolietConstraints();

			ctx.bCdRomXa			  = chkCdXa.Checked;

			ctx.bLoadLastTrack		  = checkBoxLoadLastTrack.Checked;

			ctx.bBootable = (chkBootable.Checked) && (ctx.imageType != ImageType.Udf);

			ctx.nBootEmulation		= GetBootEmulationType();
			ctx.nBootLoadSegment	= ushort.Parse(textBoxBootLoadSegment.Text, System.Globalization.NumberStyles.HexNumber);
			ctx.nBootSectorCount	= ushort.Parse(textBoxBootSectorCount.Text, System.Globalization.NumberStyles.HexNumber);
			ctx.sBootImageFile		= txtBootImage.Text;

			ctx.bSortByFileName		= checkBoxSortByFilename.Checked;	

			ctx.bQuick				  = checkBoxQuick.Checked;
			ctx.strImageFile		  = _imageFile;
			ctx.strVolumeName         = textBoxVolumeName.Text;
			ctx.strRootDir            = textBoxRootDir.Text;

			ctx.bAddVersionToIsoNames = checkBoxAddVersionToIsoNames.Checked;

			ProcessDelegate  process = new ProcessDelegate(Process);
			process.BeginInvoke(ctx, null, null);
		}

		delegate void ProcessDelegate(OperationContext ctx);
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

			comboBoxDevices.SelectedIndex = _deviceIndex;
			SetDeviceControls(_deviceIndex);
		}

		void Process(OperationContext ctx)
		{
			_progressArgs = new ShowProgressArgs();

			try
			{
				if(ctx.bErasing)
				{
					Device device = _deviceEnum.CreateDevice((int)_deviceIndexes[_deviceIndex]);
					_progressArgs.status = "Erasing disc. Please wait...";
					_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
					device.WriteSpeedKB = ctx.nSpeedKB;
					device.Erase(ctx.bQuick);
					device.Dispose();
					return;
				}

                _device = _deviceEnum.CreateDevice((int)_deviceIndexes[_deviceIndex]);

				int prevTrackNumber = 0;
				if(ctx.bLoadLastTrack)
				{
					prevTrackNumber = GetLastCompleteTrack(_device);
				}
				
				ctx.bCdRomXa = chkCdXa.Checked;
				ctx.sBootImageFile = txtBootImage.Text;

				switch(ctx.operation) 
				{
					case Operations.Invalid:
						break;
					case Operations.CreateImage:
						ImageCreate(_device, ctx, 0);
						break;
					case Operations.BurnImage:
						ImageBurn(_device,ctx, 0);
						break;
					case Operations.BurnOnTheFly:
						BurnOnTheFly(_device, ctx, prevTrackNumber);
						break;
				}
			}
			finally
			{
				_progressArgs.bDone = true;
				_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
			}
		}

		private bool ImageCreate(Device device, OperationContext ctx, int prevTrackNumber) 
		{
			DataDisc dataDisc = new DataDisc();

			dataDisc.UdfVolumeProps.VolumeLabel = ctx.strVolumeName;
            dataDisc.JolietVolumeProps.VolumeLabel = ctx.strVolumeName;
            dataDisc.IsoVolumeProps.VolumeLabel = ctx.strVolumeName;

			dataDisc.ImageType = ctx.imageType;
			dataDisc.ImageConstraints = ctx.iso.nLimits;
			dataDisc.TranslateFilenames = ctx.iso.bTraslateNames;
			dataDisc.IsoVolumeProps.VersionAppendedToNames = ctx.bAddVersionToIsoNames;

			// Bootable parameters
			dataDisc.Bootable = ctx.bBootable;
			if (ctx.bBootable)
			{
				dataDisc.BootProps.Emulation = ctx.nBootEmulation;
                dataDisc.BootProps.ImageFile = ctx.sBootImageFile;
                dataDisc.BootProps.LoadSegment = ctx.nBootLoadSegment;
                dataDisc.BootProps.SectorCount = ctx.nBootSectorCount;

                // Bootable discs for Windows need to disable this features 
                // to make the disc compatible with the boot loader.
                dataDisc.IsoVolumeProps.DotAppendedToNames = false;
                dataDisc.JolietVolumeProps.DotAppendedToNames = false;
                dataDisc.IsoVolumeProps.VersionAppendedToNames = false;
                dataDisc.JolietVolumeProps.VersionAppendedToNames = false;
			}

			dataDisc.SessionStartAddress = 0;

			dataDisc.OnContinueBurn += OnContinueBurn;
			dataDisc.OnFileStatus += OnFileStatus;
			dataDisc.OnProgress += OnProgress;
			dataDisc.OnStatus += OnStatus;
			
			bool bRes = SetImageLayoutFromFolder(dataDisc, ctx.strRootDir, ctx.bSortByFileName);
			if (!bRes) 
			{
				ShowErrorMessage(dataDisc, device);
				dataDisc.Dispose();
				return false;
			}

			bRes = dataDisc.CreateImageFile(ctx.strImageFile);
			if (!bRes)
			{
				ShowErrorMessage(dataDisc, device);
				dataDisc.Dispose();
				return false;
			}

			dataDisc.Dispose();
			return true;
		}

		private bool ImageBurn(Device device, OperationContext ctx, int prevTrackNumber)
		{
			DataDisc dataDisc = new DataDisc();
			dataDisc.Device = device;
			dataDisc.SessionStartAddress = device.NewSessionStartAddress;

			dataDisc.CachePolicy.CacheSmallFiles = ctx.bCacheSmallFiles;
			
			if(ctx.bCacheSmallFiles)
			{
                dataDisc.CachePolicy.SmallFileSizeThreshold = ctx.nSmallFileSize;
                dataDisc.CachePolicy.SmallFilesCacheSize = ctx.nSmallFilesCacheLimit;
			}

			dataDisc.SimulateBurn = ctx.bSimulate;
			
			// Set write mode
			if (!ctx.bRaw && (0 == ctx.nMode || 1 == ctx.nMode))
				// Session-At-Once (also called Disc-At-Once)
				dataDisc.WriteMethod = WriteMethod.Sao;
			else if (ctx.bRaw)
				// RAW Disc-At-Once
				dataDisc.WriteMethod = WriteMethod.RawDao;
			else if (2 == ctx.nMode)
				// Track-At-Once
				dataDisc.WriteMethod = WriteMethod.Tao;
			else
				// Packet
				dataDisc.WriteMethod = WriteMethod.Packet;

			dataDisc.CloseDisc = ctx.bCloseDisc;

			dataDisc.OnContinueBurn += OnContinueBurn;
			dataDisc.OnFileStatus += OnFileStatus;
			dataDisc.OnProgress += OnProgress;
			dataDisc.OnStatus += OnStatus;

			// set speed
			device.WriteSpeedKB = ctx.nSpeedKB;

			if(!dataDisc.WriteImageToDisc(ctx.strImageFile, true))
			{
				ShowErrorMessage(dataDisc, device);
			
				dataDisc.Dispose();
				return false;
			}

			if (ctx.bEject)
				device.Eject(true);

			dataDisc.Dispose();
			return true;
		}

		private bool BurnOnTheFly(Device device, OperationContext ctx, int iPrevTrackNumber)
		{
			if(null == device)
				return false;

			DataDisc dataDisc = new DataDisc();
			dataDisc.Device = device;

			// Set the session start address. Must do this before intializing the directory structure.
			dataDisc.SessionStartAddress = device.NewSessionStartAddress;

			// Multi-session. Load previous track
			if (iPrevTrackNumber > 0)
				dataDisc.LoadTrackLayout = iPrevTrackNumber;

			dataDisc.ImageType = ctx.imageType;
			dataDisc.ImageConstraints = ctx.iso.nLimits | ctx.joliet.nLimits;
			dataDisc.TranslateFilenames = ctx.iso.bTraslateNames;

            dataDisc.IsoVolumeProps.VersionAppendedToNames = ctx.bAddVersionToIsoNames;

            // Bootable parameters
			dataDisc.Bootable = ctx.bBootable;
			if (ctx.bBootable)
			{
				dataDisc.BootProps.Emulation = ctx.nBootEmulation;
				dataDisc.BootProps.ImageFile = ctx.sBootImageFile;
				dataDisc.BootProps.LoadSegment = ctx.nBootLoadSegment;
				dataDisc.BootProps.SectorCount = ctx.nBootSectorCount;

                // Bootable discs for Windows need to disable this features 
                // to make the disc compatible with the boot loader.
                dataDisc.IsoVolumeProps.DotAppendedToNames = false;
                dataDisc.JolietVolumeProps.DotAppendedToNames = false;
                dataDisc.IsoVolumeProps.VersionAppendedToNames = false;
                dataDisc.JolietVolumeProps.VersionAppendedToNames = false;
			}

            dataDisc.UdfVolumeProps.VolumeLabel = ctx.strVolumeName;
            dataDisc.JolietVolumeProps.VolumeLabel = ctx.strVolumeName;
            dataDisc.IsoVolumeProps.VolumeLabel = ctx.strVolumeName;

			dataDisc.SimulateBurn = ctx.bSimulate;
			
			// Set write mode
			if (!ctx.bRaw && (0 == ctx.nMode || 1 == ctx.nMode))
				// Session-At-Once (also called Disc-At-Once)
				dataDisc.WriteMethod = WriteMethod.Sao;
			else if (ctx.bRaw)
				// RAW Disc-At-Once
				dataDisc.WriteMethod = WriteMethod.RawDao;
			else if (2 == ctx.nMode)
				// Track-At-Once
				dataDisc.WriteMethod = WriteMethod.Tao;
			else
				// Packet
				dataDisc.WriteMethod = WriteMethod.Packet;

			// CD-ROM XA
			dataDisc.CdRomXa = ctx.bCdRomXa;

			// CloseDisc controls multi-session. Disk must be left open when multi-sessions are desired. 
			dataDisc.CloseDisc = ctx.bCloseDisc;

			dataDisc.OnContinueBurn += OnContinueBurn;
			dataDisc.OnFileStatus += OnFileStatus;
			dataDisc.OnProgress += OnProgress;
			dataDisc.OnStatus += OnStatus;

			// Load the layout
			bool bRes = SetImageLayoutFromFolder(dataDisc, ctx.strRootDir, ctx.bSortByFileName);
			if (!bRes) 
			{
				ShowErrorMessage(dataDisc, device);
				dataDisc.Dispose();
				return false;
			}

			// Set speed
			device.WriteSpeedKB = ctx.nSpeedKB;

			// Burn 
			bRes = dataDisc.WriteToDisc(true);
			if (!bRes) 
			{
				ShowErrorMessage(dataDisc, device);
				
				dataDisc.Dispose();
				return false;
			}

			if (ctx.bEject)
				device.Eject(true);

			dataDisc.Dispose();
			return true;
		}

		protected ImageType GetImageType()
		{
			switch(comboBoxImageType.SelectedIndex)
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

		protected int GetIsoConstraints()
		{
			int ret = (int)ImageConstraints.None;
			if(radioButtonISOLevel2_212.Checked)
				ret = (int)ImageConstraints.IsoLongLevel2;

			if(radioButtonISOLevel2_31.Checked)
				ret = (int)ImageConstraints.IsoLevel2;

			if(radioButtonISOLevel1.Checked)
				ret = (int)ImageConstraints.IsoLevel1;
				
			if(!checkBoxRestrictTreeDepth.Checked)
				ret = (int)ret & ~(int)ImageConstraints.IsoTreeDepth;

			return ret;
		}

		protected int GetJolietConstraints()
		{
			int ret = (int)ImageConstraints.None;
			if (radioButtonJolietStandardNames.Checked)
				ret |= (int)ImageConstraints.JolietStandard;
			return ret;
		}

		private int GetLastCompleteTrack(Device oDevice)
		{
			// Get the last track number from the last session if multisession option was specified
			int nLastTrack = 0;

			// Use the ReadDiscInfo method to get the last track number
			DiscInfo di = oDevice.ReadDiscInfo();
			if (null != di)
			{
				nLastTrack = di.LastTrack;
				// ReadDiskInfo reports the empty space as a track too
				// That's why we need to go back one track to get the last completed track
				
				if (DiscStatus.Open == di.DiscStatus || DiscStatus.Empty == di.DiscStatus)
					nLastTrack--;
			}

			return nLastTrack;
		}

		
		#region Burn Event Handlers

		private void OnContinueBurn(Object sender, DataDiscContinueEventArgs eArgs)
		{
			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
			eArgs.Continue = !_progressArgs.bStopRequest;
		}

		private void OnProgress(Object sender, DataDiscProgressEventArgs eArgs)
		{
			long dwPos = eArgs.Position;
			long dwAll = eArgs.All;

            int progress = (int)((double)dwPos * 100.0 / (double)dwAll);
            if (progress > 100)
                progress = 100;

            int buffer = (int)((100 * (double)(_device.InternalCacheUsedSpace)) / ((double)_device.InternalCacheCapacity));
            if (buffer > 100)
                buffer = 100;

            _progressArgs.progressPos = progress;
            _progressArgs.bufferPos = buffer;
            _progressArgs.nActualWriteSpeed = _device.WriteTransferRate;

			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
		}

		private void OnFileStatus(Object sender, DataDiscFileStatusEventArgs eArgs)
		{
			// nothing to do
		}

		private void OnStatus(Object sender, DataDiscStatusEventArgs eArgs)
		{
			_progressArgs.status = GetTextStatus(eArgs.Status);
			_progressForm.ShowProgress(System.Threading.Thread.CurrentThread, _progressArgs);
		}

		#endregion

		private void comboBoxDevices_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int nCurSel = comboBoxDevices.SelectedIndex;
			if(nCurSel > -1)
				SetDeviceControls(nCurSel);
		}

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

		private void checkBoxCacheSmallFiles_CheckedChanged(object sender, System.EventArgs e)
		{
			groupBoxCahceSmallFiles.Enabled = checkBoxCacheSmallFiles.Checked;
		}

		private void comboBoxRecordingMode_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			comboBoxRecordingModeSelectedChanged();
		}

		private void comboBoxRecordingModeSelectedChanged()
		{
			if (_rawDao && (0 == comboBoxRecordingMode.SelectedIndex))
			{
				checkBoxRawMode.Enabled = true;
				checkBoxCloseDisc.Checked = true;
				checkBoxCloseDisc.Enabled = false;
			}
			// SAO
			else if (1 == comboBoxRecordingMode.SelectedIndex)
			{
                checkBoxCloseDisc.Enabled = true;
                checkBoxRawMode.Checked = false;
                checkBoxRawMode.Enabled = false;
            }
			// TAO
			else
			{
				checkBoxCloseDisc.Enabled = true;
				checkBoxRawMode.Checked = false;
				checkBoxRawMode.Enabled = false;
			}

			checkBoxRawModeChanged();
		}

		private void checkBoxRawMode_CheckedChanged(object sender, System.EventArgs e)
		{
			checkBoxRawModeChanged();
		}

		private void checkBoxRawModeChanged()
		{
			if(0 == comboBoxRecordingMode.SelectedIndex)
			{
				if(checkBoxRawMode.Checked)
					checkBoxCloseDisc.Checked = true;

				checkBoxCloseDisc.Enabled = !checkBoxRawMode.Checked;
			}
		}

		private void comboBoxImageType_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			comboBoxImageTypeSelectedChanged();
		}

		private void comboBoxImageTypeSelectedChanged()
		{
            ImageType imgType = GetImageType();
            bool isoImage = (imgType & ImageType.Iso9660) == ImageType.Iso9660;
            bool jolietImage = (imgType & ImageType.Joliet) == ImageType.Joliet;

            groupBoxISO.Enabled = isoImage;
            groupBoxJoliet.Enabled = jolietImage;
            chkBootable.Enabled = isoImage || jolietImage;
		}

		protected override void WndProc(ref Message msg)
		{
			if (WM_DEVICECHANGE==msg.Msg)
			{
				if (comboBoxDevices.Items.Count > 0)
				{
					SetDeviceControls(_deviceIndex);
				}
			}
			base.WndProc(ref msg);
		}

		private void EnableBootGroup(bool bEnable)
		{
			grpBoot.Enabled = bEnable;
			cmbEmulation.Enabled = bEnable;
			btnBrowseBootImage.Enabled = bEnable;
		}

		private void chkBootable_CheckedChanged(object sender, System.EventArgs e)
		{
			EnableBootGroup(chkBootable.Checked);
		}

		private void btnBrowseBootImage_Click(object sender, System.EventArgs e)
		{
			if(openFileDialog.ShowDialog() == DialogResult.OK)
			{
				txtBootImage.Text = openFileDialog.FileName;
			}
		}

		private void DataBurner_Load(object sender, System.EventArgs e)
		{
			cmbEmulation.SelectedIndex = 0;
		}
	}

	public class Kernel32
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern int GetShortPathName(
			[MarshalAs(UnmanagedType.LPTStr)]
			string path,
			[MarshalAs(UnmanagedType.LPTStr)]
			StringBuilder shortPath,
			int shortPathLength);
	}

	internal enum Operations
	{
		Invalid      = -1,
		CreateImage  = 1,
		BurnImage    = 2,
		BurnOnTheFly = 3
	};

	internal class OperationContext
	{
		public OperationContext()
		{
			nMode = 0;
			bLoadLastTrack = false;
			bCacheSmallFiles = false;
			nSmallFilesCacheLimit = 0;
			nSmallFileSize = 0;
			operation = Operations.Invalid;
			iso = new ISORestrictions();
			joliet = new JolietRestrictions();

			nBootLoadSegment = 0x00;
			nBootSectorCount = 1;
			bAddVersionToIsoNames = false;
			bSortByFileName = false;
		}

		public bool		bErasing;
		public bool		bQuick;
		public bool		bSimulate;
		public bool		bEject;
		public bool		bRaw;
		public bool		bCloseDisc;
		public bool		bCacheSmallFiles;
		public bool	    bStopRequest;
		public int		nMode;
		public Operations	operation;
		public int		nSpeedKB;
		public int		nSmallFileSize;
		public int		nSmallFilesCacheLimit;
		public string   strImageFile;
		public string   strVolumeName;
		public string   strRootDir;
		public bool    bLoadLastTrack;
		public ISORestrictions iso;
		public JolietRestrictions joliet;
		public ImageType imageType;
		public bool bCdRomXa;

		public bool bBootable;
		public BootEmulation nBootEmulation;
		public ushort nBootLoadSegment;
		public ushort nBootSectorCount;
		public string sBootImageFile;
		public bool bAddVersionToIsoNames;
		public bool bSortByFileName;
	}

	internal class ISORestrictions
	{
		public ISORestrictions()
		{
			bTraslateNames = false;
			bTreeDepth = false;
			nLimits = 0;
		}

		public int nLimits;
		public bool bTreeDepth;
		public bool bTraslateNames;
	}

	internal class JolietRestrictions
	{
		public JolietRestrictions()
		{
			nLimits = 0;
		}

		public int nLimits;
	}

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
		public int nActualWriteSpeed;
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
