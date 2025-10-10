# 🚀 常用命令速查卡

## ⚡ 最常用命令

```powershell
# 进入测试目录
cd d:\Develop\Projects\Astrum\AstrumTest

# 列出所有测试
.\run-test.ps1 -List

# 运行包含特定名称的测试
.\run-test.ps1 -TestName "LoginRequest"

# 禁用编译失败的测试
.\临时禁用测试.ps1 -TestFile "AstrumTest/BrokenTest.cs"

# 重新启用测试
.\临时禁用测试.ps1 -TestFile "AstrumTest/BrokenTest.cs" -Enable
```

---

## 📋 按需求分类

### 运行测试

```powershell
# 按名称（模糊匹配）
.\run-test.ps1 -TestName "Serialize"
.\run-test.ps1 -TestName "HitManager"
.\run-test.ps1 -TestName "TypeConverter"

# 按类别
.\run-test.ps1 -Category Unit          # 单元测试
.\run-test.ps1 -Category Integration   # 集成测试

# 按模块
.\run-test.ps1 -Module Physics   # 物理模块
.\run-test.ps1 -Module Network   # 网络模块
.\run-test.ps1 -Module Skill     # 技能模块
.\run-test.ps1 -Module Entity    # 实体模块

# 详细输出
.\run-test.ps1 -TestName "LoginRequest" -Verbose
```

### 管理测试

```powershell
# 禁用测试（临时不参与编译）
.\临时禁用测试.ps1 -TestFile "AstrumTest/SkillSystemTests.cs"
.\临时禁用测试.ps1 -TestFile "AstrumTest/EntitySystemTests.cs"
.\临时禁用测试.ps1 -TestFile "AstrumTest/HitManagerTests.cs"

# 重新启用
.\临时禁用测试.ps1 -TestFile "AstrumTest/SkillSystemTests.cs" -Enable

# 查看禁用的测试
Get-ChildItem -Recurse -Filter "*.disabled"

# 查看备份的测试
Get-ChildItem -Recurse -Filter "*.bak"
```

---

## 🎯 原生 dotnet test 命令

### 基础命令

```bash
# 运行所有测试
dotnet test AstrumTest/AstrumTest/AstrumTest.csproj

# 列出所有测试
dotnet test AstrumTest/AstrumTest/AstrumTest.csproj --list-tests

# 编译但不运行
dotnet build AstrumTest/AstrumTest/AstrumTest.csproj
```

### 过滤器命令

```bash
# 完全限定名（精确匹配）
dotnet test --filter "FullyQualifiedName=AstrumTest.ProtocolSerializationTests.LoginRequest_SerializeDeserialize_ShouldWork"

# 名称模糊匹配
dotnet test --filter "Name~LoginRequest"
dotnet test --filter "Name~Serialize"

# 类名匹配
dotnet test --filter "FullyQualifiedName~ProtocolSerializationTests"

# Trait 筛选
dotnet test --filter "Category=Unit"
dotnet test --filter "Module=Network"
dotnet test --filter "Priority=High"

# 组合条件（AND）
dotnet test --filter "Category=Unit&Module=Physics"

# 组合条件（OR）
dotnet test --filter "Module=Physics|Module=Network"
```

### 高级选项

```bash
# 详细输出
dotnet test --logger "console;verbosity=detailed"

# 生成 TRX 报告
dotnet test --logger "trx;LogFileName=TestResults.trx"

# 不重新编译
dotnet test --no-build

# 并行运行（Xunit 默认）
dotnet test --parallel

# 禁用并行
dotnet test -- xUnit.ParallelizeAssembly=false

# 失败时停止
dotnet test -- xUnit.StopOnFail=true
```

---

## 🔍 过滤器语法速查

### 操作符

| 操作符 | 说明 | 示例 |
|--------|------|------|
| `=` | 完全匹配 | `Category=Unit` |
| `!=` | 不匹配 | `Category!=Integration` |
| `~` | 包含 | `Name~Login` |
| `!~` | 不包含 | `Name!~Slow` |
| `&` | 并且 | `Category=Unit&Module=Physics` |
| `\|` | 或者 | `Priority=High\|Priority=Critical` |

### 属性名

| 属性 | 说明 | 示例值 |
|------|------|--------|
| `FullyQualifiedName` | 完整测试名 | `Namespace.Class.Method` |
| `Name` | 方法名 | `Test_Something` |
| `ClassName` | 类名 | `TypeConverterTests` |
| `Namespace` | 命名空间 | `AstrumTest.PhysicsTests` |
| `Category` | 自定义分类 | `Unit`, `Integration` |
| `Module` | 模块名 | `Physics`, `Network` |
| `Priority` | 优先级 | `High`, `Medium`, `Low` |

---

## 💼 工作场景速查

### 开发新功能

```powershell
# 开发物理功能，只跑物理测试
.\run-test.ps1 -Module Physics

# 实时监听，代码改变自动运行
dotnet watch test --filter "Module=Physics"
```

### 调试问题

```powershell
# 运行单个测试，查看详细输出
.\run-test.ps1 -TestName "Test_FP_To_Fix64_Basic" -Verbose

# 或
dotnet test --filter "Name=Test_FP_To_Fix64_Basic" --logger "console;verbosity=detailed"
```

### 快速验证

```powershell
# 只运行高优先级测试
dotnet test --filter "Priority=High"

# 只运行单元测试（快）
.\run-test.ps1 -Category Unit
```

### 提交前检查

```powershell
# 1. 快速单元测试
.\run-test.ps1 -Category Unit

# 2. 如果通过，运行所有测试
dotnet test

# 3. 检查禁用的测试
Get-ChildItem -Recurse -Filter "*.disabled"
```

---

## 🛠️ 故障排除

### Q: 测试列表是空的？
```powershell
# 清理并重新编译
dotnet clean
dotnet build
dotnet test --list-tests
```

### Q: 找不到测试？
```bash
# 使用模糊匹配
dotnet test --filter "Name~PartialName"

# 而不是完全匹配
dotnet test --filter "Name=ExactFullName"
```

### Q: 测试被跳过？
```csharp
// 检查是否有 Skip 标记
[Fact(Skip = "...")]  // ❌ 会被跳过

// 改为
[Fact]  // ✅ 正常运行
```

### Q: 脚本无法执行？
```powershell
# Windows PowerShell 执行策略
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned

# 或者使用点运行
. .\run-test.ps1 -TestName "SomeTest"
```

---

## 📍 快速链接

- **详细文档**: 见 `README.md`
- **实战示例**: 见 `快速使用演示.md`
- **完整语法**: 见 `运行单个测试用例指南.md`
- **方案说明**: 见 `测试项目改进方案.md`

---

**提示**: 把这个速查卡收藏起来，日常开发必备！ 📌

