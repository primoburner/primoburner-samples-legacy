using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace PacketBurnerEx.NET
{
	/// <summary>
	/// Summary description for Progress.
	/// </summary>
	
	public delegate void BurningDoneHandler();


	public class Progress : System.Windows.Forms.Form
	{
		public System.Windows.Forms.Button btnStop;
		public System.Windows.Forms.Label labelStatus;
		public System.Windows.Forms.ProgressBar progressBarBuffer;
		public System.Windows.Forms.ProgressBar progressBarWorking;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		
		public bool StopButtonEnabled
		{
			get { return btnStop.Enabled; }
			set { btnStop.Enabled = value; }
		}

		//Local variable
		bool bStopped;
		public BurningDoneHandler burningDone;

		public Progress()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.ControlBox		= false;
			this.MaximizeBox	= false;
			this.MinimizeBox	= false;
			this.ShowInTaskbar	= false;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.progressBarWorking.Value = 0;

			bStopped = false;
			
			
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
			this.progressBarWorking = new System.Windows.Forms.ProgressBar();
			this.btnStop = new System.Windows.Forms.Button();
			this.labelStatus = new System.Windows.Forms.Label();
			this.progressBarBuffer = new System.Windows.Forms.ProgressBar();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// progressBarWorking
			// 
			this.progressBarWorking.Location = new System.Drawing.Point(66, 19);
			this.progressBarWorking.Name = "progressBarWorking";
			this.progressBarWorking.Size = new System.Drawing.Size(280, 18);
			this.progressBarWorking.TabIndex = 0;
			// 
			// btnStop
			// 
			this.btnStop.Location = new System.Drawing.Point(364, 19);
			this.btnStop.Name = "btnStop";
			this.btnStop.Size = new System.Drawing.Size(104, 27);
			this.btnStop.TabIndex = 1;
			this.btnStop.Text = "Stop";
			this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
			// 
			// labelStatus
			// 
			this.labelStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelStatus.Location = new System.Drawing.Point(66, 46);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(280, 19);
			this.labelStatus.TabIndex = 2;
			this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// progressBarBuffer
			// 
			this.progressBarBuffer.Location = new System.Drawing.Point(159, 75);
			this.progressBarBuffer.Name = "progressBarBuffer";
			this.progressBarBuffer.Size = new System.Drawing.Size(187, 19);
			this.progressBarBuffer.TabIndex = 3;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(309, 103);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(46, 18);
			this.label1.TabIndex = 5;
			this.label1.Text = "100%";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(159, 103);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(28, 18);
			this.label2.TabIndex = 6;
			this.label2.Text = "0%";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(66, 75);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(93, 19);
			this.label3.TabIndex = 7;
			this.label3.Text = "Device Buffer:";
			// 
			// Progress
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(476, 127);
			this.ControlBox = false;
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.label3,
																		  this.label2,
																		  this.label1,
																		  this.progressBarBuffer,
																		  this.labelStatus,
																		  this.btnStop,
																		  this.progressBarWorking});
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Progress";
			this.Text = "Working...";
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
				progressBarWorking.Value = e.progressPos;
				progressBarBuffer.Value = e.bufferPos;
				
				e.bStopRequest = bStopped;

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
				ShowProgressHandler showProgress =
					new ShowProgressHandler(ShowProgress);
				Invoke(showProgress, new object[] { sender, e});
			}
		}

		private void btnStop_Click(object sender, System.EventArgs e)
		{
			btnStop.Enabled = false;
			bStopped = true;
		}

	}
}
