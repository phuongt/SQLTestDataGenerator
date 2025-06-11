using SqlTestDataGenerator.Core.Models;
using System.Text.RegularExpressions;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Enhanced Dependency Resolver - Giải quyết vấn đề Foreign Key dependencies
/// Theo approach: Parse SQL → Extract metadata → Generate trong đúng thứ tự dependencies
/// </summary>
public class EnhancedDependencyResolver
{
    /// <summary>
    /// Step 1: Parse SELECT query và extract tất cả tables cần thiết
    /// Không chỉ FROM tables mà cả FK dependencies
    /// </summary>
    public ParsedQuery ParseSelectQuery(string sqlQuery, DatabaseInfo databaseInfo)
    {
        var result = new ParsedQuery();
        
        // Extract tables từ FROM và JOIN clauses
        result.MainTables = ExtractTablesFromQuery(sqlQuery);
        
        // Extract WHERE conditions
        result.WhereConditions = ExtractWhereConditions(sqlQuery);
        
        // Extract JOIN conditions  
        result.JoinConditions = ExtractJoinConditions(sqlQuery);
        
        // Step 2: Resolve ALL dependencies for extracted tables
        result.AllRequiredTables = ResolveAllDependencies(result.MainTables, databaseInfo);
        
        return result;
    }
    
    /// <summary>
    /// Step 2: Resolve tất cả FK dependencies recursively
    /// Nếu query cần users, mà users có FK tới companies → cần tạo companies trước
    /// </summary>
    private List<string> ResolveAllDependencies(List<string> mainTables, DatabaseInfo databaseInfo)
    {
        var allTables = new HashSet<string>(mainTables, StringComparer.OrdinalIgnoreCase);
        var toProcess = new Queue<string>(mainTables);
        
        // Recursively resolve dependencies
        while (toProcess.Count > 0)
        {
            var currentTable = toProcess.Dequeue();
            
            if (databaseInfo.Tables.TryGetValue(currentTable, out var tableSchema))
            {
                // Add all FK dependency tables
                foreach (var fk in tableSchema.ForeignKeys)
                {
                    var referencedTable = fk.ReferencedTable; // Extract referenced table
                    
                    if (!allTables.Contains(referencedTable))
                    {
                        allTables.Add(referencedTable);
                        toProcess.Enqueue(referencedTable);
                    }
                }
            }
        }
        
        return allTables.ToList();
    }
    
    /// <summary>
    /// Step 3: Generate INSERT statements trong đúng dependency order
    /// Parent tables (no FK) → Child tables (has FK)
    /// </summary>
    public List<string> OrderTablesByDependencies(List<string> tables, DatabaseInfo databaseInfo)
    {
        var result = new List<string>();
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var processing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var table in tables)
        {
            if (!processed.Contains(table))
            {
                VisitTable(table, databaseInfo, result, processed, processing, tables);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Recursive dependency resolution với cycle detection
    /// </summary>
    private void VisitTable(string tableName, DatabaseInfo databaseInfo, 
        List<string> result, HashSet<string> processed, HashSet<string> processing, List<string> availableTables)
    {
        if (processing.Contains(tableName))
        {
            // Cycle detected - skip for now
            return;
        }
        
        if (processed.Contains(tableName))
        {
            return;
        }
        
        processing.Add(tableName);
        
        // Visit all dependencies first (parents before children)
        if (databaseInfo.Tables.TryGetValue(tableName, out var tableSchema))
        {
            foreach (var fk in tableSchema.ForeignKeys)
            {
                var referencedTable = fk.ReferencedTable;
                
                // Only process if it's in our list of required tables
                if (availableTables.Contains(referencedTable, StringComparer.OrdinalIgnoreCase))
                {
                    VisitTable(referencedTable, databaseInfo, result, processed, processing, availableTables);
                }
            }
        }
        
        processing.Remove(tableName);
        processed.Add(tableName);
        result.Add(tableName);
    }
    
    /// <summary>
    /// Step 4: Validate generated data satisfies original SELECT query conditions
    /// </summary>
    public bool ValidateDataSatisfiesQuery(ParsedQuery parsedQuery, DatabaseInfo databaseInfo)
    {
        // Basic validation - can be enhanced
        
        // Check if all required tables will have data
        foreach (var table in parsedQuery.MainTables)
        {
            if (!parsedQuery.AllRequiredTables.Contains(table, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        
        // Check if JOIN conditions can be satisfied (có FK relationships)
        foreach (var join in parsedQuery.JoinConditions)
        {
            if (!CanSatisfyJoinCondition(join, databaseInfo))
            {
                return false;
            }
        }
        
        return true;
    }
    
    #region Helper Methods
    
    private List<string> ExtractTablesFromQuery(string sqlQuery)
    {
        var tables = new List<string>();
        var cleanSql = RemoveStringsAndComments(sqlQuery);
        
        // FROM clause
        var fromPattern = @"\bFROM\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)(?:\s+(?:AS\s+)?(\w+))?";
        var fromMatches = Regex.Matches(cleanSql, fromPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in fromMatches)
        {
            var tableName = match.Groups[1].Value ?? match.Groups[2].Value ?? match.Groups[3].Value;
            if (!string.IsNullOrEmpty(tableName))
            {
                tables.Add(tableName);
            }
        }
        
        // JOIN clauses
        var joinPattern = @"\b(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|OUTER\s+)?JOIN\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)(?:\s+(?:AS\s+)?(\w+))?";
        var joinMatches = Regex.Matches(cleanSql, joinPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in joinMatches)
        {
            var tableName = match.Groups[1].Value ?? match.Groups[2].Value ?? match.Groups[3].Value;
            if (!string.IsNullOrEmpty(tableName))
            {
                tables.Add(tableName);
            }
        }
        
        return tables.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
    
    private List<string> ExtractWhereConditions(string sqlQuery)
    {
        var conditions = new List<string>();
        
        // Simple WHERE extraction - can be enhanced
        var wherePattern = @"\bWHERE\s+(.+?)(?:\s+GROUP\s+BY|\s+ORDER\s+BY|\s+HAVING|\s+LIMIT|\s*$)";
        var match = Regex.Match(sqlQuery, wherePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        if (match.Success)
        {
            conditions.Add(match.Groups[1].Value.Trim());
        }
        
        return conditions;
    }
    
    private List<string> ExtractJoinConditions(string sqlQuery)
    {
        var conditions = new List<string>();
        
        // Extract ON conditions from JOINs
        var joinPattern = @"\bJOIN\s+[^\.]+\s+ON\s+([^(JOIN|WHERE|GROUP|ORDER|HAVING|LIMIT)]+)";
        var matches = Regex.Matches(sqlQuery, joinPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            conditions.Add(match.Groups[1].Value.Trim());
        }
        
        return conditions;
    }
    
    private bool CanSatisfyJoinCondition(string joinCondition, DatabaseInfo databaseInfo)
    {
        // Basic check - có thể enhance thêm
        // Check if join condition references valid FK relationships
        return true; // Placeholder
    }
    
    private string RemoveStringsAndComments(string sql)
    {
        // Remove string literals and comments for cleaner parsing
        var result = Regex.Replace(sql, @"'([^'\\]|\\.)*'", "''");
        result = Regex.Replace(result, @"""([^""\\]|\\.)*""", @"""""");
        result = Regex.Replace(result, @"--.*$", "", RegexOptions.Multiline);
        result = Regex.Replace(result, @"/\*.*?\*/", "", RegexOptions.Singleline);
        
        return result;
    }
    
    #endregion
}

/// <summary>
/// Kết quả parse SQL query với dependency information
/// </summary>
public class ParsedQuery
{
    public List<string> MainTables { get; set; } = new();
    public List<string> AllRequiredTables { get; set; } = new();
    public List<string> WhereConditions { get; set; } = new();
    public List<string> JoinConditions { get; set; } = new();
    public int ExpectedMinimumRows { get; set; } = 1;
} 