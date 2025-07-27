using SqlTestDataGenerator.Core.Models;
using Serilog;
using SqlTestDataGenerator.Core.Abstractions;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Common service for building INSERT statements with consistent column filtering.
/// Eliminates duplicate code between DataGenService and CoordinatedDataGenerator.
/// Handles generated columns, identity columns, and database-specific quoting.
/// </summary>
public class CommonInsertBuilder
{
    private readonly ILogger _logger;
    private readonly ISqlDialectHandler _dialectHandler;

    public CommonInsertBuilder(ISqlDialectHandler dialectHandler)
    {
        _logger = Log.ForContext<CommonInsertBuilder>();
        _dialectHandler = dialectHandler;
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
        var columns = string.Join(", ", filteredRecord.Select(kvp => _dialectHandler.EscapeIdentifier(kvp.Key)));
        // 🗑️ REMOVED: Last-line safeguard that was overriding constraint-aware is_active values
        // The safeguard was preventing JOIN constraints like "ur.is_active = False" from working
        
        var values = string.Join(", ", filteredRecord.Select(kvp => 
        {
            var columnSchema = tableSchema.Columns.FirstOrDefault(c => 
                c.ColumnName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
            return FormatValue(kvp.Value, columnSchema, databaseType);
        }));
        
        // Oracle doesn't accept semicolons in ExecuteNonQueryAsync()
        var sqlTerminator = _dialectHandler.SupportedDatabaseType == DatabaseType.Oracle ? "" : ";";
        var sql = $"INSERT INTO {_dialectHandler.EscapeIdentifier(tableName)} ({columns}) VALUES ({values}){sqlTerminator}";
        
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

        // 🗑️ REMOVED: Last-line safeguard that was overriding constraint-aware is_active values
        // The safeguard was preventing JOIN constraints like "ur.is_active = False" from working

        var columnNames = columns.Select(c => _dialectHandler.EscapeIdentifier(c.ColumnName));
        
        // 🔧 CRITICAL FIX: Format values properly for Oracle date/time columns
        var formattedValues = new List<string>();
        for (int i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var value = columnValues[i];
            
            // Use FormatValue with columnSchema to ensure proper Oracle formatting
            var formattedValue = FormatValue(value, column, databaseType);
            formattedValues.Add(formattedValue);
        }
        
        // Oracle doesn't accept semicolons in ExecuteNonQueryAsync()
        var sqlTerminator = databaseType == DatabaseType.Oracle ? "" : ";";
        var sql = $"INSERT INTO {_dialectHandler.EscapeIdentifier(tableName)} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", formattedValues)}){sqlTerminator}";
        
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
            DatabaseType.Oracle => identifier, // Oracle supports both lowercase and uppercase unquoted identifiers
            _ => identifier
        };
    }

    /// <summary>
    /// Format value for SQL statement with proper escaping
    /// </summary>
    public string FormatValue(object value, DatabaseType databaseType = DatabaseType.MySQL)
    {
        // 🔧 DEBUG: Log all value formatting for troubleshooting
        if (value != null)
        {
            Console.WriteLine($"[FORMAT-DEBUG-MAIN] FormatValue called with: value='{value}' type={value.GetType().Name} dbType={databaseType}");
        }
        
        return value switch
        {
            null => "NULL",
            string str => FormatStringValue(str, databaseType),
            DateTime dt => FormatDateTime(dt, databaseType),
            DateOnly date => FormatDate(date, databaseType),
            TimeOnly time => FormatTime(time, databaseType), // Use database-specific time formatting
            bool b => b ? "1" : "0",
            decimal or double or float => value.ToString()!.Replace(",", "."), // Use dot for decimal separator
            int or long or short => value.ToString()!, // Numbers without quotes
            _ => FormatUnknownValue(value, databaseType) // 🔧 FIX: Handle unknown types properly
        };
    }
    
    /// <summary>
    /// Format string value with Oracle empty string protection
    /// </summary>
    private string FormatStringValue(string str, DatabaseType databaseType)
    {
        // 🔧 CRITICAL FIX: Oracle empty string protection
        if (databaseType == DatabaseType.Oracle && string.IsNullOrEmpty(str))
        {
            return "NULL"; // Oracle treats empty string as NULL anyway
        }
        
        if (string.IsNullOrEmpty(str))
        {
            return "''"; // For non-Oracle databases
        }
        
        // 🔧 CRITICAL FIX: Handle Oracle datetime strings that look like dates
        if (databaseType == DatabaseType.Oracle && IsLikelyDateTimeString(str))
        {
            Console.WriteLine($"[FORMAT-DEBUG] Oracle datetime string detected in FormatStringValue: '{str}'");
            if (DateTime.TryParse(str, out var parsedDateTime))
            {
                var result = FormatDateTime(parsedDateTime, databaseType);
                Console.WriteLine($"[FORMAT-DEBUG] Oracle datetime formatted in FormatStringValue: '{str}' -> '{result}'");
                return result;
            }
        }
        else
        {
            Console.WriteLine($"[FORMAT-DEBUG] FormatStringValue: '{str}' - Oracle={databaseType == DatabaseType.Oracle}, IsDateTime={IsLikelyDateTimeString(str)}");
        }
        
        return $"'{EscapeSqlString(str)}'";
    }

    /// <summary>
    /// Handle unknown value types by detecting likely datetime strings and other patterns
    /// 🔧 CRITICAL FIX: This prevents datetime strings from being output without quotes
    /// </summary>
    private string FormatUnknownValue(object value, DatabaseType databaseType)
    {
        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return "NULL";
            
        // 🔧 CRITICAL: Detect datetime patterns and format them properly with database functions
        if (IsLikelyDateTimeString(stringValue))
        {
            Console.WriteLine($"[FORMAT-DEBUG] Oracle datetime string detected: '{stringValue}'");
            // Try to parse as DateTime for proper formatting
            if (DateTime.TryParse(stringValue, out var parsedDateTime))
            {
                var result = FormatDateTime(parsedDateTime, databaseType);
                Console.WriteLine($"[FORMAT-DEBUG] Oracle datetime formatted: '{stringValue}' -> '{result}'");
                return result;
            }
            Console.WriteLine($"[FORMAT-DEBUG] Oracle datetime parse failed: '{stringValue}'");
            // If parsing fails, at least quote it as a string
            return $"'{stringValue}'";
        }
        
        // 🔧 CRITICAL: Detect numeric patterns and keep them unquoted
        if (IsLikelyNumericString(stringValue))
        {
            return stringValue;
        }
        
        // Default: treat as string and quote it
        return $"'{EscapeSqlString(stringValue)}'";
    }
    
    /// <summary>
    /// Check if a string looks like a datetime value
    /// </summary>
    private bool IsLikelyDateTimeString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        // Common datetime patterns: YYYY-MM-DD or YYYY-MM-DD HH:MM:SS
        var hasDash = value.Contains("-");
        var hasColon = value.Contains(":");
        var length = value.Length;
        var startsWithDigit = char.IsDigit(value[0]);
        var canParse = DateTime.TryParse(value, out _);
        
        Console.WriteLine($"[FORMAT-DEBUG] IsLikelyDateTimeString check for '{value}': hasDash={hasDash}, hasColon={hasColon}, length={length}, startsWithDigit={startsWithDigit}, canParse={canParse}");
        
        var result = hasDash && 
               (hasColon || length == 10) && 
               (length >= 10 && length <= 19) &&
               startsWithDigit &&
               canParse;
               
        Console.WriteLine($"[FORMAT-DEBUG] IsLikelyDateTimeString result for '{value}': {result}");
        return result;
    }
    
    /// <summary>
    /// Check if a string looks like a numeric value
    /// </summary>
    private bool IsLikelyNumericString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        return decimal.TryParse(value, out _);
    }

    /// <summary>
    /// Format value for SQL statement with proper escaping and column schema awareness
    /// </summary>
    public string FormatValue(object value, ColumnSchema? columnSchema, DatabaseType databaseType = DatabaseType.MySQL, bool isExportFile = false)
    {
        // 🔧 DEBUG: Add detailed logging for foreign key debugging
        if (columnSchema != null && (columnSchema.ColumnName.ToUpper().EndsWith("_ID") || columnSchema.ColumnName.ToUpper().EndsWith("_BY")))
        {
            Console.WriteLine($"[FORMAT-DEBUG] Foreign key column {columnSchema.ColumnName}: input_value={value} (type: {value?.GetType().Name}), dataType={columnSchema.DataType}");
        }
        
        // 🔧 CRITICAL FIX: Oracle empty string handling
        if (databaseType == DatabaseType.Oracle && columnSchema != null)
        {
            var oracleStringValue = value?.ToString();
            var dataType = columnSchema.DataType.ToUpper();
            
            // 🔧 DEBUG: Log Oracle processing
            if (columnSchema.ColumnName.ToUpper().EndsWith("_ID") || columnSchema.ColumnName.ToUpper().EndsWith("_BY"))
            {
                Console.WriteLine($"[FORMAT-DEBUG] Oracle processing for {columnSchema.ColumnName}: oracleStringValue='{oracleStringValue}', dataType={dataType}");
            }
            
            // Oracle treats empty string as NULL for numeric/date types
            if (string.IsNullOrEmpty(oracleStringValue))
            {
                Console.WriteLine($"[FORMAT-DEBUG] Oracle empty string detected for {columnSchema.ColumnName}, returning NULL");
                if (dataType.Contains("NUMBER") || dataType.Contains("TIMESTAMP") || dataType.Contains("DATE"))
                {
                    return "NULL";
                }
                else if (dataType.Contains("VARCHAR2") || dataType.Contains("CHAR") || dataType.Contains("CLOB"))
                {
                    return "NULL"; // Oracle treats empty string as NULL for string types too
                }
            }
            
            // Handle specific Oracle data types
            if (dataType == "DATE")
            {
                DateTime dt;
                if (value is DateTime d)
                    dt = d;
                else if (!DateTime.TryParse(oracleStringValue, out dt))
                    dt = DateTime.Now;
                return $"TO_DATE('{dt:yyyy-MM-dd}', 'YYYY-MM-DD')";
            }
            if (dataType.StartsWith("TIMESTAMP"))
            {
                DateTime dt;
                if (value is DateTime d)
                    dt = d;
                else if (!DateTime.TryParse(oracleStringValue, out dt))
                    dt = DateTime.Now;
                return $"TO_TIMESTAMP('{dt:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
            }
            if (dataType == "TIME")
            {
                TimeSpan ts;
                if (value is TimeSpan t)
                    ts = t;
                else if (!TimeSpan.TryParse(oracleStringValue, out ts))
                    ts = DateTime.Now.TimeOfDay;
                return $"TO_DATE('{ts:hh\\:mm\\:ss}', 'HH24:MI:SS')";
            }
            if (dataType.Contains("NUMBER"))
            {
                // 🔧 DEBUG: Log NUMBER processing
                if (columnSchema.ColumnName.ToUpper().EndsWith("_ID") || columnSchema.ColumnName.ToUpper().EndsWith("_BY"))
                {
                    Console.WriteLine($"[FORMAT-DEBUG] Processing NUMBER for {columnSchema.ColumnName}: attempting to parse '{oracleStringValue}'");
                }
                
                // Handle Oracle NUMBER type properly
                if (decimal.TryParse(oracleStringValue, out var numValue))
                {
                    var result = numValue.ToString();
                    if (columnSchema.ColumnName.ToUpper().EndsWith("_ID") || columnSchema.ColumnName.ToUpper().EndsWith("_BY"))
                    {
                        Console.WriteLine($"[FORMAT-DEBUG] NUMBER parsing successful for {columnSchema.ColumnName}: '{oracleStringValue}' -> '{result}'");
                    }
                    return result;
                }
                
                Console.WriteLine($"[FORMAT-DEBUG] NUMBER parsing FAILED for {columnSchema.ColumnName}: '{oracleStringValue}' -> NULL");
                return "NULL";
            }
        }
        // Handle date/time formatting based on database type
        if (columnSchema != null)
        {
            var dataType = columnSchema.DataType.ToLower();
            
            // MySQL: use STR_TO_DATE for proper datetime handling
            if (databaseType == DatabaseType.MySQL)
            {
                if (dataType == "datetime" || dataType == "timestamp")
                {
                    DateTime dt;
                    if (value is DateTime d)
                        dt = d;
                    else if (!DateTime.TryParse(value?.ToString(), out dt))
                        dt = DateTime.Now;
                    return $"STR_TO_DATE('{dt:yyyy-MM-dd HH:mm:ss}', '%Y-%m-%d %H:%i:%s')"; // ✅ REVERT: MySQL needs STR_TO_DATE
                }
                if (dataType == "date")
                {
                    DateTime dt;
                    if (value is DateTime d)
                        dt = d;
                    else if (!DateTime.TryParse(value?.ToString(), out dt))
                        dt = DateTime.Now;
                    return $"STR_TO_DATE('{dt:yyyy-MM-dd}', '%Y-%m-%d')"; // ✅ REVERT: MySQL needs STR_TO_DATE
                }
                if (dataType == "time")
                {
                    TimeSpan ts;
                    if (value is TimeSpan t)
                        ts = t;
                    else if (!TimeSpan.TryParse(value?.ToString(), out ts))
                        ts = DateTime.Now.TimeOfDay;
                    return $"STR_TO_DATE('{ts:hh\\:mm\\:ss}', '%H:%i:%s')"; // ✅ REVERT: MySQL needs STR_TO_DATE
                }
            }
            // Oracle: wrap datetime/timestamp/date/time bằng TO_DATE
            else if (databaseType == DatabaseType.Oracle)
            {
                // FIXED: Handle both uppercase and lowercase Oracle data types
                var normalizedDataType = dataType.ToLower();
                if (normalizedDataType == "date" || normalizedDataType == "timestamp" || normalizedDataType == "timestamp(6)")
                {
                    DateTime dt;
                    if (value is DateTime d)
                        dt = d;
                    else if (!DateTime.TryParse(value?.ToString(), out dt))
                        dt = DateTime.Now;
                    return $"TO_DATE('{dt:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
                }
            }
        }
        // Handle JSON columns specially - wrap in quotes but don't escape the JSON content
        if (columnSchema != null && IsJsonColumn(columnSchema))
        {
            // Use Error level logging to ensure it appears
            _logger.Error("🔧 FormatValue: JSON column detected by schema - column={ColumnName}, dataType={DataType}, value={Value}", 
                columnSchema.ColumnName, columnSchema.DataType, value?.ToString());
            return value switch
            {
                null => "NULL",
                string str => $"'{str}'", // JSON strings should be wrapped in single quotes
                _ => $"'{value}'"
            };
        }
            
        // 🔧 FALLBACK: Detect JSON values even when columnSchema is null or not detected
        if (value is string stringValue && IsLikelyJsonValue(stringValue))
        {
            _logger.Error("🔧 FormatValue: JSON value detected by content pattern - value={Value}", stringValue);
            return $"'{stringValue}'"; // JSON strings should be wrapped in single quotes
        }

        // Use the original FormatValue for non-JSON columns
        // 🔧 CRITICAL FIX: Ensure DateTime objects are properly formatted
        if (value is DateTime dateTimeValue)
        {
            return FormatDateTime(dateTimeValue, databaseType);
        }
        
        // 🔧 CRITICAL FIX: Check if value is already formatted as Oracle date function
        var valueString = value.ToString();
        if (valueString.StartsWith("TO_TIMESTAMP(") || valueString.StartsWith("TO_DATE("))
        {
            return valueString; // Already formatted, don't format again
        }
        
        // 🔧 CRITICAL FIX: Handle Oracle datetime strings that look like dates
        if (databaseType == DatabaseType.Oracle && IsLikelyDateTimeString(valueString))
        {
            Console.WriteLine($"[FORMAT-DEBUG] Oracle datetime string detected: '{valueString}'");
            if (DateTime.TryParse(valueString, out var parsedDateTime))
            {
                var result = FormatDateTime(parsedDateTime, databaseType);
                Console.WriteLine($"[FORMAT-DEBUG] Oracle datetime formatted: '{valueString}' -> '{result}'");
                return result;
            }
            Console.WriteLine($"[FORMAT-DEBUG] Oracle datetime parse failed: '{valueString}'");
        }
        
        return FormatValue(value, databaseType);
    }

    /// <summary>
    /// Check if a column is a JSON column based on its data type
    /// </summary>
    private bool IsJsonColumn(ColumnSchema columnSchema)
    {
        var dataType = columnSchema.DataType?.ToLower();
        var columnName = columnSchema.ColumnName?.ToLower();
        
        // Use Error level logging to ensure it appears
        _logger.Error("🔧 IsJsonColumn: column={ColumnName}, dataType={DataType}", 
            columnSchema.ColumnName, columnSchema.DataType);
        
        // Check by data type
        if (dataType == "json" || dataType == "jsonb")
        {
            _logger.Error("🔧 IsJsonColumn: Detected by dataType - TRUE");
            return true;
        }
            
        // Check by column name patterns (fallback when data type is missing)
        if (columnName != null && (columnName.Contains("permissions") || columnName.Contains("metadata") || columnName.Contains("settings")))
        {
            _logger.Error("🔧 IsJsonColumn: Detected by columnName pattern - TRUE");
            return true;
        }
            
        // Oracle stores JSON as CLOB
        if (dataType == "clob" && columnName != null && columnName.Contains("permissions"))
        {
            _logger.Error("🔧 IsJsonColumn: Detected by CLOB + permissions - TRUE");
            return true;
        }
            
        _logger.Error("🔧 IsJsonColumn: Not detected - FALSE");
        return false;
    }

    /// <summary>
    /// Check if a string value looks like JSON content (array or object)
    /// </summary>
    private bool IsLikelyJsonValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        var trimmed = value.Trim();
        
        // Check if it looks like a JSON array or object
        bool looksLikeJson = (trimmed.StartsWith("[") && trimmed.EndsWith("]")) ||
                            (trimmed.StartsWith("{") && trimmed.EndsWith("}"));
                            
        if (looksLikeJson)
        {
            _logger.Error("🔧 IsLikelyJsonValue: Detected JSON-like value - {Value}", value);
        }
        
        return looksLikeJson;
    }

    /// <summary>
    /// Check if a column is a date column based on its name or data type
    /// </summary>
    private bool IsDateColumn(ColumnSchema columnSchema)
    {
        var dataType = columnSchema.DataType?.ToLower();
        var columnName = columnSchema.ColumnName?.ToLower();
        
        // Check by data type first
        if (dataType == "date" || dataType == "datetime" || dataType == "timestamp")
            return true;
            
        // Check by column name patterns (fallback when data type is missing)
        if (columnName != null && (columnName.Contains("date") || columnName.Contains("time") || 
                                   columnName.EndsWith("_at") || columnName.EndsWith("_on")))
            return true;
            
        return false;
    }

    /// <summary>
    /// Format DateTime based on database type
    /// For Oracle: Use TO_DATE function for proper date format
    /// For MySQL: Use STR_TO_DATE function for compatibility
    /// </summary>
    private string FormatDateTime(DateTime dt, DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.Oracle => $"TO_DATE('{dt:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')", // Oracle function format
            DatabaseType.MySQL => $"STR_TO_DATE('{dt:yyyy-MM-dd HH:mm:ss}', '%Y-%m-%d %H:%i:%s')", // ✅ REVERT: MySQL needs STR_TO_DATE for proper parsing
            _ => $"'{dt:yyyy-MM-dd HH:mm:ss}'" // Fallback for other databases
        };
    }

    /// <summary>
    /// Format Date based on database type
    /// For Oracle: Use TO_DATE function with proper format
    /// For MySQL: Use STR_TO_DATE function for compatibility
    /// </summary>
    private string FormatDate(DateOnly date, DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.Oracle => $"TO_DATE('{date:yyyy-MM-dd}', 'YYYY-MM-DD')", // Oracle function format  
            DatabaseType.MySQL => $"STR_TO_DATE('{date:yyyy-MM-dd}', '%Y-%m-%d')", // ✅ REVERT: MySQL needs STR_TO_DATE
            _ => $"'{date:yyyy-MM-dd}'" // Fallback for other databases
        };
    }

    /// <summary>
    /// Format TimeOnly based on database type
    /// For Oracle: Use TO_DATE function for time (Oracle doesn't have native TIME type)
    /// For MySQL: Use TIME() function or STR_TO_DATE for compatibility
    /// </summary>
    private string FormatTime(TimeOnly time, DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.Oracle => $"TO_DATE('{time:HH:mm:ss}', 'HH24:MI:SS')", // Oracle uses DATE for time
            DatabaseType.MySQL => $"STR_TO_DATE('{time:HH:mm:ss}', '%H:%i:%s')", // MySQL TIME function
            _ => $"'{time:HH:mm:ss}'" // Fallback for other databases
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

    /// <summary>
    /// Build INSERT statement WITH ID columns included for SQL file export
    /// This includes auto-increment/identity columns with sequential values (1,2,3,...)
    /// Used specifically for file export to ensure FK references work correctly
    /// </summary>
    public string BuildInsertStatementWithIds(
        string tableName,
        Dictionary<string, object> record, 
        TableSchema tableSchema,
        DatabaseType databaseType,
        int recordIndex,
        bool isExportFile = false) // <--- thêm tham số này
    {
        _logger.Information("Building INSERT WITH IDs for export - table {TableName} record #{RecordIndex}", 
            tableName, recordIndex + 1);

        // Always include the primary key column so that reference IDs are deterministic across tables
        // (MySQL AUTO_INCREMENT still accepts explicit values when INSERTing, provided UNIQUE constraints are respected)
        var primaryKeyColumn = tableSchema.Columns.FirstOrDefault(c => c.IsPrimaryKey && c.IsIdentity);
        
        // Create record copy and add ID if found
        var recordWithId = new Dictionary<string, object>(record);
        if (primaryKeyColumn != null)
        {
            var idValue = recordIndex + 1; // Convert 0-based to 1-based ID
            recordWithId[primaryKeyColumn.ColumnName] = idValue;
            
            _logger.Information("Added ID column {IdColumn} = {IdValue} for table {TableName}", 
                primaryKeyColumn.ColumnName, idValue, tableName);
        }

        // For export, only exclude generated columns (NOT identity columns)
        var excludedColumns = tableSchema.Columns
            .Where(c => c.IsGenerated && !c.IsIdentity) // Keep Identity, exclude only Generated
            .Select(c => c.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (excludedColumns.Any())
        {
            _logger.Information("Export mode - Excluding {ExcludedCount} generated columns: {ExcludedColumns}", 
                excludedColumns.Count, string.Join(", ", excludedColumns));
        }

        // Filter record to exclude only generated columns  
        var filteredRecord = recordWithId.Where(kvp => !excludedColumns.Contains(kvp.Key)).ToList();
        
        if (filteredRecord.Count == 0)
        {
            throw new InvalidOperationException($"No columns left to insert after filtering for table {tableName}");
        }

        _logger.Information("Export filtered to {FilteredCount} columns for table {TableName} (ID included)", 
            filteredRecord.Count, tableName);

        // Build SQL statement with schema-aware value conversion
        var columns = string.Join(", ", filteredRecord.Select(kvp => _dialectHandler.EscapeIdentifier(kvp.Key)));
        var values = string.Join(", ", filteredRecord.Select(kvp => 
        {
            var columnSchema = tableSchema.Columns.FirstOrDefault(c => 
                c.ColumnName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
            var convertedValue = ConvertValueToSchemaType(kvp.Value, columnSchema, databaseType);
            // Pass isExportFile to FormatValue for robust Oracle export
            return FormatValue(convertedValue, columnSchema, databaseType, isExportFile);
        }));
        
        // Oracle doesn't accept semicolons in ExecuteNonQueryAsync()
        var sqlTerminator = databaseType == DatabaseType.Oracle ? "" : ";";
        var sql = $"INSERT INTO {_dialectHandler.EscapeIdentifier(tableName)} ({columns}) VALUES ({values}){sqlTerminator}";
        // Chỉ thêm dấu ; nếu là export file cho Oracle
        if (isExportFile && databaseType == DatabaseType.Oracle && !sql.TrimEnd().EndsWith(";"))
            sql += ";";
        _logger.Debug("Generated INSERT with ID: {SQL}", sql.Substring(0, Math.Min(200, sql.Length)) + "...");
        return sql;
    }

    /// <summary>
    /// Convert generated string values to appropriate data types based on column schema
    /// This fixes Oracle data type issues where random strings need to become numbers, dates, etc.
    /// </summary>
    private object ConvertValueToSchemaType(object value, ColumnSchema? columnSchema, DatabaseType databaseType)
    {
        if (columnSchema == null || value == null)
        {
            _logger.Debug("🔧 ConvertValueToSchemaType: columnSchema or value is null - returning original");
            return value;
        }

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
        {
            _logger.Debug("🔧 ConvertValueToSchemaType: stringValue is empty - generating default value");
            // For Oracle, we need to provide proper default values instead of empty strings
            if (databaseType == DatabaseType.Oracle && columnSchema != null)
            {
                return GenerateOracleDefaultValue(columnSchema);
            }
            return value;
        }
        
        // Fix Oracle empty string issue - Oracle treats empty string as NULL for non-VARCHAR columns
        if (databaseType == DatabaseType.Oracle && stringValue == "" && columnSchema != null)
        {
            var dataType = columnSchema.DataType.ToUpper();
            if (dataType.Contains("NUMBER") || dataType.Contains("TIMESTAMP") || dataType.Contains("DATE"))
            {
                return GenerateOracleDefaultValue(columnSchema);
            }
        }

        // 🔧 CRITICAL: Check if this is a JSON column first and skip conversion
        if (IsJsonColumn(columnSchema))
        {
            _logger.Information("🔧 ConvertValueToSchemaType: JSON column detected - skipping conversion for {ColumnName}", 
                columnSchema.ColumnName);
            return value; // Keep JSON as-is for proper formatting later
        }

        // 🔧 CRITICAL: Check if this is a date column by name when DataType is missing
        if (IsDateColumn(columnSchema))
        {
            // Always strip any surrounding quotes (single or double) from the date string
            var cleanDateString = stringValue.Trim('\'', '\"');
            if (DateTime.TryParse(cleanDateString, out var parsedDate))
            {
                _logger.Information("🔧 ConvertValueToSchemaType: Date column detected by name - converting '{CleanDateString}' to DateTime for {ColumnName}", 
                    cleanDateString, columnSchema.ColumnName);
                return parsedDate; // Convert to DateTime for proper formatting later
            }
            else
            {
                _logger.Warning("🔧 ConvertValueToSchemaType: Failed to parse date string '{DateString}' for {ColumnName} - generating default", 
                    cleanDateString, columnSchema.ColumnName);
                return DateTime.Now; // Generate default date instead of empty
            }
        }
        
        // 🔧 CRITICAL FIX: Always respect MaxLength constraint for Oracle VARCHAR2/CHAR columns
        if (databaseType == DatabaseType.Oracle && columnSchema.MaxLength.HasValue)
        {
            var dataType = columnSchema.DataType.ToUpper();
            if (dataType.Contains("VARCHAR2") || dataType.Contains("CHAR") || dataType.Contains("CLOB"))
            {
                if (stringValue.Length > columnSchema.MaxLength.Value)
                {
                    var truncated = stringValue.Substring(0, Math.Max(1, columnSchema.MaxLength.Value));
                    _logger.Information("🔧 ConvertValueToSchemaType: Truncated '{OriginalValue}' to '{TruncatedValue}' for Oracle {ColumnName} (MaxLength: {MaxLength})", 
                        stringValue, truncated, columnSchema.ColumnName, columnSchema.MaxLength.Value);
                    return truncated;
                }
            }
        }

        _logger.Information("🔧 ConvertValueToSchemaType: column={ColumnName}, dataType={DataType}, dbType={DatabaseType}, inputValue={InputValue}", 
            columnSchema.ColumnName, columnSchema.DataType, databaseType, stringValue);

        try
        {
            // Handle Oracle-specific data type conversions
            if (databaseType == DatabaseType.Oracle)
            {
                var result = columnSchema.DataType.ToUpper() switch
                {
                    "NUMBER" => ConvertToNumber(stringValue, columnSchema),
                    "TIMESTAMP" or "TIMESTAMP(6)" or "DATE" => ConvertToOracleDate(stringValue, columnSchema),
                    "VARCHAR2" or "CHAR" or "CLOB" => stringValue, // Keep as string
                    _ => value // Keep original for unknown types
                };
                
                _logger.Debug("🔧 ConvertValueToSchemaType ORACLE result: column={ColumnName}, dataType={DataType}, input={InputValue}, output={OutputValue}", 
                    columnSchema.ColumnName, columnSchema.DataType, stringValue, result);
                
                return result;
            }

            // Handle other database types
            var nonOracleResult = columnSchema.DataType.ToLower() switch
            {
                "int" or "integer" or "bigint" or "smallint" => ConvertToInteger(stringValue),
                "decimal" or "numeric" or "float" or "double" => ConvertToDecimal(stringValue),
                "bit" or "boolean" or "tinyint(1)" => ConvertToBoolean(stringValue),
                "datetime" or "timestamp" or "date" =>
                    DateTime.TryParse(stringValue, out var dtParsed) ? dtParsed : value,
                _ => value // Keep as string for other types
            };
            
            _logger.Debug("🔧 ConvertValueToSchemaType NON-ORACLE result: column={ColumnName}, dataType={DataType}, input={InputValue}, output={OutputValue}", 
                columnSchema.ColumnName, columnSchema.DataType, stringValue, nonOracleResult);
                
            return nonOracleResult;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to convert value {Value} to schema type {DataType}, keeping as string", 
                stringValue, columnSchema.DataType);
            return value;
        }
    }

    /// <summary>
    /// Generate appropriate default value for Oracle columns when empty string is encountered
    /// </summary>
    private object GenerateOracleDefaultValue(ColumnSchema columnSchema)
    {
        var dataType = columnSchema.DataType.ToUpper();
        var columnName = columnSchema.ColumnName.ToLower();
        
        if (dataType.Contains("NUMBER"))
        {
            // Boolean-like NUMBER(1) columns
            if (columnName.Contains("is_") || columnName.Contains("active") || 
                columnName.Contains("enabled") || columnName.Contains("flag") ||
                (columnSchema.NumericPrecision == 1 && columnSchema.NumericScale == 0))
            {
                return new Random().Next(0, 2); // Returns 0 or 1 as int
            }
            
            // Primary key or ID columns
            if (columnSchema.IsPrimaryKey || columnName.Contains("id"))
            {
                return new Random().Next(1, 1000);
            }
            
            // Other NUMBER columns
            return new Random().Next(1, 100);
        }
        
        if (dataType.Contains("TIMESTAMP") || dataType.Contains("DATE"))
        {
            // Return DateTime object - FormatValue will handle TO_TIMESTAMP/TO_DATE conversion
            if (columnName.Contains("created") || columnName.Contains("added"))
            {
                return DateTime.Now.AddDays(-new Random().Next(1, 365));
            }
            if (columnName.Contains("updated") || columnName.Contains("modified"))
            {
                return DateTime.Now.AddDays(-new Random().Next(1, 30));
            }
            if (columnName.Contains("expires"))
            {
                return DateTime.Now.AddDays(new Random().Next(1, 365));
            }
            return DateTime.Now;
        }
        
        if (dataType.Contains("VARCHAR2") || dataType.Contains("CHAR") || dataType.Contains("CLOB"))
        {
            return $"default_{new Random().Next(1, 1000)}";
        }
        
        return "default_value";
    }

    /// <summary>
    /// Convert string to NUMBER type based on Oracle column precision/scale
    /// </summary>
    private object ConvertToNumber(string stringValue, ColumnSchema columnSchema)
    {
        // 🎯 FIXED: For primary keys, preserve existing sequential integer values
        if (columnSchema.IsPrimaryKey)
        {
            // If the input is already a valid integer (like 1, 2, 3), preserve it!
            if (int.TryParse(stringValue, out int existingId))
            {
                _logger.Debug("🔧 ConvertToNumber: Preserving existing sequential PK ID {ExistingId} for column {ColumnName}", 
                    existingId, columnSchema.ColumnName);
                return existingId;
            }
            
            // Only generate hash-based ID if input is not a valid integer
            var pkSeed = Math.Abs(stringValue.GetHashCode());
            var generatedId = pkSeed % 1000 + 1;
            _logger.Debug("🔧 ConvertToNumber: Generated hash-based PK ID {GeneratedId} for non-integer input '{StringValue}' in column {ColumnName}", 
                generatedId, stringValue, columnSchema.ColumnName);
            return generatedId;
        }

        // 🎯 FIXED: For foreign keys, preserve existing sequential integer values too
        if (columnSchema.ColumnName.ToUpper().EndsWith("_ID"))
        {
            // If the input is already a valid integer (like 1, 2, 3), preserve it!
            if (int.TryParse(stringValue, out int existingFkId))
            {
                _logger.Debug("🔧 ConvertToNumber: Preserving existing sequential FK ID {ExistingFkId} for column {ColumnName}", 
                    existingFkId, columnSchema.ColumnName);
                return existingFkId;
            }
            
            // Only generate new FK value if input is not a valid integer
            var fkSeed = Math.Abs(stringValue.GetHashCode());
            var fkRandom = new Random(fkSeed);
            var generatedFkId = fkRandom.Next(1, 11);
            _logger.Debug("🔧 ConvertToNumber: Generated FK ID {GeneratedFkId} for non-integer input '{StringValue}' in column {ColumnName}", 
                generatedFkId, stringValue, columnSchema.ColumnName);
            return generatedFkId;
        }

        // Generate hash-based seed for consistent but varied values
        var seed = Math.Abs(stringValue.GetHashCode());
        var random = new Random(seed);

        // For boolean-like columns (IS_ACTIVE, ACTIVE flags with precision 1) – PRESERVE existing 0/1 input
        if (columnSchema.ColumnName.ToUpper().Contains("IS_") ||
            columnSchema.ColumnName.ToUpper().Contains("ACTIVE") ||
            (columnSchema.NumericPrecision == 1 && columnSchema.NumericScale == 0))
        {
            if (int.TryParse(stringValue, out var boolInt) && (boolInt == 0 || boolInt == 1))
            {
                return boolInt; // keep the intended value
            }

            // Fallback: generate deterministically based on hash but still ensure 0/1 only
            return seed % 2; // deterministic 0 or 1
        }

        // For NUMBER with scale > 0 (decimal numbers)
        if (columnSchema.NumericScale > 0)
        {
            var integerPart = random.Next(1, 10000);
            var decimalPart = random.NextDouble();
            return (decimal)(integerPart + decimalPart);
        }

        // For NUMBER with precision (integers)
        if (columnSchema.NumericPrecision > 0)
        {
            var maxValue = (int)Math.Min(Math.Pow(10, columnSchema.NumericPrecision.Value - 1), 100000);
            return random.Next(1, maxValue);
        }

        // Default integer with reasonable range
        return random.Next(1, 10000);
    }

    /// <summary>
    /// Convert string to integer
    /// </summary>
    private int ConvertToInteger(string stringValue)
    {
        if (int.TryParse(stringValue, out int result))
            return result;

        // Generate reasonable integer from string hash
        return Math.Abs(stringValue.GetHashCode() % 10000) + 1;
    }

    /// <summary>
    /// Convert string to decimal
    /// </summary>
    private decimal ConvertToDecimal(string stringValue)
    {
        if (decimal.TryParse(stringValue, out decimal result))
            return result;

        // Generate reasonable decimal from string hash
        return Math.Abs(stringValue.GetHashCode() % 10000) + 0.50m;
    }

    /// <summary>
    /// Convert string to boolean (0/1 for databases)
    /// </summary>
    private int ConvertToBoolean(string stringValue)
    {
        var v = stringValue.Trim().ToLower();
        if (v == "1" || v == "true" || v == "yes" || v == "on" || v == "y") return 1;
        if (v == "0" || v == "false" || v == "no" || v == "off" || v == "n") return 0;
        if (bool.TryParse(stringValue, out bool result))
            return result ? 1 : 0;
        _logger.Warning("[BOOLEAN] Giá trị không hợp lệ cho boolean: '{Value}', ép về 0", stringValue);
        return 0;
    }

    /// <summary>
    /// Convert string to proper Oracle DATE or TIMESTAMP format
    /// If input is a valid date, use it; otherwise generate a realistic date
    /// </summary>
    private DateTime ConvertToOracleDate(string stringValue, ColumnSchema columnSchema)
    {
        // Try to parse existing date string first
        if (DateTime.TryParse(stringValue, out DateTime parsedDate))
        {
            _logger.Debug("🔧 ConvertToOracleDate: Successfully parsed existing date {InputValue} -> {OutputValue}", 
                stringValue, parsedDate);
            return parsedDate;
        }

        // Generate deterministic date based on string hash and column type
        var seed = Math.Abs(stringValue.GetHashCode());
        var random = new Random(seed);

        // Different date ranges based on column name patterns
        var columnName = columnSchema.ColumnName.ToUpper();
        var baseDate = DateTime.Now;

        if (columnName.Contains("BIRTH") || columnName.Contains("DOB"))
        {
            // Birth dates: 18-80 years ago
            var yearsAgo = random.Next(18, 81);
            baseDate = DateTime.Now.AddYears(-yearsAgo);
        }
        else if (columnName.Contains("CREATED") || columnName.Contains("ADDED"))
        {
            // Created dates: within last 5 years
            var daysAgo = random.Next(0, 5 * 365);
            baseDate = DateTime.Now.AddDays(-daysAgo);
        }
        else if (columnName.Contains("UPDATED") || columnName.Contains("MODIFIED"))
        {
            // Updated dates: within last year
            var daysAgo = random.Next(0, 365);
            baseDate = DateTime.Now.AddDays(-daysAgo);
        }
        else if (columnName.Contains("EXPIRES") || columnName.Contains("EXPIRY"))
        {
            // Expiry dates: within next 2 years
            var daysAhead = random.Next(1, 2 * 365);
            baseDate = DateTime.Now.AddDays(daysAhead);
        }
        else if (columnName.Contains("ASSIGNED") || columnName.Contains("HIRED") || columnName.Contains("START"))
        {
            // Assignment/hire dates: within last 10 years
            var daysAgo = random.Next(0, 10 * 365);
            baseDate = DateTime.Now.AddDays(-daysAgo);
        }
        else
        {
            // Default: within last 2 years
            var daysAgo = random.Next(0, 2 * 365);
            baseDate = DateTime.Now.AddDays(-daysAgo);
        }

        // Add some random variance (±30 days) to avoid exact patterns
        var variance = random.Next(-30, 31);
        var finalDate = baseDate.AddDays(variance);

        _logger.Information("🔧 ConvertToOracleDate: Generated date for column {ColumnName}: {InputValue} -> {OutputValue}", 
            columnSchema.ColumnName, stringValue, finalDate);

        return finalDate;
    }
} 