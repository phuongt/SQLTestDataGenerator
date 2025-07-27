using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Data;
using MySqlConnector;
using Dapper;

namespace SqlTestDataGenerator.UI;

public partial class MainForm : Form
{
    private EngineService? _engineService;
    private readonly ConfigurationService _configService;
    private SshTunnelService? _sshTunnelService;
    private readonly AppSettings _appSettings = new AppSettings();

    // SQL Export tracking
    private string _lastGeneratedSqlFilePath = string.Empty;

    public MainForm()
    {
        InitializeComponent();
        _configService = new ConfigurationService();
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        LoadSettings();
        UpdateConnectionStringTemplate();
        InitializeEngineService();

        // Set button colors
        btnTestConnection.BackColor = Color.FromArgb(33, 150, 243); // Blue
        btnTestConnection.ForeColor = Color.White;
        btnTestConnection.FlatStyle = FlatStyle.Flat;

        btnGenerateData.BackColor = Color.FromArgb(76, 175, 80); // Green
        btnGenerateData.ForeColor = Color.White;
        btnGenerateData.FlatStyle = FlatStyle.Flat;

        btnRunQuery.BackColor = Color.FromArgb(255, 152, 0); // Orange
        btnRunQuery.ForeColor = Color.White;
        btnRunQuery.FlatStyle = FlatStyle.Flat;

        btnExecuteFromFile.BackColor = Color.FromArgb(156, 39, 176); // Purple
        btnExecuteFromFile.ForeColor = Color.White;
        btnExecuteFromFile.FlatStyle = FlatStyle.Flat;

        btnTestSSH.BackColor = Color.FromArgb(121, 85, 72); // Brown
        btnTestSSH.ForeColor = Color.White;
        btnTestSSH.FlatStyle = FlatStyle.Flat;

        // Chỉ hiển thị message sẵn sàng
        lblStatus.Text = "Ready - Select database type and click Test Connection to begin";
        lblStatus.ForeColor = Color.FromArgb(102, 102, 102);

        // Start API status monitoring
        apiStatusTimer.Start();
        UpdateApiStatus(null, EventArgs.Empty);
    }

    private void InitializeEngineService()
    {
        var settings = _configService.LoadUserSettings();
        var apiKey = settings.OpenAiApiKey ?? "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM";
        // Initialize with default values - will be recreated when needed with actual connection
        _engineService = new EngineService(DatabaseType.Oracle, "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=;CharSet=utf8mb4;", apiKey);
        Console.WriteLine($"EngineService initialized with API key: {apiKey.Substring(0, 10)}...");
    }

    private void LoadSettings()
    {
        var settings = _configService.LoadUserSettings();
        if (settings != null)
        {
            if (!string.IsNullOrEmpty(settings.DatabaseType))
            {
                var index = cboDbType.Items.IndexOf(settings.DatabaseType);
                if (index >= 0) cboDbType.SelectedIndex = index;
            }
            else
            {
                // Default to MySQL if no saved database type
                var oracleIndex = cboDbType.Items.IndexOf("Oracle");
                if (oracleIndex >= 0) cboDbType.SelectedIndex = oracleIndex;
            }

            if (!string.IsNullOrEmpty(settings.ConnectionString))
                txtConnectionString.Text = settings.ConnectionString;

            if (!string.IsNullOrEmpty(settings.LastQuery))
                sqlEditor.Text = settings.LastQuery;
            else
            {
                // If no saved query, use default complex SQL for selected database type
                var selectedDbType = cboDbType.SelectedItem?.ToString() ?? "Oracle";
                sqlEditor.Text = GetDefaultComplexSQL(selectedDbType);
            }

            numRecords.Value = Math.Max(1, Math.Min(1000, settings.DefaultRecordCount));
        }
        else
        {
            // No settings found - set MySQL as default
            var mysqlIndex = cboDbType.Items.IndexOf("Oracle");
            if (mysqlIndex >= 0) cboDbType.SelectedIndex = mysqlIndex;

            // Set default complex SQL for MySQL
            sqlEditor.Text = GetDefaultComplexSQL("Oracle");
        }
    }

    private void SaveSettings()
    {
        var settings = new UserSettings
        {
            DatabaseType = cboDbType.Text,
            ConnectionString = txtConnectionString.Text,
            LastQuery = sqlEditor.Text,
            DefaultRecordCount = (int)numRecords.Value
        };
        _configService.SaveUserSettings(settings);
    }

    private void UpdateConnectionStringTemplate()
    {
        var selectedDbType = cboDbType.SelectedItem?.ToString() ?? "Oracle";

        switch (selectedDbType)
        {
            case "MySQL":
                txtConnectionString.Text = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;";
                break;
            case "SQL Server":
                txtConnectionString.Text = "Server=localhost;Database=my_database;Trusted_Connection=true;Connection Timeout=120;Command Timeout=120;";
                break;
            case "PostgreSQL":
                txtConnectionString.Text = "Host=localhost;Port=5432;Database=my_database;Username=postgres;Password=password;CommandTimeout=120;";
                break;
            case "Oracle":
                txtConnectionString.Text = "Data Source=localhost:1521/XE;User Id=system;Password=22092012;Connection Timeout=120;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
                break;
        }
    }

    private static string GetDefaultComplexSQL(string databaseType)
    {
        return databaseType switch
        {
            "Oracle" => @"-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date,
       c.name AS company_name, c.code AS company_code, r.name AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE
           WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= SYSDATE + 30 THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND EXTRACT(YEAR FROM u.date_of_birth) = 1989
  AND c.name LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= SYSDATE + 60)
ORDER BY ur.expires_at ASC, u.created_at DESC",

            "MySQL" => @"-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date,
       c.name AS company_name, c.code AS company_code, r.name AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE
           WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND YEAR(u.date_of_birth) = 1989
  AND c.name LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
ORDER BY ur.expires_at ASC, u.created_at DESC",

            "SQL Server" => @"-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date,
       c.name AS company_name, c.code AS company_code, r.name AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE
           WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATEADD(day, 30, GETDATE()) THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND YEAR(u.date_of_birth) = 1989
  AND c.name LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= DATEADD(day, 60, GETDATE()))
ORDER BY ur.expires_at ASC, u.created_at DESC",

            "PostgreSQL" => @"-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date,
       c.name AS company_name, c.code AS company_code, r.name AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE
           WHEN u.is_active = false THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= NOW() + INTERVAL '30 days' THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = true
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND EXTRACT(YEAR FROM u.date_of_birth) = 1989
  AND c.name LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = false OR ur.expires_at <= NOW() + INTERVAL '60 days')
ORDER BY ur.expires_at ASC, u.created_at DESC",

            _ => @"-- Generic example: Select active users
SELECT id, name, email, created_at, status
FROM users 
WHERE status = 'active'
ORDER BY created_at DESC"
        };
    }

    private async void btnTestConnection_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtConnectionString.Text))
        {
            MessageBox.Show("Vui lòng nhập connection string!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtConnectionString.Focus();
            return;
        }

        try
        {
            btnTestConnection.Enabled = false;
            btnTestConnection.Text = "🔗 Testing...";
            lblStatus.Text = "🔄 Testing database connection...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            // Setup SSH tunnel if enabled
            if (chkUseSSH.Checked)
            {
                lblStatus.Text = "🔐 Setting up SSH tunnel...";
                Application.DoEvents();

                _sshTunnelService = new SshTunnelService();
                var tunnelSuccess = await _sshTunnelService.CreateTunnelAsync(
                    txtSSHHost.Text,
                    (int)numSSHPort.Value,
                    txtSSHUsername.Text,
                    txtSSHPassword.Text,
                    txtRemoteDbHost.Text,
                    (int)numRemoteDbPort.Value
                );

                if (!tunnelSuccess)
                {
                    lblStatus.Text = "❌ SSH tunnel failed";
                    lblStatus.ForeColor = Color.Red;
                    lblSSHStatus.Text = "SSH: Failed";
                    lblSSHStatus.ForeColor = Color.Red;
                    MessageBox.Show("SSH tunnel failed. Please check your SSH credentials and connection.", "SSH Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                lblSSHStatus.Text = "SSH: Connected";
                lblSSHStatus.ForeColor = Color.Green;
                lblStatus.Text = "✅ SSH tunnel established successfully";
            }

            // Test database connection
            lblStatus.Text = "🔗 Testing database connection...";
            Application.DoEvents();

            var connectionString = txtConnectionString.Text;
            if (chkUseSSH.Checked && _sshTunnelService != null)
            {
                connectionString = _sshTunnelService.GetTunnelConnectionString(
                    GetDatabaseNameFromConnectionString(txtConnectionString.Text),
                    GetUsernameFromConnectionString(txtConnectionString.Text),
                    GetPasswordFromConnectionString(txtConnectionString.Text)
                );
            }

            var dbType = GetDatabaseTypeFromComboBox();
            _engineService = new EngineService(dbType, connectionString);

            var connected = await _engineService.TestConnectionAsync(dbType.ToString(), connectionString);

            if (connected)
            {
                lblStatus.Text = "✅ Connection successful!";
                lblStatus.ForeColor = Color.Green;

                MessageBox.Show($"🎉 Kết nối thành công!\n\n" +
                              $"Bây giờ bạn có thể generate test data hoặc run query!",
                              "Connection Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                SaveSettings();
            }
            else
            {
                lblStatus.Text = "❌ Connection failed";
                lblStatus.ForeColor = Color.Red;

                MessageBox.Show($"❌ Lỗi kết nối:\n\n" +
                              $"Kiểm tra lại:\n" +
                              $"• Connection string syntax\n" +
                              $"• Database server status\n" +
                              $"• Username/password\n" +
                              $"• Network connectivity",
                              "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"❌ Connection error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;

            MessageBox.Show($"❌ Lỗi kết nối:\n\n{ex.Message}\n\n" +
                          $"Stack trace:\n{ex.StackTrace}",
                          "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestConnection.Enabled = true;
            btnTestConnection.Text = "🔗 Test Connection";
        }
    }

    private async void btnGenerateData_Click(object? sender, EventArgs e)
    {
        if (_engineService == null)
        {
            MessageBox.Show("Vui lòng test connection trước khi generate data!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(sqlEditor.Text))
        {
            MessageBox.Show("Vui lòng nhập SQL query!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            sqlEditor.Focus();
            return;
        }

        try
        {
            btnGenerateData.Enabled = false;
            btnRunQuery.Enabled = false;
            progressBar.Visible = true;
            lblStatus.Text = "🤖 Starting AI-powered data generation...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            var settings = _configService.LoadUserSettings();
            var apiKey = settings.OpenAiApiKey ?? "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM";

            Console.WriteLine($"Using API Key: {apiKey.Substring(0, 10)}...");
            Console.WriteLine($"Use AI: {settings?.UseAI ?? true}");
            Console.WriteLine($"Desired Records: {(int)numRecords.Value}");

            int desiredRecords = (int)numRecords.Value;
            int currentRecords = 0;
            QueryExecutionResult? finalResult = null;

            // Generation loop
            while (generationAttempt < maxAttempts)
            {
                generationAttempt++;

                lblStatus.Text = $"🤖 AI Generate attempt {generationAttempt}/{maxAttempts} (Target: {desiredRecords} records)...";
                lblStatus.ForeColor = Color.Blue;
                lblGenerateStats.Text = $"Generation attempt {generationAttempt}/{maxAttempts} | Generated: {totalGeneratedRecords} records | Time: {totalStopwatch.Elapsed.TotalSeconds:F1}s";
                Application.DoEvents();

                var request = new QueryExecutionRequest
                {
                    DatabaseType = cboDbType.Text,
                    ConnectionString = txtConnectionString.Text,
                    SqlQuery = sqlEditor.Text,
                    DesiredRecordCount = desiredRecords - currentRecords, // Generate remaining records
                    OpenAiApiKey = apiKey, // Use configured API key
                    UseAI = true, // Enable AI service for data generation
                    CurrentRecordCount = currentRecords // Pass current count to AI
                };

                Console.WriteLine($"Calling ExecuteQueryWithTestDataAsync (attempt {generationAttempt})...");
                Console.WriteLine($"Target records: {desiredRecords}, Current records: {currentRecords}, Need to generate: {desiredRecords - currentRecords}");

                // Log AI model call if using AI
                if (settings?.UseAI == true)
                {
                    var rotationService = _engineService?.DataGenService?.GeminiAIService?.FlashRotationService;
                    if (rotationService != null)
                    {
                        var currentModel = rotationService.GetCurrentModelName();
                        var promptPreview = $"Generate {desiredRecords - currentRecords} records for SQL: {sqlEditor.Text.Substring(0, Math.Min(50, sqlEditor.Text.Length))}...";
                        LogAIModelCall(currentModel, promptPreview);
                    }
                }

                QueryExecutionResult result;
                try
                {
                    result = await _engineService.ExecuteQueryWithTestDataAsync(request);
                    Console.WriteLine($"Execution result: Success={result.Success}, Records={result.GeneratedRecords}, Error={result.ErrorMessage}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during execution: {ex.Message}");
                    result = new QueryExecutionResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        GeneratedRecords = 0
                    };
                }

                if (result.Success)
                {
                    currentRecords += result.GeneratedRecords;
                    totalGeneratedRecords += result.GeneratedRecords;
                    finalResult = result;

                    lblStatus.Text = $"✅ Generated {result.GeneratedRecords} records (Total: {currentRecords}/{desiredRecords})";
                    lblStatus.ForeColor = Color.Green;
                    lblGenerateStats.Text = $"Generated: {totalGeneratedRecords} records | Time: {totalStopwatch.Elapsed.TotalSeconds:F1}s | Attempt: {generationAttempt}";

                    // Check if we have enough records
                    if (currentRecords >= desiredRecords)
                    {
                        lblStatus.Text = $"🎉 Successfully generated {currentRecords} records! (Target: {desiredRecords})";
                        lblStatus.ForeColor = Color.Green;
                        break;
                    }
                    else
                    {
                        lblStatus.Text = $"🔄 Generated {result.GeneratedRecords} records, need {desiredRecords - currentRecords} more...";
                        lblStatus.ForeColor = Color.Blue;
                    }
                }
                else
                {
                    lblStatus.Text = $"❌ Generation failed (attempt {generationAttempt}): {result.ErrorMessage}";
                    lblStatus.ForeColor = Color.Red;

                    // If it's the last attempt, show error
                    if (generationAttempt >= maxAttempts)
                    {
                        MessageBox.Show($"❌ Generate Test Data thất bại sau {maxAttempts} lần thử:\n\n" +
                                      $"Lỗi cuối cùng: {result.ErrorMessage}\n\n" +
                                      $"Tổng số records đã generate: {totalGeneratedRecords}\n" +
                                      $"Thời gian thực thi: {totalStopwatch.Elapsed.TotalSeconds:F2} giây\n\n" +
                                      $"Kiểm tra lại:\n" +
                                      $"• SQL query syntax\n" +
                                      $"• Database connection\n" +
                                      $"• Table names trong query có tồn tại\n" +
                                      $"• Foreign key constraints\n" +
                                      $"• Gemini API key và connectivity\n" +
                                      $"• Database permissions for INSERT",
                                      "Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Wait before retry
                    await Task.Delay(2000);
                }
            }

            // Final success
            if (finalResult != null && finalResult.Success)
            {
                lblStatus.Text = $"🎉 Successfully generated {currentRecords} records! Total time: {totalStopwatch.Elapsed.TotalSeconds:F2}s";
                lblStatus.ForeColor = Color.Green;

                // Show success details
                var successMessage = $"🎉 Thành công!\n\n" +
                                   $"• Records generated: {currentRecords}\n" +
                                   $"• Total time: {totalStopwatch.Elapsed.TotalSeconds:F2} giây\n" +
                                   $"• Generation attempts: {generationAttempt}\n" +
                                   $"• AI Model: Google Gemini\n\n" +
                                   $"💡 Dữ liệu đã được tạo thành công trong database.";

                MessageBox.Show(successMessage, "Generation Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                SaveSettings();
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"❌ Generation error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;

            MessageBox.Show($"❌ Lỗi generate data:\n\n{ex.Message}\n\n" +
                          $"Stack trace:\n{ex.StackTrace}",
                          "Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnGenerateData.Enabled = true;
            btnRunQuery.Enabled = true;
            progressBar.Visible = false;
        }
    }

    private async void btnRunQuery_Click(object? sender, EventArgs e)
    {
        if (_engineService == null)
        {
            MessageBox.Show("Engine service chưa được khởi tạo!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(sqlEditor.Text))
        {
            MessageBox.Show("Vui lòng nhập SQL query!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            sqlEditor.Focus();
            return;
        }

        try
        {
            btnRunQuery.Enabled = false;
            progressBar.Visible = true;
            lblStatus.Text = "🔄 Running query...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            var settings = _configService.LoadUserSettings();
            var request = new QueryExecutionRequest
            {
                DatabaseType = cboDbType.Text,
                ConnectionString = txtConnectionString.Text,
                SqlQuery = sqlEditor.Text,
                DesiredRecordCount = (int)numRecords.Value,
                OpenAiApiKey = settings?.OpenAiApiKey,
                UseAI = settings?.UseAI ?? true
            };

            lblStatus.Text = "🤖 Running query with Gemini AI...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            var result = await _engineService.ExecuteQueryAsync(request);

            if (result.Success)
            {
                lblStatus.Text = $"✅ Query executed successfully | Query returned {result.ResultData.Rows.Count} rows | {result.ExecutionTime.TotalMilliseconds:F0}ms";
                lblStatus.ForeColor = Color.Green;

                SaveSettings();

                // Show success details
                var successMessage = $"🎉 Thành công!\n\n" +
                                   $"• Thời gian thực thi: {result.ExecutionTime.TotalSeconds:F2} giây\n" +
                                   $"• Kết quả truy vấn: {result.ResultData.Rows.Count} dòng\n" +
                                   $"• AI Model: Google Gemini\n\n" +
                                   $"💡 Các kết quả đã được lưu lại trong database.";

                MessageBox.Show(successMessage, "Execution Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblStatus.Text = $"❌ Execution failed: {result.ErrorMessage}";
                lblStatus.ForeColor = Color.Red;

                MessageBox.Show($"❌ Lỗi thực thi:\n\n{result.ErrorMessage}\n\n" +
                              $"Kiểm tra lại:\n" +
                              $"• SQL query syntax\n" +
                              $"• Database connection\n" +
                              $"• Table names trong query có tồn tại",
                              "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"❌ Execution error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;

            MessageBox.Show($"❌ Lỗi thực thi:\n\n{ex.Message}\n\n" +
                          $"Stack trace:\n{ex.StackTrace}",
                          "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnRunQuery.Enabled = true;
            progressBar.Visible = false;
        }
    }

    private static DataTable CreateDataTableFromDynamic(IEnumerable<dynamic> data)
    {
        var dataTable = new DataTable();

        if (!data.Any()) return dataTable;

        // Get properties from first item
        var firstItem = data.First();
        var properties = ((IDictionary<string, object>)firstItem).Keys;

        // Add columns
        foreach (var property in properties)
        {
            dataTable.Columns.Add(property);
        }

        // Add rows
        foreach (var item in data)
        {
            var row = dataTable.NewRow();
            var itemDict = (IDictionary<string, object>)item;

            foreach (var property in properties)
            {
                row[property] = itemDict[property] ?? DBNull.Value;
            }

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    private async void btnExecuteFromFile_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_lastGeneratedSqlFilePath))
        {
            MessageBox.Show("Chưa có file SQL để execute. Vui lòng generate data trước!", "No SQL File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(_lastGeneratedSqlFilePath))
        {
            MessageBox.Show($"File SQL không tồn tại: {_lastGeneratedSqlFilePath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            btnExecuteFromFile.Enabled = false;
            progressBar.Visible = true;
            lblStatus.Text = "💾 Executing SQL from file...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            var sqlContent = await File.ReadAllTextAsync(_lastGeneratedSqlFilePath);
            var insertStatements = sqlContent.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Trim().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Trim() + ";")
                .ToList();

            if (!insertStatements.Any())
            {
                MessageBox.Show("Không tìm thấy INSERT statements trong file!", "No INSERT Statements", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblStatus.Text = $"💾 Executing {insertStatements.Count} INSERT statements...";
            Application.DoEvents();

            // Read SQL file content
            var fileContent = await File.ReadAllTextAsync(_lastGeneratedSqlFilePath);

            // Create execution request
            var request = new QueryExecutionRequest
            {
                SqlQuery = fileContent,
                DatabaseType = GetDatabaseTypeFromComboBox().ToString(),
                ConnectionString = txtConnectionString.Text,
                UseAI = false
            };

            var result = await _engineService.ExecuteQueryAsync(request);

            if (result.Success)
            {
                lblStatus.Text = $"✅ Successfully executed statements | Time: {result.ExecutionTime.TotalMilliseconds:F0}ms";
                lblStatus.ForeColor = Color.Green;

                MessageBox.Show($"🎉 Thành công!\n\n" +
                              $"• Execution time: {result.ExecutionTime.TotalSeconds:F2} giây\n" +
                              $"• File: {Path.GetFileName(_lastGeneratedSqlFilePath)}\n\n" +
                              $"💡 Dữ liệu đã được commit vào database!",
                              "Execution Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblStatus.Text = $"❌ Execution failed: {result.ErrorMessage}";
                lblStatus.ForeColor = Color.Red;

                MessageBox.Show($"❌ Lỗi thực thi:\n\n{result.ErrorMessage}", "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"❌ Execution error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;

            MessageBox.Show($"❌ Lỗi thực thi:\n\n{ex.Message}", "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExecuteFromFile.Enabled = true;
            progressBar.Visible = false;
        }
    }

    private void UpdateCommitButtonAppearance(object? sender, EventArgs e)
    {
        if (btnExecuteFromFile.Enabled)
        {
            btnExecuteFromFile.BackColor = Color.FromArgb(76, 175, 80); // Green
            btnExecuteFromFile.Cursor = Cursors.Hand;
            btnExecuteFromFile.Text = "💾 Commit";
        }
        else
        {
            btnExecuteFromFile.BackColor = Color.Gray;
            btnExecuteFromFile.Cursor = Cursors.Default;
            btnExecuteFromFile.Text = "💾 Commit (Disabled)";
        }
    }

    /// <summary>
    /// Update API status display with current model and usage information
    /// </summary>
    private void UpdateApiStatus(object? sender, EventArgs e)
    {
        try
        {
            // Access actual Gemini Flash rotation service
            var rotationService = _engineService?.DataGenService?.GeminiAIService?.FlashRotationService;

            if (rotationService != null)
            {
                // Get current model name and clean it for display
                var currentModelName = rotationService.GetCurrentModelName();
                var displayName = GetModelDisplayName(currentModelName);
                lblApiModel.Text = $"🤖 {displayName}";
                lblApiModel.ForeColor = Color.FromArgb(33, 150, 243);

                // Get daily usage statistics
                var apiStats = rotationService.GetAPIUsageStatistics();
                var dailyUsed = apiStats.ContainsKey("DailyCallsUsed") ? apiStats["DailyCallsUsed"] : 0;
                var dailyLimit = apiStats.ContainsKey("DailyCallLimit") ? apiStats["DailyCallLimit"] : 100;
                lblDailyUsage.Text = $"📊 Daily: {dailyUsed}/{dailyLimit}";
                lblDailyUsage.ForeColor = Color.FromArgb(102, 102, 102);

                // Check if API is available
                var canCall = rotationService.CanCallAPINow();
                if (canCall)
                {
                    lblApiStatus.Text = "🟢 Ready";
                    lblApiStatus.ForeColor = Color.FromArgb(76, 175, 80);
                }
                else
                {
                    lblApiStatus.Text = "⏳ Rate Limited";
                    lblApiStatus.ForeColor = Color.FromArgb(255, 152, 0);
                }

                // Update prompt display if available
                var (modelName, prompt, timestamp) = rotationService.GetCurrentPromptInfo();
                if (!string.IsNullOrEmpty(modelName) && !string.IsNullOrEmpty(prompt))
                {
                    if (txtPromptContent != null)
                    {
                        var displayModelName = GetModelDisplayName(modelName);
                        var timeStr = timestamp.ToString("HH:mm:ss");
                        var promptPreview = prompt.Length > 300 ? prompt.Substring(0, 300) + "..." : prompt;
                        var logEntry = $"[{timeStr}] 🤖 Model: {displayModelName}\n📝 Prompt: {promptPreview}";
                        txtPromptContent.Text = logEntry;
                    }
                }
            }
            else
            {
                // Fallback display when service not initialized
                lblApiModel.Text = "🤖 Initializing...";
                lblApiModel.ForeColor = Color.FromArgb(102, 102, 102);

                lblApiStatus.Text = "🔄 Loading";
                lblApiStatus.ForeColor = Color.FromArgb(102, 102, 102);

                lblDailyUsage.Text = "📊 Daily: --/--";
                lblDailyUsage.ForeColor = Color.FromArgb(102, 102, 102);
            }
        }
        catch (Exception ex)
        {
            // Silent error handling for UI updates
            Console.WriteLine($"Error updating API status: {ex.Message}");

            lblApiModel.Text = "🤖 Error";
            lblApiModel.ForeColor = Color.FromArgb(244, 67, 54);

            lblApiStatus.Text = "❌ Error";
            lblApiStatus.ForeColor = Color.FromArgb(244, 67, 54);

            lblDailyUsage.Text = "📊 --/--";
            lblDailyUsage.ForeColor = Color.FromArgb(244, 67, 54);
        }
    }

    /// <summary>
    /// Log AI model call with prompt content
    /// </summary>
    private void LogAIModelCall(string modelName, string promptContent)
    {
        try
        {
            var displayName = GetModelDisplayName(modelName);
            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Update prompt display with full information
            if (txtPromptContent != null)
            {
                var promptPreview = promptContent.Length > 300 ? promptContent.Substring(0, 300) + "..." : promptContent;
                var logEntry = $"[{timestamp}] 🤖 Model: {displayName}\n📝 Prompt: {promptPreview}";
                txtPromptContent.Text = logEntry;
            }

            // Update model label
            lblApiModel.Text = $"🤖 AI Model: {displayName}";
            lblApiModel.ForeColor = Color.FromArgb(33, 150, 243);

            // Log to console
            Console.WriteLine($"[{timestamp}] 🤖 AI Model Call: {displayName}");
            Console.WriteLine($"[{timestamp}] 📝 Prompt Preview: {promptContent.Substring(0, Math.Min(100, promptContent.Length))}...");

            // Update status
            lblStatus.Text = $"🤖 AI Model: {displayName} | Generating data...";
            lblStatus.ForeColor = Color.Blue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging AI model call: {ex.Message}");
        }
    }

    /// <summary>
    /// Get display-friendly model name with specific version info
    /// </summary>
    private static string GetModelDisplayName(string fullModelName)
    {
        if (string.IsNullOrEmpty(fullModelName))
            return "No Model";

        // Extract specific version information from full model name
        return fullModelName switch
        {
            // Gemini 2.5 Flash
            var name when name.Contains("gemini-2.5-flash") => "2.5 Flash",

            // Gemini 2.0 Flash variations
            var name when name.Contains("gemini-2.0-flash-lite") => "2.0 Flash Lite",
            var name when name.Contains("gemini-2.0-flash-exp") => "2.0 Flash Exp",
            var name when name.Contains("gemini-2.0-flash") => "2.0 Flash",

            // Gemini 1.5 Flash variations  
            var name when name.Contains("gemini-1.5-flash-8b") => "1.5 Flash 8B",
            var name when name.Contains("gemini-1.5-flash-latest") => "1.5 Flash Latest",
            var name when name.Contains("gemini-1.5-flash") => "1.5 Flash",

            // Generic fallbacks
            var name when name.Contains("flash") => "Flash Model",
            var name when name.Contains("pro") => "Pro Model",
            var name when name.Contains("gemini") => "Gemini AI",

            // Unknown models
            _ => fullModelName.Length > 15 ? fullModelName.Substring(0, 12) + "..." : fullModelName
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        apiStatusTimer?.Stop();
        SaveSettings();

        // Close SSH tunnel if connected
        try
        {
            _sshTunnelService?.CloseTunnel();
            _sshTunnelService?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing SSH tunnel: {ex.Message}");
        }

        base.OnFormClosing(e);
    }

    private static string ExtractTableNameFromInsert(string insertStatement)
    {
        try
        {
            // Simple regex to extract table name from INSERT statement
            var match = System.Text.RegularExpressions.Regex.Match(
                insertStatement,
                @"INSERT\s+INTO\s+([^\s(]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private void chkUseSSH_CheckedChanged(object? sender, EventArgs e)
    {
        grpSSH.Visible = chkUseSSH.Checked;

        if (chkUseSSH.Checked)
        {
            // Adjust form size to accommodate SSH controls
            this.Height = Math.Min(this.Height + 120, Screen.PrimaryScreen.WorkingArea.Height - 50);
        }
        else
        {
            // Reset form size
            this.Height = Math.Max(this.Height - 120, 800);

            // Close SSH tunnel if open
            try
            {
                _sshTunnelService?.CloseTunnel();
                _sshTunnelService?.Dispose();
                _sshTunnelService = null;

                lblSSHStatus.Text = "SSH: Not connected";
                lblSSHStatus.ForeColor = Color.Gray;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing SSH tunnel: {ex.Message}");
            }
        }
    }

    private void cboDbType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateConnectionStringTemplate();
        sqlEditor.Text = GetDefaultComplexSQL(cboDbType.SelectedItem?.ToString() ?? "Oracle");
    }

    private async void btnTestSSH_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSSHHost.Text) ||
            string.IsNullOrWhiteSpace(txtSSHUsername.Text) ||
            string.IsNullOrWhiteSpace(txtSSHPassword.Text))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ thông tin SSH!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnTestSSH.Enabled = false;
            btnTestSSH.Text = "🧪 Testing...";
            lblSSHStatus.Text = "SSH: Testing connection...";
            lblSSHStatus.ForeColor = Color.Blue;

            _sshTunnelService = new SshTunnelService();
            var success = await _sshTunnelService.CreateTunnelAsync(
                txtSSHHost.Text,
                (int)numSSHPort.Value,
                txtSSHUsername.Text,
                txtSSHPassword.Text,
                txtRemoteDbHost.Text,
                (int)numRemoteDbPort.Value
            );

            if (success)
            {
                lblSSHStatus.Text = "SSH: Connected successfully";
                lblSSHStatus.ForeColor = Color.Green;

                MessageBox.Show($"🎉 SSH tunnel established successfully!\n\n" +
                              $"• SSH Host: {txtSSHHost.Text}:{numSSHPort.Value}\n" +
                              $"• Remote DB: {txtRemoteDbHost.Text}:{numRemoteDbPort.Value}\n" +
                              $"• Local Port: {_sshTunnelService.LocalPort}",
                              "SSH Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblSSHStatus.Text = "SSH: Connection failed";
                lblSSHStatus.ForeColor = Color.Red;

                MessageBox.Show("❌ SSH tunnel failed. Please check your SSH credentials and connection.", "SSH Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            lblSSHStatus.Text = "SSH: Error occurred";
            lblSSHStatus.ForeColor = Color.Red;

            MessageBox.Show($"❌ SSH error:\n\n{ex.Message}", "SSH Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestSSH.Enabled = true;
            btnTestSSH.Text = "🧪 Test SSH";
        }
    }

    // Helper methods for connection string parsing
    private static string GetDatabaseNameFromConnectionString(string connectionString)
    {
        var match = System.Text.RegularExpressions.Regex.Match(connectionString, @"Database=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "test";
    }

    private static string GetUsernameFromConnectionString(string connectionString)
    {
        var match = System.Text.RegularExpressions.Regex.Match(connectionString, @"Uid=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "root";
    }

    private static string GetPasswordFromConnectionString(string connectionString)
    {
        var match = System.Text.RegularExpressions.Regex.Match(connectionString, @"Pwd=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "";
    }

    private DatabaseType GetDatabaseTypeFromComboBox()
    {
        return cboDbType.Text switch
        {
            "MySQL" => DatabaseType.MySQL,
            "SQL Server" => DatabaseType.SqlServer,
            "PostgreSQL" => DatabaseType.PostgreSQL,
            "Oracle" => DatabaseType.Oracle,
            _ => DatabaseType.Oracle
        };
    }

    // Variables for generation tracking
    private int generationAttempt = 0;
    private const int maxAttempts = 3;
    private int totalGeneratedRecords = 0;
    private readonly System.Diagnostics.Stopwatch totalStopwatch = new System.Diagnostics.Stopwatch();

    private void sqlEditor_TextChanged(object sender, EventArgs e)
    {

    }
} 