using System.Text.RegularExpressions;
using SqlTestDataGenerator.Core.Models;
using Serilog;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.IO;
using System.Linq;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Advanced SQL query parser using Microsoft.SqlServer.TransactSql.ScriptDom
/// Provides robust SQL parsing with full AST support for constraint resolution
/// </summary>
public class SqlQueryParserV2
{
    private readonly ILogger _logger;

    public SqlQueryParserV2()
    {
        _logger = Log.ForContext<SqlQueryParserV2>();
    }

    /// <summary>
    /// Parse SQL query to extract data generation requirements using ScriptDom
    /// </summary>
    public SqlDataRequirements ParseQuery(string sqlQuery)
    {
        _logger.Information("SqlQueryParserV2: Parsing SQL query with ScriptDom engine");
        
        var requirements = new SqlDataRequirements
        {
            OriginalQuery = sqlQuery
        };

        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            return requirements;
        }

        try
        {
            // Create ScriptDom parser
            var parser = new TSql160Parser(true); // SQL Server 2022, case-insensitive
            IList<ParseError> parseErrors;
            
            using var reader = new StringReader(sqlQuery);
            var fragment = parser.Parse(reader, out parseErrors);

            if (parseErrors.Any())
            {
                _logger.Warning("ScriptDom parse errors found: {ErrorCount}", parseErrors.Count);
                foreach (var error in parseErrors)
                {
                    _logger.Warning("Parse error at line {Line}, column {Column}: {Message}", 
                        error.Line, error.Column, error.Message);
                }
                
                // If there are significant parsing errors, fallback to regex parser immediately
                // Enhanced detection for MySQL-specific syntax
                if (parseErrors.Any(e => e.Message.Contains("Incorrect syntax") || 
                                       e.Message.Contains("INTERVAL") ||
                                       e.Message.Contains("DATE_ADD") ||
                                       e.Message.Contains("NOW()") ||
                                       sqlQuery.Contains("DATE_ADD") ||
                                       sqlQuery.Contains("INTERVAL")))
                {
                    _logger.Information("Falling back to regex parser due to MySQL syntax incompatibility");
                    var fallbackParser = new SqlQueryParser();
                    return fallbackParser.ParseQuery(sqlQuery);
                }
            }

            // Create visitor to analyze the parsed SQL
            var visitor = new SqlConstraintVisitor(requirements, _logger);
            fragment.Accept(visitor);

            _logger.Information("ScriptDom extracted {WhereCount} WHERE conditions and {JoinCount} JOIN requirements", 
                requirements.WhereConditions.Count, requirements.JoinRequirements.Count);

            return requirements;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "ScriptDom parsing failed, falling back to regex parser");
            
            // Fallback to regex parser if ScriptDom fails
            try
            {
                var fallbackParser = new SqlQueryParser();
                return fallbackParser.ParseQuery(sqlQuery);
            }
            catch (Exception fallbackEx)
            {
                _logger.Warning(fallbackEx, "Fallback regex parser also failed, returning minimal requirements");
                
                // If even fallback fails, return basic requirements with original query
                // This ensures we don't crash on invalid SQL
                return new SqlDataRequirements
                {
                    OriginalQuery = sqlQuery,
                    TableRequirements = new Dictionary<string, TableDataRequirement>(),
                    WhereConditions = new List<WhereCondition>(),
                    JoinRequirements = new List<JoinRequirement>()
                };
            }
        }
    }
}

/// <summary>
/// Visitor class to traverse ScriptDom AST and extract constraint information
/// </summary>
public class SqlConstraintVisitor : TSqlFragmentVisitor
{
    private readonly SqlDataRequirements _requirements;
    private readonly ILogger _logger;
    private readonly Dictionary<string, string> _tableAliases = new();
    
    public SqlConstraintVisitor(SqlDataRequirements requirements, ILogger logger)
    {
        _requirements = requirements;
        _logger = logger;
    }

    public override void ExplicitVisit(SelectStatement selectStatement)
    {
        _logger.Debug("Visiting SelectStatement");
        
        // Extract table information from SELECT
        if (selectStatement.QueryExpression is QuerySpecification querySpec)
        {
            ExtractTableRequirements(querySpec.FromClause);
            ExtractWhereConditions(querySpec.WhereClause);
        }
        
        base.ExplicitVisit(selectStatement);
    }

    public override void ExplicitVisit(QualifiedJoin qualifiedJoin)
    {
        _logger.Debug("Visiting QualifiedJoin: {JoinType}", qualifiedJoin.QualifiedJoinType);
        
        ExtractJoinRequirements(qualifiedJoin);
        
        base.ExplicitVisit(qualifiedJoin);
    }

    private void ExtractTableRequirements(FromClause fromClause)
    {
        if (fromClause?.TableReferences == null) return;

        foreach (var tableRef in fromClause.TableReferences)
        {
            ExtractTableFromReference(tableRef);
        }
    }

    private void ExtractTableFromReference(TableReference tableRef)
    {
        switch (tableRef)
        {
            case NamedTableReference namedTable:
                var tableName = GetTableName(namedTable.SchemaObject);
                var alias = namedTable.Alias?.Value ?? tableName;
                
                _tableAliases[alias] = tableName;
                
                if (!_requirements.TableRequirements.ContainsKey(tableName))
                {
                    _requirements.TableRequirements[tableName] = new TableDataRequirement
                    {
                        TableName = tableName
                    };
                }
                
                _logger.Debug("Found table: {TableName} with alias: {Alias}", tableName, alias);
                break;
                
            case QualifiedJoin joinTable:
                ExtractTableFromReference(joinTable.FirstTableReference);
                ExtractTableFromReference(joinTable.SecondTableReference);
                break;
        }
    }

    private void ExtractWhereConditions(WhereClause whereClause)
    {
        if (whereClause?.SearchCondition == null) return;

        _logger.Debug("Extracting WHERE conditions from clause");
        AnalyzeSearchCondition(whereClause.SearchCondition);
    }

    private void AnalyzeSearchCondition(BooleanExpression condition)
    {
        switch (condition)
        {
            case BooleanBinaryExpression binaryExpr:
                // Handle AND/OR expressions
                AnalyzeSearchCondition(binaryExpr.FirstExpression);
                AnalyzeSearchCondition(binaryExpr.SecondExpression);
                break;
                
            case BooleanComparisonExpression compExpr:
                ExtractComparisonCondition(compExpr);
                break;
                
            case LikePredicate likeExpr:
                ExtractLikeCondition(likeExpr);
                break;
                
            case InPredicate inExpr:
                ExtractInCondition(inExpr);
                break;
                
            case BooleanIsNullExpression nullExpr:
                ExtractNullCondition(nullExpr);
                break;
        }
    }

    private void ExtractComparisonCondition(BooleanComparisonExpression compExpr)
    {
        var leftColumn = ExtractColumnInfo(compExpr.FirstExpression);
        var rightValue = ExtractValueInfo(compExpr.SecondExpression);
        
        if (leftColumn != null && rightValue != null)
        {
            var condition = new WhereCondition
            {
                TableAlias = leftColumn.Value.TableAlias,
                ColumnName = leftColumn.Value.ColumnName,
                Operator = GetOperatorString(compExpr.ComparisonType),
                Value = rightValue,
                OriginalCondition = compExpr.ScriptTokenStream?
                    .Skip(compExpr.FirstTokenIndex)
                    .Take(compExpr.LastTokenIndex - compExpr.FirstTokenIndex + 1)
                    .Select(t => t.Text)
                    .Aggregate((a, b) => a + b) ?? ""
            };
            
            _requirements.WhereConditions.Add(condition);
            
            _logger.Debug("Found comparison condition: {TableAlias}.{ColumnName} {Operator} {Value}", 
                condition.TableAlias, condition.ColumnName, condition.Operator, condition.Value);
        }
    }

    private void ExtractLikeCondition(LikePredicate likeExpr)
    {
        var leftColumn = ExtractColumnInfo(likeExpr.FirstExpression);
        var rightValue = ExtractValueInfo(likeExpr.SecondExpression);
        
        if (leftColumn != null && rightValue != null)
        {
            // Remove % wildcards from LIKE values
            var cleanValue = rightValue.Replace("%", "").Trim();
            
            var condition = new WhereCondition
            {
                TableAlias = leftColumn.Value.TableAlias,
                ColumnName = leftColumn.Value.ColumnName,
                Operator = "LIKE",
                Value = cleanValue,
                OriginalCondition = "LIKE condition from ScriptDom"
            };
            
            _requirements.WhereConditions.Add(condition);
            
            _logger.Debug("Found LIKE condition: {TableAlias}.{ColumnName} LIKE {Value}", 
                condition.TableAlias, condition.ColumnName, cleanValue);
        }
    }

    private void ExtractInCondition(InPredicate inExpr)
    {
        var column = ExtractColumnInfo(inExpr.Expression);
        
        if (column != null && inExpr.Values != null)
        {
            var values = new List<string>();
            foreach (var value in inExpr.Values)
            {
                var val = ExtractValueInfo(value);
                if (val != null) values.Add(val);
            }
            
            if (values.Any())
            {
                var condition = new WhereCondition
                {
                    TableAlias = column.Value.TableAlias,
                    ColumnName = column.Value.ColumnName,
                    Operator = "IN",
                    Value = string.Join(",", values),
                    OriginalCondition = "IN condition from ScriptDom"
                };
                
                _requirements.WhereConditions.Add(condition);
                
                _logger.Debug("Found IN condition: {TableAlias}.{ColumnName} IN ({Values})", 
                    condition.TableAlias, condition.ColumnName, string.Join(", ", values));
            }
        }
    }

    private void ExtractNullCondition(BooleanIsNullExpression nullExpr)
    {
        var column = ExtractColumnInfo(nullExpr.Expression);
        
        if (column != null)
        {
            var condition = new WhereCondition
            {
                TableAlias = column.Value.TableAlias,
                ColumnName = column.Value.ColumnName,
                Operator = nullExpr.IsNot ? "IS_NOT_NULL" : "IS_NULL",
                Value = "",
                OriginalCondition = "NULL condition from ScriptDom"
            };
            
            _requirements.WhereConditions.Add(condition);
            
            _logger.Debug("Found NULL condition: {TableAlias}.{ColumnName} IS {NotNull}NULL", 
                condition.TableAlias, condition.ColumnName, nullExpr.IsNot ? "NOT " : "");
        }
    }

    private void ExtractJoinRequirements(QualifiedJoin joinExpr)
    {
        _logger.Debug("Processing JOIN SearchCondition type: {Type}", joinExpr.SearchCondition?.GetType().Name);
        
        // First analyze the search condition to find all components
        if (joinExpr.SearchCondition != null)
        {
            AnalyzeJoinSearchCondition(joinExpr.SearchCondition);
        }
    }
    
    private void AnalyzeJoinSearchCondition(BooleanExpression condition)
    {
        switch (condition)
        {
            case BooleanBinaryExpression binaryExpr when binaryExpr.BinaryExpressionType == BooleanBinaryExpressionType.And:
                _logger.Debug("Found AND expression in JOIN - analyzing both sides");
                // Handle AND expressions - analyze both sides
                AnalyzeJoinSearchCondition(binaryExpr.FirstExpression);
                AnalyzeJoinSearchCondition(binaryExpr.SecondExpression);
                break;
                
            case BooleanComparisonExpression compExpr:
                _logger.Debug("Found comparison in JOIN: {CompType}", compExpr.ComparisonType);
                var leftColumn = ExtractColumnInfo(compExpr.FirstExpression);
                var rightInfo = ExtractColumnInfo(compExpr.SecondExpression) ?? 
                               (ExtractValueInfo(compExpr.SecondExpression) != null ? 
                                   (TableAlias: "", ColumnName: ExtractValueInfo(compExpr.SecondExpression)!) : 
                                   null);
                
                if (leftColumn != null && rightInfo != null)
                {
                    // Check if this is a join condition (column = column) or additional condition (column = value)
                    if (rightInfo.Value.TableAlias != "" && rightInfo.Value.ColumnName != null && !IsLiteralValue(rightInfo.Value.ColumnName))
                    {
                        // This is a JOIN condition: table1.col = table2.col
                        var joinReq = new JoinRequirement
                        {
                            LeftTableAlias = leftColumn.Value.TableAlias,
                            LeftColumn = leftColumn.Value.ColumnName,
                            RightTableAlias = rightInfo.Value.TableAlias,
                            RightColumn = rightInfo.Value.ColumnName,
                            RightTable = _tableAliases.GetValueOrDefault(rightInfo.Value.TableAlias, rightInfo.Value.TableAlias)
                        };
                        
                        _requirements.JoinRequirements.Add(joinReq);
                        
                        _logger.Debug("Extracted JOIN condition: {LeftAlias}.{LeftCol} = {RightAlias}.{RightCol}",
                            joinReq.LeftTableAlias, joinReq.LeftColumn, joinReq.RightTableAlias, joinReq.RightColumn);
                    }
                    else
                    {
                        // This is an additional WHERE condition in JOIN: table.col = value
                        var whereCondition = new WhereCondition
                        {
                            TableAlias = leftColumn.Value.TableAlias,
                            ColumnName = leftColumn.Value.ColumnName,
                            Operator = GetOperatorString(compExpr.ComparisonType),
                            Value = rightInfo.Value.ColumnName, // This is actually the value
                            OriginalCondition = "JOIN additional condition"
                        };
                        
                        _requirements.WhereConditions.Add(whereCondition);
                        
                        _logger.Debug("Extracted JOIN additional condition: {TableAlias}.{ColumnName} {Operator} {Value}", 
                            whereCondition.TableAlias, whereCondition.ColumnName, whereCondition.Operator, whereCondition.Value);
                    }
                }
                break;
                
            default:
                _logger.Debug("Unhandled JOIN condition type: {Type}", condition?.GetType().Name);
                break;
        }
    }
    
    private bool IsLiteralValue(string value)
    {
        // Check if this looks like a literal value rather than a column name
        return value == "TRUE" || value == "FALSE" || value == "NULL" ||
               value.All(char.IsDigit) ||
               (value.StartsWith("'") && value.EndsWith("'")) ||
               (value.StartsWith("\"") && value.EndsWith("\""));
    }

    private (string TableAlias, string ColumnName)? ExtractColumnInfo(ScalarExpression expression)
    {
        if (expression is ColumnReferenceExpression columnRef)
        {
            var parts = columnRef.MultiPartIdentifier.Identifiers;
            
            if (parts.Count == 2)
            {
                return (parts[0].Value, parts[1].Value);
            }
            else if (parts.Count == 1)
            {
                return ("", parts[0].Value);
            }
        }
        
        return null;
    }

    private string? ExtractValueInfo(ScalarExpression expression)
    {
        switch (expression)
        {
            case StringLiteral stringLit:
                return stringLit.Value;
                
            case IntegerLiteral intLit:
                return intLit.Value;
                
            case NumericLiteral numLit:
                return numLit.Value;
                
            case NullLiteral:
                return "NULL";
                
            case VariableReference variable:
                return variable.Name;
                
            default:
                return null;
        }
    }

    private string GetOperatorString(BooleanComparisonType comparisonType)
    {
        return comparisonType switch
        {
            BooleanComparisonType.Equals => "=",
            BooleanComparisonType.GreaterThan => ">",
            BooleanComparisonType.GreaterThanOrEqualTo => ">=",
            BooleanComparisonType.LessThan => "<",
            BooleanComparisonType.LessThanOrEqualTo => "<=",
            BooleanComparisonType.NotEqualToBrackets => "!=",
            BooleanComparisonType.NotEqualToExclamation => "<>",
            _ => "="
        };
    }

    private string GetTableName(SchemaObjectName schemaObject)
    {
        var parts = schemaObject.Identifiers;
        return parts.Count switch
        {
            1 => parts[0].Value,
            2 => $"{parts[0].Value}.{parts[1].Value}",
            3 => $"{parts[0].Value}.{parts[1].Value}.{parts[2].Value}",
            _ => string.Join(".", parts.Select(p => p.Value))
        };
    }
} 