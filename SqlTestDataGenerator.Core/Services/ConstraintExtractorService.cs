using System.Text.RegularExpressions;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Extract comprehensive constraints từ SQL analysis + DB metadata
/// 100% Generic - works với any SQL và database
/// </summary>
public class ConstraintExtractorService
{
    private readonly ILogger _logger;

    public ConstraintExtractorService()
    {
        _logger = Log.Logger.ForContext<ConstraintExtractorService>();
    }

    /// <summary>
    /// Build comprehensive generation context cho AI
    /// </summary>
    public GenerationContext BuildGenerationContext(
        string tableName, 
        ColumnSchema column, 
        TableSchema tableSchema,
        string sqlQuery,
        List<WhereCondition> whereConditions)
    {
        _logger.Information("Building generation context for {TableName}.{ColumnName}", tableName, column.ColumnName);

        var context = new GenerationContext
        {
            TableName = tableName,
            Column = MapColumnContext(column),
            Constraints = ExtractConstraints(column, tableSchema),
            Relationships = ExtractRelationships(column, tableSchema),
            SqlConditions = ExtractSqlConditions(column.ColumnName, whereConditions),
            BusinessHints = ExtractBusinessHints(tableName, column.ColumnName, sqlQuery)
        };

        _logger.Information("Generated context with {ConstraintCount} constraints, {RelationshipCount} relationships",
            context.Constraints.Count, context.Relationships.Count);

        return context;
    }

    /// <summary>
    /// Map database column schema to generation context
    /// </summary>
    private ColumnContext MapColumnContext(ColumnSchema column)
    {
        var columnContext = new ColumnContext
        {
            Name = column.ColumnName,
            DataType = column.DataType,
            MaxLength = column.MaxLength,
            IsRequired = !column.IsNullable,
            IsUnique = column.IsUnique,
            IsPrimaryKey = column.IsPrimaryKey,
            IsGenerated = column.IsGenerated,
            DefaultValue = column.DefaultValue
        };

        // Extract ENUM values if applicable
        if (column.DataType.ToLower().StartsWith("enum("))
        {
            columnContext.EnumValues = ParseEnumValues(column.DataType);
        }

        return columnContext;
    }

    /// <summary>
    /// Extract database constraints (CHECK, FK, UNIQUE, etc.)
    /// </summary>
    private List<ConstraintInfo> ExtractConstraints(ColumnSchema column, TableSchema tableSchema)
    {
        var constraints = new List<ConstraintInfo>();

        // ENUM constraint
        if (column.DataType.ToLower().StartsWith("enum("))
        {
            constraints.Add(new ConstraintInfo
            {
                Type = "ENUM",
                Description = $"Must be one of the predefined values",
                AllowedValues = ParseEnumValues(column.DataType)
            });
        }

        // Length constraint
        if (column.MaxLength.HasValue)
        {
            constraints.Add(new ConstraintInfo
            {
                Type = "LENGTH",
                Description = $"Maximum length: {column.MaxLength}",
                MaxValue = column.MaxLength.Value
            });
        }

        // NOT NULL constraint
        if (!column.IsNullable)
        {
            constraints.Add(new ConstraintInfo
            {
                Type = "NOT_NULL",
                Description = "Value is required"
            });
        }

        // UNIQUE constraint
        if (column.IsUnique || column.IsPrimaryKey)
        {
            constraints.Add(new ConstraintInfo
            {
                Type = "UNIQUE",
                Description = "Value must be unique"
            });
        }

        return constraints;
    }

    /// <summary>
    /// Extract relationship information (FK, JOIN references)
    /// </summary>
    private List<RelationshipInfo> ExtractRelationships(ColumnSchema column, TableSchema tableSchema)
    {
        var relationships = new List<RelationshipInfo>();

        // Check for foreign key relationships
        var fk = tableSchema.ForeignKeys.FirstOrDefault(fk => 
            fk.ColumnName.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase));

        if (fk != null)
        {
            relationships.Add(new RelationshipInfo
            {
                RelatedTable = fk.ReferencedTable,
                RelatedColumn = fk.ReferencedColumn,
                RelationshipType = "FOREIGN_KEY",
                AvailableValues = new List<object>() // Will be populated later
            });
        }

        return relationships;
    }

    /// <summary>
    /// Extract SQL conditions từ WHERE clauses
    /// </summary>
    private List<ConditionInfo> ExtractSqlConditions(string columnName, List<WhereCondition> whereConditions)
    {
        var conditions = new List<ConditionInfo>();

        var relevantConditions = whereConditions.Where(wc => 
            wc.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

        foreach (var condition in relevantConditions)
        {
            var conditionInfo = new ConditionInfo
            {
                Operator = condition.Operator
            };

            switch (condition.Operator.ToUpper())
            {
                case "=":
                    conditionInfo.Value = condition.Value;
                    break;
                case "LIKE":
                    conditionInfo.Pattern = condition.Value;
                    break;
                case "IN":
                    conditionInfo.InValues = ParseInValues(condition.Value);
                    break;
                case ">":
                case ">=":
                    conditionInfo.Value = condition.Value;
                    break;
                case "<":
                case "<=":
                    conditionInfo.Value = condition.Value;
                    break;
            }

            conditions.Add(conditionInfo);
        }

        return conditions;
    }

    /// <summary>
    /// Extract business context hints từ table/column names và SQL structure
    /// </summary>
    private BusinessContext ExtractBusinessHints(string tableName, string columnName, string sqlQuery)
    {
        var hints = new BusinessContext
        {
            Domain = DetermineDomain(tableName),
            SemanticHints = ExtractSemanticHints(columnName),
            PurposeContext = "test_data_generation"
        };

        return hints;
    }

    /// <summary>
    /// Determine business domain từ table name (generic pattern detection)
    /// </summary>
    private string DetermineDomain(string tableName)
    {
        var lowerTable = tableName.ToLower();
        
        if (lowerTable.Contains("user") || lowerTable.Contains("person") || lowerTable.Contains("employee"))
            return "user_management";
        
        if (lowerTable.Contains("company") || lowerTable.Contains("organization") || lowerTable.Contains("business"))
            return "company_management";
            
        if (lowerTable.Contains("product") || lowerTable.Contains("item") || lowerTable.Contains("inventory"))
            return "product_management";
            
        if (lowerTable.Contains("order") || lowerTable.Contains("sale") || lowerTable.Contains("transaction"))
            return "transaction_management";

        return "generic_business";
    }

    /// <summary>
    /// Extract semantic hints từ column name (patterns only, not hardcoded values)
    /// </summary>
    private List<string> ExtractSemanticHints(string columnName)
    {
        var hints = new List<string>();
        var lowerColumn = columnName.ToLower();

        // Pattern-based detection (not value generation)
        if (lowerColumn.Contains("email")) hints.Add("email_format");
        if (lowerColumn.Contains("phone")) hints.Add("phone_format");
        if (lowerColumn.Contains("address")) hints.Add("address_format");
        if (lowerColumn.Contains("name")) hints.Add("name_format");
        if (lowerColumn.Contains("code")) hints.Add("code_format");
        if (lowerColumn.Contains("url")) hints.Add("url_format");
        if (lowerColumn.Contains("date")) hints.Add("date_format");

        return hints;
    }

    /// <summary>
    /// Parse ENUM values từ MySQL ENUM definition
    /// </summary>
    private List<string> ParseEnumValues(string enumDefinition)
    {
        var values = new List<string>();
        
        try
        {
            var match = Regex.Match(enumDefinition, @"enum\s*\(\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var valuesString = match.Groups[1].Value;
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
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse ENUM values from: {EnumDefinition}", enumDefinition);
        }
        
        return values;
    }

    /// <summary>
    /// Parse IN clause values
    /// </summary>
    private List<object> ParseInValues(string inClause)
    {
        var values = new List<object>();
        
        try
        {
            // Simple parsing - can be enhanced for complex cases
            var cleanValues = inClause.Trim('(', ')').Split(',');
            foreach (var value in cleanValues)
            {
                var cleanValue = value.Trim().Trim('\'').Trim('"');
                values.Add(cleanValue);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse IN values from: {InClause}", inClause);
        }
        
        return values;
    }
} 