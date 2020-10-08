using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MultiDataDisc
{
    public partial class WriteSpeedsForm : Form
    {
        public WriteSpeedsForm()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            _selectedWriteSpeed = cbWriteSpeed.SelectedItem as SpeedInfo;
            DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        public string WriterTitle
        {
            get
            {
                return lblWriter.Text;
            }

            set
            {
                lblWriter.Text = value;
            }
        }

        List<SpeedInfo> _writeSpeeds;
        public List<SpeedInfo> WriteSpeeds
        {
            get { return _writeSpeeds; }
            set { _writeSpeeds = value; UpdateWriteSpeeds(); } 
        }

        SpeedInfo _selectedWriteSpeed;
        public SpeedInfo SelectedWriteSpeed 
        {
            get { return _selectedWriteSpeed; }
            set { _selectedWriteSpeed = value; UpdateWriteSpeeds();  } 
        }

        void UpdateWriteSpeeds()
        {
            cbWriteSpeed.Items.Clear();

            if (_writeSpeeds != null)
            {
                for (int i = 0; i < _writeSpeeds.Count; i++)
                    cbWriteSpeed.Items.Add(_writeSpeeds[i]);
            }

            if (_selectedWriteSpeed != null)
            {
                if (cbWriteSpeed.Items.Count > 0)
                {
                    cbWriteSpeed.SelectedIndex = cbWriteSpeed.FindString(_selectedWriteSpeed.ToString());

                    if (-1 == cbWriteSpeed.SelectedIndex)
                        cbWriteSpeed.SelectedIndex = 0;
                }
            }
        }
    }
}
