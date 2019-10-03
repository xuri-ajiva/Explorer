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
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.LocalExplorer = new System.Windows.Forms.GroupBox();
            this.explorerClass2 = new Explorer.ExplorerClass();
            this.RemoteExplorrer = new System.Windows.Forms.GroupBox();
            this.explorerClass1 = new Explorer.ExplorerClass();
            this.LocalExplorer.SuspendLayout();
            this.RemoteExplorrer.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(652, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(10, 544);
            this.splitter1.TabIndex = 1;
            this.splitter1.TabStop = false;
            // 
            // LocalExplorer
            // 
            this.LocalExplorer.Controls.Add(this.explorerClass2);
            this.LocalExplorer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LocalExplorer.Location = new System.Drawing.Point(662, 0);
            this.LocalExplorer.Name = "LocalExplorer";
            this.LocalExplorer.Size = new System.Drawing.Size(668, 544);
            this.LocalExplorer.TabIndex = 3;
            this.LocalExplorer.TabStop = false;
            this.LocalExplorer.Text = "LocalExplorer";
            // 
            // explorerClass2
            // 
            this.explorerClass2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("explorerClass2.BackgroundImage")));
            this.explorerClass2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.explorerClass2.Location = new System.Drawing.Point(3, 16);
            this.explorerClass2.Name = "explorerClass2";
            this.explorerClass2.Size = new System.Drawing.Size(662, 525);
            this.explorerClass2.TabIndex = 3;
            this.explorerClass2.Load += new System.EventHandler(this.explorerClass2_Load);
            // 
            // RemoteExplorrer
            // 
            this.RemoteExplorrer.Controls.Add(this.explorerClass1);
            this.RemoteExplorrer.Dock = System.Windows.Forms.DockStyle.Left;
            this.RemoteExplorrer.Location = new System.Drawing.Point(0, 0);
            this.RemoteExplorrer.Name = "RemoteExplorrer";
            this.RemoteExplorrer.Size = new System.Drawing.Size(652, 544);
            this.RemoteExplorrer.TabIndex = 4;
            this.RemoteExplorrer.TabStop = false;
            this.RemoteExplorrer.Text = "RemoteExplorrer";
            // 
            // explorerClass1
            // 
            this.explorerClass1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("explorerClass1.BackgroundImage")));
            this.explorerClass1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.explorerClass1.Location = new System.Drawing.Point(3, 16);
            this.explorerClass1.Name = "explorerClass1";
            this.explorerClass1.Size = new System.Drawing.Size(646, 525);
            this.explorerClass1.TabIndex = 2;
            this.explorerClass1.Load += new System.EventHandler(this.explorerClass1_Load);
            // 
            // UserInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1330, 544);
            this.Controls.Add(this.LocalExplorer);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.RemoteExplorrer);
            this.Name = "UserInterface";
            this.Text = "UserInterface";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.UserInterface_FormClosed);
            this.LocalExplorer.ResumeLayout(false);
            this.RemoteExplorrer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.GroupBox LocalExplorer;
        private System.Windows.Forms.GroupBox RemoteExplorrer;
        private ExplorerClass explorerClass1;
        private ExplorerClass explorerClass2;
    }
}