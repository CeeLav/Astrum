#!/usr/bin/env pwsh
<#
.SYNOPSIS
    新测试结构的测试运行脚本

.DESCRIPTION
    支持按测试项目、类别、模块运行测试

.PARAMETER Project
    测试项目（Shared, Client, Server, E2E, All）

.PARAMETER Category
    测试类别（Unit, Integration, All）

.PARAMETER Module
    测试模块（Physics, Network, Skill, Entity等）

.PARAMETER Verbose
    显示详细输出

.EXAMPLE
    .\run-test-new.ps1 -Project Shared
    运行所有共享代码测试

.EXAMPLE
    .\run-test-new.ps1 -Project Shared -Category Unit
    运行共享代码的单元测试

.EXAMPLE
    .\run-test-new.ps1 -Project Shared -Category Unit -Module Physics
    运行共享代码物理模块的单元测试
#>

param(
    [ValidateSet("Shared", "Client", "Server", "E2E", "All")]
    [string]$Project = "All",
    
    [ValidateSet("Unit", "Integration", "All")]
    [string]$Category = "All",
    
    [string]$Module,
    
    [switch]$Verbose
)

# 切换到测试根目录
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# 构建项目路径
$projects = @()
switch ($Project) {
    "Shared" { $projects += "AstrumTest.Shared" }
    "Client" { $projects += "AstrumTest.Client" }
    "Server" { $projects += "AstrumTest.Server" }
    "E2E" { $projects += "AstrumTest.E2E" }
    "All" { $projects += @("AstrumTest.Shared", "AstrumTest.Client", "AstrumTest.Server", "AstrumTest.E2E") }
}

# 构建过滤器
$filter = ""
if ($Category -ne "All") {
    $filter = "TestCategory=$Category"
}
if ($Module) {
    if ($filter) {
        $filter += "&Module=$Module"
    } else {
        $filter = "Module=$Module"
    }
}

# 运行测试
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "运行测试项目: $($projects -join ', ')" -ForegroundColor Green
if ($filter) {
    Write-Host "过滤条件: $filter" -ForegroundColor Yellow
}
Write-Host "=" * 60 -ForegroundColor Cyan

$totalSuccess = $true
foreach ($proj in $projects) {
    Write-Host ""
    Write-Host ">>> 测试项目: $proj" -ForegroundColor Magenta
    
    $testArgs = @()
    if ($filter) {
        $testArgs += "--filter"
        $testArgs += $filter
    }
    if ($Verbose) {
        $testArgs += "--logger"
        $testArgs += "console;verbosity=detailed"
    }
    
    $cmd = "dotnet test $proj"
    if ($testArgs.Count -gt 0) {
        $cmd += " " + ($testArgs -join " ")
    }
    
    Write-Host "> $cmd" -ForegroundColor Yellow
    Invoke-Expression $cmd
    
    if ($LASTEXITCODE -ne 0) {
        $totalSuccess = $false
    }
}

# 显示结果
Write-Host ""
Write-Host "=" * 60 -ForegroundColor Cyan
if ($totalSuccess) {
    Write-Host "✅ 所有测试通过" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ 部分测试失败" -ForegroundColor Red
    exit 1
}

