# Task T015: Fix Coordinated Data Generation - Ensure Query Results = Expected Count

## Mục Tiêu
Sửa bug logic trong DataGenService để ensure **Query Results = DesiredRecordCount** bất kể câu query thay đổi như thế nào.

## Root Cause
Engine generate data theo từng table riêng lẻ, không ensure các records sẽ JOIN và match WHERE conditions để tạo ra đúng số lượng results mong muốn.

## Checklist Steps

### Phase 1: Phân Tích và Design Solution
- [ ] 1.1 Phân tích current logic trong DataGenService.GenerateBogusData()
- [ ] 1.2 Design new approach: Coordinated Data Generation Pattern
- [ ] 1.3 Define interface cho CoordinatedDataGenerator
- [ ] 1.4 Plan integration với existing SqlQueryParser

### Phase 2: Implement Coordinated Data Generation
- [ ] 2.1 Tạo class CoordinatedDataGenerator
- [ ] 2.2 Implement method AnalyzeQueryRequirements()
- [ ] 2.3 Implement method GenerateTargetRecords() 
- [ ] 2.4 Implement method EnsureCrossTableCoordination()
- [ ] 2.5 Integrate với DataGenService.GenerateBogusData()

### Phase 3: Handle WHERE Conditions Coordination
- [ ] 3.1 Parse và group WHERE conditions theo table
- [ ] 3.2 Identify cross-table dependencies (JOINs)
- [ ] 3.3 Generate records satisfying ALL conditions simultaneously
- [ ] 3.4 Ensure FK relationships link target records correctly

### Phase 4: Implement Query Validation Loop
- [ ] 4.1 Add method ValidateGeneratedData()
- [ ] 4.2 Execute query sau khi generate để check result count
- [ ] 4.3 If insufficient results → generate more targeted data
- [ ] 4.4 Repeat until DesiredRecordCount achieved

### Phase 5: Testing và Validation
- [ ] 5.1 Update existing unit tests
- [ ] 5.2 Test với Complex Vowis SQL (expect 15 → get 15)
- [ ] 5.3 Test với Simple SQL (expect 20 → get 20)
- [ ] 5.4 Test với Multi-table JOIN (expect 25 → get 25)
- [ ] 5.5 Performance testing với large datasets

### Phase 6: Documentation và Cleanup
- [ ] 6.1 Update code comments và XML documentation
- [ ] 6.2 Create integration test cases
- [ ] 6.3 Update README with new functionality
- [ ] 6.4 Log defect in common-defects folder với solution

## Expected Outcome
- ✅ Query Results = DesiredRecordCount (100% accuracy)
- ✅ Engine works với ANY complex query (adaptive)
- ✅ Performance remains acceptable (<30s execution)
- ✅ All existing tests pass

## Success Criteria
- Complex Query: 15 expected → 15+ actual results ✅
- Simple Query: 20 expected → 20+ actual results ✅  
- Multi-table: 25 expected → 25+ actual results ✅
- Zero "insufficient data" scenarios 