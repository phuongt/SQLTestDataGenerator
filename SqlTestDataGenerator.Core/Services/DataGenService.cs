using Bogus;
using SqlTestDataGenerator.Core.Models;
using Serilog;
using System.Text.RegularExpressions;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Abstractions;

namespace SqlTestDataGenerator.Core.Services;

public class DataGenService
{
    private readonly OpenAiService _openAiService;
    private readonly ILogger _logger;
    private readonly ISqlParser _queryParser;
    private readonly CoordinatedDataGenerator _coordinatedGenerator;
    private readonly CommonInsertBuilder _insertBuilder;
    private readonly AIEnhancedCoordinatedDataGenerator? _aiEnhancedGenerator;
    private readonly GeminiAIDataGenerationService? _geminiAIService;

    // Public property to access Gemini AI service for UI display
    public GeminiAIDataGenerationService? GeminiAIService => _geminiAIService;

    public DataGenService(string? openAiApiKey = null, ISqlParser? sqlParser = null)
    {
        _openAiService = new OpenAiService(openAiApiKey);
        _logger = Log.ForContext<DataGenService>();
        if (sqlParser != null)
            _queryParser = sqlParser;
        else
            _queryParser = new SqlQueryParserV3();
        _coordinatedGenerator = new CoordinatedDataGenerator();
        
        // Initialize with MySQL handler as default (will be replaced based on database type)
        _insertBuilder = new CommonInsertBuilder(new MySqlDialectHandler());
        
        // Initialize AI-enhanced generator if API key is provided
        if (!string.IsNullOrEmpty(openAiApiKey))
        {
            try
            {
                var metadataService = new SqlMetadataService();
                var constraintExtractor = new ConstraintExtractorService();
                _geminiAIService = new GeminiAIDataGenerationService(openAiApiKey);
                
                _aiEnhancedGenerator = new AIEnhancedCoordinatedDataGenerator(
                    _coordinatedGenerator, constraintExtractor, _geminiAIService, metadataService, _insertBuilder);
                
                _logger.Information("AI-enhanced data generation initialized with Gemini API");
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to initialize AI-enhanced generator, falling back to standard generation");
                _aiEnhancedGenerator = null;
                _geminiAIService = null;
            }
        }
    }

    public async Task<List<InsertStatement>> GenerateInsertStatementsAsync(
        DatabaseInfo databaseInfo, 
        string sqlQuery, 
        int desiredRecordCount,
        bool useAI = true,
        int currentRecordCount = 0,
        string databaseType = "",
        string connectionString = "",
        ComprehensiveConstraints? comprehensiveConstraints = null)
    {
        // 🔧 CRITICAL FIX: When UseAI=false, skip ALL AI services and go directly to Bogus generation
        if (!useAI)
        {
            _logger.Information("UseAI=false, using Bogus data generation directly");
            return GenerateBogusDataWithConstraints(databaseInfo, desiredRecordCount, sqlQuery, comprehensiveConstraints, databaseType);
        }
        
        // 🚀 NEW: Try AI-enhanced generation first if available and enabled
        if (useAI && _aiEnhancedGenerator != null)
        {
            try
            {
                _logger.Information("Using AI-enhanced coordinated data generation");
                var aiStatements = await _aiEnhancedGenerator.GenerateIntelligentDataAsync(
                    databaseInfo, sqlQuery, desiredRecordCount, databaseType, connectionString);
                
                if (aiStatements.Any())
                {
                    _logger.Information("Successfully generated {Count} AI-enhanced statements", aiStatements.Count);
                    return aiStatements;
                }
                else
                {
                    _logger.Warning("AI-enhanced generator returned empty list, falling back");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "AI-enhanced generation failed, falling back to coordinated approach");
            }
        }

        // 🚀 Try coordinated data generation for complex queries
        if (IsComplexQuery(sqlQuery))
        {
            try
            {
                _logger.Information("Using coordinated data generation for complex query");
                var coordinatedStatements = await _coordinatedGenerator.GenerateCoordinatedDataAsync(
                    databaseInfo, sqlQuery, desiredRecordCount, databaseType, connectionString);
                
                if (coordinatedStatements.Any())
                {
                    _logger.Information("Successfully generated {Count} coordinated statements", coordinatedStatements.Count);
                    return coordinatedStatements;
                }
                else
                {
                    _logger.Warning("CoordinatedDataGenerator returned empty list, falling back");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Coordinated generation failed, falling back to standard approach");
            }
        }
        else
        {
            _logger.Information("Query not classified as complex, using standard generation");
        }

        if (useAI)
        {
            _logger.Information("Attempting to generate data using AI");
            var aiStatements = await _openAiService.GenerateInsertStatementsAsync(databaseInfo, sqlQuery, desiredRecordCount, currentRecordCount);
            
            if (aiStatements.Any())
            {
                _logger.Information("Successfully generated {Count} statements using AI", aiStatements.Count);
                return aiStatements;
            }
        }

        _logger.Information("Falling back to Bogus data generation with SQL requirements and comprehensive constraints");
        return GenerateBogusDataWithConstraints(databaseInfo, desiredRecordCount, sqlQuery, comprehensiveConstraints, databaseType);
    }

    /// <summary>
    /// Determine if query is complex enough to benefit from coordinated generation
    /// </summary>
    private bool IsComplexQuery(string sqlQuery)
    {
        if (string.IsNullOrEmpty(sqlQuery))
        {
            return false;
        }

        var queryLower = sqlQuery.ToLower();
        
        // Check for multiple tables (JOINs)
        var hasJoins = queryLower.Contains("join");
        
        // Check for complex WHERE conditions
        var hasLikeConditions = queryLower.Contains("like");
        var hasYearConditions = queryLower.Contains("year(");
        var hasMultipleConditions = queryLower.Split("and").Length > 2;
        
        // 🐛 DEBUG: Log detection results
        _logger.Information("Complex query detection: hasJoins={HasJoins}, hasLikeConditions={HasLike}, hasYearConditions={HasYear}, hasMultipleConditions={HasMultiple}", 
            hasJoins, hasLikeConditions, hasYearConditions, hasMultipleConditions);
        
        // Complex if has JOINs AND specific WHERE conditions
        var isComplex = hasJoins && (hasLikeConditions || hasYearConditions || hasMultipleConditions);
        
        _logger.Information("Query classified as complex: {IsComplex}", isComplex);
        
        return isComplex;
    }

    /// <summary>
    /// Generate Bogus data with comprehensive constraints and database type support
    /// </summary>
    private List<InsertStatement> GenerateBogusDataWithConstraints(
        DatabaseInfo databaseInfo, 
        int recordCount, 
        string sqlQuery, 
        ComprehensiveConstraints? comprehensiveConstraints,
        string databaseType = "")
    {
        // 🔧 CRITICAL FIX: Use appropriate dialect handler based on database type
        var dialectHandler = CreateDialectHandler(databaseType, databaseInfo.Type);
        var insertBuilder = new CommonInsertBuilder(dialectHandler);
        
        _logger.Information("Using dialect handler: {DialectType} for database type: {DatabaseType}", 
            dialectHandler.GetType().Name, databaseType);
        
        return GenerateBogusData(databaseInfo, recordCount, sqlQuery, comprehensiveConstraints, insertBuilder);
    }

    /// <summary>
    /// Create appropriate dialect handler based on database type
    /// </summary>
    private ISqlDialectHandler CreateDialectHandler(string databaseType, DatabaseType dbType)
    {
        // Try to parse from string first
        if (!string.IsNullOrEmpty(databaseType))
        {
            if (databaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                return new OracleDialectHandler();
            }
            if (databaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                return new MySqlDialectHandler();
            }
        }
        
        // Fall back to DatabaseType enum
        return dbType switch
        {
            DatabaseType.Oracle => new OracleDialectHandler(),
            DatabaseType.MySQL => new MySqlDialectHandler(),
            _ => new MySqlDialectHandler() // Default fallback
        };
    }

    public List<InsertStatement> GenerateBogusData(DatabaseInfo databaseInfo, int recordCount, string sqlQuery = "", ComprehensiveConstraints? comprehensiveConstraints = null, CommonInsertBuilder? insertBuilder = null)
    {
        var insertStatements = new List<InsertStatement>();
        var faker = new Faker();
        
        // Use provided insertBuilder or default one
        var builder = insertBuilder ?? _insertBuilder;
        
        // 🚀 NEW: Parse SQL query to extract WHERE conditions and requirements
        var sqlRequirements = _queryParser.ParseQuery(sqlQuery);
        _logger.Information("Parsed SQL requirements: {WhereCount} WHERE conditions, {JoinCount} JOIN requirements", 
            sqlRequirements.WhereConditions.Count, sqlRequirements.JoinRequirements.Count);
        
        // Create table alias mapping for JOIN requirements
        var aliasToTableMapping = CreateAliasMapping(sqlQuery, databaseInfo);
        
        // Order tables by dependencies (tables with no FK first)
        var tables = databaseInfo.Tables.Values
            .OrderBy(t => t.ForeignKeys.Count)
            .ThenBy(t => t.TableName);

        // Track generated primary keys for FK references
        var generatedPrimaryKeys = new Dictionary<string, List<int>>();
        
        // Track unique combinations for composite keys (specifically for user_roles)
        var usedCombinations = new HashSet<string>();

        // FIXED: Calculate records per table to achieve target final result
        // For complex join queries, we need to ensure enough records in each table
        var actualRecordCount = CalculateRequiredRecordsPerTable(recordCount, tables.Count());

        // Initialize PK tracking for all tables
        foreach (var table in tables)
        {
            generatedPrimaryKeys[table.TableName] = new List<int>();
        }

        foreach (var table in tables)
        {
            // 🎯 FIXED: Use GetInsertableColumns to properly filter generated/identity columns
            var columns = builder.GetInsertableColumns(table);
            
            var tableRecordCount = actualRecordCount;
            _logger.Information("Generating {RecordCount} records for table {TableName} with {ColumnCount} columns", 
                tableRecordCount, table.TableName, columns.Count);

            // 🎯 GENERIC junction table detection (no hardcode table names)
            if (IsJunctionTable(table))
            {
                var foreignKeyColumns = columns.Where(c => IsLikelyForeignKey(c)).ToList();
                var nonFkColumns = columns.Where(c => !IsLikelyForeignKey(c)).ToList();
                
                for (int i = 1; i <= tableRecordCount; i++)
                {
                    var fkValues = GenerateUniqueForeignKeyCombination(faker, foreignKeyColumns, i, generatedPrimaryKeys, usedCombinations, tableRecordCount);
                    var allValues = new List<string>();
                    
                    // Add FK values first
                    allValues.AddRange(fkValues);
                    
                    // Add non-FK columns
                    foreach (var column in nonFkColumns)
                    {
                        var value = GenerateBogusValue(faker, column, i, databaseInfo, tableRecordCount, sqlQuery, table.TableName, sqlRequirements, comprehensiveConstraints);
                        
                        // 🔧 FIX: Properly format DateTime objects to avoid culture-dependent formatting
                        string formattedValue;
                        if (value is DateTime dateTimeValue)
                        {
                            formattedValue = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");
                            Console.WriteLine($"[DEBUG-DATE-FORMAT-JUNCTION] {table.TableName}.{column.ColumnName}: DateTime '{dateTimeValue}' -> Formatted '{formattedValue}'");
                        }
                        else
                        {
                            formattedValue = value.ToString();
                        }
                        
                        allValues.Add(formattedValue);
                    }
                    
                    // Reorder columns to match FK columns first  
                    var orderedColumns = new List<ColumnSchema>();
                    orderedColumns.AddRange(foreignKeyColumns);
                    orderedColumns.AddRange(nonFkColumns);
                    
                    // 🎯 FIXED: Use provided CommonInsertBuilder with correct dialect
                    var sql = builder.BuildInsertStatement(table.TableName, orderedColumns, allValues, databaseInfo.Type);
                    
                    Console.WriteLine($"[DEBUG] Generated SQL for {table.TableName}: {sql}");
                    
                    insertStatements.Add(new InsertStatement
                    {
                        TableName = table.TableName,
                        SqlStatement = sql,
                        Priority = table.ForeignKeys.Count > 0 ? 1 : 0
                    });
                }
            }
            else
            {
                // 🎯 REGULAR TABLES: Generate data based on SQL requirements for each column
                for (int i = 1; i <= tableRecordCount; i++)
                {
                    var columnValues = new List<string>();
                    
                    foreach (var column in columns)
                    {
                        var value = GenerateBogusValue(faker, column, i, databaseInfo, tableRecordCount, sqlQuery, table.TableName, sqlRequirements, comprehensiveConstraints);
                        
                        // 🔧 FIX: Properly format DateTime objects to avoid culture-dependent formatting
                        string formattedValue;
                        if (value is DateTime dateTimeValue)
                        {
                            formattedValue = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");
                            Console.WriteLine($"[DEBUG-DATE-FORMAT] {table.TableName}.{column.ColumnName}: DateTime '{dateTimeValue}' -> Formatted '{formattedValue}'");
                        }
                        else
                        {
                            formattedValue = value.ToString();
                        }
                        
                        columnValues.Add(formattedValue);
                        
                        // Track primary key values for FK references
                        if (column.IsPrimaryKey && column.DataType.ToLower().Contains("int"))
                        {
                            if (int.TryParse(formattedValue, out int pkValue))
                            {
                                generatedPrimaryKeys[table.TableName].Add(pkValue);
                            }
                        }
                    }
                    
                    // 🎯 FIXED: Use provided CommonInsertBuilder with correct dialect
                    var sql = builder.BuildInsertStatement(table.TableName, columns, columnValues, databaseInfo.Type);
                    
                    // DEBUG: Log the generated SQL
                    Console.WriteLine($"[DEBUG-T021] Generated SQL for {table.TableName}: {sql}");
                    
                    insertStatements.Add(new InsertStatement
                    {
                        TableName = table.TableName,
                        SqlStatement = sql,
                        Priority = table.ForeignKeys.Count > 0 ? 1 : 0
                    });
                }
            }
        }

        return insertStatements;
    }

    /// <summary>
    /// Create mapping between table aliases and actual table names from SQL query
    /// </summary>
    private Dictionary<string, string> CreateAliasMapping(string sqlQuery, DatabaseInfo databaseInfo)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (string.IsNullOrEmpty(sqlQuery))
        {
            return mapping;
        }

        // Extract table aliases from FROM and JOIN clauses
        // Pattern: FROM table alias or JOIN table alias
        var aliasPattern = @"(?:FROM|JOIN)\s+(\w+)\s+(\w+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(sqlQuery, aliasPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var tableName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            
            if (databaseInfo.Tables.ContainsKey(tableName))
            {
                mapping[alias] = tableName;
                _logger.Information("Mapped alias '{Alias}' to table '{Table}'", alias, tableName);
            }
        }

        return mapping;
    }

    /// <summary>
    /// Calculate required records per table for complex JOIN queries
    /// </summary>
    private int CalculateRequiredRecordsPerTable(int desiredFinalCount, int tableCount)
    {
        // 🚀 ENHANCED: Ensure sufficient data generation to satisfy query results
        // For complex joins or queries with LIMIT constraints, we need to generate 
        // significantly more records to guarantee sufficient matches
        
        // Base multiplier for simple scenarios
        var baseMultiplier = Math.Max(1, tableCount / 2);
        
        // Enhanced multiplier for complex scenarios that might filter results heavily
        var enhancedMultiplier = Math.Max(3, desiredFinalCount);
        
        // Use enhanced multiplier to ensure we always have enough data
        var recordsPerTable = Math.Max(desiredFinalCount, enhancedMultiplier);
        
        _logger.Information("Calculated records per table: {RecordsPerTable} (desired: {DesiredCount}, tables: {TableCount})", 
            recordsPerTable, desiredFinalCount, tableCount);
            
        return recordsPerTable;
    }

    /// <summary>
    /// Detect if table is a junction/bridge table - GENERIC
    /// </summary>
    private bool IsJunctionTable(TableSchema table)
    {
        var fkCount = table.Columns.Count(c => IsLikelyForeignKey(c));
        var totalColumns = table.Columns.Count;
        
        // Junction table characteristics:
        // 1. Has 2 or more foreign keys
        // 2. Foreign keys make up significant portion of columns (>= 50%)
        // 3. Usually small number of total columns
        return fkCount >= 2 && (fkCount * 1.0 / totalColumns) >= 0.5 && totalColumns <= 6;
    }

    /// <summary>
    /// Detect if column is likely a foreign key - GENERIC
    /// </summary>
    private bool IsLikelyForeignKey(ColumnSchema column)
    {
        var name = column.ColumnName.ToLower();
        return name.EndsWith("_id") || name.EndsWith("_by") || name.Contains("ref");
    }

    /// <summary>
    /// Generate unique foreign key combination for junction tables - GENERIC
    /// </summary>
    private List<string> GenerateUniqueForeignKeyCombination(
        Faker faker,
        List<ColumnSchema> foreignKeyColumns,
        int recordIndex,
        Dictionary<string, List<int>> generatedPrimaryKeys,
        HashSet<string> usedCombinations,
        int totalRecords)
    {
        _logger.Information($"[GENERIC] Generating unique FK combination for record {recordIndex}/{totalRecords}");
        
        var fkValues = new List<string>();
        
        for (int i = 0; i < foreignKeyColumns.Count; i++)
        {
            // Use recordIndex + offset to create unique combinations
            var offset = i * totalRecords / Math.Max(1, foreignKeyColumns.Count);
            var fkValue = ((recordIndex + offset) % totalRecords) + 1;
            fkValues.Add(fkValue.ToString());
        }
        
        // Generate combination key for uniqueness checking
        var combination = string.Join("-", fkValues);
        
        // Adjust if combination already used
        int attempts = 0;
        while (usedCombinations.Contains(combination) && attempts < 10)
        {
            attempts++;
            _logger.Warning($"[GENERIC] Collision detected for {combination}, adjusting...");
            
            // Adjust last FK value
            if (fkValues.Count > 0)
            {
                var lastIndex = fkValues.Count - 1;
                var newValue = (int.Parse(fkValues[lastIndex]) + attempts - 1) % totalRecords + 1;
                fkValues[lastIndex] = newValue.ToString();
                combination = string.Join("-", fkValues);
            }
        }
        
        usedCombinations.Add(combination);
        _logger.Information($"[GENERIC] Generated unique FK combination: {combination} (attempts: {attempts})");
        
        return fkValues;
    }

    // 🎯 REMOVED: QuoteIdentifier method - now handled by CommonInsertBuilder

    private object GenerateBogusValue(Faker faker, ColumnSchema column, int recordIndex, DatabaseInfo databaseInfo, int recordCount, string sqlQuery, string tableName, SqlDataRequirements sqlRequirements, ComprehensiveConstraints? comprehensiveConstraints = null)
    {
        // 🎯 NEW: Check comprehensive constraints first
        if (comprehensiveConstraints != null)
        {
            var constraintAwareValue = GenerateConstraintAwareValue(faker, column, recordIndex, tableName, comprehensiveConstraints, sqlQuery, databaseInfo);
            if (constraintAwareValue != null)
            {
                Console.WriteLine($"[CONSTRAINT-AWARE] {tableName}.{column.ColumnName}: Generated '{constraintAwareValue}' for constraint satisfaction");
                return constraintAwareValue;
            }
        }

        // Handle foreign keys first
        if (column.ColumnName.EndsWith("_id", StringComparison.OrdinalIgnoreCase) || 
            column.ColumnName.EndsWith("_by", StringComparison.OrdinalIgnoreCase))
        {
            var fk = databaseInfo.Tables.Values
                .SelectMany(t => t.ForeignKeys)
                .FirstOrDefault(fk => fk.ColumnName.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (fk != null)
            {
                // 🔧 CRITICAL FIX: Generate FK value that references valid primary key range
                // Primary keys are sequential: 1,2,3,4,5... so FK should be in same range
                var fkValue = (recordIndex % recordCount) + 1;
                
                // Safety clamp: ensure FK value is within valid range [1, recordCount]
                fkValue = Math.Max(1, Math.Min(fkValue, recordCount));
                
                _logger.Information($"[FIXED] FK {column.ColumnName} -> {fk.ReferencedTable}.{fk.ReferencedColumn} = {fkValue} (recordIndex: {recordIndex}, recordCount: {recordCount})");
                
                return fkValue.ToString();
            }
        }

        // Check for boolean column first (before tinyint case)
        if (IsBooleanColumn(column))
        {
            Console.WriteLine($"[DEBUG] Boolean column detected: {column.ColumnName} ({column.DataType}) -> generating boolean");
            return GenerateBooleanValue(faker, recordIndex, column, tableName, sqlQuery);
        }

        // Generate based on data type and column name
        var dataType = column.DataType.ToLower();
        
        // 🎯 FIX: Check for ENUM at start of DataType (MySQL format: "enum('Male','Female','Other')")
        if (dataType.StartsWith("enum("))
        {
            return GenerateEnumValue(faker, column);
        }
        
        return dataType switch
        {
            "int" or "integer" or "bigint" or "smallint" or "tinyint" => GenerateIntValue(faker, column, recordIndex, sqlRequirements, tableName),
            "decimal" or "numeric" or "float" or "double" or "money" => GenerateDecimalValue(faker, column),
            "varchar" or "nvarchar" or "text" or "char" or "nchar" or "string" => GenerateStringValue(faker, column, recordIndex, sqlQuery, tableName, sqlRequirements),
            "datetime" or "datetime2" or "date" or "timestamp" => GenerateDateValue(faker, column, sqlQuery, tableName, sqlRequirements), // 🔧 FIX: Return DateTime object for proper Oracle formatting
            "bit" or "boolean" or "bool" => GenerateBooleanValue(faker, recordIndex, column, tableName, sqlQuery),
            "uniqueidentifier" or "uuid" => $"'{Guid.NewGuid()}'",
            "json" or "jsonb" => GenerateJsonValue(faker, column, recordIndex),
            "enum" => GenerateEnumValue(faker, column), // Legacy fallback
            _ => GenerateDefaultValue(faker, column, recordIndex)
        };
    }

    private string GenerateIntValue(Faker faker, ColumnSchema column, int recordIndex, SqlDataRequirements sqlRequirements, string tableName)
    {
        var columnName = column.ColumnName.ToLower();
        
        // Primary key generation
        if (column.IsPrimaryKey)
        {
            return recordIndex.ToString();  // No quotes for NUMBER
        }
        
        // Boolean-like NUMBER(1) columns
        if (column.NumericPrecision == 1 && column.NumericScale == 0)
        {
            return faker.Random.Bool() ? "1" : "0";  // No quotes for NUMBER
        }
        
        // Age columns
        if (columnName.Contains("age"))
        {
            return faker.Random.Int(18, 65).ToString();  // No quotes for NUMBER
        }
        
        // ID-related columns
        if (columnName.Contains("id") && !column.IsPrimaryKey)
        {
            return faker.Random.Int(1, 1000).ToString();  // No quotes for NUMBER
        }
        
        // Default random integer
        return faker.Random.Int(1, 10000).ToString();  // No quotes for NUMBER
    }

    /// <summary>
    /// Generate integer value based on WHERE condition requirement
    /// </summary>
    private string GenerateIntForRequirement(Faker faker, WhereCondition requirement)
    {
        return requirement.Operator switch
        {
            "=" => requirement.Value,
            ">" => GenerateGreaterThanValue(faker, requirement.Value),
            ">=" => GenerateGreaterThanValue(faker, requirement.Value),
            "<" => GenerateLessThanValue(faker, requirement.Value),
            "<=" => GenerateLessThanValue(faker, requirement.Value),
            "IN" => GenerateInValue(faker, requirement.Value, 0),
            _ => faker.Random.Int(1, 100).ToString()
        };
    }

    private string GenerateDecimalValue(Faker faker, ColumnSchema column)
    {
        if (column.ColumnName.ToLower().Contains("salary"))
            return faker.Random.Decimal(30000, 200000).ToString("F2");
            
        if (column.ColumnName.ToLower().Contains("price") || column.ColumnName.ToLower().Contains("amount"))
            return faker.Random.Decimal(10, 10000).ToString("F2");

        return faker.Random.Decimal(1, 1000).ToString("F2");
    }

    /// <summary>
    /// Generate string value based on column metadata and SQL requirements
    /// GENERIC VERSION - NO HARDCODE COLUMN NAMES
    /// </summary>
    private string GenerateStringValue(Faker faker, ColumnSchema column, int recordIndex, string sqlQuery, string tableName, SqlDataRequirements sqlRequirements)
    {
        var columnName = column.ColumnName.ToLower();
        var maxLength = Math.Max(1, column.MaxLength ?? 255);
        string value;

        // 🎯 CHECK SQL REQUIREMENTS FIRST (from WHERE conditions)
        var columnRequirement = GetColumnRequirement(tableName, columnName, sqlRequirements);
        
        if (columnRequirement != null)
        {
            // Generate data based on WHERE condition requirements
            value = GenerateValueForRequirement(faker, columnRequirement, recordIndex, maxLength);
        }
        else
        {
            // 🎯 GENERIC PATTERN-BASED GENERATION (NO HARDCODE)
            // Use column patterns rather than specific names
            value = GenerateValueByPattern(faker, columnName, recordIndex, maxLength);
        }
        
        // Fix: Truncate value to respect MaxLength
        if (value.Length > maxLength)
        {
            value = value.Substring(0, maxLength);
        }
        
        return $"'{EscapeSqlString(value)}'";
    }

    /// <summary>
    /// Generate value based on column name patterns (GENERIC - NO HARDCODE)
    /// </summary>
    private string GenerateValueByPattern(Faker faker, string columnName, int recordIndex, int maxLength)
    {
        // 🎯 PATTERN-BASED GENERATION - works with ANY column names
        
        // Email pattern detection
        if (columnName.Contains("email") || columnName.Contains("mail"))
            return faker.Internet.Email();
            
        // Name pattern detection
        if (columnName.Contains("name") && !columnName.Contains("username") && !columnName.Contains("filename"))
        {
            if (columnName.Contains("first"))
                return faker.Name.FirstName();
            else if (columnName.Contains("last"))
                return faker.Name.LastName();
            else if (columnName.Contains("full"))
                return faker.Name.FullName();
            else if (columnName.Contains("company") || columnName.Contains("business"))
                return faker.Company.CompanyName();
            else
                return faker.Name.FullName(); // Default for name fields
        }
        
        // Username/login pattern
        if (columnName.Contains("username") || columnName.Contains("login") || columnName.Contains("user"))
            return faker.Internet.UserName();
            
        // Phone pattern
        if (columnName.Contains("phone") || columnName.Contains("tel") || columnName.Contains("mobile"))
            return faker.Phone.PhoneNumber("###-###-####");
            
        // Address pattern
        if (columnName.Contains("address") || columnName.Contains("street") || columnName.Contains("location"))
            return faker.Address.StreetAddress();
            
        // City pattern
        if (columnName.Contains("city") || columnName.Contains("town"))
            return faker.Address.City();
            
        // Code pattern - GENERIC unique generation
        if (columnName.Contains("code") || columnName.Contains("identifier") || columnName.Contains("reference"))
        {
            return GenerateUniqueCode(faker, recordIndex, maxLength);
        }
        
        // Description pattern
        if (columnName.Contains("description") || columnName.Contains("note") || columnName.Contains("comment"))
            return faker.Lorem.Sentence(3);
            
        // URL pattern
        if (columnName.Contains("url") || columnName.Contains("website") || columnName.Contains("link"))
            return faker.Internet.Url();
            
        // Department/category pattern
        if (columnName.Contains("department") || columnName.Contains("category") || columnName.Contains("type"))
            return faker.Commerce.Department();
            
        // Industry pattern
        if (columnName.Contains("industry") || columnName.Contains("sector"))
            return faker.Company.CatchPhrase();
            
        // Status pattern
        if (columnName.Contains("status") || columnName.Contains("state"))
            return faker.PickRandom("Active", "Inactive", "Pending", "Completed");
            
        // 🎯 REMOVED: Hardcode gender pattern - violates genericity principle
            
        // Default: Generic text with recordIndex for uniqueness
        var baseWord = faker.Lorem.Word();
        return $"{baseWord}_{recordIndex}";
    }

    /// <summary>
    /// Generate unique code - GENERIC VERSION
    /// </summary>
    private string GenerateUniqueCode(Faker faker, int recordIndex, int maxLength)
    {
        // Use timestamp + recordIndex for guaranteed uniqueness
        var timestamp = DateTime.Now.Ticks % 100000;
        var baseCode = $"CODE_{timestamp}_{recordIndex:D3}";
        
        // If too long, use shorter GUID approach
        if (baseCode.Length > maxLength)
        {
            var shortGuid = Guid.NewGuid().ToString("N")[..Math.Min(8, maxLength - 4)].ToUpper();
            baseCode = $"C{recordIndex:D3}_{shortGuid}";
            
            // Final fallback
            if (baseCode.Length > maxLength)
            {
                baseCode = $"C{recordIndex}_{shortGuid[..Math.Min(4, maxLength - recordIndex.ToString().Length - 2)]}";
            }
        }
        
        return baseCode;
    }

    /// <summary>
    /// Get table requirement from SQL analysis
    /// </summary>
    private TableDataRequirement? GetTableRequirement(string tableName, SqlDataRequirements sqlRequirements)
    {
        return sqlRequirements.TableRequirements.TryGetValue(tableName, out var requirement) ? requirement : null;
    }

    /// <summary>
    /// Get column requirement from WHERE conditions
    /// </summary>
    private WhereCondition? GetColumnRequirement(string tableName, string columnName, SqlDataRequirements sqlRequirements)
    {
        // Find WHERE condition for this column
        return sqlRequirements.WhereConditions.FirstOrDefault(wc => 
            (string.IsNullOrEmpty(wc.TableAlias) || GetTableNameFromAlias(wc.TableAlias, tableName)) &&
            wc.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Generate value based on WHERE condition requirement
    /// </summary>
    private string GenerateValueForRequirement(Faker faker, WhereCondition requirement, int recordIndex, int maxLength)
    {
        var value = requirement.Operator switch
        {
            "LIKE" => GenerateLikeValue(faker, requirement.Value, recordIndex),
            "=" => requirement.Value,
            "IN" => GenerateInValue(faker, requirement.Value, recordIndex),
            ">" or ">=" => GenerateGreaterThanValue(faker, requirement.Value),
            "<" or "<=" => GenerateLessThanValue(faker, requirement.Value),
            _ => requirement.Value
        };

        // Ensure value fits within max length
        if (value.Length > maxLength)
        {
            value = value.Substring(0, maxLength);
        }

        return value;
    }

    /// <summary>
    /// Generate value for LIKE condition - GENERIC VERSION
    /// </summary>
    private string GenerateLikeValue(Faker faker, string likeValue, int recordIndex)
    {
        if (string.IsNullOrEmpty(likeValue))
        {
            return faker.Lorem.Word();
        }

        // 🎯 GENERIC PATTERN-BASED LIKE VALUE GENERATION
        // Remove wildcards to get core pattern
        var corePattern = likeValue.Replace("%", "").Replace("_", "").Trim();
        
        if (string.IsNullOrEmpty(corePattern))
        {
            return faker.Lorem.Word();
        }
        
        // Generate variations based on pattern characteristics
        if (corePattern.Length <= 3 && corePattern.All(char.IsUpper))
        {
            // Short uppercase pattern (like "DD", "IT", "HR") - assume it's a code
            return GenerateCodeVariation(faker, corePattern, recordIndex);
        }
        else if (corePattern.Any(char.IsDigit))
        {
            // Contains numbers - might be version/reference
            return GenerateNumericVariation(faker, corePattern, recordIndex);
        }
        else if (corePattern.Length > 10)
        {
            // Long pattern - might be company/description
            return GenerateDescriptiveVariation(faker, corePattern, recordIndex);
        }
        else
        {
            // Default: include the required pattern in generated string with unique suffix
            var uniqueSuffix = $"{recordIndex:D3}_{DateTime.Now.Ticks % 1000:D3}";
            return $"{corePattern}_{uniqueSuffix}";
        }
    }

    /// <summary>
    /// Generate code variation - GENERIC
    /// </summary>
    private string GenerateCodeVariation(Faker faker, string corePattern, int recordIndex)
    {
        var timestamp = DateTime.Now.Ticks % 10000;
        var baseCode = $"{corePattern}_{timestamp}_{recordIndex:D3}";
        
        // If too long, use shorter approach
        if (baseCode.Length > 50)
        {
            var shortGuid = Guid.NewGuid().ToString("N")[..4].ToUpper();
            baseCode = $"{corePattern}_{shortGuid}_{recordIndex:D2}";
        }
        
        return baseCode;
    }

    /// <summary>
    /// Generate numeric variation - GENERIC
    /// </summary>
    private string GenerateNumericVariation(Faker faker, string corePattern, int recordIndex)
    {
        var randomNum = faker.Random.Number(100, 999);
        return $"{corePattern}_{randomNum}_{recordIndex}";
    }

    /// <summary>
    /// Generate descriptive variation - GENERIC
    /// </summary>
    private string GenerateDescriptiveVariation(Faker faker, string corePattern, int recordIndex)
    {
        var suffix = faker.Lorem.Word();
        return $"{corePattern} {suffix} {recordIndex}";
    }

    /// <summary>
    /// Generate value for IN condition
    /// </summary>
    private string GenerateInValue(Faker faker, string inValues, int recordIndex)
    {
        var values = inValues.Split(',').Select(v => v.Trim()).ToArray();
        return faker.PickRandom(values);
    }

    /// <summary>
    /// Generate value greater than specified value
    /// </summary>
    private string GenerateGreaterThanValue(Faker faker, string minValue)
    {
        if (int.TryParse(minValue, out int intMin))
        {
            return faker.Random.Int(intMin + 1, intMin + 100).ToString();
        }
        
        if (decimal.TryParse(minValue, out decimal decMin))
        {
            return faker.Random.Decimal(decMin + 1, decMin + 1000).ToString("F2");
        }

        return minValue;
    }

    /// <summary>
    /// Generate value less than specified value
    /// </summary>
    private string GenerateLessThanValue(Faker faker, string maxValue)
    {
        if (int.TryParse(maxValue, out int intMax))
        {
            return faker.Random.Int(1, Math.Max(1, intMax - 1)).ToString();
        }
        
        if (decimal.TryParse(maxValue, out decimal decMax))
        {
            return faker.Random.Decimal(1, Math.Max(1, decMax - 1)).ToString("F2");
        }

        return maxValue;
    }

    /// <summary>
    /// Get table name from alias - GENERIC VERSION
    /// </summary>
    private bool GetTableNameFromAlias(string alias, string tableName)
    {
        // 🎯 GENERIC ALIAS MATCHING - no hardcode table names
        var aliasLower = alias.ToLower();
        var tableNameLower = tableName.ToLower();
        
        // Direct match
        if (aliasLower == tableNameLower)
            return true;
            
        // Abbreviated match (first letter(s))
        if (aliasLower.Length <= 3 && tableNameLower.StartsWith(aliasLower))
            return true;
            
        // Common abbreviation patterns
        if (aliasLower.Length == 1)
        {
            // Single letter abbreviation
            return tableNameLower.StartsWith(aliasLower);
        }
        
        if (aliasLower.Length == 2)
        {
            // Two letter abbreviation - check if it's combination of first letters of underscore-separated words
            var words = tableNameLower.Split('_');
            if (words.Length >= 2)
            {
                var firstLetters = string.Join("", words.Select(w => w.FirstOrDefault()));
                return firstLetters.StartsWith(aliasLower);
            }
        }
        
        // Partial name match
        return tableNameLower.Contains(aliasLower) || aliasLower.Contains(tableNameLower);
    }

    // Fix: Add SQL string escaping method
    private static string EscapeSqlString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        // Escape single quotes by doubling them
        return input.Replace("'", "''");
    }

    private DateTime GenerateDateValue(Faker faker, ColumnSchema column, string sqlQuery, string tableName, SqlDataRequirements sqlRequirements)
    {
        var columnName = column.ColumnName.ToLower();
        var dataType = column.DataType?.ToUpper() ?? "";
        
        DateTime dateValue;
        
        // Fallback: Generate near-future dates for columns with "expires" in name
        if (columnName.Contains("expires"))
        {
            dateValue = faker.Date.Between(DateTime.Now.AddDays(1), DateTime.Now.AddDays(50));
            Console.WriteLine($"[DATE-FALLBACK] {tableName}.{columnName}: Generated {dateValue:yyyy-MM-dd HH:mm:ss} (expires)");
        }
        // Birth date columns
        else if (columnName.Contains("birth") || columnName.Contains("dob"))
        {
            dateValue = faker.Date.Between(DateTime.Now.AddYears(-65), DateTime.Now.AddYears(-18));
            Console.WriteLine($"[DATE-FALLBACK] {tableName}.{columnName}: Generated {dateValue:yyyy-MM-dd} (birth)");
        }
        // Created/Added columns - past dates
        else if (columnName.Contains("created") || columnName.Contains("added") || columnName.Contains("hired") || columnName.Contains("start"))
        {
            dateValue = faker.Date.Between(DateTime.Now.AddDays(-365), DateTime.Now);
            Console.WriteLine($"[DATE-FALLBACK] {tableName}.{columnName}: Generated {dateValue:yyyy-MM-dd HH:mm:ss} (created)");
        }
        // Updated/Modified columns - recent dates
        else if (columnName.Contains("updated") || columnName.Contains("modified") || columnName.Contains("last"))
        {
            dateValue = faker.Date.Between(DateTime.Now.AddDays(-30), DateTime.Now);
            Console.WriteLine($"[DATE-FALLBACK] {tableName}.{columnName}: Generated {dateValue:yyyy-MM-dd HH:mm:ss} (updated)");
        }
        // Default: random recent date
        else
        {
            dateValue = faker.Date.Between(DateTime.Now.AddDays(-365), DateTime.Now.AddDays(30));
            Console.WriteLine($"[DATE-FALLBACK] {tableName}.{columnName}: Generated {dateValue:yyyy-MM-dd HH:mm:ss} (default)");
        }
        
        // Return DateTime object for CommonInsertBuilder.FormatValue to handle database-specific formatting
        return dateValue;
    }

    /// <summary>
    /// Generate date value based on WHERE condition requirement
    /// </summary>
    private string GenerateDateForRequirement(Faker faker, WhereCondition requirement, string columnName)
    {
        return requirement.Operator switch
        {
            "YEAR_EQUALS" => GenerateYearEqualsDate(faker, requirement.Value, columnName),
            "DATE_WITHIN_DAYS" => GenerateDateWithinDays(faker, requirement.Value),
            ">" or ">=" => GenerateDateAfter(faker, requirement.Value),
            "<" or "<=" => GenerateDateBefore(faker, requirement.Value),
            _ => faker.Date.Recent(365).ToString("yyyy-MM-dd HH:mm:ss") // Return raw datetime string for FormatValue to handle
        };
    }

    /// <summary>
    /// Generate date for YEAR(column) = year condition
    /// FIXED: Return raw datetime string instead of quoted string for Oracle dialect handler
    /// </summary>
    private string GenerateYearEqualsDate(Faker faker, string year, string columnName)
    {
        if (int.TryParse(year, out int targetYear))
        {
            // Generate random date within the target year
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear, 12, 31);
            var randomDate = faker.Date.Between(startDate, endDate);
            
            // Return raw datetime string - let CommonInsertBuilder.FormatValue handle Oracle/MySQL formatting
            return columnName.Contains("birth") ? 
                randomDate.ToString("yyyy-MM-dd") : 
                randomDate.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        return faker.Date.Recent(365).ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Generate date within specified days from now
    /// FIXED: Return raw datetime string for Oracle dialect handler
    /// </summary>
    private string GenerateDateWithinDays(Faker faker, string days)
    {
        if (int.TryParse(days, out int dayCount))
        {
            var futureDate = faker.Date.Between(DateTime.Now, DateTime.Now.AddDays(dayCount));
            return futureDate.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        return faker.Date.Future().ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Generate date after specified date
    /// FIXED: Return raw datetime string for Oracle dialect handler
    /// </summary>
    private string GenerateDateAfter(Faker faker, string dateValue)
    {
        if (DateTime.TryParse(dateValue, out DateTime minDate))
        {
            var futureDate = faker.Date.Between(minDate.AddDays(1), minDate.AddYears(1));
            return futureDate.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        return faker.Date.Future().ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Generate date before specified date
    /// FIXED: Return raw datetime string for Oracle dialect handler
    /// </summary>
    private string GenerateDateBefore(Faker faker, string dateValue)
    {
        if (DateTime.TryParse(dateValue, out DateTime maxDate))
        {
            var pastDate = faker.Date.Between(maxDate.AddYears(-1), maxDate.AddDays(-1));
            return pastDate.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        return faker.Date.Past().ToString("yyyy-MM-dd HH:mm:ss");
    }

    private string GenerateBooleanValue(Faker faker, int recordIndex, ColumnSchema column, string tableName, string sqlQuery)
    {
        // For Oracle, boolean is typically NUMBER(1) with 0/1 values
        var columnName = column.ColumnName.ToLower();
        
        // Default probability
        var trueProbability = 0.8f; // 80% true by default
        
        // Check if there are WHERE conditions affecting this boolean column
        var wherePattern = $@"\b{Regex.Escape(tableName)}\.\s*{Regex.Escape(columnName)}\s*=\s*(0|1|true|false)";
        var whereMatch = Regex.Match(sqlQuery, wherePattern, RegexOptions.IgnoreCase);
        
        if (whereMatch.Success)
        {
            var requiredValue = whereMatch.Groups[1].Value.ToLower();
            return requiredValue switch
            {
                "1" or "true" => "1",  // No quotes for NUMBER
                "0" or "false" => "0",  // No quotes for NUMBER
                _ => faker.Random.Float() < trueProbability ? "1" : "0"  // No quotes for NUMBER
            };
        }
        
        // Generate based on column semantics
        if (columnName.Contains("active") || columnName.Contains("enabled"))
        {
            trueProbability = 0.9f; // 90% active/enabled
        }
        else if (columnName.Contains("deleted") || columnName.Contains("disabled"))
        {
            trueProbability = 0.1f; // 10% deleted/disabled
        }
        
        return faker.Random.Float() < trueProbability ? "1" : "0";  // No quotes for NUMBER
    }
    
    /// <summary>
    /// Generate boolean value based on WHERE condition requirement (GENERIC for any table/column)
    /// </summary>
    private string GenerateBooleanForRequirement(Faker faker, WhereCondition requirement, ColumnSchema column, string tableName)
    {
        return requirement.Operator switch
        {
            "=" => GenerateBooleanEquals(faker, requirement.Value, column, tableName),
            "!=" or "<>" => GenerateBooleanNotEquals(faker, requirement.Value, column, tableName),
            _ => GenerateBooleanDefault(faker, column, tableName)
        };
    }
    
    /// <summary>
    /// Generate boolean for EQUALITY condition (column = value) - GENERIC
    /// </summary>
    private string GenerateBooleanEquals(Faker faker, string targetValue, ColumnSchema column, string tableName)
    {
        // Parse target value (TRUE, FALSE, 1, 0)
        if (bool.TryParse(targetValue, out bool boolValue))
        {
            // Generate 95% matching values to satisfy WHERE clause
            var shouldMatch = faker.Random.Double() < 0.95;
            var result = shouldMatch ? boolValue : !boolValue;
            Console.WriteLine($"[QUERY-AWARE] {tableName}.{column.ColumnName}: Generated {(result ? "1" : "0")} for = {targetValue} condition");
            return result ? "1" : "0";
        }
        
        if (int.TryParse(targetValue, out int intValue))
        {
            // Handle numeric boolean (1 = true, 0 = false)
            var shouldMatch = faker.Random.Double() < 0.95;
            var result = shouldMatch ? (intValue != 0) : (intValue == 0);
            Console.WriteLine($"[QUERY-AWARE] {tableName}.{column.ColumnName}: Generated {(result ? "1" : "0")} for = {targetValue} condition");
            return result ? "1" : "0";
        }
        
        // Default fallback
        return GenerateBooleanDefault(faker, column, tableName);
    }
    
    /// <summary>
    /// Generate boolean for NOT EQUALS condition (column != value) - GENERIC
    /// </summary>
    private string GenerateBooleanNotEquals(Faker faker, string targetValue, ColumnSchema column, string tableName)
    {
        // Parse target value and generate opposite
        if (bool.TryParse(targetValue, out bool boolValue))
        {
            // Generate 95% NOT matching values to satisfy WHERE clause
            var shouldNotMatch = faker.Random.Double() < 0.95;
            var result = shouldNotMatch ? !boolValue : boolValue;
            Console.WriteLine($"[QUERY-AWARE] {tableName}.{column.ColumnName}: Generated {(result ? "1" : "0")} for != {targetValue} condition");
            return result ? "1" : "0";
        }
        
        if (int.TryParse(targetValue, out int intValue))
        {
            // Handle numeric boolean (generate opposite)
            var shouldNotMatch = faker.Random.Double() < 0.95;
            var result = shouldNotMatch ? (intValue == 0) : (intValue != 0);
            Console.WriteLine($"[QUERY-AWARE] {tableName}.{column.ColumnName}: Generated {(result ? "1" : "0")} for != {targetValue} condition");
            return result ? "1" : "0";
        }
        
        // Default fallback
        return GenerateBooleanDefault(faker, column, tableName);
    }
    
    /// <summary>
    /// Default boolean generation (no specific requirements) - GENERIC
    /// </summary>
    private string GenerateBooleanDefault(Faker faker, ColumnSchema column, string tableName)
    {
        // Default: 80% true rate for general active/enabled columns
        var shouldBeTrue = faker.Random.Double() < 0.8;
        return shouldBeTrue ? "1" : "0";
    }

    private string GenerateEnumValue(Faker faker, ColumnSchema column)
    {
        // 🎯 FIXED: Use actual ENUM values from database schema
        if (column.EnumValues != null && column.EnumValues.Count > 0)
        {
            var enumValue = faker.PickRandom(column.EnumValues);
            return $"'{enumValue}'";
        }
        
        // Fallback: Try to extract ENUM values from DataType if available
        if (column.DataType.ToLower().Contains("enum"))
        {
            // Parse enum values from DataType like "enum('Male','Female','Other')"
            var enumMatch = System.Text.RegularExpressions.Regex.Match(
                column.DataType, 
                @"enum\s*\(\s*([^)]+)\s*\)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (enumMatch.Success)
            {
                var enumValuesStr = enumMatch.Groups[1].Value;
                var enumValues = enumValuesStr
                    .Split(',')
                    .Select(v => v.Trim().Trim('\'', '"'))
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();
                
                if (enumValues.Count > 0)
                {
                    var enumValue = faker.PickRandom(enumValues);
                    return $"'{enumValue}'";
                }
            }
        }
        
        // Final fallback: Use MaxLength constraint
        var maxLength = Math.Max(1, column.MaxLength ?? 10);
        var baseValue = $"V{faker.Random.Int(1, 99)}";
        
        // Ensure value fits within column constraint
        if (baseValue.Length > maxLength)
        {
            baseValue = faker.Random.AlphaNumeric(Math.Min(maxLength, 3));
        }
        
        return $"'{baseValue}'";
    }

    private string GenerateJsonValue(Faker faker, ColumnSchema column, int recordIndex)
    {
        var columnName = column.ColumnName.ToLower();
        
        // Generate realistic JSON based on common column names
        if (columnName.Contains("permissions") || columnName.Contains("permission"))
        {
            // Generate permissions array
            var permissions = new List<string>();
            var availablePermissions = new[] { "read", "write", "delete", "create", "update", "admin", "view", "edit" };
            var permissionCount = faker.Random.Int(1, 4); // 1-4 permissions
            
            for (int i = 0; i < permissionCount; i++)
            {
                var permission = faker.PickRandom(availablePermissions);
                if (!permissions.Contains(permission))
                {
                    permissions.Add(permission);
                }
            }
            
            var jsonArray = string.Join(",", permissions.Select(p => $"\"{p}\""));
            return $"'[{jsonArray}]'";
        }
        else if (columnName.Contains("config") || columnName.Contains("settings"))
        {
            // Generate configuration object
            var config = new Dictionary<string, object>
            {
                ["theme"] = faker.PickRandom("light", "dark", "auto"),
                ["notifications"] = faker.Random.Bool(),
                ["language"] = faker.PickRandom("en", "vi", "ja"),
                ["timezone"] = faker.PickRandom("UTC", "Asia/Ho_Chi_Minh", "America/New_York")
            };
            
            var jsonPairs = config.Select(kvp => 
                kvp.Value is bool boolVal ? $"\"{kvp.Key}\":{boolVal.ToString().ToLower()}" :
                $"\"{kvp.Key}\":\"{kvp.Value}\"");
            
            return $"'{{{string.Join(",", jsonPairs)}}}'";
        }
        else if (columnName.Contains("metadata") || columnName.Contains("data") || columnName.Contains("info"))
        {
            // Generate generic metadata object
            var metadata = new Dictionary<string, object>
            {
                ["id"] = recordIndex,
                ["name"] = faker.Lorem.Word(),
                ["created"] = DateTime.Now.ToString("yyyy-MM-dd"),
                ["active"] = faker.Random.Bool()
            };
            
            var jsonPairs = metadata.Select(kvp => 
                kvp.Value is bool boolVal ? $"\"{kvp.Key}\":{boolVal.ToString().ToLower()}" :
                kvp.Value is int intVal ? $"\"{kvp.Key}\":{intVal}" :
                $"\"{kvp.Key}\":\"{kvp.Value}\"");
            
            return $"'{{{string.Join(",", jsonPairs)}}}'";
        }
        else
        {
            // Default: empty JSON object
            return "'{}'";
        }
    }

    private string GenerateDefaultValue(Faker faker, ColumnSchema column, int recordIndex)
    {
        Console.WriteLine($"[DEBUG] GenerateDefaultValue called for: {column.ColumnName} ({column.DataType}) - this shouldn't happen often");
        
        var dataType = column.DataType?.ToUpper() ?? "";
        var columnName = column.ColumnName?.ToLower() ?? "";
        
        // Oracle-specific handling
        if (dataType.Contains("NUMBER"))
        {
            // Check if it's a boolean-like column
            if (columnName.Contains("is_") || columnName.Contains("active") || 
                columnName.Contains("enabled") || columnName.Contains("flag"))
            {
                return faker.Random.Bool() ? "1" : "0";  // No quotes for NUMBER
            }
            // Check if precision indicates boolean (NUMBER(1))
            if (column.NumericPrecision == 1 && column.NumericScale == 0)
            {
                return faker.Random.Bool() ? "1" : "0";  // No quotes for NUMBER
            }
            return recordIndex.ToString();  // No quotes for NUMBER
        }
        
        if (dataType.Contains("TIMESTAMP") || dataType.Contains("DATE"))
        {
            var dateValue = "";
            if (columnName.Contains("birth"))
            {
                dateValue = faker.Date.Between(DateTime.Now.AddYears(-65), DateTime.Now.AddYears(-18)).ToString("yyyy-MM-dd");
            }
            else if (columnName.Contains("created") || columnName.Contains("added"))
            {
                dateValue = faker.Date.Recent(365).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (columnName.Contains("updated") || columnName.Contains("modified"))
            {
                dateValue = faker.Date.Recent(30).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (columnName.Contains("expires"))
            {
                dateValue = faker.Date.Future(30).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                dateValue = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            
            // Return Oracle TO_TIMESTAMP format for TIMESTAMP columns
            if (dataType.Contains("TIMESTAMP"))
            {
                return $"TO_TIMESTAMP('{dateValue}', 'YYYY-MM-DD HH24:MI:SS')";
            }
            else
            {
                return $"TO_DATE('{dateValue}', 'YYYY-MM-DD')";
            }
        }
        
        if (dataType.Contains("VARCHAR2") || dataType.Contains("CHAR") || dataType.Contains("CLOB"))
        {
            var maxLength = Math.Max(1, column.MaxLength ?? 50);
            var baseValue = $"Value_{recordIndex}";
            
            if (baseValue.Length > maxLength)
            {
                var shortValue = $"V{recordIndex}";
                if (shortValue.Length > maxLength)
                {
                    shortValue = faker.Random.AlphaNumeric(Math.Min(maxLength, 5));
                }
                return $"'{shortValue}'";  // Quotes for VARCHAR2/CHAR
            }
            
            return $"'{baseValue}'";  // Quotes for VARCHAR2/CHAR
        }
        
        // Legacy handling for other databases
        if (dataType.ToLower().Contains("int"))
            return recordIndex.ToString();  // No quotes for INT
        
        // Final fallback - ensure we never return empty string
        var fallbackMaxLength = Math.Max(1, column.MaxLength ?? 50);
        var fallbackValue = $"Default_{recordIndex}";
        
        if (fallbackValue.Length > fallbackMaxLength)
        {
            fallbackValue = $"D{recordIndex}";
            if (fallbackValue.Length > fallbackMaxLength)
            {
                fallbackValue = faker.Random.AlphaNumeric(Math.Min(fallbackMaxLength, 5));
            }
        }
        
        return $"'{fallbackValue}'";  // Quotes for string fallback
    }

    /// <summary>
    /// Check if column is a boolean field based on column name and data type
    /// </summary>
    private static bool IsBooleanColumn(ColumnSchema column)
    {
        var columnName = column.ColumnName.ToLower();
        var dataType = column.DataType.ToLower();
        
        // Check for boolean data types first
        if (dataType == "bit" || dataType == "boolean" || dataType == "bool")
        {
            return true;
        }
        
        // Check for MySQL TINYINT used as boolean
        if (dataType.Contains("tinyint"))
        {
            // TINYINT(1) is commonly used for boolean in MySQL
            if (column.MaxLength == 1)
            {
                return true;
            }
            
            // Also check for boolean-like column names with TINYINT
            if (columnName.StartsWith("is_") || columnName.StartsWith("has_") || 
                columnName.StartsWith("can_") || columnName.StartsWith("should_") ||
                columnName.EndsWith("_active") || columnName.EndsWith("_enabled") ||
                columnName == "active" || columnName == "enabled" || columnName == "deleted" ||
                columnName == "verified" || columnName == "approved" || columnName == "published")
            {
                return true;
            }
        }
        
        return false;
    }

    // 🗑️ REMOVED: IsGeneratedColumn() method - now using ColumnSchema.IsGenerated field directly

    /// <summary>
    /// Analyze SQL query to understand filtering requirements and adjust data generation
    /// REFACTORED: Use pattern-based detection instead of hardcode values
    /// </summary>
    private bool ShouldGenerateDataForQueryRequirements(string sqlQuery, string tableName, int recordIndex, int baseRecordCount)
    {
        if (string.IsNullOrEmpty(sqlQuery))
            return true;

        // REFACTORED: Extract patterns dynamically instead of hardcode
        var patterns = ExtractQueryPatterns(sqlQuery);
        
        foreach (var pattern in patterns)
        {
            // Check if this table is involved in the pattern
            if (IsTableInvolvedInPattern(pattern, tableName))
            {
                // Generate some records that satisfy the pattern
                return recordIndex <= Math.Min(3, baseRecordCount / 2);
            }
        }
        
        return true; // Default: generate all records
    }

    /// <summary>
    /// Extract value patterns from SQL query dynamically
    /// </summary>
    private List<QueryPattern> ExtractQueryPatterns(string sqlQuery)
    {
        var patterns = new List<QueryPattern>();
        
        // Extract LIKE patterns: column LIKE '%value%'
        var likeMatches = Regex.Matches(sqlQuery, @"(\w+)\.(\w+)\s+LIKE\s+'%([^%']+)%'", RegexOptions.IgnoreCase);
        foreach (Match match in likeMatches)
        {
            patterns.Add(new QueryPattern
            {
                Type = "LIKE",
                TableAlias = match.Groups[1].Value,
                ColumnName = match.Groups[2].Value,
                Value = match.Groups[3].Value
            });
        }
        
        // Extract equality patterns: column = value
        var equalMatches = Regex.Matches(sqlQuery, @"(\w+)\.(\w+)\s*=\s*[']?([^'\s,)]+)[']?", RegexOptions.IgnoreCase);
        foreach (Match match in equalMatches)
        {
            patterns.Add(new QueryPattern
            {
                Type = "EQUALS",
                TableAlias = match.Groups[1].Value,
                ColumnName = match.Groups[2].Value,
                Value = match.Groups[3].Value
            });
        }
        
        // Extract year patterns: YEAR(column) = year
        var yearMatches = Regex.Matches(sqlQuery, @"YEAR\s*\(\s*(\w+)\.(\w+)\s*\)\s*=\s*(\d{4})", RegexOptions.IgnoreCase);
        foreach (Match match in yearMatches)
        {
            patterns.Add(new QueryPattern
            {
                Type = "YEAR",
                TableAlias = match.Groups[1].Value,
                ColumnName = match.Groups[2].Value,
                Value = match.Groups[3].Value
            });
        }
        
        return patterns;
    }

    /// <summary>
    /// Check if table is involved in a query pattern
    /// </summary>
    private bool IsTableInvolvedInPattern(QueryPattern pattern, string tableName)
    {
        // Use dynamic alias resolution instead of hardcode
        var aliasResolver = new DynamicAliasResolver();
        
        // Simple heuristic: check if table name starts with alias or alias matches pattern
        var tableNameLower = tableName.ToLower();
        var aliasLower = pattern.TableAlias.ToLower();
        
        return tableNameLower.StartsWith(aliasLower) || 
               GenerateTableAcronym(tableName).Equals(aliasLower, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generate acronym from table name (user_roles → ur)
    /// </summary>
    private string GenerateTableAcronym(string tableName)
    {
        var parts = tableName.Split('_', '-');
        if (parts.Length > 1)
        {
            return string.Join("", parts.Select(p => p.FirstOrDefault())).ToLower();
        }
        
        return tableName.Length >= 2 ? tableName.Substring(0, 2).ToLower() : tableName.ToLower();
    }

    /// <summary>
    /// Query pattern data structure
    /// </summary>
    private class QueryPattern
    {
        public string Type { get; set; } = "";
        public string TableAlias { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string Value { get; set; } = "";
    }

    /// <summary>
    /// Generate deterministic code based on recordIndex to ensure uniqueness
    /// </summary>
    private string GenerateDeterministicCode(int recordIndex, int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[length];
        var seed = recordIndex;
        
        for (int i = 0; i < length; i++)
        {
            // Use recordIndex as seed to generate predictable but unique sequences
            result[i] = chars[(seed + i * 7) % chars.Length];
            seed = (seed * 31 + 17) % 997; // Use prime numbers for better distribution
        }
        
        return new string(result);
    }

    /// <summary>
    /// Generate value that satisfies comprehensive constraints (LIKE, JOIN, Boolean, Date)
    /// </summary>
    private string? GenerateConstraintAwareValue(Faker faker, ColumnSchema column, int recordIndex, string tableName, ComprehensiveConstraints constraints, string sqlQuery = "", DatabaseInfo? databaseInfo = null)
    {
        // REFACTORED: Use dynamic alias resolver instead of hardcode
        var aliasResolver = new DynamicAliasResolver();
        var tableAliasMapping = aliasResolver.ExtractAliasMapping(sqlQuery, databaseInfo);
        
        // Check LIKE patterns first
        foreach (var likePattern in constraints.LikePatterns)
        {
            if (MatchesTableAndColumn(likePattern.TableAlias, likePattern.ColumnName, tableName, column.ColumnName, tableAliasMapping))
            {
                Console.WriteLine($"[LIKE-MATCH] Found LIKE constraint for {tableName}.{column.ColumnName}: requires '{likePattern.RequiredValue}'");
                
                // Generate value that contains the required pattern
                var baseValue = GenerateValueWithPattern(faker, likePattern.RequiredValue, recordIndex, column.MaxLength ?? 255);
                return $"'{EscapeSqlString(baseValue)}'";
            }
        }
        
        // Check boolean constraints
        foreach (var boolConstraint in constraints.BooleanConstraints)
        {
            if (MatchesTableAndColumn(boolConstraint.TableAlias, boolConstraint.ColumnName, tableName, column.ColumnName, tableAliasMapping))
            {
                Console.WriteLine($"[BOOLEAN-MATCH] Found Boolean constraint for {tableName}.{column.ColumnName}: requires {boolConstraint.BooleanValue}");
                
                // Convert boolean to appropriate database value
                return boolConstraint.BooleanValue ? "1" : "0";
            }
        }
        
        // Check JOIN constraints 
        foreach (var joinConstraint in constraints.JoinConstraints)
        {
            if (MatchesTableAndColumn(joinConstraint.LeftTableAlias, joinConstraint.LeftColumn, tableName, column.ColumnName, tableAliasMapping))
            {
                if (!string.IsNullOrEmpty(joinConstraint.Value))
                {
                    Console.WriteLine($"[JOIN-MATCH] Found JOIN constraint for {tableName}.{column.ColumnName}: requires '{joinConstraint.Value}'");
                    
                    // For JOIN constraints with specific values (like ur.is_active = TRUE)
                    if (joinConstraint.Value.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                    {
                        return "1";
                    }
                    else if (joinConstraint.Value.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                    {
                        return "0";
                    }
                    else
                    {
                        return $"'{EscapeSqlString(joinConstraint.Value)}'";
                    }
                }
            }
        }
        
        // Check date constraints
        foreach (var dateConstraint in constraints.DateConstraints)
        {
            if (MatchesTableAndColumn(dateConstraint.TableAlias, dateConstraint.ColumnName, tableName, column.ColumnName, tableAliasMapping))
            {
                Console.WriteLine($"[DATE-MATCH] Found Date constraint for {tableName}.{column.ColumnName}: {dateConstraint.ConstraintType} = {dateConstraint.Value}");
                
                if (dateConstraint.ConstraintType == "YEAR_EQUALS" && int.TryParse(dateConstraint.Value, out var year))
                {
                    // Generate date within the specified year
                    var startDate = new DateTime(year, 1, 1);
                    var endDate = new DateTime(year, 12, 31);
                    var randomDate = faker.Date.Between(startDate, endDate);
                    // FIXED: Return raw datetime string for Oracle dialect handler to process
                    return randomDate.ToString("yyyy-MM-dd");
                }
            }
        }
        
        // Check WHERE constraints
        foreach (var whereConstraint in constraints.WhereConstraints)
        {
            if (MatchesTableAndColumn(whereConstraint.TableAlias, whereConstraint.ColumnName, tableName, column.ColumnName, tableAliasMapping))
            {
                Console.WriteLine($"[WHERE-MATCH] Found WHERE constraint for {tableName}.{column.ColumnName}: {whereConstraint.Operator} '{whereConstraint.Value}'");
                
                if (whereConstraint.Operator == "=" && !string.IsNullOrEmpty(whereConstraint.Value))
                {
                    return $"'{EscapeSqlString(whereConstraint.Value)}'";
                }
            }
        }
        
        return null; // No constraints found, use default generation
    }
    
    /// <summary>
    /// Check if constraint matches current table and column (handling aliases)
    /// </summary>
    private bool MatchesTableAndColumn(string constraintTableAlias, string constraintColumn, string actualTableName, string actualColumnName, Dictionary<string, string> aliasMapping)
    {
        // Resolve table alias to actual table name
        string constraintTableName = constraintTableAlias;
        if (aliasMapping.ContainsKey(constraintTableAlias))
        {
            constraintTableName = aliasMapping[constraintTableAlias];
        }
        
        return constraintTableName.Equals(actualTableName, StringComparison.OrdinalIgnoreCase) &&
               constraintColumn.Equals(actualColumnName, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Generate value that contains required pattern (for LIKE constraints)
    /// </summary>
    private string GenerateValueWithPattern(Faker faker, string requiredValue, int recordIndex, int maxLength)
    {
        // Ensure value contains the required pattern
        var baseValue = $"{requiredValue}_{recordIndex:000}_{faker.Random.AlphaNumeric(3)}";
        
        // Respect max length constraint
        if (baseValue.Length > maxLength)
        {
            baseValue = baseValue.Substring(0, maxLength);
        }
        
        return baseValue;
    }
} 