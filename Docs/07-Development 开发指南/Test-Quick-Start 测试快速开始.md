# 测试项目快速开始

## 🎉 改进完成！

测试项目已经完成以下改进：
- ✅ 统一使用 Xunit 测试框架
- ✅ 添加 ConfigFixture 隔离测试状态
- ✅ 支持 Trait 分类标记
- ✅ 提供便捷的运行脚本
- ✅ 转换 NUnit 测试为 Xunit

---

## 🚀 运行单个测试用例

### 方法 1: 使用便捷脚本（推荐）

#### Windows (PowerShell)

```powershell
# 进入测试目录
cd AstrumTest

# 运行包含特定名称的测试
.\run-test.ps1 -TestName "GetSkillInfo"

# 运行单元测试
.\run-test.ps1 -Category Unit

# 运行物理模块的测试
.\run-test.ps1 -Module Physics

# 列出所有测试
.\run-test.ps1 -List

# 详细输出
.\run-test.ps1 -TestName "TypeConverter" -Verbose
```

#### Linux/Mac (Bash)

```bash
# 进入测试目录
cd AstrumTest

# 运行包含特定名称的测试
./run-test.sh -n GetSkillInfo

# 运行单元测试
./run-test.sh -c Unit

# 运行物理模块的测试
./run-test.sh -m Physics

# 列出所有测试
./run-test.sh -l

# 详细输出
./run-test.sh -n TypeConverter -v
```

### 方法 2: 直接使用 dotnet test

```bash
# 运行特定测试方法
dotnet test --filter "FullyQualifiedName=AstrumTest.SkillSystemTests.GetSkillInfo_Level1_ShouldReturnSkillInfo"

# 运行包含特定名称的测试（模糊匹配）
dotnet test --filter "Name~GetSkillInfo"

# 运行整个测试类
dotnet test --filter "FullyQualifiedName~SkillSystemTests"

# 按类别运行
dotnet test --filter "Category=Unit"

# 按模块运行
dotnet test --filter "Module=Physics"

# 组合条件
dotnet test --filter "Category=Unit&Module=Physics"
```

---

## 📊 当前测试分类

### 按类别 (Category)
- **Unit** - 单元测试（快速、无依赖）
- **Integration** - 集成测试（较慢、有依赖）
- **Performance** - 性能测试

### 按模块 (Module)
- **Physics** - 物理系统测试
- **Skill** - 技能系统测试
- **Entity** - 实体系统测试
- **Network** - 网络系统测试

### 按优先级 (Priority)
- **High** - 高优先级（核心功能）
- **Medium** - 中优先级
- **Low** - 低优先级

---

## 📝 常用测试场景

### 场景 1: 开发新功能时

只运行相关模块的测试：

```bash
# 开发物理功能
.\run-test.ps1 -Module Physics

# 开发技能功能
.\run-test.ps1 -Module Skill
```

### 场景 2: 调试特定测试

运行单个测试，查看详细输出：

```bash
.\run-test.ps1 -TestName "Test_FP_To_Fix64_Basic" -Verbose
```

### 场景 3: 快速验证

只运行高优先级的单元测试：

```bash
dotnet test --filter "Category=Unit&Priority=High"
```

### 场景 4: 提交前检查

运行所有单元测试：

```bash
.\run-test.ps1 -Category Unit
```

### 场景 5: CI/CD 流水线

分批运行测试：

```bash
# 阶段 1: 快速单元测试
dotnet test --filter "Category=Unit"

# 阶段 2: 集成测试（如果阶段 1 通过）
dotnet test --filter "Category=Integration"
```

---

## 🎯 测试命名示例

现有的测试已经按照规范命名：

```csharp
// ✅ 好的命名
[Fact]
public void GetSkillInfo_Level1_ShouldReturnSkillInfo()

[Fact]
public void CreateByArchetype_ValidArchetype_ShouldCreateEntity()

[Fact]
public void Test_FP_To_Fix64_Basic()

// 格式：方法名_场景_预期结果
```

---

## 🔧 IDE 中运行测试

### Visual Studio 2022

1. **运行单个测试**：
   - 在测试方法上右键 → `Run Tests`
   - 或点击行号左侧的 ▶️ 图标

2. **使用 Test Explorer**：
   - `View` → `Test Explorer` (Ctrl+E, T)
   - 可以按类别、模块分组
   - 支持搜索和筛选

### JetBrains Rider

1. **运行单个测试**：
   - 点击测试方法左侧的 ▶️ 图标
   - 或右键 → `Run 'TestName'`

2. **使用 Unit Tests 窗口**：
   - `View` → `Tool Windows` → `Unit Tests`
   - 支持分组、搜索、筛选

### VS Code

1. **安装扩展**：
   - `.NET Core Test Explorer`

2. **运行测试**：
   - 侧边栏打开测试视图
   - 点击测试前的 ▶️ 图标

---

## 📈 测试覆盖率

查看测试覆盖率：

```bash
# 生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"

# 安装报告生成器
dotnet tool install -g dotnet-reportgenerator-globaltool

# 生成 HTML 报告
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport"
```

---

## 🐛 调试测试

### 方法 1: IDE 调试

1. 在测试代码中设置断点
2. 右键测试方法 → `Debug Test`

### 方法 2: 命令行调试

```bash
# 等待调试器附加
dotnet test --filter "Name~MyTest" --blame-hang-timeout 30m
```

---

## 💡 提示与技巧

### 1. 列出所有测试

```bash
# 查看有哪些测试可用
.\run-test.ps1 -List

# 或
dotnet test --list-tests
```

### 2. 查看测试时长

```bash
dotnet test --logger "console;verbosity=normal"
```

### 3. 生成测试报告

```bash
# TRX 格式（Visual Studio）
dotnet test --logger "trx;LogFileName=TestResults.trx"

# HTML 格式
dotnet test --logger "html;LogFileName=TestResults.html"
```

### 4. 并行执行

Xunit 默认并行执行不同类的测试，但同一 Collection 内的测试会串行执行。

```bash
# 禁用并行（如果需要）
dotnet test -- xUnit.ParallelizeAssembly=false
```

### 5. 失败时停止

```bash
# 遇到第一个失败就停止
dotnet test -- xUnit.StopOnFail=true
```

---

## 📚 相关文档

- [运行单个测试用例指南.md](./运行单个测试用例指南.md) - 详细的命令和过滤器语法
- [测试项目改进方案.md](./测试项目改进方案.md) - 完整的改进方案说明

---

## ✅ 改进效果

实施改进后，你现在可以：

1. ✅ **精确运行单个测试** - 不再需要运行全部测试
2. ✅ **测试互不干扰** - 使用 Fixture 隔离状态
3. ✅ **快速过滤测试** - 按类别、模块、名称筛选
4. ✅ **便捷的脚本** - 一行命令运行想要的测试
5. ✅ **清晰的分类** - Trait 标记让测试组织清晰

---

## 🎯 下一步

1. **添加更多测试** - 为新功能编写测试
2. **标记 Trait** - 为现有测试添加分类标记
3. **设置 CI/CD** - 在流水线中自动运行测试
4. **监控覆盖率** - 确保核心功能有足够的测试覆盖

---

**享受隔离、高效的测试体验！** 🎉

