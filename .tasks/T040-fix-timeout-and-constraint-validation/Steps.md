# Task T040: Fix Timeout và Constraint Validation

## Mục tiêu
- Phương án 1: Tăng connection timeout cho database tests
- Phương án 2: Review logic constraint validation để khắc phục 9 tests failed

## Checklist thực hiện

### Phase 1: Phân tích hiện trạng
- [ ] 1.1. Kiểm tra cấu trúc project và test failures hiện tại
- [ ] 1.2. Xác định các test cases bị fail và nguyên nhân
- [ ] 1.3. Kiểm tra connection timeout settings hiện tại
- [ ] 1.4. Review logic constraint validation trong codebase

### Phase 2: Fix Connection Timeout (Phương án 1)
- [ ] 2.1. Tìm và cập nhật timeout settings trong appsettings.json
- [ ] 2.2. Cập nhật timeout trong database connection strings
- [ ] 2.3. Fix timeout trong integration test configuration
- [ ] 2.4. Test lại connection với timeout mới

### Phase 3: Fix Constraint Validation Logic (Phương án 2)
- [ ] 3.1. Review constraint validation logic trong Services
- [ ] 3.2. Fix các issues với constraint parsing
- [ ] 3.3. Cập nhật logic validate foreign key relationships
- [ ] 3.4. Fix constraint handling cho complex scenarios

### Phase 4: Test và Validation
- [ ] 4.1. Chạy lại integration tests
- [ ] 4.2. Verify các test cases đã pass
- [ ] 4.3. Kiểm tra performance với timeout mới
- [ ] 4.4. Update documentation nếu cần

### Phase 5: Clean up
- [ ] 5.1. Remove debug code và logs không cần thiết
- [ ] 5.2. Update error messages cho rõ ràng hơn
- [ ] 5.3. Commit changes với clear commit message
- [ ] 5.4. Verify final state

## Expected Results
- Tất cả 9 test cases failed phải pass
- Connection timeout phù hợp cho môi trường test
- Constraint validation logic hoạt động đúng
- Code clean và maintainable 