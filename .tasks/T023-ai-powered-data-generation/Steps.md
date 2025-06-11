# Task T023: AI-Powered Data Generation with Engine-Driven Constraints

## Mục tiêu
Implement hybrid approach: **Engine extract constraints** + **AI generate meaningful data**

## Strategy
1. **Engine**: Parse SQL + DB metadata → extract ALL constraints/conditions
2. **AI**: Generate realistic data tuân thủ constraints
3. **Validation**: Ensure generated data meets ALL requirements

## Checklist Implementation

### 1. 🔧 Enhanced Engine Layer (100% Generic)
- [ ] **SQL Constraint Extractor**: Parse WHERE conditions, value patterns
- [ ] **DB Metadata Enricher**: Extract ENUM values, FK relationships, CHECK constraints
- [ ] **Condition Analyzer**: Understand business rules từ SQL
- [ ] **Context Builder**: Build comprehensive generation context

### 2. 🤖 AI Integration Layer  
- [ ] **AI Service Interface**: Define contract cho AI data generation
- [ ] **Constraint Translator**: Convert DB constraints → AI prompts
- [ ] **Context Formatter**: Format generation requests cho AI
- [ ] **Response Validator**: Verify AI output meets constraints

### 3. 🎯 Implementation Steps
- [ ] Create `AIDataGenerationService`
- [ ] Enhance `ConstraintExtractor` từ SQL
- [ ] Build `GenerationContext` model
- [ ] Implement constraint validation pipeline
- [ ] Test với TC001 Complex SQL

### 4. ✅ Success Criteria
- [ ] TC001 passes với meaningful data
- [ ] All DB constraints respected
- [ ] Generated data có business context
- [ ] No hardcoded column patterns
- [ ] Works với ANY SQL + ANY database

## Benefits
- **Intelligent**: AI generates realistic, contextual data
- **Compliant**: Engine ensures ALL constraints met
- **Generic**: Works với any database/SQL
- **Scalable**: Easy to extend cho complex scenarios 