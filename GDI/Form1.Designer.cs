namespace LuaEmuPlayerGDI
{
    partial class LuaEmuPlayerForm
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
            this.framePictureBox = new System.Windows.Forms.PictureBox();
            this.statusLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.framePictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // framePictureBox
            // 
            this.framePictureBox.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.framePictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.framePictureBox.Location = new System.Drawing.Point(0, 0);
            this.framePictureBox.Name = "framePictureBox";
            this.framePictureBox.Size = new System.Drawing.Size(319, 761);
            this.framePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.framePictureBox.TabIndex = 0;
            this.framePictureBox.TabStop = false;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.BackColor = System.Drawing.Color.Black;
            this.statusLabel.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.statusLabel.Location = new System.Drawing.Point(13, 13);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(61, 13);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Initializing...";
            // 
            // LuaEmuPlayerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(319, 761);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.framePictureBox);
            this.Name = "LuaEmuPlayerForm";
            this.Text = "LuaEmuPlayer";
            this.Load += new System.EventHandler(this.LuaEmuPlayerForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.framePictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox framePictureBox;
        private System.Windows.Forms.Label statusLabel;
    }
}

