using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AudioBurner.NET
{
    public partial class CDTextForm : Form
    {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TextBox txtTitle;
		private System.Windows.Forms.TextBox txtPerformer;
		private System.Windows.Forms.TextBox txtSongWriter;
		private System.Windows.Forms.TextBox txtComposer;
		private System.Windows.Forms.TextBox txtArranger;
		private System.Windows.Forms.TextBox txtMessage;
		private System.Windows.Forms.TextBox txtDiskID;
		private System.Windows.Forms.TextBox txtUPC;
		private System.Windows.Forms.ComboBox comboGenre;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;

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

        public CDTextForm()
        {
            InitializeComponent();

            txtTitle.Text = string.Empty;
            txtPerformer.Text = string.Empty;
            txtSongWriter.Text = string.Empty;
            txtComposer.Text = string.Empty;
            txtArranger.Text = string.Empty;
	        txtMessage.Text = string.Empty;
	        txtUPC.Text = string.Empty;        
	        txtDiskID.Text = string.Empty;

            AddGenres();

            comboGenre.SelectedIndex = 0;
        }

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.txtTitle = new System.Windows.Forms.TextBox();
			this.txtPerformer = new System.Windows.Forms.TextBox();
			this.txtSongWriter = new System.Windows.Forms.TextBox();
			this.txtComposer = new System.Windows.Forms.TextBox();
			this.txtArranger = new System.Windows.Forms.TextBox();
			this.txtMessage = new System.Windows.Forms.TextBox();
			this.txtDiskID = new System.Windows.Forms.TextBox();
			this.txtUPC = new System.Windows.Forms.TextBox();
			this.comboGenre = new System.Windows.Forms.ComboBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(18, 26);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(39, 17);
			this.label1.TabIndex = 0;
			this.label1.Text = "Title:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(18, 61);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(70, 17);
			this.label2.TabIndex = 1;
			this.label2.Text = "Peformer:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(18, 96);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(87, 17);
			this.label3.TabIndex = 2;
			this.label3.Text = "Song Writer:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(18, 131);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(76, 17);
			this.label4.TabIndex = 3;
			this.label4.Text = "Composer:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(18, 271);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(52, 17);
			this.label5.TabIndex = 4;
			this.label5.Text = "Genre:";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(18, 166);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(68, 17);
			this.label6.TabIndex = 5;
			this.label6.Text = "Arranger:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(18, 201);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(69, 17);
			this.label7.TabIndex = 6;
			this.label7.Text = "Message:";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(18, 236);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(56, 17);
			this.label8.TabIndex = 7;
			this.label8.Text = "Disk ID:";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(18, 306);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(75, 17);
			this.label9.TabIndex = 8;
			this.label9.Text = "UPC/ISRC:";
			// 
			// txtTitle
			// 
			this.txtTitle.Location = new System.Drawing.Point(116, 23);
			this.txtTitle.Name = "txtTitle";
			this.txtTitle.Size = new System.Drawing.Size(228, 22);
			this.txtTitle.TabIndex = 9;
			// 
			// txtPerformer
			// 
			this.txtPerformer.Location = new System.Drawing.Point(116, 58);
			this.txtPerformer.Name = "txtPerformer";
			this.txtPerformer.Size = new System.Drawing.Size(228, 22);
			this.txtPerformer.TabIndex = 10;
			// 
			// txtSongWriter
			// 
			this.txtSongWriter.Location = new System.Drawing.Point(116, 93);
			this.txtSongWriter.Name = "txtSongWriter";
			this.txtSongWriter.Size = new System.Drawing.Size(228, 22);
			this.txtSongWriter.TabIndex = 11;
			// 
			// txtComposer
			// 
			this.txtComposer.Location = new System.Drawing.Point(116, 128);
			this.txtComposer.Name = "txtComposer";
			this.txtComposer.Size = new System.Drawing.Size(228, 22);
			this.txtComposer.TabIndex = 12;
			// 
			// txtArranger
			// 
			this.txtArranger.Location = new System.Drawing.Point(116, 163);
			this.txtArranger.Name = "txtArranger";
			this.txtArranger.Size = new System.Drawing.Size(228, 22);
			this.txtArranger.TabIndex = 13;
			// 
			// txtMessage
			// 
			this.txtMessage.Location = new System.Drawing.Point(116, 198);
			this.txtMessage.Name = "txtMessage";
			this.txtMessage.Size = new System.Drawing.Size(228, 22);
			this.txtMessage.TabIndex = 14;
			// 
			// txtDiskID
			// 
			this.txtDiskID.Location = new System.Drawing.Point(116, 233);
			this.txtDiskID.Name = "txtDiskID";
			this.txtDiskID.Size = new System.Drawing.Size(228, 22);
			this.txtDiskID.TabIndex = 15;
			// 
			// txtUPC
			// 
			this.txtUPC.Location = new System.Drawing.Point(116, 303);
			this.txtUPC.Name = "txtUPC";
			this.txtUPC.Size = new System.Drawing.Size(228, 22);
			this.txtUPC.TabIndex = 16;
			// 
			// comboGenre
			// 
			this.comboGenre.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboGenre.FormattingEnabled = true;
			this.comboGenre.Location = new System.Drawing.Point(116, 267);
			this.comboGenre.Name = "comboGenre";
			this.comboGenre.Size = new System.Drawing.Size(228, 24);
			this.comboGenre.TabIndex = 17;
			// 
			// buttonOK
			// 
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(359, 22);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(100, 35);
			this.buttonOK.TabIndex = 18;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(359, 60);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(100, 35);
			this.buttonCancel.TabIndex = 19;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// CDTextForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(468, 342);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.comboGenre);
			this.Controls.Add(this.txtUPC);
			this.Controls.Add(this.txtDiskID);
			this.Controls.Add(this.txtMessage);
			this.Controls.Add(this.txtArranger);
			this.Controls.Add(this.txtComposer);
			this.Controls.Add(this.txtSongWriter);
			this.Controls.Add(this.txtPerformer);
			this.Controls.Add(this.txtTitle);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "CDTextForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "CD-Text Settings";
			this.Load += new System.EventHandler(this.CDTextForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

        public bool EditAlbum;
        
        private void AddGenres()
        {
            comboGenre.Items.Add("Not used");
            comboGenre.Items.Add("Not defined");
            comboGenre.Items.Add("Adult Contemporary");
            comboGenre.Items.Add("Alternative Rock");
            comboGenre.Items.Add("Childrens Music");
            comboGenre.Items.Add("Classical");
            comboGenre.Items.Add("Contemporary Christian");
            comboGenre.Items.Add("Country");
            comboGenre.Items.Add("Dance");
            comboGenre.Items.Add("Easy Listening");
            comboGenre.Items.Add("Erotic");
            comboGenre.Items.Add("Folk");
            comboGenre.Items.Add("Gospel");
            comboGenre.Items.Add("Hip Hop");
            comboGenre.Items.Add("Jazz");
            comboGenre.Items.Add("Latin");
            comboGenre.Items.Add("Musical");
            comboGenre.Items.Add("New Age");
            comboGenre.Items.Add("Opera");
            comboGenre.Items.Add("Operetta");
            comboGenre.Items.Add("Pop Music");
            comboGenre.Items.Add("RAP");
            comboGenre.Items.Add("Reggae");
            comboGenre.Items.Add("Rock Music");
            comboGenre.Items.Add("Rhythm & Blues");
            comboGenre.Items.Add("Sound Effects");
			comboGenre.Items.Add("Soundtrack");
            comboGenre.Items.Add("Spoken Word");
            comboGenre.Items.Add("World Music");
            comboGenre.Items.Add("Reserved");
        }

        public CDTextEntry CDText
        {
            get
            {
                CDTextEntry cdt = new CDTextEntry();
                cdt.Title = txtTitle.Text;
                cdt.Performer = txtPerformer.Text;
                cdt.SongWriter = txtSongWriter.Text;
                cdt.Composer = txtComposer.Text;
                cdt.Arranger = txtArranger.Text;
                cdt.Message = txtMessage.Text;
                cdt.UpcIsrc = txtUPC.Text;
                cdt.DiskId = txtDiskID.Text;
                cdt.Genre = comboGenre.SelectedIndex;
				if (null != comboGenre.SelectedItem)
				{
					cdt.GenreText = comboGenre.GetItemText(comboGenre.SelectedItem);
				}
				else
				{
					cdt.GenreText = string.Empty;
				}

                return cdt;
            }

            set
            {
                txtTitle.Text = value.Title;
                txtPerformer.Text = value.Performer;
                txtSongWriter.Text = value.SongWriter;
                txtComposer.Text = value.Composer;
                txtArranger.Text = value.Arranger;
                txtMessage.Text = value.Message;
                txtUPC.Text = value.UpcIsrc;
                txtDiskID.Text = value.DiskId;

                comboGenre.SelectedIndex = value.Genre;
            }
        }

        private void CDTextForm_Load(object sender, EventArgs e)
        {
            if (!EditAlbum)
            {
                comboGenre.SelectedIndex = 0;
                txtDiskID.Text = string.Empty;
            }

            comboGenre.Enabled = EditAlbum;
            txtDiskID.Enabled = EditAlbum;
        }
    }
}