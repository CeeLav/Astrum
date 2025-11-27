# 运行单个测试用例指南

## 📋 概述

改进后的测试项目支持灵活的测试执行方式，可以精确控制运行哪些测试。

---

## 🎯 方法一：命令行运行单个测试

### 1. 运行特定的测试方法（完全限定名）

```bash
# 格式：命名空间.类名.方法名
dotnet test --filter "FullyQualifiedName=AstrumTest.SkillSystemTests.GetSkillInfo_Level1_ShouldReturnSkillInfo"

# 示例：运行物理测试中的单个用例
dotnet test --filter "FullyQualifiedName=AstrumTest.PhysicsTests.TypeConverterTests.Test_FP_To_Fix64_Basic"
```

### 2. 运行整个测试类的所有方法

```bash
# 格式：命名空间.类名
dotnet test --filter "FullyQualifiedName~AstrumTest.SkillSystemTests"

# 示例：运行所有类型转换测试
dotnet test --filter "FullyQualifiedName~AstrumTest.PhysicsTests.TypeConverterTests"
```

### 3. 使用通配符匹配

```bash
# 运行所有包含 "SkillSystem" 的测试
dotnet test --filter "FullyQualifiedName~SkillSystem"

# 运行所有以 "Test_FP" 开头的测试方法
dotnet test --filter "Name~Test_FP"

# 运行所有物理相关的测试
dotnet test --filter "FullyQualifiedName~Physics"
```

---

## 🎯 方法二：使用 Trait 分类运行

### 1. 为测试添加 Trait 标记

编辑测试文件，添加 `[Trait]` 特性：

```csharp
namespace AstrumTest.PhysicsTests
{
    [Trait("Category", "Unit")]          // 标记为单元测试
    [Trait("Module", "Physics")]          // 标记为物理模块
    public class TypeConverterTests
    {
        [Fact]
        [Trait("Priority", "High")]       // 高优先级测试
        public void Test_FP_To_Fix64_Basic()
        {
            // ...
        }
        
        [Fact]
        [Trait("Priority", "Low")]        // 低优先级测试
        [Trait("Performance", "true")]    // 性能测试
        public void Test_FP_Fix64_Performance()
        {
            // ...
        }
    }
}

namespace AstrumTest
{
    [Trait("Category", "Integration")]   // 标记为集成测试
    [Trait("Module", "Network")]
    public class ProtocolSerializationTests
    {
        // ...
    }
}
```

### 2. 按 Trait 运行测试

```bash
# 只运行单元测试
dotnet test --filter "Category=Unit"

# 只运行集成测试
dotnet test --filter "Category=Integration"

# 运行物理模块的测试
dotnet test --filter "Module=Physics"

# 运行高优先级测试
dotnet test --filter "Priority=High"

# 组合条件：物理模块的单元测试
dotnet test --filter "Module=Physics&Category=Unit"

# 或条件：物理模块或网络模块
dotnet test --filter "Module=Physics|Module=Network"
```

---

## 🎯 方法三：Visual Studio / Rider 中运行

### Visual Studio

1. **运行单个测试**：
   - 打开测试文件
   - 在测试方法上右键 → `Run Tests`
   - 或点击方法左侧的 ▶️ 图标

2. **运行整个类**：
   - 在类名上右键 → `Run Tests`

3. **使用 Test Explorer**：
   - `View` → `Test Explorer`
   - 可以搜索、筛选、分组运行测试

### JetBrains Rider

1. **运行单个测试**：
   - 在测试方法左侧点击 ▶️ 图标
   - 或右键方法名 → `Run 'TestName'`

2. **运行整个类/文件**：
   - 右键文件/类 → `Run Unit Tests`

3. **使用 Unit Tests 窗口**：
   - `View` → `Tool Windows` → `Unit Tests`
   - 支持搜索、分组、筛选

---

## 🎯 方法四：使用测试列表文件

### 1. 创建测试列表

创建 `test-list.txt`：

```
AstrumTest.SkillSystemTests.GetSkillInfo_Level1_ShouldReturnSkillInfo
AstrumTest.PhysicsTests.TypeConverterTests.Test_FP_To_Fix64_Basic
AstrumTest.PhysicsTests.TypeConverterTests.Test_FP_Fix64_RoundTrip
```

### 2. 使用列表运行

```bash
# PowerShell
Get-Content test-list.txt | ForEach-Object { 
    dotnet test --filter "FullyQualifiedName=$_" 
}

# Bash
while IFS= read -r test; do
    dotnet test --filter "FullyQualifiedName=$test"
done < test-list.txt
```

---

## 🎯 方法五：创建测试运行脚本

### Windows (PowerShell)

创建 `run-single-test.ps1`：

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$TestName
)

Write-Host "运行测试: $TestName" -ForegroundColor Green
dotnet test --filter "FullyQualifiedName~$TestName" --logger "console;verbosity=detailed"
```

使用：
```powershell
.\run-single-test.ps1 -TestName "GetSkillInfo_Level1"
```

### Linux/Mac (Bash)

创建 `run-single-test.sh`：

```bash
#!/bin/bash

if [ -z "$1" ]; then
    echo "用法: ./run-single-test.sh <测试名称>"
    exit 1
fi

echo "运行测试: $1"
dotnet test --filter "FullyQualifiedName~$1" --logger "console;verbosity=detailed"
```

使用：
```bash
chmod +x run-single-test.sh
./run-single-test.sh GetSkillInfo_Level1
```

---

## 📊 常用过滤器语法

### 操作符

| 操作符 | 说明 | 示例 |
|--------|------|------|
| `=` | 完全匹配 | `Category=Unit` |
| `!=` | 不匹配 | `Category!=Integration` |
| `~` | 包含（模糊匹配） | `FullyQualifiedName~Physics` |
| `!~` | 不包含 | `FullyQualifiedName!~Slow` |
| `&` | 并且（AND） | `Category=Unit&Module=Physics` |
| `|` | 或者（OR） | `Priority=High|Priority=Critical` |

### 属性名

| 属性 | 说明 | 示例 |
|------|------|------|
| `FullyQualifiedName` | 完整测试名称 | `Namespace.Class.Method` |
| `Name` | 方法名（不含命名空间） | `Test_Something` |
| `ClassName` | 类名 | `TypeConverterTests` |
| `Namespace` | 命名空间 | `AstrumTest.PhysicsTests` |
| `Category` | 自定义分类 | `Unit`, `Integration` |
| 其他 Trait | 任何自定义 Trait | `Priority`, `Module` 等 |

---

## 🚀 实用示例

### 开发时运行单个测试

```bash
# 只运行正在开发的测试
dotnet test --filter "Name~GetSkillInfo_Level1"
```

### 快速验证修改

```bash
# 运行相关的一组测试
dotnet test --filter "FullyQualifiedName~SkillSystem"
```

### CI/CD 中分批运行

```bash
# 先运行快速的单元测试
dotnet test --filter "Category=Unit" --logger "trx"

# 成功后运行慢速集成测试
dotnet test --filter "Category=Integration" --logger "trx"
```

### 调试时运行

```bash
# 运行单个测试，详细输出
dotnet test --filter "Name~Test_FP_To_Fix64_Basic" \
    --logger "console;verbosity=detailed" \
    --blame-hang-timeout 5m
```

### 查找失败的测试

```bash
# 运行测试并生成报告
dotnet test --logger "trx;LogFileName=results.trx"

# 从报告中提取失败的测试
# (需要解析 XML 报告)
```

---

## 💡 高级技巧

### 1. 创建测试别名 (PowerShell Profile)

编辑 `$PROFILE`：

```powershell
function Test-Single {
    param([string]$Name)
    dotnet test --filter "FullyQualifiedName~$Name"
}

function Test-Unit {
    dotnet test --filter "Category=Unit"
}

function Test-Integration {
    dotnet test --filter "Category=Integration"
}

# 使用别名
Set-Alias ts Test-Single
Set-Alias tu Test-Unit
Set-Alias ti Test-Integration
```

使用：
```powershell
ts GetSkillInfo  # 运行包含 GetSkillInfo 的测试
tu               # 运行所有单元测试
ti               # 运行所有集成测试
```

### 2. 监听模式（文件变化时自动运行）

```bash
# 安装 dotnet-watch
dotnet tool install -g dotnet-watch

# 监听模式运行测试
dotnet watch test --filter "Name~TypeConverter"
```

### 3. 并行运行多个测试

```bash
# 运行不同模块的测试（并行）
dotnet test --filter "Module=Physics" & \
dotnet test --filter "Module=Network" & \
wait
```

---

## 📝 推荐的测试命名规范

为了更好地利用过滤功能，建议采用统一的命名规范：

```csharp
// 格式：方法名_场景_预期结果
[Fact]
public void GetSkillInfo_ValidSkillId_ShouldReturnSkillInfo() { }

[Fact]
public void GetSkillInfo_InvalidSkillId_ShouldReturnNull() { }

[Fact]
public void GetSkillInfo_ZeroLevel_ShouldThrowException() { }
```

这样可以方便地使用通配符：

```bash
# 运行所有 GetSkillInfo 的测试
dotnet test --filter "Name~GetSkillInfo"

# 运行所有验证异常的测试
dotnet test --filter "Name~ThrowException"

# 运行所有返回 null 的测试
dotnet test --filter "Name~ShouldReturnNull"
```

---

## 🎓 学习资源

- [Xunit Documentation](https://xunit.net/)
- [.NET Test CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
- [VSTest Filter](https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests)

---

**提示**: 使用 `dotnet test --list-tests` 可以列出所有可用的测试，不实际运行。

