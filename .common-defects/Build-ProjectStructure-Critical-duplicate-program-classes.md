# Lỗi: Duplicate Program Classes và Cấu Trúc Project Sai

## Tóm tắt vấn đề
Có nhiều class Program và MainForm được định nghĩa trùng lặp trong cùng một project, gây lỗi compilation. Ngoài ra, cấu trúc project không đúng với many-to-many file references.

## Cách tái hiện
1. Build project GenDataDebug.csproj
2. Xuất hiện các lỗi:
   - CS0101: already contains a definition for 'Program'
   - CS0102: already contains a definition for 'MainForm'
   - CS0111: Type already defines a member
   - Missing Windows Forms references

## Nguyên nhân gốc
1. **Project reference path sai:** `../../../../SqlTestDataGenerator.Core/` không tồn tại
2. **File structure bị lẫn lộn:** Có nhiều Program.cs files trong cùng project
3. **Missing framework reference:** Windows Forms không được reference đúng cách
4. **Duplicate files:** Form1.cs và Form1.Designer.cs bị duplicate giữa root và UI project

## Giải pháp
1. **Fix project references:**
   - Sửa path trong GenDataDebug.csproj
   - Đảm bảo reference đúng local Core project

2. **Cấu trúc lại files:**
   - Tách riêng các Program.cs files vào đúng project
   - Chỉ giữ một MainForm trong UI project
   - Xóa duplicate files ở root directory

3. **Fix framework references:**
   - Thêm Windows Forms support cho UI project
   - Thêm các NuGet packages thiếu

## ID Task liên quan
T004 - Run and Test Application

## Mức độ ưu tiên
Critical - Cần fix ngay để có thể build được project 