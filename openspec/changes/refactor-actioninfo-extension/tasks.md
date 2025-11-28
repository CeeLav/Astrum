## 1. 实施

### 1.1 创建运行时 Extension 类
- [ ] 创建 `AstrumProj/Assets/Script/AstrumLogic/ActionSystem/MoveActionExtension.cs`
  - [ ] 定义 `MoveSpeed` (int) 字段
  - [ ] 定义 `RootMotionData` (AnimationRootMotionData) 字段
  - [ ] 添加 `[MemoryPackable]` 和 `[MemoryPackAllowSerialize]` 属性
  - [ ] 实现 MemoryPack 构造函数
- [ ] 创建 `AstrumProj/Assets/Script/AstrumLogic/SkillSystem/SkillActionExtension.cs`
  - [ ] 定义 `ActualCost` (int) 字段
  - [ ] 定义 `ActualCooldown` (int) 字段
  - [ ] 定义 `TriggerFrames` (string) 字段
  - [ ] 定义 `TriggerEffects` (List<TriggerFrameInfo>) 字段
  - [ ] 定义 `RootMotionData` (AnimationRootMotionData) 字段
  - [ ] 添加 `[MemoryPackable]` 和 `[MemoryPackAllowSerialize]` 属性
  - [ ] 实现 MemoryPack 构造函数

### 1.2 修改 ActionInfo
- [ ] 移除 `abstract` 关键字
- [ ] 移除所有 `[MemoryPackUnion]` 属性
- [ ] 添加 `MoveExtension?` 字段（`[MemoryPackAllowSerialize]`）
- [ ] 添加 `SkillExtension?` 字段（`[MemoryPackAllowSerialize]`）
- [ ] 添加便利方法：
  - [ ] `GetBaseMoveSpeed(): FP?`
  - [ ] `GetRootMotionData(): AnimationRootMotionData?`
  - [ ] `GetActualCost(): int`
  - [ ] `GetActualCooldown(): int`
  - [ ] `GetTriggerEffects(): List<TriggerFrameInfo>`
- [ ] 更新 MemoryPack 构造函数，添加 Extension 参数

### 1.3 修改 ActionConfig
- [ ] 修改 `GetAction` 方法返回类型为 `ActionInfo`（不再是 `ActionInfo?` 的派生类）
- [ ] 统一构造逻辑，创建 `ActionInfo` 实例
- [ ] 根据 `ActionType` 填充对应的 Extension：
  - [ ] "Move" -> 从 MoveActionTable 加载并填充 `MoveExtension`
  - [ ] "Skill" -> 从 SkillActionTable 加载并填充 `SkillExtension`
  - [ ] 其他 -> Extension 字段为 null
- [ ] 移除 `ConstructNormalActionInfo` 方法
- [ ] 移除 `ConstructMoveActionInfo` 方法
- [ ] 移除 `ConstructSkillActionInfo` 方法
- [ ] 更新 `PopulateMoveActionFields` 方法为填充 `MoveExtension`
- [ ] 更新 `PopulateSkillActionFields` 方法为填充 `SkillExtension`

### 1.4 更新使用代码
- [ ] 修改 `ActionCapability.cs`：
  - [ ] 将 `is MoveActionInfo` 改为 `MoveExtension != null`
  - [ ] 使用 `GetBaseMoveSpeed()` 方法访问移动速度
- [ ] 修改 `TransViewComponent.cs`：
  - [ ] 将 `GetType().Name == "SkillActionInfo"` 改为 `SkillExtension != null`
  - [ ] 使用 `GetRootMotionData()` 方法访问根节点位移
- [ ] 搜索所有使用 `NormalActionInfo`、`MoveActionInfo`、`SkillActionInfo` 的地方并更新
- [ ] **检查编辑器代码**：确认编辑器代码（`AstrumProj/Assets/Script/Editor`）不依赖运行时的 ActionInfo 派生类
  - [ ] 编辑器使用 `ActionEditorData`（Unity ScriptableObject），不依赖运行时 ActionInfo
  - [ ] 编辑器已有 Extension 字段（Unity 序列化版本），无需修改

### 1.5 删除旧类
- [ ] 删除 `AstrumProj/Assets/Script/AstrumLogic/ActionSystem/NormalActionInfo.cs`
- [ ] 删除 `AstrumProj/Assets/Script/AstrumLogic/ActionSystem/MoveActionInfo.cs`
- [ ] 删除 `AstrumProj/Assets/Script/AstrumLogic/SkillSystem/SkillActionInfo.cs`

## 2. 验证

- [ ] 编译通过，无编译错误
- [ ] 运行单元测试（如果有）
- [ ] 测试 ActionConfig.GetAction 方法：
  - [ ] 普通动作返回 ActionInfo，Extension 为 null
  - [ ] 移动动作返回 ActionInfo，MoveExtension 非空
  - [ ] 技能动作返回 ActionInfo，SkillExtension 非空
- [ ] 测试序列化/反序列化：
  - [ ] ActionInfo 可以正确序列化
  - [ ] 包含 Extension 的 ActionInfo 可以正确序列化和反序列化
- [ ] 测试运行时功能：
  - [ ] 移动动作的移动速度计算正常
  - [ ] 技能动作的触发效果正常
  - [ ] 根节点位移数据访问正常

## 3. 文档

- [ ] 更新相关代码注释
- [ ] 更新 ActionInfo 类的 XML 文档注释
- [ ] 如有必要，更新设计文档

