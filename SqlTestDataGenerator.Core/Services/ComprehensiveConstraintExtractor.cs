using System.Text.RegularExpressions;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Comprehensive constraint extractor để bóc tách tất cả điều kiện từ SQL
/// Supports: WHERE conditions, JOIN conditions, LIKE patterns, complex AND/OR logic
/// </summary>
public class ComprehensiveConstraintExtractor
{
    private readonly ILogger _logger;

    public ComprehensiveConstraintExtractor()
    {
        _logger = Log.Logger.ForContext<ComprehensiveConstraintExtractor>();
    }

    /// <summary>
    /// Extract tất cả constraints từ SQL query để truyền cho AI
    /// </summary>
    public ComprehensiveConstraints ExtractAllConstraints(string sqlQuery)
    {
        _logger.Information("Extracting comprehensive constraints from SQL query");

        var constraints = new ComprehensiveConstraints
        {
            OriginalQuery = sqlQuery
        };

        var cleanQuery = CleanSqlQuery(sqlQuery);
        
        // Extract WHERE conditions
        ExtractWhereConstraints(cleanQuery, constraints);
        
        // Extract JOIN conditions  
        ExtractJoinConstraints(cleanQuery, constraints);
        
        // Extract LIKE patterns
        ExtractLikePatterns(cleanQuery, constraints);
        
        // Extract date constraints
        ExtractDateConstraints(cleanQuery, constraints);
        
        // Extract boolean constraints
        ExtractBooleanConstraints(cleanQuery, constraints);
        
        // Extract IN clause constraints
        ExtractInClauseConstraints(cleanQuery, constraints);
        
        // Extract BETWEEN constraints
        ExtractBetweenConstraints(cleanQuery, constraints);
        
        // Extract NULL constraints
        ExtractNullConstraints(cleanQuery, constraints);
        
        // Extract EXISTS constraints
        ExtractExistsConstraints(cleanQuery, constraints);

        _logger.Information("Extracted {Total} total constraints: {WHERE} WHERE, {JOIN} JOIN, {LIKE} LIKE, {IN} IN, {BETWEEN} BETWEEN, {NULL} NULL, {EXISTS} EXISTS", 
            constraints.GetTotalCount(), constraints.WhereConstraints.Count, 
            constraints.JoinConstraints.Count, constraints.LikePatterns.Count,
            constraints.InClauseConstraints.Count, constraints.BetweenConstraints.Count, constraints.NullConstraints.Count, constraints.ExistsConstraints.Count);

        return constraints;
    }

    /// <summary>
    /// Extract WHERE clause constraints
    /// </summary>
    private void ExtractWhereConstraints(string cleanQuery, ComprehensiveConstraints constraints)
    {
        // Find WHERE clause
        var whereMatch = Regex.Match(cleanQuery, @"\bWHERE\s+(.*?)(?:\s+(?:ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT)|$)", 
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!whereMatch.Success) return;

        var whereClause = whereMatch.Groups[1].Value;
        _logger.Information("Found WHERE clause: {WhereClause}", whereClause);

        // ========== WITH TABLE ALIAS PATTERNS ==========
        
        // Parse equality conditions: table.column = value
        var equalityPatternWithAlias = @"(\w+)\.(\w+)\s*=\s*(['""]?)([^'"";\s\)]+)\3";
        var aliasEqualityMatches = Regex.Matches(whereClause, equalityPatternWithAlias, RegexOptions.IgnoreCase);

        foreach (Match match in aliasEqualityMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var value = match.Groups[4].Value;

            // Skip column comparisons, keep value comparisons
            if (!value.Contains('.') && !IsReservedWord(value))
            {
                constraints.WhereConstraints.Add(new GeneralConstraintInfo
                {
                    TableAlias = tableAlias,
                    ColumnName = columnName,
                    Operator = "=",
                    Value = value,
                    Source = "WHERE"
                });

                _logger.Information("Found WHERE equality with alias: {Table}.{Column} = '{Value}'", tableAlias, columnName, value);
            }
        }

        // Parse comparison conditions: table.column >, <, >=, <=
        var comparisonPatternWithAlias = @"(\w+)\.(\w+)\s*(>=|<=|>|<)\s*(['""]?)([^'"";\s\)]+)\4";
        var aliasComparisonMatches = Regex.Matches(whereClause, comparisonPatternWithAlias, RegexOptions.IgnoreCase);

        foreach (Match match in aliasComparisonMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var operator_ = match.Groups[3].Value;
            var value = match.Groups[5].Value;

            if (!value.Contains('.') && !IsReservedWord(value))
            {
                constraints.WhereConstraints.Add(new GeneralConstraintInfo
                {
                    TableAlias = tableAlias,
                    ColumnName = columnName,
                    Operator = operator_,
                    Value = value,
                    Source = "WHERE"
                });

                _logger.Information("Found WHERE comparison with alias: {Table}.{Column} {Op} '{Value}'", tableAlias, columnName, operator_, value);
            }
        }

        // ========== WITHOUT TABLE ALIAS PATTERNS ==========
        
        // Parse equality conditions: column = value (no table alias)
        var equalityPatternNoAlias = @"\b(\w+)\s*=\s*(['""]?)([^'"";\s\)]+)\2";
        var noAliasEqualityMatches = Regex.Matches(whereClause, equalityPatternNoAlias, RegexOptions.IgnoreCase);

        foreach (Match match in noAliasEqualityMatches)
        {
            var columnName = match.Groups[1].Value;
            var value = match.Groups[3].Value;

            // Skip if already captured with alias, or if column is a reserved word
            if (aliasEqualityMatches.Cast<Match>().Any(m => m.Groups[2].Value == columnName) || 
                IsReservedWord(columnName) || value.Contains('.'))
                continue;

            if (!IsReservedWord(value))
            {
                constraints.WhereConstraints.Add(new GeneralConstraintInfo
                {
                    TableAlias = "", // No alias available
                    ColumnName = columnName,
                    Operator = "=",
                    Value = value,
                    Source = "WHERE"
                });

                _logger.Information("Found WHERE equality without alias: {Column} = '{Value}'", columnName, value);
            }
        }

        // Parse comparison conditions: column >, <, >=, <= (no table alias)
        var comparisonPatternNoAlias = @"\b(\w+)\s*(>=|<=|>|<)\s*(['""]?)([^'"";\s\)]+)\3";
        var noAliasComparisonMatches = Regex.Matches(whereClause, comparisonPatternNoAlias, RegexOptions.IgnoreCase);

        foreach (Match match in noAliasComparisonMatches)
        {
            var columnName = match.Groups[1].Value;
            var operator_ = match.Groups[2].Value;
            var value = match.Groups[4].Value;

            // Skip if already captured with alias, or if column is a reserved word
            if (aliasComparisonMatches.Cast<Match>().Any(m => m.Groups[2].Value == columnName) || 
                IsReservedWord(columnName) || value.Contains('.'))
                continue;

            if (!IsReservedWord(value))
            {
                constraints.WhereConstraints.Add(new GeneralConstraintInfo
                {
                    TableAlias = "", // No alias available
                    ColumnName = columnName,
                    Operator = operator_,
                    Value = value,
                    Source = "WHERE"
                });

                _logger.Information("Found WHERE comparison without alias: {Column} {Op} '{Value}'", columnName, operator_, value);
            }
        }
    }

    /// <summary>
    /// Extract JOIN conditions including additional constraints trong ON clause
    /// </summary>
    private void ExtractJoinConstraints(string cleanQuery, ComprehensiveConstraints constraints)
    {
        // 🎯 ENHANCED: Extract JOIN với complex ON conditions
        var joinPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|OUTER\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^()]+?)(?=\s+(?:INNER|LEFT|RIGHT|FULL|OUTER|WHERE|ORDER|GROUP|LIMIT|$)|$)";
        var matches = Regex.Matches(cleanQuery, joinPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var table = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            var onClause = match.Groups[3].Value;

            _logger.Information("Processing JOIN: {Table} {Alias} ON {OnClause}", table, alias, onClause);

            // Extract main JOIN condition: table1.col = table2.col
            var joinConditionPattern = @"(\w+)\.(\w+)\s*=\s*(\w+)\.(\w+)";
            var joinMatch = Regex.Match(onClause, joinConditionPattern);

            if (joinMatch.Success)
            {
                constraints.JoinConstraints.Add(new JoinConstraintInfo
                {
                    LeftTableAlias = joinMatch.Groups[1].Value,
                    LeftColumn = joinMatch.Groups[2].Value,
                    RightTableAlias = joinMatch.Groups[3].Value,
                    RightColumn = joinMatch.Groups[4].Value,
                    RightTable = table
                });
            }

            // 🎯 KEY FIX: Extract additional constraints trong JOIN ON clause
            // Parse: "AND ur.is_active = TRUE" patterns
            var additionalConstraintPattern = @"AND\s+(\w+)\.(\w+)\s*=\s*(['""]?)([^'"";\s\)]+)\3";
            var additionalMatches = Regex.Matches(onClause, additionalConstraintPattern, RegexOptions.IgnoreCase);

            foreach (Match additionalMatch in additionalMatches)
            {
                var tableAlias = additionalMatch.Groups[1].Value;
                var columnName = additionalMatch.Groups[2].Value;
                var value = additionalMatch.Groups[4].Value;

                constraints.JoinConstraints.Add(new JoinConstraintInfo
                {
                    LeftTableAlias = tableAlias,
                    LeftColumn = columnName,
                    Operator = "=",
                    Value = value,
                    Source = "JOIN_CONDITION",
                    RightTable = table
                });

                _logger.Information("Found JOIN additional constraint: {Table}.{Column} = '{Value}'", tableAlias, columnName, value);
            }
        }
    }

    /// <summary>
    /// Extract LIKE patterns để ensure AI generates matching values
    /// </summary>
    private void ExtractLikePatterns(string cleanQuery, ComprehensiveConstraints constraints)
    {
        // LIKE pattern with table alias: table.column LIKE '%value%'
        var likePatternWithAlias = @"(\w+)\.(\w+)\s+LIKE\s+['""]([^'""]*)['""]";
        var aliasMatches = Regex.Matches(cleanQuery, likePatternWithAlias, RegexOptions.IgnoreCase);

        foreach (Match match in aliasMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var likeValue = match.Groups[3].Value;

            // Extract actual value by removing % and _ wildcards
            var actualValue = likeValue.Replace("%", "").Replace("_", "").Trim();

            if (!string.IsNullOrEmpty(actualValue))
            {
                constraints.LikePatterns.Add(new LikePatternInfo
                {
                    TableAlias = tableAlias,
                    ColumnName = columnName,
                    Pattern = likeValue,
                    RequiredValue = actualValue,
                    PatternType = DetermineLikePatternType(likeValue)
                });

                _logger.Information("Found LIKE pattern with alias: {Table}.{Column} LIKE '{Pattern}' (requires: '{Value}')", 
                    tableAlias, columnName, likeValue, actualValue);
            }
        }

        // LIKE pattern without table alias: column LIKE '%value%'
        var likePatternWithoutAlias = @"\b(\w+)\s+LIKE\s+['""]([^'""]*)['""]";
        var noAliasMatches = Regex.Matches(cleanQuery, likePatternWithoutAlias, RegexOptions.IgnoreCase);

        foreach (Match match in noAliasMatches)
        {
            var columnName = match.Groups[1].Value;
            var likeValue = match.Groups[2].Value;

            // Skip if already captured with alias
            if (aliasMatches.Cast<Match>().Any(m => m.Groups[2].Value == columnName))
                continue;

            // Extract actual value by removing % and _ wildcards
            var actualValue = likeValue.Replace("%", "").Replace("_", "").Trim();

            if (!string.IsNullOrEmpty(actualValue))
            {
                constraints.LikePatterns.Add(new LikePatternInfo
                {
                    TableAlias = "", // No alias available
                    ColumnName = columnName,
                    Pattern = likeValue,
                    RequiredValue = actualValue,
                    PatternType = DetermineLikePatternType(likeValue)
                });

                _logger.Information("Found LIKE pattern without alias: {Column} LIKE '{Pattern}' (requires: '{Value}')", 
                    columnName, likeValue, actualValue);
            }
        }
    }

    /// <summary>
    /// Extract date constraints như YEAR() functions
    /// </summary>
    private void ExtractDateConstraints(string cleanQuery, ComprehensiveConstraints constraints)
    {
        // Pattern: YEAR(column) = 1989
        var yearPattern = @"YEAR\s*\(\s*(\w+)\.(\w+)\s*\)\s*=\s*(\d{4})";
        var matches = Regex.Matches(cleanQuery, yearPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var year = match.Groups[3].Value;

            constraints.DateConstraints.Add(new DateConstraintInfo
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                ConstraintType = "YEAR_EQUALS",
                Value = year
            });

            _logger.Information("Found YEAR constraint: YEAR({Table}.{Column}) = {Year}", tableAlias, columnName, year);
        }

        // MySQL DATE_ADD patterns - enhanced to support more formats
        // Pattern: column <= DATE_ADD(NOW(), INTERVAL 30 DAY)
        var dateIntervalPattern = @"(\w+)\.(\w+)\s*(<=|>=|<|>|=)\s*DATE_ADD\s*\(\s*NOW\s*\(\s*\)\s*,\s*INTERVAL\s+(\d+)\s+(DAY|DAYS|MONTH|MONTHS|YEAR|YEARS)\s*\)";
        var intervalMatches = Regex.Matches(cleanQuery, dateIntervalPattern, RegexOptions.IgnoreCase);

        foreach (Match match in intervalMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var operator_ = match.Groups[3].Value;
            var interval = match.Groups[4].Value;
            var unit = match.Groups[5].Value.ToUpper();
            
            // Normalize unit (remove S if present)
            if (unit.EndsWith("S"))
                unit = unit.Substring(0, unit.Length - 1);

            constraints.DateConstraints.Add(new DateConstraintInfo
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                ConstraintType = "DATE_INTERVAL",
                Operator = operator_,
                Value = $"{interval}_{unit}"
            });

            _logger.Information("Found DATE constraint: {Table}.{Column} {Op} DATE_ADD(NOW(), INTERVAL {Interval} {Unit})", 
                tableAlias, columnName, operator_, interval, unit);
        }
        
        // Pattern: DATE_ADD(NOW(), INTERVAL 30 DAY) >= column (reverse order)
        var reverseDateIntervalPattern = @"DATE_ADD\s*\(\s*NOW\s*\(\s*\)\s*,\s*INTERVAL\s+(\d+)\s+(DAY|DAYS|MONTH|MONTHS|YEAR|YEARS)\s*\)\s*(<=|>=|<|>|=)\s*(\w+)\.(\w+)";
        var reverseMatches = Regex.Matches(cleanQuery, reverseDateIntervalPattern, RegexOptions.IgnoreCase);

        foreach (Match match in reverseMatches)
        {
            var interval = match.Groups[1].Value;
            var unit = match.Groups[2].Value.ToUpper();
            var operator_ = match.Groups[3].Value;
            var tableAlias = match.Groups[4].Value;
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

            constraints.DateConstraints.Add(new DateConstraintInfo
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                ConstraintType = "DATE_INTERVAL",
                Operator = reversedOperator,
                Value = $"{interval}_{unit}"
            });

            _logger.Information("Found DATE constraint (reversed): {Table}.{Column} {Op} DATE_ADD(NOW(), INTERVAL {Interval} {Unit})", 
                tableAlias, columnName, reversedOperator, interval, unit);
        }
    }

    /// <summary>
    /// Extract boolean constraints như is_active = TRUE/FALSE
    /// </summary>
    private void ExtractBooleanConstraints(string cleanQuery, ComprehensiveConstraints constraints)
    {
        // Boolean pattern with table alias: table.column = TRUE/FALSE/0/1
        var booleanPatternWithAlias = @"(\w+)\.(\w+)\s*=\s*(TRUE|FALSE|0|1)\b";
        var aliasMatches = Regex.Matches(cleanQuery, booleanPatternWithAlias, RegexOptions.IgnoreCase);

        foreach (Match match in aliasMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var value = match.Groups[3].Value;

            constraints.BooleanConstraints.Add(new BooleanConstraintInfo
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Value = value,
                BooleanValue = ParseBooleanValue(value)
            });

            _logger.Information("Found BOOLEAN constraint with alias: {Table}.{Column} = {Value}", tableAlias, columnName, value);
        }

        // Boolean pattern without table alias: column = TRUE/FALSE/0/1
        var booleanPatternWithoutAlias = @"\b(\w+)\s*=\s*(TRUE|FALSE|0|1)\b";
        var noAliasMatches = Regex.Matches(cleanQuery, booleanPatternWithoutAlias, RegexOptions.IgnoreCase);

        foreach (Match match in noAliasMatches)
        {
            var columnName = match.Groups[1].Value;
            var value = match.Groups[2].Value;

            // Skip if already captured with alias or if column is a reserved word
            if (aliasMatches.Cast<Match>().Any(m => m.Groups[2].Value == columnName) || IsReservedWord(columnName))
                continue;

            constraints.BooleanConstraints.Add(new BooleanConstraintInfo
            {
                TableAlias = "", // No alias available
                ColumnName = columnName,
                Value = value,
                BooleanValue = ParseBooleanValue(value)
            });

            _logger.Information("Found BOOLEAN constraint without alias: {Column} = {Value}", columnName, value);
        }
    }

    /// <summary>
    /// Extract IN clause constraints như IN ('HR', 'IT', 'Finance')
    /// </summary>
    private void ExtractInClauseConstraints(string cleanQuery, ComprehensiveConstraints constraints)
    {
        // IN clause với table alias: table.column IN ('value1', 'value2', ...)
        var inClausePatternWithAlias = @"(\w+)\.(\w+)\s+IN\s*\(\s*([^)]+)\s*\)";
        var aliasMatches = Regex.Matches(cleanQuery, inClausePatternWithAlias, RegexOptions.IgnoreCase);

        foreach (Match match in aliasMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var inClauseContent = match.Groups[3].Value.Trim();

            ProcessInClauseContent(tableAlias, columnName, inClauseContent, constraints);
        }

        // IN clause without table alias: column IN ('value1', 'value2', ...)
        var inClausePatternWithoutAlias = @"\b(\w+)\s+IN\s*\(\s*([^)]+)\s*\)";
        var noAliasMatches = Regex.Matches(cleanQuery, inClausePatternWithoutAlias, RegexOptions.IgnoreCase);

        foreach (Match match in noAliasMatches)
        {
            var columnName = match.Groups[1].Value;
            var inClauseContent = match.Groups[2].Value.Trim();

            // Skip if already captured with alias or if column is a reserved word
            if (aliasMatches.Cast<Match>().Any(m => m.Groups[2].Value == columnName) || IsReservedWord(columnName))
                continue;

            ProcessInClauseContent("", columnName, inClauseContent, constraints);
        }
    }

    /// <summary>
    /// Process IN clause content để determine type và extract values
    /// </summary>
    private void ProcessInClauseContent(string tableAlias, string columnName, string inClauseContent, ComprehensiveConstraints constraints)
    {
        // Check if it's a subquery
        if (inClauseContent.Trim().ToUpper().StartsWith("SELECT"))
        {
            constraints.InClauseConstraints.Add(new InClauseConstraintInfo
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                InClauseType = "SUBQUERY",
                SubQuery = inClauseContent.Trim(),
                Values = new List<string>()
            });

            _logger.Information("Found IN subquery: {Table}.{Column} IN ({SubQuery})", tableAlias, columnName, inClauseContent.Trim());
            return;
        }

        // Parse value list: 'value1', 'value2', value3, ...
        var values = new List<string>();
        var valuePattern = @"'([^']*)'|""([^""]*)""|([^,\s]+)";
        var valueMatches = Regex.Matches(inClauseContent, valuePattern);

        foreach (Match valueMatch in valueMatches)
        {
            var value = valueMatch.Groups[1].Value; // Single quoted
            if (string.IsNullOrEmpty(value))
                value = valueMatch.Groups[2].Value; // Double quoted
            if (string.IsNullOrEmpty(value))
                value = valueMatch.Groups[3].Value; // Unquoted

            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value.Trim());
        }

        if (values.Count > 0)
        {
            // Determine if values are numeric or string
            var inClauseType = values.All(v => decimal.TryParse(v, out _)) ? "NUMERIC_LIST" : "STRING_LIST";

            constraints.InClauseConstraints.Add(new InClauseConstraintInfo
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                InClauseType = inClauseType,
                Values = values,
                SubQuery = ""
            });

            _logger.Information("Found IN clause: {Table}.{Column} IN ({Values}) - Type: {Type}", 
                tableAlias, columnName, string.Join(", ", values), inClauseType);
        }
    }

    /// <summary>
    /// Extract BETWEEN constraints như BETWEEN value1 AND value2
    /// </summary>
    private void ExtractBetweenConstraints(string cleanQuery, ComprehensiveConstraints constraints)
    {
        // BETWEEN với table alias: table.column BETWEEN value1 AND value2
        var betweenPatternWithAlias = @"(\w+)\.(\w+)\s+BETWEEN\s+(?:'([^']+)'|""([^""]+)""|([^\s]+))\s+AND\s+(?:'([^']+)'|""([^""]+)""|([^\s]+))";
        var aliasMatches = Regex.Matches(cleanQuery, betweenPatternWithAlias, RegexOptions.IgnoreCase);

        foreach (Match match in aliasMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            
            // Extract min value (single quote, double quote, or unquoted)
            var minValue = match.Groups[3].Value;  // Single quoted
            if (string.IsNullOrEmpty(minValue))
                minValue = match.Groups[4].Value;  // Double quoted
            if (string.IsNullOrEmpty(minValue))
                minValue = match.Groups[5].Value;  // Unquoted
                
            // Extract max value (single quote, double quote, or unquoted)
            var maxValue = match.Groups[6].Value;  // Single quoted
            if (string.IsNullOrEmpty(maxValue))
                maxValue = match.Groups[7].Value;  // Double quoted
            if (string.IsNullOrEmpty(maxValue))
                maxValue = match.Groups[8].Value;  // Unquoted

            if (!string.IsNullOrEmpty(minValue) && !string.IsNullOrEmpty(maxValue))
            {
                var dataType = DetermineBetweenDataType(minValue, maxValue);

                constraints.BetweenConstraints.Add(new BetweenConstraintInfo
                {
                    TableAlias = tableAlias,
                    ColumnName = columnName,
                    MinValue = minValue,
                    MaxValue = maxValue,
                    DataType = dataType
                });

                _logger.Information("Found BETWEEN constraint: {Table}.{Column} BETWEEN '{Min}' AND '{Max}' - Type: {Type}", 
                    tableAlias, columnName, minValue, maxValue, dataType);
            }
        }

        // BETWEEN without table alias: column BETWEEN value1 AND value2
        var betweenPatternWithoutAlias = @"\b(\w+)\s+BETWEEN\s+(?:'([^']+)'|""([^""]+)""|([^\s]+))\s+AND\s+(?:'([^']+)'|""([^""]+)""|([^\s]+))";
        var noAliasMatches = Regex.Matches(cleanQuery, betweenPatternWithoutAlias, RegexOptions.IgnoreCase);

        foreach (Match match in noAliasMatches)
        {
            var columnName = match.Groups[1].Value;
            
            // Skip if already captured with alias or if column is a reserved word
            if (aliasMatches.Cast<Match>().Any(m => m.Groups[2].Value == columnName) || IsReservedWord(columnName))
                continue;
            
            // Extract min value (single quote, double quote, or unquoted)
            var minValue = match.Groups[2].Value;  // Single quoted
            if (string.IsNullOrEmpty(minValue))
                minValue = match.Groups[3].Value;  // Double quoted
            if (string.IsNullOrEmpty(minValue))
                minValue = match.Groups[4].Value;  // Unquoted
                
            // Extract max value (single quote, double quote, or unquoted)
            var maxValue = match.Groups[5].Value;  // Single quoted
            if (string.IsNullOrEmpty(maxValue))
                maxValue = match.Groups[6].Value;  // Double quoted
            if (string.IsNullOrEmpty(maxValue))
                maxValue = match.Groups[7].Value;  // Unquoted

            if (!string.IsNullOrEmpty(minValue) && !string.IsNullOrEmpty(maxValue))
            {
                var dataType = DetermineBetweenDataType(minValue, maxValue);

                constraints.BetweenConstraints.Add(new BetweenConstraintInfo
                {
                    TableAlias = "",
                    ColumnName = columnName,
                    MinValue = minValue,
                    MaxValue = maxValue,
                    DataType = dataType
                });

                _logger.Information("Found BETWEEN constraint without alias: {Column} BETWEEN '{Min}' AND '{Max}' - Type: {Type}", 
                    columnName, minValue, maxValue, dataType);
            }
        }
    }

    /// <summary>
    /// Extract NULL constraints như IS NULL và IS NOT NULL
    /// </summary>
    private void ExtractNullConstraints(string cleanQuery, ComprehensiveConstraints constraints)
    {
        // IS NULL với table alias: table.column IS NULL
        var isNullPatternWithAlias = @"(\w+)\.(\w+)\s+IS\s+NULL\b";
        var nullAliasMatches = Regex.Matches(cleanQuery, isNullPatternWithAlias, RegexOptions.IgnoreCase);

        foreach (Match match in nullAliasMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;

            constraints.NullConstraints.Add(new NullConstraintInfo
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                IsNull = true,
                NullCheckType = "IS_NULL"
            });

            _logger.Information("Found NULL constraint: {Table}.{Column} IS NULL", tableAlias, columnName);
        }

        // IS NOT NULL với table alias: table.column IS NOT NULL
        var isNotNullPatternWithAlias = @"(\w+)\.(\w+)\s+IS\s+NOT\s+NULL\b";
        var notNullAliasMatches = Regex.Matches(cleanQuery, isNotNullPatternWithAlias, RegexOptions.IgnoreCase);

        foreach (Match match in notNullAliasMatches)
        {
            var tableAlias = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;

            constraints.NullConstraints.Add(new NullConstraintInfo
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                IsNull = false,
                NullCheckType = "IS_NOT_NULL"
            });

            _logger.Information("Found NOT NULL constraint: {Table}.{Column} IS NOT NULL", tableAlias, columnName);
        }

        // IS NULL without table alias: column IS NULL
        var isNullPatternWithoutAlias = @"\b(\w+)\s+IS\s+NULL\b";
        var nullNoAliasMatches = Regex.Matches(cleanQuery, isNullPatternWithoutAlias, RegexOptions.IgnoreCase);

        foreach (Match match in nullNoAliasMatches)
        {
            var columnName = match.Groups[1].Value;

            // Skip if already captured with alias or if column is a reserved word
            if (nullAliasMatches.Cast<Match>().Any(m => m.Groups[2].Value == columnName) || IsReservedWord(columnName))
                continue;

            constraints.NullConstraints.Add(new NullConstraintInfo
            {
                TableAlias = "",
                ColumnName = columnName,
                IsNull = true,
                NullCheckType = "IS_NULL"
            });

            _logger.Information("Found NULL constraint without alias: {Column} IS NULL", columnName);
        }

        // IS NOT NULL without table alias: column IS NOT NULL
        var isNotNullPatternWithoutAlias = @"\b(\w+)\s+IS\s+NOT\s+NULL\b";
        var notNullNoAliasMatches = Regex.Matches(cleanQuery, isNotNullPatternWithoutAlias, RegexOptions.IgnoreCase);

        foreach (Match match in notNullNoAliasMatches)
        {
            var columnName = match.Groups[1].Value;

            // Skip if already captured with alias or if column is a reserved word
            if (notNullAliasMatches.Cast<Match>().Any(m => m.Groups[2].Value == columnName) || IsReservedWord(columnName))
                continue;

            constraints.NullConstraints.Add(new NullConstraintInfo
            {
                TableAlias = "",
                ColumnName = columnName,
                IsNull = false,
                NullCheckType = "IS_NOT_NULL"
            });

            _logger.Information("Found NOT NULL constraint without alias: {Column} IS NOT NULL", columnName);
        }
    }

    /// <summary>
    /// Extract EXISTS constraints như EXISTS (subquery) và NOT EXISTS (subquery)
    /// Enhanced with two-phase processing to avoid regex conflicts
    /// </summary>
    private void ExtractExistsConstraints(string cleanQuery, ComprehensiveConstraints constraints)
    {
        _logger.Information("Starting EXISTS constraints extraction with two-phase approach");

        // Phase 1: Extract NOT EXISTS patterns first
        var notExistsPattern = @"\bNOT\s+EXISTS\s*\(\s*([^()]+(?:\([^()]*\)[^()]*)*)\s*\)";
        var notExistsMatches = Regex.Matches(cleanQuery, notExistsPattern, RegexOptions.IgnoreCase);

        // Store NOT EXISTS info and create modified query
        var modifiedQuery = cleanQuery;
        var placeholderCounter = 0;

        foreach (Match match in notExistsMatches)
        {
            var subQuery = match.Groups[1].Value.Trim();

            constraints.ExistsConstraints.Add(new ExistsConstraintInfo
            {
                TableAlias = "", // NOT EXISTS doesn't directly relate to a specific table alias
                ColumnName = "", // NOT EXISTS doesn't directly relate to a specific column
                IsExists = false,
                SubQuery = subQuery,
                ExistsType = "NOT_EXISTS"
            });

            _logger.Information("Found NOT EXISTS constraint: NOT EXISTS ({SubQuery})", subQuery);

            // Replace NOT EXISTS với placeholder để tránh conflict với EXISTS pattern
            var placeholder = $"__NOT_EXISTS_PLACEHOLDER_{placeholderCounter++}__";
            modifiedQuery = modifiedQuery.Replace(match.Value, placeholder);
        }

        // Phase 2: Extract pure EXISTS patterns từ modified query (without NOT EXISTS)
        var existsPattern = @"\bEXISTS\s*\(\s*([^()]+(?:\([^()]*\)[^()]*)*)\s*\)";
        var existsMatches = Regex.Matches(modifiedQuery, existsPattern, RegexOptions.IgnoreCase);

        foreach (Match match in existsMatches)
        {
            var subQuery = match.Groups[1].Value.Trim();

            // Skip nếu match placeholder (safety check)
            if (subQuery.Contains("__NOT_EXISTS_PLACEHOLDER_"))
            {
                _logger.Information("Skipping placeholder match in EXISTS extraction");
                continue;
            }

            constraints.ExistsConstraints.Add(new ExistsConstraintInfo
            {
                TableAlias = "", // EXISTS doesn't directly relate to a specific table alias
                ColumnName = "", // EXISTS doesn't directly relate to a specific column
                IsExists = true,
                SubQuery = subQuery,
                ExistsType = "EXISTS"
            });

            _logger.Information("Found EXISTS constraint: EXISTS ({SubQuery})", subQuery);
        }

        _logger.Information("Completed EXISTS constraints extraction: {NotExistsCount} NOT EXISTS, {ExistsCount} EXISTS", 
            notExistsMatches.Count, existsMatches.Count);
    }

    #region Helper Methods

    private string CleanSqlQuery(string sqlQuery)
    {
        // Remove SQL comments
        var withoutLineComments = Regex.Replace(sqlQuery, @"--[^\r\n]*", "", RegexOptions.Multiline);
        var withoutBlockComments = Regex.Replace(withoutLineComments, @"/\*[\s\S]*?\*/", "", RegexOptions.Multiline);
        
        // Normalize whitespace
        var normalized = Regex.Replace(withoutBlockComments, @"\s+", " ");
        
        return normalized.Trim();
    }

    private static bool IsReservedWord(string value)
    {
        var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "NOW", "NULL", "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS"
        };
        return reservedWords.Contains(value);
    }

    private static string DetermineLikePatternType(string pattern)
    {
        if (pattern.StartsWith("%") && pattern.EndsWith("%"))
            return "CONTAINS";
        if (pattern.StartsWith("%"))
            return "ENDS_WITH";
        if (pattern.EndsWith("%"))
            return "STARTS_WITH";
        return "EXACT";
    }

    private static bool ParseBooleanValue(string value)
    {
        return value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    private static string DetermineBetweenDataType(string minValue, string maxValue)
    {
        // Check if both values are numeric
        if (decimal.TryParse(minValue, out _) && decimal.TryParse(maxValue, out _))
            return "NUMERIC";
            
        // Check if both values are dates
        if (DateTime.TryParse(minValue, out _) && DateTime.TryParse(maxValue, out _))
            return "DATE";
            
        // Default to string
        return "STRING";
    }

    #endregion
} 