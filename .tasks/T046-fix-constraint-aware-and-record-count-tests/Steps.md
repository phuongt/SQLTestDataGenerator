# T046: Fix ConstraintAwareGenerationTests và RecordCountVerificationTests

## Mục tiêu
Fix 2 test classes thất bại:
1. ConstraintAwareGenerationTests - Cần AI API key (Medium priority)
2. RecordCountVerificationTests - Database constraints (High priority)

## Checklist

### Phase 1: Phân tích vấn đề
- [ ] 1.1 Đọc và phân tích ConstraintAwareGenerationTests.cs để hiểu lỗi
- [ ] 1.2 Đọc và phân tích RecordCountVerificationTests.cs để hiểu lỗi
- [ ] 1.3 Kiểm tra cấu hình AI API key hiện tại trong appsettings.json
- [ ] 1.4 Xác định database constraints đang gây lỗi

### Phase 2: Fix ConstraintAwareGenerationTests (AI API key issue)
- [ ] 2.1 Kiểm tra AI API key configuration trong ConfigurationService
- [ ] 2.2 Cập nhật appsettings.json với AI API key
- [ ] 2.3 Đảm bảo test có thể access AI API key từ config
- [ ] 2.4 Test lại ConstraintAwareGenerationTests

### Phase 3: Fix RecordCountVerificationTests (Database constraint issue)
- [ ] 3.1 Xác định database constraints gây lỗi
- [ ] 3.2 Sửa logic constraint validation trong test
- [ ] 3.3 Cập nhật mock data hoặc database setup nếu cần
- [ ] 3.4 Test lại RecordCountVerificationTests

### Phase 4: Verification
- [ ] 4.1 Chạy cả 2 test classes để đảm bảo pass
- [ ] 4.2 Chạy regression test cho các test liên quan
- [ ] 4.3 Cập nhật documentation nếu có thay đổi config

### Phase 5: Cleanup
- [ ] 5.1 Review code changes
- [ ] 5.2 Commit và document changes
- [ ] 5.3 Update task status

## Priority
- RecordCountVerificationTests: High (Database constraints)
- ConstraintAwareGenerationTests: Medium (AI API key)

## Expected Outcome
- 2/2 failing tests should pass
- AI API key properly configured
- Database constraint validation working correctly 