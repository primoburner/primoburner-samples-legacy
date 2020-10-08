using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using PrimoSoftware.Burner;

namespace AudioBurner.NET
{
	public delegate void BurningDoneHandler();

	public class ProgressForm : System.Windows.Forms.Form
	{
		public ProgressForm()
		{
			InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
				if(components != null)
					components.Dispose();

			base.Dispose(disposing);
		}

		public bool Stopped 
		{ 
			get 
			{ 
				return m_stopped; 
			} 
		}

		public bool EnableStop 
		{ 
			set 
			{ 
				buttonStop.Enabled = value; 
			}
		}

		public string Status 
		{ 
			set 
			{ 
				labelStatus.Text = value; 
			} 
		}

		public void UpdateProgress(ProgressInfo info) 
		{
			if (InvokeRequired) 
			{
				// Call the same method from the user thread using a delegate
				UpdateProgressThread thread = new UpdateProgressThread(UpdateProgress);
				Invoke(thread, new object[] {info});
				return;
			}

			// This code executes from the user interface thread
			labelStatus.Text			= info.Message;

			progressBarProgress.Value	= info.Percent;
			progressBarInternalBuffer.Value = info.UsedCachePercent;

			SetActualWriteSpeed(info.ActualWriteSpeed);
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			buttonStop.Enabled = false;
			m_stopped = true;
		}

		private void SetActualWriteSpeed(int speed)
		{
			double cdSpeed = (double)speed / Speed1xKB.CD;
			try
			{
				labelSpeed.Text = string.Format("{0} KB/s (CD: {1:#0.00}x)", speed, cdSpeed);
			}
			catch
			{
			}
		}

		private delegate void UpdateProgressThread(ProgressInfo info);
		private bool m_stopped = false;

		#region Windows Form Designer generated code
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.Label labelStatus;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label labelSpeed;
		private System.Windows.Forms.ProgressBar progressBarProgress;
		private System.Windows.Forms.ProgressBar progressBarInternalBuffer;

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
			this.buttonStop = new System.Windows.Forms.Button();
			this.progressBarProgress = new System.Windows.Forms.ProgressBar();
			this.labelStatus = new System.Windows.Forms.Label();
			this.progressBarInternalBuffer = new System.Windows.Forms.ProgressBar();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.labelSpeed = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// buttonStop
			// 
			this.buttonStop.Location = new System.Drawing.Point(336, 9);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.Size = new System.Drawing.Size(71, 20);
			this.buttonStop.TabIndex = 1;
			this.buttonStop.Text = "Stop";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// progressBarProgress
			// 
			this.progressBarProgress.Location = new System.Drawing.Point(8, 9);
			this.progressBarProgress.Name = "progressBarProgress";
			this.progressBarProgress.Size = new System.Drawing.Size(320, 20);
			this.progressBarProgress.Step = 1;
			this.progressBarProgress.TabIndex = 2;
			// 
			// labelStatus
			// 
			this.labelStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelStatus.Location = new System.Drawing.Point(8, 32);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(320, 20);
			this.labelStatus.TabIndex = 3;
			this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// progressBarInternalBuffer
			// 
			this.progressBarInternalBuffer.Location = new System.Drawing.Point(120, 64);
			this.progressBarInternalBuffer.Name = "progressBarInternalBuffer";
			this.progressBarInternalBuffer.Size = new System.Drawing.Size(208, 20);
			this.progressBarInternalBuffer.Step = 1;
			this.progressBarInternalBuffer.TabIndex = 4;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(24, 64);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(83, 20);
			this.label1.TabIndex = 5;
			this.label1.Text = "Device Buffer:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(120, 88);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(25, 20);
			this.label2.TabIndex = 6;
			this.label2.Text = "0%";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(296, 88);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(35, 20);
			this.label3.TabIndex = 7;
			this.label3.Text = "100%";
			// 
			// labelSpeed
			// 
			this.labelSpeed.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelSpeed.Location = new System.Drawing.Point(8, 112);
			this.labelSpeed.Name = "labelSpeed";
			this.labelSpeed.Size = new System.Drawing.Size(320, 20);
			this.labelSpeed.TabIndex = 8;
			this.labelSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ProgressForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(418, 143);
			this.ControlBox = false;
			this.Controls.Add(this.labelSpeed);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.progressBarInternalBuffer);
			this.Controls.Add(this.labelStatus);
			this.Controls.Add(this.progressBarProgress);
			this.Controls.Add(this.buttonStop);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ProgressForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Working ...";
			this.ResumeLayout(false);

		}
		#endregion

	}
}
