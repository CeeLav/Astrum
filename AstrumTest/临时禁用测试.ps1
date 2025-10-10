#!/usr/bin/env pwsh
<#
.SYNOPSIS
    临时禁用编译失败的测试文件

.DESCRIPTION
    将测试文件重命名为 .disabled，这样就不会参与编译
    需要时可以重新启用

.PARAMETER TestFile
    要禁用的测试文件路径（相对于 AstrumTest 目录）

.PARAMETER Enable
    启用之前禁用的测试文件

.EXAMPLE
    .\临时禁用测试.ps1 -TestFile "AstrumTest/SkillSystemTests.cs"
    禁用 SkillSystemTests.cs

.EXAMPLE
    .\临时禁用测试.ps1 -TestFile "AstrumTest/SkillSystemTests.cs" -Enable
    重新启用 SkillSystemTests.cs
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$TestFile,
    [switch]$Enable
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$fullPath = Join-Path $scriptDir $TestFile

if ($Enable) {
    # 启用测试
    $disabledPath = "$fullPath.disabled"
    if (Test-Path $disabledPath) {
        Move-Item -Path $disabledPath -Destination $fullPath -Force
        Write-Host "✅ 已启用测试: $TestFile" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  文件不存在: $disabledPath" -ForegroundColor Yellow
    }
}
else {
    # 禁用测试
    if (Test-Path $fullPath) {
        $disabledPath = "$fullPath.disabled"
        Move-Item -Path $fullPath -Destination $disabledPath -Force
        Write-Host "✅ 已禁用测试: $TestFile -> $TestFile.disabled" -ForegroundColor Yellow
        Write-Host "   (此测试不会参与编译，不影响其他测试)" -ForegroundColor Cyan
    }
    else {
        Write-Host "⚠️  文件不存在: $fullPath" -ForegroundColor Yellow
    }
}

