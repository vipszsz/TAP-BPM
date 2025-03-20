using System.Drawing;
using System.Windows.Forms;

namespace TapBPMApp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelBPM = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.closeApp = new System.Windows.Forms.PictureBox();
            this.pictureBoxReset = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.closeApp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxReset)).BeginInit();
            this.SuspendLayout();
            // 
            // labelBPM
            // 
            this.labelBPM.BackColor = System.Drawing.Color.Transparent;
            this.labelBPM.Font = new System.Drawing.Font("IBM Plex Mono", 71.99999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBPM.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelBPM.Location = new System.Drawing.Point(4, 78);
            this.labelBPM.Name = "labelBPM";
            this.labelBPM.Size = new System.Drawing.Size(230, 120);
            this.labelBPM.TabIndex = 1;
            this.labelBPM.Text = "0";
            this.labelBPM.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = global::TAP_BPM.Properties.Resources.VIPZ_TAP_BPM_LOGO1;
            this.pictureBox1.Location = new System.Drawing.Point(8, 22);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(186, 69);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 9;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // closeApp
            // 
            this.closeApp.BackColor = System.Drawing.Color.Transparent;
            this.closeApp.Image = global::TAP_BPM.Properties.Resources.x_copy;
            this.closeApp.Location = new System.Drawing.Point(245, 29);
            this.closeApp.Name = "closeApp";
            this.closeApp.Size = new System.Drawing.Size(25, 25);
            this.closeApp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.closeApp.TabIndex = 7;
            this.closeApp.TabStop = false;
            this.closeApp.Click += new System.EventHandler(this.closeApp_Click);
            // 
            // pictureBoxReset
            // 
            this.pictureBoxReset.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxReset.Image = global::TAP_BPM.Properties.Resources.undo_copy;
            this.pictureBoxReset.Location = new System.Drawing.Point(245, 65);
            this.pictureBoxReset.Name = "pictureBoxReset";
            this.pictureBoxReset.Size = new System.Drawing.Size(25, 25);
            this.pictureBoxReset.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxReset.TabIndex = 6;
            this.pictureBoxReset.TabStop = false;
            this.pictureBoxReset.Click += new System.EventHandler(this.pictureBoxReset_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ClientSize = new System.Drawing.Size(300, 207);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.closeApp);
            this.Controls.Add(this.pictureBoxReset);
            this.Controls.Add(this.labelBPM);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Form1";
            this.Text = "Tap BPM";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.closeApp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxReset)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label labelBPM;
        private PictureBox pictureBoxReset;
        private PictureBox closeApp;
        private PictureBox pictureBox1;
    }
}

