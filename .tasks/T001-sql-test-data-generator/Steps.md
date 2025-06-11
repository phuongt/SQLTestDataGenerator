# Task T001: SQL Test Data Generator Tool

## Mục tiêu
Xây dựng Windows Forms tool sinh dữ liệu test cho SQL queries bất kỳ, hỗ trợ nhiều CSDL và tích hợp AI.

## Checklist Chi Tiết

### Phase 1: Thiết lập Project & UI Foundation (Tuần 1)
- [ ] 1.1. Tạo solution .NET 8 Windows Forms
- [ ] 1.2. Cài đặt các NuGet packages cần thiết
- [ ] 1.3. Thiết kế MainForm UI với các controls:
  - ComboBox DB Type (SQL Server, MySQL, PostgreSQL, SQLite)
  - TextBox Connection String  
  - ScintillaNET SQL Editor với syntax highlighting
  - NumericUpDown số bản ghi
  - Button Generate & Run
  - DataGridView hiển thị kết quả
- [ ] 1.4. Test kết nối cơ bản đến các loại database
- [ ] 1.5. Lưu/đọc cấu hình local settings

### Phase 2: Data Access Layer & Engine Core (Tuần 2)  
- [ ] 2.1. Tạo DbConnectionFactory hỗ trợ multi-database
- [ ] 2.2. Implement SqlMetadataService đọc schema từ INFORMATION_SCHEMA
- [ ] 2.3. Tạo EngineService điều phối workflow chính
- [ ] 2.4. Implement DbExecutorService với Dapper và transaction support
- [ ] 2.5. Test query execution với rollback mechanism

### Phase 3: AI Integration & Data Generation (Tuần 3)
- [ ] 3.1. Tạo OpenAI client và prompt templates
- [ ] 3.2. Implement DataGenService với AI integration
- [ ] 3.3. Tạo Bogus fallback cho data generation
- [ ] 3.4. Test prompt engineering và JSON parsing
- [ ] 3.5. Implement retry mechanism với Polly

### Phase 4: Integration & Testing (Tuần 4)
- [ ] 4.1. Tích hợp tất cả components vào MainForm
- [ ] 4.2. Implement error handling và logging
- [ ] 4.3. Test với các SQL queries phức tạp
- [ ] 4.4. UI/UX improvements và validation
- [ ] 4.5. Browser testing các tính năng

### Phase 5: Hardening & Deployment (Tuần 5)
- [ ] 5.1. Security hardening (parameter validation, SQL injection prevention)
- [ ] 5.2. Performance optimization và quota limits
- [ ] 5.3. Unit tests cho core services
- [ ] 5.4. Packaging và MSI installer
- [ ] 5.5. Documentation và user guide

## Công nghệ Stack
- **UI**: Windows Forms .NET 8, ScintillaNET, FluentIcons
- **Data**: Dapper, Multi-database drivers, INFORMATION_SCHEMA
- **AI**: OpenAI .NET SDK, JSON response format
- **Utils**: Bogus, Polly, Serilog
- **Testing**: MCP Browser, Unit Tests

## Cấu trúc Architecture
```
UI Layer (MainForm) 
    ↓
Service Layer (EngineService)
    ↓  
├─ SqlMetadataService ──→ Database Schema
├─ DataGenService ──────→ OpenAI/Bogus  
└─ DbExecutorService ───→ Multi-DB Execute
```

## Rủi ro & Giải pháp
- **AI API failures**: Fallback to Bogus data generation
- **Database compatibility**: Factory pattern với interface abstraction  
- **SQL injection**: Parameterized queries và input validation
- **Performance**: Timeout, retry, và quota limits 