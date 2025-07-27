using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Data;
using MySqlConnector;
using Dapper;

namespace SqlTestDataGenerator.UI
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            cboDbType = new ComboBox();
            txtConnectionString = new TextBox();
            btnTestConnection = new Button();
            chkUseSSH = new CheckBox();
            grpSSH = new GroupBox();
            txtSSHHost = new TextBox();
            numSSHPort = new NumericUpDown();
            txtSSHUsername = new TextBox();
            txtSSHPassword = new TextBox();
            txtRemoteDbHost = new TextBox();
            numRemoteDbPort = new NumericUpDown();
            btnTestSSH = new Button();
            lblSSHStatus = new Label();
            sqlEditor = new TextBox();
            numRecords = new NumericUpDown();
            btnGenerateData = new Button();
            btnRunQuery = new Button();
            btnExecuteFromFile = new Button();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            lblGenerateStats = new Label();
            lblApiModel = new Label();
            lblApiStatus = new Label();
            lblDailyUsage = new Label();
            txtPromptContent = new TextBox();
            lblPromptContent = new Label();
            apiStatusTimer = new System.Windows.Forms.Timer(components);
            grpSSH.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numSSHPort).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRemoteDbPort).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRecords).BeginInit();
            SuspendLayout();
            // 
            // cboDbType
            // 
            cboDbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboDbType.FormattingEnabled = true;
            cboDbType.Items.AddRange(new object[] { "Oracle", "MySQL", "PostgreSQL", "SQL Server" });
            cboDbType.Location = new Point(51, 30);
            cboDbType.Margin = new Padding(5, 6, 5, 6);
            cboDbType.Name = "cboDbType";
            cboDbType.Size = new Size(340, 38);
            cboDbType.TabIndex = 0;
            cboDbType.SelectedIndexChanged += cboDbType_SelectedIndexChanged;
            // 
            // txtConnectionString
            // 
            txtConnectionString.Location = new Point(51, 114);
            txtConnectionString.Margin = new Padding(5, 6, 5, 6);
            txtConnectionString.Multiline = true;
            txtConnectionString.Name = "txtConnectionString";
            txtConnectionString.Size = new Size(940, 156);
            txtConnectionString.TabIndex = 1;
            txtConnectionString.Text = "Data Source=localhost:1521/XE;User Id=system;Password=22092012;Connection Timeout=120;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
            // 
            // btnTestConnection
            // 
            btnTestConnection.Location = new Point(1028, 114);
            btnTestConnection.Margin = new Padding(5, 6, 5, 6);
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.Size = new Size(257, 70);
            btnTestConnection.TabIndex = 2;
            btnTestConnection.Text = "🔗 Test Connection";
            btnTestConnection.UseVisualStyleBackColor = false;
            btnTestConnection.Click += btnTestConnection_Click;
            // 
            // chkUseSSH
            // 
            chkUseSSH.AutoSize = true;
            chkUseSSH.Location = new Point(1346, 324);
            chkUseSSH.Margin = new Padding(5, 6, 5, 6);
            chkUseSSH.Name = "chkUseSSH";
            chkUseSSH.Size = new Size(220, 34);
            chkUseSSH.TabIndex = 3;
            chkUseSSH.Text = "🔐 Use SSH Tunnel";
            chkUseSSH.UseVisualStyleBackColor = true;
            chkUseSSH.CheckedChanged += chkUseSSH_CheckedChanged;
            // 
            // grpSSH
            // 
            grpSSH.Controls.Add(txtSSHHost);
            grpSSH.Controls.Add(numSSHPort);
            grpSSH.Controls.Add(txtSSHUsername);
            grpSSH.Controls.Add(txtSSHPassword);
            grpSSH.Controls.Add(txtRemoteDbHost);
            grpSSH.Controls.Add(numRemoteDbPort);
            grpSSH.Controls.Add(btnTestSSH);
            grpSSH.Controls.Add(lblSSHStatus);
            grpSSH.Location = new Point(1346, 379);
            grpSSH.Margin = new Padding(5, 6, 5, 6);
            grpSSH.Name = "grpSSH";
            grpSSH.Padding = new Padding(5, 6, 5, 6);
            grpSSH.Size = new Size(394, 240);
            grpSSH.TabIndex = 4;
            grpSSH.TabStop = false;
            grpSSH.Text = "SSH Tunnel Configuration";
            grpSSH.Visible = false;
            // 
            // txtSSHHost
            // 
            txtSSHHost.Location = new Point(69, 47);
            txtSSHHost.Margin = new Padding(5, 6, 5, 6);
            txtSSHHost.Name = "txtSSHHost";
            txtSSHHost.Size = new Size(254, 35);
            txtSSHHost.TabIndex = 0;
            txtSSHHost.Text = "ssh.example.com";
            // 
            // numSSHPort
            // 
            numSSHPort.Location = new Point(531, 44);
            numSSHPort.Margin = new Padding(5, 6, 5, 6);
            numSSHPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numSSHPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numSSHPort.Name = "numSSHPort";
            numSSHPort.Size = new Size(103, 35);
            numSSHPort.TabIndex = 1;
            numSSHPort.Value = new decimal(new int[] { 22, 0, 0, 0 });
            // 
            // txtSSHUsername
            // 
            txtSSHUsername.Location = new Point(789, 44);
            txtSSHUsername.Margin = new Padding(5, 6, 5, 6);
            txtSSHUsername.Name = "txtSSHUsername";
            txtSSHUsername.Size = new Size(203, 35);
            txtSSHUsername.TabIndex = 2;
            txtSSHUsername.Text = "username";
            // 
            // txtSSHPassword
            // 
            txtSSHPassword.Location = new Point(1149, 44);
            txtSSHPassword.Margin = new Padding(5, 6, 5, 6);
            txtSSHPassword.Name = "txtSSHPassword";
            txtSSHPassword.Size = new Size(203, 35);
            txtSSHPassword.TabIndex = 3;
            txtSSHPassword.Text = "password";
            txtSSHPassword.UseSystemPasswordChar = true;
            // 
            // txtRemoteDbHost
            // 
            txtRemoteDbHost.Location = new Point(69, 117);
            txtRemoteDbHost.Margin = new Padding(5, 6, 5, 6);
            txtRemoteDbHost.Name = "txtRemoteDbHost";
            txtRemoteDbHost.Size = new Size(254, 35);
            txtRemoteDbHost.TabIndex = 4;
            txtRemoteDbHost.Text = "localhost";
            // 
            // numRemoteDbPort
            // 
            numRemoteDbPort.Location = new Point(531, 114);
            numRemoteDbPort.Margin = new Padding(5, 6, 5, 6);
            numRemoteDbPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numRemoteDbPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numRemoteDbPort.Name = "numRemoteDbPort";
            numRemoteDbPort.Size = new Size(103, 35);
            numRemoteDbPort.TabIndex = 5;
            numRemoteDbPort.Value = new decimal(new int[] { 3306, 0, 0, 0 });
            // 
            // btnTestSSH
            // 
            btnTestSSH.Location = new Point(651, 110);
            btnTestSSH.Margin = new Padding(5, 6, 5, 6);
            btnTestSSH.Name = "btnTestSSH";
            btnTestSSH.Size = new Size(171, 60);
            btnTestSSH.TabIndex = 6;
            btnTestSSH.Text = "🔗 Test SSH";
            btnTestSSH.UseVisualStyleBackColor = false;
            btnTestSSH.Click += btnTestSSH_Click;
            // 
            // lblSSHStatus
            // 
            lblSSHStatus.AutoSize = true;
            lblSSHStatus.Location = new Point(840, 120);
            lblSSHStatus.Margin = new Padding(5, 0, 5, 0);
            lblSSHStatus.Name = "lblSSHStatus";
            lblSSHStatus.Size = new Size(261, 30);
            lblSSHStatus.TabIndex = 7;
            lblSSHStatus.Text = "SSH Status: Not connected";
            // 
            // sqlEditor
            // 
            sqlEditor.Location = new Point(51, 324);
            sqlEditor.Margin = new Padding(5, 6, 5, 6);
            sqlEditor.Multiline = true;
            sqlEditor.Name = "sqlEditor";
            sqlEditor.ScrollBars = ScrollBars.Both;
            sqlEditor.Size = new Size(1214, 396);
            sqlEditor.TabIndex = 5;
            sqlEditor.Text = resources.GetString("sqlEditor.Text");
            sqlEditor.TextChanged += sqlEditor_TextChanged;
            // 
            // numRecords
            // 
            numRecords.Location = new Point(51, 861);
            numRecords.Margin = new Padding(5, 6, 5, 6);
            numRecords.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numRecords.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numRecords.Name = "numRecords";
            numRecords.Size = new Size(171, 35);
            numRecords.TabIndex = 6;
            numRecords.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // btnGenerateData
            // 
            btnGenerateData.Location = new Point(247, 836);
            btnGenerateData.Margin = new Padding(5, 6, 5, 6);
            btnGenerateData.Name = "btnGenerateData";
            btnGenerateData.Size = new Size(283, 84);
            btnGenerateData.TabIndex = 7;
            btnGenerateData.Text = "🤖 Generate Data";
            btnGenerateData.UseVisualStyleBackColor = false;
            btnGenerateData.Click += btnGenerateData_Click;
            // 
            // btnRunQuery
            // 
            btnRunQuery.Location = new Point(572, 836);
            btnRunQuery.Margin = new Padding(5, 6, 5, 6);
            btnRunQuery.Name = "btnRunQuery";
            btnRunQuery.Size = new Size(303, 84);
            btnRunQuery.TabIndex = 8;
            btnRunQuery.Text = "🔍 Run Query";
            btnRunQuery.UseVisualStyleBackColor = false;
            btnRunQuery.Click += btnRunQuery_Click;
            // 
            // btnExecuteFromFile
            // 
            btnExecuteFromFile.Enabled = false;
            btnExecuteFromFile.Location = new Point(937, 836);
            btnExecuteFromFile.Margin = new Padding(5, 6, 5, 6);
            btnExecuteFromFile.Name = "btnExecuteFromFile";
            btnExecuteFromFile.Size = new Size(279, 84);
            btnExecuteFromFile.TabIndex = 9;
            btnExecuteFromFile.Text = "💾 Commit";
            btnExecuteFromFile.UseVisualStyleBackColor = false;
            btnExecuteFromFile.Click += btnExecuteFromFile_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(1260, 846);
            progressBar.Margin = new Padding(5, 6, 5, 6);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(611, 50);
            progressBar.TabIndex = 10;
            progressBar.Visible = false;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblStatus.AutoEllipsis = true;
            lblStatus.Location = new Point(34, 1233);
            lblStatus.Margin = new Padding(5, 0, 5, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(1851, 60);
            lblStatus.TabIndex = 12;
            lblStatus.Text = "Ready - Select database type and click Test Connection to begin";
            // 
            // lblGenerateStats
            // 
            lblGenerateStats.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblGenerateStats.Location = new Point(34, 1293);
            lblGenerateStats.Margin = new Padding(5, 0, 5, 0);
            lblGenerateStats.Name = "lblGenerateStats";
            lblGenerateStats.Size = new Size(857, 60);
            lblGenerateStats.TabIndex = 13;
            lblGenerateStats.Text = "Generated: 0 records | Total time: 0 seconds";
            // 
            // lblApiModel
            // 
            lblApiModel.Location = new Point(34, 750);
            lblApiModel.Margin = new Padding(5, 0, 5, 0);
            lblApiModel.Name = "lblApiModel";
            lblApiModel.Size = new Size(343, 80);
            lblApiModel.TabIndex = 14;
            lblApiModel.Text = "🤖 AI Model: Initializing...";
            // 
            // lblApiStatus
            // 
            lblApiStatus.Location = new Point(394, 750);
            lblApiStatus.Margin = new Padding(5, 0, 5, 0);
            lblApiStatus.Name = "lblApiStatus";
            lblApiStatus.Size = new Size(257, 80);
            lblApiStatus.TabIndex = 15;
            lblApiStatus.Text = "🔄 Status: Ready";
            // 
            // lblDailyUsage
            // 
            lblDailyUsage.Location = new Point(669, 750);
            lblDailyUsage.Margin = new Padding(5, 0, 5, 0);
            lblDailyUsage.Name = "lblDailyUsage";
            lblDailyUsage.Size = new Size(206, 80);
            lblDailyUsage.TabIndex = 16;
            lblDailyUsage.Text = "📊 Daily: 0/100";
            // 
            // txtPromptContent
            // 
            txtPromptContent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPromptContent.BackColor = SystemColors.ActiveBorder;
            txtPromptContent.Location = new Point(48, 991);
            txtPromptContent.Margin = new Padding(5, 6, 5, 6);
            txtPromptContent.Multiline = true;
            txtPromptContent.Name = "txtPromptContent";
            txtPromptContent.ReadOnly = true;
            txtPromptContent.ScrollBars = ScrollBars.Vertical;
            txtPromptContent.Size = new Size(1837, 243);
            txtPromptContent.TabIndex = 17;
            txtPromptContent.Text = "📝 AI Prompt and Model Info will appear here...";
            // 
            // lblPromptContent
            // 
            lblPromptContent.Location = new Point(34, 937);
            lblPromptContent.Margin = new Padding(5, 0, 5, 0);
            lblPromptContent.Name = "lblPromptContent";
            lblPromptContent.Size = new Size(343, 30);
            lblPromptContent.TabIndex = 18;
            lblPromptContent.Text = "📝 AI Prompt & Model Information:";
            // 
            // apiStatusTimer
            // 
            apiStatusTimer.Interval = 10000;
            apiStatusTimer.Tick += UpdateApiStatus;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1930, 1372);
            Controls.Add(lblDailyUsage);
            Controls.Add(lblApiStatus);
            Controls.Add(lblApiModel);
            Controls.Add(lblPromptContent);
            Controls.Add(txtPromptContent);
            Controls.Add(lblGenerateStats);
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(btnExecuteFromFile);
            Controls.Add(btnRunQuery);
            Controls.Add(btnGenerateData);
            Controls.Add(numRecords);
            Controls.Add(sqlEditor);
            Controls.Add(grpSSH);
            Controls.Add(chkUseSSH);
            Controls.Add(btnTestConnection);
            Controls.Add(txtConnectionString);
            Controls.Add(cboDbType);
            Margin = new Padding(5, 6, 5, 6);
            MinimumSize = new Size(1954, 1436);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SQL Test Data Generator (Gemini AI) - SSH Tunnel Support";
            Load += MainForm_Load;
            grpSSH.ResumeLayout(false);
            grpSSH.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numSSHPort).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRemoteDbPort).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRecords).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cboDbType;
        private TextBox txtConnectionString;
        private Button btnTestConnection;
        private CheckBox chkUseSSH;
        private GroupBox grpSSH;
        private TextBox txtSSHHost;
        private NumericUpDown numSSHPort;
        private TextBox txtSSHUsername;
        private TextBox txtSSHPassword;
        private TextBox txtRemoteDbHost;
        private NumericUpDown numRemoteDbPort;
        private Button btnTestSSH;
        private Label lblSSHStatus;
        private TextBox sqlEditor;
        private NumericUpDown numRecords;
        private Button btnGenerateData;
        private Button btnRunQuery;
        private Button btnExecuteFromFile;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblGenerateStats;
        private Label lblApiModel;
        private Label lblApiStatus;
        private Label lblDailyUsage;
        private TextBox txtPromptContent;
        private Label lblPromptContent;
        private System.Windows.Forms.Timer apiStatusTimer;
    }
} 