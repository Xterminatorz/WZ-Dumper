namespace WzDumper {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
            if (CancelSource != null)
                CancelSource.Dispose();
            base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            this.SelectWzFileButton = new System.Windows.Forms.Button();
            this.DumpWzButton = new System.Windows.Forms.Button();
            this.Info = new System.Windows.Forms.TextBox();
            this.includePngMp3Box = new System.Windows.Forms.CheckBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.CancelOpButton = new System.Windows.Forms.Button();
            this.versionBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.MapleVersionComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.WZFileTB = new System.Windows.Forms.TextBox();
            this.outputFolderTB = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.openFolderButton = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.includeVersionInFolderBox = new System.Windows.Forms.CheckBox();
            this.multiThreadCheckBox = new System.Windows.Forms.CheckBox();
            this.extractorThreadsNum = new System.Windows.Forms.NumericUpDown();
            this.extractorThreadsLabel = new System.Windows.Forms.Label();
            this.SelectWzFolder = new System.Windows.Forms.Button();
            this.SelectExtractDestination = new System.Windows.Forms.Button();
            this.LinkTypeLabel = new System.Windows.Forms.Label();
            this.LinkTypeComboBox = new System.Windows.Forms.ComboBox();
            this.toolStripStatusLabel1 = new WzDumper.SafeToolStripLabel();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.extractorThreadsNum)).BeginInit();
            this.SuspendLayout();
            // 
            // SelectWzFileButton
            // 
            this.SelectWzFileButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.SelectWzFileButton.Location = new System.Drawing.Point(537, 294);
            this.SelectWzFileButton.Name = "SelectWzFileButton";
            this.SelectWzFileButton.Size = new System.Drawing.Size(67, 23);
            this.SelectWzFileButton.TabIndex = 0;
            this.SelectWzFileButton.Text = "Select File";
            this.SelectWzFileButton.UseVisualStyleBackColor = true;
            this.SelectWzFileButton.Click += new System.EventHandler(this.SelectWzFile);
            // 
            // DumpWzButton
            // 
            this.DumpWzButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.DumpWzButton.Enabled = false;
            this.DumpWzButton.Location = new System.Drawing.Point(537, 354);
            this.DumpWzButton.Name = "DumpWzButton";
            this.DumpWzButton.Size = new System.Drawing.Size(72, 23);
            this.DumpWzButton.TabIndex = 5;
            this.DumpWzButton.Text = "Dump";
            this.DumpWzButton.UseVisualStyleBackColor = true;
            this.DumpWzButton.Click += new System.EventHandler(this.DumpFile);
            // 
            // Info
            // 
            this.Info.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Info.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Info.Location = new System.Drawing.Point(12, 29);
            this.Info.Multiline = true;
            this.Info.Name = "Info";
            this.Info.ReadOnly = true;
            this.Info.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.Info.Size = new System.Drawing.Size(680, 259);
            this.Info.TabIndex = 9;
            this.Info.TabStop = false;
            // 
            // includePngMp3Box
            // 
            this.includePngMp3Box.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.includePngMp3Box.AutoSize = true;
            this.includePngMp3Box.Location = new System.Drawing.Point(12, 387);
            this.includePngMp3Box.Name = "includePngMp3Box";
            this.includePngMp3Box.Size = new System.Drawing.Size(149, 17);
            this.includePngMp3Box.TabIndex = 3;
            this.includePngMp3Box.Text = "Include Images and MP3s";
            this.includePngMp3Box.UseVisualStyleBackColor = true;
            this.includePngMp3Box.CheckedChanged += new System.EventHandler(this.IncludePngMp3Box_CheckedChanged);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 439);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(704, 22);
            this.statusStrip1.TabIndex = 17;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // CancelOpButton
            // 
            this.CancelOpButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.CancelOpButton.Enabled = false;
            this.CancelOpButton.Location = new System.Drawing.Point(615, 354);
            this.CancelOpButton.Name = "CancelOpButton";
            this.CancelOpButton.Size = new System.Drawing.Size(60, 23);
            this.CancelOpButton.TabIndex = 7;
            this.CancelOpButton.Text = "Cancel";
            this.CancelOpButton.UseVisualStyleBackColor = true;
            this.CancelOpButton.Click += new System.EventHandler(this.CancelOperation);
            // 
            // versionBox
            // 
            this.versionBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.versionBox.Enabled = false;
            this.versionBox.Location = new System.Drawing.Point(345, 355);
            this.versionBox.MaxLength = 5;
            this.versionBox.Name = "versionBox";
            this.versionBox.ReadOnly = true;
            this.versionBox.Size = new System.Drawing.Size(42, 20);
            this.versionBox.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label2.Location = new System.Drawing.Point(9, 360);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "Encryption:";
            // 
            // MapleVersionComboBox
            // 
            this.MapleVersionComboBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.MapleVersionComboBox.FormattingEnabled = true;
            this.MapleVersionComboBox.Items.AddRange(new object[] {
            "None",
            "GMS (v0.56-0.116)",
            "EMS/MSEA"});
            this.MapleVersionComboBox.Location = new System.Drawing.Point(76, 356);
            this.MapleVersionComboBox.Name = "MapleVersionComboBox";
            this.MapleVersionComboBox.Size = new System.Drawing.Size(122, 21);
            this.MapleVersionComboBox.TabIndex = 1;
            this.MapleVersionComboBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MapleVersionComboBoxKeyPress);
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label3.Location = new System.Drawing.Point(228, 360);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(111, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Detected File Version:";
            // 
            // WZFileTB
            // 
            this.WZFileTB.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.WZFileTB.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.WZFileTB.Location = new System.Drawing.Point(76, 295);
            this.WZFileTB.Name = "WZFileTB";
            this.WZFileTB.ReadOnly = true;
            this.WZFileTB.Size = new System.Drawing.Size(455, 20);
            this.WZFileTB.TabIndex = 10;
            // 
            // outputFolderTB
            // 
            this.outputFolderTB.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.outputFolderTB.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.outputFolderTB.Location = new System.Drawing.Point(76, 325);
            this.outputFolderTB.Name = "outputFolderTB";
            this.outputFolderTB.ReadOnly = true;
            this.outputFolderTB.Size = new System.Drawing.Size(455, 20);
            this.outputFolderTB.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 298);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "File/Folder:";
            // 
            // label5
            // 
            this.label5.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(27, 328);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(42, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "Output:";
            // 
            // openFolderButton
            // 
            this.openFolderButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.openFolderButton.Enabled = false;
            this.openFolderButton.Location = new System.Drawing.Point(621, 324);
            this.openFolderButton.Name = "openFolderButton";
            this.openFolderButton.Size = new System.Drawing.Size(53, 23);
            this.openFolderButton.TabIndex = 12;
            this.openFolderButton.Text = "Open";
            this.openFolderButton.UseVisualStyleBackColor = true;
            this.openFolderButton.Click += new System.EventHandler(this.OpenFolder);
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menuStrip1.Size = new System.Drawing.Size(704, 24);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearInfoToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // clearInfoToolStripMenuItem
            // 
            this.clearInfoToolStripMenuItem.Name = "clearInfoToolStripMenuItem";
            this.clearInfoToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.clearInfoToolStripMenuItem.Text = "Clear Info";
            this.clearInfoToolStripMenuItem.Click += new System.EventHandler(this.ClearInfoToolStripMenuItemClick);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItemClick);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem1});
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // aboutToolStripMenuItem1
            // 
            this.aboutToolStripMenuItem1.Name = "aboutToolStripMenuItem1";
            this.aboutToolStripMenuItem1.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem1.Text = "About";
            this.aboutToolStripMenuItem1.Click += new System.EventHandler(this.AboutToolStripMenuItem1Click);
            // 
            // includeVersionInFolderBox
            // 
            this.includeVersionInFolderBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.includeVersionInFolderBox.AutoSize = true;
            this.includeVersionInFolderBox.Location = new System.Drawing.Point(336, 387);
            this.includeVersionInFolderBox.Name = "includeVersionInFolderBox";
            this.includeVersionInFolderBox.Size = new System.Drawing.Size(195, 17);
            this.includeVersionInFolderBox.TabIndex = 4;
            this.includeVersionInFolderBox.Text = "Append File Version to Folder Name";
            this.includeVersionInFolderBox.UseVisualStyleBackColor = true;
            // 
            // multiThreadCheckBox
            // 
            this.multiThreadCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.multiThreadCheckBox.AutoSize = true;
            this.multiThreadCheckBox.Location = new System.Drawing.Point(12, 416);
            this.multiThreadCheckBox.Name = "multiThreadCheckBox";
            this.multiThreadCheckBox.Size = new System.Drawing.Size(151, 17);
            this.multiThreadCheckBox.TabIndex = 19;
            this.multiThreadCheckBox.Text = "Dump Files Simultaneously";
            this.multiThreadCheckBox.UseVisualStyleBackColor = true;
            this.multiThreadCheckBox.CheckedChanged += new System.EventHandler(this.MultiThreadCheckBox_CheckedChanged);
            // 
            // extractorThreadsNum
            // 
            this.extractorThreadsNum.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.extractorThreadsNum.Enabled = false;
            this.extractorThreadsNum.Location = new System.Drawing.Point(246, 413);
            this.extractorThreadsNum.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.extractorThreadsNum.Name = "extractorThreadsNum";
            this.extractorThreadsNum.Size = new System.Drawing.Size(39, 20);
            this.extractorThreadsNum.TabIndex = 25;
            this.extractorThreadsNum.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // extractorThreadsLabel
            // 
            this.extractorThreadsLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.extractorThreadsLabel.AutoSize = true;
            this.extractorThreadsLabel.Enabled = false;
            this.extractorThreadsLabel.Location = new System.Drawing.Point(168, 417);
            this.extractorThreadsLabel.Name = "extractorThreadsLabel";
            this.extractorThreadsLabel.Size = new System.Drawing.Size(72, 13);
            this.extractorThreadsLabel.TabIndex = 20;
            this.extractorThreadsLabel.Text = "Max Threads:";
            // 
            // SelectWzFolder
            // 
            this.SelectWzFolder.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.SelectWzFolder.Location = new System.Drawing.Point(610, 294);
            this.SelectWzFolder.Name = "SelectWzFolder";
            this.SelectWzFolder.Size = new System.Drawing.Size(82, 23);
            this.SelectWzFolder.TabIndex = 21;
            this.SelectWzFolder.Text = "Select Folder";
            this.SelectWzFolder.UseVisualStyleBackColor = true;
            this.SelectWzFolder.Click += new System.EventHandler(this.SelectWzFolder_Click);
            // 
            // SelectExtractDestination
            // 
            this.SelectExtractDestination.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.SelectExtractDestination.Location = new System.Drawing.Point(537, 324);
            this.SelectExtractDestination.Name = "SelectExtractDestination";
            this.SelectExtractDestination.Size = new System.Drawing.Size(78, 23);
            this.SelectExtractDestination.TabIndex = 22;
            this.SelectExtractDestination.Text = "Select Folder";
            this.SelectExtractDestination.UseVisualStyleBackColor = true;
            this.SelectExtractDestination.Click += new System.EventHandler(this.SelectExtractDestination_Click);
            // 
            // LinkTypeLabel
            // 
            this.LinkTypeLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.LinkTypeLabel.AutoSize = true;
            this.LinkTypeLabel.Location = new System.Drawing.Point(168, 388);
            this.LinkTypeLabel.Name = "LinkTypeLabel";
            this.LinkTypeLabel.Size = new System.Drawing.Size(57, 13);
            this.LinkTypeLabel.TabIndex = 23;
            this.LinkTypeLabel.Text = "Link Type:";
            // 
            // LinkTypeComboBox
            // 
            this.LinkTypeComboBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.LinkTypeComboBox.Enabled = false;
            this.LinkTypeComboBox.FormattingEnabled = true;
            this.LinkTypeComboBox.Location = new System.Drawing.Point(231, 384);
            this.LinkTypeComboBox.Name = "LinkTypeComboBox";
            this.LinkTypeComboBox.Size = new System.Drawing.Size(82, 21);
            this.LinkTypeComboBox.TabIndex = 24;
            this.LinkTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.LinkTypeComboBox_SelectedIndexChanged);
            this.LinkTypeComboBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LinkTypeComboBox_KeyPress);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(704, 461);
            this.Controls.Add(this.extractorThreadsNum);
            this.Controls.Add(this.extractorThreadsLabel);
            this.Controls.Add(this.LinkTypeComboBox);
            this.Controls.Add(this.multiThreadCheckBox);
            this.Controls.Add(this.LinkTypeLabel);
            this.Controls.Add(this.SelectExtractDestination);
            this.Controls.Add(this.SelectWzFolder);
            this.Controls.Add(this.includeVersionInFolderBox);
            this.Controls.Add(this.openFolderButton);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.outputFolderTB);
            this.Controls.Add(this.WZFileTB);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.MapleVersionComboBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.versionBox);
            this.Controls.Add(this.CancelOpButton);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.includePngMp3Box);
            this.Controls.Add(this.Info);
            this.Controls.Add(this.DumpWzButton);
            this.Controls.Add(this.SelectWzFileButton);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(720, 500);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WZ Dumper";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1FormClosing);
            this.Load += new System.EventHandler(this.Form1Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.extractorThreadsNum)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button SelectWzFileButton;
		private System.Windows.Forms.Button DumpWzButton;
		private System.Windows.Forms.TextBox Info;
		private System.Windows.Forms.CheckBox includePngMp3Box;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.Button CancelOpButton;
		private SafeToolStripLabel toolStripStatusLabel1;
		private System.Windows.Forms.TextBox versionBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox MapleVersionComboBox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox WZFileTB;
		private System.Windows.Forms.TextBox outputFolderTB;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button openFolderButton;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.CheckBox includeVersionInFolderBox;
		private System.Windows.Forms.ToolStripMenuItem clearInfoToolStripMenuItem;
        private System.Windows.Forms.CheckBox multiThreadCheckBox;
        private System.Windows.Forms.Label extractorThreadsLabel;
        private System.Windows.Forms.NumericUpDown extractorThreadsNum;
        private System.Windows.Forms.Button SelectWzFolder;
        private System.Windows.Forms.Button SelectExtractDestination;
        private System.Windows.Forms.Label LinkTypeLabel;
        private System.Windows.Forms.ComboBox LinkTypeComboBox;
    }
}