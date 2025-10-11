# 集成测试框架开发进展

> 📅 创建时间: 2025-10-11  
> 🎯 目标: 实现基于帧的集成测试框架，完全数据驱动，测试 AstrumLogic 层
> 📖 设计文档: [集成测试框架设计方案](../../AstrumTest/集成测试框架设计方案.md)

---

## 一、开发阶段

### 阶段1：核心框架搭建 🔄 进行中

**目标**: 实现核心测试引擎，支持基本的逐帧测试流程

#### 1.1 数据结构定义 ⏳ 待开始

- [ ] `TestCaseData` - 测试用例数据结构
- [ ] `EntityTemplate` - 实体模板定义
- [ ] `FrameData` - 帧数据结构
- [ ] `InputCommand` - 输入指令
- [ ] `QueryCommand` - 查询指令

**文件**: `AstrumTest/AstrumTest.Shared/Integration/Core/TestCaseData.cs`

#### 1.2 核心测试引擎 ⏳ 待开始

- [ ] `LogicTestExecutor` - 测试执行引擎
  - [ ] `RunTestCase()` - 运行测试用例
  - [ ] `LoadTestCaseFromJson()` - 从JSON加载
  - [ ] `InitializeManagers()` - 初始化Managers
  - [ ] `CreateRoomAndWorld()` - 创建Room+World
  - [ ] `ExecuteFrames()` - 逐帧执行

**文件**: `AstrumTest/AstrumTest.Shared/Integration/Core/LogicTestExecutor.cs`

#### 1.3 输入指令执行器 ⏳ 待开始

- [ ] `CreateEntity` - 创建实体
- [ ] `DestroyEntity` - 销毁实体
- [ ] `CastSkill` - 释放技能
- [ ] `Move` - 移动
- [ ] `Teleport` - 传送

**方法**: `LogicTestExecutor.ExecuteInput()`

#### 1.4 查询指令执行器 ⏳ 待开始

- [ ] `EntityHealth` - 查询血量
- [ ] `EntityPosition` - 查询位置
- [ ] `EntityIsAlive` - 查询存活状态
- [ ] `EntityAction` - 查询当前动作
- [ ] `EntityDistance` - 查询距离

**方法**: `LogicTestExecutor.ExecuteQuery()`

#### 1.5 结果验证器 ⏳ 待开始

- [ ] 精确匹配验证
- [ ] 范围验证（min/max）
- [ ] 错误信息输出

**方法**: `LogicTestExecutor.VerifyExpectedOutputs()`

---

### 阶段2：测试用例编写 ⏳ 待开始

**目标**: 编写第一批测试用例，验证框架可用性

#### 2.1 第一个测试用例 ⏳ 待开始

- [ ] 创建 `TwoKnightsFight.json`
- [ ] 定义实体模板
- [ ] 编写5个关键帧测试点
- [ ] 验证框架运行

**文件**: `AstrumTest/AstrumTest.Shared/Data/Scenarios/TwoKnightsFight.json`

#### 2.2 测试类编写 ⏳ 待开始

- [ ] 创建 `IntegrationTests.cs`
- [ ] 使用 `LogicTestExecutor.RunTestCase()`
- [ ] 验证测试通过/失败

**文件**: `AstrumTest/AstrumTest.Shared/Integration/IntegrationTests.cs`

---

### 阶段3：功能扩展 ⏳ 待开始

**目标**: 扩展支持更多查询类型和输入类型

#### 3.1 更多输入类型

- [ ] `UseItem` - 使用物品
- [ ] `AddBuff` - 添加Buff
- [ ] `RemoveBuff` - 移除Buff

#### 3.2 更多查询类型

- [ ] `EntityBuffCount` - Buff数量
- [ ] `EntityVelocity` - 速度
- [ ] `RoomState` - Room状态

---

## 二、核心设计要点

### 2.1 以帧为单位

```plaintext
每一帧的执行流程：
  [1] 执行输入指令（inputs）      ← CreateEntity, CastSkill等
  [2] 更新游戏逻辑一帧            ← Room.Update()
  [3] 执行查询指令（queries）     ← 查询实体状态
  [4] 验证预期输出（expectedOutputs）← 断言验证
```

### 2.2 完全数据驱动

```csharp
// 用户代码（只需2行）
using var executor = new LogicTestExecutor(_output, _configFixture);
var result = executor.RunTestCase("TwoKnightsFight.json");
Assert.True(result.Success);
```

### 2.3 实体动态创建

- 实体通过 `CreateEntity` 输入指令在指定帧创建
- 支持运行时动态召唤（Frame 100创建新实体）
- 每个实体有唯一的字符串ID（"entity_0", "boss_1"）

---

## 三、实施计划

### 第1周：核心框架（当前）

- [x] 完成设计方案文档
- [ ] 实现数据结构类
- [ ] 实现核心测试引擎
- [ ] 实现基础输入/查询指令

### 第2周：测试用例

- [ ] 编写第一个测试用例
- [ ] 运行测试验证框架
- [ ] 修复发现的问题

### 第3周：功能扩展

- [ ] 添加更多输入/查询类型
- [ ] 优化错误信息输出
- [ ] 编写使用文档

---

## 四、技术难点与解决方案

### 4.1 实体ID映射

**问题**: 如何在测试中引用实体？

**解决方案**: 
```csharp
// 字符串ID映射
Dictionary<string, Entity> _entities;

// 创建时分配ID
_entities["entity_0"] = entity;

// 引用时使用ID
var entity = _entities["entity_0"];
```

### 4.2 帧同步模拟

**问题**: 如何模拟真实的帧同步？

**解决方案**:
```csharp
// 模拟单人模式：AuthorityFrame = PredictionFrame
LSController.AuthorityFrame = LSController.PredictionFrame;
Room.Update(0.016f);  // 60FPS
```

### 4.3 范围验证

**问题**: 伤害有随机性，如何验证？

**解决方案**:
```json
"expectedOutputs": {
  "knight2_hp": {"min": 400, "max": 470}  // 支持范围
}
```

---

## 五、测试覆盖计划

### 战斗系统

- [ ] 基础攻击
- [ ] 技能释放
- [ ] 多目标技能
- [ ] AOE技能
- [ ] Buff/Debuff

### 移动系统

- [ ] 基础移动
- [ ] 传送
- [ ] 冲刺
- [ ] 碰撞检测

### 物理系统

- [ ] 碰撞检测
- [ ] 击退
- [ ] 穿透

---

## 六、已知问题

### 6.1 MemoryPack 版本兼容性问题 ❌ 关键问题

**问题描述**：
```
TypeLoadException: Virtual static method 'Serialize' is not implemented on type 
'Astrum.LogicCore.Core.Entity/World' from assembly 'AstrumLogic.dll'
```

**原因分析**：
- Unity编译的DLL（AstrumLogic.dll, CommonBase.dll等）使用了旧版本的 MemoryPack
- 测试项目 `AstrumTest.Shared` 使用 MemoryPack 1.21.4
- 两个版本的接口不兼容（虚拟静态方法签名不同）

**影响范围**：
- 无法创建 `World` 对象（MemoryPack序列化错误）
- 无法创建 `Entity` 对象（MemoryPack序列化错误）
- 无法初始化 `ArchetypeManager`（触发Component序列化）

**解决方案**：

**方案A：Unity重新编译（推荐）**
1. 在Unity项目中升级 MemoryPack 到 1.21.4
2. 激活Unity编辑器，重新编译所有DLL
3. 测试项目引用新编译的DLL

**方案B：测试项目降级**
1. 降级测试项目的MemoryPack到Unity使用的版本
2. 需要找到Unity使用的MemoryPack确切版本

**方案C：使用源代码而非DLL（临时方案）**
1. 测试项目直接包含源代码文件
2. 排除所有依赖Unity的文件
3. 但会遇到Unity API依赖问题（Room, World依赖Unity）

**当前状态**：
- ✅ 框架代码已完成（LogicTestExecutor, TestCaseData等）
- ✅ JSON测试用例已创建（TwoKnightsFight.json）
- ✅ 测试类已创建（IntegrationTests.cs）
- ❌ 无法运行测试（MemoryPack版本冲突）

**测试输出**（部分成功）：
```
========================================
运行测试用例: TwoKnightsFight.json
========================================
  测试用例: 两个骑士对战
  实体模板数量: 2
  测试帧数: 5
    模板: knight1 (RoleId=1001)
    模板: knight2 (RoleId=1001)
[1/4] ✓ 读取测试用例: 两个骑士对战
  ⚠ ArchetypeManager 跳过初始化（MemoryPack 兼容性问题）
  ✓ ConfigManager already initialized
  ✓ SkillEffectManager initialized
  ✓ HitManager ready
[2/4] ✓ Manager 初始化完成
❌ 测试执行异常: MemoryPack版本冲突
```

---

## 七、下一步计划

1. ✅ 创建开发进展文档
2. ✅ 创建数据结构类
3. ✅ 实现核心测试引擎
4. ✅ 编写第一个测试用例
5. ❌ **解决 MemoryPack 版本兼容性问题** ← 关键阻塞
6. ⏳ 运行完整测试验证框架

---

## 八、参考资料

- [集成测试框架设计方案](../../AstrumTest/集成测试框架设计方案.md)
- [测试项目结构说明](../../AstrumTest/README-NEW-STRUCTURE.md)
- [单机模式文档](../09-GameModes 游戏模式/Single-Player 单机模式.md)

---

**最后更新**: 2025-10-11

