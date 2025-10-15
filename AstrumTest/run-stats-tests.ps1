# 数值系统测试运行脚本
# 用法: .\run-stats-tests.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " 数值系统测试套件" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 切换到测试目录
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location "$scriptPath\AstrumTest.Shared"

Write-Host "[1/3] 清理旧的测试构建..." -ForegroundColor Yellow
Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue

Write-Host "[2/3] 编译测试项目..." -ForegroundColor Yellow
dotnet build --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 编译失败！" -ForegroundColor Red
    exit 1
}

Write-Host "[3/3] 运行测试..." -ForegroundColor Yellow
Write-Host ""

# 运行数值系统和战斗系统的所有测试
dotnet test --filter "Module=StatsSystem | Module=CombatSystem" `
    --logger "console;verbosity=normal" `
    --logger "trx;LogFileName=stats-system-test-results.trx"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " ✅ 所有测试通过！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "测试报告: TestResults\stats-system-test-results.trx" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host " ❌ 部分测试失败" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
}

# 返回原目录
Set-Location $scriptPath

exit $LASTEXITCODE

