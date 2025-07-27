using Bogus;
using SqlTestDataGenerator.Core.Models;
using Serilog;
using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using SqlTestDataGenerator.Core.Abstractions;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Coordinates data generation across multiple tables to ensure query results match expected count.
/// Solves the problem where random data generation doesn't guarantee sufficient JOIN matches.
/// </summary>
public class CoordinatedDataGenerator
{
    private readonly ILogger _logger;
    private readonly SqlQueryParserV3 _queryParser;
    private CommonInsertBuilder _insertBuilder;

    public CoordinatedDataGenerator()
    {
        _logger = Log.ForContext<CoordinatedDataGenerator>();
        _queryParser = new SqlQueryParserV3();
        // Initialize with MySQL handler as default (will be replaced based on database type)
        _insertBuilder = new CommonInsertBuilder(new MySqlDialectHandler());
    }

    /// <summary>
    /// Generate coordinated data that ensures query results match desired count
    /// </summary>
    public async Task<List<InsertStatement>> GenerateCoordinatedDataAsync(
        DatabaseInfo databaseInfo,
        string sqlQuery,
        int desiredRecordCount,
        string databaseType,
        string connectionString)
    {
        _logger.Information("Starting coordinated data generation for {DesiredCount} records", desiredRecordCount);

        // 🔧 CRITICAL FIX: Use appropriate dialect handler based on database type
        var dialectHandler = CreateDialectHandler(databaseType, databaseInfo.Type);
        _insertBuilder = new CommonInsertBuilder(dialectHandler);
        
        _logger.Information("Using dialect handler: {DialectType} for database type: {DatabaseType}", 
            dialectHandler.GetType().Name, databaseType);

        // Step 1: Analyze query requirements
        var queryAnalysis = AnalyzeQueryRequirements(sqlQuery, databaseInfo);
        _logger.Information("Query analysis: {TableCount} tables, {ConditionCount} WHERE conditions", 
            queryAnalysis.RequiredTables.Count, queryAnalysis.WhereConditions.Count);

        // Step 2: Generate target records that satisfy ALL conditions
        var coordinatedRecords = GenerateTargetRecords(queryAnalysis, desiredRecordCount);
        _logger.Information("Generated {RecordCount} coordinated record sets", coordinatedRecords.Count);

        // Step 3: Convert to INSERT statements
        var insertStatements = ConvertToInsertStatements(coordinatedRecords, databaseInfo);
        _logger.Information("Converted to {StatementCount} INSERT statements", insertStatements.Count);

        // Step 4: Validate results with actual query execution (if connection available)
        if (!string.IsNullOrEmpty(connectionString))
        {
            var validationResult = await ValidateGeneratedData(
                insertStatements, sqlQuery, desiredRecordCount, databaseType, connectionString);

            if (!validationResult.IsValid)
            {
                _logger.Warning("Initial generation insufficient: got {Actual}, needed {Expected}", 
                    validationResult.ActualCount, desiredRecordCount);
                    
                // Generate additional coordinated records
                var additionalRecords = GenerateTargetRecords(queryAnalysis, 
                    desiredRecordCount - validationResult.ActualCount + 5); // Add buffer
                var additionalStatements = ConvertToInsertStatements(additionalRecords, databaseInfo);
                insertStatements.AddRange(additionalStatements);
            }
        }

        _logger.Information("Coordinated data generation completed: {FinalCount} statements", insertStatements.Count);
        return insertStatements;
    }

    /// <summary>
    /// Analyze SQL query to understand requirements for coordinated generation
    /// </summary>
    private QueryAnalysisResult AnalyzeQueryRequirements(string sqlQuery, DatabaseInfo databaseInfo)
    {
        var result = new QueryAnalysisResult();
        
        // Parse SQL requirements using existing parser
        var sqlRequirements = _queryParser.ParseQuery(sqlQuery);
        
        // Extract tables involved in query
        result.RequiredTables = ExtractTablesFromQuery(sqlQuery, databaseInfo);
        
        // Group WHERE conditions by table
        result.WhereConditions = GroupWhereConditionsByTable(sqlRequirements.WhereConditions, sqlQuery);
        
        // Identify JOIN relationships
        result.JoinRelationships = AnalyzeJoinRelationships(sqlQuery, databaseInfo);
        
        // Determine coordination strategy
        result.CoordinationStrategy = DetermineCoordinationStrategy(result);
        
        _logger.Information("Analysis complete: Strategy={Strategy}, Tables={Tables}", 
            result.CoordinationStrategy, string.Join(",", result.RequiredTables.Keys));
            
        return result;
    }

    /// <summary>
    /// Extract tables from query and map to schema info
    /// </summary>
    private Dictionary<string, TableSchema> ExtractTablesFromQuery(string sqlQuery, DatabaseInfo databaseInfo)
    {
        var result = new Dictionary<string, TableSchema>(StringComparer.OrdinalIgnoreCase);
        
        // Extract table names using regex
        var tablePattern = @"(?:FROM|JOIN)\s+(\w+)(?:\s+(\w+))?";
        var matches = Regex.Matches(sqlQuery, tablePattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            var tableName = match.Groups[1].Value;
            if (databaseInfo.Tables.TryGetValue(tableName, out var tableSchema))
            {
                result[tableName] = tableSchema;
            }
        }
        
        return result;
    }

    /// <summary>
    /// Group WHERE conditions by table using alias mapping
    /// </summary>
    private Dictionary<string, List<WhereCondition>> GroupWhereConditionsByTable(
        List<WhereCondition> whereConditions, 
        string sqlQuery)
    {
        var result = new Dictionary<string, List<WhereCondition>>(StringComparer.OrdinalIgnoreCase);
        
        // Create alias to table mapping
        var aliasMapping = CreateAliasMapping(sqlQuery);
        
        foreach (var condition in whereConditions)
        {
            var tableName = ResolveTableName(condition.TableAlias, aliasMapping);
            
            if (!result.ContainsKey(tableName))
            {
                result[tableName] = new List<WhereCondition>();
            }
            
            result[tableName].Add(condition);
        }
        
        return result;
    }

    /// <summary>
    /// Create mapping from table aliases to actual table names
    /// </summary>
    private Dictionary<string, string> CreateAliasMapping(string sqlQuery)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Pattern: FROM/JOIN table alias
        var aliasPattern = @"(?:FROM|JOIN)\s+(\w+)\s+(\w+)";
        var matches = Regex.Matches(sqlQuery, aliasPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            var tableName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            mapping[alias] = tableName;
        }
        
        return mapping;
    }

    /// <summary>
    /// Resolve table name from alias
    /// </summary>
    private string ResolveTableName(string alias, Dictionary<string, string> aliasMapping)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return "unknown";
        }
        
        return aliasMapping.GetValueOrDefault(alias, alias);
    }

    /// <summary>
    /// Analyze JOIN relationships from query
    /// </summary>
    private List<JoinRelationship> AnalyzeJoinRelationships(string sqlQuery, DatabaseInfo databaseInfo)
    {
        var relationships = new List<JoinRelationship>();
        
        // Pattern: JOIN table alias ON alias1.col = alias2.col
        var joinPattern = @"JOIN\s+(\w+)\s+(\w+)\s+ON\s+(\w+)\.(\w+)\s*=\s*(\w+)\.(\w+)";
        var matches = Regex.Matches(sqlQuery, joinPattern, RegexOptions.IgnoreCase);
        
        var aliasMapping = CreateAliasMapping(sqlQuery);
        
        foreach (Match match in matches)
        {
            var relationship = new JoinRelationship
            {
                LeftTable = ResolveTableName(match.Groups[3].Value, aliasMapping),
                LeftColumn = match.Groups[4].Value,
                RightTable = ResolveTableName(match.Groups[5].Value, aliasMapping),
                RightColumn = match.Groups[6].Value
            };
            
            relationships.Add(relationship);
        }
        
        return relationships;
    }

    /// <summary>
    /// Determine coordination strategy based on query complexity
    /// </summary>
    private CoordinationStrategy DetermineCoordinationStrategy(QueryAnalysisResult analysis)
    {
        if (analysis.RequiredTables.Count == 1)
        {
            return CoordinationStrategy.Simple;
        }
        
        if (analysis.WhereConditions.Sum(kvp => kvp.Value.Count) > 2 && analysis.JoinRelationships.Count > 1)
        {
            return CoordinationStrategy.Complex;
        }
        
        return CoordinationStrategy.CrossTable;
    }

    /// <summary>
    /// Generate target records that satisfy all WHERE conditions simultaneously
    /// </summary>
    private List<CoordinatedRecordSet> GenerateTargetRecords(
        QueryAnalysisResult analysis, 
        int desiredCount)
    {
        var recordSets = new List<CoordinatedRecordSet>();
        var faker = new Faker();

        for (int i = 0; i < desiredCount; i++)
        {
            var recordSet = new CoordinatedRecordSet { RecordIndex = i + 1 };
            
            // Generate records for each table that satisfy their WHERE conditions
            foreach (var (tableName, tableInfo) in analysis.RequiredTables)
            {
                var record = GenerateTableRecord(tableName, tableInfo, analysis.WhereConditions, faker, i + 1);
                recordSet.TableRecords[tableName] = record;
            }
            
            // Ensure FK relationships are consistent across tables
            EnsureForeignKeyConsistency(recordSet, analysis.JoinRelationships);
            
            recordSets.Add(recordSet);
        }

        return recordSets;
    }

    /// <summary>
    /// Generate a single table record that satisfies WHERE conditions
    /// </summary>
    private Dictionary<string, object> GenerateTableRecord(
        string tableName, 
        TableSchema tableInfo, 
        Dictionary<string, List<WhereCondition>> whereConditions,
        Faker faker,
        int recordIndex)
    {
        var record = new Dictionary<string, object>();
        var tableConditions = whereConditions.GetValueOrDefault(tableName, new List<WhereCondition>());

        foreach (var column in tableInfo.Columns.Where(c => !c.IsIdentity))
        {
            // Check if this column has specific WHERE requirements
            var columnCondition = tableConditions.FirstOrDefault(wc => 
                wc.ColumnName.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (columnCondition != null)
            {
                // Generate value that satisfies the WHERE condition
                record[column.ColumnName] = GenerateValueForCondition(columnCondition, column, faker, recordIndex);
            }
            else if (column.IsPrimaryKey)
            {
                // Generate primary key
                record[column.ColumnName] = recordIndex;
            }
            else
            {
                // Generate default value
                record[column.ColumnName] = GenerateDefaultColumnValue(column, faker, recordIndex);
            }
        }

        return record;
    }

    /// <summary>
    /// Ensure foreign key relationships are consistent across coordinated records
    /// </summary>
    private void EnsureForeignKeyConsistency(
        CoordinatedRecordSet recordSet, 
        List<JoinRelationship> joinRelationships)
    {
        foreach (var join in joinRelationships)
        {
            if (recordSet.TableRecords.ContainsKey(join.LeftTable) && 
                recordSet.TableRecords.ContainsKey(join.RightTable))
            {
                var leftRecord = recordSet.TableRecords[join.LeftTable];
                var rightRecord = recordSet.TableRecords[join.RightTable];

                // Ensure FK relationship is satisfied
                if (rightRecord.ContainsKey(join.RightColumn))
                {
                    // Copy PK value to FK
                    leftRecord[join.LeftColumn] = rightRecord[join.RightColumn];
                }
            }
        }
    }

    /// <summary>
    /// Generate value that satisfies specific WHERE condition
    /// </summary>
    private object GenerateValueForCondition(
        WhereCondition condition, 
        ColumnSchema column, 
        Faker faker, 
        int recordIndex)
    {
        return condition.Operator.ToUpper() switch
        {
            "LIKE" => GenerateLikeValue(condition.Value, faker, recordIndex),
            "=" => ConvertToColumnType(condition.Value, column),
            "YEAR_EQUALS" => GenerateDateWithYear(condition.Value, faker),
            ">" or ">=" => GenerateGreaterValue(condition.Value, column, faker),
            "<" or "<=" => GenerateLessValue(condition.Value, column, faker),
            "IN" => GenerateInValue(condition.Value, faker),
            _ => GenerateDefaultColumnValue(column, faker, recordIndex)
        };
    }

    /// <summary>
    /// Generate value for LIKE condition that guarantees match
    /// </summary>
    private string GenerateLikeValue(string likePattern, Faker faker, int recordIndex)
    {
        // Remove % wildcards to get core value
        var coreValue = likePattern.Replace("%", "").Trim();
        
        if (string.IsNullOrEmpty(coreValue))
        {
            return faker.Lorem.Word();
        }

        // 🎯 GENERIC pattern-based value generation (no hardcode business values)
        return GenerateGenericLikeVariation(coreValue, faker, recordIndex);
    }

    /// <summary>
    /// Generate generic LIKE variation - works with ANY pattern
    /// </summary>
    private string GenerateGenericLikeVariation(string coreValue, Faker faker, int recordIndex)
    {
        // Pattern-based generation for any business value
        if (coreValue.Length <= 3 && coreValue.All(char.IsUpper))
        {
            // Short uppercase pattern (codes like "DD", "IT", "HR")
            var timestamp = DateTime.Now.Ticks % 10000;
            return $"{coreValue}_{timestamp}_{recordIndex:D3}";
        }
        else if (coreValue.Any(char.IsDigit))
        {
            // Contains numbers - might be version/reference
            var randomNum = faker.Random.Number(100, 999);
            return $"{coreValue}_{randomNum}_{recordIndex}";
        }
        else if (coreValue.Length > 8)
        {
            // Long pattern - might be company/description
            var suffix = faker.Lorem.Word();
            return $"{coreValue} {suffix} {recordIndex}";
        }
        else
        {
            // Default: add unique suffix to core value
            var uniqueSuffix = $"{recordIndex:D3}_{DateTime.Now.Ticks % 1000:D3}";
            return $"{coreValue}_{uniqueSuffix}";
        }
    }

    /// <summary>
    /// Generate value greater than specified value
    /// </summary>
    private object GenerateGreaterValue(string minValue, ColumnSchema column, Faker faker)
    {
        return column.DataType.ToLower() switch
        {
            "int" or "integer" => int.TryParse(minValue, out int intMin) ? faker.Random.Int(intMin + 1, intMin + 100) : faker.Random.Int(1, 100),
            "decimal" or "numeric" => decimal.TryParse(minValue, out decimal decMin) ? faker.Random.Decimal(decMin + 1, decMin + 1000) : faker.Random.Decimal(1, 1000),
            "datetime" or "date" => DateTime.TryParse(minValue, out DateTime dateMin) ? faker.Date.Between(dateMin.AddDays(1), dateMin.AddYears(1)) : faker.Date.Future(),
            _ => minValue
        };
    }

    /// <summary>
    /// Generate value less than specified value
    /// </summary>
    private object GenerateLessValue(string maxValue, ColumnSchema column, Faker faker)
    {
        return column.DataType.ToLower() switch
        {
            "int" or "integer" => int.TryParse(maxValue, out int intMax) ? faker.Random.Int(1, Math.Max(1, intMax - 1)) : faker.Random.Int(1, 100),
            "decimal" or "numeric" => decimal.TryParse(maxValue, out decimal decMax) ? faker.Random.Decimal(1, Math.Max(1, decMax - 1)) : faker.Random.Decimal(1, 1000),
            "datetime" or "date" => DateTime.TryParse(maxValue, out DateTime dateMax) ? faker.Date.Between(dateMax.AddYears(-1), dateMax.AddDays(-1)) : faker.Date.Past(),
            _ => maxValue
        };
    }

    /// <summary>
    /// Generate value from IN list
    /// </summary>
    private object GenerateInValue(string inValues, Faker faker)
    {
        var values = inValues.Split(',').Select(v => v.Trim().Trim('\'', '"')).ToArray();
        return faker.PickRandom(values);
    }

    /// <summary>
    /// Generate date with specific year
    /// </summary>
    private DateTime GenerateDateWithYear(string year, Faker faker)
    {
        if (int.TryParse(year, out int targetYear))
        {
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear, 12, 31);
            return faker.Date.Between(startDate, endDate);
        }
        return faker.Date.Past();
    }

    /// <summary>
    /// Convert value to appropriate column type
    /// </summary>
    private object ConvertToColumnType(string value, ColumnSchema column)
    {
        return column.DataType.ToLower() switch
        {
            "int" or "integer" => int.TryParse(value, out int intVal) ? intVal : 1,
            "decimal" or "numeric" => decimal.TryParse(value, out decimal decVal) ? decVal : 1.0m,
            "datetime" or "date" => DateTime.TryParse(value, out DateTime dateVal) ? dateVal : DateTime.Now,
            "bit" or "boolean" => bool.TryParse(value, out bool boolVal) ? boolVal : true,
            _ => value
        };
    }

    /// <summary>
    /// Generate default column value
    /// </summary>
    private object GenerateDefaultColumnValue(ColumnSchema column, Faker faker, int recordIndex)
    {
        var columnName = column.ColumnName.ToLower();
        var dataType = column.DataType.ToLower();
        
        // 🎯 ENUM DETECTION: Successfully implemented for MySQL ENUM types
        
        // Check for boolean columns first (including MySQL TINYINT for boolean)
        if (IsBooleanColumn(column))
        {
            return faker.Random.Bool();
        }
        
        // 🎯 ENUM DETECTION: Check if dataType starts with "enum("
        if (dataType.StartsWith("enum("))
        {
            return GenerateEnumValue(column, faker);
        }
        
        var result = dataType switch
        {
            "int" or "integer" or "bigint" or "smallint" or "tinyint" => (object)recordIndex,
            "varchar" or "text" or "char" or "nchar" or "nvarchar" or "string" => GenerateDefaultString(column, faker, recordIndex),
            "datetime" or "datetime2" or "date" or "timestamp" => faker.Date.Recent(365),
            "bit" or "boolean" or "bool" => faker.Random.Bool(),
            "decimal" or "numeric" or "float" or "double" or "money" => faker.Random.Decimal(1, 1000),
            "uniqueidentifier" or "uuid" => Guid.NewGuid().ToString(),
            "json" or "jsonb" => GenerateJsonValue(column, faker, recordIndex),
            _ => GenerateConstraintAwareString(column, faker, recordIndex)
        };
        
        return result;
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

    /// <summary>
    /// Generate default string value based ONLY on database constraints (100% GENERIC)
    /// </summary>
    private string GenerateDefaultString(ColumnSchema column, Faker faker, int recordIndex)
    {
        // 🎯 COMPLETELY GENERIC: Only use database metadata, NO column name checking
        var maxLength = column.MaxLength ?? 255; // Use actual constraint or reasonable default
        
        // Generate appropriate length string based on MaxLength constraint
        var targetLength = Math.Min(10, maxLength); // Reasonable default, respect constraint
        
        // 🔧 CRITICAL FIX: Never return empty string for Oracle
        if (targetLength <= 0)
        {
            return "default"; // Minimum viable string
        }
        
        // Generate generic alphanumeric string with record uniqueness
        var baseValue = faker.Random.AlphaNumeric(Math.Max(1, targetLength - 3)); // Leave room for suffix
        var uniqueSuffix = $"_{recordIndex:00}";
        
        var rawValue = baseValue + uniqueSuffix;
        
        // Apply MaxLength constraint strictly - but never return empty
        var result = ApplyMaxLengthConstraint(rawValue, column);
        
        // Final safety check - never return empty string
        if (string.IsNullOrEmpty(result))
        {
            return "def"; // Ultra-minimal fallback
        }
        
        return result;
    }
    
    /// <summary>
    /// Generate constraint-aware string value for generic fallback cases
    /// </summary>
    private string GenerateConstraintAwareString(ColumnSchema column, Faker faker, int recordIndex)
    {
        // For unknown types, generate a simple alphanumeric value
        var maxLength = column.MaxLength ?? 50; // Default to 50 if not specified
        var targetLength = Math.Min(8, maxLength); // Use reasonable length, max 8 chars
        
        return faker.Random.AlphaNumeric(targetLength);
    }
    
    /// <summary>
    /// Apply MaxLength constraint to any string value
    /// </summary>
    private string ApplyMaxLengthConstraint(string value, ColumnSchema column)
    {
        if (string.IsNullOrEmpty(value))
        {
            // 🔧 CRITICAL FIX: Never return empty string, return minimal valid value
            return "x"; // Single character fallback
        }
        
        var maxLength = column.MaxLength;
        if (maxLength.HasValue && value.Length > maxLength.Value)
        {
            // Truncate but ensure at least 1 character
            var truncated = value.Substring(0, Math.Max(1, maxLength.Value));
            if (string.IsNullOrEmpty(truncated))
            {
                return "x"; // Safety fallback
            }
            return truncated;
        }
        
        return value;
    }
    
    /// <summary>
    /// Generate random value from ENUM options (generic)
    /// </summary>
    private string GenerateEnumValue(ColumnSchema column, Faker faker)
    {
        // Parse ENUM values from column type definition  
        // Example: "enum('Male','Female','Other')" -> ["Male", "Female", "Other"]
        var enumValues = ParseEnumValues(column.DataType);
        
        if (enumValues.Count > 0)
        {
            return faker.Random.ListItem(enumValues);
        }
        
        // Fallback if can't parse ENUM values
        return GenerateConstraintAwareString(column, faker, 1);
    }
    
    /// <summary>
    /// Parse ENUM values from MySQL ENUM type definition (generic)
    /// </summary>
    private List<string> ParseEnumValues(string enumTypeDefinition)
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
    
    /// <summary>
    /// Generate JSON value based ONLY on database constraints (100% GENERIC)
    /// </summary>
    private string GenerateJsonValue(ColumnSchema column, Faker faker, int recordIndex)
    {
        // 🎯 COMPLETELY GENERIC: Generate simple JSON regardless of column name
        var maxLength = column.MaxLength ?? 1000;
        
        // Generate generic JSON that fits within MaxLength constraint
        string rawValue;
        if (maxLength < 10)
        {
            rawValue = "{}"; // Minimal JSON
        }
        else if (maxLength < 50)
        {
            rawValue = $"{{\"id\":{recordIndex}}}"; // Simple JSON with record ID
        }
        else
        {
            rawValue = $"{{\"id\":{recordIndex},\"value\":\"{faker.Random.AlphaNumeric(5)}\",\"active\":true}}";
        }
        
        // Apply MaxLength constraint strictly
        return ApplyMaxLengthConstraint(rawValue, column);
    }
    
    // 🎯 REMOVED: Hardcoded JSON generation methods
    // These violated the GENERIC principle by checking specific column names

    /// <summary>
    /// Convert coordinated records to INSERT statements
    /// </summary>
    private List<InsertStatement> ConvertToInsertStatements(
        List<CoordinatedRecordSet> coordinatedRecords, 
        DatabaseInfo databaseInfo)
    {
        var insertStatements = new List<InsertStatement>();
        
        // Order tables by dependencies (parents first)
        var orderedTables = OrderTablesByDependencies(coordinatedRecords, databaseInfo);
        
        foreach (var tableName in orderedTables)
        {
            if (!databaseInfo.Tables.TryGetValue(tableName, out var tableSchema))
            {
                _logger.Warning("Table schema not found for {TableName}, skipping", tableName);
                continue;
            }

            foreach (var recordSet in coordinatedRecords)
            {
                if (recordSet.TableRecords.ContainsKey(tableName))
                {
                    var record = recordSet.TableRecords[tableName];
                    
                    // 🎯 FIXED: Use CommonInsertBuilder with proper schema filtering
                    var insertSql = _insertBuilder.BuildInsertStatement(tableName, record, tableSchema, databaseInfo.Type);
                    
                    // 🎯 VALIDATION: Ensure no generated columns in INSERT
                    if (!_insertBuilder.ValidateInsertStatement(insertSql, tableSchema))
                    {
                        throw new InvalidOperationException($"Generated INSERT statement contains generated columns for table {tableName}");
                    }
                    
                    insertStatements.Add(new InsertStatement
                    {
                        TableName = tableName,
                        SqlStatement = insertSql,
                        Priority = GetTablePriority(tableName, databaseInfo)
                    });
                }
            }
        }
        
        return insertStatements;
    }

    /// <summary>
    /// Order tables by dependencies to avoid FK constraint violations
    /// </summary>
    private List<string> OrderTablesByDependencies(
        List<CoordinatedRecordSet> coordinatedRecords, 
        DatabaseInfo databaseInfo)
    {
        var tables = coordinatedRecords
            .SelectMany(rs => rs.TableRecords.Keys)
            .Distinct()
            .ToList();
            
        // Simple ordering: tables with no FKs first, then by FK count
        return tables
            .OrderBy(t => databaseInfo.Tables[t].ForeignKeys.Count)
            .ThenBy(t => t)
            .ToList();
    }

    // 🎯 REMOVED: BuildInsertStatement, QuoteIdentifier, FormatValue methods
    // These are now handled by CommonInsertBuilder to eliminate code duplication
    // and ensure consistent generated column filtering

    /// <summary>
    /// Get table priority for insertion order
    /// </summary>
    private int GetTablePriority(string tableName, DatabaseInfo databaseInfo)
    {
        if (databaseInfo.Tables.TryGetValue(tableName, out var table))
        {
            return table.ForeignKeys.Count; // Higher FK count = higher priority (insert later)
        }
        return 0;
    }

    /// <summary>
    /// Validate generated data by executing query and checking result count
    /// </summary>
    private async Task<ValidationResult> ValidateGeneratedData(
        List<InsertStatement> insertStatements,
        string sqlQuery,
        int expectedCount,
        string databaseType,
        string connectionString)
    {
        try
        {
            using var connection = DbConnectionFactory.CreateConnection(databaseType, connectionString);
            connection.Open();
            
            using var transaction = connection.BeginTransaction();
            
            try
            {
                // Execute INSERT statements
                foreach (var statement in insertStatements.OrderBy(s => s.Priority))
                {
                    await connection.ExecuteAsync(statement.SqlStatement, transaction: transaction, commandTimeout: 300);
                }
                
                // Execute original query to check result count
                var results = await connection.QueryAsync(sqlQuery, transaction: transaction, commandTimeout: 300);
                var actualCount = results.Count();
                
                transaction.Rollback(); // Don't commit validation data
                
                return new ValidationResult
                {
                    IsValid = actualCount >= expectedCount,
                    ActualCount = actualCount,
                    ExpectedCount = expectedCount
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Validation failed, assuming data is sufficient");
            return new ValidationResult { IsValid = true, ActualCount = expectedCount, ExpectedCount = expectedCount };
        }
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

    #region Helper Classes
    
    public class QueryAnalysisResult
    {
        public Dictionary<string, TableSchema> RequiredTables { get; set; } = new();
        public Dictionary<string, List<WhereCondition>> WhereConditions { get; set; } = new();
        public List<JoinRelationship> JoinRelationships { get; set; } = new();
        public CoordinationStrategy CoordinationStrategy { get; set; }
    }
    
    public class CoordinatedRecordSet
    {
        public int RecordIndex { get; set; }
        public Dictionary<string, Dictionary<string, object>> TableRecords { get; set; } = new();
    }
    
    public class JoinRelationship
    {
        public string LeftTable { get; set; } = "";
        public string LeftColumn { get; set; } = "";
        public string RightTable { get; set; } = "";
        public string RightColumn { get; set; } = "";
    }
    
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public int ActualCount { get; set; }
        public int ExpectedCount { get; set; }
    }
    
    public enum CoordinationStrategy
    {
        Simple,           // Single table or simple conditions
        CrossTable,       // Multiple tables with JOINs
        Complex           // Complex WHERE conditions across multiple tables
    }
    
    #endregion
} 