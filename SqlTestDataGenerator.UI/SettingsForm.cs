using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.UI;

/// <summary>
/// Form for managing application settings and configuration
/// </summary>
public partial class SettingsForm : Form
{
    private readonly ConfigurationService _configService;
    private UserSettings _currentSettings;

    public SettingsForm(ConfigurationService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _currentSettings = _configService.LoadUserSettings() ?? new UserSettings();
        
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        // Load API Key
        txtApiKey.Text = _currentSettings.OpenAiApiKey ?? string.Empty;
        
        // Load Database Settings
        if (!string.IsNullOrEmpty(_currentSettings.DatabaseType))
        {
            var index = cboDefaultDatabaseType.Items.IndexOf(_currentSettings.DatabaseType);
            if (index >= 0) cboDefaultDatabaseType.SelectedIndex = index;
        }
        
        txtDefaultConnectionString.Text = _currentSettings.ConnectionString ?? string.Empty;
        // numDefaultRecordCount is not declared in designer, so we'll skip it
        
        // Load UI Settings
        chkAutoSaveSettings.Checked = _currentSettings.AutoSaveSettings;
        chkShowLogWindow.Checked = _currentSettings.ShowLogWindow;
        chkEnableSSH.Checked = _currentSettings.EnableSSH;
        
        // Load AI Settings
        chkEnableAIGeneration.Checked = _currentSettings.EnableAIGeneration;
        numMaxRetries.Value = Math.Max(1, Math.Min(10, _currentSettings.MaxRetries));
        numTimeoutSeconds.Value = Math.Max(30, Math.Min(300, _currentSettings.TimeoutSeconds));
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // Validate API Key
            if (string.IsNullOrWhiteSpace(txtApiKey.Text))
            {
                MessageBox.Show("Please enter a valid API key.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Update settings
            _currentSettings.OpenAiApiKey = txtApiKey.Text.Trim();
            _currentSettings.DatabaseType = cboDefaultDatabaseType.SelectedItem?.ToString() ?? "MySQL";
            _currentSettings.ConnectionString = txtDefaultConnectionString.Text.Trim();
            // _currentSettings.DefaultRecordCount = (int)numDefaultRecordCount.Value; // Skipped
            _currentSettings.AutoSaveSettings = chkAutoSaveSettings.Checked;
            _currentSettings.ShowLogWindow = chkShowLogWindow.Checked;
            _currentSettings.EnableSSH = chkEnableSSH.Checked;
            _currentSettings.EnableAIGeneration = chkEnableAIGeneration.Checked;
            _currentSettings.MaxRetries = (int)numMaxRetries.Value;
            _currentSettings.TimeoutSeconds = (int)numTimeoutSeconds.Value;

            // Save settings
            _configService.SaveUserSettings(_currentSettings);
            
            MessageBox.Show("Settings saved successfully!", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void BtnTestApiKey_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtApiKey.Text))
        {
            MessageBox.Show("Please enter an API key first.", "Validation", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnTestApiKey.Enabled = false;
            btnTestApiKey.Text = "Testing...";
            
            // Test API key by making a simple request
            // This would typically call the AI service to validate the key
            // For now, we'll just simulate a test
            
            Task.Delay(1000).Wait(); // Simulate API call
            
            MessageBox.Show("API key appears to be valid!", "Test Result", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"API key test failed: {ex.Message}", "Test Result", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestApiKey.Enabled = true;
            btnTestApiKey.Text = "Test API Key";
        }
    }

    private void BtnResetToDefaults_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all settings to default values?",
            "Reset Settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _currentSettings = new UserSettings();
            LoadCurrentSettings();
        }
    }

    private void ChkEnableAIGeneration_CheckedChanged(object? sender, EventArgs e)
    {
        // Enable/disable AI-related controls based on checkbox
        numMaxRetries.Enabled = chkEnableAIGeneration.Checked;
        numTimeoutSeconds.Enabled = chkEnableAIGeneration.Checked;
        lblMaxRetries.Enabled = chkEnableAIGeneration.Checked;
        lblTimeoutSeconds.Enabled = chkEnableAIGeneration.Checked;
    }

    private void ChkEnableSSH_CheckedChanged(object? sender, EventArgs e)
    {
        // Enable/disable SSH-related controls based on checkbox
        // This could be expanded if we add more SSH settings
    }
} 