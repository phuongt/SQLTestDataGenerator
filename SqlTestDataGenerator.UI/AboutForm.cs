using System.Reflection;

namespace SqlTestDataGenerator.UI;

/// <summary>
/// Form for displaying application information and version details
/// </summary>
public partial class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();
        LoadApplicationInfo();
    }

    private void LoadApplicationInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var title = GetAssemblyAttribute<AssemblyTitleAttribute>()?.Title ?? "SQL Test Data Generator";
        var description = GetAssemblyAttribute<AssemblyDescriptionAttribute>()?.Description ?? "AI-powered SQL test data generation tool";
        var company = GetAssemblyAttribute<AssemblyCompanyAttribute>()?.Company ?? "VNEXT Software";
        var copyright = GetAssemblyAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "Copyright © 2025";

        lblAppName.Text = title;
        lblVersion.Text = $"Version {version?.ToString() ?? "1.0.0"}";
        lblDescription.Text = description;
        // lblCompany and lblCopyright are not declared in designer, so we'll skip them

        // Load additional information
        txtFeatures.Text = GetFeaturesText();
        txtTechnologies.Text = GetTechnologiesText();
    }

    private T? GetAssemblyAttribute<T>() where T : Attribute
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attributes = assembly.GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0 ? (T)attributes[0] : null;
    }

    private string GetFeaturesText()
    {
        return @"• AI-powered data generation using Gemini
• Support for multiple database types (MySQL, SQL Server, PostgreSQL, Oracle)
• SSH tunnel support for secure connections
• Real-time logging and monitoring
• Export generated SQL to files
• User-friendly Windows Forms interface
• Comprehensive error handling and validation
• Configurable settings and preferences";
    }

    private string GetTechnologiesText()
    {
        return @"• .NET 8 Windows Forms
• Google Gemini AI API
• Dapper ORM
• MySqlConnector
• System.Data.SqlClient
• Npgsql (PostgreSQL)
• Oracle.ManagedDataAccess
• SSH.NET for tunnel support
• Serilog for logging
• Newtonsoft.Json for configuration";
    }

    private void BtnClose_Click(object? sender, EventArgs e)
    {
        this.Close();
    }

    private void BtnViewLogs_Click(object? sender, EventArgs e)
    {
        try
        {
            var logPath = Path.Combine(Application.StartupPath, "logs");
            if (Directory.Exists(logPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", logPath);
            }
            else
            {
                MessageBox.Show("Log directory not found.", "Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open log directory: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnOpenWebsite_Click(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/vnext-software/sql-test-data-generator",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open website: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnCopyInfo_Click(object? sender, EventArgs e)
    {
        try
        {
            var info = $"SQL Test Data Generator\n" +
                      $"Version: {lblVersion.Text}\n" +
                      $"Company: VNEXT Software\n" +
                      $"Copyright: Copyright © 2025\n\n" +
                      $"Description: {lblDescription.Text}";

            Clipboard.SetText(info);
            MessageBox.Show("Application information copied to clipboard!", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy information: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
} 