# T028: Implement SQL Parser Library để Fix Constraint Resolution

## Mục tiêu
Research và implement professional SQL parser library để thay thế regex-based parsing trong SqlQueryParser.cs, giải quyết vấn đề constraint resolution.

## Checklist

### 1. Research SQL Parser Libraries
- [x] 1.1. Microsoft.SqlServer.TransactSql.ScriptDom  
- [x] 1.2. ANTLR4 với SQL grammar
- [ ] 1.3. SqlParser.Net
- [ ] 1.4. ZenQuery  
- [x] 1.5. So sánh performance và features

### 2. Evaluate Best Option
- [ ] 2.1. Kiểm tra compatibility với MySQL/SQL Server
- [ ] 2.2. Test parsing complex WHERE conditions
- [ ] 2.3. Test parsing JOIN conditions
- [ ] 2.4. Verify license và cost

### 3. Implementation
- [ ] 3.1. Add NuGet package dependency
- [ ] 3.2. Create new SqlQueryParserV2 class  
- [ ] 3.3. Implement WHERE condition extraction
- [ ] 3.4. Implement JOIN condition extraction
- [ ] 3.5. Implement constraint resolution logic

### 4. Testing & Validation
- [ ] 4.1. Unit tests cho parser mới
- [ ] 4.2. Integration test với TC001 complex query
- [ ] 4.3. Performance comparison với regex approach
- [ ] 4.4. Validate constraint resolution logic

### 5. Integration & Deployment
- [ ] 5.1. Replace old parser in EngineService
- [ ] 5.2. Run full test suite
- [ ] 5.3. Update documentation
- [ ] 5.4. Validate TC001 passes với new parser

## Expected Outcome
- TC001_15 test passes với constraint resolution chính xác
- Better handling of complex SQL conditions
- More robust parsing than regex approach 