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
    
    // UI Controls
    private ComboBox cboDbType = null!;
    private TextBox txtConnectionString = null!;
    private TextBox sqlEditor = null!;
    private NumericUpDown numRecords = null!;
    private Button btnGenerateData = null!;
    private Button btnRunQuery = null!;
    private Button btnTestConnection = null!;
    private Button btnExecuteFromFile = null!;
    private DataGridView dataGridView = null!;
    private Label lblStatus = null!;
    private Label lblGenerateStats = null!;
    private ProgressBar progressBar = null!;
    
    // SQL Export tracking
    private string _lastGeneratedSqlFilePath = string.Empty;

    public MainForm()
    {
        InitializeComponent();
        _configService = new ConfigurationService();
    }

    private void InitializeComponent()
    {
        this.Text = "SQL Test Data Generator (Gemini AI)";
        this.Size = new Size(1150, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1100, 750);

        // Database Type
        var lblDbType = new Label
        {
            Text = "Database Type:",
            Location = new Point(20, 20),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };

        cboDbType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(150, 18),
            Size = new Size(200, 28),
            Font = new Font("Segoe UI", 9F)
        };
        cboDbType.Items.AddRange(new object[] { "SQL Server", "MySQL", "PostgreSQL" });
        cboDbType.SelectedIndex = 0;
        cboDbType.SelectedIndexChanged += (s, e) => UpdateConnectionStringTemplate();

        // Connection String
        var lblConnection = new Label
        {
            Text = "Connection String:",
            Location = new Point(20, 65),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };

        txtConnectionString = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(150, 60),
            Size = new Size(550, 80),
            Font = new Font("Consolas", 9F),
            Text = "Server=.;Database=TestDB;Trusted_Connection=true;TrustServerCertificate=true;"
        };

        btnTestConnection = new Button
        {
            Text = "🔌 Test Connection",
            Location = new Point(720, 70),
            Size = new Size(140, 40),
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnTestConnection.FlatAppearance.BorderSize = 0;
        btnTestConnection.Click += btnTestConnection_Click;

        // SQL Query
        var lblSqlQuery = new Label
        {
            Text = "SQL Query:",
            Location = new Point(20, 170),
            Size = new Size(120, 35),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };

        sqlEditor = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Location = new Point(150, 165),
            Size = new Size(710, 200),
            Font = new Font("Consolas", 10F),
            Text = "-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc\nSELECT \n    u.id, u.username, u.first_name, u.last_name, u.email,\n    u.date_of_birth, u.salary, u.department, u.hire_date,\n    c.name as company_name, c.code as company_code,\n    r.name as role_name, r.code as role_code,\n    ur.expires_at as role_expires,\n    CASE \n        WHEN u.is_active = 0 THEN 'Đã nghỉ việc'\n        WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'\n        ELSE 'Đang làm việc'\n    END as work_status\nFROM users u\nJOIN companies c ON u.company_id = c.id\nJOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE\nJOIN roles r ON ur.role_id = r.id\nWHERE \n    (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')\n    AND YEAR(u.date_of_birth) = 1989\n    AND c.name LIKE '%VNEXT%'\n    AND r.code LIKE '%DD%'\n    AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))\nORDER BY ur.expires_at ASC, u.created_at DESC",
            BorderStyle = BorderStyle.FixedSingle
        };

        // Records and Generate Section
        var lblRecords = new Label
        {
            Text = "Desired Records:",
            Location = new Point(20, 385),
            Size = new Size(120, 35),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };

        numRecords = new NumericUpDown
        {
            Location = new Point(150, 388),
            Size = new Size(100, 28),
            Font = new Font("Segoe UI", 9F),
            Minimum = 1,
            Maximum = 1000,
            Value = 10
        };

        // Generate Button với chiều cao tăng lên
        btnGenerateData = new Button
        {
            Text = "🚀 Generate Test Data",
            Location = new Point(270, 383),
            Size = new Size(170, 42),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.FromArgb(33, 150, 243),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        btnGenerateData.FlatAppearance.BorderSize = 0;
        btnGenerateData.Click += btnGenerateData_Click;

        // Run Query Button
        btnRunQuery = new Button
        {
            Text = "🚀 Run Query",
            Location = new Point(450, 383),
            Size = new Size(170, 42),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.FromArgb(33, 150, 243),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        btnRunQuery.FlatAppearance.BorderSize = 0;
        btnRunQuery.Click += btnRunQuery_Click;

        // Commit Button
        btnExecuteFromFile = new Button
        {
            Text = "💾 Commit",
            Location = new Point(630, 383),
            Size = new Size(170, 42),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Enabled = false // Disabled initially
        };
        btnExecuteFromFile.FlatAppearance.BorderSize = 0;
        btnExecuteFromFile.Click += btnExecuteFromFile_Click;

        // Progress Bar
        progressBar = new ProgressBar
        {
            Location = new Point(820, 390),
            Size = new Size(220, 25),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Visible = false
        };

        // Results Section Header - tăng khoảng cách
        var lblResults = new Label
        {
            Text = "Results:",
            Location = new Point(20, 420),
            Size = new Size(200, 42),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(33, 33, 33)
        };

        // DataGridView - điều chỉnh vị trí để không bị che
        dataGridView = new DataGridView
        {
            Location = new Point(20, 475),
            Size = new Size(1090, 200),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            GridColor = Color.LightGray
        };

        // Status Label - đặt ở cuối cùng
        lblStatus = new Label
        {
            Text = "Ready - Select database type and click Test Connection to begin",
            Location = new Point(20, 690),
            Size = new Size(1080, 30),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(102, 102, 102),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true
        };

        // Generate Stats Label
        lblGenerateStats = new Label
        {
            Text = "Generated: 0 records | Total time: 0 seconds",
            Location = new Point(20, 730),
            Size = new Size(1080, 30),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(102, 102, 102),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // Add all controls to form
        this.Controls.AddRange(new Control[]
        {
            lblDbType, cboDbType,
            lblConnection, txtConnectionString, btnTestConnection,
            lblSqlQuery, sqlEditor,
            lblRecords, numRecords, btnGenerateData, btnRunQuery, btnExecuteFromFile, progressBar,
            lblResults, dataGridView, 
            lblStatus, lblGenerateStats
        });

        this.Load += MainForm_Load;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        LoadSettings();
        UpdateConnectionStringTemplate();
        InitializeEngineService();
        
        // Chỉ hiển thị message sẵn sàng
        lblStatus.Text = "Ready - Select database type and click Test Connection to begin";
        lblStatus.ForeColor = Color.FromArgb(102, 102, 102);
    }

    private void InitializeEngineService()
    {
        var settings = _configService.LoadUserSettings();
        var apiKey = settings.OpenAiApiKey ?? "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM";
        _engineService = new EngineService(apiKey);
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
            
            if (!string.IsNullOrEmpty(settings.ConnectionString))
                txtConnectionString.Text = settings.ConnectionString;
                
            if (!string.IsNullOrEmpty(settings.LastQuery))
                sqlEditor.Text = settings.LastQuery;
                
            numRecords.Value = Math.Max(1, Math.Min(1000, settings.DefaultRecordCount));
        }
    }

    private void SaveSettings()
    {
        var settings = _configService.LoadUserSettings();
        
        settings.DatabaseType = cboDbType.Text;
        settings.ConnectionString = txtConnectionString.Text;
        settings.LastQuery = sqlEditor.Text;
        settings.DefaultRecordCount = (int)numRecords.Value;
        
        _configService.SaveUserSettings(settings);
    }

    private void UpdateConnectionStringTemplate()
    {
        // Chỉ cập nhật nếu chưa có kết nối tùy chỉnh
        if (string.IsNullOrWhiteSpace(txtConnectionString.Text) || 
            txtConnectionString.Text.Contains("Server=.;Database=TestDB") ||
            txtConnectionString.Text.Contains("Server=localhost") ||
            txtConnectionString.Text.Contains("Server=192.84.20.226") ||
            txtConnectionString.Text.Contains("Host=localhost") ||
            false)
        {
            txtConnectionString.Text = cboDbType.Text switch
            {
                "SQL Server" => "Server=localhost;Database=TestDB;User Id=sa;Password=yourpassword;TrustServerCertificate=true;",
                "MySQL" => "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;",
                "PostgreSQL" => "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password;",
                _ => txtConnectionString.Text
            };
        }
    }

    private async void btnTestConnection_Click(object? sender, EventArgs e)
    {
        if (_engineService == null)
        {
            MessageBox.Show("Engine service chưa được khởi tạo!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            btnTestConnection.Enabled = false;
            lblStatus.Text = "🔍 Testing connection...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            // Debug connection details
            Console.WriteLine($"Testing connection to: {cboDbType.Text}");
            Console.WriteLine($"Connection string: {txtConnectionString.Text}");

            if (cboDbType.Text == "MySQL")
            {
                lblStatus.Text = "🔍 Testing MySQL connection...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();
                
                // Test basic connection first
                try
                {
                    using var testConnection = new MySqlConnection(txtConnectionString.Text);
                    await testConnection.OpenAsync();
                    Console.WriteLine("Basic MySQL connection successful");
                    
                }
                catch (Exception mysqlEx)
                {
                    lblStatus.Text = $"❌ MySQL Connection Error: {mysqlEx.Message}";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show($"❌ Lỗi kết nối MySQL:\n\n{mysqlEx.Message}\n\nKiểm tra:\n• Server: 192.84.20.226 có online?\n• Port: 3306\n• Username/Password đúng?", 
                        "MySQL Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            var success = await _engineService.TestConnectionAsync(cboDbType.Text, txtConnectionString.Text);
            
            if (success)
            {
                lblStatus.Text = "✅ Connection successful! Database ready for use.";
                lblStatus.ForeColor = Color.Green;
                MessageBox.Show("✅ Kết nối thành công!\n\nDatabase đã sẵn sàng để:\n• Generate test data\n• Run queries", 
                    "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblStatus.Text = "❌ Connection failed!";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("❌ Kết nối thất bại!\n\nKiểm tra lại:\n• Connection string syntax\n• Database server status\n• Network connectivity", 
                    "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"❌ Connection Error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
            Console.WriteLine($"Connection error details: {ex}");
            MessageBox.Show($"❌ Lỗi kết nối:\n\n{ex.Message}\n\nChi tiết:\n{ex.GetType().Name}\n\nKiểm tra lại database server và connection string.", 
                "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestConnection.Enabled = true;
        }
    }



    private async Task CreateSampleMySQLTables()
    {
        try
        {
            using var connection = new MySqlConnection(txtConnectionString.Text);
            await connection.OpenAsync();

            lblStatus.Text = "📦 Creating MySQL tables: companies, roles, users...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            // 1. Companies table
            var createCompaniesTable = @"
                CREATE TABLE IF NOT EXISTS companies (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    code VARCHAR(50) UNIQUE NOT NULL,
                    address TEXT,
                    phone VARCHAR(20),
                    email VARCHAR(255),
                    industry VARCHAR(100),
                    employee_count INT DEFAULT 0,
                    is_active BOOLEAN DEFAULT TRUE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            // 2. Roles table
            var createRolesTable = @"
                CREATE TABLE IF NOT EXISTS roles (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL UNIQUE,
                    code VARCHAR(50) NOT NULL UNIQUE,
                    description TEXT,
                    level INT DEFAULT 1,
                    is_active BOOLEAN DEFAULT TRUE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            // 3. Users table
            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    username VARCHAR(100) NOT NULL UNIQUE,
                    email VARCHAR(255) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL DEFAULT 'temp_hash',
                    first_name VARCHAR(100) NOT NULL,
                    last_name VARCHAR(100) NOT NULL,
                    phone VARCHAR(20),
                    date_of_birth DATE,
                    gender ENUM('Male', 'Female', 'Other'),
                    company_id INT,
                    primary_role_id INT,
                    salary DECIMAL(15,2),
                    department VARCHAR(100),
                    hire_date DATE,
                    is_active BOOLEAN DEFAULT TRUE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE SET NULL,
                    FOREIGN KEY (primary_role_id) REFERENCES roles(id) ON DELETE SET NULL
                )";

            // 4. User_roles table
            var createUserRolesTable = @"
                CREATE TABLE IF NOT EXISTS user_roles (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    user_id INT NOT NULL,
                    role_id INT NOT NULL,
                    assigned_by INT,
                    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    expires_at TIMESTAMP NULL,
                    is_active BOOLEAN DEFAULT TRUE,
                    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
                    FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE SET NULL,
                    UNIQUE KEY unique_user_role (user_id, role_id)
                )";

            // Execute table creation
            using var cmd1 = new MySqlCommand(createCompaniesTable, connection);
            await cmd1.ExecuteNonQueryAsync();

            using var cmd2 = new MySqlCommand(createRolesTable, connection);
            await cmd2.ExecuteNonQueryAsync();

            using var cmd3 = new MySqlCommand(createUsersTable, connection);
            await cmd3.ExecuteNonQueryAsync();

            using var cmd3b = new MySqlCommand(createUserRolesTable, connection);
            await cmd3b.ExecuteNonQueryAsync();

            // Insert sample data
            lblStatus.Text = "📦 Inserting sample data...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            // Sample companies
            var insertCompanies = @"
                INSERT IGNORE INTO companies (name, code, industry, employee_count) VALUES
                ('Tech Solutions Inc', 'TSI001', 'Technology', 150),
                ('Global Consulting Ltd', 'GCL002', 'Consulting', 75),
                ('Innovation Labs', 'IL003', 'Research', 200),
                ('VNEXT Software', 'VNEXT001', 'Software Development', 300),
                ('VNEXT Solutions', 'VNEXT002', 'IT Solutions', 120)";

            // Sample roles
            var insertRoles = @"
                INSERT IGNORE INTO roles (name, code, description, level) VALUES
                ('Super Admin', 'SUPER_ADMIN', 'Full system access', 10),
                ('Manager', 'MANAGER', 'Team management', 6),
                ('Developer', 'DEVELOPER', 'Software development', 3),
                ('Junior Developer', 'JUNIOR_DEV', 'Entry-level development', 2),
                ('Digital Director', 'DD', 'Digital transformation leader', 8),
                ('Data Director', 'DD_DATA', 'Data strategy director', 8),
                ('Design Director', 'DD_DESIGN', 'Design strategy director', 7)";

            // Sample users
            var insertUsers = @"
                INSERT IGNORE INTO users (username, email, first_name, last_name, date_of_birth, company_id, primary_role_id, salary, department, hire_date, is_active) VALUES
                ('john.doe', 'john.doe@techsolutions.com', 'John', 'Doe', '1985-03-15', 1, 1, 120000.00, 'Engineering', '2020-01-15', 1),
                ('jane.smith', 'jane.smith@techsolutions.com', 'Jane', 'Smith', '1988-07-22', 1, 2, 95000.00, 'Engineering', '2019-03-10', 1),
                ('phuong.nguyen', 'phuong.nguyen@vnext.com', 'Phương', 'Nguyễn', '1989-05-20', 4, 5, 150000.00, 'Digital', '2018-06-01', 1),
                ('phuong.tran', 'phuong.tran@vnext.com', 'Minh', 'Phương', '1989-11-12', 5, 5, 145000.00, 'Strategy', '2017-09-15', 0),
                ('mike.johnson', 'mike.johnson@globalconsulting.com', 'Mike', 'Johnson', '1990-11-08', 2, 3, 85000.00, 'Development', '2021-02-20', 1)";

            using var cmd4 = new MySqlCommand(insertCompanies, connection);
            await cmd4.ExecuteNonQueryAsync();

            using var cmd5 = new MySqlCommand(insertRoles, connection);
            await cmd5.ExecuteNonQueryAsync();

            using var cmd6 = new MySqlCommand(insertUsers, connection);
            await cmd6.ExecuteNonQueryAsync();

            // Insert user roles with expiration dates
            var insertUserRoles = @"
                INSERT IGNORE INTO user_roles (user_id, role_id, assigned_by, expires_at, is_active) VALUES
                (3, 5, 1, DATE_ADD(NOW(), INTERVAL 15 DAY), 1),  -- Phương Nguyễn - DD role expires in 15 days
                (4, 5, 1, DATE_ADD(NOW(), INTERVAL -5 DAY), 0),  -- Minh Phương - DD role expired 5 days ago
                (1, 1, 1, NULL, 1),  -- John Doe - Super Admin (no expiration)
                (2, 2, 1, NULL, 1)   -- Jane Smith - Manager (no expiration)
                ";

            using var cmd7 = new MySqlCommand(insertUserRoles, connection);
            await cmd7.ExecuteNonQueryAsync();

            lblStatus.Text = "✅ MySQL tables created successfully: companies, roles, users with sample data including VNEXT employees";
            lblStatus.ForeColor = Color.Green;
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"❌ Failed to create MySQL tables: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
        }
    }

    private async void btnGenerateData_Click(object? sender, EventArgs e)
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

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        int generationAttempt = 0;
        int maxAttempts = 3;
        int totalGeneratedRecords = 0;
        
        try
        {
            // Test connection first with detailed error reporting
            lblStatus.Text = "🔍 Testing connection before generating data...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            Console.WriteLine("=== Testing connection Start ===");
            Console.WriteLine($"Database Type: {cboDbType.Text}");
            Console.WriteLine($"Connection String: {txtConnectionString.Text}");
            Console.WriteLine($"SQL Query Length: {sqlEditor.Text.Length}");

            bool connectionOk = false;
            try
            {
                connectionOk = await _engineService.TestConnectionAsync(cboDbType.Text, txtConnectionString.Text);
                Console.WriteLine($"Connection test result: {connectionOk}");
            }
            catch (Exception connEx)
            {
                Console.WriteLine($"Connection test error: {connEx}");
                lblStatus.Text = $"❌ Connection test failed: {connEx.Message}";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show($"❌ Lỗi kết nối database:\n\n{connEx.Message}\n\nChi tiết:\n{connEx.GetType().Name}\n\n" +
                              $"Vui lòng:\n1. Kiểm tra connection string\n2. Đảm bảo database server đang chạy\n3. Click 'Test Connection' trước", 
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!connectionOk)
            {
                lblStatus.Text = "❌ Connection failed! Please test connection first.";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("❌ Kết nối database thất bại!\n\nVui lòng:\n1. Click 'Test Connection' trước khi Generate Data\n2. Kiểm tra connection string\n3. Đảm bảo database server đang chạy", 
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Proceed with data generation loop
            btnGenerateData.Enabled = false;
            btnRunQuery.Enabled = false;
            progressBar.Visible = true;

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
                    OpenAiApiKey = null,
                    UseAI = false,
                    CurrentRecordCount = currentRecords // Pass current count to AI
                };

                Console.WriteLine($"Calling ExecuteQueryWithTestDataAsync (attempt {generationAttempt})...");
                Console.WriteLine($"Target records: {desiredRecords}, Current records: {currentRecords}, Need to generate: {desiredRecords - currentRecords}");

                QueryExecutionResult result;
                try
                {
                    result = await _engineService.ExecuteQueryWithTestDataAsync(request);
                    Console.WriteLine($"Execution result: Success={result.Success}, Records={result.GeneratedRecords}, Error={result.ErrorMessage}");
                }
                catch (Exception execEx)
                {
                    Console.WriteLine($"Execution error: {execEx}");
                    lblStatus.Text = $"❌ Execution failed: {execEx.Message}";
                    lblStatus.ForeColor = Color.Red;
                    
                    var errorDetails = $"❌ Lỗi thực thi Generate Data (Attempt {generationAttempt}):\n\n{execEx.Message}\n\n";
                    if (execEx.InnerException != null)
                    {
                        errorDetails += $"Chi tiết lỗi: {execEx.InnerException.Message}\n\n";
                    }
                    errorDetails += $"Loại lỗi: {execEx.GetType().Name}\n\n";
                    errorDetails += $"Kiểm tra:\n" +
                                  $"• SQL query syntax đúng chưa?\n" +
                                  $"• Table names có tồn tại không?\n" +
                                  $"• Foreign key constraints\n" +
                                  $"• Database permissions\n" +
                                  $"• Gemini API connectivity";
                    
                    MessageBox.Show(errorDetails, "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                if (!result.Success)
                {
                    lblStatus.Text = $"❌ Generate data failed: {result.ErrorMessage}";
                    lblStatus.ForeColor = Color.Red;
                    
                    var errorDetails = $"❌ Generate Test Data thất bại (Attempt {generationAttempt}):\n\n{result.ErrorMessage}\n\n";
                    errorDetails += $"Thời gian thực thi: {result.ExecutionTime.TotalSeconds:F2} giây\n\n";
                    
                    // Show generated SQL statements if available
                    if (result.GeneratedInserts != null && result.GeneratedInserts.Any())
                    {
                        errorDetails += $"Generated SQL Statements ({result.GeneratedInserts.Count}):\n";
                        for (int i = 0; i < Math.Min(5, result.GeneratedInserts.Count); i++) // Show first 5
                        {
                            errorDetails += $"{i+1}. {result.GeneratedInserts[i]}\n";
                        }
                        if (result.GeneratedInserts.Count > 5)
                        {
                            errorDetails += $"... và {result.GeneratedInserts.Count - 5} statements khác\n";
                        }
                        errorDetails += "\n";
                    }
                    
                    errorDetails += $"Kiểm tra lại:\n" +
                                  $"• SQL query syntax\n" +
                                  $"• Database connection\n" +
                                  $"• Table names trong query có tồn tại\n" +
                                  $"• Foreign key constraints\n" +
                                  $"• Gemini API key và connectivity\n" +
                                  $"• Database permissions for INSERT";
                    
                    MessageBox.Show(errorDetails, "Generate Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                totalGeneratedRecords += result.GeneratedRecords;
                finalResult = result;

                // Check how many records we actually have by running the original query
                try
                {
                    lblStatus.Text = $"🔍 Checking result count (attempt {generationAttempt})...";
                    lblStatus.ForeColor = Color.Blue;
                    Application.DoEvents();

                    var checkRequest = new QueryExecutionRequest
                    {
                        DatabaseType = cboDbType.Text,
                        ConnectionString = txtConnectionString.Text,
                        SqlQuery = sqlEditor.Text,
                        DesiredRecordCount = 0, // Just run query, don't generate
                        OpenAiApiKey = apiKey,
                        UseAI = false
                    };

                    var checkResult = await _engineService.ExecuteQueryAsync(checkRequest);
                    if (checkResult.Success)
                    {
                        currentRecords = checkResult.ResultData.Rows.Count;
                        Console.WriteLine($"Current records after attempt {generationAttempt}: {currentRecords}/{desiredRecords}");
                        
                        if (currentRecords >= desiredRecords)
                        {
                            // Success! We have enough records
                            finalResult.ResultData = checkResult.ResultData;
                            break;
                        }
                        else if (generationAttempt < maxAttempts)
                        {
                            Console.WriteLine($"Not enough records ({currentRecords}/{desiredRecords}), continuing to attempt {generationAttempt + 1}...");
                            lblGenerateStats.Text = $"Need more data: {currentRecords}/{desiredRecords} records | Attempt {generationAttempt}/{maxAttempts} | Time: {totalStopwatch.Elapsed.TotalSeconds:F1}s";
                        }
                    }
                }
                catch (Exception checkEx)
                {
                    Console.WriteLine($"Error checking record count: {checkEx.Message}");
                    // Continue with what we have
                    break;
                }
            }

            // Show final results
            if (finalResult != null && finalResult.Success)
            {
                dataGridView.DataSource = finalResult.ResultData;
                
                // Store SQL file path and enable execute button
                _lastGeneratedSqlFilePath = finalResult.ExportedFilePath ?? string.Empty;
                btnExecuteFromFile.Enabled = !string.IsNullOrEmpty(_lastGeneratedSqlFilePath);
                
                // Update button text to show filename
                if (btnExecuteFromFile.Enabled)
                {
                    var fileName = Path.GetFileName(_lastGeneratedSqlFilePath);
                    btnExecuteFromFile.Text = $"💾 Commit: {fileName}";
                }
                else
                {
                    btnExecuteFromFile.Text = "💾 Commit";
                }
                
                lblStatus.Text = $"✅ Generated {totalGeneratedRecords} records (PREVIEW ONLY - ROLLBACK) | Query returned {finalResult.ResultData.Rows.Count} rows | {totalStopwatch.ElapsedMilliseconds:F0}ms";
                lblStatus.ForeColor = Color.Green;
                
                lblGenerateStats.Text = $"✅ Completed: {generationAttempt} attempts | Generated: {totalGeneratedRecords} records (ROLLBACK) | Result: {finalResult.ResultData.Rows.Count} rows | Time: {totalStopwatch.Elapsed.TotalSeconds:F1}s";
                
                SaveSettings();
                
                // Show success details with file export info
                var fileInfo = !string.IsNullOrEmpty(_lastGeneratedSqlFilePath) 
                    ? $"📁 SQL File: {Path.GetFileName(_lastGeneratedSqlFilePath)}" 
                    : "⚠️ SQL file export failed";
                
                var successMessage = $"🎉 Generate Test Data Preview thành công!\n\n" +
                                   $"• Số lần generate: {generationAttempt} attempts\n" +
                                   $"• Đã tạo TẠM THỜI {totalGeneratedRecords} bản ghi để preview\n" +
                                   $"• Thời gian tổng cộng: {totalStopwatch.Elapsed.TotalSeconds:F2} giây\n" +
                                   $"• Kết quả truy vấn: {finalResult.ResultData.Rows.Count} dòng\n" +
                                   $"• AI Model: Google Gemini (Smart Analysis)\n" +
                                   $"• {fileInfo}\n\n" +
                                   $"🔄 Data đã được ROLLBACK - chỉ hiển thị để preview!\n" +
                                   $"🤖 AI đã phân tích SQL query và tạo data phù hợp\n" +
                                   $"📊 Dữ liệu hiển thị trên DataGridView chỉ để xem,\n" +
                                   $"    không được lưu vào database thật.\n\n" +
                                   $"💾 Để lưu data thật vào DB, click 'Commit'!";
                
                MessageBox.Show(successMessage, "Generate Data Preview (Rollback)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblStatus.Text = $"❌ Failed to generate sufficient data after {generationAttempt} attempts";
                lblStatus.ForeColor = Color.Red;
                lblGenerateStats.Text = $"❌ Failed after {generationAttempt} attempts | Generated: {totalGeneratedRecords} records | Time: {totalStopwatch.Elapsed.TotalSeconds:F1}s";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in Generate Data: {ex}");
            lblStatus.Text = $"❌ Unexpected Error: {ex.Message.Substring(0, Math.Min(ex.Message.Length, 80))}...";
            lblStatus.ForeColor = Color.Red;
            lblGenerateStats.Text = $"❌ Error after {generationAttempt} attempts | Time: {totalStopwatch.Elapsed.TotalSeconds:F1}s";
            
            var errorDetails = $"❌ Lỗi không mong đợi khi generate data:\n\n{ex.Message}\n\n";
            if (ex.InnerException != null)
            {
                errorDetails += $"Chi tiết: {ex.InnerException.Message}\n\n";
            }
            errorDetails += $"Loại lỗi: {ex.GetType().Name}\n";
            errorDetails += $"Stack trace (5 lines đầu):\n{string.Join("\n", ex.StackTrace?.Split('\n').Take(5) ?? new string[0])}\n\n";
            errorDetails += $"Vui lòng kiểm tra:\n" +
                          $"• Connection string hợp lệ\n" +
                          $"• Database accessibility\n" +
                          $"• Network connectivity\n" +
                          $"• Gemini API availability\n" +
                          $"• SQL query có đúng table structure\n" +
                          $"• Console window để xem log chi tiết";
            
            MessageBox.Show(errorDetails, "Generate Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Console.WriteLine("=== Generate Data Debug End ===");
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
            Application.DoEvents(); // Update UI

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
                dataGridView.DataSource = result.ResultData;
                
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
            lblStatus.Text = $"❌ Unexpected Error: {ex.Message.Substring(0, Math.Min(ex.Message.Length, 80))}...";
            lblStatus.ForeColor = Color.Red;
            
            var errorDetails = $"❌ Lỗi không mong đợi:\n\n{ex.Message}\n\n";
            if (ex.InnerException != null)
            {
                errorDetails += $"Chi tiết: {ex.InnerException.Message}\n\n";
            }
            errorDetails += $"Vui lòng kiểm tra:\n" +
                          $"• Connection string\n" +
                          $"• Database accessibility\n" +
                          $"• Network connectivity\n" +
                          $"• Gemini API availability";
            
            MessageBox.Show(errorDetails, "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        var items = data.ToList();
        
        if (!items.Any()) return dataTable;

        // Get column names from first row
        var first = items.First() as IDictionary<string, object>;
        if (first != null)
        {
            foreach (var key in first.Keys)
            {
                dataTable.Columns.Add(key);
            }

            // Add rows
            foreach (var item in items)
            {
                var row = dataTable.NewRow();
                var dict = item as IDictionary<string, object>;
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        row[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                }
                dataTable.Rows.Add(row);
            }
        }

        return dataTable;
    }

    private async void btnExecuteFromFile_Click(object? sender, EventArgs e)
    {
        if (_engineService == null)
        {
            MessageBox.Show("Engine service chưa được khởi tạo!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrEmpty(_lastGeneratedSqlFilePath) || !File.Exists(_lastGeneratedSqlFilePath))
        {
            MessageBox.Show("Không tìm thấy file SQL! Vui lòng Generate data trước.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnExecuteFromFile.Enabled = false;
            btnGenerateData.Enabled = false;
            btnRunQuery.Enabled = false;
            progressBar.Visible = true;
            
            lblStatus.Text = "💾 Committing SQL from file to database...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            // Read SQL statements from file
            var sqlContent = await File.ReadAllTextAsync(_lastGeneratedSqlFilePath);
            var sqlStatements = sqlContent.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Select(s => s.Trim() + ";")
                                         .ToList();

            Console.WriteLine($"[MainForm] Reading {sqlStatements.Count} SQL statements from file: {_lastGeneratedSqlFilePath}");

            using var connection = SqlTestDataGenerator.Core.Services.DbConnectionFactory.CreateConnection(cboDbType.Text, txtConnectionString.Text);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                int executedCount = 0;
                foreach (var sqlStatement in sqlStatements)
                {
                    if (string.IsNullOrWhiteSpace(sqlStatement)) continue;
                    
                    Console.WriteLine($"[MainForm] Executing: {sqlStatement.Substring(0, Math.Min(100, sqlStatement.Length))}...");
                    await connection.ExecuteAsync(sqlStatement, transaction: transaction, commandTimeout: 300);
                    executedCount++;
                }

                // Commit transaction
                transaction.Commit();
                Console.WriteLine($"[MainForm] Successfully executed and committed {executedCount} SQL statements");

                // Run the original query to show results
                var queryResult = await connection.QueryAsync(sqlEditor.Text, commandTimeout: 300);
                var resultTable = CreateDataTableFromDynamic(queryResult);
                dataGridView.DataSource = resultTable;

                lblStatus.Text = $"✅ Committed {executedCount} SQL statements successfully! Query returned {resultTable.Rows.Count} rows";
                lblStatus.ForeColor = Color.Green;

                lblGenerateStats.Text = $"✅ SQL Committed: {executedCount} statements | Result: {resultTable.Rows.Count} rows | Data saved permanently";

                // Show success message
                var successMessage = $"🎉 Execute SQL File thành công!\n\n" +
                                   $"• Đã thực thi {executedCount} câu lệnh SQL từ file\n" +
                                   $"• File: {Path.GetFileName(_lastGeneratedSqlFilePath)}\n" +
                                   $"• Kết quả truy vấn: {resultTable.Rows.Count} dòng\n" +
                                   $"• Trạng thái: Dữ liệu đã được LÂN VÀO DATABASE\n\n" +
                                   $"✅ Tất cả dữ liệu đã được lưu vào database thật!";

                MessageBox.Show(successMessage, "Execute SQL File Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"❌ Execute SQL File failed: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
            
            var errorMessage = $"❌ Lỗi khi thực thi SQL file:\n\n{ex.Message}\n\n" +
                             $"File: {Path.GetFileName(_lastGeneratedSqlFilePath)}\n" +
                             $"Vui lòng kiểm tra:\n" +
                             $"• Database connection\n" +
                             $"• SQL syntax trong file\n" +
                             $"• Table permissions\n" +
                             $"• Foreign key constraints";
            
            MessageBox.Show(errorMessage, "Execute SQL File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExecuteFromFile.Enabled = true;
            btnGenerateData.Enabled = true;
            btnRunQuery.Enabled = true;
            progressBar.Visible = false;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveSettings();
        base.OnFormClosing(e);
    }
}
