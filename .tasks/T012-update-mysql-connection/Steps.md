# Task T012: Cập nhật MySQL Connection String

## Mô tả
Đổi MySQL connection string từ freedb.tech sang MySQL 8.4 server mới (192.84.20.226)

## Các bước thực hiện

### Bước 1: Phân tích connection string hiện tại
- [x] Tìm file chứa connection string cũ
- [x] Xác định format connection string hiện tại
- [x] Ghi chú các thông số cần thay đổi

### Bước 2: Cập nhật connection string
- [x] Thay đổi server từ `sql.freedb.tech` thành `192.84.20.226`
- [x] Thay đổi user từ `freedb_TestAdmin` thành `root`
- [x] Thay đổi password từ `Vt5B&Mx6Jcu#jeN` thành `password`
- [x] Cập nhật database name từ `freedb_DBTest` thành `test`

### Bước 3: Kiểm tra tính nhất quán
- [x] Tìm kiếm các file khác có thể chứa connection string
- [x] Đảm bảo tất cả references được cập nhật
- [x] Xác minh format connection string MySQL đúng

### Bước 4: Validation
- [x] Kiểm tra syntax connection string
- [x] Ghi chú các thay đổi trong comments nếu cần
- [x] Chuẩn bị cho test connection (nếu có)

## Thông số mới
- Server: 192.84.20.226
- Port: 3306
- User: root
- Password: password
- Database: freedb_DBTest (giữ nguyên hoặc thay đổi theo yêu cầu) 