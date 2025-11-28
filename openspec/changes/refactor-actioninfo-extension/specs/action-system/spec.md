## ADDED Requirements

### Requirement: ActionInfo Extension 模式

ActionInfo SHALL 使用 Extension 字段存储动作类型的扩展数据，而非使用继承体系。

#### Scenario: 创建普通动作
- **WHEN** ActionConfig.GetAction 被调用，且 ActionType 为普通动作（非 "Move" 或 "Skill"）
- **THEN** 返回 ActionInfo 实例，MoveExtension 和 SkillExtension 均为 null
- **AND** 基础字段（Id, Catalog, Duration 等）已正确填充

#### Scenario: 创建移动动作
- **WHEN** ActionConfig.GetAction 被调用，且 ActionType 为 "Move"
- **THEN** 返回 ActionInfo 实例，MoveExtension 非空且包含 MoveSpeed 和 RootMotionData
- **AND** SkillExtension 为 null
- **AND** 基础字段已正确填充

#### Scenario: 创建技能动作
- **WHEN** ActionConfig.GetAction 被调用，且 ActionType 为 "Skill"
- **THEN** 返回 ActionInfo 实例，SkillExtension 非空且包含 ActualCost、ActualCooldown、TriggerFrames、TriggerEffects 和 RootMotionData
- **AND** MoveExtension 为 null
- **AND** 基础字段已正确填充

### Requirement: ActionInfo 便利访问方法

ActionInfo SHALL 提供类型安全的便利方法用于访问扩展数据。

#### Scenario: 获取移动速度
- **WHEN** 调用 actionInfo.GetBaseMoveSpeed()
- **THEN** 如果 MoveExtension 非空且 MoveSpeed > 0，返回转换后的 FP 值
- **ELSE** 返回 null

#### Scenario: 获取根节点位移数据
- **WHEN** 调用 actionInfo.GetRootMotionData()
- **THEN** 返回 MoveExtension 或 SkillExtension 中的 RootMotionData（优先 MoveExtension）
- **ELSE** 如果两个 Extension 都为 null 或 RootMotionData 为空，返回 null

#### Scenario: 获取技能消耗
- **WHEN** 调用 actionInfo.GetActualCost()
- **THEN** 如果 SkillExtension 非空，返回 ActualCost 值
- **ELSE** 返回 0

#### Scenario: 获取触发效果
- **WHEN** 调用 actionInfo.GetTriggerEffects()
- **THEN** 如果 SkillExtension 非空，返回 TriggerEffects 列表
- **ELSE** 返回空列表

### Requirement: ActionInfo 序列化支持

ActionInfo 及其 Extension 字段 SHALL 支持 MemoryPack 序列化和反序列化。

#### Scenario: 序列化包含 Extension 的 ActionInfo
- **WHEN** 序列化包含 MoveExtension 或 SkillExtension 的 ActionInfo
- **THEN** Extension 数据被正确序列化
- **AND** 反序列化后 Extension 数据完整恢复

#### Scenario: 序列化普通 ActionInfo
- **WHEN** 序列化 Extension 为 null 的 ActionInfo
- **THEN** 序列化成功
- **AND** 反序列化后 Extension 仍为 null

## REMOVED Requirements

### Requirement: ActionInfo 继承体系

**Reason**: 使用继承体系增加了序列化复杂度和类型检查开销，且 NormalActionInfo 无额外字段仅作为占位。

**Migration**: 
- NormalActionInfo、MoveActionInfo、SkillActionInfo 类已删除
- 所有使用这些类的代码应改为使用 ActionInfo 和 Extension 字段
- 类型检查（`is MoveActionInfo`）应改为 Extension 字段检查（`MoveExtension != null`）

### Requirement: MemoryPackUnion 多态序列化

**Reason**: ActionInfo 不再是抽象类，无需多态序列化支持。

**Migration**: 
- 移除所有 `[MemoryPackUnion]` 属性
- ActionInfo 作为单一具体类直接序列化

