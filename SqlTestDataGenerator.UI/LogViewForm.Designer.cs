using SqlTestDataGenerator.Core.Services;

namespace SqlTestDataGenerator.UI
{
    partial class LogViewForm
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
            this.dataGridViewLogs = new System.Windows.Forms.DataGridView();
            this.cboLogLevel = new System.Windows.Forms.ComboBox();
            this.btnClearLogs = new System.Windows.Forms.Button();
            this.btnExportLogs = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.chkAutoScroll = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewLogs)).BeginInit();
            this.SuspendLayout();
            
            // 
            // dataGridViewLogs
            // 
            this.dataGridViewLogs.AllowUserToAddRows = false;
            this.dataGridViewLogs.AllowUserToDeleteRows = false;
            this.dataGridViewLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.dataGridViewLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewLogs.Location = new System.Drawing.Point(20, 60);
            this.dataGridViewLogs.Name = "dataGridViewLogs";
            this.dataGridViewLogs.ReadOnly = true;
            this.dataGridViewLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewLogs.Size = new System.Drawing.Size(1140, 570);
            this.dataGridViewLogs.TabIndex = 0;
            
            // 
            // cboLogLevel
            // 
            this.cboLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLogLevel.FormattingEnabled = true;
            this.cboLogLevel.Items.AddRange(new object[] {
            "All",
            "Information",
            "Warning",
            "Error"});
            this.cboLogLevel.Location = new System.Drawing.Point(105, 18);
            this.cboLogLevel.Name = "cboLogLevel";
            this.cboLogLevel.Size = new System.Drawing.Size(120, 28);
            this.cboLogLevel.TabIndex = 1;
            this.cboLogLevel.SelectedIndex = 0;
            this.cboLogLevel.SelectedIndexChanged += new System.EventHandler((s, e) => this.FilterLogs());
            
            // 
            // btnClearLogs
            // 
            this.btnClearLogs.Location = new System.Drawing.Point(660, 17);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(100, 32);
            this.btnClearLogs.TabIndex = 2;
            this.btnClearLogs.Text = "Clear Logs";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Click += new System.EventHandler(this.BtnClearLogs_Click);
            
            // 
            // btnExportLogs
            // 
            this.btnExportLogs.Location = new System.Drawing.Point(775, 17);
            this.btnExportLogs.Name = "btnExportLogs";
            this.btnExportLogs.Size = new System.Drawing.Size(100, 32);
            this.btnExportLogs.TabIndex = 3;
            this.btnExportLogs.Text = "Export Logs";
            this.btnExportLogs.UseVisualStyleBackColor = true;
            this.btnExportLogs.Click += new System.EventHandler(this.BtnExportLogs_Click);
            
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.Location = new System.Drawing.Point(20, 645);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(800, 25);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Ready";
            
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(315, 18);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(200, 28);
            this.txtSearch.TabIndex = 5;
            this.txtSearch.TextChanged += new System.EventHandler((s, e) => this.FilterLogs());
            
            // 
            // chkAutoScroll
            // 
            this.chkAutoScroll.AutoSize = true;
            this.chkAutoScroll.Checked = true;
            this.chkAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoScroll.Location = new System.Drawing.Point(540, 20);
            this.chkAutoScroll.Name = "chkAutoScroll";
            this.chkAutoScroll.Size = new System.Drawing.Size(100, 24);
            this.chkAutoScroll.TabIndex = 6;
            this.chkAutoScroll.Text = "Auto Scroll";
            this.chkAutoScroll.UseVisualStyleBackColor = true;
            
            // 
            // LogViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.chkAutoScroll);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnExportLogs);
            this.Controls.Add(this.btnClearLogs);
            this.Controls.Add(this.cboLogLevel);
            this.Controls.Add(this.dataGridViewLogs);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "LogViewForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Application Logs";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewLogs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DataGridView dataGridViewLogs;
        private ComboBox cboLogLevel;
        private Button btnClearLogs;
        private Button btnExportLogs;
        private Label lblStatus;
        private TextBox txtSearch;
        private CheckBox chkAutoScroll;
    }
} 