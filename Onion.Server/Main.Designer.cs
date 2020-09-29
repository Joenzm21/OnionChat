namespace Onion.Server
{
    partial class Main
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
            this.components = new System.ComponentModel.Container();
            this.startastopb = new System.Windows.Forms.Button();
            this.portnum = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.peerCountText = new System.Windows.Forms.ToolStripStatusLabel();
            this.userCountText = new System.Windows.Forms.ToolStripStatusLabel();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.teToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addresstb = new System.Windows.Forms.TextBox();
            this.connectb = new System.Windows.Forms.Button();
            this.userg = new System.Windows.Forms.GroupBox();
            this.userslistv = new System.Windows.Forms.ListView();
            this.index1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.name1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.key1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.upnp = new System.Windows.Forms.CheckBox();
            this.localserver = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.peerg = new System.Windows.Forms.GroupBox();
            this.peerslistv = new System.Windows.Forms.ListView();
            this.index2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.iep2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.key2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ipaddr = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.portnum)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.userg.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.peerg.SuspendLayout();
            this.SuspendLayout();
            // 
            // startastopb
            // 
            this.startastopb.Location = new System.Drawing.Point(200, 19);
            this.startastopb.Name = "startastopb";
            this.startastopb.Size = new System.Drawing.Size(75, 23);
            this.startastopb.TabIndex = 2;
            this.startastopb.Text = "Start";
            this.startastopb.UseVisualStyleBackColor = true;
            this.startastopb.Click += new System.EventHandler(this.startastopb_Click);
            // 
            // portnum
            // 
            this.portnum.Location = new System.Drawing.Point(41, 21);
            this.portnum.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.portnum.Name = "portnum";
            this.portnum.Size = new System.Drawing.Size(59, 20);
            this.portnum.TabIndex = 3;
            this.portnum.Value = new decimal(new int[] {
            8080,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Port:";
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.peerCountText,
            this.userCountText,
            this.ipaddr});
            this.statusStrip.Location = new System.Drawing.Point(0, 406);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(601, 22);
            this.statusStrip.TabIndex = 6;
            this.statusStrip.Text = "statusStrip1";
            // 
            // peerCountText
            // 
            this.peerCountText.Name = "peerCountText";
            this.peerCountText.Size = new System.Drawing.Size(47, 17);
            this.peerCountText.Text = "Peers: 0";
            // 
            // userCountText
            // 
            this.userCountText.Name = "userCountText";
            this.userCountText.Size = new System.Drawing.Size(47, 17);
            this.userCountText.Text = "Users: 0";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.teToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(85, 26);
            // 
            // teToolStripMenuItem
            // 
            this.teToolStripMenuItem.Name = "teToolStripMenuItem";
            this.teToolStripMenuItem.Size = new System.Drawing.Size(84, 22);
            this.teToolStripMenuItem.Text = "te";
            // 
            // addresstb
            // 
            this.addresstb.Location = new System.Drawing.Point(327, 21);
            this.addresstb.Name = "addresstb";
            this.addresstb.Size = new System.Drawing.Size(163, 20);
            this.addresstb.TabIndex = 8;
            // 
            // connectb
            // 
            this.connectb.Location = new System.Drawing.Point(494, 19);
            this.connectb.Name = "connectb";
            this.connectb.Size = new System.Drawing.Size(75, 23);
            this.connectb.TabIndex = 9;
            this.connectb.Text = "Connect";
            this.connectb.UseVisualStyleBackColor = true;
            this.connectb.Click += new System.EventHandler(this.connectb_Click);
            // 
            // userg
            // 
            this.userg.Controls.Add(this.userslistv);
            this.userg.Location = new System.Drawing.Point(12, 78);
            this.userg.Name = "userg";
            this.userg.Size = new System.Drawing.Size(579, 152);
            this.userg.TabIndex = 15;
            this.userg.TabStop = false;
            this.userg.Text = "Users";
            // 
            // userslistv
            // 
            this.userslistv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.index1,
            this.name1,
            this.key1});
            this.userslistv.Cursor = System.Windows.Forms.Cursors.Default;
            this.userslistv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userslistv.GridLines = true;
            this.userslistv.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.userslistv.HideSelection = false;
            this.userslistv.LabelEdit = true;
            this.userslistv.Location = new System.Drawing.Point(3, 16);
            this.userslistv.MultiSelect = false;
            this.userslistv.Name = "userslistv";
            this.userslistv.Size = new System.Drawing.Size(573, 133);
            this.userslistv.TabIndex = 13;
            this.userslistv.UseCompatibleStateImageBehavior = false;
            this.userslistv.View = System.Windows.Forms.View.Details;
            // 
            // index1
            // 
            this.index1.Text = "Num";
            this.index1.Width = 0;
            // 
            // name1
            // 
            this.name1.Text = "Name";
            this.name1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.name1.Width = 116;
            // 
            // key1
            // 
            this.key1.Text = "Public Key";
            this.key1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.key1.Width = 450;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.upnp);
            this.groupBox3.Controls.Add(this.localserver);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.portnum);
            this.groupBox3.Controls.Add(this.startastopb);
            this.groupBox3.Controls.Add(this.connectb);
            this.groupBox3.Controls.Add(this.addresstb);
            this.groupBox3.Location = new System.Drawing.Point(12, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(579, 60);
            this.groupBox3.TabIndex = 15;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Options";
            // 
            // upnp
            // 
            this.upnp.AutoSize = true;
            this.upnp.Enabled = false;
            this.upnp.Location = new System.Drawing.Point(106, 37);
            this.upnp.Name = "upnp";
            this.upnp.Size = new System.Drawing.Size(95, 17);
            this.upnp.TabIndex = 15;
            this.upnp.Text = "NAT Traversal";
            this.upnp.UseVisualStyleBackColor = true;
            // 
            // localserver
            // 
            this.localserver.AutoSize = true;
            this.localserver.Checked = true;
            this.localserver.CheckState = System.Windows.Forms.CheckState.Checked;
            this.localserver.Location = new System.Drawing.Point(106, 15);
            this.localserver.Name = "localserver";
            this.localserver.Size = new System.Drawing.Size(86, 17);
            this.localserver.TabIndex = 14;
            this.localserver.Text = "Local Server";
            this.localserver.UseVisualStyleBackColor = true;
            this.localserver.CheckedChanged += new System.EventHandler(this.localserver_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(279, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Address:";
            // 
            // peerg
            // 
            this.peerg.Controls.Add(this.peerslistv);
            this.peerg.Location = new System.Drawing.Point(12, 233);
            this.peerg.Name = "peerg";
            this.peerg.Size = new System.Drawing.Size(579, 152);
            this.peerg.TabIndex = 16;
            this.peerg.TabStop = false;
            this.peerg.Text = "Peers";
            // 
            // peerslistv
            // 
            this.peerslistv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.index2,
            this.iep2,
            this.key2});
            this.peerslistv.Cursor = System.Windows.Forms.Cursors.Default;
            this.peerslistv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.peerslistv.GridLines = true;
            this.peerslistv.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.peerslistv.HideSelection = false;
            this.peerslistv.LabelEdit = true;
            this.peerslistv.Location = new System.Drawing.Point(3, 16);
            this.peerslistv.MultiSelect = false;
            this.peerslistv.Name = "peerslistv";
            this.peerslistv.Size = new System.Drawing.Size(573, 133);
            this.peerslistv.TabIndex = 13;
            this.peerslistv.UseCompatibleStateImageBehavior = false;
            this.peerslistv.View = System.Windows.Forms.View.Details;
            // 
            // index2
            // 
            this.index2.Text = "Num";
            this.index2.Width = 0;
            // 
            // iep2
            // 
            this.iep2.Text = "IP End Point";
            this.iep2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.iep2.Width = 119;
            // 
            // key2
            // 
            this.key2.Text = "Public Key";
            this.key2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.key2.Width = 449;
            // 
            // ipaddr
            // 
            this.ipaddr.Name = "ipaddr";
            this.ipaddr.Size = new System.Drawing.Size(56, 17);
            this.ipaddr.Text = "IP: 0.0.0.0";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(601, 428);
            this.Controls.Add(this.peerg);
            this.Controls.Add(this.userg);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.statusStrip);
            this.Name = "Main";
            this.Text = "Onion Server";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Main_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.portnum)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.userg.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.peerg.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button startastopb;
        private System.Windows.Forms.NumericUpDown portnum;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem teToolStripMenuItem;
        private System.Windows.Forms.TextBox addresstb;
        private System.Windows.Forms.Button connectb;
        private System.Windows.Forms.GroupBox userg;
        private System.Windows.Forms.ListView userslistv;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ColumnHeader index1;
        private System.Windows.Forms.ColumnHeader name1;
        private System.Windows.Forms.ColumnHeader key1;
        private System.Windows.Forms.GroupBox peerg;
        private System.Windows.Forms.ListView peerslistv;
        private System.Windows.Forms.ColumnHeader index2;
        private System.Windows.Forms.ColumnHeader iep2;
        private System.Windows.Forms.ColumnHeader key2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox upnp;
        private System.Windows.Forms.CheckBox localserver;
        private System.Windows.Forms.ToolStripStatusLabel peerCountText;
        private System.Windows.Forms.ToolStripStatusLabel userCountText;
        private System.Windows.Forms.ToolStripStatusLabel ipaddr;
    }
}

