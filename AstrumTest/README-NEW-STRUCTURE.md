# Astrum 测试项目 - 新结构

> 🧪 按部署单元和测试类型组织的测试框架

---

## 📁 项目结构

```plaintext
AstrumTest/
├── AstrumTest.Shared/          # 共享逻辑测试（最重要）
│   ├── Unit/                   # 单元测试
│   │   ├── Physics/           # 物理系统测试
│   │   ├── Components/        # 组件测试
│   │   ├── Serialization/     # 序列化测试
│   │   ├── ECS/              # ECS系统测试
│   │   └── Network/          # 网络基础测试
│   ├── Integration/           # 集成测试
│   │   ├── Physics/          # 物理集成测试
│   │   ├── Skill/            # 技能系统集成测试
│   │   └── Config/           # 配置系统集成测试
│   └── Fixtures/             # 测试基础设施
│       ├── ConfigFixture.cs
│       └── SharedTestScenario.cs
│
├── AstrumTest.Client/          # 客户端测试
│   ├── Unit/                  # 客户端单元测试
│   │   ├── Managers/         # 管理器测试
│   │   └── View/             # 视图层测试
│   └── Integration/          # 客户端集成测试
│
├── AstrumTest.Server/          # 服务器测试
│   ├── Unit/                  # 服务器单元测试
│   │   ├── Managers/         # 管理器测试
│   │   └── Handlers/         # 消息处理器测试
│   └── Integration/          # 服务器集成测试
│
└── AstrumTest.E2E/            # 端到端测试
    ├── LoginE2ETests.cs      # 登录流程测试
    ├── RoomE2ETests.cs       # 房间系统测试
    └── CombatE2ETests.cs     # 战斗流程测试
```

---

## 🚀 快速开始

### 运行所有测试

```bash
# 运行所有测试项目
.\run-test-new.ps1 -Project All

# 或分别运行
dotnet test AstrumTest.Shared
dotnet test AstrumTest.Client
dotnet test AstrumTest.Server
dotnet test AstrumTest.E2E
```

### 运行特定项目的测试

```bash
# 只运行共享代码测试（推荐优先运行）
.\run-test-new.ps1 -Project Shared

# 只运行客户端测试
.\run-test-new.ps1 -Project Client

# 只运行服务器测试
.\run-test-new.ps1 -Project Server

# 只运行端到端测试
.\run-test-new.ps1 -Project E2E
```

### 按类别运行

```bash
# 只运行共享代码的单元测试
.\run-test-new.ps1 -Project Shared -Category Unit

# 只运行共享代码的集成测试
.\run-test-new.ps1 -Project Shared -Category Integration
```

### 按模块运行

```bash
# 只运行物理模块的单元测试
.\run-test-new.ps1 -Project Shared -Category Unit -Module Physics

# 运行技能模块的集成测试
.\run-test-new.ps1 -Project Shared -Category Integration -Module Skill
```

---

## 📊 测试分类

### 按项目分类

| 项目 | 包含内容 | 测试对象 |
|------|----------|----------|
| **Shared** | 共享逻辑代码测试 | AstrumLogic, CommonBase, Network |
| **Client** | 客户端专属测试 | AstrumClient, AstrumView |
| **Server** | 服务器专属测试 | AstrumServer |
| **E2E** | 端到端测试 | 完整的客户端-服务器流程 |

### 按测试类型分类

| 类型 | 特点 | 运行速度 | 依赖 |
|------|------|----------|------|
| **Unit** | 单元测试 | 快速 | 最小依赖 |
| **Integration** | 集成测试 | 中等 | 多个模块 |
| **E2E** | 端到端测试 | 较慢 | 完整环境 |

---

## 🔧 项目依赖关系

```plaintext
AstrumTest.Shared
  ├── AstrumLogic.dll (Unity程序集)
  ├── CommonBase.dll (Unity程序集)
  └── Network.dll (Unity程序集)

AstrumTest.Client
  ├── AstrumTest.Shared (项目引用)
  ├── AstrumClient.dll (Unity程序集)
  └── AstrumView.dll (Unity程序集)

AstrumTest.Server
  ├── AstrumTest.Shared (项目引用)
  └── AstrumServer (项目引用)

AstrumTest.E2E
  ├── AstrumTest.Shared (项目引用)
  ├── AstrumTest.Client (项目引用)
  └── AstrumTest.Server (项目引用)
```

---

## 📝 编写测试指南

### 共享代码测试（最重要）

共享代码是核心逻辑，测试要求最严格：

```csharp
// AstrumTest.Shared/Unit/Physics/MyPhysicsTest.cs
using Xunit;
using TrueSync;

namespace AstrumTest.Shared.Unit.Physics
{
    [Trait("TestCategory", "Unit")]
    [Trait("Module", "Physics")]
    [Trait("Priority", "High")]
    public class MyPhysicsTest
    {
        [Fact]
        public void Test_PhysicsCalculation()
        {
            // 测试纯函数，无依赖
            var result = TSVector.Dot(TSVector.one, TSVector.up);
            Assert.Equal(FP.One, result);
        }
    }
}
```

### 集成测试示例

```csharp
// AstrumTest.Shared/Integration/Skill/SkillFlowTest.cs
using Xunit;
using AstrumTest.Shared.Fixtures;

namespace AstrumTest.Shared.Integration.Skill
{
    [Collection("Shared Test Collection")]
    [Trait("TestCategory", "Integration")]
    [Trait("Module", "Skill")]
    public class SkillFlowTest
    {
        private readonly SharedTestScenario _scenario;
        
        public SkillFlowTest(SharedTestScenario scenario)
        {
            _scenario = scenario;
        }
        
        [Fact]
        public void Test_SkillExecution_CompleteFlow()
        {
            // 使用预初始化的测试环境
            var caster = _scenario.EntityFactory.CreateEntity(1001, _scenario.World);
            var target = _scenario.EntityFactory.CreateEntity(1001, _scenario.World);
            
            // 执行技能
            // ...
            
            // 验证结果
            Assert.True(target.GetComponent<HealthComponent>().CurrentHealth < 100);
        }
    }
}
```

---

## 🎯 CI/CD 集成

推荐的 CI/CD 流程：

```yaml
# .github/workflows/test.yml
jobs:
  test-shared:
    name: 共享代码测试
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Test Shared
        run: dotnet test AstrumTest.Shared
  
  test-client:
    name: 客户端测试
    needs: test-shared
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Test Client
        run: dotnet test AstrumTest.Client
  
  test-server:
    name: 服务器测试
    needs: test-shared
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Test Server
        run: dotnet test AstrumTest.Server
  
  test-e2e:
    name: 端到端测试
    needs: [test-client, test-server]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Test E2E
        run: dotnet test AstrumTest.E2E
```

---

## 🆚 新旧结构对比

| 方面 | 旧结构 | 新结构 |
|------|--------|--------|
| **组织方式** | 单一项目 | 4个独立项目 |
| **职责划分** | 混合 | 按部署单元明确划分 |
| **编译隔离** | 无 | 完全隔离 |
| **运行速度** | 需运行全部 | 可按需运行 |
| **依赖管理** | 混乱 | 清晰的依赖关系 |
| **CI/CD** | 一次性全跑 | 分阶段并行 |

---

## 💡 最佳实践

1. **优先测试共享代码** - Shared 是核心，保证其质量最重要
2. **频繁运行单元测试** - 开发时持续运行单元测试
3. **定期运行集成测试** - 提交前运行相关集成测试
4. **谨慎运行 E2E 测试** - E2E 测试较慢，重大改动时运行

---

## 🔗 相关文档

- [旧测试结构 README](./README.md)
- [测试快速开始](../Docs/07-Development%20开发指南/Test-Quick-Start%20测试快速开始.md)
- [测试常用命令](../Docs/07-Development%20开发指南/Test-Commands%20测试常用命令.md)

---

**最后更新**: 2025-10-11  
**版本**: 2.0 (新测试结构)

