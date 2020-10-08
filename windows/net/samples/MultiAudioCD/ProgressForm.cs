using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using PrimoSoftware.Burner;


namespace MultiAudioCD
{
	public class ProgressForm: System.Windows.Forms.Form
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

        void UIThread(MethodInvoker code)
        {
            if (InvokeRequired)
            {
                Invoke(code);
                return;
            }

            code.Invoke();
        }

		public void UpdateProgress(ProgressInfo info, int rowIndex) 
		{
            MethodInvoker del = delegate
            {
                while (lvDevices.Items.Count <= rowIndex)
                {
                    ListViewItem lviNewEntry = new ListViewItem();
                    lviNewEntry.SubItems.Add(string.Empty);
                    lviNewEntry.SubItems.Add(string.Empty);
                    lviNewEntry.SubItems.Add(string.Empty);
                    lvDevices.Items.Add(lviNewEntry);
                }

                ListViewItem lvi = lvDevices.Items[rowIndex];

                lvi.SubItems[0].Text = info.DeviceTitle;
                lvi.SubItems[1].Text = info.Status;
                lvi.SubItems[2].Text = info.ProgressStr;
                lvi.SubItems[3].Text = info.WriteSpeed;
            };

            UIThread(del);
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			buttonStop.Enabled = false;
			m_stopped = true;
		}

		private bool m_stopped = false;

		#region Windows Form Designer generated code
        private System.Windows.Forms.Button buttonStop;
        private ListView lvDevices;
        private ColumnHeader columnHeaderDevice;
        private ColumnHeader columnHeaderStatus;
        private ColumnHeader columnHeaderProgress;
        private ColumnHeader columnHeaderWriteSpeed;

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
            this.lvDevices = new System.Windows.Forms.ListView();
            this.columnHeaderDevice = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderProgress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderWriteSpeed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(687, 169);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(71, 20);
            this.buttonStop.TabIndex = 1;
            this.buttonStop.Text = "Stop";
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // lvDevices
            // 
            this.lvDevices.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderDevice,
            this.columnHeaderStatus,
            this.columnHeaderProgress,
            this.columnHeaderWriteSpeed});
            this.lvDevices.FullRowSelect = true;
            this.lvDevices.GridLines = true;
            this.lvDevices.Location = new System.Drawing.Point(7, 5);
            this.lvDevices.Name = "lvDevices";
            this.lvDevices.Size = new System.Drawing.Size(757, 148);
            this.lvDevices.TabIndex = 2;
            this.lvDevices.UseCompatibleStateImageBehavior = false;
            this.lvDevices.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderDevice
            // 
            this.columnHeaderDevice.Text = "Device";
            this.columnHeaderDevice.Width = 363;
            // 
            // columnHeaderStatus
            // 
            this.columnHeaderStatus.Text = "Status";
            this.columnHeaderStatus.Width = 218;
            // 
            // columnHeaderProgress
            // 
            this.columnHeaderProgress.Text = "Progress";
            this.columnHeaderProgress.Width = 77;
            // 
            // columnHeaderWriteSpeed
            // 
            this.columnHeaderWriteSpeed.Text = "Write Speed";
            this.columnHeaderWriteSpeed.Width = 88;
            // 
            // ProgressForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(770, 203);
            this.ControlBox = false;
            this.Controls.Add(this.lvDevices);
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
