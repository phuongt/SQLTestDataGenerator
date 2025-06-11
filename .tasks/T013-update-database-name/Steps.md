# Task T013: Cập nhật Database Name trong Connection String

## Mô tả
Đổi database name từ `test` thành `phuonglm_test_db` trong tất cả các function test

## Connection String mới
`Server=192.84.20.226;Port=3306;Database=phuonglm_test_db;Uid=root;Pwd=password;`

## Các bước thực hiện

### Bước 1: Tìm tất cả connection strings hiện tại
- [x] Tìm kiếm tất cả file chứa connection string với database `test`
- [x] Liệt kê các file cần cập nhật
- [x] Xác định các constant và variables cần thay đổi

### Bước 2: Cập nhật các file test chính
- [x] ExecuteQueryWithTestDataAsyncDemoTests.cs - 3 places updated
- [x] RealMySQLIntegrationTests.cs - 1 place updated
- [x] MySQLIntegrationDuplicateKeyTests.cs - 1 place updated
- [x] RecordCountVerificationTests.cs - 1 place updated

### Bước 3: Cập nhật các file khác
- [x] Kiểm tra các file trong thư mục .tasks - không cần cập nhật
- [x] Cập nhật MainForm.cs nếu cần - không cần thay đổi
- [x] Kiểm tra các file config khác - hoàn tất

### Bước 4: Validation
- [x] Kiểm tra syntax connection string - đúng format MySQL
- [x] Đảm bảo tính nhất quán - tất cả đã thống nhất
- [x] Verify không có connection string cũ nào bị bỏ sót - hoàn tất

## Thay đổi
- Database: `test` → `phuonglm_test_db`
- Các thông số khác giữ nguyên: Server=192.84.20.226, Port=3306, User=root, Password=password 