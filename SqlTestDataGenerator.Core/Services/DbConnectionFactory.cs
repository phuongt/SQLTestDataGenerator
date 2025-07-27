using System.Data;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using SqlTestDataGenerator.Core.Models;
using Oracle.ManagedDataAccess.Client;

namespace SqlTestDataGenerator.Core.Services;

public static class DbConnectionFactory
{
    public static IDbConnection CreateConnection(string databaseType, string connectionString)
    {
        // Ensure connection string has proper timeout settings
        connectionString = EnsureTimeoutSettings(databaseType, connectionString);
        
        return databaseType.ToLower() switch
        {
            "sql server" => new SqlConnection(connectionString),
            "mysql" => new MySqlConnection(connectionString),
            "postgresql" => new NpgsqlConnection(connectionString),
            "oracle" => new OracleConnection(connectionString),
            _ => throw new NotSupportedException($"Database type '{databaseType}' is not supported.")
        };
    }

    /// <summary>
    /// Create connection through SSH tunnel if configured
    /// </summary>
    public static async Task<IDbConnection> CreateConnectionAsync(string databaseType, string connectionString, SshTunnelService? sshService = null)
    {
        if (sshService?.IsConnected == true)
        {
            // Use SSH tunnel connection
            var tunnelConnectionString = sshService.GetTunnelConnectionString(
                ExtractDatabaseName(connectionString),
                ExtractUsername(connectionString),
                ExtractPassword(connectionString)
            );
            return CreateConnection(databaseType, tunnelConnectionString);
        }
        
        // Use direct connection
        return CreateConnection(databaseType, connectionString);
    }

    /// <summary>
    /// Extract database name from connection string
    /// </summary>
    public static string? ExtractDatabaseName(string connectionString)
    {
        var parts = connectionString.Split(';');
        var dbPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase));
        return dbPart?.Split('=')[1] ?? "";
    }

    /// <summary>
    /// Extract username from connection string
    /// </summary>
    public static string? ExtractUsername(string connectionString)
    {
        var parts = connectionString.Split(';');
        var userPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Uid=", StringComparison.OrdinalIgnoreCase) || 
                                                 p.Trim().StartsWith("User Id=", StringComparison.OrdinalIgnoreCase));
        return userPart?.Split('=')[1] ?? "";
    }

    /// <summary>
    /// Extract password from connection string
    /// </summary>
    public static string? ExtractPassword(string connectionString)
    {
        var parts = connectionString.Split(';');
        var pwdPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase) || 
                                                p.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase));
        return pwdPart?.Split('=')[1] ?? "";
    }

    /// <summary>
    /// Đảm bảo connection string có timeout settings đầy đủ
    /// </summary>
    private static string EnsureTimeoutSettings(string databaseType, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return connectionString;
        
        var dbType = databaseType.ToLower();
        
        // Kiểm tra và thêm timeout settings nếu chưa có
        switch (dbType)
        {
            case "mysql":
                if (!connectionString.Contains("Connection Timeout") && !connectionString.Contains("Connect Timeout"))
                {
                    connectionString += ";Connection Timeout=120";
                }
                if (!connectionString.Contains("Command Timeout"))
                {
                    connectionString += ";Command Timeout=300";
                }
                if (!connectionString.Contains("Connection Lifetime"))
                {
                    connectionString += ";Connection Lifetime=300";
                }
                if (!connectionString.Contains("Pooling"))
                {
                    connectionString += ";Pooling=true";
                }
                break;
                
            case "sql server":
                if (!connectionString.Contains("Connection Timeout") && !connectionString.Contains("Connect Timeout"))
                {
                    connectionString += ";Connection Timeout=120";
                }
                if (!connectionString.Contains("Command Timeout"))
                {
                    connectionString += ";Command Timeout=300";
                }
                break;
                
            case "postgresql":
                if (!connectionString.Contains("Timeout"))
                {
                    connectionString += ";Timeout=120";
                }
                if (!connectionString.Contains("Command Timeout"))
                {
                    connectionString += ";Command Timeout=300";
                }
                break;
                
            case "oracle":
                if (!connectionString.Contains("Connection Timeout"))
                {
                    connectionString += ";Connection Timeout=120";
                }
                if (!connectionString.Contains("Connection Lifetime"))
                {
                    connectionString += ";Connection Lifetime=300";
                }
                if (!connectionString.Contains("Pooling"))
                {
                    connectionString += ";Pooling=true";
                }
                break;
        }
        
        return connectionString;
    }

    public static DatabaseType ParseDatabaseType(string databaseType)
    {
        return databaseType.ToLower() switch
        {
            "sql server" => DatabaseType.SqlServer,
            "mysql" => DatabaseType.MySQL,
            "postgresql" => DatabaseType.PostgreSQL,
            "oracle" => DatabaseType.Oracle,
            _ => throw new NotSupportedException($"Database type '{databaseType}' is not supported.")
        };
    }

    public static string GetInformationSchemaQuery(DatabaseType dbType, string tableName)
    {
        return dbType switch
        {
            DatabaseType.SqlServer => $@"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    c.COLUMN_DEFAULT,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IS_PRIMARY_KEY,
                    CASE WHEN ic.is_identity IS NOT NULL THEN 1 ELSE 0 END as IS_IDENTITY
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                        AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                ) pk ON c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
                LEFT JOIN sys.columns ic ON ic.object_id = OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME) 
                    AND ic.name = c.COLUMN_NAME AND ic.is_identity = 1
                WHERE c.TABLE_NAME = '{tableName}'
                ORDER BY c.ORDINAL_POSITION",

            DatabaseType.MySQL => $@"
                SELECT 
                    COLUMN_NAME,
                    DATA_TYPE,
                    COLUMN_TYPE,
                    IS_NULLABLE,
                    CHARACTER_MAXIMUM_LENGTH,
                    NUMERIC_PRECISION,
                    NUMERIC_SCALE,
                    COLUMN_DEFAULT,
                    EXTRA,
                    CASE WHEN COLUMN_KEY = 'PRI' THEN 1 ELSE 0 END as IS_PRIMARY_KEY,
                    CASE WHEN EXTRA = 'auto_increment' THEN 1 ELSE 0 END as IS_IDENTITY,
                    CASE WHEN EXTRA LIKE '%GENERATED%' OR EXTRA LIKE '%STORED%' OR EXTRA LIKE '%VIRTUAL%' THEN 1 ELSE 0 END as IS_GENERATED
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = '{tableName}'
                    AND TABLE_SCHEMA = DATABASE()
                ORDER BY ORDINAL_POSITION",

            DatabaseType.PostgreSQL => $@"
                SELECT 
                    c.column_name,
                    c.data_type,
                    c.is_nullable,
                    c.character_maximum_length,
                    c.numeric_precision,
                    c.numeric_scale,
                    c.column_default,
                    CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key,
                    CASE WHEN c.column_default LIKE 'nextval%' THEN true ELSE false END as is_identity
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT kcu.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu 
                        ON tc.constraint_name = kcu.constraint_name
                    WHERE tc.constraint_type = 'PRIMARY KEY' 
                        AND tc.table_name = '{tableName}'
                ) pk ON c.column_name = pk.column_name
                WHERE c.table_name = '{tableName}'
                ORDER BY c.ordinal_position",

            DatabaseType.Oracle => $@"
                SELECT 
                    c.column_name,
                    c.data_type,
                    c.nullable,
                    c.char_length as character_maximum_length,
                    c.data_precision as numeric_precision,
                    c.data_scale as numeric_scale,
                    'NULL' as column_default,
                    CASE WHEN pk.column_name IS NOT NULL THEN 1 ELSE 0 END as is_primary_key,
                    CASE 
                        WHEN c.identity_column = 'YES' THEN 1 
                        ELSE 0 
                    END as is_identity
                FROM user_tab_columns c
                LEFT JOIN (
                    SELECT cc.column_name
                    FROM user_constraints uc
                    JOIN user_cons_columns cc ON uc.constraint_name = cc.constraint_name
                    WHERE uc.constraint_type = 'P' 
                        AND uc.table_name = UPPER('{tableName}')
                ) pk ON UPPER(c.column_name) = UPPER(pk.column_name)
                WHERE UPPER(c.table_name) = UPPER('{tableName}')
                ORDER BY c.column_id",

            _ => throw new NotSupportedException($"Database type '{dbType}' is not supported.")
        };
    }

    public static string GetForeignKeyQuery(DatabaseType dbType, string tableName)
    {
        return dbType switch
        {
            DatabaseType.SqlServer => $@"
                SELECT 
                    fk.name as CONSTRAINT_NAME,
                    c.name as COLUMN_NAME,
                    rt.name as REFERENCED_TABLE,
                    rc.name as REFERENCED_COLUMN,
                    s.name as REFERENCED_SCHEMA
                FROM sys.foreign_keys fk
                INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
                INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
                INNER JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
                INNER JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
                INNER JOIN sys.schemas s ON rt.schema_id = s.schema_id
                WHERE t.name = '{tableName}'",

            DatabaseType.MySQL => $@"
                SELECT 
                    CONSTRAINT_NAME,
                    COLUMN_NAME,
                    REFERENCED_TABLE_NAME as REFERENCED_TABLE,
                    REFERENCED_COLUMN_NAME as REFERENCED_COLUMN,
                    REFERENCED_TABLE_SCHEMA as REFERENCED_SCHEMA
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                WHERE TABLE_NAME = '{tableName}' 
                    AND TABLE_SCHEMA = DATABASE()
                    AND REFERENCED_TABLE_NAME IS NOT NULL",

            DatabaseType.PostgreSQL => $@"
                SELECT 
                    tc.constraint_name,
                    kcu.column_name,
                    ccu.table_name as referenced_table,
                    ccu.column_name as referenced_column,
                    ccu.table_schema as referenced_schema
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu 
                    ON tc.constraint_name = kcu.constraint_name
                JOIN information_schema.constraint_column_usage ccu 
                    ON ccu.constraint_name = tc.constraint_name
                WHERE tc.constraint_type = 'FOREIGN KEY' 
                    AND tc.table_name = '{tableName}'",

            DatabaseType.Oracle => $@"
                SELECT 
                    uc.constraint_name,
                    ucc.column_name,
                    r_uc.table_name as referenced_table,
                    r_ucc.column_name as referenced_column,
                    USER as referenced_schema
                FROM user_constraints uc
                JOIN user_cons_columns ucc ON uc.constraint_name = ucc.constraint_name
                JOIN user_constraints r_uc ON uc.r_constraint_name = r_uc.constraint_name
                JOIN user_cons_columns r_ucc ON r_uc.constraint_name = r_ucc.constraint_name 
                    AND ucc.position = r_ucc.position
                WHERE uc.constraint_type = 'R' 
                    AND UPPER(uc.table_name) = UPPER('{tableName}')
                ORDER BY ucc.position",

            _ => throw new NotSupportedException($"Database type '{dbType}' is not supported.")
        };
    }
} 