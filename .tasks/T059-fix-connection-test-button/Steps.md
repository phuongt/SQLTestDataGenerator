# Task: T059 - Fix Connection Test Button

## Sub-Tasks

1. ✅ **Review Common Defects & Rules**  
   - Đọc `/common-defects/` và coding rules liên quan.

2. ☐ **Analyze Connection Test Issue**  
   - Kiểm tra lỗi khi ấn nút "Test Connection"
   - Phân tích code `btnTestConnection_Click` và `TestConnectionAsync`
   - Xác định nguyên nhân gây lỗi connection

3. ☐ **Check Database Server Status**  
   - Kiểm tra MySQL server có đang chạy không
   - Test connection string mặc định
   - Verify database credentials

4. ☐ **Fix Connection String Issues**  
   - Sửa connection string template
   - Thêm validation cho connection string
   - Cải thiện error handling

5. ☐ **Improve Error Messages**  
   - Cung cấp thông báo lỗi chi tiết hơn
   - Thêm troubleshooting guide
   - Hiển thị connection details an toàn

6. ☐ **Test Connection Fix**  
   - Test với các database khác nhau
   - Verify SSH tunnel connection
   - Kiểm tra timeout settings

7. ☐ **Finalize & Document**  
   - Ghi lỗi mới (nếu có) vào `/common-defects/`  
   - Rà lại toàn bộ các sub-task

## Issue Description
Người dùng báo lỗi khi ấn nút "Test Connection" trên giao diện. Cần kiểm tra và sửa lỗi connection.

## Expected Outcome
- Nút Test Connection hoạt động bình thường
- Hiển thị thông báo lỗi rõ ràng nếu có vấn đề
- Connection string validation tốt hơn 