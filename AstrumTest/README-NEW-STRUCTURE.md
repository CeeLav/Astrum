# Astrum 测试项目 - 新结构

> 🧪 按部署单元和测试层级组织的测试框架

---

## 📁 项目结构

```plaintext
AstrumTest/
├── AstrumTest.Shared/          # 共享逻辑测试（最重要）
│   ├── Unit/                   # 单元测试 + 组件测试
│   │   ├── Physics/           # 物理系统测试
│   │   │   ├── TypeConverterTests.cs      [TestLevel=Unit]
│   │   │   └── HitManagerTests.cs         [TestLevel=Component]
│   │   ├── Serialization/     # 序列化测试
│   │   │   └── ProtocolSerializationTests.cs [TestLevel=Unit]
│   │   ├── Skill/             # 技能系统测试
│   │   ├── Config/            # 配置系统测试
│   │   ├── ECS/               # ECS系统测试
│   │   └── Network/           # 网络基础测试
│   ├── Integration/           # 集成测试（完整游戏流程）
│   │   └── [留空，等待真正的集成测试]
│   └── Fixtures/             # 测试基础设施
│       ├── ConfigFixture.cs
│       └── SharedTestScenario.cs
│
├── AstrumTest.Client/          # 客户端测试
│   ├── Unit/
│   └── Integration/
│
├── AstrumTest.Server/          # 服务器测试
│   ├── Unit/
│   └── Integration/
│
└── AstrumTest.E2E/            # 端到端测试（客户端-服务器）
```

---

## 🎯 测试层级定义

### TestLevel 分类

| TestLevel | 特点 | 依赖 | 速度 | 示例 |
|-----------|------|------|------|------|
| **Unit** | 纯函数，无外部依赖 | 无 | <10ms | TypeConverterTests |
| **Component** | 单个模块+少量依赖 | 单模块 | 10-100ms | HitManagerTests |
| **Integration** | 完整游戏流程 | 多模块 | 100ms+ | 完整战斗流程 |

### 使用 Trait 标记

```csharp
// 纯单元测试
[Trait("TestLevel", "Unit")]
[Trait("Category", "Unit")]
[Trait("Module", "Physics")]
public class TypeConverterTests { }

// 组件测试
[Trait("TestLevel", "Component")]
[Trait("Category", "Unit")]
[Trait("Module", "Physics")]
public class HitManagerTests { }

// 集成测试（未来）
[Trait("TestLevel", "Integration")]
[Trait("Category", "Integration")]
[Trait("Flow", "Combat")]
public class CombatFlowTests { }
```

---

## 🚀 运行测试

### 按测试层级运行

```bash
# 只运行纯单元测试（最快）
dotnet test AstrumTest.Shared --filter "TestLevel=Unit"

# 只运行组件测试
dotnet test AstrumTest.Shared --filter "TestLevel=Component"

# 运行单元 + 组件测试（推荐日常使用）
dotnet test AstrumTest.Shared --filter "TestLevel=Unit|TestLevel=Component"

# 运行完整流程集成测试（未来）
dotnet test AstrumTest.Shared --filter "TestLevel=Integration"
```

### 按项目运行

```bash
# 运行所有共享代码测试（推荐）
dotnet test AstrumTest.Shared

# 运行所有客户端测试
dotnet test AstrumTest.Client

# 运行所有服务器测试
dotnet test AstrumTest.Server

# 运行所有E2E测试
dotnet test AstrumTest.E2E
```

### 使用便捷脚本

```bash
# 运行共享代码测试
.\run-test-new.ps1 -Project Shared

# 运行所有项目测试
.\run-test-new.ps1 -Project All
```

---

## 📊 当前测试统计

### AstrumTest.Shared 测试覆盖

| 测试类 | TestLevel | 测试数 | 状态 | 功能 |
|--------|-----------|--------|------|------|
| TypeConverterTests | Unit | 20 | ✅ | TrueSync ↔ BEPU 类型转换 |
| ProtocolSerializationTests | Unit | 8 | ✅ | 网络协议序列化 |
| HitManagerTests | Component | 14 | ✅ | 碰撞检测和命中管理 |
| EntityConfigTests | Component | 4 | ✅ | 实体配置和创建 |
| SkillEffectTests | Component | 5 | ✅ | 技能效果处理 |

**总计**: 51个测试
- **Unit (纯单元)**: 28个测试
- **Component (组件)**: 23个测试  
- **Integration (流程)**: 0个测试（待创建）

---

## 🔧 项目依赖关系

```plaintext
AstrumTest.Shared
  ├── AstrumLogic (直接包含源代码)
  ├── CommonBase (直接包含源代码)
  ├── Generated (直接包含源代码)
  ├── Network.dll (Unity程序集)
  └── Luban.Runtime.dll (Unity程序集)

AstrumTest.Client
  ├── AstrumTest.Shared (项目引用)
  ├── AstrumClient.dll (Unity程序集)
  └── AstrumView.dll (Unity程序集)

AstrumTest.Server
  ├── AstrumTest.Shared (项目引用)
  └── AstrumServer.csproj (项目引用)

AstrumTest.E2E
  ├── AstrumTest.Shared (项目引用)
  ├── AstrumTest.Client (项目引用)
  └── AstrumTest.Server (项目引用)
```

---

## 💡 测试分类原则

### Unit（纯单元测试）
- ✅ 纯函数，无外部依赖
- ✅ 确定性输出
- ✅ 速度极快（<10ms）
- ✅ 示例：数学计算、类型转换、序列化

### Component（组件测试）
- ✅ 测试单个模块或系统
- ✅ 可以有少量依赖（如物理引擎、配置系统）
- ✅ 速度较快（10-100ms）
- ✅ 示例：碰撞检测、技能效果、实体创建

### Integration（集成测试）
- ✅ 测试完整的游戏流程
- ✅ 模拟真实游戏环境
- ✅ 多个系统协同工作
- ✅ 速度较慢（100ms+）
- ✅ 示例：完整战斗流程、房间匹配流程、登录流程

---

## 🎯 开发工作流

### 开发新功能时

```bash
# 1. 先写纯单元测试（TDD）
dotnet test --filter "TestLevel=Unit&Module=Physics"

# 2. 再写组件测试
dotnet test --filter "TestLevel=Component&Module=Physics"

# 3. 提交前运行所有相关测试
dotnet test AstrumTest.Shared --filter "Module=Physics"
```

### CI/CD 流水线

```yaml
# 分阶段运行
Stage 1: 纯单元测试 (最快，每次提交都跑)
  dotnet test --filter "TestLevel=Unit"
  
Stage 2: 组件测试 (中速，每次提交都跑)
  dotnet test --filter "TestLevel=Component"
  
Stage 3: 集成测试 (慢速，重要改动时跑)
  dotnet test --filter "TestLevel=Integration"
```

---

## 📚 当前测试文件

### Unit/Physics/
- `TypeConverterTests.cs` - FP/Fix64/TSVector 类型转换测试
- `HitManagerTests.cs` - 碰撞检测管理器测试

### Unit/Serialization/
- `ProtocolSerializationTests.cs` - MemoryPack 序列化测试

### Unit/Skill/ (空)
- 将来放置技能系统的组件测试

### Unit/Config/ (空)
- 将来放置配置系统的组件测试

### Integration/ (空)
- 留给真正的完整流程集成测试
- 如：完整战斗流程、房间匹配流程等

---

## 🔗 相关文档

- [旧测试结构 README](./README.md)
- [测试快速开始](../Docs/07-Development%20开发指南/Test-Quick-Start%20测试快速开始.md)

---

**最后更新**: 2025-10-11  
**版本**: 2.1 (重新分类测试层级)
