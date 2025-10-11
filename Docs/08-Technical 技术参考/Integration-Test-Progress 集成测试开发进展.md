# 集成测试开发进展

**最后更新**: 2025-10-11  
**状态**: ✅ **第一阶段完成 - 测试框架可用**

---

## 📋 当前状态

### ✅ 已完成
1. **测试项目重构** - 创建 `AstrumTest.Shared` 项目
2. **核心框架代码** - `LogicTestExecutor` 完整实现
   - 固定流程：JSON加载 → Manager初始化 → Room/World创建 → 逐帧执行 → 结果验证
   - 帧驱动架构：每帧支持输入指令、查询指令、预期输出验证
   - 动态实体管理：CreateEntity/DestroyEntity 作为输入指令执行
3. **数据结构定义** - `TestCaseData`, `FrameData`, `InputCommand`, `QueryCommand`
4. **第一个测试场景** - `TwoKnightsFight.json` ✅ **已通过**
5. **测试集合配置** - `LogicTestCollection` 和 `ConfigFixture` 集成
6. **伤害计算验证** - 创建 `Integration-Test-DamageCalculation.md` 详细分析文档

### ✅ 已解决问题

#### MemoryPack 兼容性问题 ✓

**问题描述**:
```
System.TypeLoadException: Virtual static method 'Serialize' is not implemented on type 
'Astrum.LogicCore.Core.Entity/World' from assembly 'AstrumLogic'
```

**最终解决方案**: 
- 问题根源不是版本不兼容，而是**目标框架不匹配**
- Unity 使用：`MemoryPack.Core 1.21.4 for netstandard2.1`
- 测试项目误用：`MemoryPack 1.21.4 for net9.0` (从NuGet)

**修复方法**:
```xml
<!-- AstrumTest.Shared.csproj -->
<Reference Include="MemoryPack.Core">
  <HintPath>..\..\AstrumProj\Assets\Packages\MemoryPack.Core.1.21.4\lib\netstandard2.1\MemoryPack.Core.dll</HintPath>
</Reference>
```

**验证结果**: ✅ 测试完全通过
- Room 和 World 成功创建
- ArchetypeManager 正常初始化
- 实体创建、技能施放、伤害计算全部正常
- Exit code: 0

---

## 📊 测试结果

### TwoKnights_BasicFight ✅
```
✅ 已通过 - 失败: 0, 通过: 1, 持续时间: 260ms

Frame 0:  ✓ 创建两个骑士 (HP: 500, 500)
Frame 10: ✓ 骑士1施放技能 → 骑士2
Frame 30: ✓ 验证伤害 (500 → 492, -8点)
Frame 60: ✓ 最终状态验证
```

**伤害计算验证**:
- **预期伤害**: 7.9 ~ 8.7 点（不暴击）或 15.8 ~ 17.5 点（暴击20%）
- **实际伤害**: 8 点 ✓
- **计算公式**: `100 ATK × 10% × (1 - 16.67% DEF) × [0.95, 1.05] = 7.9~8.7`
- **结论**: **伤害逻辑完全正确** ✓

详细分析请参考：[Integration-Test-DamageCalculation.md](./Integration-Test-DamageCalculation.md)

---

## ⚠️ 发现的游戏逻辑问题

### 1. 硬编码属性值

**位置**: `AstrumProj/Assets/Script/AstrumLogic/SkillSystem/DamageCalculator.cs`

```csharp
// 当前使用的是硬编码临时值（简化版）
private static float GetEntityAttack(Entity entity)    => 100f;  // 配置表值：10.0
private static float GetEntityDefense(Entity entity)   => 20f;   // 配置表值：8.0
private static float GetEntityCritRate(Entity entity)  => 0.2f;  // 配置表值：0.1
private static float GetEntityCritDamage(Entity entity) => 2.0f;
```

**影响**: 
- 伤害计算未使用实体配置表数据
- 所有实体使用相同属性值

**优先级**: 🟡 中 (测试可以继续，但需要后续实现真实属性系统)

**解决方案**: 
1. 实现完整的属性系统 (StatComponent)
2. 从配置表读取实体属性值
3. 更新 `DamageCalculator` 使用真实属性

### 2. 反击技能未命中问题

**现象**: 
- Frame 60: 骑士2施放反击技能 → 骑士1
- Frame 90: 骑士1血量依然是 500（未受到伤害）

**可能原因**:
- ✅ 技能碰撞判定问题
- ⚠️ 技能朝向/距离问题
- ⚠️ Team 限制（只能攻击不同队伍？）
- ⚠️ 攻击动画/碰撞时机问题

**优先级**: 🔴 高 (影响战斗系统的双向攻击)

**下一步**: 需要调查技能系统的碰撞检测逻辑

---

## 🎯 测试框架特性

### 已实现功能

#### 1. 数据驱动测试
- JSON格式定义测试场景
- 自动加载和解析
- 支持多个测试用例并行运行

#### 2. 帧驱动架构
```json
{
  "frameNumber": 30,
  "inputs": [...],      // 输入指令
  "queries": [...],     // 查询指令
  "expectedOutputs": {} // 预期输出
}
```

#### 3. 支持的输入指令
- ✅ `CreateEntity` - 动态创建实体
- ✅ `DestroyEntity` - 销毁实体
- ✅ `CastSkill` - 施放技能
- ✅ `Move` - 移动
- ✅ `Teleport` - 瞬移
- ✅ `Wait` - 等待（无操作）

#### 4. 支持的查询指令
- ✅ `EntityHealth` - 查询生命值
- ✅ `EntityPosition` - 查询位置
- ✅ `EntityIsAlive` - 查询存活状态
- ✅ `EntityAction` - 查询当前动作
- ✅ `EntityDistance` - 查询两实体间距离

#### 5. 灵活的预期值验证
```json
{
  "knight_hp": 500,                    // 精确值
  "knight_hp": {"min": 482, "max": 493}, // 范围值
  "knight_alive": "True"               // 字符串比较
}
```

---

## 📂 文件结构

```
AstrumTest/
├── AstrumTest.Shared/
│   ├── Integration/
│   │   ├── Core/
│   │   │   ├── LogicTestExecutor.cs          # 核心测试引擎
│   │   │   ├── LogicTestExecutor.Executors.cs # 输入/查询执行器
│   │   │   └── TestCaseData.cs                # 数据结构定义
│   │   ├── Data/
│   │   │   └── Scenarios/
│   │   │       └── TwoKnightsFight.json       # 测试场景数据
│   │   ├── LogicTestCollection.cs             # xUnit 测试集合
│   │   └── IntegrationTests.cs                # 测试入口
│   ├── Fixtures/
│   │   └── ConfigFixture.cs                   # 配置初始化Fixture
│   └── AstrumTest.Shared.csproj
└── README-NEW-STRUCTURE.md
```

---

## 🔜 下一步计划

### 短期 (本周)
1. **扩展测试用例**:
   - ✅ 基础攻击测试（已完成）
   - 🔲 暴击测试用例（验证20%暴击率）
   - 🔲 双向攻击测试（修复反击问题后）
   - 🔲 移动测试
   - 🔲 距离验证测试

2. **调查反击问题**:
   - 🔲 检查技能碰撞检测代码
   - 🔲 验证Team限制逻辑
   - 🔲 测试不同距离的攻击

### 中期 (本月)
3. **游戏逻辑改进**:
   - 🔲 实现属性系统 (StatComponent)
   - 🔲 从配置表读取属性值
   - 🔲 修复反击技能问题

4. **更多测试场景**:
   - 🔲 技能CD测试
   - 🔲 多目标技能测试
   - 🔲 法师vs弓箭手场景
   - 🔲 持续伤害 (DOT) 测试

### 长期
5. **框架增强**:
   - 🔲 支持条件断言（if-then验证）
   - 🔲 支持循环场景（重复动作直到条件满足）
   - 🔲 支持并发测试（多个场景并行）
   - 🔲 性能测试支持（帧率、内存监控）

6. **工具改进**:
   - 🔲 JSON Schema 验证
   - 🔲 测试场景可视化编辑器
   - 🔲 测试报告HTML生成
   - 🔲 CI/CD集成

---

## 📝 使用指南

### 运行测试
```bash
# 运行所有集成测试
cd AstrumTest
dotnet test AstrumTest.Shared/AstrumTest.Shared.csproj --filter "TestLevel=Integration"

# 运行特定测试
dotnet test AstrumTest.Shared/AstrumTest.Shared.csproj --filter "FullyQualifiedName~TwoKnights_BasicFight"
```

### 创建新测试场景
1. 在 `Integration/Data/Scenarios/` 创建新的 JSON 文件
2. 定义实体模板和帧序列
3. 在 `IntegrationTests.cs` 添加测试方法或使用 `[Theory]` 数据驱动

### JSON 场景模板
```json
{
  "name": "测试场景名称",
  "description": "场景描述",
  "entityTemplates": [
    {
      "templateId": "entity_id",
      "roleId": 1001,
      "team": 1,
      "customHealth": 500
    }
  ],
  "frames": [
    {
      "frameNumber": 0,
      "comment": "帧说明",
      "inputs": [...],
      "queries": [...],
      "expectedOutputs": {...}
    }
  ]
}
```

---

## 🎓 经验总结

### 关键教训

1. **MemoryPack 跨项目引用**:
   - ⚠️ 确保目标框架一致（netstandard2.1 vs net9.0）
   - ✅ 直接引用 Unity 编译的 DLL，而不是 NuGet 包
   - ✅ 测试项目应该尽量使用与 Unity 相同的依赖版本

2. **测试数据设计**:
   - ✅ 基于帧的数据结构非常适合游戏逻辑测试
   - ✅ 范围验证 (`{"min": x, "max": y}`) 对于有随机性的系统很重要
   - ✅ 注释（comment）在调试时非常有用

3. **测试策略**:
   - ✅ 从简单场景开始（单次攻击）
   - ✅ 逐步增加复杂度（双向攻击、多实体、多技能）
   - ✅ 保持测试场景独立，避免状态泄漏

4. **框架设计**:
   - ✅ 固定流程减少用户误用
   - ✅ 数据驱动降低维护成本
   - ✅ 清晰的日志输出简化调试

---

## 📖 相关文档

- [集成测试框架设计方案](../../AstrumTest/集成测试框架设计方案.md)
- [伤害计算分析](./Integration-Test-DamageCalculation.md)
- [测试项目新结构说明](../../AstrumTest/README-NEW-STRUCTURE.md)
