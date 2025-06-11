# SQL Parser Libraries Comparison

## 1. Microsoft.SqlServer.TransactSql.ScriptDom

### ✅ Pros
- **Official Microsoft library** - robust và well-tested
- **Production-ready** - được sử dụng trong SQL Tools, SSDT, SqlPackage
- **Full T-SQL support** - Complete SQL Server và Azure SQL compatibility  
- **Free & Open Source** - Available on NuGet
- **AST (Abstract Syntax Tree)** - Full parsing with rich object model
- **Visitor Pattern** - Easy to traverse và analyze SQL
- **Error Handling** - Professional error reporting
- **Mature & Stable** - Years of development và testing

### ❌ Cons  
- **T-SQL only** - Không support MySQL/PostgreSQL natively
- **Large library** - Big dependency size
- **Complex API** - Learning curve for advanced features

### Use Case
Perfect cho T-SQL parsing trong enterprise applications

## 2. ANTLR4 với SQL Grammar

### ✅ Pros
- **Multi-dialect support** - MySQL, PostgreSQL, Oracle, etc.
- **Flexible** - Can customize grammar cho specific needs
- **Cross-language** - Generate parsers for multiple languages
- **Community grammars** - Ready-made SQL grammars available
- **Visual tools** - Good debugging và visualization

### ❌ Cons
- **Complex setup** - Requires grammar generation step
- **Performance overhead** - Generally slower than native parsers
- **Grammar maintenance** - Need to keep grammar updated
- **Learning curve** - ANTLR expertise required

### Use Case  
Good cho multi-database support hoặc custom SQL dialects

## 3. Decision Matrix

| Criteria | ScriptDom | ANTLR4 | Weight |
|----------|-----------|---------|---------|
| **Ease of Use** | 9/10 | 6/10 | 25% |
| **Performance** | 9/10 | 7/10 | 20% |
| **SQL Server Support** | 10/10 | 8/10 | 30% |
| **MySQL Support** | 6/10 | 9/10 | 15% |
| **Maintenance** | 9/10 | 6/10 | 10% |

**Total Score:**
- **ScriptDom**: 8.75/10
- **ANTLR4**: 7.25/10

## 🎯 Recommendation: Microsoft.SqlServer.TransactSql.ScriptDom

### Reasons:
1. **Project Context**: Chủ yếu work với SQL Server/MySQL
2. **Stability**: Production-ready, enterprise-grade
3. **Easy Integration**: Simple NuGet package
4. **Rich Features**: Complete AST với visitor pattern
5. **Microsoft Support**: Official library với good documentation

### Implementation Plan:
1. Add Microsoft.SqlServer.TransactSql.ScriptDom NuGet package
2. Create SqlQueryParserV2 class using ScriptDom
3. Implement visitor pattern để extract constraints
4. Test với complex queries như TC001
5. Performance comparison với regex approach 