# Task T022: Fix Column Length/Type Constraint Violations

## Mục tiêu
Fix issue `Data truncated for column 'gender' at row 1` - Engine generate data vi phạm database constraints.

## Current Issue
Engine generate `'DefaultValue_1'` (13 chars) cho column có MaxLength nhỏ hơn.

## Checklist Fix Column Constraints

### 1. ✅ Debug current data generation
- [x] Tạo task folder T022
- [x] Tạo checklist Steps.md
- [ ] Identify nơi generate `'DefaultValue_1'`
- [ ] Check tại sao không respect MaxLength
- [ ] Analyze database metadata usage

### 2. 🔧 Fix data generation logic
- [x] Ensure ALL string generation respects MaxLength
- [x] Fix GenerateDefaultValue() method  
- [x] Update fallback logic to be constraint-aware
- [x] Add validation cho generated values
- [x] **ENUM SUPPORT**: Added COLUMN_TYPE parsing for MySQL ENUM
- [x] **GENDER FIXED**: Now generates 'Male', 'Female', 'Other' correctly
- [ ] **NEW ISSUE**: Fix Foreign Key constraint violation

### 3. 🧪 Test fix
- [ ] Build solution
- [ ] Test TC001 với generic constraint handling
- [ ] Verify no constraint violations
- [ ] Confirm engine still generic

### 4. ✅ Final validation
- [ ] TC001 passes without constraint errors
- [ ] No hardcode solutions
- [ ] Proper generic constraint handling
- [ ] Update task status

## Strategy
**100% GENERIC approach** - Use database metadata (MaxLength, DataType) để generate appropriate data for ANY column without hardcode. 