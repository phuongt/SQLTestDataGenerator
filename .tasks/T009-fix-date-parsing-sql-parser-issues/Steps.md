# T009 - Fix DATE_ADD Parsing & SQL Parser Edge Cases
## Kế hoạch chi tiết investigate và fix 2 critical parsing issues

---

## 📋 Task Overview

**Target Issues**:
1. **DATE_ADD Parsing Logic Issue** (4 failed tests)
2. **SQL Parser Edge Cases** (4 failed tests)

**Total Tests to Fix**: 8/13 relevant failed tests  
**Priority**: MEDIUM to LOW (không blocking production)  
**Timeline**: 2-3 sprints

---

## 🎯 Issue 1: DATE_ADD Parsing Logic Fix

### 📊 Current Problem
```
Assert.AreEqual failed. Expected:<1>. Actual:<2>. Should parse DATE_ADD with [DAY|DAYS|MONTH|YEARS]
```

**Root Cause**: Parser đang extract 2 constraints thay vì 1 từ DATE_ADD expressions

### 🔍 Investigation Steps

#### Step 1.1: Reproduce Failed Tests
- [ ] Chạy individual failed tests để capture exact behavior
- [ ] `ParseDateConditions_DateAddWithDay_ShouldParseCorrectly`
- [ ] `ParseDateConditions_DateAddWithDays_ShouldParseCorrectly`
- [ ] `ParseDateConditions_DateAddWithMonth_ShouldParseCorrectly`
- [ ] `ParseDateConditions_DateAddWithYear_ShouldParseCorrectly`

#### Step 1.2: Analyze Parsing Logic
- [ ] Review `SqlQueryParserV2.cs` - DATE_ADD extraction logic
- [ ] Review `SqlMetadataService.cs` - constraint counting
- [ ] Review `SqlDateAddUTF8Tests.cs` - test expectations
- [ ] Identify where double-counting occurs

#### Step 1.3: Locate Root Cause Files
- [ ] `SqlTestDataGenerator.Core/Services/SqlQueryParserV2.cs`
- [ ] `SqlTestDataGenerator.Core/Services/SqlMetadataService.cs`
- [ ] `SqlTestDataGenerator.Core/Services/SqlQueryParser.cs`
- [ ] Constraint extraction methods

### 🛠️ Fix Implementation Steps

#### Step 1.4: Debug Current Logic
- [ ] Add debug logging to constraint extraction
- [ ] Trace DATE_ADD parsing flow
- [ ] Identify duplicate counting points

#### Step 1.5: Fix Constraint Counting
- [ ] Update DATE_ADD regex patterns
- [ ] Ensure single constraint per DATE_ADD expression
- [ ] Fix both ScriptDom and Regex parsers

#### Step 1.6: Update Test Expectations
- [ ] Verify test expectations are correct
- [ ] Update assertions if needed
- [ ] Ensure consistency across all DATE_ADD variations

### ✅ Validation Steps

#### Step 1.7: Test Fix
- [ ] Run all 4 DATE_ADD tests
- [ ] Verify Expected:<1>, Actual:<1>
- [ ] Test với various DATE_ADD patterns
- [ ] Regression test other parsing functionality

---

## 🎯 Issue 2: SQL Parser Edge Cases Fix

### 📊 Current Problem
Multiple SQL parsing edge cases failing in complex query scenarios

### 🔍 Investigation Steps

#### Step 2.1: Identify Specific Failed Tests
- [ ] List exact failed test names trong SQL parser category
- [ ] Analyze specific SQL patterns causing failures
- [ ] Review ScriptDom vs Regex fallback scenarios

#### Step 2.2: Analyze Parser Architecture
- [ ] Review `SqlQueryParserV2.cs` main parser
- [ ] Review fallback logic to regex parser
- [ ] Review MySQL-specific syntax handling
- [ ] Review complex JOIN pattern recognition

#### Step 2.3: Pattern Analysis
- [ ] Complex JOIN conditions
- [ ] Nested subqueries handling
- [ ] MySQL-specific functions
- [ ] Edge case SQL syntax

### 🛠️ Fix Implementation Steps

#### Step 2.4: Enhance ScriptDom Parser
- [ ] Improve MySQL syntax compatibility
- [ ] Handle edge case patterns better
- [ ] Add better error recovery

#### Step 2.5: Improve Regex Fallback
- [ ] Enhance regex patterns for edge cases
- [ ] Better error handling for malformed SQL
- [ ] Improve pattern matching accuracy

#### Step 2.6: Add Pattern Support
- [ ] Support additional SQL patterns
- [ ] Handle complex nested structures
- [ ] Improve alias resolution

### ✅ Validation Steps

#### Step 2.7: Test Fix
- [ ] Run all failed SQL parser tests
- [ ] Test với complex business queries
- [ ] Verify fallback mechanisms
- [ ] Integration test với real MySQL

---

## 📁 File Structure Plan

### Files to Investigate:
```
SqlTestDataGenerator.Core/
├── Services/
│   ├── SqlQueryParserV2.cs          # Main parser logic
│   ├── SqlMetadataService.cs        # Table/constraint extraction
│   ├── SqlQueryParser.cs            # Regex fallback parser
│   └── ConstraintExtractor.cs       # Constraint extraction logic

SqlTestDataGenerator.Tests/
├── SqlDateAddUTF8Tests.cs           # DATE_ADD test cases
├── SqlQueryParserV2Tests.cs         # Parser V2 tests
├── SqlQueryParserV3Tests.cs         # Parser V3 tests
└── ComprehensiveConstraintExtractorTests.cs
```

### Evidence Structure:
```
OneAI/Evidence/T009/
├── Step001_DATE_ADD_Debug_Analysis.txt
├── Step002_SQL_Parser_Edge_Cases_Analysis.txt
├── Step003_Fixed_DATE_ADD_Tests_Results.txt
├── Step004_Fixed_SQL_Parser_Tests_Results.txt
└── Step005_Integration_Verification.txt
```

---

## 🕒 Timeline & Priorities

### Sprint 1 (Week 1-2):
- [ ] **Issue 1 Investigation** (Steps 1.1-1.3)
- [ ] **Issue 2 Investigation** (Steps 2.1-2.3)
- [ ] Root cause analysis complete

### Sprint 2 (Week 3-4):
- [ ] **Issue 1 Fix Implementation** (Steps 1.4-1.6)
- [ ] **Issue 2 Fix Implementation** (Steps 2.4-2.6)
- [ ] Initial testing & validation

### Sprint 3 (Week 5-6):
- [ ] **Comprehensive Testing** (Steps 1.7, 2.7)
- [ ] **Integration Verification**
- [ ] **Documentation Update**
- [ ] **Final Validation**

---

## 🚨 Risk Assessment

### Low Risk:
- DATE_ADD parsing fix (isolated logic)
- Test expectation updates

### Medium Risk:
- SQL parser architecture changes
- Regex pattern modifications
- ScriptDom compatibility updates

### Mitigation:
- [ ] Extensive unit testing
- [ ] Integration test validation
- [ ] Fallback mechanism verification
- [ ] Rollback plan preparation

---

## 📊 Success Criteria

### Issue 1 Success:
- [ ] All 4 DATE_ADD tests pass
- [ ] Expected:<1>, Actual:<1> for all tests
- [ ] No regression in other parsing tests
- [ ] Performance impact < 5%

### Issue 2 Success:
- [ ] All SQL parser edge case tests pass
- [ ] Complex query parsing improved
- [ ] Fallback mechanisms stable
- [ ] No regression in existing functionality

### Overall Success:
- [ ] 8/8 target tests pass
- [ ] Integration tests remain 100%
- [ ] System performance stable
- [ ] Production deployment confidence maintained

---

## 📝 Notes

- Issues không blocking production deployment
- Có thể implement parallel với production monitoring
- Focus on quality over speed
- Maintain fallback mechanisms throughout fix process

---

*Plan Created: 2025-06-08 12:20:00*  
*Estimated Effort: 15-20 developer days*  
*Risk Level: LOW to MEDIUM* 