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
    private readonly ComprehensiveConstraintExtractor _constraintExtractor;
    private readonly ConstraintValidator _constraintValidator;
    private readonly AdvancedConstraintValidator _advancedConstraintValidator; // NEW: Deep validation system
    private readonly EnhancedDependencyResolver _dependencyResolver;
    private readonly ConfigurationService _configurationService; // FIXED: Add missing field
    private readonly ILogger _logger;

    // Public property to access DataGenService for UI display
    public DataGenService DataGenService => _dataGenService;

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
        _advancedConstraintValidator = new AdvancedConstraintValidator(); // NEW: Initialize deep validator
        _configurationService = new ConfigurationService();

        // Initialize centralized logging manager
        try
        {
            CentralizedLoggingManager.Initialize();
            var logsDir = CentralizedLoggingManager.GetLogsDirectory();
            Console.WriteLine($"[EngineService] Centralized logs directory: {logsDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EngineService] Warning: Could not initialize centralized logging: {ex.Message}");
        }
    }

    public EngineService(DatabaseType databaseType, string connectionString, string? openAiApiKey = null)
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
        _advancedConstraintValidator = new AdvancedConstraintValidator(); // NEW: Initialize deep validator
        _configurationService = new ConfigurationService();

        // Initialize centralized logging manager
        try
        {
            CentralizedLoggingManager.Initialize();
            var logsDir = CentralizedLoggingManager.GetLogsDirectory();
            Console.WriteLine($"[EngineService] Centralized logs directory: {logsDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EngineService] Warning: Could not initialize centralized logging: {ex.Message}");
        }
    }

    public async Task<bool> TestConnectionAsync(string databaseType, string connectionString)
    {
        return await TestConnectionAsync(databaseType, connectionString, null);
    }

    public async Task<bool> TestConnectionAsync(string databaseType, string connectionString, SshTunnelService? sshService)
    {
        try
        {
            IDbConnection connection;
            
            if (sshService?.IsConnected == true)
            {
                // Use SSH tunnel connection
                connection = await DbConnectionFactory.CreateConnectionAsync(databaseType, connectionString, sshService);
                _logger.Information("Testing database connection via SSH tunnel on port {LocalPort}", sshService.LocalPort);
            }
            else
            {
                // Use direct connection
                connection = DbConnectionFactory.CreateConnection(databaseType, connectionString);
                _logger.Information("Testing direct database connection");
            }

            using (connection)
            {
                connection.Open();
                
                // Test with a simple query
                var testQuery = databaseType.ToLower() switch
                {
                    "sql server" => "SELECT 1",
                    "mysql" => "SELECT 1",
                    "postgresql" => "SELECT 1",
                    "oracle" => "SELECT 1 FROM DUAL",
                    _ => "SELECT 1"
                };

                await connection.QueryAsync(testQuery, commandTimeout: 300); // 5 phút timeout
                
                var connectionType = sshService?.IsConnected == true ? "SSH tunneled" : "direct";
                _logger.Information("Database connection test successful for {DatabaseType} via {ConnectionType}", databaseType, connectionType);
                return true;
            }
        }
        catch (Exception ex)
        {
            var connectionType = sshService?.IsConnected == true ? "SSH tunneled" : "direct";
            _logger.Error(ex, "Database connection test failed for {DatabaseType} via {ConnectionType}", databaseType, connectionType);
            return false;
        }
    }

    public async Task<QueryExecutionResult> ExecuteQueryWithTestDataAsync(QueryExecutionRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new QueryExecutionResult();
        var stepTimings = new Dictionary<string, TimeSpan>();
        var overallStartTime = DateTime.UtcNow;

        try
        {
            _logger.Information("Starting query execution with comprehensive constraint-aware test data generation");
            Console.WriteLine($"[EngineService] 🚀 Starting execution for {request.DatabaseType}");
            Console.WriteLine($"[EngineService] 📊 SQL Length: {request.SqlQuery.Length} characters");
            Console.WriteLine($"[EngineService] 🤖 UseAI: {request.UseAI}");
            Console.WriteLine($"[EngineService] 🎯 Desired Records: {request.DesiredRecordCount}");
            Console.WriteLine($"[EngineService] ⏱️ Overall start time: {overallStartTime:HH:mm:ss.fff}");
            
            // Track AI service availability
            if (request.UseAI)
            {
                Console.WriteLine($"[EngineService] 🤖 AI Service: ENABLED");
                Console.WriteLine($"[EngineService] 🔑 API Key available: {!string.IsNullOrEmpty(request.OpenAiApiKey)}");
                if (!string.IsNullOrEmpty(request.OpenAiApiKey))
                {
                    Console.WriteLine($"[EngineService] 🔑 API Key preview: {request.OpenAiApiKey.Substring(0, Math.Min(20, request.OpenAiApiKey.Length))}...");
                }
            }
            else
            {
                Console.WriteLine($"[EngineService] 🤖 AI Service: DISABLED (using Bogus generation)");
            }

            // Step 0: Convert SQL to MySQL syntax if needed (fix edge cases)
            var step0Start = Stopwatch.StartNew();
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
            step0Start.Stop();
            stepTimings["Step0_SQL_Conversion"] = step0Start.Elapsed;
            Console.WriteLine($"[EngineService] ✅ Step 0 (SQL Conversion): {step0Start.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[EngineService] ⏱️ Time since start: {(DateTime.UtcNow - overallStartTime).TotalSeconds:F2}s");

            // Step 1: Parse SQL và resolve ALL dependencies  
            var step1Start = Stopwatch.StartNew();
            _logger.Information("Analyzing query and resolving ALL dependencies");
            Console.WriteLine($"[EngineService] 🔍 Step 1: Analyzing query: {request.SqlQuery.Length} chars");
            Console.WriteLine($"[EngineService] ⏱️ Step 1 start time: {DateTime.UtcNow:HH:mm:ss.fff}");
            
            // Step 1.1: Get initial database schema from SQL
            var step1_1Start = Stopwatch.StartNew();
            DatabaseInfo initialDatabaseInfo;
            try
            {
                Console.WriteLine($"[EngineService] 📋 Step 1.1: Getting initial database schema...");
                Console.WriteLine($"[EngineService] ⏱️ Step 1.1 start: {DateTime.UtcNow:HH:mm:ss.fff}");
                
                initialDatabaseInfo = await _metadataService.GetDatabaseInfoAsync(
                    request.DatabaseType, 
                    request.ConnectionString, 
                    request.SqlQuery);
                
                Console.WriteLine($"[EngineService] ✅ Found {initialDatabaseInfo.Tables.Count} direct tables from SQL");
                Console.WriteLine($"[EngineService] 📋 Tables: {string.Join(", ", initialDatabaseInfo.Tables.Keys)}");
            }
            catch (Exception metaEx)
            {
                Console.WriteLine($"[EngineService] ❌ Metadata extraction failed: {metaEx.Message}");
                Console.WriteLine($"[EngineService] 🔍 Exception type: {metaEx.GetType().Name}");
                throw new InvalidOperationException($"Không thể phân tích database schema: {metaEx.Message}", metaEx);
            }
            step1_1Start.Stop();
            stepTimings["Step1_1_Initial_Schema"] = step1_1Start.Elapsed;
            Console.WriteLine($"[EngineService] ✅ Step 1.1 (Initial Schema): {step1_1Start.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[EngineService] ⏱️ Time since start: {(DateTime.UtcNow - overallStartTime).TotalSeconds:F2}s");
            
            // Step 1.2: Extract comprehensive constraints from SQL query
            var step1_2Start = Stopwatch.StartNew();
            Console.WriteLine($"[EngineService] 📋 Step 1.2: Extracting comprehensive constraints from SQL query");
            Console.WriteLine($"[EngineService] ⏱️ Step 1.2 start: {DateTime.UtcNow:HH:mm:ss.fff}");
            
            var comprehensiveConstraints = _constraintExtractor.ExtractAllConstraints(request.SqlQuery);
            
            Console.WriteLine($"[EngineService] ✅ Extracted constraints: {comprehensiveConstraints.LikePatterns.Count} LIKE, " +
                             $"{comprehensiveConstraints.JoinConstraints.Count} JOIN, " +
                             $"{comprehensiveConstraints.WhereConstraints.Count} WHERE, " +
                             $"{comprehensiveConstraints.DateConstraints.Count} DATE, " +
                             $"{comprehensiveConstraints.BooleanConstraints.Count} BOOLEAN");
            step1_2Start.Stop();
            stepTimings["Step1_2_Constraint_Extraction"] = step1_2Start.Elapsed;
            Console.WriteLine($"[EngineService] ✅ Step 1.2 (Constraint Extraction): {step1_2Start.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[EngineService] ⏱️ Time since start: {(DateTime.UtcNow - overallStartTime).TotalSeconds:F2}s");
            
            // Step 1.3: Use Enhanced Dependency Resolver để tìm ALL required tables
            var step1_3Start = Stopwatch.StartNew();
            Console.WriteLine($"[EngineService] 📋 Step 1.3: Resolving dependencies...");
            Console.WriteLine($"[EngineService] ⏱️ Step 1.3 start: {DateTime.UtcNow:HH:mm:ss.fff}");
            
            var parsedQuery = _dependencyResolver.ParseSelectQuery(request.SqlQuery, initialDatabaseInfo);
            Console.WriteLine($"[EngineService] ✅ Main tables: {string.Join(", ", parsedQuery.MainTables)}");
            Console.WriteLine($"[EngineService] ✅ ALL required tables (with dependencies): {string.Join(", ", parsedQuery.AllRequiredTables)}");
            step1_3Start.Stop();
            stepTimings["Step1_3_Dependency_Resolution"] = step1_3Start.Elapsed;
            Console.WriteLine($"[EngineService] ✅ Step 1.3 (Dependency Resolution): {step1_3Start.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[EngineService] ⏱️ Time since start: {(DateTime.UtcNow - overallStartTime).TotalSeconds:F2}s");
            
            // Step 1.4: Get FULL database schema cho tất cả required tables
            var step1_4Start = Stopwatch.StartNew();
            DatabaseInfo databaseInfo;
            try
            {
                Console.WriteLine($"[EngineService] Step 1.4: Getting full database schema for all required tables...");
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
            step1_4Start.Stop();
            stepTimings["Step1_4_Full_Schema"] = step1_4Start.Elapsed;
            Console.WriteLine($"[EngineService] Step 1.4 (Full Schema): {step1_4Start.Elapsed.TotalSeconds:F2}s");

            if (!databaseInfo.Tables.Any())
            {
                throw new InvalidOperationException("Không thể xác định bảng nào từ SQL query. Vui lòng kiểm tra lại câu truy vấn.");
            }

            _logger.Information("Found {TableCount} tables in query: {Tables}", 
                databaseInfo.Tables.Count, 
                string.Join(", ", databaseInfo.Tables.Keys));

            // Step 1.5: Truncate tables to ensure clean state and avoid duplicate keys
            var step1_5Start = Stopwatch.StartNew();
            _logger.Information("Truncating tables to ensure clean state for data generation");
            Console.WriteLine($"[EngineService] Step 1.5: Truncating {databaseInfo.Tables.Count} tables to avoid duplicate keys");
            
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
            
            step1_5Start.Stop();
            stepTimings["Step1_5_Table_Truncation"] = step1_5Start.Elapsed;
            Console.WriteLine($"[EngineService] Step 1.5 (Table Truncation): {step1_5Start.Elapsed.TotalSeconds:F2}s");
            
            step1Start.Stop();
            stepTimings["Step1_Total_Analysis"] = step1Start.Elapsed;
            Console.WriteLine($"[EngineService] Step 1 (Total Analysis): {step1Start.Elapsed.TotalSeconds:F2}s");

            // Step 2: Generate constraint-aware test data với validation và retry mechanism
            var step2Start = Stopwatch.StartNew();
            _logger.Information("Generating constraint-aware test data for {RecordCount} records with validation", request.DesiredRecordCount);
            Console.WriteLine($"[EngineService] Step 2: Generating constraint-aware data for {request.DesiredRecordCount} records, UseAI: {request.UseAI}, Current: {request.CurrentRecordCount}");
            
            // Step 2.1: Order tables by dependencies (parents first)
            var step2_1Start = Stopwatch.StartNew();
            var orderedTables = _dependencyResolver.OrderTablesByDependencies(parsedQuery.AllRequiredTables, databaseInfo);
            Console.WriteLine($"[EngineService] Generation order: {string.Join(" → ", orderedTables)}");
            step2_1Start.Stop();
            stepTimings["Step2_1_Table_Ordering"] = step2_1Start.Elapsed;
            Console.WriteLine($"[EngineService] Step 2.1 (Table Ordering): {step2_1Start.Elapsed.TotalSeconds:F2}s");
            
            List<InsertStatement> insertStatements;
            try
            {
                            // Step 2.2: Generate data with AI (this is the main bottleneck)
            var step2_2Start = Stopwatch.StartNew();
            Console.WriteLine($"[EngineService] 🤖 Step 2.2: Starting AI-powered data generation...");
            Console.WriteLine($"[EngineService] ⏱️ Step 2.2 start: {DateTime.UtcNow:HH:mm:ss.fff}");
            Console.WriteLine($"[EngineService] 🤖 AI Enabled: {request.UseAI && !string.IsNullOrEmpty(request.OpenAiApiKey)}");
            Console.WriteLine($"[EngineService] 🎯 Target Records: {request.DesiredRecordCount}");
            Console.WriteLine($"[EngineService] 📊 Current Records: {request.CurrentRecordCount}");
            Console.WriteLine($"[EngineService] 📋 Database Type: {databaseInfo.Type}");
            Console.WriteLine($"[EngineService] 📋 Tables to generate: {string.Join(", ", databaseInfo.Tables.Keys)}");
            
            // ENHANCED: Pass comprehensive constraints to data generation
            Console.WriteLine($"[EngineService] 🔄 Calling GenerateConstraintAwareDataAsync...");
            var aiGenerationStart = DateTime.UtcNow;
            
            insertStatements = await GenerateConstraintAwareDataAsync(
                databaseInfo, 
                request.SqlQuery, 
                comprehensiveConstraints,
                request.DesiredRecordCount,
                request.UseAI && !string.IsNullOrEmpty(request.OpenAiApiKey),
                request.CurrentRecordCount,
                databaseInfo.Type, // FIXED: Use DatabaseType enum
                request.ConnectionString);
            
            var aiGenerationEnd = DateTime.UtcNow;
            var aiGenerationDuration = aiGenerationEnd - aiGenerationStart;
            
            step2_2Start.Stop();
            stepTimings["Step2_2_AI_Data_Generation"] = step2_2Start.Elapsed;
            Console.WriteLine($"[EngineService] ✅ Step 2.2 (AI Data Generation): {step2_2Start.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[EngineService] 🤖 AI Generation duration: {aiGenerationDuration.TotalSeconds:F2}s");
            Console.WriteLine($"[EngineService] 📊 Generated {insertStatements.Count} constraint-aware INSERT statements");
            Console.WriteLine($"[EngineService] ⏱️ Time since start: {(DateTime.UtcNow - overallStartTime).TotalSeconds:F2}s");
                
                // Step 2.3: Re-order INSERT statements theo dependency order
                var step2_3Start = Stopwatch.StartNew();
                var reorderedInserts = ReorderInsertsByDependencies(insertStatements, orderedTables);
                insertStatements = reorderedInserts;
                Console.WriteLine($"[EngineService] Reordered INSERT statements by dependencies");
                step2_3Start.Stop();
                stepTimings["Step2_3_Reorder_Inserts"] = step2_3Start.Elapsed;
                Console.WriteLine($"[EngineService] Step 2.3 (Reorder Inserts): {step2_3Start.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception dataGenEx)
            {
                Console.WriteLine($"[EngineService] Constraint-aware data generation failed: {dataGenEx.Message}");
                throw new InvalidOperationException($"Lỗi khi generate constraint-aware test data: {dataGenEx.Message}", dataGenEx);
            }
            
            step2Start.Stop();
            stepTimings["Step2_Data_Generation"] = step2Start.Elapsed;
            Console.WriteLine($"[EngineService] Step 2 (Total Data Generation): {step2Start.Elapsed.TotalSeconds:F2}s");
            
            result.GeneratedInserts = insertStatements.Select(i => i.SqlStatement).ToList();

            // Export SQL statements to file WITH ID columns included before executing
            if (result.GeneratedInserts.Any())
            {
                var exportPath = await ExportSqlToFileWithIdsAsync(insertStatements, databaseInfo, request.DatabaseType);
                result.ExportedFilePath = exportPath;
                Console.WriteLine($"[EngineService] Exported {result.GeneratedInserts.Count} SQL statements WITH IDs to: {exportPath}");
            }

            // Step 3: Execute and COMMIT data to database
            var step3Start = Stopwatch.StartNew();
            _logger.Information("Executing test data insertion and committing to database");
            Console.WriteLine($"[EngineService] Step 3: Executing {insertStatements.Count} INSERT statements");
            
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

            // 🔧 CRITICAL FIX: Oracle-specific transaction handling
            if (request.DatabaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                // For Oracle, we need to commit each table separately to ensure FK constraints are satisfied
                await ExecuteOracleInsertsWithTableCommits(connection, insertStatements, databaseInfo, result, request);
            }
            else
            {
                // For other databases, use single transaction
                using var transaction = connection.BeginTransaction();
                try
                {
                    await ExecuteInsertsInTransaction(connection, transaction, insertStatements, databaseInfo, result, request);
                }
                catch
                {
                    transaction.Rollback();
                    Console.WriteLine($"[EngineService] Transaction rolled back due to error");
                    throw;
                }
            }

            step3Start.Stop();
            stepTimings["Step3_Database_Execution"] = step3Start.Elapsed;
            Console.WriteLine($"[EngineService] Step 3 (Database Execution): {step3Start.Elapsed.TotalSeconds:F2}s");
            
            result.Success = true;
            result.ExecutionTime = stopwatch.Elapsed;
            
            // Print detailed timing summary
            Console.WriteLine($"[EngineService] ===== DETAILED TIMING SUMMARY =====");
            Console.WriteLine($"[EngineService] Total Execution Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[EngineService] Breakdown:");
            foreach (var timing in stepTimings.OrderBy(kvp => kvp.Key))
            {
                var percentage = (timing.Value.TotalSeconds / stopwatch.Elapsed.TotalSeconds) * 100;
                Console.WriteLine($"[EngineService]   {timing.Key}: {timing.Value.TotalSeconds:F2}s ({percentage:F1}%)");
            }
            
            // Identify bottleneck
            var bottleneck = stepTimings.OrderByDescending(kvp => kvp.Value.TotalSeconds).First();
            Console.WriteLine($"[EngineService] 🚨 BOTTLENECK: {bottleneck.Key} took {bottleneck.Value.TotalSeconds:F2}s ({bottleneck.Value.TotalSeconds / stopwatch.Elapsed.TotalSeconds * 100:F1}%)");
            
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
    /// Generate test data with comprehensive constraint-aware approach (ENHANCED with Deep Validation)
    /// </summary>
    private async Task<List<InsertStatement>> GenerateConstraintAwareDataAsync(
        DatabaseInfo databaseInfo,
        string sqlQuery,
        ComprehensiveConstraints constraints,
        int desiredRecordCount,
        bool useAI,
        int currentRecordCount,
        DatabaseType databaseType,
        string connectionString)
    {
        const int maxRetries = 3;
        const double minValidationPassRate = 75.0; // Restored original value
        var methodStartTime = DateTime.UtcNow;

        Console.WriteLine($"[EngineService] 🤖 GenerateConstraintAwareDataAsync started at: {methodStartTime:HH:mm:ss.fff}");
        Console.WriteLine($"[EngineService] 🤖 AI Enabled: {useAI}");
        Console.WriteLine($"[EngineService] 🎯 Desired Records: {desiredRecordCount}");
        Console.WriteLine($"[EngineService] 📊 Current Records: {currentRecordCount}");
        Console.WriteLine($"[EngineService] 📋 Database Type: {databaseType}");
        Console.WriteLine($"[EngineService] 📋 Tables: {string.Join(", ", databaseInfo.Tables.Keys)}");

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var attemptStartTime = DateTime.UtcNow;
                Console.WriteLine($"[EngineService] 🔄 Deep constraint-aware generation attempt {attempt}/{maxRetries}");
                Console.WriteLine($"[EngineService] ⏱️ Attempt {attempt} start: {attemptStartTime:HH:mm:ss.fff}");
                
                // Generate data using existing service với AI context and comprehensive constraints
                Console.WriteLine($"[EngineService] 🔄 Calling _dataGenService.GenerateInsertStatementsAsync...");
                var dataGenStartTime = DateTime.UtcNow;
                
                var insertStatements = await _dataGenService.GenerateInsertStatementsAsync(
                    databaseInfo, 
                    sqlQuery, 
                    desiredRecordCount,
                    useAI,
                    currentRecordCount,
                    databaseType.ToString(), // FIXED: Convert enum to string
                    connectionString,
                    constraints);
                
                var dataGenEndTime = DateTime.UtcNow;
                var dataGenDuration = dataGenEndTime - dataGenStartTime;
                
                Console.WriteLine($"[EngineService] ✅ Data generation completed in {dataGenDuration.TotalSeconds:F2}s");
                Console.WriteLine($"[EngineService] 📊 Generated {insertStatements.Count} statements, running DEEP validation...");
                Console.WriteLine($"[EngineService] ⏱️ Time since method start: {(DateTime.UtcNow - methodStartTime).TotalSeconds:F2}s");
                
                // 🚀 NEW: Use AdvancedConstraintValidator for deep validation
                var deepValidationResult = _advancedConstraintValidator.ValidateWithDeepAnalysis(
                    insertStatements, 
                    constraints, 
                    sqlQuery, 
                    databaseInfo);
                
                var passRate = deepValidationResult.PassRate;
                
                Console.WriteLine($"[EngineService] Deep validation result: {deepValidationResult.PassedConstraints}/{deepValidationResult.TotalConstraints} checks passed ({passRate:F1}%)");
                
                if (deepValidationResult.IsValid || passRate >= minValidationPassRate)
                {
                    var attemptEndTime = DateTime.UtcNow;
                    var attemptDuration = attemptEndTime - attemptStartTime;
                    var totalDuration = DateTime.UtcNow - methodStartTime;
                    
                    Console.WriteLine($"[EngineService] ✅ DEEP constraint validation PASSED on attempt {attempt} (pass rate: {passRate:F1}%)");
                    Console.WriteLine($"[EngineService] ⏱️ Attempt {attempt} duration: {attemptDuration.TotalSeconds:F2}s");
                    Console.WriteLine($"[EngineService] 🏁 Total method duration: {totalDuration.TotalSeconds:F2}s");
                    
                    // Log successful constraint satisfaction with detailed breakdown
                    LogDeepConstraintSatisfaction(constraints, deepValidationResult);
                    
                    return insertStatements;
                }
                else
                {
                    Console.WriteLine($"[EngineService] ❌ DEEP constraint validation FAILED on attempt {attempt} (pass rate: {passRate:F1}%)");
                    LogDeepConstraintViolations(deepValidationResult);
                    
                    if (attempt == maxRetries)
                    {
                        var totalDuration = DateTime.UtcNow - methodStartTime;
                        Console.WriteLine($"[EngineService] ⚠️ Max retries reached, proceeding with best effort data (pass rate: {passRate:F1}%)");
                        Console.WriteLine($"[EngineService] 🏁 Total method duration: {totalDuration.TotalSeconds:F2}s");
                        return insertStatements; // Return best effort on final attempt
                    }
                    
                    // Wait before retry với exponential backoff
                    var delayMs = attempt * 200; // Increased delay for better constraint generation
                    Console.WriteLine($"[EngineService] Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EngineService] Error in deep constraint-aware generation attempt {attempt}: {ex.Message}");
                
                if (attempt == maxRetries)
                {
                    Console.WriteLine($"[EngineService] ⚠️ Falling back to standard generation after {maxRetries} failed attempts");
                    
                    // Fallback to standard generation without constraint validation
                    Console.WriteLine($"[EngineService] ⚠️ Fallback to standard generation without constraints");
                    var fallbackStartTime = DateTime.UtcNow;
                    
                    var fallbackResult = await _dataGenService.GenerateInsertStatementsAsync(
                        databaseInfo, 
                        sqlQuery, 
                        desiredRecordCount,
                        useAI,
                        currentRecordCount,
                        databaseType.ToString(), // FIXED: Convert enum to string
                        connectionString);
                    
                    var fallbackEndTime = DateTime.UtcNow;
                    var fallbackDuration = fallbackEndTime - fallbackStartTime;
                    Console.WriteLine($"[EngineService] ✅ Fallback generation completed in {fallbackDuration.TotalSeconds:F2}s");
                    
                    var totalDuration = DateTime.UtcNow - methodStartTime;
                    Console.WriteLine($"[EngineService] 🏁 GenerateConstraintAwareDataAsync completed in {totalDuration.TotalSeconds:F2}s");
                    
                    return fallbackResult;
                }
            }
        }
        
        // This should never be reached, but just in case
        throw new InvalidOperationException("Deep constraint-aware data generation failed after all retries");
    }

    /// <summary>
    /// Log successful deep constraint satisfaction with detailed breakdown
    /// </summary>
    private void LogDeepConstraintSatisfaction(ComprehensiveConstraints constraints, DeepValidationResult result)
    {
        _logger.Information($"✅ Deep constraint validation PASSED");
        _logger.Information($"📊 Validation summary: {result.PassedConstraints}/{result.TotalConstraints} constraints satisfied ({result.PassRate:F1}%)");
        
        // Log constraint type breakdown
        var constraintBreakdown = new Dictionary<string, int>
        {
            ["LIKE patterns"] = constraints.LikePatterns.Count,
            ["JOIN constraints"] = constraints.JoinConstraints.Count,
            ["Boolean constraints"] = constraints.BooleanConstraints.Count,
            ["Date constraints"] = constraints.DateConstraints.Count,
            ["WHERE constraints"] = constraints.WhereConstraints.Count
        };
        
        foreach (var kvp in constraintBreakdown.Where(x => x.Value > 0))
        {
            _logger.Information($"  - {kvp.Key}: {kvp.Value} constraints");
        }
        
        if (result.DetailedViolations.Any())
        {
            _logger.Information($"⚠️ Minor violations found ({result.DetailedViolations.Count} total):");
            foreach (var violation in result.DetailedViolations.Take(3))
            {
                _logger.Information($"  - {violation.ViolationType}: {violation.Description}");
            }
        }
    }
    
    /// <summary>
    /// Log deep constraint violations with detailed analysis
    /// </summary>
    private void LogDeepConstraintViolations(DeepValidationResult result)
    {
        _logger.Warning($"❌ Deep constraint validation FAILED");
        _logger.Warning($"📊 Validation summary: {result.PassedConstraints}/{result.TotalConstraints} constraints satisfied ({result.PassRate:F1}%)");
        
        // Group violations by type
        var violationsByType = result.DetailedViolations
            .GroupBy(v => v.ViolationType)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var kvp in violationsByType)
        {
            _logger.Warning($"🔍 {kvp.Key} violations ({kvp.Value.Count} total):");
            foreach (var violation in kvp.Value.Take(3))
            {
                _logger.Warning($"  - {violation.TableName}.{violation.ColumnName}: {violation.Description}");
            }
            
            if (kvp.Value.Count > 3)
            {
                _logger.Warning($"  ... and {kvp.Value.Count - 3} more {kvp.Key} violations");
            }
        }
    }

    /// <summary>
    /// Export generated SQL INSERT statements to file
    /// </summary>
    private async Task<string> ExportSqlToFileAsync(List<string> sqlStatements, string databaseType)
    {
        try
        {
            // Create export directory if it doesn't exist
            var exportDir = "sql-exports";
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
                Console.WriteLine($"[EngineService] Created export directory: {exportDir}");
            }

            // Generate filename with timestamp and milliseconds for uniqueness
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var uniqueId = Guid.NewGuid().ToString("N")[0..6]; // 6 chars from GUID
            var fileName = $"generated_inserts_{databaseType}_{timestamp}_{uniqueId}.sql";
            var filePath = Path.Combine(exportDir, fileName);

            // Prepare SQL content with header and foreign key management
            var sqlContent = new List<string>
            {
                $"-- Generated SQL INSERT statements",
                $"-- Database Type: {databaseType}",
                $"-- Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"-- Total statements: {sqlStatements.Count}",
                "",
                "-- Execute all statements in a transaction:",
                "-- BEGIN TRANSACTION;",
                ""
            };

            // Add foreign key constraint management for MySQL
            if (databaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                sqlContent.AddRange(new[]
                {
                    "-- Disable foreign key checks temporarily",
                    "SET FOREIGN_KEY_CHECKS = 0;",
                    ""
                });
            }

            // Add all SQL statements
            sqlContent.AddRange(sqlStatements);

            // Add footer with foreign key re-enable
            if (databaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                sqlContent.AddRange(new[]
                {
                    "",
                    "-- Re-enable foreign key checks",
                    "SET FOREIGN_KEY_CHECKS = 1;",
                    "",
                    "-- COMMIT;",
                    $"-- End of generated SQL ({sqlStatements.Count} statements)"
                });
            }
            else
            {
                sqlContent.AddRange(new[]
                {
                    "",
                    "-- COMMIT;",
                    $"-- End of generated SQL ({sqlStatements.Count} statements)"
                });
            }

            // Write to file
            await File.WriteAllLinesAsync(filePath, sqlContent);
            
            _logger.Information("Exported {StatementCount} SQL statements to file: {FilePath}", sqlStatements.Count, filePath);
            Console.WriteLine($"[EngineService] SQL export completed: {filePath}");

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export SQL statements to file");
            Console.WriteLine($"[EngineService] SQL export failed: {ex.Message}");
            
            // Return empty string if export failed
            return string.Empty;
        }
    }

    /// <summary>
    /// Export generated SQL INSERT statements to file WITH ID columns included
    /// Rebuilds INSERT statements to include auto-increment/identity columns with sequential values
    /// </summary>
    private async Task<string> ExportSqlToFileWithIdsAsync(List<InsertStatement> insertStatements, DatabaseInfo databaseInfo, string databaseType)
    {
        try
        {
            Console.WriteLine($"[EngineService] Starting SQL export WITH IDs for {insertStatements.Count} statements");
            
            // Create export directory if it doesn't exist
            var exportDir = "sql-exports";
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
                Console.WriteLine($"[EngineService] Created export directory: {exportDir}");
            }

            // Generate filename with timestamp and milliseconds for uniqueness
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var uniqueId = Guid.NewGuid().ToString("N")[0..6]; // 6 chars from GUID
            var fileName = $"generated_inserts_{databaseType}_{timestamp}_{uniqueId}.sql";
            var filePath = Path.Combine(exportDir, fileName);

            // Prepare SQL content with header and foreign key management
            var sqlContent = new List<string>
            {
                $"-- Generated SQL INSERT statements (WITH ID COLUMNS)",
                $"-- Database Type: {databaseType}",
                $"-- Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"-- Total statements: {insertStatements.Count}",
                $"-- Note: ID columns included with sequential values (1,2,3...)",
                "",
                "-- Execute all statements in a transaction:",
                "-- BEGIN TRANSACTION;",
                ""
            };

            // Add foreign key constraint management for MySQL
            if (databaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                sqlContent.AddRange(new[]
                {
                    "-- Disable foreign key checks temporarily",
                    "SET FOREIGN_KEY_CHECKS = 0;",
                    "",
                    "-- Reset auto_increment to start from 1 for all tables",
                });
                
                // Add AUTO_INCREMENT reset for each table
                var tableNames = insertStatements.Select(i => ExtractTableNameFromInsert(i.SqlStatement)).Distinct();
                foreach (var tableName in tableNames)
                {
                    sqlContent.Add($"ALTER TABLE `{tableName}` AUTO_INCREMENT = 1;");
                }
                sqlContent.Add("");
            }

            // Rebuild INSERT statements with ID columns
            var commonInsertBuilder = new CommonInsertBuilder(new MySqlDialectHandler());
            var recordCountByTable = new Dictionary<string, int>();
            
            foreach (var insertStatement in insertStatements.OrderBy(i => i.Priority))
            {
                try
                {
                    var tableName = ExtractTableNameFromInsert(insertStatement.SqlStatement);
                    
                    // Track record count per table for ID generation
                    if (!recordCountByTable.ContainsKey(tableName))
                        recordCountByTable[tableName] = 0;
                    
                    var recordIndex = recordCountByTable[tableName];
                    recordCountByTable[tableName]++;

                    // Get table schema
                    if (databaseInfo.Tables.TryGetValue(tableName, out var tableSchema))
                    {
                        // Parse original record data from INSERT statement
                        var recordData = ParseInsertStatementToRecord(insertStatement.SqlStatement, tableSchema);
                        
                        if (recordData != null)
                        {
                            // Rebuild INSERT with ID included
                            var databaseTypeEnum = Enum.Parse<DatabaseType>(databaseType, true);
                            var insertWithId = commonInsertBuilder.BuildInsertStatementWithIds(
                                tableName, recordData, tableSchema, databaseTypeEnum, recordIndex);
                            
                            sqlContent.Add(insertWithId);
                            Console.WriteLine($"[EngineService] Rebuilt INSERT for {tableName} #{recordIndex + 1} with ID");
                        }
                        else
                        {
                            // Fallback to original statement if parsing fails
                            sqlContent.Add(insertStatement.SqlStatement);
                            Console.WriteLine($"[EngineService] Using original INSERT for {tableName} (parsing failed)");
                        }
                    }
                    else
                    {
                        // Fallback to original statement if table schema not found
                        sqlContent.Add(insertStatement.SqlStatement);
                        Console.WriteLine($"[EngineService] Using original INSERT for {tableName} (schema not found)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EngineService] Error rebuilding INSERT: {ex.Message}");
                    // Fallback to original statement
                    sqlContent.Add(insertStatement.SqlStatement);
                }
            }

            // Add footer with foreign key re-enable
            if (databaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                sqlContent.AddRange(new[]
                {
                    "",
                    "-- Re-enable foreign key checks",
                    "SET FOREIGN_KEY_CHECKS = 1;",
                    "",
                    "-- COMMIT;",
                    $"-- End of generated SQL WITH IDs ({insertStatements.Count} statements)"
                });
            }
            else
            {
                sqlContent.AddRange(new[]
                {
                    "",
                    "-- COMMIT;",
                    $"-- End of generated SQL WITH IDs ({insertStatements.Count} statements)"
                });
            }

            // Write to file
            await File.WriteAllLinesAsync(filePath, sqlContent);
            
            _logger.Information("Exported {StatementCount} SQL statements WITH IDs to file: {FilePath}", insertStatements.Count, filePath);
            Console.WriteLine($"[EngineService] SQL export WITH IDs completed: {filePath}");

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export SQL statements with IDs to file");
            Console.WriteLine($"[EngineService] SQL export WITH IDs failed: {ex.Message}");
            
            // Return empty string if export failed
            return string.Empty;
        }
    }

    /// <summary>
    /// Parse INSERT statement back to record data dictionary
    /// Extracts column names and values from INSERT INTO ... VALUES (...) format
    /// </summary>
    private Dictionary<string, object>? ParseInsertStatementToRecord(string insertSql, TableSchema tableSchema)
    {
        try
        {
            // Pattern: INSERT INTO `table` (`col1`, `col2`) VALUES ('val1', 'val2');
            var match = System.Text.RegularExpressions.Regex.Match(insertSql, 
                @"INSERT INTO\s+[`']?(\w+)[`']?\s*\(([^)]+)\)\s*VALUES\s*\(([^)]+)\)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (!match.Success) return null;
            
            var columnsStr = match.Groups[2].Value;
            var valuesStr = match.Groups[3].Value;
            
            // Parse column names (remove quotes and trim)
            var columns = columnsStr.Split(',')
                .Select(c => c.Trim().Trim('`', '\'', '"'))
                .ToArray();
            
            // Parse values (basic parsing - handles quoted strings and nulls)
            var values = ParseSqlValues(valuesStr);
            
            if (columns.Length != values.Length) return null;
            
            var record = new Dictionary<string, object>();
            for (int i = 0; i < columns.Length; i++)
            {
                record[columns[i]] = values[i];
            }
            
            return record;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EngineService] Failed to parse INSERT statement: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parse SQL VALUES clause into array of values
    /// Handles basic cases: 'string', 123, NULL, etc.
    /// </summary>
    private object[] ParseSqlValues(string valuesStr)
    {
        var values = new List<object>();
        var chars = valuesStr.ToCharArray();
        var currentValue = new List<char>();
        bool inQuotes = false;
        char quoteChar = '\0';
        
        for (int i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            
            if (!inQuotes && (ch == '\'' || ch == '"'))
            {
                inQuotes = true;
                quoteChar = ch;
                // Don't include quote in value
            }
            else if (inQuotes && ch == quoteChar)
            {
                inQuotes = false;
                // Don't include quote in value
            }
            else if (!inQuotes && ch == ',')
            {
                // End of current value
                var valueStr = new string(currentValue.ToArray()).Trim();
                values.Add(ConvertSqlValue(valueStr));
                currentValue.Clear();
            }
            else
            {
                currentValue.Add(ch);
            }
        }
        
        // Add last value
        if (currentValue.Count > 0)
        {
            var valueStr = new string(currentValue.ToArray()).Trim();
            values.Add(ConvertSqlValue(valueStr));
        }
        
        return values.ToArray();
    }

    /// <summary>
    /// Convert SQL value string to appropriate object type
    /// </summary>
    private object ConvertSqlValue(string valueStr)
    {
        if (string.IsNullOrWhiteSpace(valueStr) || valueStr.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return DBNull.Value;
        
        if (int.TryParse(valueStr, out int intVal))
            return intVal;
        
        if (decimal.TryParse(valueStr, out decimal decVal))
            return decVal;
        
        if (DateTime.TryParse(valueStr, out DateTime dateVal))
            return dateVal;
        
        if (bool.TryParse(valueStr, out bool boolVal))
            return boolVal;
        
        // Return as string by default
        return valueStr;
    }

    /// <summary>
    /// Execute INSERT statements in a single transaction.
    /// </summary>
    private async Task ExecuteInsertsInTransaction(IDbConnection connection, IDbTransaction transaction, List<InsertStatement> insertStatements, DatabaseInfo databaseInfo, QueryExecutionResult result, QueryExecutionRequest request)
    {
        // CRITICAL FIX: Insert test data by table groups to avoid FK constraint violations
        int insertedStatements = 0;
        
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
        
        // Process tables in dependency order (parents first, children last)
        var tableExecutionOrder = _dependencyResolver.OrderTablesByDependencies(
            statementsByTable.Keys.ToList(), databaseInfo);
        Console.WriteLine($"[EngineService] STRICT table execution order: {string.Join(" → ", tableExecutionOrder)}");
        
        // 🔧 CRITICAL FIX: Oracle-specific foreign key constraint handling
        if (request.DatabaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[EngineService] Oracle detected - enforcing strict dependency order execution");
            Console.WriteLine($"[EngineService] Oracle doesn't support SET FOREIGN_KEY_CHECKS - using dependency order only");
        }
        
        // Insert ALL statements for each table before moving to next table
        foreach (var currentTable in tableExecutionOrder)
        {
            if (!statementsByTable.TryGetValue(currentTable, out var tableStatements))
                continue;
                
            Console.WriteLine($"[EngineService] Processing {tableStatements.Count} statements for table: {currentTable}");
            
            // 🔧 CRITICAL FIX: For Oracle, commit each table's statements to ensure FK constraints are satisfied
            if (request.DatabaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[EngineService] Oracle: Committing table {currentTable} before proceeding to next table");
            }
            
            foreach (var insert in tableStatements)
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
                    
                    // 🔧 CRITICAL FIX: For Oracle, provide more specific error information
                    if (request.DatabaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase) && 
                        insertEx.Message.Contains("ORA-02291"))
                    {
                        Console.WriteLine($"[EngineService] Oracle Foreign Key Constraint Violation detected");
                        Console.WriteLine($"[EngineService] This suggests a dependency order issue or missing parent record");
                        Console.WriteLine($"[EngineService] Current table: {currentTable}");
                        Console.WriteLine($"[EngineService] Execution order: {string.Join(" → ", tableExecutionOrder)}");
                    }
                    
                    throw new InvalidOperationException($"Lỗi khi thực thi INSERT: {insertEx.Message}\nSQL: {insert.SqlStatement}", insertEx);
                }
            }
            
            // 🔧 CRITICAL FIX: For Oracle, commit each table to ensure FK constraints are satisfied
            if (request.DatabaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Commit();
                Console.WriteLine($"[EngineService] Oracle: Committed table {currentTable} successfully");
                
                // Start new transaction for next table
                transaction = connection.BeginTransaction();
                Console.WriteLine($"[EngineService] Oracle: Started new transaction for next table");
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

        // ROLLBACK to undo data changes after displaying results
        transaction.Rollback();
        Console.WriteLine($"[EngineService] Transaction rolled back successfully - data was temporary for preview only");
        _logger.Information("Transaction rolled back successfully - {RecordCount} records were generated temporarily for preview only", result.GeneratedRecords);
    }

    /// <summary>
    /// Execute INSERT statements for Oracle with table-specific commits to satisfy FK constraints.
    /// </summary>
    private async Task ExecuteOracleInsertsWithTableCommits(IDbConnection connection, List<InsertStatement> insertStatements, DatabaseInfo databaseInfo, QueryExecutionResult result, QueryExecutionRequest request)
    {
        Console.WriteLine($"[EngineService] Oracle detected - enforcing strict dependency order execution with table-specific commits");
        Console.WriteLine($"[EngineService] Oracle doesn't support SET FOREIGN_KEY_CHECKS - using dependency order only");

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

        // Process tables in dependency order (parents first, children last)
        var tableExecutionOrder = _dependencyResolver.OrderTablesByDependencies(
            statementsByTable.Keys.ToList(), databaseInfo);
        Console.WriteLine($"[EngineService] STRICT table execution order: {string.Join(" → ", tableExecutionOrder)}");

        // Insert ALL statements for each table before moving to next table
        foreach (var currentTable in tableExecutionOrder)
        {
            if (!statementsByTable.TryGetValue(currentTable, out var tableStatements))
                continue;
                
            Console.WriteLine($"[EngineService] Processing {tableStatements.Count} statements for table: {currentTable}");
            
            // 🔧 CRITICAL FIX: For Oracle, commit each table's statements to ensure FK constraints are satisfied
            if (currentTable == "ALL_USERS") // Example: If ALL_USERS is a table that needs to be committed
            {
                Console.WriteLine($"[EngineService] Oracle: Committing table {currentTable} before proceeding to next table");
            }
            
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var insert in tableStatements)
                {
                    try
                    {
                        Console.WriteLine($"[EngineService] Executing: {insert.SqlStatement.Substring(0, Math.Min(100, insert.SqlStatement.Length))}...");
                        await connection.ExecuteAsync(insert.SqlStatement, transaction: transaction, commandTimeout: 300);
                    }
                    catch (Exception insertEx)
                    {
                        Console.WriteLine($"[EngineService] INSERT failed: {insertEx.Message}");
                        Console.WriteLine($"[EngineService] Problem statement: {insert.SqlStatement}");
                        
                        // 🔧 CRITICAL FIX: For Oracle, provide more specific error information
                        if (currentTable == "ALL_USERS" && insertEx.Message.Contains("ORA-02291"))
                        {
                            Console.WriteLine($"[EngineService] Oracle Foreign Key Constraint Violation detected");
                            Console.WriteLine($"[EngineService] This suggests a dependency order issue or missing parent record");
                            Console.WriteLine($"[EngineService] Current table: {currentTable}");
                            Console.WriteLine($"[EngineService] Execution order: {string.Join(" → ", tableExecutionOrder)}");
                        }
                        
                        throw new InvalidOperationException($"Lỗi khi thực thi INSERT: {insertEx.Message}\nSQL: {insert.SqlStatement}", insertEx);
                    }
                }
                transaction.Commit();
                Console.WriteLine($"[EngineService] Oracle: Committed table {currentTable} successfully");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[EngineService] Oracle: Transaction rolled back for table {currentTable}: {ex.Message}");
                throw; // Re-throw to be caught by the outer catch block
            }
        }

        // Execute the original query to verify results and get final query result
        Console.WriteLine($"[EngineService] Executing original query for verification");
        try
        {
            // 🚀 SIMPLIFIED: Execute original query exactly as provided by user
            var queryResult = await connection.QueryAsync(request.SqlQuery, commandTimeout: 300);
            result.ResultData = ToDataTable(queryResult);
            
            // FIXED: Generated Records = số INSERT statements thực sự được execute thành công
            // ResultData.Rows.Count chỉ là số rows từ query cuối (bị affect bởi LIMIT, WHERE, etc.)
            result.GeneratedRecords = insertStatements.Count; // All statements were attempted
            Console.WriteLine($"[EngineService] Successfully executed {result.GeneratedRecords} INSERT statements");
            Console.WriteLine($"[EngineService] Original query returned {result.ResultData.Rows.Count} rows (affected by LIMIT/WHERE)");
        }
        catch (Exception queryEx)
        {
            Console.WriteLine($"[EngineService] Original query failed: {queryEx.Message}");
            throw new InvalidOperationException($"Lỗi khi thực thi query gốc: {queryEx.Message}", queryEx);
        }

        // ROLLBACK to undo data changes after displaying results
        // No need to rollback here as each table's transaction was committed
        _logger.Information("Oracle table-specific commits completed successfully for all {RecordCount} INSERT statements", result.GeneratedRecords);
    }
} 