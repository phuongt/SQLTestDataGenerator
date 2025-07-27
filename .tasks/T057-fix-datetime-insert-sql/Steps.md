# Task: T057 - Fix datetime insert SQL export for Oracle

## Sub-Tasks

1. ☐ **Review Common Defects & Rules**  
   - Đọc `/common-defects/` và coding rules liên quan.

2. ☐ **Xác định vị trí logic sinh file export Oracle**  
   - Tìm class/service chịu trách nhiệm sinh SQL export cho Oracle.

3. ☐ **Phân tích & xác định cách detect các trường Date/Time**  
   - Đảm bảo detect đúng các cột kiểu DATE, TIMESTAMP, DATETIME, TIME.

4. ☐ **Sửa code sinh SQL**  
   - Convert giá trị Date/Time sang TO_DATE/TO_TIMESTAMP đúng chuẩn Oracle.
   - Đảm bảo không ảnh hưởng các kiểu dữ liệu khác.

5. ☐ **Viết/Update unit test cho logic này**  
   - Đảm bảo test case cho các trường hợp Date/Time, các kiểu dữ liệu khác không bị ảnh hưởng.

6. ☐ **Manual test: Sinh thử file export Oracle, kiểm tra kết quả**

7. ☐ **Finalize & Document**  
   - Ghi nhận lỗi mới (nếu có) vào `/common-defects/`  
   - Rà lại toàn bộ các sub-task


