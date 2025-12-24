## 1. Specification

- [ ] 1.1 明确 ViewRead 字段约束（仅值类型/ID；避免引用类型）
- [ ] 1.2 明确 View 读取入口约束（禁止 `Entity.GetComponent<T>()` 直读组件对象）
- [ ] 1.3 明确条件 swap（仅在本帧发生写 back 时 swap）
- [ ] 1.4 明确与 dirty 机制集成（dirty→导出→清脏）

## 2. Implementation (after approval)

### 2.1 基础结构（本阶段先完成）

- [x] 2.1.1 实现 `ViewReadStore`（World 集中式存储，按 `componentTypeId` 管理 buffer）
- [x] 2.1.2 实现 `ViewReadDoubleBuffer<TViewRead>`（front/back + 本帧写入标记 + 条件 swap）
- [x] 2.1.3 提供泛型读取入口：`TryGet<TViewRead>(world, entityId, componentTypeId, out TViewRead)`（避免装/拆箱）
- [x] 2.1.4 提供泛型写入入口（Logic-only）：`WriteBack<TViewRead>(world, entityId, componentTypeId, in TViewRead)`
- [x] 2.1.5 接入 LogicTick 帧末同步骨架：dirty → 导出 back →（若写入）swap → 清脏
- [x] 2.1.6 定义/实现无效化约定：组件移除/实体销毁时写入 `IsValid=false` 的 ViewRead（swap 后生效）

### 2.2 组件试点改造（选少量组件做闭环验证）

- [x] 2.2.1 选择 2~3 个组件定义 `ViewRead` struct（组件内声明），并实现导出（例如 Trans/Movement/Action）
- [x] 2.2.2 将对应的 View 读取点迁移为 snapshot-only（仅改这几个组件相关的 ViewComponent）
- [x] 2.2.3 验证组件增删/实体销毁场景：View 不读到残留旧值（通过 `IsValid` 或等价校验）
- [x] 2.2.4 将 TransComponent 和 MovementComponent 的所有字段改为 internal（添加 [MemoryPackInclude]）
- [x] 2.2.5 修复所有依赖这些组件的 View 组件（CollisionDebug, Projectile, PredictedMovement, Trans）
- [x] 2.2.6 修复 AstrumClient 和 Editor 中的访问点（使用公开API或反射）

### 2.3 收口与扩展（后续阶段）

- [x] 2.3.1 扩展到更多组件：按 View 实际读取点逐步迁移
  - [x] AnimationViewComponent：重构为从 EntityConfig 和配置表获取动作信息，不再直接访问 ActionComponent
  - [x] ActionComponent.ViewRead：添加 AnimationSpeedMultiplier 字段以支持动画播放速度同步
  - [x] 修复 ProjectileViewComponent 中的 socketRefs 错误
  - [x] ProjectileCapability：修复子弹位置更新时未标记 TransComponent 为 dirty 的问题
  - [x] PredictedMovementViewComponent：清理注释掉的旧直接访问代码（OnReset方法）
  - [x] 验证所有 ViewComponent 不再直接访问 Logic 组件（通过 grep 检查）
  - 注：CollisionDebugViewComponent 保持直接访问（仅调试工具，暂不处理）
- [ ] 2.3.2 收口写入口：AstrumClient 侧对 Entity 的写入迁移至 AstrumLogic 可控入口
  - 发现需要迁移的写入点：
    - SinglePlayerGameMode.cs: MonsterInfoComponent, TransComponent, LevelComponent（3处）
    - PlayerDataManager.cs: 多个组件的存档加载/保存（15处）
  - 解决方案：通过 `refactor-entity-eventqueue-thread-safe` 提供线程安全的事件队列
    - Entity.EventQueue 已改为 ConcurrentQueue（线程安全）
    - Client 线程可以调用 `entity.QueueEvent<T>()` 发送事件
    - 定义了常用事件类型（SetPosition, LoadComponentData 等）
  - 状态：基础设施已完成，待迁移具体写入点（在 refactor-entity-eventqueue-thread-safe 中完成）

## 3. Validation

- [x] 3.1 Unity 执行 `Assets/Refresh` 使新增文件被识别
  - ✅ 已在 Unity 中执行，ViewRead 文件已被识别
- [x] 3.2 `dotnet build AstrumProj.sln` 编译通过（优先通过 Unity/MCP 读取报错定位）
  - ✅ 编译成功，无错误
- [ ] 3.3 在 Unity 内进行最小冒烟验证：试点组件的数据能从 Logic 帧末导出并在 View 读取（不修改生产逻辑代码路径）
  - Trans/Movement/Action 组件已迁移至 ViewRead
  - AnimationViewComponent 已改为从配置表读取动作信息
  - 待用户在 Unity 中运行验证

