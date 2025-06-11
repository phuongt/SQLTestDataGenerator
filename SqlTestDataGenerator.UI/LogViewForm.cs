using SqlTestDataGenerator.Core.Services;

namespace SqlTestDataGenerator.UI;

/// <summary>
/// Form for displaying real-time application logs
/// </summary>
public partial class LogViewForm : Form
{
    private readonly ILoggerService _loggerService;
    
    // UI Controls
    private DataGridView dataGridViewLogs = null!;
    private ComboBox cboLogLevel = null!;
    private Button btnClearLogs = null!;
    private Button btnExportLogs = null!;
    private Label lblStatus = null!;
    private TextBox txtSearch = null!;
    private CheckBox chkAutoScroll = null!;

    private BindingSource _logsBindingSource = new();
    private List<LogEntry> _filteredLogs = new();

    public LogViewForm(ILoggerService loggerService)
    {
        _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        InitializeComponent();
        SetupLogViewer();
        
        // Subscribe to real-time log updates
        _loggerService.LogEntryCreated += OnLogEntryCreated;
    }

    private void InitializeComponent()
    {
        this.Text = "Application Logs";
        this.Size = new Size(1200, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.MinimumSize = new Size(800, 500);

        // Create controls
        CreateFilterControls();
        CreateLogDataGrid();
        CreateStatusBar();

        // Layout
        LayoutControls();
    }

    private void CreateFilterControls()
    {
        // Log Level Filter
        var lblLogLevel = new Label
        {
            Text = "Log Level:",
            Location = new Point(20, 20),
            Size = new Size(80, 25),
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft
        };

        cboLogLevel = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(105, 18),
            Size = new Size(120, 28),
            Font = new Font("Segoe UI", 9F)
        };
        cboLogLevel.Items.AddRange(new object[] { "All", "Information", "Warning", "Error" });
        cboLogLevel.SelectedIndex = 0;
        cboLogLevel.SelectedIndexChanged += (s, e) => FilterLogs();

        // Search
        var lblSearch = new Label
        {
            Text = "Search:",
            Location = new Point(250, 20),
            Size = new Size(60, 25),
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft
        };

        txtSearch = new TextBox
        {
            Location = new Point(315, 18),
            Size = new Size(200, 28),
            Font = new Font("Segoe UI", 9F)
        };
        txtSearch.TextChanged += (s, e) => FilterLogs();

        // Auto Scroll
        chkAutoScroll = new CheckBox
        {
            Text = "Auto Scroll",
            Location = new Point(540, 20),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 9F),
            Checked = true
        };

        // Clear Logs Button
        btnClearLogs = new Button
        {
            Text = "Clear Logs",
            Location = new Point(660, 17),
            Size = new Size(100, 32),
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(244, 67, 54),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClearLogs.FlatAppearance.BorderSize = 0;
        btnClearLogs.Click += BtnClearLogs_Click;

        // Export Logs Button
        btnExportLogs = new Button
        {
            Text = "Export Logs",
            Location = new Point(775, 17),
            Size = new Size(100, 32),
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnExportLogs.FlatAppearance.BorderSize = 0;
        btnExportLogs.Click += BtnExportLogs_Click;

        // Add controls to form
        this.Controls.AddRange(new Control[] 
        { 
            lblLogLevel, cboLogLevel, lblSearch, txtSearch, 
            chkAutoScroll, btnClearLogs, btnExportLogs 
        });
    }

    private void CreateLogDataGrid()
    {
        dataGridViewLogs = new DataGridView
        {
            Location = new Point(20, 60),
            Size = new Size(1140, 570),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        };

        // Configure columns
        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Timestamp",
            HeaderText = "Timestamp",
            DataPropertyName = "Timestamp",
            Width = 150,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss.fff" }
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Level",
            HeaderText = "Level",
            DataPropertyName = "Level",
            Width = 100
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Context",
            HeaderText = "Context",
            DataPropertyName = "Context",
            Width = 120
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Message",
            HeaderText = "Message",
            DataPropertyName = "Message",
            Width = 400,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Parameters",
            HeaderText = "Parameters",
            DataPropertyName = "Parameters",
            Width = 200
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Exception",
            HeaderText = "Exception",
            DataPropertyName = "Exception",
            Width = 300
        });

        // Set row colors based on log level
        dataGridViewLogs.CellFormatting += DataGridViewLogs_CellFormatting;

        this.Controls.Add(dataGridViewLogs);
    }

    private void CreateStatusBar()
    {
        lblStatus = new Label
        {
            Text = "Ready",
            Location = new Point(20, 645),
            Size = new Size(800, 25),
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = SystemColors.Control,
            BorderStyle = BorderStyle.FixedSingle
        };

        this.Controls.Add(lblStatus);
    }

    private void LayoutControls()
    {
        // All controls are already positioned in their creation methods
        // This method is for any additional layout logic if needed
    }

    private void SetupLogViewer()
    {
        _logsBindingSource.DataSource = _filteredLogs;
        dataGridViewLogs.DataSource = _logsBindingSource;
        
        LoadExistingLogs();
        UpdateStatus();
    }

    private void LoadExistingLogs()
    {
        var existingLogs = _loggerService.GetLogEntries().ToList();
        _filteredLogs.AddRange(existingLogs);
        RefreshGrid();
    }

    private void OnLogEntryCreated(LogEntry logEntry)
    {
        // Ensure UI updates happen on the UI thread
        if (this.InvokeRequired)
        {
            this.Invoke(new Action<LogEntry>(OnLogEntryCreated), logEntry);
            return;
        }

        // Add new log entry
        if (ShouldShowLogEntry(logEntry))
        {
            _filteredLogs.Insert(0, logEntry); // Add to beginning for newest first
            RefreshGrid();
            UpdateStatus();

            // Auto scroll to latest if enabled
            if (chkAutoScroll.Checked && dataGridViewLogs.Rows.Count > 0)
            {
                dataGridViewLogs.FirstDisplayedScrollingRowIndex = 0;
            }
        }
    }

    private bool ShouldShowLogEntry(LogEntry logEntry)
    {
        // Filter by log level
        if (cboLogLevel.SelectedIndex > 0)
        {
            var selectedLevel = (LogLevel)(cboLogLevel.SelectedIndex - 1);
            if (logEntry.Level != selectedLevel)
                return false;
        }

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(txtSearch.Text))
        {
            var searchText = txtSearch.Text.ToLower();
            if (!logEntry.Message.ToLower().Contains(searchText) &&
                !logEntry.Context?.ToLower().Contains(searchText) == true &&
                !logEntry.Parameters?.ToLower().Contains(searchText) == true)
            {
                return false;
            }
        }

        return true;
    }

    private void FilterLogs()
    {
        var allLogs = _loggerService.GetLogEntries().ToList();
        _filteredLogs.Clear();

        foreach (var log in allLogs)
        {
            if (ShouldShowLogEntry(log))
            {
                _filteredLogs.Add(log);
            }
        }

        RefreshGrid();
        UpdateStatus();
    }

    private void RefreshGrid()
    {
        _logsBindingSource.ResetBindings(false);
    }

    private void UpdateStatus()
    {
        var totalLogs = _loggerService.GetLogEntries().Count();
        var filteredCount = _filteredLogs.Count;
        lblStatus.Text = $"Showing {filteredCount} of {totalLogs} log entries";
    }

    private void DataGridViewLogs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _filteredLogs.Count) return;

        var logEntry = _filteredLogs[e.RowIndex];
        var row = dataGridViewLogs.Rows[e.RowIndex];

        // Set row colors based on log level
        switch (logEntry.Level)
        {
            case LogLevel.Error:
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(183, 28, 28);
                break;
            case LogLevel.Warning:
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 225);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(251, 140, 0);
                break;
            case LogLevel.Information:
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = Color.Black;
                break;
        }
    }

    private void BtnClearLogs_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all logs?",
            "Clear Logs",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _loggerService.ClearLogs();
            _filteredLogs.Clear();
            RefreshGrid();
            UpdateStatus();
        }
    }

    private void BtnExportLogs_Click(object? sender, EventArgs e)
    {
        using var saveDialog = new SaveFileDialog
        {
            Title = "Export Logs",
            Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = "txt",
            FileName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (saveDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                ExportLogsToFile(saveDialog.FileName);
                MessageBox.Show($"Logs exported successfully to:\n{saveDialog.FileName}", 
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export logs:\n{ex.Message}", 
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ExportLogsToFile(string filePath)
    {
        using var writer = new StreamWriter(filePath);
        
        // Write header
        writer.WriteLine("Timestamp\tLevel\tContext\tMessage\tParameters\tException");

        // Write log entries
        foreach (var log in _filteredLogs)
        {
            writer.WriteLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\t{log.Level}\t{log.Context}\t{log.Message}\t{log.Parameters}\t{log.Exception}");
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Unsubscribe from events
        _loggerService.LogEntryCreated -= OnLogEntryCreated;
        base.OnFormClosing(e);
    }
} 