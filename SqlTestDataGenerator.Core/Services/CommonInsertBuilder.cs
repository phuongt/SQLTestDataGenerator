using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Common service for building INSERT statements with consistent column filtering.
/// Eliminates duplicate code between DataGenService and CoordinatedDataGenerator.
/// Handles generated columns, identity columns, and database-specific quoting.
/// </summary>
public class CommonInsertBuilder
{
    private readonly ILogger _logger;

    public CommonInsertBuilder()
    {
        _logger = Log.ForContext<CommonInsertBuilder>();
    }

    /// <summary>
    /// Build INSERT statement with proper column filtering for generated/identity columns
    /// </summary>
    public string BuildInsertStatement(
        string tableName,
        Dictionary<string, object> record, 
        TableSchema tableSchema,
        DatabaseType databaseType)
    {
        _logger.Information("Building INSERT for table {TableName} with {RecordCount} columns", 
            tableName, record.Count);

        // Filter out generated AND identity columns consistently
        var excludedColumns = tableSchema.Columns
            .Where(c => c.IsGenerated || c.IsIdentity)
            .Select(c => c.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (excludedColumns.Any())
        {
            _logger.Information("Excluding {ExcludedCount} generated/identity columns: {ExcludedColumns}", 
                excludedColumns.Count, string.Join(", ", excludedColumns));
        }

        // Filter record to exclude generated/identity columns
        var filteredRecord = record.Where(kvp => !excludedColumns.Contains(kvp.Key)).ToList();
        
        if (filteredRecord.Count == 0)
        {
            throw new InvalidOperationException($"No columns left to insert after filtering for table {tableName}");
        }

        _logger.Information("Filtered to {FilteredCount} insertable columns for table {TableName}", 
            filteredRecord.Count, tableName);

        // Build SQL statement
        var columns = string.Join(", ", filteredRecord.Select(kvp => QuoteIdentifier(kvp.Key, databaseType)));
        var values = string.Join(", ", filteredRecord.Select(kvp => FormatValue(kvp.Value)));
        
        var sql = $"INSERT INTO {QuoteIdentifier(tableName, databaseType)} ({columns}) VALUES ({values})";
        
        _logger.Debug("Generated INSERT: {SQL}", sql.Substring(0, Math.Min(200, sql.Length)) + "...");
        
        return sql;
    }

    /// <summary>
    /// Build INSERT statement from filtered columns list (for DataGenService compatibility)
    /// </summary>
    public string BuildInsertStatement(
        string tableName,
        List<ColumnSchema> columns,
        List<string> columnValues,
        DatabaseType databaseType)
    {
        _logger.Information("Building INSERT for table {TableName} with {ColumnCount} pre-filtered columns", 
            tableName, columns.Count);

        if (columns.Count != columnValues.Count)
        {
            throw new ArgumentException($"Column count ({columns.Count}) doesn't match value count ({columnValues.Count})");
        }

        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"No columns provided for table {tableName}");
        }

        var columnNames = columns.Select(c => QuoteIdentifier(c.ColumnName, databaseType));
        var sql = $"INSERT INTO {QuoteIdentifier(tableName, databaseType)} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", columnValues)})";
        
        _logger.Debug("Generated INSERT: {SQL}", sql.Substring(0, Math.Min(200, sql.Length)) + "...");
        
        return sql;
    }

    /// <summary>
    /// Get filtered columns excluding generated and identity columns
    /// </summary>
    public List<ColumnSchema> GetInsertableColumns(TableSchema tableSchema)
    {
        var insertableColumns = tableSchema.Columns
            .Where(c => !c.IsGenerated && !c.IsIdentity)
            .ToList();

        _logger.Information("Table {TableName}: {InsertableCount}/{TotalCount} insertable columns", 
            tableSchema.TableName, insertableColumns.Count, tableSchema.Columns.Count);

        var excludedColumns = tableSchema.Columns
            .Where(c => c.IsGenerated || c.IsIdentity)
            .Select(c => $"{c.ColumnName}(Generated:{c.IsGenerated},Identity:{c.IsIdentity})")
            .ToList();

        if (excludedColumns.Any())
        {
            _logger.Information("Excluded columns: {ExcludedColumns}", string.Join(", ", excludedColumns));
        }

        return insertableColumns;
    }

    /// <summary>
    /// Quote identifier based on database type
    /// </summary>
    public string QuoteIdentifier(string identifier, DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => $"`{identifier}`",
            DatabaseType.PostgreSQL => $"\"{identifier}\"",
            DatabaseType.SqlServer => $"[{identifier}]",
            _ => identifier
        };
    }

    /// <summary>
    /// Format value for SQL statement with proper escaping
    /// </summary>
    public string FormatValue(object value)
    {
        return value switch
        {
            null => "NULL",
            string str => $"'{EscapeSqlString(str)}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            DateOnly date => $"'{date:yyyy-MM-dd}'",
            TimeOnly time => $"'{time:HH:mm:ss}'",
            bool b => b ? "1" : "0",
            decimal or double or float => value.ToString()!.Replace(",", "."), // Use dot for decimal separator
            _ => value.ToString()!
        };
    }

    /// <summary>
    /// Escape SQL string to prevent injection and handle quotes
    /// </summary>
    private static string EscapeSqlString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        return input.Replace("'", "''"); // Escape single quotes by doubling them
    }

    /// <summary>
    /// Validate that INSERT statement doesn't contain generated columns
    /// </summary>
    public bool ValidateInsertStatement(string insertSql, TableSchema tableSchema)
    {
        var generatedColumnNames = tableSchema.Columns
            .Where(c => c.IsGenerated || c.IsIdentity)
            .Select(c => c.ColumnName)
            .ToList();

        foreach (var columnName in generatedColumnNames)
        {
            // Check if generated column appears in INSERT statement
            var patterns = new[]
            {
                $"`{columnName}`",      // MySQL
                $"[{columnName}]",      // SQL Server  
                $"\"{columnName}\"",    // PostgreSQL
                $" {columnName} ",      // Unquoted (risky but possible)
                $"({columnName},",      // In column list
                $",{columnName},",      // Middle of column list
                $",{columnName})"       // End of column list
            };

            foreach (var pattern in patterns)
            {
                if (insertSql.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Error("INSERT statement contains generated column {ColumnName}: {SQL}", 
                        columnName, insertSql.Substring(0, Math.Min(200, insertSql.Length)));
                    return false;
                }
            }
        }

        _logger.Debug("INSERT statement validation passed - no generated columns found");
        return true;
    }
} 