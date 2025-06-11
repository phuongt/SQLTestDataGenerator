# Checklist Task T002: Constraint-Aware AI Generation với Validation & Regeneration

## Mục Tiêu
Implement logic để extract constraint values từ SQL (WHERE, JOIN, GROUP BY) và đưa cho AI, kèm theo validation mechanism để kiểm tra và regenerate nếu data không match constraints.

## Phân Tích Từ TC001
Từ test case TC001, ta thấy engine thiếu:
1. **Company name LIKE '%VNEXT%'** - AI không generate company names có "VNEXT"
2. **Logic validation** - Không check generated data có satisfy constraints không
3. **Regeneration mechanism** - Không retry khi data không match

## Checklist Implementation

### Bước 1: Constraint Extraction Enhancement
- [x] Enhance SqlMetadataService để extract constraint values  
- [x] Parse WHERE clause conditions với actual values
- [ ] Extract JOIN conditions với foreign key relationships  
- [x] Parse LIKE patterns, IN values, range conditions
- [ ] Extract GROUP BY và HAVING constraints

### Bước 2: AI Context Enhancement
- [x] Modify AiService để nhận constraint context
- [x] Enhance AI prompt với specific constraint values
- [x] Include business context patterns (VNEXT, DD, Phương, etc.)
- [x] Add constraint satisfaction instructions cho AI

### Bước 3: Data Validation Logic
- [x] Tạo ConstraintValidator service
- [x] Implement validation cho each constraint type:
  - [x] LIKE pattern validation
  - [x] Range validation (dates, numbers)
  - [x] Foreign key relationship validation
  - [x] Boolean condition validation
- [x] Create validation report structure

### Bước 4: Regeneration Mechanism
- [x] Implement retry logic với max attempts
- [x] Track failed constraints cho targeted regeneration
- [x] Optimize regeneration để chỉ gen lại fields vi phạm
- [x] Add fallback strategy khi AI fails multiple times

### Bước 5: Integration với EngineService
- [ ] Integrate constraint extraction vào data generation workflow
- [ ] Add validation step after each generation attempt
- [ ] Implement regeneration loop với timeout protection
- [ ] Add detailed logging cho constraint satisfaction

### Bước 6: Testing & Validation
- [ ] Update TC001 để test VNEXT constraint satisfaction
- [ ] Create unit tests cho constraint extraction
- [ ] Test validation logic với various SQL patterns
- [ ] Performance test với regeneration scenarios

### Bước 7: Documentation & Examples
- [ ] Document constraint extraction patterns
- [ ] Add examples cho complex constraint scenarios
- [ ] Create troubleshooting guide cho constraint issues

## Technical Specifications

### Constraint Types Cần Support:
1. **LIKE Patterns**: `c.NAME LIKE '%VNEXT%'`
2. **Exact Values**: `YEAR(u.date_of_birth) = 1989`
3. **Range Conditions**: `r.level >= 2`
4. **Boolean Logic**: `u.is_active = 0`
5. **Date Functions**: `DATE_ADD(NOW(), INTERVAL 30 DAY)`
6. **IN Clauses**: `r.code IN ('DD', 'ADMIN')`

### Validation Logic:
```csharp
public class ConstraintValidator
{
    bool ValidateConstraints(GeneratedData data, List<Constraint> constraints);
    List<FailedConstraint> GetViolations(GeneratedData data, List<Constraint> constraints);
    RegenerationStrategy SuggestRegeneration(List<FailedConstraint> violations);
}
```

### Regeneration Strategy:
- **Max Attempts**: 3 attempts per constraint
- **Targeted Regen**: Chỉ regenerate fields vi phạm constraints
- **Fallback**: Use constraint-based generation nếu AI fails
- **Timeout**: 30 seconds max cho full regeneration cycle

## Expected Outcomes
1. **TC001 Success**: Generate data với company names có "VNEXT"
2. **Constraint Satisfaction**: 95%+ constraints được satisfy
3. **Performance**: < 2 seconds cho complex scenarios với regeneration
4. **Reliability**: Graceful fallback khi AI generation fails

## Success Criteria
- [ ] TC001 passes với generated data satisfying ALL constraints
- [ ] Constraint extraction accuracy: 100% cho supported SQL patterns
- [ ] Validation accuracy: 100% cho generated data
- [ ] Regeneration effectiveness: 90%+ success rate after retry
- [ ] Performance impact: < 50% increase in generation time 