using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Centralized database configuration provider to eliminate hardcoded connection strings
/// and provide environment-specific configurations.
/// </summary>
public static class DatabaseConfigurationProvider
{
    private static readonly object _lock = new();
    private static DatabaseConfiguration? _cachedConfiguration;
    
    /// <summary>
    /// Get database configuration for current environment
    /// </summary>
    public static DatabaseConfiguration GetConfiguration(string? environment = null)
    {
        environment ??= GetCurrentEnvironment();
        
        lock (_lock)
        {
            if (_cachedConfiguration == null || _cachedConfiguration.Environment != environment)
            {
                _cachedConfiguration = LoadConfiguration(environment);
            }
            return _cachedConfiguration;
        }
    }
    
    /// <summary>
    /// Get connection string for specific database type
    /// </summary>
    public static string GetConnectionString(DatabaseType databaseType, string? environment = null)
    {
        var config = GetConfiguration(environment);
        
        return databaseType switch
        {
            DatabaseType.MySQL => config.MySQL.ConnectionString,
            DatabaseType.Oracle => config.Oracle.ConnectionString,
            DatabaseType.SqlServer => config.SqlServer.ConnectionString,
            DatabaseType.PostgreSQL => config.PostgreSQL.ConnectionString,
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
        };
    }
    
    /// <summary>
    /// Get timeout settings for database operations
    /// </summary>
    public static TimeoutSettings GetTimeoutSettings(string? environment = null)
    {
        var config = GetConfiguration(environment);
        return config.Timeouts;
    }
    
    /// <summary>
    /// Invalidate cache to force reload on next access
    /// </summary>
    public static void InvalidateCache()
    {
        lock (_lock)
        {
            _cachedConfiguration = null;
        }
    }
    
    #region Private Implementation
    
    private static string GetCurrentEnvironment()
    {
        // Check environment variable first
        var env = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? 
                 Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                 Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        
        if (!string.IsNullOrEmpty(env))
            return env;
            
        // Check if running in test context
        if (IsTestEnvironment())
            return "Test";
            
        // Check if running in CI/CD
        if (IsContinuousIntegration())
            return "CI";
            
        return "Development";
    }
    
    private static bool IsTestEnvironment()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        return assembly.FullName?.Contains("Test") == true ||
               Environment.StackTrace.Contains("TestHost") ||
               Environment.StackTrace.Contains("mstest") ||
               Environment.StackTrace.Contains("NUnit") ||
               Environment.StackTrace.Contains("xunit");
    }
    
    private static bool IsContinuousIntegration()
    {
        var ciIndicators = new[]
        {
            "CI", "CONTINUOUS_INTEGRATION", "BUILD_ID", "BUILD_NUMBER",
            "GITHUB_ACTIONS", "GITLAB_CI", "JENKINS_URL", "TEAMCITY_VERSION"
        };
        
        return ciIndicators.Any(indicator => 
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(indicator)));
    }
    
    private static DatabaseConfiguration LoadConfiguration(string environment)
    {
        return environment.ToLowerInvariant() switch
        {
            "production" => GetProductionConfiguration(),
            "staging" => GetStagingConfiguration(),
            "ci" or "continuous_integration" => GetCIConfiguration(),
            "test" => GetTestConfiguration(),
            _ => GetDevelopmentConfiguration()
        };
    }
    
    private static DatabaseConfiguration GetDevelopmentConfiguration()
    {
        return new DatabaseConfiguration
        {
            Environment = "Development",
            MySQL = new DatabaseConnectionConfig
            {
                ConnectionString = BuildConnectionString(
                    "mysql",
                    Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost",
                    Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306",
                    Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "dev_database",
                    Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root",
                    Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "dev_password"
                )
            },
            Oracle = new DatabaseConnectionConfig
            {
                ConnectionString = BuildOracleConnectionString(
                    Environment.GetEnvironmentVariable("ORACLE_HOST") ?? "localhost",
                    Environment.GetEnvironmentVariable("ORACLE_PORT") ?? "1521",
                    Environment.GetEnvironmentVariable("ORACLE_SERVICE") ?? "XE",
                    Environment.GetEnvironmentVariable("ORACLE_USER") ?? "system",
                    Environment.GetEnvironmentVariable("ORACLE_PASSWORD") ?? "dev_password"
                )
            },
            SqlServer = new DatabaseConnectionConfig
            {
                ConnectionString = BuildSqlServerConnectionString(
                    Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "localhost",
                    Environment.GetEnvironmentVariable("SQLSERVER_DATABASE") ?? "dev_database",
                    Environment.GetEnvironmentVariable("SQLSERVER_USER") ?? "sa",
                    Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD") ?? "dev_password"
                )
            },
            PostgreSQL = new DatabaseConnectionConfig
            {
                ConnectionString = BuildPostgreSqlConnectionString(
                    Environment.GetEnvironmentVariable("POSTGRESQL_HOST") ?? "localhost",
                    Environment.GetEnvironmentVariable("POSTGRESQL_PORT") ?? "5432",
                    Environment.GetEnvironmentVariable("POSTGRESQL_DATABASE") ?? "dev_database",
                    Environment.GetEnvironmentVariable("POSTGRESQL_USER") ?? "postgres",
                    Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD") ?? "dev_password"
                )
            },
            Timeouts = GetDefaultTimeouts()
        };
    }
    
    private static DatabaseConfiguration GetTestConfiguration()
    {
        return new DatabaseConfiguration
        {
            Environment = "Test",
            MySQL = new DatabaseConnectionConfig
            {
                ConnectionString = BuildConnectionString(
                    "mysql",
                    Environment.GetEnvironmentVariable("TEST_MYSQL_HOST") ?? "localhost",
                    Environment.GetEnvironmentVariable("TEST_MYSQL_PORT") ?? "3306",
                    Environment.GetEnvironmentVariable("TEST_MYSQL_DATABASE") ?? "test_database",
                    Environment.GetEnvironmentVariable("TEST_MYSQL_USER") ?? "root",
                    Environment.GetEnvironmentVariable("TEST_MYSQL_PASSWORD") ?? "test_password"
                )
            },
            Oracle = new DatabaseConnectionConfig
            {
                ConnectionString = BuildOracleConnectionString(
                    Environment.GetEnvironmentVariable("TEST_ORACLE_HOST") ?? "localhost",
                    Environment.GetEnvironmentVariable("TEST_ORACLE_PORT") ?? "1521",
                    Environment.GetEnvironmentVariable("TEST_ORACLE_SERVICE") ?? "XE",
                    Environment.GetEnvironmentVariable("TEST_ORACLE_USER") ?? "system",
                    Environment.GetEnvironmentVariable("TEST_ORACLE_PASSWORD") ?? "test_password"
                )
            },
            SqlServer = new DatabaseConnectionConfig
            {
                ConnectionString = BuildSqlServerConnectionString(
                    Environment.GetEnvironmentVariable("TEST_SQLSERVER_HOST") ?? "localhost",
                    Environment.GetEnvironmentVariable("TEST_SQLSERVER_DATABASE") ?? "test_database",
                    Environment.GetEnvironmentVariable("TEST_SQLSERVER_USER") ?? "sa",
                    Environment.GetEnvironmentVariable("TEST_SQLSERVER_PASSWORD") ?? "test_password"
                )
            },
            PostgreSQL = new DatabaseConnectionConfig
            {
                ConnectionString = BuildPostgreSqlConnectionString(
                    Environment.GetEnvironmentVariable("TEST_POSTGRESQL_HOST") ?? "localhost",
                    Environment.GetEnvironmentVariable("TEST_POSTGRESQL_PORT") ?? "5432",
                    Environment.GetEnvironmentVariable("TEST_POSTGRESQL_DATABASE") ?? "test_database",
                    Environment.GetEnvironmentVariable("TEST_POSTGRESQL_USER") ?? "postgres",
                    Environment.GetEnvironmentVariable("TEST_POSTGRESQL_PASSWORD") ?? "test_password"
                )
            },
            Timeouts = GetTestTimeouts()
        };
    }
    
    private static DatabaseConfiguration GetCIConfiguration()
    {
        return new DatabaseConfiguration
        {
            Environment = "CI",
            MySQL = new DatabaseConnectionConfig
            {
                ConnectionString = BuildConnectionString(
                    "mysql",
                    Environment.GetEnvironmentVariable("CI_MYSQL_HOST") ?? "mysql-service",
                    Environment.GetEnvironmentVariable("CI_MYSQL_PORT") ?? "3306",
                    Environment.GetEnvironmentVariable("CI_MYSQL_DATABASE") ?? "ci_test_db",
                    Environment.GetEnvironmentVariable("CI_MYSQL_USER") ?? "ci_user",
                    Environment.GetEnvironmentVariable("CI_MYSQL_PASSWORD") ?? "ci_password"
                )
            },
            Oracle = new DatabaseConnectionConfig
            {
                ConnectionString = BuildOracleConnectionString(
                    Environment.GetEnvironmentVariable("CI_ORACLE_HOST") ?? "oracle-service",
                    Environment.GetEnvironmentVariable("CI_ORACLE_PORT") ?? "1521",
                    Environment.GetEnvironmentVariable("CI_ORACLE_SERVICE") ?? "XE",
                    Environment.GetEnvironmentVariable("CI_ORACLE_USER") ?? "ci_user",
                    Environment.GetEnvironmentVariable("CI_ORACLE_PASSWORD") ?? "ci_password"
                )
            },
            SqlServer = new DatabaseConnectionConfig
            {
                ConnectionString = BuildSqlServerConnectionString(
                    Environment.GetEnvironmentVariable("CI_SQLSERVER_HOST") ?? "sqlserver-service",
                    Environment.GetEnvironmentVariable("CI_SQLSERVER_DATABASE") ?? "ci_test_db",
                    Environment.GetEnvironmentVariable("CI_SQLSERVER_USER") ?? "sa",
                    Environment.GetEnvironmentVariable("CI_SQLSERVER_PASSWORD") ?? "ci_password"
                )
            },
            PostgreSQL = new DatabaseConnectionConfig
            {
                ConnectionString = BuildPostgreSqlConnectionString(
                    Environment.GetEnvironmentVariable("CI_POSTGRESQL_HOST") ?? "postgres-service",
                    Environment.GetEnvironmentVariable("CI_POSTGRESQL_PORT") ?? "5432",
                    Environment.GetEnvironmentVariable("CI_POSTGRESQL_DATABASE") ?? "ci_test_db",
                    Environment.GetEnvironmentVariable("CI_POSTGRESQL_USER") ?? "ci_user",
                    Environment.GetEnvironmentVariable("CI_POSTGRESQL_PASSWORD") ?? "ci_password"
                )
            },
            Timeouts = GetCITimeouts()
        };
    }
    
    private static DatabaseConfiguration GetStagingConfiguration()
    {
        return new DatabaseConfiguration
        {
            Environment = "Staging",
            MySQL = new DatabaseConnectionConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("STAGING_MYSQL_CONNECTION") ??
                    throw new InvalidOperationException("STAGING_MYSQL_CONNECTION environment variable is required")
            },
            Oracle = new DatabaseConnectionConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("STAGING_ORACLE_CONNECTION") ??
                    throw new InvalidOperationException("STAGING_ORACLE_CONNECTION environment variable is required")
            },
            SqlServer = new DatabaseConnectionConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("STAGING_SQLSERVER_CONNECTION") ??
                    throw new InvalidOperationException("STAGING_SQLSERVER_CONNECTION environment variable is required")
            },
            PostgreSQL = new DatabaseConnectionConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("STAGING_POSTGRESQL_CONNECTION") ??
                    throw new InvalidOperationException("STAGING_POSTGRESQL_CONNECTION environment variable is required")
            },
            Timeouts = GetProductionTimeouts()
        };
    }
    
    private static DatabaseConfiguration GetProductionConfiguration()
    {
        return new DatabaseConfiguration
        {
            Environment = "Production",
            MySQL = new DatabaseConnectionConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("PROD_MYSQL_CONNECTION") ??
                    throw new InvalidOperationException("PROD_MYSQL_CONNECTION environment variable is required")
            },
            Oracle = new DatabaseConnectionConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("PROD_ORACLE_CONNECTION") ??
                    throw new InvalidOperationException("PROD_ORACLE_CONNECTION environment variable is required")
            },
            SqlServer = new DatabaseConnectionConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("PROD_SQLSERVER_CONNECTION") ??
                    throw new InvalidOperationException("PROD_SQLSERVER_CONNECTION environment variable is required")
            },
            PostgreSQL = new DatabaseConnectionConfig
            {
                ConnectionString = Environment.GetEnvironmentVariable("PROD_POSTGRESQL_CONNECTION") ??
                    throw new InvalidOperationException("PROD_POSTGRESQL_CONNECTION environment variable is required")
            },
            Timeouts = GetProductionTimeouts()
        };
    }
    
    #endregion
    
    #region Connection String Builders
    
    private static string BuildConnectionString(string type, string host, string port, string database, string user, string password)
    {
        return type.ToLower() switch
        {
            "mysql" => $"Server={host};Port={port};Database={database};Uid={user};Pwd={password};Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;",
            _ => throw new NotSupportedException($"Database type '{type}' is not supported")
        };
    }
    
    private static string BuildOracleConnectionString(string host, string port, string service, string user, string password)
    {
        return $"Data Source={host}:{port}/{service};User Id={user};Password={password};Connection Timeout=120;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
    }
    
    private static string BuildSqlServerConnectionString(string host, string database, string user, string password)
    {
        return $"Server={host};Database={database};User Id={user};Password={password};TrustServerCertificate=True;Connection Timeout=120;Command Timeout=300;";
    }
    
    private static string BuildPostgreSqlConnectionString(string host, string port, string database, string user, string password)
    {
        return $"Host={host};Port={port};Database={database};Username={user};Password={password};Timeout=120;CommandTimeout=300;Pooling=true;";
    }
    
    #endregion
    
    #region Timeout Configurations
    
    private static TimeoutSettings GetDefaultTimeouts()
    {
        return new TimeoutSettings
        {
            ConnectionTimeout = 120,
            CommandTimeout = 300,
            TestTimeout = 180000, // 3 minutes for MSTest
            LongRunningTestTimeout = 300000 // 5 minutes for complex tests
        };
    }
    
    private static TimeoutSettings GetTestTimeouts()
    {
        return new TimeoutSettings
        {
            ConnectionTimeout = 30,
            CommandTimeout = 120,
            TestTimeout = 60000, // 1 minute for fast tests
            LongRunningTestTimeout = 180000 // 3 minutes for integration tests
        };
    }
    
    private static TimeoutSettings GetCITimeouts()
    {
        return new TimeoutSettings
        {
            ConnectionTimeout = 60,
            CommandTimeout = 180,
            TestTimeout = 120000, // 2 minutes
            LongRunningTestTimeout = 300000 // 5 minutes
        };
    }
    
    private static TimeoutSettings GetProductionTimeouts()
    {
        return new TimeoutSettings
        {
            ConnectionTimeout = 120,
            CommandTimeout = 600, // 10 minutes for complex operations
            TestTimeout = 300000, // 5 minutes
            LongRunningTestTimeout = 600000 // 10 minutes
        };
    }
    
    #endregion
}

/// <summary>
/// Database configuration for specific environment
/// </summary>
public class DatabaseConfiguration
{
    public string Environment { get; set; } = string.Empty;
    public DatabaseConnectionConfig MySQL { get; set; } = new();
    public DatabaseConnectionConfig Oracle { get; set; } = new();
    public DatabaseConnectionConfig SqlServer { get; set; } = new();
    public DatabaseConnectionConfig PostgreSQL { get; set; } = new();
    public TimeoutSettings Timeouts { get; set; } = new();
}

/// <summary>
/// Connection configuration for specific database type
/// </summary>
public class DatabaseConnectionConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Timeout settings for database operations
/// </summary>
public class TimeoutSettings
{
    /// <summary>
    /// Database connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 120;
    
    /// <summary>
    /// SQL command execution timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 300;
    
    /// <summary>
    /// Test method timeout in milliseconds
    /// </summary>
    public int TestTimeout { get; set; } = 180000;
    
    /// <summary>
    /// Long-running test timeout in milliseconds
    /// </summary>
    public int LongRunningTestTimeout { get; set; } = 300000;
} 