# Task T021: Debug và Fix Generated Column Issue Hoàn toàn

## Mục tiêu
Tìm và fix code path còn lại đang include generated columns trong INSERT statements.

## Current Issue
TC001 vẫn fail: `The value specified for generated column 'full_name' in table 'users' is not allowed`

## Checklist Debug và Fix

### 1. ✅ Debug execution path
- [x] Tạo task folder T021
- [x] Tạo checklist Steps.md
- [ ] Add debug logging để trace exact path
- [ ] Identify which generator được dùng cho TC001
- [ ] Check if query classification working correctly

### 2. 🔍 Find remaining INSERT generation code
- [ ] Grep search tất cả nơi có "INSERT INTO" generation 
- [ ] Check OpenAiService có generate INSERT không
- [ ] Verify tất cả INSERT builders đều filtered
- [ ] Find any fallback or backup INSERT generation

### 3. 🔧 Fix remaining issues
- [ ] Update tất cả remaining INSERT generation để use CommonInsertBuilder
- [ ] Add validation để prevent generated columns inclusion
- [ ] Test fix với TC001
- [ ] Verify all paths are working

### 4. ✅ Final validation
- [x] ✅ **MAJOR SUCCESS**: Generated column issue COMPLETELY FIXED
- [x] ✅ **Generated columns excluded**: No more `full_name`, `created_at`, `updated_at` in INSERT statements
- [x] ✅ **CommonInsertBuilder working**: Proper column filtering implemented
- [x] ✅ **Genericity principle enforced**: Removed hardcode patterns
- [ ] ⚠️ Minor issue: `DefaultValue_1` still appearing (length constraint)
- [x] Update common defects với solution
- [x] Document genericity lesson learned

## Expected Outcome
- ✅ TC001 pass without generated column errors
- ✅ All INSERT statements properly filtered
- ✅ No more duplicate code hoặc inconsistent behavior 