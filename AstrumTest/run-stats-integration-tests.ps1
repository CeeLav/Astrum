# ===================================================================
# 数值系统集成测试运行脚本
# ===================================================================
# 功能：运行所有数值系统相关的集成测试
# 作者：AI Assistant
# 日期：2025-10-15
# ===================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  数值系统集成测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 进入测试项目目录
Write-Host "[1/3] 进入测试项目目录..." -ForegroundColor Yellow
cd AstrumTest.Shared
if ($LASTEXITCODE -ne 0) { 
    Write-Error "无法进入 AstrumTest.Shared 目录"
    exit 1 
}
Write-Host "  ✓ 当前目录: $(Get-Location)" -ForegroundColor Green
Write-Host ""

# 清理并构建测试项目
Write-Host "[2/3] 构建测试项目..." -ForegroundColor Yellow
dotnet build --configuration Debug --verbosity minimal
if ($LASTEXITCODE -ne 0) { 
    Write-Error "测试项目构建失败"
    exit 1 
}
Write-Host "  ✓ 测试项目构建成功" -ForegroundColor Green
Write-Host ""

# 运行数值系统集成测试
Write-Host "[3/3] 运行数值系统集成测试..." -ForegroundColor Yellow
Write-Host ""

dotnet test `
    --filter "Module=StatsSystem&Category=Integration" `
    --logger "console;verbosity=detailed" `
    --logger "trx;LogFileName=stats-integration-test-results.trx" `
    --no-build

$testExitCode = $LASTEXITCODE

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

if ($testExitCode -eq 0) {
    Write-Host "  ✅ 所有数值系统集成测试通过！" -ForegroundColor Green
} else {
    Write-Host "  ❌ 部分测试失败！" -ForegroundColor Red
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 显示测试结果文件位置
$trxPath = Get-ChildItem -Path "TestResults" -Filter "*.trx" -Recurse | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1

if ($trxPath) {
    Write-Host "📊 测试报告: $($trxPath.FullName)" -ForegroundColor Cyan
}

# 返回到根目录
cd ..

exit $testExitCode

