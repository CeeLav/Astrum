# AstrumTest

> 🧪 测试项目 | Test Project

Astrum 项目的单元测试和集成测试。

---

## 🚀 快速开始

### 运行所有测试
```bash
cd AstrumTest/AstrumTest
dotnet test
```

### 运行特定测试类
```bash
# 技能效果集成测试
dotnet test --filter ClassName=SkillEffectIntegrationTests

# 物理系统测试
dotnet test --filter ClassName=HitManagerTests

# 类型转换测试
dotnet test --filter ClassName=TypeConverterTests
```

### 运行单个测试方法
```bash
dotnet test --filter FullyQualifiedName~SkillEffectIntegrationTests.Test_RealCombatScenario_TwoKnightsBasicAttack
```

---

## 📊 测试覆盖

| 测试类 | 测试数 | 状态 | 功能 |
|--------|--------|------|------|
| TypeConverterTests | 20 | ✅ | TrueSync ↔ BEPU 类型转换 |
| HitManagerTests | 15 | ✅ | 碰撞检测和命中管理 |
| EntityConfigIntegrationTests | 4 | ✅ | 实体配置和创建 |
| SkillEffectIntegrationTests | 5 | ✅ | 技能效果完整流程 |
| ProtocolSerializationTests | N | ✅ | 网络协议序列化 |

---

## 🔧 测试工具

### ConfigFixture
Xunit 共享配置类，用于初始化 ConfigManager

```csharp
[Collection("ConfigCollection")]
public class MyTests
{
    private readonly ConfigFixture _configFixture;
    
    public MyTests(ConfigFixture configFixture)
    {
        _configFixture = configFixture;
    }
}
```

---

## 📚 测试文档

详细的测试指南和说明，请查看：

- [测试快速开始](../Docs/07-Development%20开发指南/Test-Quick-Start%20测试快速开始.md)
- [测试常用命令](../Docs/07-Development%20开发指南/Test-Commands%20测试常用命令.md)
- [单个测试用例指南](../Docs/07-Development%20开发指南/Test-Single-Case%20单个测试用例指南.md)

---

## 🔗 相关链接

- [项目首页](../README.md)
- [开发指南](../Docs/07-Development%20开发指南/)
- [核心架构](../Docs/05-CoreArchitecture%20核心架构/)

---

**最后更新**: 2025-10-10
