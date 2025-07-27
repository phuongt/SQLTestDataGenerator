using SqlTestDataGenerator.Core.Abstractions;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Centralized factory for creating SQL dialect handlers.
/// Consolidates dialect handler creation logic from multiple services.
/// Replaces scattered CreateDialectHandler methods.
/// </summary>
public static class DialectHandlerFactory
{
    private static readonly Dictionary<DatabaseType, Func<ISqlDialectHandler>> _handlers = new()
    {
        { DatabaseType.Oracle, () => new OracleDialectHandler() },
        { DatabaseType.MySQL, () => new MySqlDialectHandler() },
        { DatabaseType.SqlServer, () => new MySqlDialectHandler() }, // Use MySQL as fallback for now
        { DatabaseType.PostgreSQL, () => new MySqlDialectHandler() } // Use MySQL as fallback for now
    };

    /// <summary>
    /// Create dialect handler for specified database type
    /// Consolidates CreateDialectHandler logic from DataGenService, CoordinatedDataGenerator, etc.
    /// </summary>
    public static ISqlDialectHandler Create(DatabaseType databaseType)
    {
        if (_handlers.TryGetValue(databaseType, out var factory))
        {
            return factory();
        }

        throw new NotSupportedException($"Database type '{databaseType}' is not supported");
    }
    
    /// <summary>
    /// Create dialect handler from string database type
    /// Handles both enum and string inputs for backward compatibility
    /// </summary>
    public static ISqlDialectHandler Create(string databaseType, DatabaseType fallbackType = DatabaseType.MySQL)
    {
        if (string.IsNullOrEmpty(databaseType))
            return Create(fallbackType);
            
        // Try to parse from string first
        if (databaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            return Create(DatabaseType.Oracle);
            
        if (databaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            return Create(DatabaseType.MySQL);
            
        if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) ||
            databaseType.Equals("SQL Server", StringComparison.OrdinalIgnoreCase))
            return Create(DatabaseType.SqlServer);
            
        if (databaseType.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            return Create(DatabaseType.PostgreSQL);
            
        // Fall back to enum if provided
        return Create(fallbackType);
    }

    /// <summary>
    /// Get supported database types
    /// </summary>
    public static IEnumerable<DatabaseType> GetSupportedDatabaseTypes()
    {
        return _handlers.Keys;
    }

    /// <summary>
    /// Check if database type is supported
    /// </summary>
    public static bool IsSupported(DatabaseType databaseType)
    {
        return _handlers.ContainsKey(databaseType);
    }
    
    /// <summary>
    /// Check if database type string is supported
    /// </summary>
    public static bool IsSupported(string databaseType)
    {
        if (string.IsNullOrEmpty(databaseType))
            return false;
            
        var supportedTypes = new[]
        {
            "oracle", "mysql", "sqlserver", "sql server", "postgresql"
        };
        
        return supportedTypes.Contains(databaseType.ToLowerInvariant());
    }
} 