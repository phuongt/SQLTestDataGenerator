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
    
    // API Status Controls
    private Label lblApiModel = null!;
    private Label lblApiStatus = null!;
    private Label lblDailyUsage = null!;
    private System.Windows.Forms.Timer apiStatusTimer = null!;
    
    // SSH Controls
    private CheckBox chkUseSSH = null!;
    private GroupBox grpSSH = null!;
    private TextBox txtSSHHost = null!;
    private NumericUpDown numSSHPort = null!;
    private TextBox txtSSHUsername = null!;
    private TextBox txtSSHPassword = null!;
    private TextBox txtRemoteDbHost = null!;
    private NumericUpDown numRemoteDbPort = null!;
    private Button btnTestSSH = null!;
    private Label lblSSHStatus = null!;
    
    // SQL Export tracking
    private string _lastGeneratedSqlFilePath = string.Empty;

    public MainForm()
    {
        InitializeComponent();
        _configService = new ConfigurationService();
    }

    private void InitializeComponent()
    {
        this.Text = "SQL Test Data Generator (Gemini AI) - SSH Tunnel Support";
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1150, 750);

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
        cboDbType.SelectedIndex = 2; // Default to MySQL
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
            Text = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=password;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;"
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

        // SSH Configuration
        chkUseSSH = new CheckBox
        {
            Text = "🔐 Use SSH Tunnel",
            Location = new Point(870, 65),
            Size = new Size(150, 25),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 150, 243)
        };
        chkUseSSH.CheckedChanged += chkUseSSH_CheckedChanged;

        grpSSH = new GroupBox
        {
            Text = "SSH Tunnel Configuration",
            Location = new Point(880, 95),
            Size = new Size(250, 200),
            Font = new Font("Segoe UI", 9F),
            Visible = false
        };

        // SSH Host
        var lblSSHHost = new Label
        {
            Text = "SSH Host:",
            Location = new Point(10, 25),
            Size = new Size(70, 20),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH
        };

        txtSSHHost = new TextBox
        {
            Location = new Point(85, 23),
            Size = new Size(150, 23),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH,
            PlaceholderText = "ssh.server.com"
        };

        // SSH Port
        var lblSSHPort = new Label
        {
            Text = "SSH Port:",
            Location = new Point(10, 50),
            Size = new Size(70, 20),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH
        };

        numSSHPort = new NumericUpDown
        {
            Location = new Point(85, 48),
            Size = new Size(80, 23),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH,
            Minimum = 1,
            Maximum = 65535,
            Value = 22
        };

        // SSH Username
        var lblSSHUsername = new Label
        {
            Text = "Username:",
            Location = new Point(10, 75),
            Size = new Size(70, 20),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH
        };

        txtSSHUsername = new TextBox
        {
            Location = new Point(85, 73),
            Size = new Size(150, 23),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH,
            PlaceholderText = "ubuntu"
        };

        // SSH Password
        var lblSSHPassword = new Label
        {
            Text = "Password:",
            Location = new Point(10, 100),
            Size = new Size(70, 20),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH
        };

        txtSSHPassword = new TextBox
        {
            Location = new Point(85, 98),
            Size = new Size(150, 23),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH,
            UseSystemPasswordChar = true,
            PlaceholderText = "ssh password"
        };

        // Remote DB Host
        var lblRemoteDbHost = new Label
        {
            Text = "Remote DB:",
            Location = new Point(10, 125),
            Size = new Size(70, 20),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH
        };

        txtRemoteDbHost = new TextBox
        {
            Location = new Point(85, 123),
            Size = new Size(100, 23),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH,
            Text = "localhost",
            PlaceholderText = "localhost"
        };

        // Remote DB Port
        var lblRemoteDbPort = new Label
        {
            Text = "Port:",
            Location = new Point(190, 125),
            Size = new Size(30, 20),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH
        };

        numRemoteDbPort = new NumericUpDown
        {
            Location = new Point(190, 148),
            Size = new Size(55, 23),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH,
            Minimum = 1,
            Maximum = 65535,
            Value = 3306
        };

        // Test SSH Button
        btnTestSSH = new Button
        {
            Text = "🧪 Test SSH",
            Location = new Point(85, 148),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH,
            BackColor = Color.FromArgb(255, 152, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnTestSSH.FlatAppearance.BorderSize = 0;
        btnTestSSH.Click += btnTestSSH_Click;

        // SSH Status
        lblSSHStatus = new Label
        {
            Text = "SSH: Not connected",
            Location = new Point(10, 175),
            Size = new Size(230, 20),
            Font = new Font("Segoe UI", 8F),
            Parent = grpSSH,
            ForeColor = Color.Gray
        };

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
            Text = "-- Generic example: Select active users\nSELECT id, name, email, created_at, status\nFROM users \nWHERE status = 'active'\nORDER BY created_at DESC",
            BorderStyle = BorderStyle.FixedSingle
        };

        // Records and Generate Section
        var lblRecords = new Label
        {
            Text = "Desired Records:",
            Location = new Point(20, 440),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };

        numRecords = new NumericUpDown
        {
            Location = new Point(150, 440),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 9F),
            Minimum = 1,
            Maximum = 1000,
            Value = 10
        };

        // Generate Button với chiều cao tăng lên
        btnGenerateData = new Button
        {
            Text = "🚀 Generate Test Data",
            Location = new Point(270, 440),
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
            Location = new Point(450, 440),
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
            Location = new Point(630, 440),
            Size = new Size(170, 42),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.Gray, // Initially gray because disabled
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Default, // Default cursor when disabled
            TextAlign = ContentAlignment.MiddleCenter,
            Enabled = false // Disabled initially
        };
        btnExecuteFromFile.FlatAppearance.BorderSize = 0;
        btnExecuteFromFile.Click += btnExecuteFromFile_Click;
        btnExecuteFromFile.EnabledChanged += UpdateCommitButtonAppearance;

        // Progress Bar
        progressBar = new ProgressBar
        {
            Location = new Point(820, 447),
            Size = new Size(220, 25),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Visible = false
        };

        // Results Section Header - positioned after buttons
        var lblResults = new Label
        {
            Text = "Results:",
            Location = new Point(20, 495),
            Size = new Size(200, 30),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(33, 33, 33)
        };

        // DataGridView - positioned with proper spacing from Results header
        dataGridView = new DataGridView
        {
            Location = new Point(20, 530),
            Size = new Size(1090, 140),
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
            Size = new Size(500, 30),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(102, 102, 102),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        // API Status Controls - positioned between SQL editor and Records section
        lblApiModel = new Label
        {
            Text = "🤖 AI Model: Initializing...",
            Location = new Point(20, 375),
            Size = new Size(200, 40),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(33, 150, 243),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        lblApiStatus = new Label
        {
            Text = "🔄 Status: Ready",
            Location = new Point(230, 375),
            Size = new Size(150, 40),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(76, 175, 80),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        lblDailyUsage = new Label
        {
            Text = "📊 Daily: 0/100",
            Location = new Point(390, 375),
            Size = new Size(120, 40),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(102, 102, 102),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        // Timer for updating API status
        apiStatusTimer = new System.Windows.Forms.Timer
        {
            Interval = 2000, // Update every 2 seconds
            Enabled = false
        };
        apiStatusTimer.Tick += UpdateApiStatus;

        // Add all controls to form
        this.Controls.AddRange(new Control[]
        {
            lblDbType, cboDbType,
            lblConnection, txtConnectionString, btnTestConnection,
            chkUseSSH, grpSSH,
            lblSqlQuery, sqlEditor,
            lblRecords, numRecords, btnGenerateData, btnRunQuery, btnExecuteFromFile, progressBar,
            lblResults, dataGridView, 
            lblStatus, lblGenerateStats,
            lblApiModel, lblApiStatus, lblDailyUsage
        });

        this.Load += MainForm_Load;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        LoadSettings();
        UpdateConnectionStringTemplate();
        InitializeEngineService();
        
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
            else
            {
                // Default to MySQL if no saved database type
                var mysqlIndex = cboDbType.Items.IndexOf("MySQL");
                if (mysqlIndex >= 0) cboDbType.SelectedIndex = mysqlIndex;
            }
            
            if (!string.IsNullOrEmpty(settings.ConnectionString))
                txtConnectionString.Text = settings.ConnectionString;
                
            if (!string.IsNullOrEmpty(settings.LastQuery))
                sqlEditor.Text = settings.LastQuery;
                
            numRecords.Value = Math.Max(1, Math.Min(1000, settings.DefaultRecordCount));
        }
        else
        {
            // No settings found - set MySQL as default
            var mysqlIndex = cboDbType.Items.IndexOf("MySQL");
            if (mysqlIndex >= 0) cboDbType.SelectedIndex = mysqlIndex;
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
        // Force update to default MySQL connection for fresh start
        if (cboDbType.Text == "MySQL")
        {
            txtConnectionString.Text = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;";
            sqlEditor.Text = @"  -- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
        SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
               c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
               CASE 
                   WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                   WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                   ELSE 'Đang làm việc'
               END AS work_status
        FROM users u
        INNER JOIN companies c ON u.company_id = c.id
        INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = False
        INNER JOIN roles r ON ur.role_id = r.id
        WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
          AND YEAR(u.date_of_birth) = 1989
          AND c.NAME LIKE '%HOME%'
          AND r.code LIKE '%member%'
          AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
        ORDER BY ur.expires_at ASC, u.created_at DESC";
        }
        else
        {
            // Chỉ cập nhật nếu chưa có kết nối tùy chỉnh cho other database types
            if (string.IsNullOrWhiteSpace(txtConnectionString.Text) || 
                txtConnectionString.Text.Contains("Server=.;Database=TestDB") ||
                txtConnectionString.Text.Contains("Host=localhost") ||
                false)
            {
                txtConnectionString.Text = cboDbType.Text switch
                {
                    "SQL Server" => "Server=localhost;Database=TestDB;User Id=sa;Password=yourpassword;TrustServerCertificate=true;",
                    "PostgreSQL" => "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password;",
                    _ => txtConnectionString.Text
                };
            }
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

            // Check if SSH tunnel should be used
            string connectionStringToUse = txtConnectionString.Text;
            
            if (chkUseSSH.Checked)
            {
                if (_sshTunnelService?.IsConnected != true)
                {
                    lblStatus.Text = "❌ SSH tunnel not connected! Please test SSH connection first.";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show("❌ SSH Tunnel chưa kết nối!\n\nVui lòng:\n1. Configure SSH settings\n2. Click 'Test SSH' để tạo tunnel\n3. Sau đó test database connection", 
                        "SSH Tunnel Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Use SSH tunnel connection string
                try
                {
                    connectionStringToUse = _sshTunnelService.GetTunnelConnectionString(
                        DbConnectionFactory.ExtractDatabaseName(txtConnectionString.Text) ?? "my_database",
                        DbConnectionFactory.ExtractUsername(txtConnectionString.Text) ?? "root", 
                        DbConnectionFactory.ExtractPassword(txtConnectionString.Text) ?? "password"
                    );
                    
                    lblStatus.Text = $"🔍 Testing connection via SSH tunnel (port {_sshTunnelService.LocalPort})...";
                    lblStatus.ForeColor = Color.Blue;
                    Application.DoEvents();
                }
                catch (Exception sshEx)
                {
                    lblStatus.Text = $"❌ SSH tunnel connection string error: {sshEx.Message}";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show($"❌ Lỗi tạo connection string qua SSH:\n\n{sshEx.Message}", 
                        "SSH Connection String Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (cboDbType.Text == "MySQL")
            {
                if (!chkUseSSH.Checked)
                {
                    lblStatus.Text = "🔍 Testing direct MySQL connection...";
                }
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();
                
                // Test basic connection first
                try
                {
                    using var testConnection = new MySqlConnection(connectionStringToUse);
                    await testConnection.OpenAsync();
                    
                    var connectionType = chkUseSSH.Checked ? "SSH tunneled" : "direct";
                    Console.WriteLine($"Basic MySQL {connectionType} connection successful");
                    
                    lblStatus.Text = $"✅ MySQL {connectionType} connection successful!";
                    lblStatus.ForeColor = Color.Green;
                }
                catch (Exception mysqlEx)
                {
                    var connectionType = chkUseSSH.Checked ? "SSH tunneled" : "direct";
                    lblStatus.Text = $"❌ MySQL {connectionType} Connection Error: {mysqlEx.Message}";
                    lblStatus.ForeColor = Color.Red;
                    
                    var troubleshoot = chkUseSSH.Checked 
                        ? "• SSH tunnel đang hoạt động?\n• Remote DB host/port đúng?\n• Database credentials đúng?" 
                        : "• Server có online?\n• Port 3306 mở?\n• Username/Password đúng?";
                        
                    MessageBox.Show($"❌ Lỗi kết nối MySQL {connectionType}:\n\n{mysqlEx.Message}\n\nKiểm tra:\n{troubleshoot}", 
                        "MySQL Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            var success = await _engineService.TestConnectionAsync(cboDbType.Text, connectionStringToUse, _sshTunnelService);
            
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
                // For MySQL, ensure foreign key checks are disabled during execution
                if (cboDbType.Text.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 0;", transaction: transaction, commandTimeout: 300);
                    Console.WriteLine($"[MainForm] Disabled foreign key checks for MySQL");
                }

                int executedCount = 0;
                var tableInsertCounts = new Dictionary<string, int>();
                
                foreach (var sqlStatement in sqlStatements)
                {
                    if (string.IsNullOrWhiteSpace(sqlStatement)) continue;
                    
                    // Skip foreign key check statements if they're already in the file to avoid duplication
                    var trimmedStatement = sqlStatement.Trim();
                    if (trimmedStatement.StartsWith("SET FOREIGN_KEY_CHECKS", StringComparison.OrdinalIgnoreCase) ||
                        trimmedStatement.StartsWith("-- "))
                    {
                        continue;
                    }
                    
                    // Extract table name from INSERT statement for counting
                    if (trimmedStatement.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
                    {
                        var tableName = ExtractTableNameFromInsert(trimmedStatement);
                        if (!string.IsNullOrEmpty(tableName))
                        {
                            tableInsertCounts[tableName] = tableInsertCounts.GetValueOrDefault(tableName, 0) + 1;
                        }
                    }
                    
                    Console.WriteLine($"[MainForm] Executing: {sqlStatement.Substring(0, Math.Min(100, sqlStatement.Length))}...");
                    await connection.ExecuteAsync(sqlStatement, transaction: transaction, commandTimeout: 300);
                    executedCount++;
                }

                // Re-enable foreign key checks for MySQL before committing
                if (cboDbType.Text.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 1;", transaction: transaction, commandTimeout: 300);
                    Console.WriteLine($"[MainForm] Re-enabled foreign key checks for MySQL");
                }

                // Commit transaction
                transaction.Commit();
                Console.WriteLine($"[MainForm] Successfully executed and committed {executedCount} SQL statements");

                // Run the original query to verify results  
                lblStatus.Text = "🔍 Verifying data by running original query...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();
                
                var queryResult = await connection.QueryAsync(sqlEditor.Text, commandTimeout: 300);
                var resultTable = CreateDataTableFromDynamic(queryResult);
                dataGridView.DataSource = resultTable;

                lblStatus.Text = $"✅ Committed {executedCount} SQL statements successfully! Verification: {resultTable.Rows.Count} matching records found";
                lblStatus.ForeColor = Color.Green;

                lblGenerateStats.Text = $"✅ SQL Committed: {executedCount} statements | Verification query: {resultTable.Rows.Count} records found | Data permanently saved";

                // Build detailed insert breakdown
                var insertBreakdown = "";
                if (tableInsertCounts.Any())
                {
                    insertBreakdown = "\n📊 Chi tiết INSERT theo table:\n";
                    foreach (var kvp in tableInsertCounts.OrderBy(x => x.Key))
                    {
                        insertBreakdown += $"   • {kvp.Key}: {kvp.Value} record(s)\n";
                    }
                    insertBreakdown += "\n";
                }

                // Show success message with detailed table breakdown
                var successMessage = $"🎉 Execute SQL File thành công!\n\n" +
                                   $"💾 COMMIT DETAILS:\n" +
                                   $"• Đã thực thi {executedCount} câu lệnh SQL từ file\n" +
                                   $"• File: {Path.GetFileName(_lastGeneratedSqlFilePath)}\n" +
                                   insertBreakdown +
                                   $"🔍 VERIFICATION RESULTS:\n" +
                                   $"• Chạy lại câu query gốc: {resultTable.Rows.Count} records found\n" +
                                   $"• Trạng thái: Dữ liệu đã được LÂN VÀO DATABASE\n\n" +
                                   $"✅ Tất cả dữ liệu đã được lưu vào database thật!\n" +
                                   $"✅ Query verification cho thấy {resultTable.Rows.Count} matching records!\n\n" +
                                   $"💾 Button Commit sẽ được DISABLE vì data đã commit xong.\n" +
                                   $"🔄 Để commit tiếp, hãy Generate data mới!";

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
            // After successful commit, clear the file path so button becomes disabled
            _lastGeneratedSqlFilePath = string.Empty;
            
            // Re-enable other buttons
            btnGenerateData.Enabled = true;
            btnRunQuery.Enabled = true;
            progressBar.Visible = false;
            
            // Update commit button state based on file availability
            btnExecuteFromFile.Enabled = !string.IsNullOrEmpty(_lastGeneratedSqlFilePath);
        }
    }

    /// <summary>
    /// Update Commit button appearance based on enabled state
    /// </summary>
    private void UpdateCommitButtonAppearance(object? sender, EventArgs e)
    {
        if (btnExecuteFromFile.Enabled)
        {
            // Enabled state: Green background, hand cursor
            btnExecuteFromFile.BackColor = Color.FromArgb(76, 175, 80); // Green
            btnExecuteFromFile.ForeColor = Color.White;
            btnExecuteFromFile.Cursor = Cursors.Hand;
            btnExecuteFromFile.Text = "💾 Commit";
        }
        else
        {
            // Disabled state: Gray background, default cursor
            btnExecuteFromFile.BackColor = Color.Gray;
            btnExecuteFromFile.ForeColor = Color.LightGray;
            btnExecuteFromFile.Cursor = Cursors.Default;
            btnExecuteFromFile.Text = "💾 Commit (No File)";
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

    /// <summary>
    /// Extract table name from INSERT statement for detailed commit reporting
    /// </summary>
    private static string ExtractTableNameFromInsert(string insertStatement)
    {
        try
        {
            // Pattern: INSERT INTO table_name (...) VALUES (...)
            var match = System.Text.RegularExpressions.Regex.Match(insertStatement, 
                @"INSERT\s+INTO\s+[`'""]?([a-zA-Z_][a-zA-Z0-9_]*)[`'""]?\s*\(", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            // Alternative pattern: INSERT INTO table_name VALUES (...)
            match = System.Text.RegularExpressions.Regex.Match(insertStatement, 
                @"INSERT\s+INTO\s+[`'""]?([a-zA-Z_][a-zA-Z0-9_]*)[`'""]?\s+VALUES", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        catch
        {
            // If regex fails, fall back to simple parsing
        }
        
        return string.Empty;
    }

    #region SSH Tunnel Event Handlers

    /// <summary>
    /// Handle SSH checkbox change event
    /// </summary>
    private void chkUseSSH_CheckedChanged(object? sender, EventArgs e)
    {
        grpSSH.Visible = chkUseSSH.Checked;
        
        if (chkUseSSH.Checked)
        {
            lblStatus.Text = "SSH Tunnel enabled - Configure SSH settings and test connection";
            lblStatus.ForeColor = Color.FromArgb(33, 150, 243);
        }
        else
        {
            // Close existing SSH tunnel if any
            _sshTunnelService?.CloseTunnel();
            _sshTunnelService?.Dispose();
            _sshTunnelService = null;
            
            lblSSHStatus.Text = "SSH: Not connected";
            lblSSHStatus.ForeColor = Color.Gray;
            
            lblStatus.Text = "SSH Tunnel disabled - Using direct database connection";
            lblStatus.ForeColor = Color.FromArgb(102, 102, 102);
        }
    }

    /// <summary>
    /// Handle SSH connection test
    /// </summary>
    private async void btnTestSSH_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSSHHost.Text) || 
            string.IsNullOrWhiteSpace(txtSSHUsername.Text) || 
            string.IsNullOrWhiteSpace(txtSSHPassword.Text))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ thông tin SSH:\n• SSH Host\n• Username\n• Password", 
                "SSH Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnTestSSH.Enabled = false;
            lblSSHStatus.Text = "SSH: Connecting...";
            lblSSHStatus.ForeColor = Color.Orange;
            Application.DoEvents();

            // Close existing tunnel if any
            _sshTunnelService?.CloseTunnel();
            _sshTunnelService?.Dispose();
            
            // Create new SSH tunnel service
            _sshTunnelService = new SshTunnelService();

            // Test SSH connection first
            var sshConnected = await _sshTunnelService.TestSshConnectionAsync(
                txtSSHHost.Text.Trim(),
                (int)numSSHPort.Value,
                txtSSHUsername.Text.Trim(),
                txtSSHPassword.Text
            );

            if (!sshConnected)
            {
                lblSSHStatus.Text = "SSH: Connection failed";
                lblSSHStatus.ForeColor = Color.Red;
                MessageBox.Show("❌ SSH connection failed!\n\nKiểm tra:\n• SSH host và port\n• Username/password\n• Network connectivity", 
                    "SSH Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Create SSH tunnel
            var tunnelCreated = await _sshTunnelService.CreateTunnelAsync(
                txtSSHHost.Text.Trim(),
                (int)numSSHPort.Value,
                txtSSHUsername.Text.Trim(),
                txtSSHPassword.Text,
                txtRemoteDbHost.Text.Trim(),
                (int)numRemoteDbPort.Value
            );

            if (tunnelCreated && _sshTunnelService.IsConnected)
            {
                lblSSHStatus.Text = $"SSH: Connected (Port {_sshTunnelService.LocalPort})";
                lblSSHStatus.ForeColor = Color.Green;
                
                lblStatus.Text = $"✅ SSH Tunnel active: localhost:{_sshTunnelService.LocalPort} -> {txtRemoteDbHost.Text}:{numRemoteDbPort.Value}";
                lblStatus.ForeColor = Color.Green;
                
                MessageBox.Show($"✅ SSH Tunnel thành công!\n\n" +
                              $"SSH Server: {txtSSHHost.Text}:{numSSHPort.Value}\n" +
                              $"Remote DB: {txtRemoteDbHost.Text}:{numRemoteDbPort.Value}\n" +
                              $"Local Port: {_sshTunnelService.LocalPort}\n\n" +
                              $"Bây giờ có thể test database connection.", 
                    "SSH Tunnel Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblSSHStatus.Text = "SSH: Tunnel failed";
                lblSSHStatus.ForeColor = Color.Red;
                MessageBox.Show("❌ SSH tunnel creation failed!\n\nKiểm tra:\n• Remote database host/port\n• SSH permissions\n• Firewall settings", 
                    "SSH Tunnel Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            lblSSHStatus.Text = "SSH: Error";
            lblSSHStatus.ForeColor = Color.Red;
            lblStatus.Text = $"❌ SSH Error: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
            
            MessageBox.Show($"❌ SSH connection error:\n\n{ex.Message}\n\nChi tiết:\n{ex.GetType().Name}", 
                "SSH Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestSSH.Enabled = true;
        }
    }

    #endregion
}
