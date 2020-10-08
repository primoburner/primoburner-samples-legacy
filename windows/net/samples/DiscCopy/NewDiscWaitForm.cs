using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using PrimoSoftware.Burner;

namespace DiscCopy.NET
{
	public partial class NewDiscWaitForm : Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		private System.Windows.Forms.Label labelNewMedia;
		private System.Windows.Forms.RadioButton radioButtonErase;
		private System.Windows.Forms.RadioButton radioButtonFormat;
		private System.Windows.Forms.CheckBox checkBoxQuick;
		private System.Windows.Forms.Button buttonContinue;
		private System.Windows.Forms.Button buttonCancel;

		#region Constructor
		public NewDiscWaitForm()
		{
			InitializeComponent();
			m_Quick = true;
			m_SelectedCleanMethod = CleanMethod.None;
		}
		#endregion

		#region Dispose
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.labelNewMedia = new System.Windows.Forms.Label();
			this.radioButtonErase = new System.Windows.Forms.RadioButton();
			this.radioButtonFormat = new System.Windows.Forms.RadioButton();
			this.checkBoxQuick = new System.Windows.Forms.CheckBox();
			this.buttonContinue = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// labelNewMedia
			// 
			this.labelNewMedia.Dock = System.Windows.Forms.DockStyle.Top;
			this.labelNewMedia.Location = new System.Drawing.Point(8, 8);
			this.labelNewMedia.Name = "labelNewMedia";
			this.labelNewMedia.Size = new System.Drawing.Size(404, 13);
			this.labelNewMedia.TabIndex = 0;
			this.labelNewMedia.Text = "Please insert an empty or rewritable disc";
			this.labelNewMedia.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// radioButtonErase
			// 
			this.radioButtonErase.AutoSize = true;
			this.radioButtonErase.Location = new System.Drawing.Point(92, 34);
			this.radioButtonErase.Name = "radioButtonErase";
			this.radioButtonErase.Size = new System.Drawing.Size(91, 17);
			this.radioButtonErase.TabIndex = 0;
			this.radioButtonErase.TabStop = true;
			this.radioButtonErase.Text = "Erase medium";
			this.radioButtonErase.UseVisualStyleBackColor = true;
			// 
			// radioButtonFormat
			// 
			this.radioButtonFormat.AutoSize = true;
			this.radioButtonFormat.Location = new System.Drawing.Point(233, 34);
			this.radioButtonFormat.Name = "radioButtonFormat";
			this.radioButtonFormat.Size = new System.Drawing.Size(96, 17);
			this.radioButtonFormat.TabIndex = 1;
			this.radioButtonFormat.TabStop = true;
			this.radioButtonFormat.Text = "Format medium";
			this.radioButtonFormat.UseVisualStyleBackColor = true;
			// 
			// checkBoxQuick
			// 
			this.checkBoxQuick.AutoSize = true;
			this.checkBoxQuick.Checked = true;
			this.checkBoxQuick.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxQuick.Location = new System.Drawing.Point(142, 65);
			this.checkBoxQuick.Name = "checkBoxQuick";
			this.checkBoxQuick.Size = new System.Drawing.Size(137, 17);
			this.checkBoxQuick.TabIndex = 2;
			this.checkBoxQuick.Text = "Use quick erase/format";
			this.checkBoxQuick.UseVisualStyleBackColor = true;
			// 
			// buttonContinue
			// 
			this.buttonContinue.Location = new System.Drawing.Point(122, 99);
			this.buttonContinue.Name = "buttonContinue";
			this.buttonContinue.Size = new System.Drawing.Size(75, 23);
			this.buttonContinue.TabIndex = 3;
			this.buttonContinue.Text = "Continue";
			this.buttonContinue.UseVisualStyleBackColor = true;
			this.buttonContinue.Click += new System.EventHandler(this.buttonContinue_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Location = new System.Drawing.Point(223, 99);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 4;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// NewDiscWaitForm
			// 
			this.AcceptButton = this.buttonContinue;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(420, 133);
			this.ControlBox = false;
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonContinue);
			this.Controls.Add(this.checkBoxQuick);
			this.Controls.Add(this.radioButtonFormat);
			this.Controls.Add(this.radioButtonErase);
			this.Controls.Add(this.labelNewMedia);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "NewDiscWaitForm";
			this.Padding = new System.Windows.Forms.Padding(8);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Load += new System.EventHandler(this.NewDiscWaitForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#region Event handlers
		protected override void WndProc(ref Message msg)
		{
			if (WM_DEVICECHANGE == msg.Msg)
				SetDeviceControls();

			base.WndProc(ref msg);
		}

		private void NewDiscWaitForm_Load(object sender, EventArgs e)
		{
			SetDeviceControls();
		}

		private void buttonContinue_Click(object sender, EventArgs e)
		{
			m_Quick = checkBoxQuick.Checked;
			if (radioButtonErase.Checked)
			{
				m_SelectedCleanMethod = CleanMethod.Erase;
			}
			else if (radioButtonFormat.Checked)
			{
				m_SelectedCleanMethod = CleanMethod.Format;
			}
			else
			{
				m_SelectedCleanMethod = CleanMethod.None;
			}
			// TODO
			// DiscCopy returns DiscCopyError.IncompatibleMedia result when trying to burn an image created from
			// DVD-RW RO to DVD-RW Seq or vice versa. It is possible to convert RO to Seq as well as Seq to RO.
			// All that needs to be done is use the correct Device method - use Erase to convert RO to Seq and
			// Format to convert Seq to RO
			if (MediaProfile.DvdMinusRwRo == m_Burner.OriginalMediaProfile &&
				MediaProfile.DvdMinusRwSeq == m_Burner.MediaProfile &&
				CleanMethod.Erase == m_SelectedCleanMethod)
			{
				string message = "To use the medium currently inserted in the device it must be formatted. Do you want to continue and format the medium?";
				DialogResult result = MessageBox.Show(message , "Warning!", MessageBoxButtons.OKCancel);
				if (DialogResult.OK == result)
				{
					// quick format should be enough
					m_Quick = true;
					m_SelectedCleanMethod = CleanMethod.Format;
				}
				else
				{
					return;
				}
			}
			else if (MediaProfile.DvdMinusRwSeq == m_Burner.OriginalMediaProfile &&
				MediaProfile.DvdMinusRwRo == m_Burner.MediaProfile)
			{
				if (CleanMethod.Format == m_SelectedCleanMethod)
				{
					string message = "To use the medium currently inserted in the device it must be fully erased. Do you want to continue and erase the medium?";
					DialogResult result = MessageBox.Show(message, "Warning!", MessageBoxButtons.OKCancel);
					if (DialogResult.OK == result)
					{
						// full erase is needed
						m_Quick = false;
						m_SelectedCleanMethod = CleanMethod.Erase;
					}
					else
					{
						return;
					}
				}
				else
				{
					// full erase is needed
					m_Quick = false;
				}
			}

			DialogResult = DialogResult.OK;
			this.Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			m_Quick = checkBoxQuick.Checked;
			m_SelectedCleanMethod = CleanMethod.None;
			DialogResult = DialogResult.Cancel;
			this.Close();
		}
		#endregion

		#region Public methods
		public void SetBurner(Burner burner)
		{
			m_Burner = burner;
		}
		#endregion

		#region Private Methods
		private void SetDeviceControls()
		{
			bool usableMediumPresent = false;
			bool enableErase = false;
			bool enableFormat = false;
			bool enableQuick = true;
			m_Burner.RefreshDevice();
			if (m_Burner.MediaIsValidProfile)
			{
				//MediaProfile profile = m_Burner.MediaProfile;
				MediaProfile originalProfile = m_Burner.OriginalMediaProfile;
				MediaProfile profile = m_Burner.MediaProfile;
				// Note: A BD-R SRM disc may be formatted to BD-R SRM+POW using Device::FormatBD(BDFormatType::BdFull, BDFormatSubType::BdRSrmPow)
				if (m_Burner.MediaIsRewritable ||
					(MediaProfile.BdRSrmPow == originalProfile && MediaProfile.BdRSrm == profile))
				{
					if (m_Burner.MediaIsCD)
					{
						enableErase = true;
						enableFormat = false;
						m_SelectedCleanMethod = CleanMethod.Erase;
					}
					else if (m_Burner.MediaIsDVD)
					{
						if (MediaProfile.DvdMinusRwRo == profile ||
							MediaProfile.DvdMinusRwSeq == profile)
						{
							enableErase = true;
							enableFormat = true;
							m_SelectedCleanMethod = CleanMethod.Erase;
						}
						else if (MediaProfile.DvdPlusRw == profile ||
							MediaProfile.DvdRam == profile)
						{
							enableErase = false;
							enableFormat = true;
							m_SelectedCleanMethod = CleanMethod.Format;
						}

					}
					else if (m_Burner.MediaIsBD)
					{
						if (profile == MediaProfile.BdRe ||
							(MediaProfile.BdRSrm == profile && MediaProfile.BdRSrmPow == originalProfile))
						{
							enableErase = false;
							enableFormat = true;
							enableQuick = false;
							m_SelectedCleanMethod = CleanMethod.Format;
						}
					}
					usableMediumPresent = true;
				}
				else if (m_Burner.MediaIsBlank)
				{
					usableMediumPresent = true;
				}
			}
			buttonContinue.Enabled = usableMediumPresent;
			checkBoxQuick.Enabled = (enableErase || enableFormat) && enableQuick;
			radioButtonErase.Enabled = enableErase;
			radioButtonFormat.Enabled = enableFormat;

			radioButtonErase.Checked = false;
			radioButtonFormat.Checked = false;

			switch (m_SelectedCleanMethod)
			{
				case CleanMethod.Erase:
					radioButtonErase.Checked = true;
					break;
				case CleanMethod.Format:
					radioButtonFormat.Checked = true;
					break;
			}
		}
		#endregion

		#region Public properties
		public bool Quick
		{
			get { return m_Quick; }
		}
		public CleanMethod SelectedCleanMethod
		{
			get { return m_SelectedCleanMethod; }
		}
		#endregion

		#region Public constants
		public const int WM_DEVICECHANGE = 0x0219;
		#endregion

		#region Private members
		Burner m_Burner;
		bool m_Quick;
		CleanMethod m_SelectedCleanMethod;
		#endregion
	}
}