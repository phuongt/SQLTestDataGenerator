namespace SqlTestDataGenerator.Core.Models;

public class TableSchema
{
    public string TableName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public List<ColumnSchema> Columns { get; set; } = new();
    public List<ForeignKeySchema> ForeignKeys { get; set; } = new();
    public List<string> PrimaryKeys { get; set; } = new();
}

public class ColumnSchema
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsGenerated { get; set; }
    public int? MaxLength { get; set; }
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }
    public string? DefaultValue { get; set; }
    
    // Additional properties for AI integration
    public bool IsUnique { get; set; }
    public List<string>? EnumValues { get; set; }
}

public class ForeignKeySchema
{
    public string ConstraintName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public string ReferencedColumn { get; set; } = string.Empty;
    public string ReferencedSchema { get; set; } = "dbo";
}

public class InsertStatement
{
    public string TableName { get; set; } = string.Empty;
    public string SqlStatement { get; set; } = string.Empty;
    public int Priority { get; set; } = 0; // For dependency ordering
}

public enum DatabaseType
{
    SqlServer,
    MySQL,
    PostgreSQL
}

public class DatabaseInfo
{
    public DatabaseType Type { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public Dictionary<string, TableSchema> Tables { get; set; } = new();
} 