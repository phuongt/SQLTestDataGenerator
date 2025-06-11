namespace SqlTestDataGenerator.Core.Models;

/// <summary>
/// Comprehensive constraints extracted từ SQL query để truyền cho AI generation
/// </summary>
public class ComprehensiveConstraints
{
    public string OriginalQuery { get; set; } = "";
    public List<GeneralConstraintInfo> WhereConstraints { get; set; } = new();
    public List<JoinConstraintInfo> JoinConstraints { get; set; } = new();
    public List<LikePatternInfo> LikePatterns { get; set; } = new();
    public List<DateConstraintInfo> DateConstraints { get; set; } = new();
    public List<BooleanConstraintInfo> BooleanConstraints { get; set; } = new();
    public List<InClauseConstraintInfo> InClauseConstraints { get; set; } = new();
    public List<BetweenConstraintInfo> BetweenConstraints { get; set; } = new();
    public List<NullConstraintInfo> NullConstraints { get; set; } = new();
    public List<ExistsConstraintInfo> ExistsConstraints { get; set; } = new();

    public int GetTotalCount() => WhereConstraints.Count + JoinConstraints.Count + 
                                  LikePatterns.Count + DateConstraints.Count + BooleanConstraints.Count +
                                  InClauseConstraints.Count + BetweenConstraints.Count + NullConstraints.Count +
                                  ExistsConstraints.Count;

    /// <summary>
    /// Get constraints by table alias
    /// </summary>
    public List<IConstraint> GetConstraintsForTable(string tableAlias)
    {
        var constraints = new List<IConstraint>();
        
        constraints.AddRange(WhereConstraints.Where(c => c.TableAlias == tableAlias));
        constraints.AddRange(JoinConstraints.Where(c => c.LeftTableAlias == tableAlias));
        constraints.AddRange(LikePatterns.Where(c => c.TableAlias == tableAlias));
        constraints.AddRange(DateConstraints.Where(c => c.TableAlias == tableAlias));
        constraints.AddRange(BooleanConstraints.Where(c => c.TableAlias == tableAlias));
        constraints.AddRange(InClauseConstraints.Where(c => c.TableAlias == tableAlias));
        constraints.AddRange(BetweenConstraints.Where(c => c.TableAlias == tableAlias));
        constraints.AddRange(NullConstraints.Where(c => c.TableAlias == tableAlias));
        constraints.AddRange(ExistsConstraints.Where(c => c.TableAlias == tableAlias));
        
        return constraints;
    }

    /// <summary>
    /// Get constraints by table alias và column name
    /// </summary>
    public List<IConstraint> GetConstraintsForColumn(string tableAlias, string columnName)
    {
        return GetConstraintsForTable(tableAlias)
            .Where(c => c.ColumnName == columnName)
            .ToList();
    }
}

/// <summary>
/// Base interface cho all constraint types
/// </summary>
public interface IConstraint
{
    string TableAlias { get; set; }
    string ColumnName { get; set; }
    string Source { get; set; }
}

/// <summary>
/// General constraint info từ WHERE/JOIN conditions
/// </summary>
public class GeneralConstraintInfo : IConstraint
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string Operator { get; set; } = "";
    public string Value { get; set; } = "";
    public string Source { get; set; } = "";
}

/// <summary>
/// JOIN constraint info including additional conditions trong ON clause
/// </summary>
public class JoinConstraintInfo : IConstraint
{
    public string LeftTableAlias { get; set; } = "";
    public string LeftColumn { get; set; } = "";
    public string RightTableAlias { get; set; } = "";
    public string RightColumn { get; set; } = "";
    public string RightTable { get; set; } = "";
    public string Operator { get; set; } = "";
    public string Value { get; set; } = "";
    public string Source { get; set; } = "JOIN";

    // IConstraint implementation
    public string TableAlias { get => LeftTableAlias; set => LeftTableAlias = value; }
    public string ColumnName { get => LeftColumn; set => LeftColumn = value; }
}

/// <summary>
/// LIKE pattern constraint để ensure AI generates matching values
/// </summary>
public class LikePatternInfo : IConstraint
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string Pattern { get; set; } = "";        // '%VNEXT%'
    public string RequiredValue { get; set; } = "";  // 'VNEXT'
    public string PatternType { get; set; } = "";    // CONTAINS, STARTS_WITH, ENDS_WITH, EXACT
    public string Source { get; set; } = "LIKE";
}

/// <summary>
/// Date constraint info từ YEAR() functions và DATE_ADD
/// </summary>
public class DateConstraintInfo : IConstraint
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string ConstraintType { get; set; } = "";  // YEAR_EQUALS, DATE_INTERVAL
    public string Operator { get; set; } = "";
    public string Value { get; set; } = "";
    public string Source { get; set; } = "DATE";
}

/// <summary>
/// Boolean constraint info để handle TRUE/FALSE/0/1 values properly
/// </summary>
public class BooleanConstraintInfo : IConstraint
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string Value { get; set; } = "";        // TRUE, FALSE, 0, 1
    public bool BooleanValue { get; set; }         // Parsed boolean value
    public string Source { get; set; } = "BOOLEAN";
}

/// <summary>
/// Constraint validation result với detailed violation info
/// </summary>
public class ConstraintValidationResult
{
    public bool IsValid { get; set; }
    public List<ConstraintViolation> Violations { get; set; } = new();
    public string Summary { get; set; } = "";
    public int TotalConstraints { get; set; }
    public int ValidatedConstraints { get; set; }
    public int ViolatedConstraints { get; set; }
    
    public double CompliancePercentage => TotalConstraints > 0 
        ? (double)ValidatedConstraints / TotalConstraints * 100 
        : 0;
}

/// <summary>
/// Individual constraint violation info
/// </summary>
public class ConstraintViolation
{
    public string ConstraintType { get; set; } = "";
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string ExpectedValue { get; set; } = "";
    public string ActualValue { get; set; } = "";
    public string ViolationMessage { get; set; } = "";
    public string Severity { get; set; } = "Medium";  // Low, Medium, High, Critical
}

/// <summary>
/// IN clause constraint info để handle IN ('value1', 'value2', ...) patterns
/// </summary>
public class InClauseConstraintInfo : IConstraint
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public List<string> Values { get; set; } = new();  // List of values trong IN clause
    public string InClauseType { get; set; } = "";     // STRING_LIST, NUMERIC_LIST, SUBQUERY
    public string SubQuery { get; set; } = "";         // If IN contains subquery
    public string Source { get; set; } = "IN_CLAUSE";
}

/// <summary>
/// BETWEEN constraint info để handle BETWEEN value1 AND value2 patterns
/// </summary>
public class BetweenConstraintInfo : IConstraint
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string MinValue { get; set; } = "";         // Lower bound value
    public string MaxValue { get; set; } = "";         // Upper bound value
    public string DataType { get; set; } = "";         // NUMERIC, DATE, STRING
    public string Source { get; set; } = "BETWEEN";
}

/// <summary>
/// NULL constraint info để handle IS NULL và IS NOT NULL patterns
/// </summary>
public class NullConstraintInfo : IConstraint
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public bool IsNull { get; set; }                   // true for IS NULL, false for IS NOT NULL
    public string NullCheckType { get; set; } = "";    // IS_NULL, IS_NOT_NULL
    public string Source { get; set; } = "NULL_CHECK";
}

/// <summary>
/// EXISTS constraint info để handle EXISTS và NOT EXISTS subqueries
/// </summary>
public class ExistsConstraintInfo : IConstraint
{
    public string TableAlias { get; set; } = "";
    public string ColumnName { get; set; } = "";       // Main table column if applicable
    public bool IsExists { get; set; }                 // true for EXISTS, false for NOT EXISTS
    public string SubQuery { get; set; } = "";         // The subquery inside EXISTS()
    public string ExistsType { get; set; } = "";       // EXISTS, NOT_EXISTS
    public string Source { get; set; } = "EXISTS";
} 