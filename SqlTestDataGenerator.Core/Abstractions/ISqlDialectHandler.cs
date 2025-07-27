using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Core.Abstractions
{
    public interface ISqlDialectHandler
    {
        string EscapeIdentifier(string identifier);
        DatabaseType SupportedDatabaseType { get; }
        
        // Oracle-specific methods
        string ConvertDateFunction(string mysqlFunction);
        string ConvertDataType(string mysqlDataType);
        string FormatValue(string value, string dataType);
        string GetStatementTerminator();
        string GetPaginationSyntax(int offset, int limit);
        string GetAutoIncrementSyntax(string tableName, string columnName);
    }
} 