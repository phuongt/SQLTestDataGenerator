# PowerShell Syntax Error - && Operator
## Defect Classification
- **Layer**: FE (Frontend/Tooling)
- **Error Type**: PowerShell Syntax
- **Severity**: Minor
- **Slug**: syntax-operator-error

---

## Summary
Windows PowerShell không hỗ trợ `&&` operator như Linux/MacOS bash shells, gây ra lỗi "The token '&&' is not a valid statement separator in this version."

## How to Reproduce
1. Mở Windows PowerShell (không phải PowerShell Core)
2. Chạy command với && operator:
   ```powershell
   cd folder && dotnet test
   ```
3. Lỗi sẽ xuất hiện ngay lập tức

## Root Cause
Windows PowerShell 5.1 và các phiên bản cũ không implement `&&` và `||` operators. Đây là tính năng chỉ có trong PowerShell Core (7.0+) và các Unix shells.

## Solution/Workaround

### Cách 1: Sử dụng semicolon
```powershell
cd folder; dotnet test
```

### Cách 2: Chạy lệnh riêng biệt
```powershell
cd folder
dotnet test
```

### Cách 3: Sử dụng PowerShell Core
```powershell
pwsh -c "cd folder && dotnet test"
```

### Cách 4: Sử dụng conditional execution
```powershell
cd folder
if ($LASTEXITCODE -eq 0) { dotnet test }
```

## Prevention Tips
1. Kiểm tra PowerShell version trước khi viết scripts
2. Sử dụng semicolon thay vì && cho Windows PowerShell
3. Document shell requirements trong project README
4. Consider PowerShell Core cho cross-platform compatibility

## Related Occurrences
- **TC007**: Request ID a8783b51-f696-4020-ac26-5a7cf7da6b52
- **Resolution Date**: 2025-06-08
- **Affected Commands**: dotnet test với && operator

## Notes
- Lỗi này chỉ xảy ra với Windows PowerShell, không ảnh hưởng PowerShell Core
- Dễ fix nhưng có thể gây confusion cho developers quen với bash
- Should standardize on PowerShell syntax trong development team 