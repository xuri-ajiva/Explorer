namespace Explorer
{
    partial class UserInterface
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserInterface));
            this.explorer1 = new Explorer.ExplorerClass();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.explorer2 = new Explorer.ExplorerClass();
            this.SuspendLayout();
            // 
            // explorer1
            // 
            this.explorer1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("explorer1.BackgroundImage")));
            this.explorer1.Dock = System.Windows.Forms.DockStyle.Left;
            this.explorer1.Location = new System.Drawing.Point(0, 0);
            this.explorer1.Name = "explorer1";
            this.explorer1.Size = new System.Drawing.Size(471, 544);
            this.explorer1.TabIndex = 0;
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(471, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 544);
            this.splitter1.TabIndex = 1;
            this.splitter1.TabStop = false;
            // 
            // explorer2
            // 
            this.explorer2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("explorer2.BackgroundImage")));
            this.explorer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.explorer2.Location = new System.Drawing.Point(474, 0);
            this.explorer2.Name = "explorer2";
            this.explorer2.Size = new System.Drawing.Size(446, 544);
            this.explorer2.TabIndex = 2;
            // 
            // UserInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(920, 544);
            this.Controls.Add(this.explorer2);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.explorer1);
            this.Name = "UserInterface";
            this.Text = "UserInterface";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.UserInterface_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion

        private ExplorerClass explorer1;
        private System.Windows.Forms.Splitter splitter1;
        private ExplorerClass explorer2;
    }
}