using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using SqlTestDataGenerator.Core.Models;
using Serilog;
using MySqlConnector;
using Microsoft.Data.SqlClient;

namespace SqlTestDataGenerator.Core.Services;

public class SqlMetadataService
{
    private readonly ILogger _logger;

    public SqlMetadataService()
    {
        _logger = Log.ForContext<SqlMetadataService>();
    }

    public async Task<DatabaseInfo> GetDatabaseInfoAsync(string databaseType, string connectionString, string sqlQuery)
    {
        var dbType = DbConnectionFactory.ParseDatabaseType(databaseType);
        var tables = ExtractTablesFromQuery(sqlQuery);
        
        var databaseInfo = new DatabaseInfo
        {
            Type = dbType,
            ConnectionString = connectionString
        };

        using var connection = DbConnectionFactory.CreateConnection(databaseType, connectionString);
        connection.Open();

        foreach (var tableName in tables)
        {
            try
            {
                var tableSchema = await GetTableSchemaAsync(connection, dbType, tableName);
                databaseInfo.Tables[tableName] = tableSchema;
                _logger.Information("Loaded schema for table: {TableName}", tableName);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load schema for table: {TableName}", tableName);
                // Continue with other tables
            }
        }

        return databaseInfo;
    }

    private async Task<TableSchema> GetTableSchemaAsync(IDbConnection connection, DatabaseType dbType, string tableName)
    {
        var tableSchema = new TableSchema { TableName = tableName };

        // Get column information với command timeout
        var columnQuery = DbConnectionFactory.GetInformationSchemaQuery(dbType, tableName);
        var columns = await connection.QueryAsync(columnQuery, commandTimeout: 300); // 5 phút timeout

        foreach (var column in columns)
        {
            var columnSchema = MapToColumnSchema(column, dbType);
            tableSchema.Columns.Add(columnSchema);
            
            if (columnSchema.IsPrimaryKey)
            {
                tableSchema.PrimaryKeys.Add(columnSchema.ColumnName);
            }
        }

        // Get foreign key information
        try
        {
            var fkQuery = DbConnectionFactory.GetForeignKeyQuery(dbType, tableName);
            var foreignKeys = await connection.QueryAsync(fkQuery, commandTimeout: 300); // 5 phút timeout

            foreach (var fk in foreignKeys)
            {
                var fkSchema = MapToForeignKeySchema(fk, dbType);
                tableSchema.ForeignKeys.Add(fkSchema);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load foreign keys for table: {TableName}", tableName);
        }

        return tableSchema;
    }

    private static ColumnSchema MapToColumnSchema(dynamic column, DatabaseType dbType)
    {
        var columnSchema = new ColumnSchema();

        {
            columnSchema.ColumnName = column.COLUMN_NAME ?? column.column_name;
            columnSchema.DataType = column.DATA_TYPE ?? column.data_type;
            
            // 🎯 MYSQL ENUM SUPPORT: Use COLUMN_TYPE for ENUM parsing
            var columnType = column.COLUMN_TYPE ?? string.Empty;
            if (!string.IsNullOrEmpty(columnType) && columnType.ToString().ToLower().StartsWith("enum"))
            {
                columnSchema.DataType = columnType.ToString(); // Store full ENUM definition for parsing
                columnSchema.EnumValues = ParseEnumValues(columnType.ToString()); // Parse and store ENUM values
            }
            
            // Fix: Handle IS_NULLABLE as string only, don't mix with bool
            var isNullableValue = column.IS_NULLABLE ?? column.is_nullable;
            columnSchema.IsNullable = isNullableValue?.ToString()?.ToUpper() == "YES";
            
            // Fix: Handle IS_PRIMARY_KEY as int/long only, don't mix with bool  
            var isPrimaryKeyValue = column.IS_PRIMARY_KEY ?? column.is_primary_key;
            columnSchema.IsPrimaryKey = Convert.ToInt32(isPrimaryKeyValue ?? 0) == 1;
            
            // Fix: Handle IS_IDENTITY as int/long only, don't mix with bool
            var isIdentityValue = column.IS_IDENTITY ?? column.is_identity;
            columnSchema.IsIdentity = Convert.ToInt32(isIdentityValue ?? 0) == 1;
            
            // 🎯 MYSQL GENERATED COLUMNS: Handle IS_GENERATED and EXTRA fields
            var isGeneratedValue = column.IS_GENERATED;
            var extraValue = column.EXTRA;
            
            // Fix: Check both IS_GENERATED flag AND MySQL EXTRA patterns
            bool isGenerated = false;
            
            if (isGeneratedValue != null && Convert.ToInt32(isGeneratedValue) == 1)
            {
                isGenerated = true;
            }
            
            // Also check EXTRA field for generated column patterns
            if (!string.IsNullOrEmpty(extraValue?.ToString()))
            {
                var extraStr = extraValue.ToString().ToUpper();
                if (extraStr.Contains("GENERATED") || extraStr.Contains("STORED") || extraStr.Contains("VIRTUAL"))
                {
                    isGenerated = true;
                }
            }
            
            if (isGenerated)
            {
                // Set the IsGenerated flag directly
                columnSchema.IsGenerated = true;
                // Also store in DefaultValue for backward compatibility
                columnSchema.DefaultValue = $"GENERATED:{extraValue}";
                Console.WriteLine($"[SqlMetadataService] Found generated column: {columnSchema.ColumnName} with EXTRA: {extraValue}, IS_GENERATED: {isGeneratedValue}");
                Console.WriteLine($"[SqlMetadataService] SET IsGenerated=TRUE for column: {columnSchema.ColumnName}");
            }
            
            // Fix: Handle numeric values with safe conversion from ulong to int
            var maxLengthValue = column.CHARACTER_MAXIMUM_LENGTH ?? column.character_maximum_length;
            columnSchema.MaxLength = maxLengthValue != null ? Convert.ToInt32(maxLengthValue) : (int?)null;
            
            var numericPrecisionValue = column.NUMERIC_PRECISION ?? column.numeric_precision;
            columnSchema.NumericPrecision = numericPrecisionValue != null ? Convert.ToInt32(numericPrecisionValue) : (int?)null;
            
            var numericScaleValue = column.NUMERIC_SCALE ?? column.numeric_scale;
            columnSchema.NumericScale = numericScaleValue != null ? Convert.ToInt32(numericScaleValue) : (int?)null;
            
            columnSchema.DefaultValue = column.COLUMN_DEFAULT ?? column.column_default;
        }

        return columnSchema;
    }

    /// <summary>
    /// Parse ENUM values from MySQL ENUM type definition
    /// </summary>
    private static List<string> ParseEnumValues(string enumTypeDefinition)
    {
        var values = new List<string>();
        
        try
        {
            // Match pattern: enum('value1','value2','value3')
            var match = System.Text.RegularExpressions.Regex.Match(
                enumTypeDefinition, 
                @"enum\s*\(\s*(.+?)\s*\)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
            if (match.Success)
            {
                var valuesString = match.Groups[1].Value;
                
                // Split by comma and clean up quotes
                var rawValues = valuesString.Split(',');
                foreach (var rawValue in rawValues)
                {
                    var cleanValue = rawValue.Trim().Trim('\'').Trim('"');
                    if (!string.IsNullOrEmpty(cleanValue))
                    {
                        values.Add(cleanValue);
                    }
                }
            }
        }
        catch (Exception)
        {
            // If parsing fails, return empty list - fallback will handle
        }
        
        return values;
    }

    private static ForeignKeySchema MapToForeignKeySchema(dynamic fk, DatabaseType dbType)
    {
        return new ForeignKeySchema
        {
            ConstraintName = fk.CONSTRAINT_NAME ?? fk.constraint_name ?? string.Empty,
            ColumnName = fk.COLUMN_NAME ?? fk.column_name ?? string.Empty,
            ReferencedTable = fk.REFERENCED_TABLE ?? fk.referenced_table ?? fk.table ?? string.Empty,
            ReferencedColumn = fk.REFERENCED_COLUMN ?? fk.referenced_column ?? fk.to ?? string.Empty,
            ReferencedSchema = fk.REFERENCED_SCHEMA ?? fk.referenced_schema ?? "dbo"
        };
    }

    public static List<string> ExtractTablesFromQuery(string sqlQuery)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            Console.WriteLine($"[SqlMetadataService] Extracting tables from query: {sqlQuery?.Length ?? 0} chars");
            
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                Console.WriteLine("[SqlMetadataService] SQL query is null or empty");
                return new List<string>();
            }

            // Remove comments and normalize whitespace
            var cleanSql = RemoveComments(sqlQuery);
            Console.WriteLine($"[SqlMetadataService] Clean SQL: {cleanSql.Substring(0, Math.Min(200, cleanSql.Length))}...");
            
            // Enhanced patterns to match table names after FROM, JOIN keywords with aliases
            // Support for: standard names, [brackets], and `backticks`
            var patterns = new[]
            {
                @"\bFROM\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)(?:\s+(?:AS\s+)?(\w+))?",
                @"\b(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|OUTER\s+)?JOIN\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)(?:\s+(?:AS\s+)?(\w+))?",
                @"\bINTO\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)",
                @"\bUPDATE\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)",
                @"\bINSERT\s+INTO\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)"
            };

            foreach (var pattern in patterns)
            {
                try
                {
                    var matches = Regex.Matches(cleanSql, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    Console.WriteLine($"[SqlMetadataService] Pattern '{pattern}' found {matches.Count} matches");
                    
                    foreach (Match match in matches)
                    {
                        Console.WriteLine($"[SqlMetadataService] Match: '{match.Value}'");
                        
                        // Get the table name from different groups:
                        // Group 1: standard identifier
                        // Group 2: [bracketed] identifier  
                        // Group 3: `backtick` identifier
                        var tableName = match.Groups[1].Value;
                        if (string.IsNullOrEmpty(tableName))
                        {
                            tableName = match.Groups[2].Value;
                        }
                        if (string.IsNullOrEmpty(tableName))
                        {
                            tableName = match.Groups[3].Value;
                        }
                        
                        if (!string.IsNullOrEmpty(tableName) && !IsReservedWord(tableName))
                        {
                            tables.Add(tableName);
                            Console.WriteLine($"[SqlMetadataService] Found table: {tableName}");
                        }
                        else
                        {
                            Console.WriteLine($"[SqlMetadataService] Skipped: '{tableName}' (empty or reserved word)");
                        }
                    }
                }
                catch (Exception patternEx)
                {
                    Console.WriteLine($"[SqlMetadataService] Error with pattern '{pattern}': {patternEx.Message}");
                }
            }

            // If no tables found with complex patterns, try simpler approach
            if (tables.Count == 0)
            {
                Console.WriteLine("[SqlMetadataService] No tables found with complex patterns, trying simple approach...");
                
                // Simple pattern for basic table extraction (also support backticks)
                var simplePatterns = new[]
                {
                    @"\bFROM\s+(?:(\w+)|`([^`]+)`)",
                    @"\bJOIN\s+(?:(\w+)|`([^`]+)`)",
                    @"\bINTO\s+(?:(\w+)|`([^`]+)`)",
                    @"\bUPDATE\s+(?:(\w+)|`([^`]+)`)"
                };

                foreach (var pattern in simplePatterns)
                {
                    try
                    {
                        var matches = Regex.Matches(cleanSql, pattern, RegexOptions.IgnoreCase);
                        Console.WriteLine($"[SqlMetadataService] Simple pattern '{pattern}' found {matches.Count} matches");
                        foreach (Match match in matches)
                        {
                            var tableName = match.Groups[1].Value;
                            if (string.IsNullOrEmpty(tableName))
                            {
                                tableName = match.Groups[2].Value;
                            }
                            
                            if (!string.IsNullOrEmpty(tableName) && !IsReservedWord(tableName))
                            {
                                tables.Add(tableName);
                                Console.WriteLine($"[SqlMetadataService] Found table (simple): {tableName}");
                            }
                        }
                    }
                    catch (Exception simpleEx)
                    {
                        Console.WriteLine($"[SqlMetadataService] Error with simple pattern '{pattern}': {simpleEx.Message}");
                    }
                }
            }

            // Convert to list
            var result = new List<string>();
            foreach (var table in tables)
            {
                result.Add(table);
            }

            Console.WriteLine($"[SqlMetadataService] Total tables extracted: {result.Count} - {string.Join(", ", result)}");
            
            if (result.Count == 0)
            {
                Console.WriteLine($"[SqlMetadataService] ❌ NO TABLES FOUND! SQL Query: {sqlQuery}");
                // Return at least an error indication
                throw new InvalidOperationException("Không thể xác định bảng nào từ SQL query. Vui lòng kiểm tra lại câu truy vấn.");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SqlMetadataService] Exception in ExtractTablesFromQuery: {ex}");
            throw new InvalidOperationException($"Lỗi phân tích SQL query: {ex.Message}", ex);
        }
    }

    private static string RemoveComments(string sql)
    {
        // Remove single-line comments
        sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
        
        // Remove multi-line comments
        sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);
        
        return sql;
    }

    private static bool IsReservedWord(string word)
    {
        var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "OUTER", "ON",
            "AND", "OR", "NOT", "IN", "EXISTS", "LIKE", "BETWEEN", "IS", "NULL",
            "ORDER", "BY", "GROUP", "HAVING", "DISTINCT", "TOP", "LIMIT", "OFFSET",
            "INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP", "TABLE", "INDEX",
            "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER", "DATABASE", "SCHEMA", "AS"
        };

        return reservedWords.Contains(word);
    }
} 