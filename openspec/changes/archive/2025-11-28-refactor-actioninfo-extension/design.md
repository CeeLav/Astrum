# Design: ActionInfo Extension 模式重构

## Context

当前架构：
- ActionInfo 是抽象基类，使用 MemoryPackUnion 支持多态序列化
- NormalActionInfo、MoveActionInfo、SkillActionInfo 继承自 ActionInfo
- ActionTable 包含所有动作的基础字段
- MoveActionTable 和 SkillActionTable 分别补充移动和技能专属字段
- ActionConfig 根据 ActionType 返回不同的派生类实例

问题：
- NormalActionInfo 无额外字段，仅作为占位
- 继承体系增加了序列化和类型检查的复杂度
- RootMotionData 在 MoveActionInfo 和 SkillActionInfo 中重复
- 未来扩展需要创建新派生类

## Goals / Non-Goals

### Goals
- 简化 ActionInfo 类结构，使用组合替代继承
- 保持表格分离，避免 ActionTable 字段膨胀
- 提供类型安全的扩展数据访问
- 支持 MemoryPack 序列化
- 便于未来扩展新的动作类型

### Non-Goals
- 不合并表格（ActionTable、MoveActionTable、SkillActionTable 保持分离）
- 不改变表格字段结构
- 不改变 ActionConfig 的加载逻辑（仍从多个表格读取）

## Decisions

### Decision 1: 使用 Extension 字段存储扩展数据

**选择**: ActionInfo 包含可空的 `MoveExtension?` 和 `SkillExtension?` 字段

**理由**:
- 组合优于继承，更灵活且易于扩展
- 类型安全，编译期检查
- 运行时根据 ActionType 决定哪个 Extension 非空
- MemoryPack 支持可空类型和嵌套对象序列化

**替代方案考虑**:
- 继续使用继承：增加序列化复杂度，难以扩展
- 使用 Dictionary<string, object>：失去类型安全
- 合并表格：会导致 ActionTable 字段膨胀

### Decision 2: 创建运行时 Extension 类

**选择**: 创建支持 MemoryPack 序列化的 `MoveActionExtension` 和 `SkillActionExtension` 类

**理由**:
- 编辑器已有 `MoveActionExtension` 和 `SkillActionExtension`（Unity 序列化），但运行时需要 MemoryPack 支持
- 保持字段结构一致，便于数据转换
- 支持序列化到网络和存档

**字段设计**:
- MoveActionExtension: MoveSpeed (int), RootMotionData (AnimationRootMotionData)
- SkillActionExtension: ActualCost (int), ActualCooldown (int), TriggerFrames (string), TriggerEffects (List<TriggerFrameInfo>), RootMotionData (AnimationRootMotionData)

### Decision 3: RootMotionData 处理

**选择**: 保持 RootMotionData 在各自的 Extension 中（MoveExtension 和 SkillExtension 都有）

**理由**:
- Move 和 Skill 动作都可能需要 RootMotion，但同一动作只会有一个 Extension 非空
- 通过便利方法 `GetRootMotionData()` 统一访问，内部检查两个 Extension
- 避免在基类添加字段，保持 ActionInfo 简洁

**替代方案考虑**:
- 提升到 ActionInfo 基类：会增加所有动作的内存占用，即使不需要 RootMotion 的动作也会分配

### Decision 4: 便利访问方法

**选择**: ActionInfo 提供类型安全的便利方法访问扩展数据

**理由**:
- 避免重复的空值检查代码
- 提供统一的访问接口
- 隐藏 Extension 字段的实现细节

**方法示例**:
- `GetBaseMoveSpeed(): FP?` - 从 MoveExtension 获取移动速度
- `GetRootMotionData(): AnimationRootMotionData?` - 从任一 Extension 获取根节点位移
- `GetActualCost(): int` - 从 SkillExtension 获取法力消耗
- `GetTriggerEffects(): List<TriggerFrameInfo>` - 从 SkillExtension 获取触发效果

### Decision 5: 移除 MemoryPackUnion

**选择**: ActionInfo 不再是抽象类，移除所有 `[MemoryPackUnion]` 属性

**理由**:
- 单一具体类，无需多态序列化
- 简化序列化逻辑
- 减少序列化开销

## Risks / Trade-offs

### Risk 1: 类型检查代码迁移

**风险**: 现有代码使用 `is MoveActionInfo` 等类型检查，需要全部迁移

**缓解措施**:
- 提供便利方法统一访问接口
- 通过 Catalog/ActionType 字符串判断动作类型
- 代码审查确保所有使用点都已更新

### Risk 2: 序列化兼容性

**风险**: 旧存档可能包含派生类序列化数据，新版本无法反序列化

**缓解措施**:
- 这是破坏性变更，需要版本迁移或明确说明不兼容
- 如果必须兼容，可以保留旧的 Union ID 但标记为废弃

### Risk 3: Extension 字段访问性能

**风险**: 可空字段访问可能比直接字段访问稍慢

**缓解措施**:
- 性能影响微乎其微（单次空值检查）
- 便利方法可以缓存常用值（如果性能成为瓶颈）

## Migration Plan

### 阶段 1: 创建运行时 Extension 类
1. 创建 `AstrumProj/Assets/Script/AstrumLogic/ActionSystem/MoveActionExtension.cs`
2. 创建 `AstrumProj/Assets/Script/AstrumLogic/SkillSystem/SkillActionExtension.cs`
3. 确保支持 MemoryPack 序列化

### 阶段 2: 修改 ActionInfo
1. 移除 `abstract` 关键字
2. 移除 `[MemoryPackUnion]` 属性
3. 添加 `MoveExtension?` 和 `SkillExtension?` 字段
4. 添加便利访问方法
5. 更新 MemoryPack 构造函数

### 阶段 3: 修改 ActionConfig
1. 统一 `GetAction` 方法返回 `ActionInfo`
2. 根据 ActionType 填充对应的 Extension
3. 移除 `ConstructNormalActionInfo`、`ConstructMoveActionInfo`、`ConstructSkillActionInfo` 方法

### 阶段 4: 更新使用代码
1. 修改 `ActionCapability.cs` 中的类型检查（`is MoveActionInfo` -> `MoveExtension != null`）
2. 修改 `TransViewComponent.cs` 中的类型检查（`GetType().Name == "SkillActionInfo"` -> `SkillExtension != null`）
3. 更新所有其他使用派生类的地方

### 阶段 5: 删除旧类
1. 删除 `NormalActionInfo.cs`
2. 删除 `MoveActionInfo.cs`
3. 删除 `SkillActionInfo.cs`

### 迁移注意事项
- 这是破坏性变更，所有使用 ActionInfo 派生类的代码都需要更新
- 确保所有类型检查都改为 Extension 字段检查或便利方法调用
- 测试序列化/反序列化功能

