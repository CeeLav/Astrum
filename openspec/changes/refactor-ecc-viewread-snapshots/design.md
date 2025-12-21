## Context

- 目标是为后续 “Logic 多线程 + View 只读” 做结构准备，而不是立即引入多线程。
- 组件对象包含引用类型/复杂结构，直接对组件做双缓冲或序列化拷贝会带来过重成本。
- 项目已拆分 asmdef（Logic/View/Client），可以用 `internal` 在编译期收口访问面。

## Goals / Non-Goals

- Goals:
  - 建立清晰的 Logic→View 数据边界：View 只能读快照（值类型/ID）。
  - 提供双缓冲一致性：View 一帧内读到的快照自洽。
  - 增量刷新：复用置脏系统，仅导出 dirty 的组件快照。
  - 条件 swap：无写入则不 swap（避免无意义切换与潜在抖动）。
  - 编译期隔离：除 AstrumLogic 外不可写 Entity/Component 内部状态。
- Non-Goals:
  - 本变更不实现 Logic 多线程调度/锁策略。
  - 不要求为所有组件一次性补齐 ViewRead，只覆盖 View 实际读取路径与关键组件集合。
  - 不要求修复/维持 Editor Inspector（如果其依赖 public 内部集合）。

## Decisions

### Decision: 快照形态选择 “每组件一个 ViewRead struct”

- Why:
  - 颗粒度清晰：每个 View 读取点只依赖自己需要的组件快照。
  - 约束天然：struct 字段可被限定为值类型/ID，避免引用类型泄露到 View。

### Decision: 缓冲存储集中在 World（world-owned store），通过泛型读取避免装箱

- How:
  - 每个 World 拥有一个 ViewReadStore，内部按 ComponentTypeId 管理多份 `front/back`（EntityId → ViewRead）。
  - View 读取使用泛型入口（例如 `TryGet<TViewRead>(..., componentTypeId, out TViewRead)`）返回 struct 值，避免 object 装/拆箱。
  - 帧末 swap 可在 store 内集中遍历执行，从而避免“多处 swap 时序不一致”。
- Why:
  - 集中管理 Entity/Component 的增删改时序：组件移除/实体销毁可以在 store 侧统一无效化（写 back），避免 View 侧在读取点分散处理。
  - 读取点仍保持 per-component 语义（按 typeId 取 ViewRead），但存储集中，便于后续做生命周期与一致性治理。

#### Data Structure（存储结构）

- `World` 持有一个 `ViewReadStore`（每个 World 一份）。
- `ViewReadStore` 内部维护：`componentTypeId -> buffer` 的映射。
  - buffer 是引用类型实例（例如 `ViewReadDoubleBuffer<TViewRead>`），可用 `object` 存放以便按泛型强转；不会导致 `TViewRead` 装箱。
- `ViewReadDoubleBuffer<TViewRead>` 内部维护两张表：
  - `front`: `entityId -> TViewRead`（View 只读）
  - `back`: `entityId -> TViewRead`（Logic 帧末写）
- 读取使用泛型返回：`TryGet<TViewRead>(..., out TViewRead)`，value 为 struct，避免 object 装/拆箱。

#### ViewReadDoubleBuffer<TViewRead>（内部结构与行为）

- 核心字段（逻辑含义）：
  - `Dictionary<long, TViewRead> front`：发布给 View 的稳定快照集合。
  - `Dictionary<long, TViewRead> back`：Logic 帧末写入的待发布快照集合。
  - `bool hasWriteThisFrame`：本逻辑帧是否发生过 back 写入（决定是否 swap）。
  - （可选）`frontGeneration / backBaselineGeneration`：用于判断 back 是否需要先同步到 front（避免每帧全量拷贝）。
- 写入规则：
  - Logic 写 back 时，仅覆盖本帧 dirty 的实体条目（增量）。
  - 组件移除/实体销毁通过写入 `IsValid=false` 的 `TViewRead` 到 back 来无效化（swap 后生效）。
- Swap 规则：
  - 帧末若 `hasWriteThisFrame == false`，不交换（front 保持不变）。
  - 帧末若 `hasWriteThisFrame == true`：swap(front, back)；并重置本帧写入标记。
  - （可选）swap 后让 back 以 front 为基线（通过 generation 或 copy-on-write 策略），以便下帧继续增量写入。

### Decision: View 读取入口必须是 “(World, EntityId, 组件类型/TypeId) → ViewRead”

- 约束：
  - View 不持有组件实例，不调用 `Entity.GetComponent<T>()`。
  - View 只能通过快照入口读取。
- 说明：
  - “组件类型”可以是 C# 类型（如 `TransComponent`）或 `ComponentTypeId`（int），两者都满足“无组件实例”。
  - 若需要更统一的入口，可引入 registry 方案：`TryGet<TViewRead>(world, entityId, componentTypeId, out TViewRead)`。

### Decision: 帧末快照导出与 swap 绑定到 LogicTick

- How:
  - Logic 帧末遍历 dirty 列表：写 back。
  - 仅当本帧发生写 back 才 swap。
- Why:
  - 一致性边界明确：LogicTick 结束后发布新的 front，ViewTick 读取稳定的快照。

## Risks / Trade-offs

- **快照字段增长风险**：ViewRead 过度膨胀会变相复制完整组件。
  - Mitigation：对每个 ViewRead 建立“最小字段集”约束，新增字段必须说明 View 用途。
- **结构变更一致性**（Add/Remove）：
  - Mitigation：ViewRead 提供 `IsValid` 或等价校验；Remove 时写 back 无效化，swap 后生效。
- **Editor 调试工具**可能依赖 public 访问：
  - Mitigation：如需要，使用 Editor-only 反射读取，不反向放宽运行时代码访问面。

## Migration Plan

1. 定义 ViewRead 规范（字段约束、有效性约束、入口约束）。
2. 为 View 关键路径组件补齐 ViewRead + 双缓冲。
3. 接入 Logic 帧末：dirty→导出→条件 swap→清脏。
4. 改造 AstrumView：全部读取点迁移到快照入口。
5. 收口写入口：Client 写入迁移至 Logic 可控 API（如仍存在）。

## Open Questions

- 统一入口形态是否固定为泛型 registry：
  - A) World store `TryGet<TViewRead>(world, entityId, componentTypeId, out TViewRead)`（偏集中）
  - B) 保留 per-component wrapper（例如 `TransComponent.TryGetViewRead(...)`）但内部转调 A（偏语义友好）

