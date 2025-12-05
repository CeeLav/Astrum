## MODIFIED Requirements

### Requirement: Capability 性能优化

Capability 系统 SHALL 在热路径中避免不必要的对象分配，实现接近 0 GC 的性能目标。

#### Scenario: SkillExecutorCapability 零 GC 优化

- **WHEN** SkillExecutorCapability 在每帧处理技能触发逻辑时
- **THEN** 系统 SHALL 使用预分配缓冲区和对象复用，避免 LINQ ToList() 和临时对象分配
- **AND** GC 分配 SHALL < 1 KB/帧
- **AND** GC.Alloc 次数 SHALL < 50 次/帧

#### Scenario: 预分配缓冲区复用

- **WHEN** Capability 需要临时集合存储中间结果时
- **THEN** 系统 SHALL 使用实例字段的预分配缓冲区
- **AND** 每次使用前 SHALL 调用 Clear() 清空缓冲区
- **AND** 不 SHALL 释放缓冲区的容量

#### Scenario: 避免 LINQ 操作

- **WHEN** Capability 需要过滤或转换集合时
- **THEN** 系统 SHALL 使用 for 循环替代 LINQ 操作
- **AND** 不 SHALL 使用 Where()、Select()、ToList() 等产生临时集合的方法
- **AND** 不 SHALL 使用 foreach 循环（可能产生枚举器 GC）

#### Scenario: 对象复用

- **WHEN** Capability 需要频繁创建和销毁对象时
- **THEN** 系统 SHALL 使用实例字段复用对象
- **AND** 每次使用前 SHALL 重置对象状态
- **AND** 对于高频对象（> 20 次/帧），SHALL 考虑使用对象池

#### Scenario: 性能监控

- **WHEN** 优化 Capability 性能时
- **THEN** 系统 SHALL 使用 ProfileScope 监控关键方法的性能
- **AND** 系统 SHALL 使用 Unity Profiler 验证 GC 分配和耗时
- **AND** 优化后 SHALL 确保游戏逻辑行为完全一致

## ADDED Requirements

### Requirement: SkillExecutorCapability 预分配缓冲区

SkillExecutorCapability SHALL 使用预分配缓冲区存储当前帧的触发事件，避免每帧创建新 List。

#### Scenario: 触发事件缓冲区

- **WHEN** ProcessFrame() 需要过滤当前帧的触发事件时
- **THEN** 系统 SHALL 使用 `_triggerBuffer` 实例字段存储结果
- **AND** 缓冲区初始容量 SHALL 为 16
- **AND** 每次使用前 SHALL 调用 Clear() 清空缓冲区
- **AND** 系统 SHALL 使用 for 循环手动过滤，而非 LINQ Where()

### Requirement: SkillExecutorCapability CollisionFilter 复用

SkillExecutorCapability SHALL 复用 CollisionFilter 对象，避免每次碰撞检测时创建新对象和 HashSet。

#### Scenario: CollisionFilter 复用

- **WHEN** HandleCollisionTrigger() 需要执行碰撞检测时
- **THEN** 系统 SHALL 使用 `_collisionFilter` 实例字段
- **AND** 每次使用前 SHALL 清空 ExcludedEntityIds
- **AND** 系统 SHALL 添加当前施法者 ID 到 ExcludedEntityIds
- **AND** 不 SHALL 创建新的 CollisionFilter 或 HashSet 对象

### Requirement: SkillExecutorCapability VFX 事件对象池

当 VFX 触发频率 > 20 次/帧时，SkillExecutorCapability SHALL 使用对象池管理 VFX 事件对象。

#### Scenario: VFX 事件对象池

- **WHEN** ProcessVFXTrigger() 需要创建 VFX 事件且触发频率 > 20 次/帧时
- **THEN** 系统 SHALL 从对象池获取 VFXTriggerEventData 和 VFXTriggerEvent
- **AND** 使用完后 SHALL 归还到对象池
- **AND** 对象 SHALL 实现 IPool 接口
- **AND** 对象 SHALL 提供 Reset() 方法清空所有字段

#### Scenario: VFX 触发频率评估

- **WHEN** 决定是否实施 VFX 对象池时
- **THEN** 系统 SHALL 使用 Unity Profiler 测量实际触发频率
- **AND** 如果频率 < 20 次/帧，系统 MAY 跳过对象池实施
- **AND** 如果频率 > 20 次/帧，系统 SHALL 实施对象池优化

