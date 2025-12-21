# Change: Refactor ECC View 数据读取为 ViewRead 快照 + 双缓冲

## Why

当前 View 直接读取 Logic 组件对象（且组件内部含大量引用类型/复杂结构），这会让后续把 Logic 放入多线程时面临读写竞争与一致性问题；如果用对“组件对象”做深拷贝或 MemoryPack 序列化来解决，会产生过重的拷贝与重复数据成本。

## What Changes

- 将 View 的“数据读取源”统一切换为 **每组件 ViewRead 只读快照 struct**（值类型/ID 的最小集合），不再依赖 `Entity.GetComponent<T>()` 直接读组件对象。
- 对每组件 ViewRead 做 **双缓冲（front/back）**：
  - **Logic**：帧末基于“脏组件”增量导出到 back
  - **View**：整帧只读 front
  - **交换条件**：仅当本逻辑帧确实对 back 发生写入时才 swap
- 用 `internal` 收口不应暴露的状态与写入口，保证编译期隔离：**除 AstrumLogic 外不可写**。

## Impact

- 影响的规范/能力：
  - 新增：`view-read`（Logic→View 数据边界与快照一致性）
- 影响的代码范围（高层）：
  - `AstrumLogic`：Entity/Component 访问修饰符收口；Logic 帧末快照导出与 swap
  - `AstrumView`：所有读取点迁移到 ViewRead 快照入口
  - `AstrumClient`：直接写 Entity 的路径下沉至 AstrumLogic 受控入口（如仍存在）

