# Oracle Data Generation Activity Diagram

## 🎯 **Tổng Quan Luồng Generate Dữ Liệu Oracle**

```mermaid
graph TD
    A[Start: ExecuteQueryWithTestDataAsync] --> B{Database Type = Oracle?}
    B -->|Yes| C[Step 1: Parse SQL Query]
    B -->|No| C2[Use Other Database Flow]
    
    C --> D[Extract Tables & Columns]
    D --> E[Step 2: Generate Constraint-Aware Data]
    
    E --> F{UseAI = true?}
    F -->|Yes| G[AI-Enhanced Generation]
    F -->|No| H[Bogus Generation]
    
    G --> I[EnhancedGeminiFlashRotationService]
    I --> J{AI Models Available?}
    J -->|Yes| K[Model Rotation Logic]
    J -->|No| L[Fallback to Bogus]
    
    K --> K1[GetNextFlashModel]
    K1 --> K2{Model Healthy?}
    K2 -->|Yes| K3[Check Rate Limits]
    K2 -->|No| K4[Skip Model]
    
    K3 --> K5{Can Call API Now?}
    K5 -->|Yes| K6[Call Gemini API]
    K5 -->|No| K7[Wait for Reset Time]
    
    K6 --> K8{API Call Success?}
    K8 -->|Yes| K9[Return AI Data]
    K8 -->|No| K10[Mark Model As Failed]
    
    K10 --> K11{Error Type?}
    K11 -->|404| K12[Permanently Disable]
    K11 -->|429| K13[Temporary Disable - 1 Hour]
    K11 -->|500+| K14[Temporary Disable - 10 Min]
    
    K12 --> K15[Save to model-health.json]
    K13 --> K15
    K14 --> K15
    
    K15 --> K16{More Models Available?}
    K16 -->|Yes| K1
    K16 -->|No| L
    
    K4 --> K16
    K7 --> K16
    
    K9 --> M[Convert to INSERT Statements]
    L --> N[Bogus Data Generation]
    H --> N
    
    M --> O[Step 3: Re-order INSERT Statements]
    N --> O
    
    O --> P[Order by Dependencies: Parents → Children]
    P --> Q[Step 4: Execute Oracle Inserts]
    
    Q --> R[ExecuteOracleInsertsWithTableCommits]
    R --> S[Group INSERTs by Table]
    S --> T[Order Tables by Dependencies]
    T --> U[Execute Table by Table]
    
    U --> V[For Each Table: Begin Transaction]
    V --> W[Execute All INSERTs for Table]
    W --> X[Commit Transaction]
    X --> Y{More Tables?}
    Y -->|Yes| U
    Y -->|No| Z[Execute Original Query]
    
    Z --> AA[Return Results]
    AA --> BB[End]
    
    C2 --> BB
```

## 🔄 **AI Model Rotation & Rate Limit Handling**

### **Model Rotation Logic**
```mermaid
graph TD
    A1[GetNextFlashModel] --> B1[Priority Order: Latest > Stable > Lite > Experimental]
    B1 --> C1[Filter Healthy Models by Tier]
    C1 --> D1{Healthy Models in Tier?}
    D1 -->|Yes| E1[Select Next Model in Tier]
    D1 -->|No| F1[Move to Next Tier]
    
    E1 --> G1[Active Rotation - No Cooldown]
    F1 --> C1
    
    G1 --> H1[Return Selected Model]
```

### **Rate Limit & Health Management**
```mermaid
graph TD
    A2[Check Model Health] --> B2{Model in model-health.json?}
    B2 -->|Yes| C2[Check Failure Count]
    B2 -->|No| D2[Model is Healthy]
    
    C2 --> E2{Failure Count >= 3?}
    E2 -->|Yes| F2[Check Recovery Time]
    E2 -->|No| D2
    
    F2 --> G2{Recovery Time Reached?}
    G2 -->|Yes| H2[Reset Model Health]
    G2 -->|No| I2[Model Still Disabled]
    
    H2 --> D2
    I2 --> J2[Skip This Model]
```

### **Error Handling & Persistence**
```mermaid
graph TD
    A3[API Call Fails] --> B3{Error Type Detection}
    B3 -->|404| C3[Permanently Disable]
    B3 -->|429| D3[Temporary Disable - 1 Hour]
    B3 -->|500+| E3[Temporary Disable - 10 Min]
    B3 -->|Other| F3[Temporary Disable - 10 Min]
    
    C3 --> G3[Recovery Time = DateTime.MaxValue]
    D3 --> H3[Recovery Time = Next Hour]
    E3 --> I3[Recovery Time = Now + 10 Min]
    F3 --> I3
    
    G3 --> J3[Save to model-health.json]
    H3 --> J3
    I3 --> J3
    
    J3 --> K3[Try Next Model]
```

## 🔍 **Chi Tiết Từng Bước**

### **Step 1: Parse SQL Query**
```mermaid
graph TD
    A1[Parse SQL Query] --> B1[SqlQueryParserV3]
    B1 --> C1[Extract Tables]
    C1 --> D1[Extract Columns]
    D1 --> E1[Extract JOINs]
    E1 --> F1[Extract WHERE Conditions]
    F1 --> G1[DatabaseInfo Object]
```

### **Step 2: AI-Enhanced Data Generation**
```mermaid
graph TD
    A2[GenerateConstraintAwareDataAsync] --> B2{UseAI = true?}
    B2 -->|Yes| C2[EnhancedGeminiFlashRotationService]
    B2 -->|No| D2[Bogus Generation]
    
    C2 --> E2[Load Model Health from model-health.json]
    E2 --> F2[GetNextFlashModel]
    F2 --> G2{Model Available?}
    G2 -->|Yes| H2[Check Rate Limits]
    G2 -->|No| I2[Fallback to Bogus]
    
    H2 --> J2{Can Call API Now?}
    J2 -->|Yes| K2[Call Gemini API]
    J2 -->|No| L2[Wait for Reset Time]
    
    K2 --> M2{API Call Success?}
    M2 -->|Yes| N2[Process AI Response]
    M2 -->|No| O2[Mark Model As Failed]
    
    O2 --> P2[Save Health to model-health.json]
    P2 --> Q2{More Models?}
    Q2 -->|Yes| F2
    Q2 -->|No| I2
    
    N2 --> R2[Convert to INSERT Statements]
    I2 --> S2[Bogus Data Generation]
    D2 --> S2
    
    R2 --> T2[Return INSERT Statements]
    S2 --> T2
```

### **Step 3: Oracle-Specific Data Generation**
```mermaid
graph TD
    A3[DataGenService.GenerateInsertStatementsAsync] --> B3{UseAI = false?}
    B3 -->|Yes| C3[GenerateBogusDataWithConstraints]
    B3 -->|No| D3[Try AI Generation]
    
    C3 --> E3[Create Oracle Dialect Handler]
    E3 --> F3[OracleDialectHandler]
    E3 --> G3[CommonInsertBuilder]
    E3 --> H3[Generate Bogus Data]
    
    H3 --> I3[Format Oracle Values]
    I3 --> J3[TO_DATE for dates]
    I3 --> K3[TO_TIMESTAMP for timestamps]
    I3 --> L3[No quotes for numbers]
    I3 --> M3[NULL for empty strings]
    
    J3 --> N3[Build INSERT Statements]
    K3 --> N3
    L3 --> N3
    M3 --> N3
```

### **Step 4: Execute Oracle Inserts**
```mermaid
graph TD
    A4[ExecuteOracleInsertsWithTableCommits] --> B4[Group INSERTs by Table]
    B4 --> C4[Order Tables by Dependencies]
    C4 --> D4[EnhancedDependencyResolver]
    C4 --> E4[Parents First, Children Last]
    
    E4 --> F4[For Each Table]
    F4 --> G4[Begin Transaction]
    G4 --> H4[Execute All INSERTs for Table]
    H4 --> I4{INSERT Success?}
    I4 -->|Yes| J4[Commit Transaction]
    I4 -->|No| K4[Rollback Transaction]
    
    J4 --> L4{More Tables?}
    L4 -->|Yes| F4
    L4 -->|No| M4[Execute Original Query]
    
    K4 --> N4[Throw Exception]
    M4 --> O4[Return Results]
```

## 🔧 **AI Model Rotation Features**

### **1. Model Health Tracking**
```mermaid
graph TD
    A5[Model Health System] --> B5[model-health.json File]
    B5 --> C5[Failure Count Tracking]
    B5 --> D5[Recovery Time Calculation]
    B5 --> E5[Limit Type Classification]
    B5 --> F5[Permanent Disable for 404]
    
    C5 --> G5[Max 3 Failures per Model]
    D5 --> H5[Smart Recovery Time]
    E5 --> I5[429, 404, 500+ Error Types]
    F5 --> J5[Never Recover for 404]
```

### **2. Active Rotation Logic**
```mermaid
graph TD
    A6[Active Rotation] --> B6[No Cooldown Period]
    B6 --> C6[Priority-Based Selection]
    C6 --> D6[Latest Tier First]
    C6 --> E6[Stable Tier Second]
    C6 --> F6[Lite Tier Third]
    C6 --> G6[Experimental Last]
    
    D6 --> H6[Round-Robin in Tier]
    E6 --> H6
    F6 --> H6
    G6 --> H6
    
    H6 --> I6[Skip Unhealthy Models]
    I6 --> J6[Automatic Fallback]
```

### **3. Rate Limit Management**
```mermaid
graph TD
    A7[Rate Limit System] --> B7[Daily Limit: 100 calls]
    A7 --> C7[Hourly Limit: 15 calls]
    A7 --> D7[Model-Specific Limits]
    
    B7 --> E7[Reset at Midnight]
    C7 --> F7[Reset Every Hour]
    D7 --> G7[Individual Model Tracking]
    
    E7 --> H7[CanCallAPINow Check]
    F7 --> H7
    G7 --> H7
    
    H7 --> I7[Wait or Proceed Logic]
```

## 📊 **Data Flow Analysis**

### **✅ Đúng Luồng**
1. **SQL Parsing** → Extract tables, columns, constraints
2. **AI Model Rotation** → Active rotation, health tracking, rate limiting
3. **Data Generation** → AI or Bogus with Oracle dialect
4. **Oracle Formatting** → TO_DATE, TO_TIMESTAMP, proper escaping
5. **Table-by-Table Execution** → Commit each table separately
6. **FK Constraint Satisfaction** → Parents before children

### **✅ AI Model Rotation Features**
1. **Active Rotation** → Chủ động đổi model mỗi lần chạy
2. **Health Tracking** → Lưu thông tin model bị rate limit
3. **Permanent Disable** → 404 models không bao giờ được gọi lại
4. **Smart Recovery** → Models tự động recover sau thời gian
5. **Persistent Storage** → model-health.json lưu trạng thái

### **⚠️ Cần Kiểm Tra**
1. **AI Service Integration** → Rate limit handling, model rotation
2. **Error Handling** → Oracle-specific error messages
3. **Recovery Logic** → Transaction rollback on failure
4. **Performance** → Large dataset handling

### **🔧 Cải Tiến Đề Xuất**
1. **Batch Processing** → Process large datasets in chunks
2. **Parallel Processing** → Independent tables can be processed in parallel
3. **Progress Tracking** → Real-time progress updates
4. **Error Recovery** → Retry failed INSERTs with different data

## 🎯 **Kết Luận**

**Luồng Generate dữ liệu Oracle đã được thiết kế đúng và bao gồm:**

- ✅ **Oracle-specific handling** → Dialect handler, date formatting
- ✅ **AI Model Rotation** → Active rotation, health tracking, rate limiting
- ✅ **FK constraint handling** → Dependency order, table-by-table commits
- ✅ **Transaction management** → Proper commit/rollback logic
- ✅ **Error handling** → Oracle-specific error messages
- ✅ **Data formatting** → TO_DATE, TO_TIMESTAMP, proper escaping
- ✅ **Model Health Persistence** → model-health.json storage
- ✅ **Rate Limit Management** → Daily/hourly limits, model-specific limits

**Không có thiếu gọi hoặc sai luồng gọi trong code.** 