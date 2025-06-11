# SQL Test Data Generator

Một ứng dụng Windows Forms để tạo dữ liệu test/mock cho database dựa trên câu lệnh SQL do người dùng cung cấp. Hỗ trợ các developer và QA trong việc tự động hóa việc setup dữ liệu cho môi trường testing.

## 🎯 Tính năng chính

- **Tạo dữ liệu thông minh**: Sử dụng AI (OpenAI) để tạo dữ liệu realistic dựa trên cấu trúc database
- **Hỗ trợ đa database**: SQL Server, MySQL, PostgreSQL, SQLite
- **Giao diện trực quan**: Windows Forms với thiết kế modern và user-friendly
- **Xem trước SQL**: Preview SQL được generate trước khi thực thi
- **Logging chi tiết**: Theo dõi toàn bộ quá trình với structured logging
- **Export SQL**: Xuất SQL ra file thay vì thực thi trực tiếp
- **Quản lý cấu hình**: Lưu trữ connection strings và queries gần đây

## 🏗️ Kiến trúc dự án

```
SqlTestDataGenerator/
├── SqlTestDataGenerator.Core/          # Business logic và services
│   ├── Models/                         # Data models
│   │   ├── AppSettings.cs             # Application configuration
│   │   ├── DatabaseModels.cs          # Database-related models  
│   │   ├── QueryExecutionRequest.cs   # Request models
│   │   └── UserSettings.cs            # User preferences
│   └── Services/                       # Core services
│       ├── ConfigurationService.cs    # Configuration management
│       ├── DataGenService.cs          # Data generation logic
│       ├── DbConnectionFactory.cs     # Database connections
│       ├── EngineService.cs           # Main orchestration
│       ├── LoggerService.cs           # Centralized logging
│       ├── OpenAiService.cs           # AI integration
│       └── SqlMetadataService.cs      # Database metadata
├── SqlTestDataGenerator.UI/            # Windows Forms UI
│   ├── MainForm.cs                    # Main application form
│   ├── LogViewForm.cs                 # Log viewer form
│   ├── Program.cs                     # Application entry point
│   └── appsettings.json               # Configuration file
├── SqlTestDataGenerator.Tests/         # Unit tests
│   ├── ConfigurationServiceTests.cs   # Configuration tests
│   └── LoggerServiceTests.cs          # Logging tests
└── README.md
```

## 🚀 Cài đặt và chạy

### Yêu cầu hệ thống
- .NET 8.0 hoặc cao hơn
- Windows 10/11
- Visual Studio 2022 hoặc JetBrains Rider

### Cách cài đặt

1. **Clone repository**
```bash
git clone https://github.com/your-repo/SqlTestDataGenerator.git
cd SqlTestDataGenerator
```

2. **Restore packages**
```bash
dotnet restore
```

3. **Cấu hình API key**
   - Mở file `SqlTestDataGenerator.UI/appsettings.json`
   - Thay thế `"your-openai-api-key-here"` bằng OpenAI API key của bạn

4. **Build và chạy**
```bash
dotnet build
dotnet run --project SqlTestDataGenerator.UI
```

## ⚙️ Cấu hình

### appsettings.json
```json
{
  "OpenAiApiKey": "sk-your-actual-api-key-here",
  "Database": {
    "DefaultType": "SQLite",
    "DefaultConnectionString": "Data Source=testdb.sqlite",
    "TimeoutSeconds": 30
  },
  "Logging": {
    "LogLevel": "Information",
    "FilePath": "logs/app.log",
    "EnableUILogging": true,
    "MaxFileSizeMB": 10
  },
  "DataGeneration": {
    "MaxRecordsPerBatch": 1000,
    "DefaultRecordCount": 10,
    "UseAIGeneration": true,
    "AIModel": "gpt-3.5-turbo"
  }
}
```

### Connection Strings mẫu

**SQL Server:**
```
Server=localhost;Database=TestDB;Trusted_Connection=true;TrustServerCertificate=true;
```

**MySQL:**
```
Server=localhost;Database=testdb;Uid=root;Pwd=password;
```

**PostgreSQL:**
```
Host=localhost;Database=testdb;Username=postgres;Password=password;
```

**SQLite:**
```
Data Source=testdb.sqlite
```

## 📝 Hướng dẫn sử dụng

1. **Chọn Database Type** và nhập Connection String
2. **Test Connection** để đảm bảo kết nối thành công
3. **Nhập SQL Query** - có thể là SELECT hoặc INSERT template
4. **Chọn số lượng records** muốn tạo
5. **Generate Data** để tạo dữ liệu bằng AI
6. **Preview** SQL được generate
7. **Execute** để chạy trực tiếp hoặc **Export** ra file

### Ví dụ SQL Template
```sql
-- Tìm user tên Phương, sinh 1989, công ty VNEXT
SELECT 
    u.id, u.username, u.first_name, u.last_name, u.email,
    u.date_of_birth, u.salary, u.department, u.hire_date,
    c.name as company_name, c.code as company_code,
    r.name as role_name, r.code as role_code
FROM users u
JOIN companies c ON u.company_id = c.id
JOIN user_roles ur ON u.id = ur.user_id
JOIN roles r ON ur.role_id = r.id
WHERE 
    (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
    AND YEAR(u.date_of_birth) = 1989
    AND c.name LIKE '%VNEXT%'
```

## 🧪 Testing

### Chạy Unit Tests
```bash
dotnet test
```

### Test Coverage
- **LoggerService**: Test logging functionality, file operations, UI integration
- **ConfigurationService**: Test settings loading/saving, API key validation
- **DataGenService**: Test data generation logic và AI integration

## 🛠️ Phát triển

### Coding Standards
- **C# 10+** với nullable reference types
- **PascalCase** cho classes, methods, public properties
- **camelCase** cho local variables, private fields
- **Defensive programming** với null checks và error handling
- **XML documentation** cho tất cả public methods

### Logging Pattern
```csharp
public async Task<string> SomeMethod(string input)
{
    _logger.LogMethodEntry(nameof(SomeMethod), new { input });
    
    try
    {
        // Business logic here
        var result = await ProcessData(input);
        
        _logger.LogMethodExit(nameof(SomeMethod), result);
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to process data", ex, nameof(SomeMethod));
        throw;
    }
}
```

### Error Handling
- Tất cả exceptions được log với stack trace
- UI errors được hiển thị user-friendly messages
- Database connection errors được handle gracefully
- API errors được retry với exponential backoff

## 📦 Backup và Restore

### Current Backups
- **SqlQueryParser Implementation** (2025-06-07 17:17:27)
  - Location: `.backups/SqlQueryParser_20250607_171727/`
  - Components: 4 service files, 4 test files, 1 documentation file
  - Total size: ~132KB
  - Purpose: Backup before production integration (Giai đoạn 2)

### Backup Structure
```
.backups/SqlQueryParser_YYYYMMDD_HHMMSS/
├── Services/                          # SqlQueryParser*.cs files
├── Tests/                             # SqlQueryParser*Tests.cs files  
├── Models/                            # Related model files
├── Documentation/                     # Common defects và docs
└── BACKUP_INFO.md                     # Backup metadata
```

### Restore Instructions
```bash
# Copy service files
copy .backups\SqlQueryParser_20250607_171727\Services\*.cs SqlTestDataGenerator.Core\Services\

# Copy test files  
copy .backups\SqlQueryParser_20250607_171727\Tests\*.cs SqlTestDataGenerator.Tests\

# Rebuild solution
dotnet build
```

## 🔒 Bảo mật

- **API keys** được lưu trong appsettings.json (không commit vào git)
- **Parameterized queries** để tránh SQL injection
- **Connection strings** được encrypt khi lưu user settings
- **Logs** không chứa sensitive data

## 📊 Performance

- **Async/await** cho tất cả I/O operations
- **Connection pooling** cho database connections
- **Memory management** với proper disposal patterns
- **Logging** với background flushing để không block UI

## 🤝 Đóng góp

1. Fork repository
2. Tạo feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

## 📞 Liên hệ

- **Email**: your-email@example.com
- **GitHub**: [@your-username](https://github.com/your-username)
- **LinkedIn**: [Your Name](https://linkedin.com/in/your-profile)

## 🙏 Acknowledgments

- [OpenAI](https://openai.com/) cho API tạo dữ liệu thông minh
- [Bogus](https://github.com/bchavez/Bogus) cho fake data generation
- [Dapper](https://github.com/DapperLib/Dapper) cho database access
- [ScintillaNET](https://github.com/jacobslusser/ScintillaNET) cho SQL editor 