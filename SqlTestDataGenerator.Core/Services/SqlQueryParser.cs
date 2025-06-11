using System.Text.RegularExpressions;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Service to parse SQL queries and extract data generation requirements
/// to ensure generated data satisfies WHERE conditions and JOIN requirements
/// </summary>
public class SqlQueryParser
{
    private readonly ILogger _logger;

    public SqlQueryParser()
    {
        _logger = Log.ForContext<SqlQueryParser>();
    }

    /// <summary>
    /// Parse SQL query to extract data generation requirements
    /// </summary>
    public SqlDataRequirements ParseQuery(string sqlQuery)
    {
        _logger.Information("Parsing SQL query to extract data requirements");
        
        var requirements = new SqlDataRequirements
        {
            OriginalQuery = sqlQuery
        };

        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            return requirements;
        }

        var cleanQuery = CleanSqlQuery(sqlQuery);
        
        // Extract table requirements
        ExtractTableRequirements(cleanQuery, requirements);
        
        // Extract WHERE conditions
        ExtractWhereConditions(cleanQuery, requirements);
        
        // Extract JOIN requirements
        ExtractJoinRequirements(cleanQuery, requirements);

        _logger.Information("Extracted {WhereCount} WHERE conditions and {JoinCount} JOIN requirements", 
            requirements.WhereConditions.Count, requirements.JoinRequirements.Count);

        return requirements;
    }

    private string CleanSqlQuery(string sqlQuery)
    {
        // Remove SQL comments
        var withoutLineComments = Regex.Replace(sqlQuery, @"--[^\r\n]*", "", RegexOptions.Multiline);
        var withoutBlockComments = Regex.Replace(withoutLineComments, @"/\*[\s\S]*?\*/", "", RegexOptions.Multiline);
        
        // Normalize whitespace
        var normalized = Regex.Replace(withoutBlockComments, @"\s+", " ");
        
        return normalized.Trim();
    }

    private void ExtractTableRequirements(string cleanQuery, SqlDataRequirements requirements)
    {
        // Extract tables from FROM and JOIN clauses
        var tables = SqlMetadataService.ExtractTablesFromQuery(cleanQuery);
        
        foreach (var table in tables)
        {
            if (!requirements.TableRequirements.ContainsKey(table))
            {
                requirements.TableRequirements[table] = new TableDataRequirement
                {
                    TableName = table
                };
            }
        }
    }

    private void ExtractWhereConditions(string cleanQuery, SqlDataRequirements requirements)
    {
        // Find WHERE clause - Fixed: pattern to properly match WHERE clauses at end of query
        var whereMatch = Regex.Match(cleanQuery, @"\bWHERE\s+(.*?)(?:\s+(?:ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT)|$)", 
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!whereMatch.Success)
        {
            return;
        }

        var whereClause = whereMatch.Groups[1].Value;
        _logger.Information("Found WHERE clause: {WhereClause}", whereClause);

        // Parse various WHERE condition types
        ParseLikeConditions(whereClause, requirements);
        ParseEqualityConditions(whereClause, requirements);
        ParseDateConditions(whereClause, requirements);
        ParseNullChecks(whereClause, requirements);
        ParseInConditions(whereClause, requirements);
        ParseComparisonConditions(whereClause, requirements);
    }

    private void ParseLikeConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column LIKE 'value' or column LIKE '%value%'
        var likePattern = @"(\w+\.)?(\w+)\s+LIKE\s+['""]([^'""]*)['""]";
        var matches = Regex.Matches(whereClause, likePattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var likeValue = match.Groups[3].Value;

            // Extract actual value by removing % wildcards
            var actualValue = likeValue.Replace("%", "").Trim();

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = "LIKE",
                Value = actualValue,
                OriginalCondition = match.Value
            });

            _logger.Information("Found LIKE condition: {Column} LIKE '{Value}'", columnName, actualValue);
        }
    }

    private void ParseEqualityConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column = 'value' or column = 123 or column = TRUE/FALSE
        var equalityPattern = @"(\w+\.)?(\w+)\s*=\s*(['""]?)([^'"";\s\)]+)\3";
        var matches = Regex.Matches(whereClause, equalityPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var value = match.Groups[4].Value;

            // Skip if it's a JOIN condition (comparing columns) but allow TRUE/FALSE/0/1
            if (value.Contains('.') || (IsReservedWord(value) && !IsBooleanValue(value)))
            {
                continue;
            }

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = "=",
                Value = value,
                OriginalCondition = match.Value
            });

            _logger.Information("Found equality condition: {Column} = '{Value}'", columnName, value);
        }
    }
    
    /// <summary>
    /// Check if value is a boolean value (TRUE, FALSE, 0, 1)
    /// </summary>
    private static bool IsBooleanValue(string value)
    {
        var booleanValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TRUE", "FALSE", "0", "1"
        };
        return booleanValues.Contains(value);
    }

    private void ParseDateConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: YEAR(column) = 1989
        var yearPattern = @"YEAR\s*\(\s*(\w+\.)?(\w+)\s*\)\s*=\s*(\d{4})";
        var matches = Regex.Matches(whereClause, yearPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var year = match.Groups[3].Value;

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = "YEAR_EQUALS",
                Value = year,
                OriginalCondition = match.Value
            });

            _logger.Information("Found year condition: YEAR({Column}) = {Year}", columnName, year);
        }

        // Enhanced MySQL DATE_ADD pattern - supports multiple operators and units
        // Pattern để match: column <= DATE_ADD(NOW(), INTERVAL 30 DAY) 
        // Hỗ trợ: DAY, DAYS, MONTH, MONTHS, YEAR, YEARS
        var dateIntervalPattern = @"(\w+\.)?(\w+)\s*(<=|>=|<|>|=)\s*DATE_ADD\s*\(\s*NOW\s*\(\s*\)\s*,\s*INTERVAL\s+(\d+)\s+(DAY|DAYS|MONTH|MONTHS|YEAR|YEARS)\s*\)";
        var intervalMatches = Regex.Matches(whereClause, dateIntervalPattern, RegexOptions.IgnoreCase);

        foreach (Match match in intervalMatches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var operator_ = match.Groups[3].Value;
            var interval = match.Groups[4].Value;
            var unit = match.Groups[5].Value.ToUpper();
            
            // Normalize unit (remove S if present)
            if (unit.EndsWith("S"))
                unit = unit.Substring(0, unit.Length - 1);

            var constraintValue = $"{interval}_{unit}";
            var operatorName = operator_ == "<=" ? "DATE_WITHIN" : "DATE_COMPARE";

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = operatorName,
                Value = constraintValue,
                OriginalCondition = match.Value
            });

            _logger.Information("Found MySQL date condition: {Column} {Op} DATE_ADD(NOW(), INTERVAL {Interval} {Unit})", 
                columnName, operator_, interval, unit);
        }
        
        // Pattern để match reverse order: DATE_ADD(NOW(), INTERVAL 30 DAY) >= column
        var reverseDateIntervalPattern = @"DATE_ADD\s*\(\s*NOW\s*\(\s*\)\s*,\s*INTERVAL\s+(\d+)\s+(DAY|DAYS|MONTH|MONTHS|YEAR|YEARS)\s*\)\s*(<=|>=|<|>|=)\s*(\w+\.)?(\w+)";
        var reverseMatches = Regex.Matches(whereClause, reverseDateIntervalPattern, RegexOptions.IgnoreCase);

        foreach (Match match in reverseMatches)
        {
            var interval = match.Groups[1].Value;
            var unit = match.Groups[2].Value.ToUpper();
            var operator_ = match.Groups[3].Value;
            var tableAlias = match.Groups[4].Value.TrimEnd('.');
            var columnName = match.Groups[5].Value;
            
            // Normalize unit (remove S if present)
            if (unit.EndsWith("S"))
                unit = unit.Substring(0, unit.Length - 1);

            // Reverse operator logic for DATE_ADD >= column => column <= DATE_ADD  
            var reversedOperator = operator_ switch
            {
                ">=" => "<=",
                "<=" => ">=", 
                ">" => "<",
                "<" => ">",
                _ => operator_
            };

            var constraintValue = $"{interval}_{unit}";
            var operatorName = reversedOperator == "<=" ? "DATE_WITHIN" : "DATE_COMPARE";

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = operatorName,
                Value = constraintValue,
                OriginalCondition = match.Value
            });

            _logger.Information("Found MySQL date condition (reversed): {Column} {Op} DATE_ADD(NOW(), INTERVAL {Interval} {Unit})", 
                columnName, reversedOperator, interval, unit);
        }
    }

    private void ParseComparisonConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column >= value, column > value, column < value, column <= value
        var comparisonPattern = @"(\w+\.)?(\w+)\s*(>=|<=|>|<)\s*(['""]?)([^'"";\s\)]+)\4";
        var matches = Regex.Matches(whereClause, comparisonPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var operator_ = match.Groups[3].Value;
            var value = match.Groups[5].Value;

            // Skip if it's a column comparison or DATE_ADD function
            if (value.Contains('.') || IsReservedWord(value) || value.Contains("DATE_ADD"))
            {
                continue;
            }

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = operator_,
                Value = value,
                OriginalCondition = match.Value
            });

            _logger.Information("Found comparison condition: {Column} {Operator} '{Value}'", columnName, operator_, value);
        }
    }

    private void ParseNullChecks(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column IS NULL or column IS NOT NULL
        var nullPattern = @"(\w+\.)?(\w+)\s+IS\s+(NOT\s+)?NULL";
        var matches = Regex.Matches(whereClause, nullPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var isNotNull = !string.IsNullOrEmpty(match.Groups[3].Value);

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = isNotNull ? "IS_NOT_NULL" : "IS_NULL",
                Value = "",
                OriginalCondition = match.Value
            });

            _logger.Information("Found null check: {Column} IS {NotNull}NULL", columnName, isNotNull ? "NOT " : "");
        }
    }

    private void ParseInConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column IN (value1, value2, value3)
        var inPattern = @"(\w+\.)?(\w+)\s+IN\s*\(\s*([^)]+)\s*\)";
        var matches = Regex.Matches(whereClause, inPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var valuesList = match.Groups[3].Value;

            // Parse individual values
            var values = valuesList.Split(',')
                .Select(v => v.Trim().Trim('\'', '"'))
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();

            if (values.Any())
            {
                requirements.WhereConditions.Add(new WhereCondition
                {
                    TableAlias = tableAlias,
                    ColumnName = columnName,
                    Operator = "IN",
                    Value = string.Join(",", values),
                    OriginalCondition = match.Value
                });

                _logger.Information("Found IN condition: {Column} IN ({Values})", columnName, string.Join(", ", values));
            }
        }
    }

    private void ExtractJoinRequirements(string cleanQuery, SqlDataRequirements requirements)
    {
        // 🎯 FIXED: Simplified JOIN pattern that actually works
        // Pattern: JOIN table alias ON condition
        var joinPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|OUTER\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^()]+?)(?=\s+(?:INNER|LEFT|RIGHT|FULL|OUTER|WHERE|ORDER|GROUP|LIMIT|$)|$)";
        
        Console.WriteLine($"JOIN Debug: Clean query = '{cleanQuery}'");
        Console.WriteLine($"JOIN Debug: Pattern = '{joinPattern}'");
        
        _logger.Information("JOIN Debug: Clean query = '{CleanQuery}'", cleanQuery);
        _logger.Information("JOIN Debug: Pattern = '{Pattern}'", joinPattern);
        
        var matches = Regex.Matches(cleanQuery, joinPattern, RegexOptions.IgnoreCase);
        Console.WriteLine($"JOIN Debug: Found {matches.Count} JOIN matches");
        _logger.Information("JOIN Debug: Found {MatchCount} JOIN matches", matches.Count);

        foreach (Match match in matches)
        {
            var table = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            var onClause = match.Groups[3].Value;
            
            Console.WriteLine($"JOIN Debug: Match found - Table: '{table}', Alias: '{alias}', OnClause: '{onClause}'");
            _logger.Information("JOIN Debug: Match found - Table: '{Table}', Alias: '{Alias}', OnClause: '{OnClause}'", table, alias, onClause);

            // Parse main JOIN condition (table1.col = table2.col)
            var joinConditionPattern = @"(\w+)\.(\w+)\s*=\s*(\w+)\.(\w+)";
            var joinMatch = Regex.Match(onClause, joinConditionPattern);
            
            Console.WriteLine($"JOIN Debug: Condition pattern match success: {joinMatch.Success}");
            _logger.Information("JOIN Debug: Condition pattern match success: {Success}", joinMatch.Success);
            
            if (joinMatch.Success)
            {
                var alias1 = joinMatch.Groups[1].Value;
                var column1 = joinMatch.Groups[2].Value;
                var alias2 = joinMatch.Groups[3].Value;
                var column2 = joinMatch.Groups[4].Value;

                requirements.JoinRequirements.Add(new JoinRequirement
                {
                    LeftTableAlias = alias1,
                    LeftColumn = column1,
                    RightTableAlias = alias2,
                    RightColumn = column2,
                    RightTable = table
                });

                Console.WriteLine($"Found JOIN: {alias1}.{column1} = {alias2}.{column2}");
                _logger.Information("Found JOIN: {LeftAlias}.{LeftCol} = {RightAlias}.{RightCol}",
                    alias1, column1, alias2, column2);
            }

            // 🎯 GENERIC FIX: Parse additional WHERE conditions in JOIN ON clause
            // Extract conditions like "AND ur.is_active = TRUE"
            ParseEqualityConditions(onClause, requirements);
            ParseComparisonConditions(onClause, requirements);
            ParseNullChecks(onClause, requirements);
        }
    }

    private static bool IsReservedWord(string word)
    {
        var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "OUTER", "ON",
            "AND", "OR", "NOT", "IN", "EXISTS", "LIKE", "BETWEEN", "IS", "NULL",
            "ORDER", "BY", "GROUP", "HAVING", "DISTINCT", "TOP", "LIMIT", "OFFSET",
            "TRUE", "FALSE", "NOW", "DATE_ADD", "YEAR", "INTERVAL", "DAY"
        };

        return reservedWords.Contains(word);
    }
}

/// <summary>
/// Data structure to hold SQL query analysis results
/// </summary>
public class SqlDataRequirements
{
    public string OriginalQuery { get; set; } = "";
    public Dictionary<string, TableDataRequirement> TableRequirements { get; set; } = new();
    public List<WhereCondition> WhereConditions { get; set; } = new();
    public List<JoinRequirement> JoinRequirements { get; set; } = new();
}

public class TableDataRequirement
{
    public string TableName { get; set; } = "";
    public Dictionary<string, object> RequiredValues { get; set; } = new();
}

public class WhereCondition
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string Operator { get; set; } = "";
    public string Value { get; set; } = "";
    public string OriginalCondition { get; set; } = "";
}

public class JoinRequirement
{
    public string LeftTableAlias { get; set; } = "";
    public string LeftColumn { get; set; } = "";
    public string RightTableAlias { get; set; } = "";
    public string RightColumn { get; set; } = "";
    public string RightTable { get; set; } = "";
} 