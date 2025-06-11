# Task T026: Khắc phục JOIN Regex Pattern trong SqlQueryParser

## Checklist thực hiện

### 1. Phân tích vấn đề hiện tại
- [ ] Kiểm tra test case đang fail: `ParseQuery_JoinConditions_ShouldExtractJoinRequirements`
- [ ] Xem regex pattern hiện tại trong `ExtractJoinRequirements` method
- [ ] Phân tích tại sao pattern không match với câu SQL đơn giản

### 2. Debug và xác định root cause
- [ ] Tạo test case riêng biệt để test regex pattern JOIN
- [ ] Thêm debug logging chi tiết trong `ExtractJoinRequirements`
- [ ] Test regex pattern với các trường hợp SQL khác nhau

### 3. Sửa chữa regex pattern
- [ ] Điều chỉnh JOIN regex pattern để match đúng các trường hợp
- [ ] Đảm bảo pattern hoạt động với:
  - INNER JOIN, LEFT JOIN, RIGHT JOIN, FULL JOIN
  - Table aliases (u, c, etc.)
  - Các ON conditions khác nhau

### 4. Validation và testing
- [ ] Chạy lại test case `ParseQuery_JoinConditions_ShouldExtractJoinRequirements`
- [ ] Chạy toàn bộ test suite để đảm bảo không break code khác
- [ ] Test với các SQL queries phức tạp hơn

### 5. Tối ưu và hoàn thiện
- [ ] Thêm unit tests cho các JOIN scenarios khác
- [ ] Cập nhật documentation nếu cần
- [ ] Ghi nhận vào common defects nếu cần thiết

## Expected Outcome
- Test `ParseQuery_JoinConditions_ShouldExtractJoinRequirements` pass
- JOIN parsing hoạt động đúng với các loại JOIN khác nhau
- Giảm số lượng failed tests trong test suite 