using SqlTestDataGenerator.Core.Models;
using System.Data;
using System.Diagnostics;
using Dapper;
using MySqlConnector;
using Npgsql;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Interface for database execution services
/// </summary>
public interface IDbExecutor
{
    /// <summary>
    /// Execute a list of SQL statements
    /// </summary>
    ExecutionResult ExecuteStatements(List<string> sqlStatements, GenerationSettings settings);

    /// <summary>
    /// Execute SQL statements asynchronously
    /// </summary>
    Task<ExecutionResult> ExecuteStatementsAsync(List<string> sqlStatements, GenerationSettings settings);

    /// <summary>
    /// Execute a single SQL statement
    /// </summary>
    ExecutionResult ExecuteStatement(string sqlStatement, GenerationSettings settings);

    /// <summary>
    /// Test database connection
    /// </summary>
    bool TestConnection(string connectionString, string databaseType);

    /// <summary>
    /// Test database connection asynchronously
    /// </summary>
    Task<bool> TestConnectionAsync(string connectionString, string databaseType);

    /// <summary>
    /// Execute query and return results
    /// </summary>
    ExecutionResult ExecuteQuery(string sqlQuery, GenerationSettings settings);

    /// <summary>
    /// Get database schema information
    /// </summary>
    DatabaseSchema GetDatabaseSchema(string connectionString, string databaseType);
}

/// <summary>
/// Database executor service with comprehensive logging and error handling
/// </summary>
public class DbExecutor : IDbExecutor
{
    private readonly ILoggerService _logger;
    private readonly Dictionary<string, Func<string, IDbConnection>> _connectionFactories;

    public DbExecutor(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactories = InitializeConnectionFactories();

        _logger.LogMethodEntry(nameof(DbExecutor));
        _logger.LogMethodExit(nameof(DbExecutor));
    }

    public ExecutionResult ExecuteStatements(List<string> sqlStatements, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(ExecuteStatements), new { statementCount = sqlStatements.Count, databaseType = settings.DatabaseType });

        var stopwatch = Stopwatch.StartNew();
        var result = new ExecutionResult();

        try
        {
            // Validate input
            if (sqlStatements == null || !sqlStatements.Any())
            {
                result.Success = false;
                result.ErrorMessage = "No SQL statements provided";
                _logger.LogError("No SQL statements provided for execution", null, nameof(ExecuteStatements));
                return result;
            }

            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                result.Success = false;
                result.ErrorMessage = "Connection string is required";
                _logger.LogError("Connection string is required for SQL execution", null, nameof(ExecuteStatements));
                return result;
            }

            _logger.LogInfo($"Starting execution of {sqlStatements.Count} SQL statements", nameof(ExecuteStatements));

            // Create database connection
            using var connection = CreateConnection(settings.ConnectionString, settings.DatabaseType);
            connection.Open();

            var connectionTime = stopwatch.Elapsed;
            _logger.LogInfo($"Database connection established in {connectionTime.TotalMilliseconds}ms", nameof(ExecuteStatements));

            // Execute statements in transaction
            using var transaction = connection.BeginTransaction();
            var executedCount = 0;
            var recordsGenerated = 0;

            try
            {
                foreach (var sqlStatement in sqlStatements)
                {
                    if (string.IsNullOrWhiteSpace(sqlStatement))
                    {
                        continue;
                    }

                    _logger.LogInfo($"Executing statement {executedCount + 1}/{sqlStatements.Count}: {sqlStatement.Substring(0, Math.Min(100, sqlStatement.Length))}...", nameof(ExecuteStatements));

                    var rowsAffected = await connection.ExecuteAsync(
                        sqlStatement,
                        transaction: transaction,
                        commandTimeout: settings.Performance.DatabaseTimeoutSeconds);

                    recordsGenerated += rowsAffected;
                    executedCount++;

                    _logger.LogInfo($"Statement executed successfully, {rowsAffected} rows affected", nameof(ExecuteStatements));
                }

                // Commit transaction
                transaction.Commit();
                _logger.LogInfo("All statements executed successfully, transaction committed", nameof(ExecuteStatements));

                result.Success = true;
                result.StatementsExecuted = executedCount;
                result.RecordsGenerated = recordsGenerated;
                result.GeneratedSql = sqlStatements;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"SQL execution failed, transaction rolled back: {ex.Message}", ex, nameof(ExecuteStatements));
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            _logger.LogError($"Database execution failed: {ex.Message}", ex, nameof(ExecuteStatements));
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            result.Performance.SqlExecutionTime = stopwatch.Elapsed;
            result.Performance.DatabaseConnectionTime = result.Performance.SqlExecutionTime; // For simplicity

            if (result.Success)
            {
                result.Performance.RecordsPerSecond = result.RecordsGenerated / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                _logger.LogInfo($"Database execution completed successfully in {stopwatch.ElapsedMilliseconds}ms, {result.RecordsGenerated} records generated", nameof(ExecuteStatements));
            }
            else
            {
                _logger.LogError($"Database execution failed in {stopwatch.ElapsedMilliseconds}ms", result.Exception, nameof(ExecuteStatements));
            }

            _logger.LogMethodExit(nameof(ExecuteStatements), new { 
                success = result.Success, 
                statementsExecuted = result.StatementsExecuted, 
                recordsGenerated = result.RecordsGenerated,
                executionTimeMs = stopwatch.ElapsedMilliseconds 
            });
        }

        return result;
    }

    public async Task<ExecutionResult> ExecuteStatementsAsync(List<string> sqlStatements, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(ExecuteStatementsAsync), new { statementCount = sqlStatements.Count, databaseType = settings.DatabaseType });

        try
        {
            var result = await Task.Run(() => ExecuteStatements(sqlStatements, settings));
            _logger.LogMethodExit(nameof(ExecuteStatementsAsync), new { success = result.Success, recordsGenerated = result.RecordsGenerated });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Async database execution failed: {ex.Message}", ex, nameof(ExecuteStatementsAsync));
            _logger.LogMethodExit(nameof(ExecuteStatementsAsync), new { success = false, error = ex.Message });
            throw;
        }
    }

    public ExecutionResult ExecuteStatement(string sqlStatement, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(ExecuteStatement), new { sqlStatement = sqlStatement.Substring(0, Math.Min(100, sqlStatement.Length)), databaseType = settings.DatabaseType });

        try
        {
            var result = ExecuteStatements(new List<string> { sqlStatement }, settings);
            _logger.LogMethodExit(nameof(ExecuteStatement), new { success = result.Success, recordsGenerated = result.RecordsGenerated });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Single statement execution failed: {ex.Message}", ex, nameof(ExecuteStatement));
            _logger.LogMethodExit(nameof(ExecuteStatement), new { success = false, error = ex.Message });
            throw;
        }
    }

    public bool TestConnection(string connectionString, string databaseType)
    {
        _logger.LogMethodEntry(nameof(TestConnection), new { databaseType });

        try
        {
            using var connection = CreateConnection(connectionString, databaseType);
            connection.Open();

            // Test with a simple query
            var testQuery = GetTestQuery(databaseType);
            var result = connection.QueryFirstOrDefault<int>(testQuery);

            _logger.LogInfo($"Connection test successful for {databaseType}", nameof(TestConnection));
            _logger.LogMethodExit(nameof(TestConnection), new { success = true });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Connection test failed for {databaseType}: {ex.Message}", ex, nameof(TestConnection));
            _logger.LogMethodExit(nameof(TestConnection), new { success = false, error = ex.Message });
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(string connectionString, string databaseType)
    {
        _logger.LogMethodEntry(nameof(TestConnectionAsync), new { databaseType });

        try
        {
            var result = await Task.Run(() => TestConnection(connectionString, databaseType));
            _logger.LogMethodExit(nameof(TestConnectionAsync), new { success = result });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Async connection test failed: {ex.Message}", ex, nameof(TestConnectionAsync));
            _logger.LogMethodExit(nameof(TestConnectionAsync), new { success = false, error = ex.Message });
            return false;
        }
    }

    public ExecutionResult ExecuteQuery(string sqlQuery, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(ExecuteQuery), new { sqlQuery = sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length)), databaseType = settings.DatabaseType });

        var stopwatch = Stopwatch.StartNew();
        var result = new ExecutionResult();

        try
        {
            if (string.IsNullOrEmpty(sqlQuery))
            {
                result.Success = false;
                result.ErrorMessage = "SQL query cannot be empty";
                _logger.LogError("SQL query cannot be empty", null, nameof(ExecuteQuery));
                return result;
            }

            using var connection = CreateConnection(settings.ConnectionString, settings.DatabaseType);
            connection.Open();

            _logger.LogInfo($"Executing query: {sqlQuery}", nameof(ExecuteQuery));

            // Execute query and get results
            var queryResult = connection.Query(sqlQuery, commandTimeout: settings.Performance.DatabaseTimeoutSeconds);
            result.ResultData = ToDataTable(queryResult);
            result.Success = true;

            _logger.LogInfo($"Query executed successfully, {result.ResultData.Rows.Count} rows returned", nameof(ExecuteQuery));
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            _logger.LogError($"Query execution failed: {ex.Message}", ex, nameof(ExecuteQuery));
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            result.Performance.SqlExecutionTime = stopwatch.Elapsed;

            _logger.LogMethodExit(nameof(ExecuteQuery), new { 
                success = result.Success, 
                rowsReturned = result.ResultData?.Rows.Count ?? 0,
                executionTimeMs = stopwatch.ElapsedMilliseconds 
            });
        }

        return result;
    }

    public DatabaseSchema GetDatabaseSchema(string connectionString, string databaseType)
    {
        _logger.LogMethodEntry(nameof(GetDatabaseSchema), new { databaseType });

        try
        {
            using var connection = CreateConnection(connectionString, databaseType);
            connection.Open();

            var schema = new DatabaseSchema
            {
                DatabaseType = databaseType,
                ConnectionString = connectionString,
                Tables = new List<TableSchema>()
            };

            // Get tables information
            var tablesQuery = GetTablesQuery(databaseType);
            var tables = connection.Query<string>(tablesQuery);

            foreach (var tableName in tables)
            {
                var tableSchema = new TableSchema
                {
                    Name = tableName,
                    Columns = new List<ColumnSchema>()
                };

                // Get columns for each table
                var columnsQuery = GetColumnsQuery(databaseType, tableName);
                var columns = connection.Query<dynamic>(columnsQuery);

                foreach (var column in columns)
                {
                    var columnSchema = new ColumnSchema
                    {
                        Name = column.COLUMN_NAME,
                        DataType = column.DATA_TYPE,
                        IsNullable = column.IS_NULLABLE?.ToString().ToUpper() == "YES",
                        MaxLength = column.CHARACTER_MAXIMUM_LENGTH,
                        DefaultValue = column.COLUMN_DEFAULT
                    };

                    tableSchema.Columns.Add(columnSchema);
                }

                schema.Tables.Add(tableSchema);
            }

            _logger.LogInfo($"Database schema retrieved: {schema.Tables.Count} tables", nameof(GetDatabaseSchema));
            _logger.LogMethodExit(nameof(GetDatabaseSchema), new { tableCount = schema.Tables.Count });
            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get database schema: {ex.Message}", ex, nameof(GetDatabaseSchema));
            _logger.LogMethodExit(nameof(GetDatabaseSchema), new { success = false, error = ex.Message });
            throw;
        }
    }

    private Dictionary<string, Func<string, IDbConnection>> InitializeConnectionFactories()
    {
        return new Dictionary<string, Func<string, IDbConnection>>(StringComparer.OrdinalIgnoreCase)
        {
            ["MySQL"] = (connectionString) => new MySqlConnection(connectionString),
            ["PostgreSQL"] = (connectionString) => new NpgsqlConnection(connectionString),
            ["SQL Server"] = (connectionString) => new SqlConnection(connectionString),
            ["SQLite"] = (connectionString) => new SqliteConnection(connectionString)
        };
    }

    private IDbConnection CreateConnection(string connectionString, string databaseType)
    {
        if (_connectionFactories.TryGetValue(databaseType, out var factory))
        {
            return factory(connectionString);
        }

        throw new NotSupportedException($"Database type '{databaseType}' is not supported");
    }

    private string GetTestQuery(string databaseType)
    {
        return databaseType.ToLower() switch
        {
            "mysql" => "SELECT 1",
            "postgresql" => "SELECT 1",
            "sql server" => "SELECT 1",
            "sqlite" => "SELECT 1",
            _ => "SELECT 1"
        };
    }

    private string GetTablesQuery(string databaseType)
    {
        return databaseType.ToLower() switch
        {
            "mysql" => "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = DATABASE()",
            "postgresql" => "SELECT tablename FROM pg_tables WHERE schemaname = 'public'",
            "sql server" => "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
            "sqlite" => "SELECT name FROM sqlite_master WHERE type = 'table'",
            _ => "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
        };
    }

    private string GetColumnsQuery(string databaseType, string tableName)
    {
        return databaseType.ToLower() switch
        {
            "mysql" => $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = DATABASE()",
            "postgresql" => $"SELECT column_name as COLUMN_NAME, data_type as DATA_TYPE, is_nullable as IS_NULLABLE, character_maximum_length as CHARACTER_MAXIMUM_LENGTH, column_default as COLUMN_DEFAULT FROM information_schema.columns WHERE table_name = '{tableName}'",
            "sql server" => $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'",
            "sqlite" => $"PRAGMA table_info({tableName})",
            _ => $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'"
        };
    }

    private DataTable ToDataTable(IEnumerable<dynamic> data)
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
}

/// <summary>
/// Database schema information
/// </summary>
public class DatabaseSchema
{
    /// <summary>
    /// Database type
    /// </summary>
    public string DatabaseType { get; set; } = string.Empty;

    /// <summary>
    /// Connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// List of tables in the database
    /// </summary>
    public List<TableSchema> Tables { get; set; } = new();
}

/// <summary>
/// Table schema information
/// </summary>
public class TableSchema
{
    /// <summary>
    /// Table name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of columns in the table
    /// </summary>
    public List<ColumnSchema> Columns { get; set; } = new();
}

/// <summary>
/// Column schema information
/// </summary>
public class ColumnSchema
{
    /// <summary>
    /// Column name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data type
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the column is nullable
    /// </summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>
    /// Maximum length for string columns
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Default value
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether the column is a primary key
    /// </summary>
    public bool IsPrimaryKey { get; set; } = false;

    /// <summary>
    /// Whether the column is auto-incrementing
    /// </summary>
    public bool IsAutoIncrement { get; set; } = false;
}