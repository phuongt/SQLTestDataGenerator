using SqlTestDataGenerator.Core.Services;

namespace SqlTestDataGenerator.UI;

/// <summary>
/// Form for displaying real-time application logs
/// </summary>
public partial class LogViewForm : Form
{
        private readonly ILoggerService _loggerService;

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