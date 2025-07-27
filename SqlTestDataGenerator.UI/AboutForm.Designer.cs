namespace SqlTestDataGenerator.UI
{
    partial class AboutForm
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
            this.lblAppName = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtFeatures = new System.Windows.Forms.TextBox();
            this.txtTechnologies = new System.Windows.Forms.TextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnViewLogs = new System.Windows.Forms.Button();
            this.btnOpenWebsite = new System.Windows.Forms.Button();
            this.btnCopyInfo = new System.Windows.Forms.Button();
            this.pictureBoxIcon = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
            this.SuspendLayout();
            
            // 
            // lblAppName
            // 
            this.lblAppName.AutoSize = true;
            this.lblAppName.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblAppName.Location = new System.Drawing.Point(20, 20);
            this.lblAppName.Name = "lblAppName";
            this.lblAppName.Size = new System.Drawing.Size(300, 30);
            this.lblAppName.TabIndex = 0;
            this.lblAppName.Text = "SQL Test Data Generator";
            
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblVersion.Location = new System.Drawing.Point(20, 55);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(100, 19);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "Version 1.0.0";
            
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(20, 85);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(400, 20);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "A powerful tool for generating test data for SQL databases.";
            
            // 
            // txtFeatures
            // 
            this.txtFeatures.BackColor = System.Drawing.SystemColors.Control;
            this.txtFeatures.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtFeatures.Location = new System.Drawing.Point(20, 120);
            this.txtFeatures.Multiline = true;
            this.txtFeatures.Name = "txtFeatures";
            this.txtFeatures.ReadOnly = true;
            this.txtFeatures.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtFeatures.Size = new System.Drawing.Size(400, 150);
            this.txtFeatures.TabIndex = 3;
            this.txtFeatures.Text = "• AI-powered data generation\r\n• Support for multiple database types\r\n• SSH tunnel support\r\n• Comprehensive logging\r\n• Export functionality\r\n• User-friendly interface";
            
            // 
            // txtTechnologies
            // 
            this.txtTechnologies.BackColor = System.Drawing.SystemColors.Control;
            this.txtTechnologies.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtTechnologies.Location = new System.Drawing.Point(20, 290);
            this.txtTechnologies.Multiline = true;
            this.txtTechnologies.Name = "txtTechnologies";
            this.txtTechnologies.ReadOnly = true;
            this.txtTechnologies.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtTechnologies.Size = new System.Drawing.Size(400, 100);
            this.txtTechnologies.TabIndex = 4;
            this.txtTechnologies.Text = "• C# .NET 8\r\n• Windows Forms\r\n• Dapper ORM\r\n• OpenAI API\r\n• SSH.NET\r\n• Serilog";
            
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(320, 420);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 35);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            
            // 
            // btnViewLogs
            // 
            this.btnViewLogs.Location = new System.Drawing.Point(210, 420);
            this.btnViewLogs.Name = "btnViewLogs";
            this.btnViewLogs.Size = new System.Drawing.Size(100, 35);
            this.btnViewLogs.TabIndex = 6;
            this.btnViewLogs.Text = "View Logs";
            this.btnViewLogs.UseVisualStyleBackColor = true;
            this.btnViewLogs.Click += new System.EventHandler(this.BtnViewLogs_Click);
            
            // 
            // btnOpenWebsite
            // 
            this.btnOpenWebsite.Location = new System.Drawing.Point(100, 420);
            this.btnOpenWebsite.Name = "btnOpenWebsite";
            this.btnOpenWebsite.Size = new System.Drawing.Size(100, 35);
            this.btnOpenWebsite.TabIndex = 7;
            this.btnOpenWebsite.Text = "Website";
            this.btnOpenWebsite.UseVisualStyleBackColor = true;
            this.btnOpenWebsite.Click += new System.EventHandler(this.BtnOpenWebsite_Click);
            
            // 
            // btnCopyInfo
            // 
            this.btnCopyInfo.Location = new System.Drawing.Point(20, 420);
            this.btnCopyInfo.Name = "btnCopyInfo";
            this.btnCopyInfo.Size = new System.Drawing.Size(70, 35);
            this.btnCopyInfo.TabIndex = 8;
            this.btnCopyInfo.Text = "Copy Info";
            this.btnCopyInfo.UseVisualStyleBackColor = true;
            this.btnCopyInfo.Click += new System.EventHandler(this.BtnCopyInfo_Click);
            
            // 
            // pictureBoxIcon
            // 
            this.pictureBoxIcon.Location = new System.Drawing.Point(450, 20);
            this.pictureBoxIcon.Name = "pictureBoxIcon";
            this.pictureBoxIcon.Size = new System.Drawing.Size(64, 64);
            this.pictureBoxIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxIcon.TabIndex = 9;
            this.pictureBoxIcon.TabStop = false;
            
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(550, 480);
            this.Controls.Add(this.pictureBoxIcon);
            this.Controls.Add(this.btnCopyInfo);
            this.Controls.Add(this.btnOpenWebsite);
            this.Controls.Add(this.btnViewLogs);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.txtTechnologies);
            this.Controls.Add(this.txtFeatures);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblAppName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 450);
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About SQL Test Data Generator";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label lblAppName;
        private Label lblVersion;
        private Label lblDescription;
        private TextBox txtFeatures;
        private TextBox txtTechnologies;
        private Button btnClose;
        private Button btnViewLogs;
        private Button btnOpenWebsite;
        private Button btnCopyInfo;
        private PictureBox pictureBoxIcon;
    }
} 