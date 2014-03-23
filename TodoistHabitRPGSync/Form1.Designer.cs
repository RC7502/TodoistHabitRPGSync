namespace TodoistHabitRPGSync
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
            this.lblStatusBox = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblStatusBox
            // 
            this.lblStatusBox.AutoSize = true;
            this.lblStatusBox.Location = new System.Drawing.Point(25, 38);
            this.lblStatusBox.Name = "lblStatusBox";
            this.lblStatusBox.Size = new System.Drawing.Size(91, 13);
            this.lblStatusBox.TabIndex = 0;
            this.lblStatusBox.Text = "Starting Service...";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(441, 92);
            this.Controls.Add(this.lblStatusBox);
            this.Name = "Form1";
            this.Text = "Todoist to HabitRPG Sync";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblStatusBox;

    }
}

