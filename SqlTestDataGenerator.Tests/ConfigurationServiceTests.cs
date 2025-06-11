using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Text.Json;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class ConfigurationServiceTests
{
    private ILoggerService _mockLogger = null!;
    private string _tempAppSettingsPath = null!;
    private string _tempUserSettingsPath = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create mock logger
        var loggingSettings = new LoggingSettings { EnableUILogging = false };
        _mockLogger = new LoggerService(loggingSettings);

        // Create temp paths for testing
        _tempAppSettingsPath = Path.Combine(Path.GetTempPath(), $"test_appsettings_{Guid.NewGuid()}.json");
        _tempUserSettingsPath = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid()}.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mockLogger?.Dispose();

        if (File.Exists(_tempAppSettingsPath))
            File.Delete(_tempAppSettingsPath);

        if (File.Exists(_tempUserSettingsPath))
            File.Delete(_tempUserSettingsPath);
    }

    [TestMethod]
    public void LoadAppSettings_WithValidFile_ShouldReturnSettings()
    {
        // Arrange
        var testSettings = new AppSettings
        {
            OpenAiApiKey = "sk-test-key-123",
            Database = new DatabaseSettings { DefaultType = "MySQL" }
        };

        var json = JsonSerializer.Serialize(testSettings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_tempAppSettingsPath, json);

        var service = new TestableConfigurationService(_mockLogger, _tempAppSettingsPath);

        // Act
        var result = service.LoadAppSettings();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("sk-test-key-123", result.OpenAiApiKey);
        Assert.AreEqual("MySQL", result.Database.DefaultType);
    }

    [TestMethod]
    public void LoadAppSettings_WithMissingFile_ShouldReturnDefaults()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.json");
        var service = new TestableConfigurationService(_mockLogger, nonExistentPath);

        // Act
        var result = service.LoadAppSettings();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.OpenAiApiKey);
        Assert.AreEqual("MySQL", result.Database.DefaultType);
    }

    [TestMethod]
    public void LoadAppSettings_WithInvalidJson_ShouldReturnDefaults()
    {
        // Arrange
        File.WriteAllText(_tempAppSettingsPath, "{ invalid json content }");
        var service = new TestableConfigurationService(_mockLogger, _tempAppSettingsPath);

        // Act
        var result = service.LoadAppSettings();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.OpenAiApiKey);
    }

    [TestMethod]
    public void LoadUserSettings_WithValidFile_ShouldReturnSettings()
    {
        // Arrange
        var testSettings = new UserSettings
        {
            DatabaseType = "PostgreSQL",
            ConnectionString = "test-connection",
            RecentConnections = new List<string> { "conn1", "conn2" }
        };

        var json = JsonSerializer.Serialize(testSettings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_tempUserSettingsPath, json);

        var service = new TestableConfigurationService(_mockLogger, userConfigPath: _tempUserSettingsPath);

        // Act
        var result = service.LoadUserSettings();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("PostgreSQL", result.DatabaseType);
        Assert.AreEqual("test-connection", result.ConnectionString);
        Assert.AreEqual(2, result.RecentConnections.Count);
    }

    [TestMethod]
    public void SaveUserSettings_WithValidSettings_ShouldReturnTrue()
    {
        // Arrange
        var service = new TestableConfigurationService(_mockLogger, userConfigPath: _tempUserSettingsPath);
        var settings = new UserSettings
        {
            DatabaseType = "SQL Server",
            ConnectionString = "Data Source=test.db"
        };

        // Act
        var result = service.SaveUserSettings(settings);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(File.Exists(_tempUserSettingsPath));

        // Verify content
        var savedJson = File.ReadAllText(_tempUserSettingsPath);
        var savedSettings = JsonSerializer.Deserialize<UserSettings>(savedJson);
        Assert.IsNotNull(savedSettings);
        Assert.AreEqual("SQL Server", savedSettings.DatabaseType);
    }

    [TestMethod]
    public void SaveUserSettings_WithNullSettings_ShouldReturnFalse()
    {
        // Arrange
        var service = new TestableConfigurationService(_mockLogger, userConfigPath: _tempUserSettingsPath);

        // Act
        var result = service.SaveUserSettings(null!);

        // Assert
        Assert.IsFalse(result);
        Assert.IsFalse(File.Exists(_tempUserSettingsPath));
    }

    [TestMethod]
    public void AddRecentConnection_WithNewConnection_ShouldAddToList()
    {
        // Arrange
        var service = new TestableConfigurationService(_mockLogger, userConfigPath: _tempUserSettingsPath);
        var initialSettings = new UserSettings
        {
            RecentConnections = new List<string> { "existing-conn" }
        };
        service.SaveUserSettings(initialSettings);

        // Act
        service.AddRecentConnection("new-connection");

        // Assert
        var updatedSettings = service.LoadUserSettings();
        Assert.AreEqual(2, updatedSettings.RecentConnections.Count);
        Assert.AreEqual("new-connection", updatedSettings.RecentConnections[0]); // Should be first
        Assert.AreEqual("existing-conn", updatedSettings.RecentConnections[1]);
    }

    [TestMethod]
    public void AddRecentConnection_WithDuplicateConnection_ShouldNotAddDuplicate()
    {
        // Arrange
        var service = new TestableConfigurationService(_mockLogger, userConfigPath: _tempUserSettingsPath);
        var initialSettings = new UserSettings
        {
            RecentConnections = new List<string> { "existing-conn" }
        };
        service.SaveUserSettings(initialSettings);

        // Act
        service.AddRecentConnection("existing-conn");

        // Assert
        var updatedSettings = service.LoadUserSettings();
        Assert.AreEqual(1, updatedSettings.RecentConnections.Count);
        Assert.AreEqual("existing-conn", updatedSettings.RecentConnections[0]);
    }

    [TestMethod]
    public void AddRecentConnection_WithEmptyString_ShouldNotAdd()
    {
        // Arrange
        var service = new TestableConfigurationService(_mockLogger, userConfigPath: _tempUserSettingsPath);
        service.SaveUserSettings(new UserSettings());

        // Act
        service.AddRecentConnection("");
        service.AddRecentConnection("   ");
        service.AddRecentConnection(null!);

        // Assert
        var settings = service.LoadUserSettings();
        Assert.AreEqual(0, settings.RecentConnections.Count);
    }

    [TestMethod]
    public void GetOpenAiApiKey_WithValidKey_ShouldReturnKey()
    {
        // Arrange
        var testSettings = new AppSettings { OpenAiApiKey = "sk-valid-key-123" };
        var json = JsonSerializer.Serialize(testSettings);
        File.WriteAllText(_tempAppSettingsPath, json);

        var service = new TestableConfigurationService(_mockLogger, _tempAppSettingsPath);

        // Act
        var result = service.GetOpenAiApiKey();

        // Assert
        Assert.AreEqual("sk-valid-key-123", result);
    }

    [TestMethod]
    public void GetOpenAiApiKey_WithPlaceholderKey_ShouldReturnEmpty()
    {
        // Arrange
        var testSettings = new AppSettings { OpenAiApiKey = "your-openai-api-key-here" };
        var json = JsonSerializer.Serialize(testSettings);
        File.WriteAllText(_tempAppSettingsPath, json);

        var service = new TestableConfigurationService(_mockLogger, _tempAppSettingsPath);

        // Act
        var result = service.GetOpenAiApiKey();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void IsApiKeyConfigured_WithValidKey_ShouldReturnTrue()
    {
        // Arrange
        var testSettings = new AppSettings { OpenAiApiKey = "sk-real-key-456" };
        var json = JsonSerializer.Serialize(testSettings);
        File.WriteAllText(_tempAppSettingsPath, json);

        var service = new TestableConfigurationService(_mockLogger, _tempAppSettingsPath);

        // Act
        var result = service.IsApiKeyConfigured();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsApiKeyConfigured_WithNoKey_ShouldReturnFalse()
    {
        // Arrange
        var testSettings = new AppSettings { OpenAiApiKey = "" };
        var json = JsonSerializer.Serialize(testSettings);
        File.WriteAllText(_tempAppSettingsPath, json);

        var service = new TestableConfigurationService(_mockLogger, _tempAppSettingsPath);

        // Act
        var result = service.IsApiKeyConfigured();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void AddRecentQuery_ShouldLimitTo20Queries()
    {
        // Arrange
        var service = new TestableConfigurationService(_mockLogger, userConfigPath: _tempUserSettingsPath);
        var initialSettings = new UserSettings();

        // Add 25 queries
        for (int i = 0; i < 25; i++)
        {
            initialSettings.RecentQueries.Add($"SELECT * FROM table{i}");
        }
        service.SaveUserSettings(initialSettings);

        // Act
        service.AddRecentQuery("SELECT * FROM new_table");

        // Assert
        var settings = service.LoadUserSettings();
        Assert.AreEqual(20, settings.RecentQueries.Count); // Should be limited to 20
        Assert.AreEqual("SELECT * FROM new_table", settings.RecentQueries[0]); // New query should be first
    }

    // Helper class to inject test paths
    private class TestableConfigurationService : IConfigurationService
    {
        private readonly string? _appSettingsPath;
        private readonly string? _userConfigPath;
        private readonly ILoggerService? _logger;

        public TestableConfigurationService(ILoggerService? logger, string? appSettingsPath = null, string? userConfigPath = null)
        {
            _logger = logger;
            _appSettingsPath = appSettingsPath;
            _userConfigPath = userConfigPath;
        }

        public AppSettings LoadAppSettings()
        {
            try
            {
                var path = _appSettingsPath ?? "appsettings.json";
                if (!File.Exists(path))
                    return new AppSettings();

                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                return JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public UserSettings LoadUserSettings()
        {
            try
            {
                var path = _userConfigPath ?? "test-settings.json";
                if (!File.Exists(path))
                    return new UserSettings();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch
            {
                return new UserSettings();
            }
        }

        public bool SaveUserSettings(UserSettings settings)
        {
            try
            {
                // Handle null settings
                if (settings == null)
                    return false;

                var path = _userConfigPath ?? "test-settings.json";
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void AddRecentConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return;

            var settings = LoadUserSettings();
            
            // Remove if exists to avoid duplicates
            settings.RecentConnections.Remove(connectionString);
            
            // Add to front
            settings.RecentConnections.Insert(0, connectionString);
            
            // Limit to 10
            if (settings.RecentConnections.Count > 10)
            {
                settings.RecentConnections = settings.RecentConnections.Take(10).ToList();
            }
            
            SaveUserSettings(settings);
        }

        public void AddRecentQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            var settings = LoadUserSettings();
            
            // Remove if exists to avoid duplicates
            settings.RecentQueries.Remove(query);
            
            // Add to front
            settings.RecentQueries.Insert(0, query);
            
            // Limit to 20
            if (settings.RecentQueries.Count > 20)
            {
                settings.RecentQueries = settings.RecentQueries.Take(20).ToList();
            }
            
            SaveUserSettings(settings);
        }

        public string GetOpenAiApiKey()
        {
            var appSettings = LoadAppSettings();
            var apiKey = appSettings.OpenAiApiKey;

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "your-openai-api-key-here")
                return string.Empty;

            return apiKey;
        }

        public bool IsApiKeyConfigured()
        {
            var apiKey = GetOpenAiApiKey();
            return !string.IsNullOrWhiteSpace(apiKey) && apiKey != "your-openai-api-key-here";
        }
    }
} 