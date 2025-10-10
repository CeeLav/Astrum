#!/usr/bin/env pwsh
<#
.SYNOPSIS
    快速运行单个或一组测试

.DESCRIPTION
    提供便捷的测试运行命令，支持各种过滤方式

.PARAMETER TestName
    测试名称（支持模糊匹配）

.PARAMETER Category
    测试类别（Unit, Integration, Performance 等）

.PARAMETER Module
    测试模块（Physics, Network, Skill 等）

.PARAMETER List
    列出所有测试，不运行

.PARAMETER Verbose
    显示详细输出

.EXAMPLE
    .\run-test.ps1 -TestName "GetSkillInfo"
    运行所有包含 GetSkillInfo 的测试

.EXAMPLE
    .\run-test.ps1 -Category Unit
    运行所有单元测试

.EXAMPLE
    .\run-test.ps1 -Module Physics
    运行物理模块的所有测试

.EXAMPLE
    .\run-test.ps1 -List
    列出所有可用的测试
#>

param(
    [string]$TestName,
    [ValidateSet("Unit", "Integration", "Performance", "")]
    [string]$Category,
    [ValidateSet("Physics", "Network", "Skill", "Entity", "")]
    [string]$Module,
    [switch]$List,
    [switch]$Verbose
)

# 切换到测试项目目录
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# 构建过滤器
$filter = ""

if ($TestName) {
    $filter = "FullyQualifiedName~$TestName"
}
elseif ($Category) {
    $filter = "Category=$Category"
}
elseif ($Module) {
    $filter = "Module=$Module"
}

# 构建命令参数
$testArgs = @()

if ($List) {
    $testArgs += "--list-tests"
}
else {
    if ($filter) {
        $testArgs += "--filter"
        $testArgs += $filter
    }
    
    if ($Verbose) {
        $testArgs += "--logger"
        $testArgs += "console;verbosity=detailed"
    }
}

# 显示运行信息
Write-Host "=" * 60 -ForegroundColor Cyan
if ($List) {
    Write-Host "列出所有测试" -ForegroundColor Green
}
else {
    if ($filter) {
        Write-Host "运行测试: $filter" -ForegroundColor Green
    }
    else {
        Write-Host "运行所有测试" -ForegroundColor Green
    }
}
Write-Host "=" * 60 -ForegroundColor Cyan

# 运行测试
$testCmd = "dotnet test"
if ($testArgs.Count -gt 0) {
    $testCmd += " " + ($testArgs -join " ")
}

Write-Host "> $testCmd" -ForegroundColor Yellow
Write-Host ""

Invoke-Expression $testCmd

# 显示退出码
if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ 测试通过" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "❌ 测试失败 (退出码: $LASTEXITCODE)" -ForegroundColor Red
}

exit $LASTEXITCODE

