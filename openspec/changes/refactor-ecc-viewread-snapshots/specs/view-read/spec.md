## ADDED Requirements

### Requirement: ViewRead 只读快照数据形态

系统 SHALL 为需要被 View 读取的组件提供 `ViewRead` 只读快照结构体，并满足以下约束：

- `ViewRead` MUST 为值类型（`struct`），且只包含值类型字段或 ID（如 EntityId、ConfigId、TypeId）。
- `ViewRead` MUST NOT 包含 `List<>/Dictionary<>/string/UnityEngine.Object` 等托管引用类型字段。
- ViewRead 的读取 API SHOULD 优先使用泛型返回（`out TViewRead`）以避免 object 装/拆箱。

#### Scenario: TransComponent 提供只含值类型的 ViewRead

- **WHEN** ViewRead 用于承载 Transform 相关数据
- **THEN** 快照仅包含 Position/Rotation 等值类型与 EntityId 等 ID 字段
- **AND** 快照不包含任何托管引用类型字段

### Requirement: 每组件快照双缓冲与条件交换

系统 SHALL 为每个组件的 ViewRead 提供 front/back 双缓冲，并满足：

- Logic MUST 写入 back 缓冲。
- View MUST 只读取 front 缓冲。
- 系统 MUST 仅在“本逻辑帧确实发生 back 写入”时执行 swap(front, back)。
- 若本逻辑帧无写入，系统 MUST 保持 front 不变。

#### Scenario: 无脏写入则不发生 swap

- **WHEN** 某逻辑帧内没有任何组件导出 ViewRead 到 back
- **THEN** 帧末不发生 swap
- **AND** View 在下一帧仍读取到相同的 front 快照

### Requirement: 与 dirty 机制集成的增量导出

系统 SHALL 复用现有置脏机制实现增量导出：

- 当组件数据影响 ViewRead 时，Logic MUST 将对应组件标记为 dirty。
- 帧末同步阶段，系统 MUST 仅对 dirty 的 (Entity, ComponentType) 导出 ViewRead 到 back。
- 帧末同步阶段完成后，系统 MUST 清理本帧的 dirty 标记。

#### Scenario: Dirty 的 TransComponent 在帧末被导出并对 View 可见

- **WHEN** Entity 的 TransComponent 在本逻辑帧被标记为 dirty
- **THEN** 帧末同步阶段将该 Entity 的 TransComponent ViewRead 写入 back
- **AND** 若发生写入则 swap 后，View 在下一帧可从 front 读取到更新后的 ViewRead

### Requirement: View 读取入口（无组件实例）

系统 SHALL 为 View 提供仅通过 `(World, EntityId, 组件类型/TypeId)` 获取 ViewRead 的读取入口，并满足：

- View MUST NOT 依赖组件实例来读取数据。
- View MUST NOT 调用 `Entity.GetComponent<T>()` 直接读取组件对象。
- 读取入口 SHOULD 提供泛型形式（例如 `TryGet<TViewRead>(..., componentTypeId, out TViewRead)`），且 `TViewRead` 为 struct，以避免 object 装/拆箱。

#### Scenario: View 通过 EntityId 与组件类型读取 ViewRead

- **WHEN** View 需要读取某 Entity 的 TransComponent 数据
- **THEN** View 通过 `(World, EntityId, TransComponent 或其 TypeId)` 获取 `TransComponent.ViewRead`
- **AND** View 全程不接触 TransComponent 实例

### Requirement: 访问收口（除 AstrumLogic 外不可写）

系统 SHALL 在编译期限制写入入口与内部状态暴露面：

- Entity 的写 API（如 Add/Remove/Attach/Detach 等） MUST 为 AstrumLogic 内部可用（例如 `internal`）。
- 组件内部可变状态（影响 ViewRead 的字段） MUST NOT 对 AstrumView/AstrumClient 公开写入。

#### Scenario: AstrumView 无法调用 Entity 写 API

- **WHEN** AstrumView 尝试调用 `Entity.AddComponent(...)` 等写 API
- **THEN** 该调用在编译期被拒绝（不可访问）

