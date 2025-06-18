# =====================================================================
# Flash Model Rotation Test Script
# =====================================================================
# PURPOSE: Test and demonstrate Flash model rotation system
# USAGE: .\scripts\test-flash-model-rotation.ps1
# =====================================================================

param(
    [switch]$Verbose = $false,
    [switch]$ListModels = $false,
    [switch]$TestRotation = $false
)

# Colors for output
$script:Green = [System.ConsoleColor]::Green
$script:Red = [System.ConsoleColor]::Red
$script:Yellow = [System.ConsoleColor]::Yellow
$script:Cyan = [System.ConsoleColor]::Cyan
$script:Blue = [System.ConsoleColor]::Blue
$script:Magenta = [System.ConsoleColor]::Magenta

function Write-ColoredOutput {
    param(
        [string]$Message, 
        [System.ConsoleColor]$Color = [System.ConsoleColor]::White
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    $separator = "=" * 70
    Write-ColoredOutput $separator $Cyan
    Write-ColoredOutput " $Title" $Cyan
    Write-ColoredOutput $separator $Cyan
}

function Show-FlashModelList {
    Write-Section "GEMINI FLASH MODELS AVAILABLE FOR ROTATION"
    
    Write-ColoredOutput "🤖 Comprehensive List of Gemini Flash Models (2025)" $Blue
    Write-Host ""
    
    # Gemini 2.5 Flash (Latest)
    Write-ColoredOutput "📊 GEMINI 2.5 FLASH SERIES (Latest)" $Magenta
    Write-ColoredOutput "   ✅ gemini-2.5-flash-preview-05-20  - Latest với adaptive thinking" $Green
    Write-ColoredOutput "   ✅ gemini-2.5-flash-preview-04-17  - Hybrid reasoning model" $Green
    Write-Host ""
    
    # Gemini 2.0 Flash Series
    Write-ColoredOutput "📊 GEMINI 2.0 FLASH SERIES (Next-gen)" $Magenta
    Write-ColoredOutput "   ✅ gemini-2.0-flash                - Superior speed và capabilities" $Green
    Write-ColoredOutput "   ✅ gemini-2.0-flash-001            - Stable version" $Green
    Write-ColoredOutput "   ✅ gemini-2.0-flash-exp            - Experimental features" $Yellow
    Write-ColoredOutput "   ✅ gemini-2.0-flash-lite           - Cost efficient" $Blue
    Write-ColoredOutput "   ✅ gemini-2.0-flash-lite-001       - Lite stable version" $Blue
    Write-Host ""
    
    # Gemini 1.5 Flash Series
    Write-ColoredOutput "📊 GEMINI 1.5 FLASH SERIES (Proven)" $Magenta
    Write-ColoredOutput "   ✅ gemini-1.5-flash                - Fast và versatile" $Green
    Write-ColoredOutput "   ✅ gemini-1.5-flash-latest         - Most recent 1.5" $Green
    Write-ColoredOutput "   ✅ gemini-1.5-flash-001            - Stable v001" $Green
    Write-ColoredOutput "   ✅ gemini-1.5-flash-002            - Enhanced v002" $Green
    Write-ColoredOutput "   ✅ gemini-1.5-flash-8b             - Lightweight 8B" $Blue
    Write-ColoredOutput "   ✅ gemini-1.5-flash-8b-001         - 8B stable" $Blue
    Write-Host ""
    
    Write-ColoredOutput "📈 TOTAL MODELS AVAILABLE: 12" $Cyan
    Write-ColoredOutput "⏰ RATE LIMITING: 5 seconds between calls" $Yellow
    Write-ColoredOutput "🔄 ROTATION STRATEGY: Latest → Stable → Lite → Experimental" $Cyan
}

function Test-RotationLogic {
    Write-Section "TESTING FLASH MODEL ROTATION LOGIC"
    
    # Simulate rotation order
    $models = @(
        # Latest Tier
        @{ Name = "gemini-2.5-flash-preview-05-20"; Tier = "Latest"; Status = "Healthy" },
        @{ Name = "gemini-2.5-flash-preview-04-17"; Tier = "Latest"; Status = "Healthy" },
        
        # Stable Tier
        @{ Name = "gemini-2.0-flash"; Tier = "Stable"; Status = "Healthy" },
        @{ Name = "gemini-2.0-flash-001"; Tier = "Stable"; Status = "Healthy" },
        @{ Name = "gemini-1.5-flash"; Tier = "Stable"; Status = "Healthy" },
        @{ Name = "gemini-1.5-flash-latest"; Tier = "Stable"; Status = "Healthy" },
        @{ Name = "gemini-1.5-flash-001"; Tier = "Stable"; Status = "Healthy" },
        @{ Name = "gemini-1.5-flash-002"; Tier = "Stable"; Status = "Healthy" },
        
        # Lite Tier
        @{ Name = "gemini-2.0-flash-lite"; Tier = "Lite"; Status = "Healthy" },
        @{ Name = "gemini-2.0-flash-lite-001"; Tier = "Lite"; Status = "Healthy" },
        @{ Name = "gemini-1.5-flash-8b"; Tier = "Lite"; Status = "Healthy" },
        @{ Name = "gemini-1.5-flash-8b-001"; Tier = "Lite"; Status = "Healthy" },
        
        # Experimental Tier
        @{ Name = "gemini-2.0-flash-exp"; Tier = "Experimental"; Status = "Healthy" }
    )
    
    Write-ColoredOutput "🔄 Simulating 10 API calls với rotation logic..." $Blue
    Write-Host ""
    
    $tierPriority = @("Latest", "Stable", "Lite", "Experimental")
    $currentIndex = 0
    
    for ($i = 1; $i -le 10; $i++) {
        # Find next healthy model by tier priority
        $selectedModel = $null
        
        foreach ($tier in $tierPriority) {
            $healthyModelsInTier = $models | Where-Object { $_.Tier -eq $tier -and $_.Status -eq "Healthy" }
            
            if ($healthyModelsInTier.Count -gt 0) {
                $selectedModel = $healthyModelsInTier[$currentIndex % $healthyModelsInTier.Count]
                $currentIndex++
                break
            }
        }
        
        if ($selectedModel) {
            $tierColor = switch ($selectedModel.Tier) {
                "Latest" { $Magenta }
                "Stable" { $Green }
                "Lite" { $Blue }
                "Experimental" { $Yellow }
            }
            
            Write-ColoredOutput "📞 API Call #${i}: $($selectedModel.Name) (Tier: $($selectedModel.Tier))" $tierColor
            
            # Simulate random failure for demo
            if ((Get-Random -Minimum 1 -Maximum 10) -eq 5) {
                Write-ColoredOutput "   ❌ Simulated failure - marking model as unhealthy" $Red
                $models | Where-Object { $_.Name -eq $selectedModel.Name } | ForEach-Object { $_.Status = "Unhealthy" }
            } else {
                Write-ColoredOutput "   ✅ Success với 5s rate limit" $Green
            }
            
            Start-Sleep -Milliseconds 500  # Simulate delay
        }
    }
    
    Write-Host ""
    Write-ColoredOutput "📊 Model Health Status After Test:" $Cyan
    foreach ($tier in $tierPriority) {
        $modelsInTier = $models | Where-Object { $_.Tier -eq $tier }
        $healthyCount = ($modelsInTier | Where-Object { $_.Status -eq "Healthy" }).Count
        $totalCount = $modelsInTier.Count
        
        $tierColor = switch ($tier) {
            "Latest" { $Magenta }
            "Stable" { $Green }
            "Lite" { $Blue }
            "Experimental" { $Yellow }
        }
        
        Write-ColoredOutput "   $tier Tier: $healthyCount/$totalCount models healthy" $tierColor
    }
}

function Show-ImplementationGuide {
    Write-Section "IMPLEMENTATION GUIDE"
    
    Write-ColoredOutput "🛠️ How to Implement Flash Model Rotation:" $Blue
    Write-Host ""
    
    Write-ColoredOutput "1. UPDATE GeminiAIDataGenerationService.cs:" $Yellow
    Write-ColoredOutput "   ✅ Add _geminiFlashModels list với 12 models" $Green
    Write-ColoredOutput "   ✅ Implement GetNextFlashModel() với tier priority" $Green
    Write-ColoredOutput "   ✅ Add MarkModelAsFailed() để track health" $Green
    Write-ColoredOutput "   ✅ Update CallGeminiAPIAsync() với rotation logic" $Green
    Write-Host ""
    
    Write-ColoredOutput "2. UPDATE OpenAiService.cs:" $Yellow
    Write-ColoredOutput "   ✅ Replace hardcoded gemini-1.5-flash với rotation" $Green
    Write-ColoredOutput "   ✅ Add same rotation logic" $Green
    Write-Host ""
    
    Write-ColoredOutput "3. BENEFITS:" $Yellow
    Write-ColoredOutput "   🎯 Avoid rate limit concentration" $Green
    Write-ColoredOutput "   🎯 Automatic failover to healthy models" $Green
    Write-ColoredOutput "   🎯 5-second spacing between all calls" $Green
    Write-ColoredOutput "   🎯 Smart recovery after 10 minutes" $Green
    Write-ColoredOutput "   🎯 'Limit API AI' error khi all models fail" $Green
    Write-Host ""
    
    Write-ColoredOutput "4. ROTATION STRATEGY:" $Yellow
    Write-ColoredOutput "   🔄 Priority: Latest → Stable → Lite → Experimental" $Cyan
    Write-ColoredOutput "   🔄 Round-robin within each tier" $Cyan
    Write-ColoredOutput "   🔄 Skip unhealthy models automatically" $Cyan
    Write-ColoredOutput "   🔄 Reset failures every 10 minutes" $Cyan
}

# =====================================================================
# MAIN EXECUTION
# =====================================================================

Write-ColoredOutput "🤖 GEMINI FLASH MODEL ROTATION TESTING SYSTEM" $Cyan
Write-Host ""

if ($ListModels -or (!$TestRotation -and !$ListModels)) {
    Show-FlashModelList
}

if ($TestRotation) {
    Test-RotationLogic
}

if (!$TestRotation -and !$ListModels) {
    Show-ImplementationGuide
}

Write-Host ""
Write-ColoredOutput "✨ Ready to implement intelligent Flash model rotation!" $Green

# =====================================================================
# USAGE EXAMPLES:
# .\scripts\test-flash-model-rotation.ps1                    # Show all info
# .\scripts\test-flash-model-rotation.ps1 -ListModels        # Show models only
# .\scripts\test-flash-model-rotation.ps1 -TestRotation      # Test rotation logic
# ===================================================================== 