using System;
using System.IO;
using System.Data;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

using System.Windows.Forms;

using PrimoSoftware.Burner;
using System.Collections.Generic;

namespace VideoDVD
{
	public class BurnerForm: System.Windows.Forms.Form
	{
		#region Contruct / Finalize
		public BurnerForm()
		{
			InitializeComponent();

			m_Burner.Status			+= new VideoDVD.BurnerCallback.Status(Burner_Status);
			m_Burner.Continue		+= new VideoDVD.BurnerCallback.Continue(Burner_Continue);
			m_Burner.ImageProgress	+= new VideoDVD.BurnerCallback.ImageProgress(Burner_ImageProgress);

			try
			{
				m_Burner.Open();

				int deviceCount = 0;
				foreach (var dev in m_Burner.EnumerateDevices()) 
				{
                    comboBoxDevices.Items.Add(dev);
                    deviceCount++;
				}

				if (0 == deviceCount)
					throw new Exception("No writer devices found.");

				m_RequiredSpace = 0;

				// Device combo
				comboBoxDevices.SelectedIndex = 0;
				UpdateDeviceInformation();

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
			
			try
			{
				m_RequiredSpace = m_Burner.CalculateImageSize(sourceFolder);
				UpdateDeviceInformation();

				textBoxRootDir.Text = sourceFolder;
			}
			catch (BurnerException bme)
			{
				ShowErrorMessage(bme);
			}
		}

		private void buttonBurn_Click(object sender, System.EventArgs e)
		{
			if (!ValidateForm())
				return;

			Burn();
			UpdateDeviceInformation();
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

		private bool Burner_Continue()
		{
			return (false == m_progressWindow.Stopped);
		}
		#endregion

		#region Private Methods

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
				settings.VolumeLabel = textBoxVolumeName.Text;

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
                if (!m_Burner.SelectDevice(dev.Index, false))
                    return;

                // Get and display the media profile
                m_ddwCapacity = m_Burner.MediaFreeSpace * (int)BlockSize.Dvd;

                // Media profile
                labelMediaType.Text = m_Burner.MediaProfileString;

                // Required space
                labelRequiredSpace.Text = String.Format("Required space : {0}GB", ((double)m_RequiredSpace / (1e9)).ToString("0.00"));

                // Capacity
                labelFreeSpace.Text = String.Format("Free space : {0}GB", ((double)m_ddwCapacity / (1e9)).ToString("0.00"));

                // Burn Button
                buttonBurn.Enabled = (m_ddwCapacity > 0 && m_ddwCapacity >= m_RequiredSpace);


                m_Burner.ReleaseDevice();

            }
            catch (Exception e)
            {
                ShowErrorMessage(e);
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
		private long	m_RequiredSpace = 0;

		private ProgressForm m_progressWindow;
		private ProgressInfo m_progressInfo = new ProgressInfo();
            
		#endregion

		#region Windows Form Designer generated code
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label labelFreeSpace;
		private System.Windows.Forms.Label labelRequiredSpace;
		private System.Windows.Forms.ComboBox comboBoxDevices;
        private System.Windows.Forms.TextBox textBoxVolumeName;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Button buttonBurn;
        private System.Windows.Forms.TextBox textBoxRootDir;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Label labelMediaType;
        private Label label2;


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
            this.textBoxRootDir = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxVolumeName = new System.Windows.Forms.TextBox();
            this.buttonBurn = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.labelMediaType = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(10, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "DVD Writers :";
            // 
            // comboBoxDevices
            // 
            this.comboBoxDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDevices.Location = new System.Drawing.Point(115, 12);
            this.comboBoxDevices.Name = "comboBoxDevices";
            this.comboBoxDevices.Size = new System.Drawing.Size(413, 24);
            this.comboBoxDevices.TabIndex = 1;
            this.comboBoxDevices.SelectedIndexChanged += new System.EventHandler(this.comboBoxDevices_SelectedIndexChanged);
            // 
            // labelFreeSpace
            // 
            this.labelFreeSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelFreeSpace.Location = new System.Drawing.Point(365, 46);
            this.labelFreeSpace.Name = "labelFreeSpace";
            this.labelFreeSpace.Size = new System.Drawing.Size(163, 24);
            this.labelFreeSpace.TabIndex = 2;
            this.labelFreeSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelRequiredSpace
            // 
            this.labelRequiredSpace.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelRequiredSpace.Location = new System.Drawing.Point(537, 46);
            this.labelRequiredSpace.Name = "labelRequiredSpace";
            this.labelRequiredSpace.Size = new System.Drawing.Size(171, 24);
            this.labelRequiredSpace.TabIndex = 3;
            this.labelRequiredSpace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBoxRootDir
            // 
            this.textBoxRootDir.Location = new System.Drawing.Point(10, 106);
            this.textBoxRootDir.Name = "textBoxRootDir";
            this.textBoxRootDir.ReadOnly = true;
            this.textBoxRootDir.Size = new System.Drawing.Size(589, 22);
            this.textBoxRootDir.TabIndex = 5;
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(605, 104);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(103, 27);
            this.buttonBrowse.TabIndex = 6;
            this.buttonBrowse.Text = "Browse...";
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(249, 165);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(108, 19);
            this.label5.TabIndex = 7;
            this.label5.Text = "Volume Name:";
            // 
            // textBoxVolumeName
            // 
            this.textBoxVolumeName.Location = new System.Drawing.Point(365, 163);
            this.textBoxVolumeName.MaxLength = 16;
            this.textBoxVolumeName.Name = "textBoxVolumeName";
            this.textBoxVolumeName.Size = new System.Drawing.Size(163, 22);
            this.textBoxVolumeName.TabIndex = 8;
            this.textBoxVolumeName.Text = "DVDVIDEO";
            // 
            // buttonBurn
            // 
            this.buttonBurn.Location = new System.Drawing.Point(537, 158);
            this.buttonBurn.Name = "buttonBurn";
            this.buttonBurn.Size = new System.Drawing.Size(171, 32);
            this.buttonBurn.TabIndex = 28;
            this.buttonBurn.Text = "Burn";
            this.buttonBurn.Click += new System.EventHandler(this.buttonBurn_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(10, 158);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(143, 32);
            this.buttonExit.TabIndex = 31;
            this.buttonExit.Text = "Exit";
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // labelMediaType
            // 
            this.labelMediaType.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelMediaType.Location = new System.Drawing.Point(10, 46);
            this.labelMediaType.Name = "labelMediaType";
            this.labelMediaType.Size = new System.Drawing.Size(345, 24);
            this.labelMediaType.TabIndex = 32;
            this.labelMediaType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 84);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(345, 17);
            this.label2.TabIndex = 33;
            this.label2.Text = "DVD-Video Source (must contain a VIDEO_TS folder)";
            // 
            // BurnerForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
            this.ClientSize = new System.Drawing.Size(721, 200);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelMediaType);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonBurn);
            this.Controls.Add(this.textBoxVolumeName);
            this.Controls.Add(this.textBoxRootDir);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.labelRequiredSpace);
            this.Controls.Add(this.labelFreeSpace);
            this.Controls.Add(this.comboBoxDevices);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BurnerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PrimoBurner(tm) for .NET - VideoDVD";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion
	}
}
