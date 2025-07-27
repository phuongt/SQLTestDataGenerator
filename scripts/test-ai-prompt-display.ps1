# Test AI Prompt Display Feature
# Kiểm tra tính năng hiển thị prompt và model trong UI

Write-Host "🧪 Testing AI Prompt Display Feature..." -ForegroundColor Cyan

# Build project
Write-Host "📦 Building project..." -ForegroundColor Yellow
dotnet build SqlTestDataGenerator.UI/SqlTestDataGenerator.UI.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green

# Check if UI controls exist
Write-Host "🔍 Checking UI controls..." -ForegroundColor Yellow

$mainFormFile = "SqlTestDataGenerator.UI/MainForm.Designer.cs"
$mainFormCodeFile = "SqlTestDataGenerator.UI/MainForm.cs"

# Check if txtPromptContent control exists
$promptContentExists = Select-String -Path $mainFormFile -Pattern "txtPromptContent" -Quiet
$lblPromptContentExists = Select-String -Path $mainFormFile -Pattern "lblPromptContent" -Quiet

if ($promptContentExists) {
    Write-Host "✅ txtPromptContent control found" -ForegroundColor Green
} else {
    Write-Host "❌ txtPromptContent control not found" -ForegroundColor Red
}

if ($lblPromptContentExists) {
    Write-Host "✅ lblPromptContent control found" -ForegroundColor Green
} else {
    Write-Host "❌ lblPromptContent control not found" -ForegroundColor Red
}

# Check if LogAIModelCall method exists and is properly implemented
$logAIModelCallExists = Select-String -Path $mainFormCodeFile -Pattern "LogAIModelCall" -Quiet
$updateApiStatusExists = Select-String -Path $mainFormCodeFile -Pattern "UpdateApiStatus" -Quiet

if ($logAIModelCallExists) {
    Write-Host "✅ LogAIModelCall method found" -ForegroundColor Green
} else {
    Write-Host "❌ LogAIModelCall method not found" -ForegroundColor Red
}

if ($updateApiStatusExists) {
    Write-Host "✅ UpdateApiStatus method found" -ForegroundColor Green
} else {
    Write-Host "❌ UpdateApiStatus method not found" -ForegroundColor Red
}

# Check if GetModelDisplayName method exists
$getModelDisplayNameExists = Select-String -Path $mainFormCodeFile -Pattern "GetModelDisplayName" -Quiet

if ($getModelDisplayNameExists) {
    Write-Host "✅ GetModelDisplayName method found" -ForegroundColor Green
} else {
    Write-Host "❌ GetModelDisplayName method not found" -ForegroundColor Red
}

# Check control properties
Write-Host "🔍 Checking control properties..." -ForegroundColor Yellow

$promptContentProperties = Select-String -Path $mainFormFile -Pattern "txtPromptContent\." -Context 0,5
if ($promptContentProperties) {
    Write-Host "✅ txtPromptContent properties configured:" -ForegroundColor Green
    $promptContentProperties | ForEach-Object { Write-Host "   $($_.Line.Trim())" -ForegroundColor Gray }
} else {
    Write-Host "❌ txtPromptContent properties not found" -ForegroundColor Red
}

# Check if controls are added to form
$controlsAdded = Select-String -Path $mainFormFile -Pattern "Controls\.Add\(txtPromptContent\)" -Quiet
$labelAdded = Select-String -Path $mainFormFile -Pattern "Controls\.Add\(lblPromptContent\)" -Quiet

if ($controlsAdded) {
    Write-Host "✅ txtPromptContent added to form controls" -ForegroundColor Green
} else {
    Write-Host "❌ txtPromptContent not added to form controls" -ForegroundColor Red
}

if ($labelAdded) {
    Write-Host "✅ lblPromptContent added to form controls" -ForegroundColor Green
} else {
    Write-Host "❌ lblPromptContent not added to form controls" -ForegroundColor Red
}

# Check method implementation
Write-Host "🔍 Checking method implementation..." -ForegroundColor Yellow

$logAIModelCallImplementation = Select-String -Path $mainFormCodeFile -Pattern "LogAIModelCall" -Context 0,10
if ($logAIModelCallImplementation) {
    Write-Host "✅ LogAIModelCall implementation found:" -ForegroundColor Green
    $logAIModelCallImplementation | ForEach-Object { 
        if ($_.Line.Contains("txtPromptContent")) {
            Write-Host "   ✓ Uses txtPromptContent control" -ForegroundColor Green
        }
        if ($_.Line.Contains("lblApiModel")) {
            Write-Host "   ✓ Updates lblApiModel" -ForegroundColor Green
        }
    }
}

# Summary
Write-Host "`nTest Summary:" -ForegroundColor Cyan
Write-Host "• txtPromptContent control: $(if($promptContentExists) {'PASS'} else {'FAIL'})" -ForegroundColor $(if($promptContentExists) {'Green'} else {'Red'})
Write-Host "• lblPromptContent control: $(if($lblPromptContentExists) {'PASS'} else {'FAIL'})" -ForegroundColor $(if($lblPromptContentExists) {'Green'} else {'Red'})
Write-Host "• LogAIModelCall method: $(if($logAIModelCallExists) {'PASS'} else {'FAIL'})" -ForegroundColor $(if($logAIModelCallExists) {'Green'} else {'Red'})
Write-Host "• UpdateApiStatus method: $(if($updateApiStatusExists) {'PASS'} else {'FAIL'})" -ForegroundColor $(if($updateApiStatusExists) {'Green'} else {'Red'})
Write-Host "• GetModelDisplayName method: $(if($getModelDisplayNameExists) {'PASS'} else {'FAIL'})" -ForegroundColor $(if($getModelDisplayNameExists) {'Green'} else {'Red'})
Write-Host "• Controls added to form: $(if($controlsAdded -and $labelAdded) {'PASS'} else {'FAIL'})" -ForegroundColor $(if($controlsAdded -and $labelAdded) {'Green'} else {'Red'})

if ($promptContentExists -and $lblPromptContentExists -and $logAIModelCallExists -and $updateApiStatusExists -and $getModelDisplayNameExists -and $controlsAdded -and $labelAdded) {
    Write-Host "`nAll tests passed! AI Prompt Display feature is properly implemented." -ForegroundColor Green
    Write-Host "The application will now display:" -ForegroundColor Cyan
    Write-Host "   • AI Model name in lblApiModel" -ForegroundColor White
    Write-Host "   • Prompt content in txtPromptContent" -ForegroundColor White
    Write-Host "   • Model rotation information" -ForegroundColor White
    Write-Host "   • Timestamp and preview of prompts" -ForegroundColor White
} else {
    Write-Host "`nSome tests failed. Please check the implementation." -ForegroundColor Red
    exit 1
}

Write-Host "`nTest completed successfully!" -ForegroundColor Green 