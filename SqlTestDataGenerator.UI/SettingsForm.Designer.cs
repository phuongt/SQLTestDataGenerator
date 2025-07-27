using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.UI
{
    partial class SettingsForm
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
            this.txtApiKey = new System.Windows.Forms.TextBox();
            this.txtDefaultConnectionString = new System.Windows.Forms.TextBox();
            this.cboDefaultDatabaseType = new System.Windows.Forms.ComboBox();
            this.chkAutoSaveSettings = new System.Windows.Forms.CheckBox();
            this.chkShowLogWindow = new System.Windows.Forms.CheckBox();
            this.chkEnableSSH = new System.Windows.Forms.CheckBox();
            this.chkEnableAIGeneration = new System.Windows.Forms.CheckBox();
            this.numMaxRetries = new System.Windows.Forms.NumericUpDown();
            this.numTimeoutSeconds = new System.Windows.Forms.NumericUpDown();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnTestApiKey = new System.Windows.Forms.Button();
            this.btnResetToDefaults = new System.Windows.Forms.Button();
            this.lblApiKey = new System.Windows.Forms.Label();
            this.lblDefaultConnectionString = new System.Windows.Forms.Label();
            this.lblDefaultDatabaseType = new System.Windows.Forms.Label();
            this.lblMaxRetries = new System.Windows.Forms.Label();
            this.lblTimeoutSeconds = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxRetries)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeoutSeconds)).BeginInit();
            this.SuspendLayout();
            
            // 
            // txtApiKey
            // 
            this.txtApiKey.Location = new System.Drawing.Point(120, 22);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(350, 25);
            this.txtApiKey.TabIndex = 0;
            this.txtApiKey.UseSystemPasswordChar = true;
            
            // 
            // txtDefaultConnectionString
            // 
            this.txtDefaultConnectionString.Location = new System.Drawing.Point(120, 57);
            this.txtDefaultConnectionString.Name = "txtDefaultConnectionString";
            this.txtDefaultConnectionString.Size = new System.Drawing.Size(500, 25);
            this.txtDefaultConnectionString.TabIndex = 1;
            
            // 
            // cboDefaultDatabaseType
            // 
            this.cboDefaultDatabaseType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDefaultDatabaseType.FormattingEnabled = true;
            this.cboDefaultDatabaseType.Items.AddRange(new object[] {
            "MySQL",
            "SQL Server",
            "PostgreSQL",
            "Oracle"});
            this.cboDefaultDatabaseType.Location = new System.Drawing.Point(120, 22);
            this.cboDefaultDatabaseType.Name = "cboDefaultDatabaseType";
            this.cboDefaultDatabaseType.Size = new System.Drawing.Size(150, 25);
            this.cboDefaultDatabaseType.TabIndex = 2;
            
            // 
            // chkAutoSaveSettings
            // 
            this.chkAutoSaveSettings.AutoSize = true;
            this.chkAutoSaveSettings.Checked = true;
            this.chkAutoSaveSettings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoSaveSettings.Location = new System.Drawing.Point(10, 25);
            this.chkAutoSaveSettings.Name = "chkAutoSaveSettings";
            this.chkAutoSaveSettings.Size = new System.Drawing.Size(200, 24);
            this.chkAutoSaveSettings.TabIndex = 3;
            this.chkAutoSaveSettings.Text = "Auto-save settings on exit";
            this.chkAutoSaveSettings.UseVisualStyleBackColor = true;
            
            // 
            // chkShowLogWindow
            // 
            this.chkShowLogWindow.AutoSize = true;
            this.chkShowLogWindow.Location = new System.Drawing.Point(10, 50);
            this.chkShowLogWindow.Name = "chkShowLogWindow";
            this.chkShowLogWindow.Size = new System.Drawing.Size(200, 24);
            this.chkShowLogWindow.TabIndex = 4;
            this.chkShowLogWindow.Text = "Show log window on startup";
            this.chkShowLogWindow.UseVisualStyleBackColor = true;
            
            // 
            // chkEnableSSH
            // 
            this.chkEnableSSH.AutoSize = true;
            this.chkEnableSSH.Checked = true;
            this.chkEnableSSH.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableSSH.Location = new System.Drawing.Point(220, 25);
            this.chkEnableSSH.Name = "chkEnableSSH";
            this.chkEnableSSH.Size = new System.Drawing.Size(200, 24);
            this.chkEnableSSH.TabIndex = 5;
            this.chkEnableSSH.Text = "Enable SSH tunnel support";
            this.chkEnableSSH.UseVisualStyleBackColor = true;
            this.chkEnableSSH.CheckedChanged += new System.EventHandler(this.ChkEnableSSH_CheckedChanged);
            
            // 
            // chkEnableAIGeneration
            // 
            this.chkEnableAIGeneration.AutoSize = true;
            this.chkEnableAIGeneration.Checked = true;
            this.chkEnableAIGeneration.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableAIGeneration.Location = new System.Drawing.Point(10, 25);
            this.chkEnableAIGeneration.Name = "chkEnableAIGeneration";
            this.chkEnableAIGeneration.Size = new System.Drawing.Size(250, 24);
            this.chkEnableAIGeneration.TabIndex = 6;
            this.chkEnableAIGeneration.Text = "Enable AI-powered data generation";
            this.chkEnableAIGeneration.UseVisualStyleBackColor = true;
            this.chkEnableAIGeneration.CheckedChanged += new System.EventHandler(this.ChkEnableAIGeneration_CheckedChanged);
            
            // 
            // numMaxRetries
            // 
            this.numMaxRetries.Location = new System.Drawing.Point(370, 22);
            this.numMaxRetries.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numMaxRetries.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxRetries.Name = "numMaxRetries";
            this.numMaxRetries.Size = new System.Drawing.Size(60, 25);
            this.numMaxRetries.TabIndex = 7;
            this.numMaxRetries.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            
            // 
            // numTimeoutSeconds
            // 
            this.numTimeoutSeconds.Location = new System.Drawing.Point(540, 22);
            this.numTimeoutSeconds.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numTimeoutSeconds.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numTimeoutSeconds.Name = "numTimeoutSeconds";
            this.numTimeoutSeconds.Size = new System.Drawing.Size(60, 25);
            this.numTimeoutSeconds.TabIndex = 8;
            this.numTimeoutSeconds.Value = new decimal(new int[] {
            120,
            0,
            0,
            0});
            
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(480, 540);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 35);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "Save Settings";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(590, 540);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 35);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            
            // 
            // btnTestApiKey
            // 
            this.btnTestApiKey.Location = new System.Drawing.Point(480, 20);
            this.btnTestApiKey.Name = "btnTestApiKey";
            this.btnTestApiKey.Size = new System.Drawing.Size(100, 30);
            this.btnTestApiKey.TabIndex = 11;
            this.btnTestApiKey.Text = "Test API Key";
            this.btnTestApiKey.UseVisualStyleBackColor = true;
            this.btnTestApiKey.Click += new System.EventHandler(this.BtnTestApiKey_Click);
            
            // 
            // btnResetToDefaults
            // 
            this.btnResetToDefaults.Location = new System.Drawing.Point(20, 540);
            this.btnResetToDefaults.Name = "btnResetToDefaults";
            this.btnResetToDefaults.Size = new System.Drawing.Size(120, 35);
            this.btnResetToDefaults.TabIndex = 12;
            this.btnResetToDefaults.Text = "Reset to Defaults";
            this.btnResetToDefaults.UseVisualStyleBackColor = true;
            this.btnResetToDefaults.Click += new System.EventHandler(this.BtnResetToDefaults_Click);
            
            // 
            // lblApiKey
            // 
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(10, 25);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(100, 20);
            this.lblApiKey.TabIndex = 13;
            this.lblApiKey.Text = "OpenAI API Key:";
            
            // 
            // lblDefaultConnectionString
            // 
            this.lblDefaultConnectionString.AutoSize = true;
            this.lblDefaultConnectionString.Location = new System.Drawing.Point(10, 60);
            this.lblDefaultConnectionString.Name = "lblDefaultConnectionString";
            this.lblDefaultConnectionString.Size = new System.Drawing.Size(100, 20);
            this.lblDefaultConnectionString.TabIndex = 14;
            this.lblDefaultConnectionString.Text = "Default Connection:";
            
            // 
            // lblDefaultDatabaseType
            // 
            this.lblDefaultDatabaseType.AutoSize = true;
            this.lblDefaultDatabaseType.Location = new System.Drawing.Point(10, 25);
            this.lblDefaultDatabaseType.Name = "lblDefaultDatabaseType";
            this.lblDefaultDatabaseType.Size = new System.Drawing.Size(100, 20);
            this.lblDefaultDatabaseType.TabIndex = 15;
            this.lblDefaultDatabaseType.Text = "Default DB Type:";
            
            // 
            // lblMaxRetries
            // 
            this.lblMaxRetries.AutoSize = true;
            this.lblMaxRetries.Location = new System.Drawing.Point(280, 25);
            this.lblMaxRetries.Name = "lblMaxRetries";
            this.lblMaxRetries.Size = new System.Drawing.Size(80, 20);
            this.lblMaxRetries.TabIndex = 16;
            this.lblMaxRetries.Text = "Max Retries:";
            
            // 
            // lblTimeoutSeconds
            // 
            this.lblTimeoutSeconds.AutoSize = true;
            this.lblTimeoutSeconds.Location = new System.Drawing.Point(450, 25);
            this.lblTimeoutSeconds.Name = "lblTimeoutSeconds";
            this.lblTimeoutSeconds.Size = new System.Drawing.Size(80, 20);
            this.lblTimeoutSeconds.TabIndex = 17;
            this.lblTimeoutSeconds.Text = "Timeout (s):";
            
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.lblTimeoutSeconds);
            this.Controls.Add(this.lblMaxRetries);
            this.Controls.Add(this.lblDefaultDatabaseType);
            this.Controls.Add(this.lblDefaultConnectionString);
            this.Controls.Add(this.lblApiKey);
            this.Controls.Add(this.btnResetToDefaults);
            this.Controls.Add(this.btnTestApiKey);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.numTimeoutSeconds);
            this.Controls.Add(this.numMaxRetries);
            this.Controls.Add(this.chkEnableAIGeneration);
            this.Controls.Add(this.chkEnableSSH);
            this.Controls.Add(this.chkShowLogWindow);
            this.Controls.Add(this.chkAutoSaveSettings);
            this.Controls.Add(this.cboDefaultDatabaseType);
            this.Controls.Add(this.txtDefaultConnectionString);
            this.Controls.Add(this.txtApiKey);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(700, 600);
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Application Settings";
            ((System.ComponentModel.ISupportInitialize)(this.numMaxRetries)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeoutSeconds)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private TextBox txtApiKey;
        private TextBox txtDefaultConnectionString;
        private ComboBox cboDefaultDatabaseType;
        private CheckBox chkAutoSaveSettings;
        private CheckBox chkShowLogWindow;
        private CheckBox chkEnableSSH;
        private CheckBox chkEnableAIGeneration;
        private NumericUpDown numMaxRetries;
        private NumericUpDown numTimeoutSeconds;
        private Button btnSave;
        private Button btnCancel;
        private Button btnTestApiKey;
        private Button btnResetToDefaults;
        private Label lblApiKey;
        private Label lblDefaultConnectionString;
        private Label lblDefaultDatabaseType;
        private Label lblMaxRetries;
        private Label lblTimeoutSeconds;
    }
} 