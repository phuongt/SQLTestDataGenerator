using System.Data;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// AI-enhanced wrapper cho CoordinatedDataGenerator
/// Kết hợp Engine constraint extraction và AI intelligent generation
/// </summary>
public class AIEnhancedCoordinatedDataGenerator
{
    private readonly ILogger _logger;
    private readonly CoordinatedDataGenerator _coordinatedGenerator;
    private readonly ConstraintExtractorService _constraintExtractor;
    private readonly IAIDataGenerationService _aiService;
    private readonly SqlMetadataService _metadataService;
    private readonly CommonInsertBuilder _insertBuilder;

    public AIEnhancedCoordinatedDataGenerator(
        CoordinatedDataGenerator coordinatedGenerator,
        ConstraintExtractorService constraintExtractor,
        IAIDataGenerationService aiService,
        SqlMetadataService metadataService,
        CommonInsertBuilder insertBuilder)
    {
        _logger = Log.Logger.ForContext<AIEnhancedCoordinatedDataGenerator>();
        _coordinatedGenerator = coordinatedGenerator;
        _constraintExtractor = constraintExtractor;
        _aiService = aiService;
        _metadataService = metadataService;
        _insertBuilder = insertBuilder;
    }

    /// <summary>
    /// Generate data using AI-enhanced approach with constraint extraction
    /// </summary>
    public async Task<List<InsertStatement>> GenerateIntelligentDataAsync(
        DatabaseInfo databaseInfo,
        string sqlQuery,
        int desiredRecordCount,
        string databaseType,
        string connectionString)
    {
        _logger.Information("Starting AI-enhanced data generation for {DesiredCount} records", desiredRecordCount);

        try
        {
            // Step 1: Check if AI is available
            var isAIAvailable = await _aiService.IsAvailableAsync();
            _logger.Information("AI service availability: {IsAvailable}", isAIAvailable);

            if (!isAIAvailable)
            {
                _logger.Information("AI unavailable, falling back to coordinated generation");
                return await _coordinatedGenerator.GenerateCoordinatedDataAsync(
                    databaseInfo, sqlQuery, desiredRecordCount, databaseType, connectionString);
            }

            // Step 2: Extract constraints from SQL and database metadata
            var constraints = ExtractConstraintsFromDatabase(databaseInfo);

            _logger.Information("Extracted {ConstraintCount} constraints across {TableCount} tables", 
                constraints.Sum(c => c.Value.Sum(tc => tc.Value.Count)), 
                constraints.Count);

            // Step 3: Generate AI-enhanced data with constraint compliance
            var insertStatements = await GenerateAIDataWithConstraints(
                constraints, databaseInfo, sqlQuery, desiredRecordCount, databaseType, connectionString);

            _logger.Information("AI-enhanced generation completed: {StatementCount} statements", insertStatements.Count);
            return insertStatements;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "AI-enhanced generation failed, falling back to coordinated generation");
            return await _coordinatedGenerator.GenerateCoordinatedDataAsync(
                databaseInfo, sqlQuery, desiredRecordCount, databaseType, connectionString);
        }
    }

    /// <summary>
    /// Generate data using AI với comprehensive constraint context
    /// </summary>
    private async Task<List<InsertStatement>> GenerateAIDataWithConstraints(
        Dictionary<string, Dictionary<string, List<ConstraintInfo>>> constraints,
        DatabaseInfo databaseInfo,
        string sqlQuery,
        int desiredRecordCount,
        string databaseType,
        string connectionString)
    {
        var insertStatements = new List<InsertStatement>();
        
        // Group tables by dependency order
        var orderedTables = OrderTablesByDependencies(constraints.Keys.ToList(), databaseInfo);
        
        foreach (var tableName in orderedTables)
        {
            if (!constraints.ContainsKey(tableName)) continue;
            
            var tableConstraints = constraints[tableName];
            var tableSchema = databaseInfo.Tables[tableName];
            
            _logger.Information("Generating AI data for table {Table} with {ColumnCount} columns", 
                tableName, tableConstraints.Count);
            
            // Generate records for this table
            var tableRecords = await GenerateTableRecordsWithAI(
                tableName, tableSchema, tableConstraints, desiredRecordCount, sqlQuery, databaseInfo);
            
            // Convert to INSERT statements
            var tableInserts = ConvertToInsertStatements(tableName, tableSchema, tableRecords, (DatabaseType)Enum.Parse(typeof(DatabaseType), databaseType));
            insertStatements.AddRange(tableInserts);
        }
        
        return insertStatements;
    }

    /// <summary>
    /// Generate records cho một table using AI với full constraint context
    /// </summary>
    private async Task<List<Dictionary<string, object>>> GenerateTableRecordsWithAI(
        string tableName,
        TableSchema tableSchema,
        Dictionary<string, List<ConstraintInfo>> columnConstraints,
        int recordCount,
        string sqlQuery,
        DatabaseInfo databaseInfo)
    {
        var records = new List<Dictionary<string, object>>();
        
        // Get business domain hints
        var businessHints = new BusinessContext
        {
            Domain = DetectBusinessDomain(tableName),
            SemanticHints = DetectSemanticHints(tableSchema),
            PurposeContext = "test_data"
        };
        
        for (int i = 1; i <= recordCount; i++)
        {
            var record = new Dictionary<string, object>();
            
            foreach (var column in tableSchema.Columns)
            {
                // Skip generated columns
                if (column.IsGenerated) continue;
                
                // Build generation context
                var context = BuildGenerationContext(
                    tableName, column, columnConstraints.GetValueOrDefault(column.ColumnName, new List<ConstraintInfo>()),
                    sqlQuery, businessHints, databaseInfo);
                
                // Generate value using AI
                var value = await _aiService.GenerateColumnValueAsync(context, i);
                record[column.ColumnName] = value;
                
                _logger.Debug("Generated value for {Table}.{Column}: {Value}", 
                    tableName, column.ColumnName, value);
            }
            
            records.Add(record);
        }
        
        return records;
    }

    /// <summary>
    /// Build comprehensive GenerationContext cho AI service
    /// </summary>
    private GenerationContext BuildGenerationContext(
        string tableName,
        ColumnSchema column,
        List<ConstraintInfo> constraints,
        string sqlQuery,
        BusinessContext businessHints,
        DatabaseInfo databaseInfo)
    {
        var context = new GenerationContext
        {
            TableName = tableName,
            Column = new ColumnContext
            {
                Name = column.ColumnName,
                DataType = column.DataType,
                MaxLength = column.MaxLength,
                IsRequired = !column.IsNullable,
                IsUnique = column.IsUnique,
                IsPrimaryKey = column.IsPrimaryKey,
                IsGenerated = column.IsGenerated,
                EnumValues = column.EnumValues,
                DefaultValue = column.DefaultValue
            },
            Constraints = constraints,
            BusinessHints = businessHints,
            SqlConditions = ExtractSqlConditionsForColumn(sqlQuery, tableName, column.ColumnName),
            Relationships = ExtractRelationshipContext(tableName, column, databaseInfo)
        };
        
        return context;
    }

    /// <summary>
    /// Extract SQL conditions that apply to specific column
    /// </summary>
    private List<ConditionInfo> ExtractSqlConditionsForColumn(string sqlQuery, string tableName, string columnName)
    {
        var conditions = new List<ConditionInfo>();
        
        // Extract WHERE conditions that mention this column
        var wherePattern = $@"WHERE.*?{tableName}\.{columnName}\s*([=<>!]+|LIKE|IN)\s*([^,\s]+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(sqlQuery, wherePattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var condition = new ConditionInfo
            {
                Operator = match.Groups[1].Value.Trim(),
                Value = match.Groups[2].Value.Trim()
            };
            conditions.Add(condition);
        }
        
        return conditions;
    }

    /// <summary>
    /// Extract relationship context for foreign key generation
    /// </summary>
    private List<RelationshipInfo> ExtractRelationshipContext(
        string tableName, 
        ColumnSchema column, 
        DatabaseInfo databaseInfo)
    {
        var relationships = new List<RelationshipInfo>();
        
        // Check if this column is a foreign key
        if (column.ColumnName.EndsWith("_id") || column.ColumnName.EndsWith("Id"))
        {
            // Try to find referenced table
            var referencedTableName = column.ColumnName.Replace("_id", "").Replace("Id", "");
            
            if (databaseInfo.Tables.ContainsKey(referencedTableName))
            {
                var relationship = new RelationshipInfo
                {
                    RelatedTable = referencedTableName,
                    RelatedColumn = "id", // Assume primary key is 'id'
                    RelationshipType = "FK",
                    AvailableValues = new List<object>() // Would be populated from DB
                };
                relationships.Add(relationship);
            }
        }
        
        return relationships;
    }

    /// <summary>
    /// Order tables by dependency để ensure FK references exist
    /// </summary>
    private List<string> OrderTablesByDependencies(List<string> tableNames, DatabaseInfo databaseInfo)
    {
        var orderedTables = new List<string>();
        var processed = new HashSet<string>();
        
        // Simple ordering: tables without FK first, then tables with FK
        var tablesWithoutFK = tableNames.Where(t => !HasForeignKeys(t, databaseInfo)).ToList();
        var tablesWithFK = tableNames.Where(t => HasForeignKeys(t, databaseInfo)).ToList();
        
        orderedTables.AddRange(tablesWithoutFK);
        orderedTables.AddRange(tablesWithFK);
        
        return orderedTables;
    }

    /// <summary>
    /// Check if table has foreign key columns
    /// </summary>
    private bool HasForeignKeys(string tableName, DatabaseInfo databaseInfo)
    {
        if (!databaseInfo.Tables.ContainsKey(tableName)) return false;
        
        var table = databaseInfo.Tables[tableName];
        return table.Columns.Any(c => c.ColumnName.EndsWith("_id") || c.ColumnName.EndsWith("Id"));
    }

    /// <summary>
    /// Convert generated records to INSERT statements
    /// </summary>
    private List<InsertStatement> ConvertToInsertStatements(
        string tableName,
        TableSchema tableSchema,
        List<Dictionary<string, object>> records,
        DatabaseType databaseType = DatabaseType.Oracle)
    {
        var insertStatements = new List<InsertStatement>();
        
        foreach (var record in records)
        {
            // 🔧 CRITICAL FIX: Use CommonInsertBuilder with proper Oracle support
            var insertSql = _insertBuilder.BuildInsertStatement(tableName, record, tableSchema, databaseType);
            
            var insertStatement = new InsertStatement
            {
                TableName = tableName,
                SqlStatement = insertSql,
                Priority = 0
            };
            
            insertStatements.Add(insertStatement);
        }
        
        return insertStatements;
    }

    /// <summary>
    /// Detect business domain from table name
    /// </summary>
    private string DetectBusinessDomain(string tableName)
    {
        var lowerName = tableName.ToLower();
        
        return lowerName switch
        {
            var name when name.Contains("user") => "user_management",
            var name when name.Contains("company") || name.Contains("organization") => "company_management",
            var name when name.Contains("product") || name.Contains("item") => "product_catalog",
            var name when name.Contains("order") || name.Contains("purchase") => "order_management",
            var name when name.Contains("role") || name.Contains("permission") => "access_control",
            var name when name.Contains("payment") || name.Contains("transaction") => "financial",
            _ => "general"
        };
    }

    /// <summary>
    /// Detect semantic hints from column names
    /// </summary>
    private List<string> DetectSemanticHints(TableSchema tableSchema)
    {
        var hints = new List<string>();
        
        foreach (var column in tableSchema.Columns)
        {
            var lowerName = column.ColumnName.ToLower();
            
            if (lowerName.Contains("email"))
                hints.Add("email_format");
            else if (lowerName.Contains("phone"))
                hints.Add("phone_format");
            else if (lowerName.Contains("address"))
                hints.Add("address_format");
            else if (lowerName.Contains("name") && !lowerName.Contains("username"))
                hints.Add("person_name");
            else if (lowerName.Contains("username"))
                hints.Add("username_format");
            else if (lowerName.Contains("code"))
                hints.Add("code_format");
            else if (lowerName.Contains("url") || lowerName.Contains("website"))
                hints.Add("url_format");
            else if (lowerName.Contains("description"))
                hints.Add("descriptive_text");
        }
        
        return hints.Distinct().ToList();
    }

    /// <summary>
    /// Extract constraints from database info
    /// </summary>
    private Dictionary<string, Dictionary<string, List<ConstraintInfo>>> ExtractConstraintsFromDatabase(DatabaseInfo databaseInfo)
    {
        var constraints = new Dictionary<string, Dictionary<string, List<ConstraintInfo>>>();
        
        foreach (var table in databaseInfo.Tables.Values)
        {
            var tableConstraints = new Dictionary<string, List<ConstraintInfo>>();
            
            foreach (var column in table.Columns)
            {
                var columnConstraints = new List<ConstraintInfo>();
                
                // Length constraint
                if (column.MaxLength.HasValue)
                {
                    columnConstraints.Add(new ConstraintInfo
                    {
                        Type = "LENGTH",
                        Description = $"Maximum length: {column.MaxLength}",
                        MaxValue = column.MaxLength.Value
                    });
                }
                
                // NOT NULL constraint
                if (!column.IsNullable)
                {
                    columnConstraints.Add(new ConstraintInfo
                    {
                        Type = "NOT_NULL",
                        Description = "Value is required"
                    });
                }
                
                // ENUM constraint
                if (column.EnumValues?.Count > 0)
                {
                    columnConstraints.Add(new ConstraintInfo
                    {
                        Type = "ENUM",
                        Description = "Must be one of predefined values",
                        AllowedValues = column.EnumValues
                    });
                }
                
                tableConstraints[column.ColumnName] = columnConstraints;
            }
            
            constraints[table.TableName] = tableConstraints;
        }
        
        return constraints;
    }
} 