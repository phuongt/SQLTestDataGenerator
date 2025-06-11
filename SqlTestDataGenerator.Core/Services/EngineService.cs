using System.Data;
using System.Diagnostics;
using Dapper;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

public class EngineService
{
    private readonly SqlMetadataService _metadataService;
    private readonly DataGenService _dataGenService;
    private readonly EnhancedDependencyResolver _dependencyResolver;
    private readonly ComprehensiveConstraintExtractor _constraintExtractor;
    private readonly ConstraintValidator _constraintValidator;
    private readonly ILogger _logger;

    public EngineService(string? openAiApiKey = null)
    {
        // Initialize logging first
        const string logTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
                    Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                path: Path.Combine("logs", "engine-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: logTemplate,
                retainedFileCountLimit: 7)
            .CreateLogger();

        _logger = Log.Logger.ForContext<EngineService>();
        _logger.Information("EngineService initialized with OpenAI key: {HasKey}", !string.IsNullOrEmpty(openAiApiKey));

        // Initialize services with constraint extraction capabilities
        _metadataService = new SqlMetadataService();
        _dataGenService = new DataGenService(openAiApiKey);
        _dependencyResolver = new EnhancedDependencyResolver();
        _constraintExtractor = new ComprehensiveConstraintExtractor();
        _constraintValidator = new ConstraintValidator();

        // Check if logs directory is accessible
        try
        {
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            Console.WriteLine($"[EngineService] Logs directory confirmed: {logsDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EngineService] Warning: Could not create logs directory: {ex.Message}");
        }

        Console.WriteLine($"[EngineService] Expected log path: {Path.Combine(Directory.GetCurrentDirectory(), "logs")}");
    }

    public async Task<bool> TestConnectionAsync(string databaseType, string connectionString)
    {
        try
        {
            using var connection = DbConnectionFactory.CreateConnection(databaseType, connectionString);
            connection.Open();
            
            // Test with a simple query
            var testQuery = databaseType.ToLower() switch
            {
                "sql server" => "SELECT 1",
                "mysql" => "SELECT 1",
                "postgresql" => "SELECT 1",
                _ => "SELECT 1"
            };

            await connection.QueryAsync(testQuery, commandTimeout: 300); // 5 phút timeout
            _logger.Information("Database connection test successful for {DatabaseType}", databaseType);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Database connection test failed for {DatabaseType}", databaseType);
            return false;
        }
    }

    public async Task<QueryExecutionResult> ExecuteQueryWithTestDataAsync(QueryExecutionRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new QueryExecutionResult();

        try
        {
            _logger.Information("Starting query execution with comprehensive constraint-aware test data generation");
            Console.WriteLine($"[EngineService] Starting execution for {request.DatabaseType}");

            // Step 0: Convert SQL to MySQL syntax if needed (fix edge cases)
            if (request.DatabaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                var dateConverter = new MySQLDateFunctionConverter();
                var originalQuery = request.SqlQuery;
                request.SqlQuery = dateConverter.ConvertToMySQLSyntax(request.SqlQuery);
                
                if (originalQuery != request.SqlQuery)
                {
                    _logger.Information("Converted SQL query to MySQL syntax");
                    Console.WriteLine($"[EngineService] Converted SQL syntax for MySQL compatibility");
                }
                
                // Validate MySQL syntax
                if (!dateConverter.ValidateMySQLSyntax(request.SqlQuery))
                {
                    var suggestions = dateConverter.GetConversionSuggestions(originalQuery);
                    var suggestionText = string.Join("; ", suggestions.Select(s => $"{s.OriginalSyntax} -> {s.MySQLSyntax}"));
                    _logger.Warning("SQL contains non-MySQL syntax. Suggestions: {Suggestions}", suggestionText);
                }
            }

            // Step 1: Parse SQL và resolve ALL dependencies  
            _logger.Information("Analyzing query and resolving ALL dependencies");
            Console.WriteLine($"[EngineService] Analyzing query: {request.SqlQuery.Length} chars");
            
            // Step 1.1: Get initial database schema from SQL
            DatabaseInfo initialDatabaseInfo;
            try
            {
                initialDatabaseInfo = await _metadataService.GetDatabaseInfoAsync(
                    request.DatabaseType, 
                    request.ConnectionString, 
                    request.SqlQuery);
                Console.WriteLine($"[EngineService] Found {initialDatabaseInfo.Tables.Count} direct tables from SQL");
            }
            catch (Exception metaEx)
            {
                Console.WriteLine($"[EngineService] Metadata extraction failed: {metaEx.Message}");
                throw new InvalidOperationException($"Không thể phân tích database schema: {metaEx.Message}", metaEx);
            }
            
            // Step 1.2: Extract comprehensive constraints from SQL query
            Console.WriteLine($"[EngineService] Extracting comprehensive constraints from SQL query");
            var comprehensiveConstraints = _constraintExtractor.ExtractAllConstraints(request.SqlQuery);
            
            Console.WriteLine($"[EngineService] Extracted constraints: {comprehensiveConstraints.LikePatterns.Count} LIKE, " +
                             $"{comprehensiveConstraints.JoinConstraints.Count} JOIN, " +
                             $"{comprehensiveConstraints.WhereConstraints.Count} WHERE, " +
                             $"{comprehensiveConstraints.DateConstraints.Count} DATE, " +
                             $"{comprehensiveConstraints.BooleanConstraints.Count} BOOLEAN");
            
            // Step 1.3: Use Enhanced Dependency Resolver để tìm ALL required tables
            var parsedQuery = _dependencyResolver.ParseSelectQuery(request.SqlQuery, initialDatabaseInfo);
            Console.WriteLine($"[EngineService] Main tables: {string.Join(", ", parsedQuery.MainTables)}");
            Console.WriteLine($"[EngineService] ALL required tables (with dependencies): {string.Join(", ", parsedQuery.AllRequiredTables)}");
            
            // Step 1.4: Get FULL database schema cho tất cả required tables
            DatabaseInfo databaseInfo;
            try
            {
                // Build a fake query containing all required tables để extract full schema
                var allTablesQuery = $"SELECT * FROM {string.Join(" UNION ALL SELECT * FROM ", parsedQuery.AllRequiredTables)}";
                databaseInfo = await _metadataService.GetDatabaseInfoAsync(
                    request.DatabaseType, 
                    request.ConnectionString, 
                    allTablesQuery);
                Console.WriteLine($"[EngineService] Full schema loaded for {databaseInfo.Tables.Count} tables (including dependencies)");
            }
            catch (Exception fullMetaEx)
            {
                Console.WriteLine($"[EngineService] Full metadata extraction failed: {fullMetaEx.Message}");
                // Fallback to initial schema
                databaseInfo = initialDatabaseInfo;
            }

            if (!databaseInfo.Tables.Any())
            {
                throw new InvalidOperationException("Không thể xác định bảng nào từ SQL query. Vui lòng kiểm tra lại câu truy vấn.");
            }

            _logger.Information("Found {TableCount} tables in query: {Tables}", 
                databaseInfo.Tables.Count, 
                string.Join(", ", databaseInfo.Tables.Keys));

            // Step 1.5: Truncate tables to ensure clean state and avoid duplicate keys
            _logger.Information("Truncating tables to ensure clean state for data generation");
            Console.WriteLine($"[EngineService] Truncating {databaseInfo.Tables.Count} tables to avoid duplicate keys");
            
            try
            {
                await TruncateTablesAsync(request.DatabaseType, request.ConnectionString, databaseInfo);
                Console.WriteLine($"[EngineService] All tables truncated successfully");
            }
            catch (Exception truncateEx)
            {
                Console.WriteLine($"[EngineService] Table truncation failed: {truncateEx.Message}");
                throw new InvalidOperationException($"Lỗi khi xóa dữ liệu cũ: {truncateEx.Message}", truncateEx);
            }

            // Step 2: Generate constraint-aware test data với validation và retry mechanism
            _logger.Information("Generating constraint-aware test data for {RecordCount} records with validation", request.DesiredRecordCount);
            Console.WriteLine($"[EngineService] Generating constraint-aware data for {request.DesiredRecordCount} records, UseAI: {request.UseAI}, Current: {request.CurrentRecordCount}");
            
            // Step 2.1: Order tables by dependencies (parents first)
            var orderedTables = _dependencyResolver.OrderTablesByDependencies(parsedQuery.AllRequiredTables, databaseInfo);
            Console.WriteLine($"[EngineService] Generation order: {string.Join(" → ", orderedTables)}");
            
            List<InsertStatement> insertStatements;
            try
            {
                // ENHANCED: Pass comprehensive constraints to data generation
                insertStatements = await GenerateConstraintAwareDataAsync(
                    databaseInfo, 
                    request.SqlQuery, 
                    comprehensiveConstraints,
                    request.DesiredRecordCount,
                    request.UseAI && !string.IsNullOrEmpty(request.OpenAiApiKey),
                    request.CurrentRecordCount,
                    request.DatabaseType,
                    request.ConnectionString);
                
                Console.WriteLine($"[EngineService] Generated {insertStatements.Count} constraint-aware INSERT statements");
                
                // Step 2.2: Re-order INSERT statements theo dependency order
                var reorderedInserts = ReorderInsertsByDependencies(insertStatements, orderedTables);
                insertStatements = reorderedInserts;
                Console.WriteLine($"[EngineService] Reordered INSERT statements by dependencies");
            }
            catch (Exception dataGenEx)
            {
                Console.WriteLine($"[EngineService] Constraint-aware data generation failed: {dataGenEx.Message}");
                throw new InvalidOperationException($"Lỗi khi generate constraint-aware test data: {dataGenEx.Message}", dataGenEx);
            }
            
            result.GeneratedInserts = insertStatements.Select(i => i.SqlStatement).ToList();

            // Step 3: Execute and COMMIT data to database
            _logger.Information("Executing test data insertion and committing to database");
            Console.WriteLine($"[EngineService] Executing {insertStatements.Count} INSERT statements");
            
            using var connection = DbConnectionFactory.CreateConnection(request.DatabaseType, request.ConnectionString);
            try
            {
                connection.Open();
                Console.WriteLine($"[EngineService] Database connection opened successfully");
            }
            catch (Exception connEx)
            {
                Console.WriteLine($"[EngineService] Failed to open connection: {connEx.Message}");
                throw new InvalidOperationException($"Không thể kết nối database: {connEx.Message}", connEx);
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                // Insert test data in order of dependencies
                int insertedStatements = 0;
                foreach (var insert in insertStatements.OrderBy(i => i.Priority))
                {
                    try
                    {
                        Console.WriteLine($"[EngineService] Executing: {insert.SqlStatement.Substring(0, Math.Min(100, insert.SqlStatement.Length))}...");
                        await connection.ExecuteAsync(insert.SqlStatement, transaction: transaction, commandTimeout: 300);
                        insertedStatements++;
                    }
                    catch (Exception insertEx)
                    {
                        Console.WriteLine($"[EngineService] INSERT failed: {insertEx.Message}");
                        Console.WriteLine($"[EngineService] Problem statement: {insert.SqlStatement}");
                        throw new InvalidOperationException($"Lỗi khi thực thi INSERT: {insertEx.Message}\nSQL: {insert.SqlStatement}", insertEx);
                    }
                }
                Console.WriteLine($"[EngineService] Successfully executed {insertedStatements} INSERT statements");

                // Execute the original query to verify results and get final query result
                Console.WriteLine($"[EngineService] Executing original query for verification");
                try
                {
                    // 🚀 SIMPLIFIED: Execute original query exactly as provided by user
                    var queryResult = await connection.QueryAsync(request.SqlQuery, transaction: transaction, commandTimeout: 300);
                    result.ResultData = ToDataTable(queryResult);
                    
                    // FIXED: Generated Records = số INSERT statements thực sự được execute thành công
                    // ResultData.Rows.Count chỉ là số rows từ query cuối (bị affect bởi LIMIT, WHERE, etc.)
                    result.GeneratedRecords = insertedStatements; // Actual số INSERT statements executed
                    Console.WriteLine($"[EngineService] Successfully executed {result.GeneratedRecords} INSERT statements");
                    Console.WriteLine($"[EngineService] Original query returned {result.ResultData.Rows.Count} rows (affected by LIMIT/WHERE)");
                }
                catch (Exception queryEx)
                {
                    Console.WriteLine($"[EngineService] Original query failed: {queryEx.Message}");
                    throw new InvalidOperationException($"Lỗi khi thực thi query gốc: {queryEx.Message}", queryEx);
                }

                // COMMIT to save data permanently
                transaction.Commit();
                Console.WriteLine($"[EngineService] Transaction committed successfully");
                _logger.Information("Transaction committed successfully - {RecordCount} records added to database permanently", result.GeneratedRecords);
            }
            catch
            {
                transaction.Rollback();
                Console.WriteLine($"[EngineService] Transaction rolled back due to error");
                throw;
            }

            result.Success = true;
            result.ExecutionTime = stopwatch.Elapsed;
            
            _logger.Information("Query execution completed successfully in {ExecutionTime}ms with {ResultCount} result rows", 
                stopwatch.ElapsedMilliseconds, result.ResultData.Rows.Count);

            Console.WriteLine($"[EngineService] Execution completed successfully in {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ExecutionTime = stopwatch.Elapsed;
            
            Console.WriteLine($"[EngineService] Execution failed: {ex.Message}");
            Console.WriteLine($"[EngineService] Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[EngineService] Inner exception: {ex.InnerException.Message}");
            }
            
            _logger.Error(ex, "Query execution failed");
            return result;
        }
    }

    public async Task<QueryExecutionResult> ExecuteQueryAsync(QueryExecutionRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new QueryExecutionResult();

        try
        {
            _logger.Information("Starting query execution without test data generation");

            // Execute query directly without generating test data
            using var connection = DbConnectionFactory.CreateConnection(request.DatabaseType, request.ConnectionString);
            connection.Open();

            // Execute the query
            var queryResult = await connection.QueryAsync(request.SqlQuery, commandTimeout: 300);
            result.ResultData = ToDataTable(queryResult);
            result.GeneratedRecords = 0; // No test data generated

            result.Success = true;
            result.ExecutionTime = stopwatch.Elapsed;
            
            _logger.Information("Query execution completed successfully in {ExecutionTime}ms", 
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ExecutionTime = stopwatch.Elapsed;
            
            _logger.Error(ex, "Query execution failed");
            return result;
        }
    }

    private static DataTable ToDataTable(IEnumerable<dynamic> data)
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

    /// <summary>
    /// Reorder INSERT statements theo dependency order để tránh FK constraint violations
    /// </summary>
    private List<InsertStatement> ReorderInsertsByDependencies(List<InsertStatement> insertStatements, List<string> orderedTables)
    {
        var reordered = new List<InsertStatement>();
        
        // Group INSERT statements by table name
        var statementsByTable = new Dictionary<string, List<InsertStatement>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var statement in insertStatements)
        {
            var tableName = ExtractTableNameFromInsert(statement.SqlStatement);
            if (!string.IsNullOrEmpty(tableName))
            {
                if (!statementsByTable.ContainsKey(tableName))
                {
                    statementsByTable[tableName] = new List<InsertStatement>();
                }
                statementsByTable[tableName].Add(statement);
            }
        }
        
        // Add statements theo dependency order
        foreach (var tableName in orderedTables)
        {
            if (statementsByTable.TryGetValue(tableName, out var tableStatements))
            {
                reordered.AddRange(tableStatements);
            }
        }
        
        // Add any remaining statements that weren't matched
        foreach (var statement in insertStatements)
        {
            if (!reordered.Contains(statement))
            {
                reordered.Add(statement);
            }
        }
        
        return reordered;
    }
    
    /// <summary>
    /// Extract table name từ INSERT statement
    /// </summary>
    private string ExtractTableNameFromInsert(string insertSql)
    {
        if (string.IsNullOrEmpty(insertSql)) return string.Empty;
        
        var match = System.Text.RegularExpressions.Regex.Match(
            insertSql, 
            @"INSERT\s+INTO\s+([`\[\w]+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value.Trim('`', '[', ']') : string.Empty;
    }

    /// <summary>
    /// Truncate all tables to ensure clean state for data generation
    /// Enhanced to handle duplicate keys by ensuring complete data cleanup
    /// </summary>
    private async Task TruncateTablesAsync(string databaseType, string connectionString, DatabaseInfo databaseInfo)
    {
        _logger.Information("Starting enhanced table truncation for {TableCount} tables", databaseInfo.Tables.Count);
        Console.WriteLine($"[EngineService] Starting enhanced truncation for {databaseInfo.Tables.Count} tables");
        
        using var connection = DbConnectionFactory.CreateConnection(databaseType, connectionString);
        connection.Open();
        Console.WriteLine($"[EngineService] Connection opened for truncation");

        // Start transaction for atomic truncation
        using var transaction = connection.BeginTransaction();
        try
        {
            // For MySQL: Disable foreign key checks temporarily
            if (databaseType.ToLower() == "mysql")
            {
                try
                {
                    await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 0", transaction: transaction, commandTimeout: 300);
                    Console.WriteLine($"[EngineService] Disabled foreign key checks for MySQL");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EngineService] Warning: Could not disable foreign key checks: {ex.Message}");
                }
            }

            // Order tables by dependencies (reverse: child tables first, parent tables last)
            // This ensures we delete child records before parent records
            var tables = databaseInfo.Tables.Values
                .OrderByDescending(t => t.ForeignKeys.Count)
                .ThenBy(t => t.TableName)
                .ToList();

            Console.WriteLine($"[EngineService] Truncation order: {string.Join(" → ", tables.Select(t => t.TableName))}");

            // Phase 1: Delete all data from tables
            foreach (var table in tables)
            {
                try
                {
                    string deleteCommand = databaseType.ToLower() switch
                    {
                        "mysql" => $"DELETE FROM `{table.TableName}`",
                        "postgresql" => $"DELETE FROM \"{table.TableName}\"",
                        "sql server" => $"DELETE FROM [{table.TableName}]",
                        _ => $"DELETE FROM {table.TableName}"
                    };

                    Console.WriteLine($"[EngineService] Deleting all data from table: {table.TableName}");
                    var deletedRows = await connection.ExecuteAsync(deleteCommand, transaction: transaction, commandTimeout: 300);
                    Console.WriteLine($"[EngineService] Deleted {deletedRows} rows from {table.TableName}");
                    
                    _logger.Information("Successfully deleted all data from table: {TableName}, Rows: {DeletedRows}", 
                        table.TableName, deletedRows);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to delete data from table {TableName}: {Error}", table.TableName, ex.Message);
                    Console.WriteLine($"[EngineService] Warning: Could not delete from {table.TableName}: {ex.Message}");
                    // Continue with other tables
                }
            }

            // Phase 2: Reset auto-increment/identity counters
            Console.WriteLine($"[EngineService] Resetting auto-increment counters");
            
            if (databaseType.ToLower() == "mysql")
            {
                // Create reversed order for auto-increment reset (parent tables first)
                var reversedTables = tables.AsEnumerable().Reverse().ToList();
                foreach (var table in reversedTables)
                {
                    try
                    {
                        var resetCommand = $"ALTER TABLE `{table.TableName}` AUTO_INCREMENT = 1";
                        await connection.ExecuteAsync(resetCommand, transaction: transaction, commandTimeout: 300);
                        Console.WriteLine($"[EngineService] Reset auto-increment for: {table.TableName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EngineService] Note: Could not reset auto-increment for {table.TableName}: {ex.Message}");
                        // Non-critical, continue
                    }
                }
            }


            // Phase 3: Re-enable foreign key checks (MySQL)
            if (databaseType.ToLower() == "mysql")
            {
                try
                {
                    await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 1", transaction: transaction, commandTimeout: 300);
                    Console.WriteLine($"[EngineService] Re-enabled foreign key checks for MySQL");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EngineService] Warning: Could not re-enable foreign key checks: {ex.Message}");
                }
            }
            
            // Commit truncation transaction
            transaction.Commit();
            Console.WriteLine($"[EngineService] Enhanced truncation completed and committed successfully");
            _logger.Information("Enhanced table truncation completed successfully for all {TableCount} tables", databaseInfo.Tables.Count);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"[EngineService] Truncation failed, transaction rolled back: {ex.Message}");
            _logger.Error(ex, "Table truncation failed, transaction rolled back");
            throw new InvalidOperationException($"Enhanced table truncation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate constraint-aware test data với comprehensive validation và retry mechanism
    /// </summary>
    private async Task<List<InsertStatement>> GenerateConstraintAwareDataAsync(
        DatabaseInfo databaseInfo,
        string sqlQuery,
        ComprehensiveConstraints constraints,
        int desiredRecordCount,
        bool useAI,
        int currentRecordCount,
        string databaseType,
        string connectionString)
    {
        const int maxRetries = 5;
        const int minValidationPassRate = 60; // 60% of generated data must satisfy constraints (giảm từ 80% để tolerant hơn)
        
        _logger.Information("Starting constraint-aware data generation with {MaxRetries} max retries", maxRetries);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"[EngineService] Constraint-aware generation attempt {attempt}/{maxRetries}");
                
                // Generate data using existing service với AI context and comprehensive constraints
                var insertStatements = await _dataGenService.GenerateInsertStatementsAsync(
                    databaseInfo, 
                    sqlQuery, 
                    desiredRecordCount,
                    useAI,
                    currentRecordCount,
                    databaseType,
                    connectionString,
                    constraints);
                
                Console.WriteLine($"[EngineService] Generated {insertStatements.Count} statements, validating against constraints...");
                
                // Validate generated data against comprehensive constraints
                var validationResult = _constraintValidator.ValidateGeneratedData(insertStatements, constraints);
                
                var passRate = validationResult.TotalChecks > 0 
                    ? (validationResult.PassedChecks * 100.0 / validationResult.TotalChecks) 
                    : 100.0;
                
                Console.WriteLine($"[EngineService] Validation result: {validationResult.PassedChecks}/{validationResult.TotalChecks} checks passed ({passRate:F1}%)");
                
                if (validationResult.IsValid || passRate >= minValidationPassRate)
                {
                    Console.WriteLine($"[EngineService] ✅ Constraint validation PASSED on attempt {attempt} (pass rate: {passRate:F1}%)");
                    
                    // Log successful constraint satisfaction
                    LogConstraintSatisfaction(constraints, validationResult);
                    
                    return insertStatements;
                }
                else
                {
                    Console.WriteLine($"[EngineService] ❌ Constraint validation FAILED on attempt {attempt} (pass rate: {passRate:F1}%)");
                    LogConstraintViolations(validationResult);
                    
                    if (attempt == maxRetries)
                    {
                        Console.WriteLine($"[EngineService] ⚠️ Max retries reached, proceeding with best effort data (pass rate: {passRate:F1}%)");
                        return insertStatements; // Return best effort on final attempt
                    }
                    
                    // Wait before retry với exponential backoff
                    var delayMs = attempt * 100;
                    Console.WriteLine($"[EngineService] Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EngineService] Error in constraint-aware generation attempt {attempt}: {ex.Message}");
                
                if (attempt == maxRetries)
                {
                    Console.WriteLine($"[EngineService] ⚠️ Falling back to standard generation after {maxRetries} failed attempts");
                    
                    // Fallback to standard generation without constraint validation
                    return await _dataGenService.GenerateInsertStatementsAsync(
                        databaseInfo, 
                        sqlQuery, 
                        desiredRecordCount,
                        useAI,
                        currentRecordCount,
                        databaseType,
                        connectionString);
                }
            }
        }
        
        // This should never be reached, but just in case
        throw new InvalidOperationException("Constraint-aware data generation failed after all retries");
    }
    
    /// <summary>
    /// Log successful constraint satisfaction for debugging
    /// </summary>
    private void LogConstraintSatisfaction(ComprehensiveConstraints constraints, ValidationResult validationResult)
    {
        Console.WriteLine($"[EngineService] 🎯 CONSTRAINT SATISFACTION SUMMARY:");
        
        // LIKE constraints
        if (constraints.LikePatterns.Any())
        {
            Console.WriteLine($"[EngineService] LIKE Constraints: {constraints.LikePatterns.Count} defined");
            foreach (var like in constraints.LikePatterns)
            {
                Console.WriteLine($"[EngineService]   - {like.TableAlias}.{like.ColumnName} LIKE '{like.Pattern}' (required: {like.RequiredValue})");
            }
        }
        
        // JOIN constraints  
        if (constraints.JoinConstraints.Any())
        {
            Console.WriteLine($"[EngineService] JOIN Constraints: {constraints.JoinConstraints.Count} defined");
            foreach (var join in constraints.JoinConstraints)
            {
                Console.WriteLine($"[EngineService]   - {join.TableAlias}.{join.ColumnName} = {join.Value}");
            }
        }
        
        // Boolean constraints
        if (constraints.BooleanConstraints.Any())
        {
            Console.WriteLine($"[EngineService] BOOLEAN Constraints: {constraints.BooleanConstraints.Count} defined");
            foreach (var boolean in constraints.BooleanConstraints)
            {
                Console.WriteLine($"[EngineService]   - {boolean.TableAlias}.{boolean.ColumnName} = {boolean.Value} (bool: {boolean.BooleanValue})");
            }
        }
        
        // Date constraints
        if (constraints.DateConstraints.Any())
        {
            Console.WriteLine($"[EngineService] DATE Constraints: {constraints.DateConstraints.Count} defined");
            foreach (var date in constraints.DateConstraints)
            {
                Console.WriteLine($"[EngineService]   - {date.TableAlias}.{date.ColumnName} {date.ConstraintType} = {date.Value}");
            }
        }
        
        Console.WriteLine($"[EngineService] Total validation checks: {validationResult.TotalChecks}, Passed: {validationResult.PassedChecks}");
    }
    
    /// <summary>
    /// Log constraint violations for debugging
    /// </summary>
    private void LogConstraintViolations(ValidationResult validationResult)
    {
        Console.WriteLine($"[EngineService] ❌ CONSTRAINT VIOLATIONS:");
        foreach (var violation in validationResult.Violations)
        {
            Console.WriteLine($"[EngineService]   - {violation}");
        }
    }
} 