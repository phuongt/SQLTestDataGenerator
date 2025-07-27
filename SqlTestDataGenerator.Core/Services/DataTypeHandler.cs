using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Centralized data type handling utility to eliminate duplicate type checking logic
/// across multiple services. Replaces scattered dataType switch patterns.
/// </summary>
public static class DataTypeHandler
{
    #region Data Type Categories
    
    /// <summary>
    /// Get the category of a data type for unified handling
    /// </summary>
    public static DataTypeCategory GetCategory(string dataType)
    {
        if (string.IsNullOrEmpty(dataType))
            return DataTypeCategory.Unknown;
            
        var normalizedType = dataType.ToLower();
        
        return normalizedType switch
        {
            var dt when IntegerTypes.Contains(dt) => DataTypeCategory.Integer,
            var dt when StringTypes.Contains(dt) => DataTypeCategory.String,
            var dt when DateTimeTypes.Contains(dt) => DataTypeCategory.DateTime,
            var dt when BooleanTypes.Contains(dt) => DataTypeCategory.Boolean,
            var dt when DecimalTypes.Contains(dt) => DataTypeCategory.Decimal,
            var dt when JsonTypes.Contains(dt) => DataTypeCategory.Json,
            var dt when BinaryTypes.Contains(dt) => DataTypeCategory.Binary,
            var dt when dt.StartsWith("enum(") => DataTypeCategory.Enum,
            _ => DataTypeCategory.Unknown
        };
    }
    
    /// <summary>
    /// Check if a data type is numeric (integer or decimal)
    /// </summary>
    public static bool IsNumeric(string dataType)
    {
        var category = GetCategory(dataType);
        return category == DataTypeCategory.Integer || category == DataTypeCategory.Decimal;
    }
    
    /// <summary>
    /// Check if a data type is a text type
    /// </summary>
    public static bool IsTextType(string dataType)
    {
        return GetCategory(dataType) == DataTypeCategory.String;
    }
    
    /// <summary>
    /// Check if a data type is a date/time type
    /// </summary>
    public static bool IsDateTimeType(string dataType)
    {
        return GetCategory(dataType) == DataTypeCategory.DateTime;
    }
    
    /// <summary>
    /// Check if a data type is boolean
    /// </summary>
    public static bool IsBooleanType(string dataType)
    {
        return GetCategory(dataType) == DataTypeCategory.Boolean;
    }
    
    #endregion
    
    #region Type Collections
    
    private static readonly HashSet<string> IntegerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "integer", "bigint", "smallint", "tinyint", 
        "number", "long", "short", "byte"
    };
    
    private static readonly HashSet<string> StringTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "varchar", "nvarchar", "text", "char", "nchar", "string",
        "varchar2", "clob", "longtext", "mediumtext", "tinytext"
    };
    
    private static readonly HashSet<string> DateTimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "datetime", "datetime2", "date", "timestamp", "time",
        "timestamp(6)", "smalldatetime", "datetimeoffset"
    };
    
    private static readonly HashSet<string> BooleanTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bit", "boolean", "bool"
    };
    
    private static readonly HashSet<string> DecimalTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "decimal", "numeric", "float", "double", "money", "real"
    };
    
    private static readonly HashSet<string> JsonTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "json", "jsonb"
    };
    
    private static readonly HashSet<string> BinaryTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "blob", "longblob", "mediumblob", "tinyblob", "binary", "varbinary", "image"
    };
    
    #endregion
    
    #region Database-Specific Type Mapping
    
    /// <summary>
    /// Get equivalent data type for target database
    /// </summary>
    public static string GetEquivalentType(string sourceType, DatabaseType targetDatabase)
    {
        var category = GetCategory(sourceType);
        
        return targetDatabase switch
        {
            DatabaseType.Oracle => GetOracleEquivalent(category, sourceType),
            DatabaseType.MySQL => GetMySqlEquivalent(category, sourceType),
            DatabaseType.SqlServer => GetSqlServerEquivalent(category, sourceType),
            DatabaseType.PostgreSQL => GetPostgreSqlEquivalent(category, sourceType),
            _ => sourceType
        };
    }
    
    private static string GetOracleEquivalent(DataTypeCategory category, string sourceType)
    {
        return category switch
        {
            DataTypeCategory.Integer => "NUMBER",
            DataTypeCategory.Decimal => "NUMBER",
            DataTypeCategory.String => GetOracleStringType(sourceType),
            DataTypeCategory.DateTime => GetOracleDateType(sourceType),
            DataTypeCategory.Boolean => "NUMBER(1)",
            DataTypeCategory.Json => "CLOB",
            DataTypeCategory.Binary => "BLOB",
            _ => "VARCHAR2(255)"
        };
    }
    
    private static string GetMySqlEquivalent(DataTypeCategory category, string sourceType)
    {
        return category switch
        {
            DataTypeCategory.Integer => "INT",
            DataTypeCategory.Decimal => "DECIMAL",
            DataTypeCategory.String => "VARCHAR(255)",
            DataTypeCategory.DateTime => "DATETIME",
            DataTypeCategory.Boolean => "TINYINT(1)",
            DataTypeCategory.Json => "JSON",
            DataTypeCategory.Binary => "BLOB",
            _ => "VARCHAR(255)"
        };
    }
    
    private static string GetSqlServerEquivalent(DataTypeCategory category, string sourceType)
    {
        return category switch
        {
            DataTypeCategory.Integer => "INT",
            DataTypeCategory.Decimal => "DECIMAL",
            DataTypeCategory.String => "NVARCHAR(255)",
            DataTypeCategory.DateTime => "DATETIME2",
            DataTypeCategory.Boolean => "BIT",
            DataTypeCategory.Json => "NVARCHAR(MAX)",
            DataTypeCategory.Binary => "VARBINARY(MAX)",
            _ => "NVARCHAR(255)"
        };
    }
    
    private static string GetPostgreSqlEquivalent(DataTypeCategory category, string sourceType)
    {
        return category switch
        {
            DataTypeCategory.Integer => "INTEGER",
            DataTypeCategory.Decimal => "DECIMAL",
            DataTypeCategory.String => "VARCHAR(255)",
            DataTypeCategory.DateTime => "TIMESTAMP",
            DataTypeCategory.Boolean => "BOOLEAN",
            DataTypeCategory.Json => "JSONB",
            DataTypeCategory.Binary => "BYTEA",
            _ => "VARCHAR(255)"
        };
    }
    
    private static string GetOracleStringType(string sourceType)
    {
        var lower = sourceType.ToLower();
        if (lower.Contains("text") || lower.Contains("clob") || lower == "longtext")
            return "CLOB";
        return "VARCHAR2(255)";
    }
    
    private static string GetOracleDateType(string sourceType)
    {
        var lower = sourceType.ToLower();
        if (lower == "date")
            return "DATE";
        if (lower.Contains("time"))
            return "TIMESTAMP";
        return "TIMESTAMP";
    }
    
    #endregion
    
    #region Column Analysis Utilities
    
    /// <summary>
    /// Check if column is likely a boolean based on name and type
    /// Consolidates boolean detection logic from multiple services
    /// </summary>
    public static bool IsBooleanColumn(ColumnSchema column)
    {
        if (column == null) return false;
        
        var columnName = column.ColumnName?.ToLower() ?? "";
        var dataType = column.DataType?.ToLower() ?? "";
        
        // Check data type first
        if (IsBooleanType(dataType))
            return true;
            
        // Check for MySQL TINYINT(1) used as boolean
        if (dataType.Contains("tinyint") && column.MaxLength == 1)
            return true;
            
        // Check for Oracle NUMBER(1) used as boolean
        if (dataType.Contains("number") && column.NumericPrecision == 1 && column.NumericScale == 0)
            return true;
            
        // Check boolean-like column names
        return IsBooleanColumnName(columnName);
    }
    
    /// <summary>
    /// Check if column name suggests boolean usage
    /// </summary>
    public static bool IsBooleanColumnName(string columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return false;
        
        var lower = columnName.ToLower();
        
        // Boolean prefixes
        if (lower.StartsWith("is_") || lower.StartsWith("has_") || 
            lower.StartsWith("can_") || lower.StartsWith("should_") ||
            lower.StartsWith("enable") || lower.StartsWith("disable"))
            return true;
            
        // Boolean suffixes
        if (lower.EndsWith("_active") || lower.EndsWith("_enabled") ||
            lower.EndsWith("_deleted") || lower.EndsWith("_verified") ||
            lower.EndsWith("_approved") || lower.EndsWith("_published"))
            return true;
            
        // Common boolean column names
        var booleanNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "active", "enabled", "deleted", "verified", "approved", 
            "published", "visible", "locked", "expired", "completed"
        };
        
        return booleanNames.Contains(lower);
    }
    
    /// <summary>
    /// Check if column is likely a JSON column based on name and type
    /// </summary>
    public static bool IsJsonColumn(ColumnSchema column)
    {
        if (column == null) return false;
        
        var dataType = column.DataType?.ToLower() ?? "";
        var columnName = column.ColumnName?.ToLower() ?? "";
        
        // Check data type first
        if (JsonTypes.Contains(dataType))
            return true;
            
        // Oracle stores JSON as CLOB
        if (dataType == "clob" && IsJsonColumnName(columnName))
            return true;
            
        // Check by column name patterns
        return IsJsonColumnName(columnName);
    }
    
    /// <summary>
    /// Check if column name suggests JSON usage
    /// </summary>
    public static bool IsJsonColumnName(string columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return false;
        
        var lower = columnName.ToLower();
        var jsonPatterns = new[] { "permissions", "metadata", "settings", "config", "data", "attributes", "properties" };
        
        return jsonPatterns.Any(pattern => lower.Contains(pattern));
    }
    
    /// <summary>
    /// Check if column is likely a date column based on name
    /// </summary>
    public static bool IsDateColumn(ColumnSchema column)
    {
        if (column == null) return false;
        
        // Check data type first
        if (IsDateTimeType(column.DataType))
            return true;
            
        // Check by column name patterns
        var columnName = column.ColumnName?.ToLower() ?? "";
        var datePatterns = new[] { "date", "time", "created", "updated", "modified", "birth", "expire", "start", "end" };
        
        return datePatterns.Any(pattern => columnName.Contains(pattern));
    }
    
    #endregion
}

/// <summary>
/// Data type categories for unified handling
/// </summary>
public enum DataTypeCategory
{
    Unknown,
    Integer,
    Decimal,
    String,
    DateTime,
    Boolean,
    Json,
    Binary,
    Enum
} 