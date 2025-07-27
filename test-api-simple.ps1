Write-Host "=== KIỂM TRA TRẠNG THÁI API GEMINI ===" -ForegroundColor Yellow

# API key từ config file test
$apiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"

Write-Host "API Key: $($apiKey.Substring(0, 10))..." -ForegroundColor Green

# Test API call
try {
    Write-Host "🔄 Đang gọi Gemini API..." -ForegroundColor Cyan
    
    $headers = @{
        'Content-Type' = 'application/json'
    }
    
    $body = @{
        contents = @(
            @{
                parts = @(
                    @{
                        text = "Hello, can you respond with just 'API is working'?"
                    }
                )
            }
        )
        generationConfig = @{
            temperature = 0.1
            maxOutputTokens = 50
        }
    } | ConvertTo-Json -Depth 10
    
    $url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=$apiKey"
    
    $response = Invoke-RestMethod -Uri $url -Method POST -Body $body -Headers $headers -TimeoutSec 30
    
    Write-Host "✅ API hoạt động bình thường!" -ForegroundColor Green
    Write-Host "📝 Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor White
}
catch {
    $errorMessage = $_.Exception.Message
    Write-Host "❌ API có vấn đề!" -ForegroundColor Red
    Write-Host "🔍 Error: $errorMessage" -ForegroundColor Red
    
    if ($errorMessage -match "quota|limit|exceeded" -or $_.ErrorDetails.Message -match "quota|limit|exceeded") {
        Write-Host "🚫 CÓ THỂ ĐÃ HẾT QUOTA API!" -ForegroundColor Magenta
    }
    
    if ($_.ErrorDetails) {
        Write-Host "📋 Error Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
}

Write-Host "`n=== KẾT THÚC KIỂM TRA ===" -ForegroundColor Yellow 