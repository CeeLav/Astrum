# 受击与击退系统开发进展

**版本**: v1.0  
**创建日期**: 2025-01-08  
**状态**: 设计完成，待实现  

> 📖 **相关文档**：
> - [受击与击退设计](Hit-Reaction-And-Knockback%20受击与击退.md)
> - [事件队列开发进展](../../../05-CoreArchitecture%20核心架构/事件/Event-Queue-Progress%20事件队列开发进展.md)

---

## TL;DR

**目标**：实现击退效果和受击反馈系统，作为事件队列系统的第一个应用场景。

**进展**：✅ 设计完成，📋 待实现

**预估工时**：**10-14 小时**

**关键依赖**：
- ⚠️ 需要先完成事件队列系统（Phase 1-4）
- ✅ SkillDisplacementCapability 已存在，参考其实现

---

## 版本历史

| 版本 | 日期 | 变更内容 | 作者 |
|------|------|----------|------|
| v1.0 | 2025-01-08 | 初始版本，设计完成 | AI Assistant |

---

## 任务分解

### Phase 0: 前置准备 ⏳

**预估工时**: 1 小时

- [ ] **P0.1**: 确认事件队列系统已完成 Phase 1-4
- [ ] **P0.2**: 阅读 `SkillDisplacementCapability` 实现（参考移动输入禁用）
- [ ] **P0.3**: 确认 `CapabilitySystem.DisableCapabilitiesByTag` API
- [ ] **P0.4**: 确认 `SkillEffectSystem` 发布事件的位置

**验收标准**：
- 事件队列系统可用
- 清楚了解相关 API

---

### Phase 1: 数据结构 🔨

**预估工时**: 2-3 小时

#### 1.1 KnockbackComponent

- [ ] **P1.1.1**: 创建 `KnockbackComponent` 类
  - `bool IsKnockingBack`
  - `TSVector Direction`
  - `FP Speed`
  - `FP RemainingTime`
  - `FP TotalDistance`
  - `FP MovedDistance`
  - `KnockbackType Type`
  - `long CasterId`
- [ ] **P1.1.2**: 添加 MemoryPack 特性（如需要）

#### 1.2 KnockbackType 枚举

- [ ] **P1.2.1**: 创建 `KnockbackType` 枚举
  - `Linear` (线性)
  - `Decelerate` (减速)
  - `Launch` (抛射，可选)

#### 1.3 SkillEffectEvent（已存在，确认）

- [ ] **P1.3.1**: 确认 `SkillEffectEvent` 结构
  - `long CasterId`
  - `long TargetId`
  - `int EffectId`
  - `int TriggerFrame`

**验收标准**：
- 数据结构定义清晰
- 编译通过，无警告
- MemoryPack 序列化正常（如需要）

---

### Phase 2: KnockbackCapability 实现 🔨

**预估工时**: 3-4 小时

#### 2.1 基本结构

- [ ] **P2.1.1**: 创建 `KnockbackCapability` 类
  - `Priority = 150`
  - `Tags = [Movement, Combat]`
- [ ] **P2.1.2**: 添加 `_knockbackInstigatorId` 字段

#### 2.2 生命周期

- [ ] **P2.2.1**: 实现 `ShouldActivate(Entity entity)`
  - 检查 `KnockbackComponent.IsKnockingBack`
  - 检查 `PositionComponent` 存在
- [ ] **P2.2.2**: 实现 `ShouldDeactivate(Entity entity)`
  - 检查击退是否结束
- [ ] **P2.2.3**: 实现 `OnActivate(Entity entity)`
  - 禁用 `UserInputMovement` 标签
  - 记录 `_knockbackInstigatorId`
- [ ] **P2.2.4**: 实现 `OnDeactivate(Entity entity)`
  - 恢复 `UserInputMovement` 标签

#### 2.3 核心逻辑

- [ ] **P2.3.1**: 实现 `Tick(Entity entity)`
  - 获取 `KnockbackComponent` 和 `PositionComponent`
  - 调用 `CalculateMoveDistance()`
  - 应用位移
  - 更新 `MovedDistance` 和 `RemainingTime`
  - 检查结束条件，调用 `EndKnockback()`
- [ ] **P2.3.2**: 实现 `CalculateMoveDistance(KnockbackComponent, FP deltaTime)`
  - Linear: `speed * deltaTime`
  - Decelerate: 线性减速公式
- [ ] **P2.3.3**: 实现 `EndKnockback(KnockbackComponent)`
  - 设置 `IsKnockingBack = false`
  - 清零相关字段

**验收标准**：
- 击退位移正确应用
- 移动输入被正确禁用和恢复
- 击退结束时状态正确清理
- 编译通过，无运行时错误

---

### Phase 3: HitReactionCapability 实现 🔨

**预估工时**: 3-4 小时

#### 3.1 基本结构

- [ ] **P3.1.1**: 创建 `HitReactionCapability` 类
  - `Priority = 200`
  - `Tags = [Combat, Animation]`

#### 3.2 事件处理声明

- [ ] **P3.2.1**: 实现 `RegisterEventHandlers()`
  - 注册 `SkillEffectEvent` 处理器
- [ ] **P3.2.2**: 实现 `OnSkillEffect(Entity entity, SkillEffectEvent evt)`
  - 调用 `ProcessSkillEffect()`

#### 3.3 事件处理逻辑

- [ ] **P3.3.1**: 实现 `ProcessSkillEffect(Entity, SkillEffectEvent)`
  - 获取 `SkillEffectConfig`
  - 根据 `EffectType` 分发
    - 1: `ProcessDamage()` (TODO)
    - 2: `ProcessHeal()` (TODO)
    - 3: `ProcessKnockback()`
    - 4: `ProcessBuff()` (TODO)
    - 5: `ProcessDebuff()` (TODO)
- [ ] **P3.3.2**: 实现 `ProcessKnockback(Entity, SkillEffectEvent, SkillEffectConfig)`
  - 播放受击动作 `PlayHitAction()`
  - 播放受击特效 `PlayHitVFX()`
  - 获取或添加 `KnockbackComponent`
  - 获取施法者实体
  - 计算击退方向 `CalculateKnockbackDirection()`
  - 写入击退数据
    - `IsKnockingBack = true`
    - `Direction`
    - `TotalDistance = config.EffectValue`
    - `RemainingTime = config.EffectDuration`
    - `Speed = TotalDistance / RemainingTime`
    - `MovedDistance = 0`
    - `Type = Linear`
    - `CasterId`

#### 3.4 辅助方法

- [ ] **P3.4.1**: 实现 `CalculateKnockbackDirection(Entity caster, Entity target)`
  - 获取双方 `PositionComponent`
  - 计算方向向量（水平面）
  - 归一化
- [ ] **P3.4.2**: 实现 `PlayHitAction(Entity, long casterId)` (TODO，可先留空)
- [ ] **P3.4.3**: 实现 `PlayHitVFX(Entity, long casterId)` (TODO，可先留空)
- [ ] **P3.4.4**: 实现 `GetEffectConfig(int effectId)`
  - 从配置表获取

**验收标准**：
- 接收到技能效果事件
- 正确识别击退效果
- 击退数据正确写入 `KnockbackComponent`
- 编译通过，无运行时错误

---

### Phase 4: SkillEffectSystem 集成 🔧

**预估工时**: 1-2 小时

- [ ] **P4.1**: 确认 `SkillEffectSystem` 现有逻辑
- [ ] **P4.2**: 在合适位置调用 `World.EntityEventQueue.QueueEvent<SkillEffectEvent>()`
  - 构造 `SkillEffectEvent`
  - 指定 `TargetId`
  - 入队
- [ ] **P4.3**: 添加日志和调试信息

**验收标准**：
- 技能效果触发时正确发布事件
- 事件成功入队
- 日志输出正确

---

### Phase 5: CombatArchetype 集成 🔧

**预估工时**: 0.5-1 小时

- [ ] **P5.1**: 在 `CombatArchetype` 中添加 `KnockbackCapability`
- [ ] **P5.2**: 在 `CombatArchetype` 中添加 `HitReactionCapability`
- [ ] **P5.3**: 在 `CombatArchetype` 中添加 `KnockbackComponent`
- [ ] **P5.4**: 确认优先级顺序正确
  - `SkillExecutorCapability` (250)
  - `HitReactionCapability` (200)
  - `KnockbackCapability` (150)
  - `MovementCapability` (100)

**验收标准**：
- Archetype 正确配置
- Capability 初始化顺序正确
- 编译通过

---

### Phase 6: 测试和验证 ✅

**预估工时**: 2-3 小时

#### 6.1 单元测试

- [ ] **P6.1.1**: 测试 `KnockbackComponent` 数据结构
- [ ] **P6.1.2**: 测试 `CalculateMoveDistance()` 各种类型
  - Linear
  - Decelerate
- [ ] **P6.1.3**: 测试 `CalculateKnockbackDirection()`

#### 6.2 集成测试

- [ ] **P6.2.1**: 创建测试场景
  - 创建施法者和目标实体
  - 发布击退效果
  - 验证击退位移
  - 验证移动输入禁用
  - 验证击退结束后恢复
- [ ] **P6.2.2**: 测试边界情况
  - 击退中再次击退（后者覆盖）
  - 击退中实体销毁
  - 目标没有 PositionComponent

#### 6.3 性能测试

- [ ] **P6.3.1**: 测试多个实体同时击退（10+）
- [ ] **P6.3.2**: 测试击退开销（CPU profiling）

**验收标准**：
- 所有测试通过
- 击退表现符合预期
- 性能满足要求
- 无内存泄漏

---

## 依赖系统状态

| 系统 | 状态 | 说明 |
|------|------|------|
| EntityEventQueue | ⏳ 待实现 | 必须先完成 |
| CapabilitySystem.ProcessEntityEvents | ⏳ 待实现 | 必须先完成 |
| Capability 事件声明 | ⏳ 待实现 | 必须先完成 |
| SkillDisplacementCapability | ✅ 已存在 | 参考其移动输入禁用实现 |
| MovementCapability | ✅ 已存在 | 已支持 UserInputMovement 标签检查 |
| SkillEffectSystem | ✅ 已存在 | 需要添加事件发布 |

---

## 技术债务

- 📝 **受击动作和特效**: `PlayHitAction` 和 `PlayHitVFX` 暂时留空，需要后续补充
- 📝 **其他技能效果**: 伤害、治疗、Buff/Debuff 暂时留空，需要后续补充
- ⚠️ **击退打断**: 目前击退可以被新的击退覆盖，可能需要更复杂的打断逻辑
- 📝 **配置表扩展**: 击退类型、击退曲线等高级配置需要表格支持

---

## 关键决策

| 决策 | 理由 |
|------|------|
| 使用方案一（主动禁用） | 与 SkillDisplacementCapability 保持架构一致 |
| 使用静态事件声明 | 配合新的事件队列系统 |
| 击退优先级 150 | 高于普通移动，低于受击反应 |
| 击退类型支持 | 线性、减速，抛射可选（后续扩展） |

---

## 验收标准

### 功能完整性
- ✅ 技能效果触发时正确发布 SkillEffectEvent
- ✅ HitReactionCapability 正确接收事件
- ✅ 击退数据正确写入 KnockbackComponent
- ✅ KnockbackCapability 正确应用击退位移
- ✅ 击退期间禁用移动输入
- ✅ 击退结束后恢复移动输入

### 表现要求
- ✅ 击退方向正确（从施法者指向目标）
- ✅ 击退距离和时间符合配置
- ✅ 线性击退匀速
- ✅ 减速击退逐渐变慢

### 代码质量
- ✅ 编译通过，无警告
- ✅ 单元测试覆盖核心逻辑
- ✅ 代码符合项目规范
- ✅ 有清晰的注释和日志

---

## 相关文档

- [受击与击退设计](Hit-Reaction-And-Knockback%20受击与击退.md) - 技术方案
- [事件队列开发进展](../../../05-CoreArchitecture%20核心架构/事件/Event-Queue-Progress%20事件队列开发进展.md) - 依赖系统
- [技能位移能力](../../移动-位移/SkillDisplacementCapability%20技能位移能力.md) - 参考实现

