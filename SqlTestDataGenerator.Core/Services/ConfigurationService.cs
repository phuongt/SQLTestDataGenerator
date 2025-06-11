using System.Text.Json;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Service for managing application configuration and user settings
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Load application settings from appsettings.json
    /// </summary>
    AppSettings LoadAppSettings();

    /// <summary>
    /// Load user settings from user config file
    /// </summary>
    UserSettings LoadUserSettings();

    /// <summary>
    /// Save user settings to config file
    /// </summary>
    bool SaveUserSettings(UserSettings settings);

    /// <summary>
    /// Add connection string to recent connections
    /// </summary>
    void AddRecentConnection(string connectionString);

    /// <summary>
    /// Add query to recent queries
    /// </summary>
    void AddRecentQuery(string query);

    /// <summary>
    /// Get OpenAI API key with validation
    /// </summary>
    string GetOpenAiApiKey();

    /// <summary>
    /// Validate if API key is properly configured
    /// </summary>
    bool IsApiKeyConfigured();
}

/// <summary>
/// Implementation of configuration service with secure API key management
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly string _userConfigPath;
    private readonly string _appSettingsPath;
    private readonly ILoggerService? _logger;
    private AppSettings? _cachedAppSettings;

    public ConfigurationService(ILoggerService? logger = null)
    {
        _logger = logger;
        
        _userConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "SqlTestDataGenerator", "settings.json");
            
        _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        
        _logger?.LogMethodEntry(nameof(ConfigurationService), new { _userConfigPath, _appSettingsPath });
        
        // Ensure config directory exists
        var configDir = Path.GetDirectoryName(_userConfigPath);
        if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            _logger?.LogInfo("Created config directory", "CONFIG_INIT", new { configDir });
        }
    }

    public AppSettings LoadAppSettings()
    {
        _logger?.LogMethodEntry(nameof(LoadAppSettings));
        
        try
        {
            if (_cachedAppSettings != null)
            {
                _logger?.LogMethodExit(nameof(LoadAppSettings), "CACHED");
                return _cachedAppSettings;
            }

            if (!File.Exists(_appSettingsPath))
            {
                _logger?.LogWarning("appsettings.json not found, using default settings", "CONFIG_LOAD", 
                    new { _appSettingsPath });
                _cachedAppSettings = new AppSettings();
                return _cachedAppSettings;
            }

            var json = File.ReadAllText(_appSettingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger?.LogWarning("appsettings.json is empty, using default settings", "CONFIG_LOAD");
                _cachedAppSettings = new AppSettings();
                return _cachedAppSettings;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            _cachedAppSettings = JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();
            
            _logger?.LogInfo("App settings loaded successfully", "CONFIG_LOAD", 
                new { _appSettingsPath, HasApiKey = !string.IsNullOrEmpty(_cachedAppSettings.OpenAiApiKey) });
                
            _logger?.LogMethodExit(nameof(LoadAppSettings), "SUCCESS");
            return _cachedAppSettings;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to load app settings from {_appSettingsPath}", ex, "CONFIG_LOAD");
            _cachedAppSettings = new AppSettings();
            _logger?.LogMethodExit(nameof(LoadAppSettings), "ERROR_DEFAULT");
            return _cachedAppSettings;
        }
    }

    public UserSettings LoadUserSettings()
    {
        _logger?.LogMethodEntry(nameof(LoadUserSettings));
        
        try
        {
            if (!File.Exists(_userConfigPath))
            {
                _logger?.LogInfo("User settings file not found, returning default settings", "USER_CONFIG_LOAD");
                var defaultSettings = new UserSettings();
                _logger?.LogMethodExit(nameof(LoadUserSettings), "DEFAULT");
                return defaultSettings;
            }

            var json = File.ReadAllText(_userConfigPath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);
            
            _logger?.LogInfo("User settings loaded successfully", "USER_CONFIG_LOAD", 
                new { _userConfigPath, RecentConnectionsCount = settings?.RecentConnections?.Count ?? 0 });
                
            _logger?.LogMethodExit(nameof(LoadUserSettings), "SUCCESS");
            return settings ?? new UserSettings();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to load user settings from {_userConfigPath}", ex, "USER_CONFIG_LOAD");
            _logger?.LogMethodExit(nameof(LoadUserSettings), "ERROR_DEFAULT");
            return new UserSettings();
        }
    }

    public bool SaveUserSettings(UserSettings settings)
    {
        _logger?.LogMethodEntry(nameof(SaveUserSettings), new { 
            RecentConnectionsCount = settings.RecentConnections?.Count ?? 0,
            RecentQueriesCount = settings.RecentQueries?.Count ?? 0
        });
        
        if (settings == null)
        {
            _logger?.LogError("Cannot save null user settings", null, "USER_CONFIG_SAVE");
            _logger?.LogMethodExit(nameof(SaveUserSettings), false);
            return false;
        }

        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_userConfigPath, json);
            
            _logger?.LogInfo("User settings saved successfully", "USER_CONFIG_SAVE", 
                new { _userConfigPath });
            _logger?.LogMethodExit(nameof(SaveUserSettings), true);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to save user settings to {_userConfigPath}", ex, "USER_CONFIG_SAVE");
            _logger?.LogMethodExit(nameof(SaveUserSettings), false);
            return false;
        }
    }

    public void AddRecentConnection(string connectionString)
    {
        _logger?.LogMethodEntry(nameof(AddRecentConnection), new { 
            ConnectionStringLength = connectionString?.Length ?? 0 
        });
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger?.LogWarning("Cannot add empty connection string", "RECENT_CONNECTION");
            return;
        }

        var settings = LoadUserSettings();
        
        if (!settings.RecentConnections.Contains(connectionString))
        {
            settings.RecentConnections.Insert(0, connectionString);
            
            // Keep only last 10 connections
            if (settings.RecentConnections.Count > 10)
            {
                settings.RecentConnections = settings.RecentConnections.Take(10).ToList();
            }
            
            SaveUserSettings(settings);
            _logger?.LogInfo("Added new recent connection", "RECENT_CONNECTION", 
                new { TotalConnections = settings.RecentConnections.Count });
        }
        
        _logger?.LogMethodExit(nameof(AddRecentConnection));
    }

    public void AddRecentQuery(string query)
    {
        _logger?.LogMethodEntry(nameof(AddRecentQuery), new { 
            QueryLength = query?.Length ?? 0 
        });
        
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger?.LogWarning("Cannot add empty query", "RECENT_QUERY");
            return;
        }

        var settings = LoadUserSettings();
        
        if (!settings.RecentQueries.Contains(query))
        {
            settings.RecentQueries.Insert(0, query);
            
            // Keep only last 20 queries
            if (settings.RecentQueries.Count > 20)
            {
                settings.RecentQueries = settings.RecentQueries.Take(20).ToList();
            }
            
            SaveUserSettings(settings);
            _logger?.LogInfo("Added new recent query", "RECENT_QUERY", 
                new { TotalQueries = settings.RecentQueries.Count });
        }
        
        _logger?.LogMethodExit(nameof(AddRecentQuery));
    }

    public string GetOpenAiApiKey()
    {
        _logger?.LogMethodEntry(nameof(GetOpenAiApiKey));
        
        var appSettings = LoadAppSettings();
        var apiKey = appSettings.OpenAiApiKey;
        
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "your-openai-api-key-here")
        {
            _logger?.LogWarning("OpenAI API key not configured", "API_KEY_VALIDATION");
            _logger?.LogMethodExit(nameof(GetOpenAiApiKey), "EMPTY");
            return string.Empty;
        }

        // Basic validation - OpenAI keys should start with 'sk-'
        if (!apiKey.StartsWith("sk-"))
        {
            _logger?.LogWarning("OpenAI API key format appears invalid", "API_KEY_VALIDATION", 
                new { KeyPrefix = apiKey.Length > 3 ? apiKey.Substring(0, 3) : "SHORT" });
        }

        _logger?.LogInfo("OpenAI API key retrieved", "API_KEY_VALIDATION", 
            new { HasKey = true, KeyLength = apiKey.Length });
        _logger?.LogMethodExit(nameof(GetOpenAiApiKey), "SUCCESS");
        return apiKey;
    }

    public bool IsApiKeyConfigured()
    {
        _logger?.LogMethodEntry(nameof(IsApiKeyConfigured));
        
        var apiKey = GetOpenAiApiKey();
        var isConfigured = !string.IsNullOrWhiteSpace(apiKey) && apiKey != "your-openai-api-key-here";
        
        _logger?.LogMethodExit(nameof(IsApiKeyConfigured), isConfigured);
        return isConfigured;
    }
} 