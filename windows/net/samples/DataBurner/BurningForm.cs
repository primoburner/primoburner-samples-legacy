using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace DataBurner.NET
{
	public delegate void BurningDoneHandler();

	/// <summary>
	/// Summary description for BurningForm.
	/// </summary>
	public class BurningForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.ProgressBar progressBarOp;
		private System.Windows.Forms.ProgressBar progressBarBuffer;
		private System.Windows.Forms.Label labelStatus;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private bool stop;
		private System.Windows.Forms.Label lblSpeed;
		public BurningDoneHandler burningDone;

		public BurningForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			stop = false;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
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
			this.buttonStop = new System.Windows.Forms.Button();
			this.progressBarOp = new System.Windows.Forms.ProgressBar();
			this.labelStatus = new System.Windows.Forms.Label();
			this.progressBarBuffer = new System.Windows.Forms.ProgressBar();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.lblSpeed = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// buttonStop
			// 
			this.buttonStop.Location = new System.Drawing.Point(342, 9);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.Size = new System.Drawing.Size(80, 24);
			this.buttonStop.TabIndex = 1;
			this.buttonStop.Text = "Stop";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// progressBarOp
			// 
			this.progressBarOp.Location = new System.Drawing.Point(51, 9);
			this.progressBarOp.Name = "progressBarOp";
			this.progressBarOp.Size = new System.Drawing.Size(281, 24);
			this.progressBarOp.Step = 1;
			this.progressBarOp.TabIndex = 2;
			// 
			// labelStatus
			// 
			this.labelStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelStatus.Location = new System.Drawing.Point(51, 42);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(281, 26);
			this.labelStatus.TabIndex = 3;
			this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// progressBarBuffer
			// 
			this.progressBarBuffer.Location = new System.Drawing.Point(162, 77);
			this.progressBarBuffer.Name = "progressBarBuffer";
			this.progressBarBuffer.Size = new System.Drawing.Size(170, 17);
			this.progressBarBuffer.Step = 1;
			this.progressBarBuffer.TabIndex = 4;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(51, 77);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(86, 24);
			this.label1.TabIndex = 5;
			this.label1.Text = "Device Buffer:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(162, 102);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(25, 25);
			this.label2.TabIndex = 6;
			this.label2.Text = "0%";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(298, 102);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(44, 25);
			this.label3.TabIndex = 7;
			this.label3.Text = "100%";
			// 
			// lblSpeed
			// 
			this.lblSpeed.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblSpeed.Location = new System.Drawing.Point(48, 120);
			this.lblSpeed.Name = "lblSpeed";
			this.lblSpeed.Size = new System.Drawing.Size(288, 23);
			this.lblSpeed.TabIndex = 8;
			// 
			// BurningForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(428, 151);
			this.ControlBox = false;
			this.Controls.Add(this.lblSpeed);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.progressBarBuffer);
			this.Controls.Add(this.labelStatus);
			this.Controls.Add(this.progressBarOp);
			this.Controls.Add(this.buttonStop);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BurningForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Working ...";
			this.ResumeLayout(false);

		}
		#endregion

		internal delegate void ShowProgressHandler(object sender, ShowProgressArgs e);

		internal void ShowProgress(object sender, ShowProgressArgs e) 
		{
			// Make sure we're on the right thread
			if( this.InvokeRequired == false ) 
			{
				labelStatus.Text = e.status;
				progressBarOp.Value = e.progressPos;
				progressBarBuffer.Value = e.bufferPos;
				SetActualWriteSpeed(e.nActualWriteSpeed);
				
				e.bStopRequest = stop;

				// Check for completion
				if(e.bDone) 
				{
					if(null != burningDone)
					{
						burningDone();
					}
				}
			}
			else 
			{
				ShowProgressHandler showProgress = new ShowProgressHandler(ShowProgress);
				Invoke(showProgress, new object[] { sender, e});
			}
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			buttonStop.Enabled = false;
			stop = true;
		}

		public bool StopButtonEnabled
		{
			get { return buttonStop.Enabled; }
			set { buttonStop.Enabled = value; }
		}

		void SetActualWriteSpeed(int nSpeed)
		{
			float cdspeed = (float)nSpeed/175.0f;
			float dvdspeed = (float)nSpeed / 1350.0f;
			try
			{
				string sSpeed = string.Format("{0} KB/s (CD: {1}x DVD: {2}x)",nSpeed,cdspeed.ToString("f"),dvdspeed.ToString("f"));
				lblSpeed.Text = sSpeed;
			}
			catch
			{
			}
		}
	}
}
