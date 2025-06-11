# Task T003: Comprehensive Constraint-Aware AI Generation với Validation & Retry

## Mục Tiêu
Implement solution để bóc tách tất cả điều kiện SQL (WHERE, JOIN, LIKE) và truyền vào AI để generate đúng dữ liệu, kèm validation và retry mechanism (max 5 lần).

## Phân Tích từ TC001
TC001 failed vì:
1. **Missing VNEXT pattern** - LIKE constraint không apply
2. **Wrong is_active value** - JOIN condition không extract đúng 
3. **No validation** - Không check generated data có satisfy constraints

## Implementation Checklist

### Bước 1: Enhanced Constraint Extraction
- [x] Fix SqlQueryParser để extract JOIN conditions properly
- [x] Capture `ur.is_active = TRUE` từ JOIN ON clauses  
- [x] Extract all LIKE patterns với exact values (VNEXT, DD, Phương)
- [x] Parse complex WHERE conditions with AND/OR logic
- [x] Test constraint extraction với TC001 query

### Bước 2: AI Context Enhancement  
- [ ] Enhance GenerationContext với detailed constraint info
- [ ] Pass LIKE patterns to AI (e.g., "company name must contain VNEXT")
- [ ] Pass JOIN constraints to AI (e.g., "is_active must be TRUE")
- [ ] Include business context instructions cho specific patterns
- [ ] Test AI prompt generation với extracted constraints

### Bước 3: Constraint Validation Service
- [ ] Enhance ConstraintValidator để validate all constraint types
- [ ] Validate LIKE patterns (VNEXT, DD, Phương)
- [ ] Validate JOIN conditions (is_active = TRUE)  
- [ ] Validate date constraints (YEAR = 1989)
- [ ] Return detailed violation information

### Bước 4: Retry Mechanism Implementation
- [ ] Implement retry logic trong AI generation (max 5 attempts)
- [ ] Track failed constraints cho targeted regeneration
- [ ] Enhance prompts với failure feedback for retry
- [ ] Add progressive constraint enforcement (stricter each retry)
- [ ] Log retry attempts và success rates

### Bước 5: Integration với Engine Pipeline
- [ ] Integrate enhanced constraint extraction vào EngineService
- [ ] Apply validation check sau mỗi generated record
- [ ] Trigger retry mechanism khi validation fails
- [ ] Maintain performance targets (< 2 seconds per retry)
- [ ] Log constraint satisfaction metrics

### Bước 6: TC001 Validation
- [ ] Test với TC001 query để verify VNEXT constraint
- [ ] Verify ur.is_active = TRUE constraint satisfaction
- [ ] Check all generated records satisfy constraints
- [ ] Measure retry success rate và performance impact
- [ ] Confirm TC001 returns expected 15 rows

## Expected Implementation Flow

```csharp
// 1. Enhanced Constraint Extraction
var constraints = await ExtractAllConstraints(sqlQuery);
// VNEXT, ur.is_active=TRUE, YEAR=1989, etc.

// 2. AI Generation với Constraints  
var aiContext = BuildDetailedAIContext(constraints, tableSchema);
var generatedData = await GenerateWithConstraints(aiContext);

// 3. Validation
var validationResult = ValidateConstraints(generatedData, constraints);

// 4. Retry if Failed (max 5 times)
var retryCount = 0;
while (!validationResult.IsValid && retryCount < 5) {
    var enhancedContext = EnhanceContextWithFailures(aiContext, validationResult);
    generatedData = await RegenerateWithConstraints(enhancedContext);
    validationResult = ValidateConstraints(generatedData, constraints);
    retryCount++;
}
```

## Success Metrics
- **TC001 test**: PASS ✅ (15 rows returned)
- **Constraint satisfaction**: 100% compliance 
- **Retry success rate**: > 80% within 3 attempts
- **Performance**: < 10 seconds total for constraint-aware generation
- **VNEXT pattern**: Present trong generated company names
- **JOIN constraints**: All is_active values correct

## Implementation Priority
1. **High**: Enhanced constraint extraction (Step 1)
2. **High**: AI context với constraints (Step 2)  
3. **Medium**: Validation service (Step 3)
4. **Medium**: Retry mechanism (Step 4)
5. **Low**: Engine integration (Step 5)
6. **Low**: TC001 validation (Step 6) 