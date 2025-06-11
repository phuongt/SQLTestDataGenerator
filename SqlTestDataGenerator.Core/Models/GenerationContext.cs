using System.Collections.Generic;

namespace SqlTestDataGenerator.Core.Models;

/// <summary>
/// Comprehensive context cho AI data generation
/// Extracted from SQL analysis + DB metadata
/// </summary>
public class GenerationContext
{
    public string TableName { get; set; } = "";
    public ColumnContext Column { get; set; } = new();
    public List<ConstraintInfo> Constraints { get; set; } = new();
    public List<RelationshipInfo> Relationships { get; set; } = new();
    public List<ConditionInfo> SqlConditions { get; set; } = new();
    public BusinessContext BusinessHints { get; set; } = new();
}

/// <summary>
/// Column information với enhanced metadata
/// </summary>
public class ColumnContext
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public int? MaxLength { get; set; }
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsGenerated { get; set; }
    public List<string>? EnumValues { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Database constraint information
/// </summary>
public class ConstraintInfo
{
    public string Type { get; set; } = ""; // CHECK, FK, UNIQUE, etc.
    public string Description { get; set; } = "";
    public List<string> AllowedValues { get; set; } = new();
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
}

/// <summary>
/// Foreign key and relationship information
/// </summary>
public class RelationshipInfo
{
    public string RelatedTable { get; set; } = "";
    public string RelatedColumn { get; set; } = "";
    public string RelationshipType { get; set; } = ""; // FK, JOIN
    public List<object> AvailableValues { get; set; } = new();
}

/// <summary>
/// SQL conditions và business hints từ WHERE clauses
/// </summary>
public class ConditionInfo
{
    public string Operator { get; set; } = ""; // =, LIKE, IN, >, <
    public object? Value { get; set; }
    public string Pattern { get; set; } = ""; // For LIKE patterns
    public List<object> InValues { get; set; } = new();
}

/// <summary>
/// Business context hints extracted từ SQL analysis
/// </summary>
public class BusinessContext
{
    public string Domain { get; set; } = ""; // user, company, product, etc.
    public List<string> SemanticHints { get; set; } = new(); // email, phone, address
    public string PurposeContext { get; set; } = ""; // test data, demo data
} 