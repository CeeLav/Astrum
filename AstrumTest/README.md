# AstrumTest - 测试项目使用指南

## 🎉 改进完成！

测试项目已经支持**运行单个测试用例**，不再需要全部测试都能编译才能运行！

---

## 🚀 快速开始

### 运行单个测试用例

```powershell
# Windows - 使用便捷脚本
cd d:\Develop\Projects\Astrum\AstrumTest
.\run-test.ps1 -TestName "GameNetworkMessage"

# 或者直接使用 dotnet test
dotnet test AstrumTest/AstrumTest/AstrumTest.csproj --filter "Name~GameNetworkMessage"
```

### 禁用编译失败的测试

```powershell
# 临时禁用过时的测试（不参与编译）
.\临时禁用测试.ps1 -TestFile "AstrumTest/OldTests.cs"

# 现在可以运行其他测试了！
.\run-test.ps1 -List
```

### 重新启用测试

```powershell
# 修复后重新启用
.\临时禁用测试.ps1 -TestFile "AstrumTest/OldTests.cs" -Enable
```

---

## 📁 项目结构

```
AstrumTest/
├── AstrumTest/
│   ├── AstrumTest.csproj          # 主测试项目
│   ├── Common/
│   │   └── ConfigFixture.cs       # 测试隔离基础设施
│   ├── ProtocolSerializationTests.cs  # 网络协议测试
│   ├── EntitySystemTests.cs       # 实体系统测试（原 UnitTest1.cs）
│   ├── *.disabled                  # 临时禁用的测试文件
│   └── *.bak                       # 备份的测试文件
│
├── run-test.ps1                    # 便捷运行脚本 (Windows)
├── run-test.sh                     # 便捷运行脚本 (Linux/Mac)
├── 临时禁用测试.ps1                # 测试文件管理脚本
│
├── 快速开始.md                      # 快速使用指南
├── 快速使用演示.md                  # 实战示例
├── 运行单个测试用例指南.md         # 详细的命令参考
├── 测试项目改进方案.md             # 完整的改进方案
└── 测试隔离解决方案.md             # 问题分析和解决方案
```

---

## 📋 常用命令

### 列出测试

```powershell
# 列出所有可用的测试
.\run-test.ps1 -List

# 或
dotnet test --list-tests
```

### 运行测试

```powershell
# 运行包含特定名称的测试
.\run-test.ps1 -TestName "Entity"

# 运行单元测试
.\run-test.ps1 -Category Unit

# 运行网络模块测试
.\run-test.ps1 -Module Network

# 详细输出
.\run-test.ps1 -TestName "SerializeDeserialize" -Verbose
```

### 管理测试

```powershell
# 禁用测试（临时）
.\临时禁用测试.ps1 -TestFile "AstrumTest/SomeTests.cs"

# 启用测试
.\临时禁用测试.ps1 -TestFile "AstrumTest/SomeTests.cs" -Enable
```

---

## 🎯 使用场景

### 场景 1: 开发新功能，只测试相关模块

```powershell
# 开发物理功能时
.\run-test.ps1 -Module Physics

# 开发网络功能时
.\run-test.ps1 -Module Network
```

### 场景 2: 某个测试编译失败，不影响其他测试

```powershell
# 1. 禁用编译失败的测试
.\临时禁用测试.ps1 -TestFile "AstrumTest/BrokenTests.cs"

# 2. 继续开发和测试其他功能
.\run-test.ps1 -Module Physics

# 3. 稍后修复并重新启用
.\临时禁用测试.ps1 -TestFile "AstrumTest/BrokenTests.cs" -Enable
```

### 场景 3: 调试单个测试

```powershell
# 运行单个测试，详细输出
.\run-test.ps1 -TestName "Test_FP_To_Fix64_Basic" -Verbose
```

### 场景 4: 提交代码前快速验证

```powershell
# 只运行单元测试（快速）
.\run-test.ps1 -Category Unit

# 如果通过，运行所有测试
dotnet test
```

---

## 🔧 已完成的改进

✅ **统一测试框架** - 使用 Xunit  
✅ **添加 Trait 标记** - 支持按类别/模块筛选  
✅ **创建 ConfigFixture** - 隔离测试状态  
✅ **转换 NUnit 测试** - 从 NUnit 迁移到 Xunit  
✅ **便捷运行脚本** - 一行命令运行测试  
✅ **测试管理脚本** - 方便禁用/启用测试  

---

## 📚 文档索引

- **[快速开始.md](./快速开始.md)** - 改进后的快速使用指南
- **[快速使用演示.md](./快速使用演示.md)** - 实战命令示例
- **[运行单个测试用例指南.md](./运行单个测试用例指南.md)** - 详细的过滤器语法
- **[测试项目改进方案.md](./测试项目改进方案.md)** - 完整的改进方案
- **[测试隔离解决方案.md](./测试隔离解决方案.md)** - 问题分析和多种解决方案

---

## 🐛 已知问题

### 当前已禁用的测试文件

- `SkillSystemTests.cs.disabled` - 使用过时的 API (SkillActions → SkillActionIds)
- `HitManagerTests.cs.disabled` - Entity.Id → Entity.UniqueId, 缺少 SetEntityId
- `TypeConverterTests.cs.disabled` - ToBepuMatrix → ToBepuMatrix3x3, TSMatrix.M44 不存在
- `*.bak` 文件 - 之前备份的测试

### 待修复的问题

1. **MemoryPack 序列化** - GameNetworkMessage 序列化失败
2. **过时的 API** - 部分测试使用了旧版 API
3. **缺少依赖** - 部分测试需要额外的配置或依赖

---

## 🎯 下一步计划

### 短期（今天）
- [x] 禁用编译失败的测试
- [x] 验证单个测试可以运行
- [ ] 修复 ProtocolSerializationTests

### 中期（1周内）
- [ ] 修复所有 .disabled 测试
- [ ] 添加更多 Trait 标记
- [ ] 完善测试覆盖率

### 长期（持续）
- [ ] 拆分为多个测试项目（可选）
- [ ] 建立 CI/CD 流水线
- [ ] 定期清理过时测试

---

## ✅ 效果总结

**改进前**：
❌ 一个测试过时 → 整个项目编译失败 → 无法运行任何测试

**改进后**：
✅ 一个测试过时 → 临时禁用这个测试 → 其他测试正常运行  
✅ 可以运行单个测试用例  
✅ 可以按模块/类别筛选  
✅ 开发时不受其他测试影响  

---

**享受高效、隔离的测试体验！** 🚀

