# Change: 重构 ActionInfo 使用 Extension 模式

## Why

当前 ActionInfo 使用继承体系（ActionInfo -> NormalActionInfo/MoveActionInfo/SkillActionInfo）来区分不同类型的动作。这种方式存在以下问题：

1. **类膨胀**：NormalActionInfo 没有任何额外字段，仅作为占位类存在
2. **继承复杂性**：需要 MemoryPackUnion 支持多态序列化，增加了序列化复杂度
3. **类型检查开销**：运行时需要 `is MoveActionInfo` 等类型检查来访问扩展字段
4. **字段重复**：MoveActionInfo 和 SkillActionInfo 都包含 RootMotionData，存在重复
5. **扩展困难**：未来新增动作类型需要创建新的派生类，增加维护成本

通过使用 Extension 模式（组合而非继承），可以：
- 简化类结构，ActionInfo 成为单一具体类
- 保持表格分离，避免 ActionTable 字段膨胀
- 提供类型安全的扩展数据访问
- 便于未来扩展新的动作类型

## What Changes

- **BREAKING**: ActionInfo 从抽象类改为具体类，移除 `abstract` 关键字和 `[MemoryPackUnion]` 属性
- **BREAKING**: 删除 `NormalActionInfo`、`MoveActionInfo`、`SkillActionInfo` 类
- **ADDED**: 创建运行时 `MoveActionExtension` 和 `SkillActionExtension` 类（支持 MemoryPack 序列化）
- **ADDED**: ActionInfo 添加可空的 `MoveExtension` 和 `SkillExtension` 字段
- **MODIFIED**: ActionConfig 的 `GetAction` 方法统一返回 `ActionInfo`，根据 `ActionType` 填充对应的 Extension
- **MODIFIED**: 所有使用 `is MoveActionInfo` 或 `is SkillActionInfo` 的代码改为检查 Extension 字段
- **ADDED**: ActionInfo 提供便利方法（如 `GetBaseMoveSpeed()`、`GetRootMotionData()`）用于类型安全访问扩展数据

## Impact

- 影响的规范：`action-system` 功能（需要创建新规范）
- 影响的代码：
  - `AstrumProj/Assets/Script/AstrumLogic/ActionSystem/ActionInfo.cs` - 改为具体类，添加 Extension 字段
  - `AstrumProj/Assets/Script/AstrumLogic/ActionSystem/NormalActionInfo.cs` - **删除**
  - `AstrumProj/Assets/Script/AstrumLogic/ActionSystem/MoveActionInfo.cs` - **删除**
  - `AstrumProj/Assets/Script/AstrumLogic/SkillSystem/SkillActionInfo.cs` - **删除**
  - `AstrumProj/Assets/Script/AstrumLogic/ActionSystem/MoveActionExtension.cs` - **新增**（运行时版本）
  - `AstrumProj/Assets/Script/AstrumLogic/SkillSystem/SkillActionExtension.cs` - **新增**（运行时版本）
  - `AstrumProj/Assets/Script/AstrumLogic/Managers/ActionConfig.cs` - 修改构造逻辑
  - `AstrumProj/Assets/Script/AstrumLogic/Capabilities/ActionCapability.cs` - 修改类型检查逻辑
  - `AstrumProj/Assets/Script/AstrumView/Components/TransViewComponent.cs` - 修改类型检查逻辑
- 表格配置：**不改变**，ActionTable、MoveActionTable、SkillActionTable 保持分离
- 编辑器代码：**不需要改动**，编辑器使用 `ActionEditorData`（Unity ScriptableObject）和编辑器版本的 Extension 类（Unity 序列化），不依赖运行时的 ActionInfo 派生类

