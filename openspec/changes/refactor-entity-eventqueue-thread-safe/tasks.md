## 1. Entity.EventQueue 线程安全改造

### 1.1 修改 Entity.EventQueue.cs
- [x] 1.1.1 将 `_eventQueue` 字段类型从 `Queue<EntityEvent>` 改为 `ConcurrentQueue<EntityEvent>`
- [x] 1.1.2 实现线程安全的延迟初始化（使用 `Interlocked.CompareExchange`）
- [x] 1.1.3 更新 `EventQueue` 属性返回类型
- [x] 1.1.4 更新 `HasPendingEvents` 使用 `IsEmpty` 属性
- [x] 1.1.5 更新 `ClearEventQueue()` 使用 `TryDequeue` 循环清空
- [x] 1.1.6 添加代码注释说明线程安全性和使用场景

### 1.2 修改 CapabilitySystem.cs
- [x] 1.2.1 在 `ProcessTargetedEvents()` 中使用 `TryDequeue(out var evt)` 替代 `Dequeue()`
- [x] 1.2.2 移除 `eventQueue.Count > 0` 的二次检查（`TryDequeue` 自然处理空队列）
- [x] 1.2.3 添加线程安全注释

### 1.3 单元测试
- [ ] 1.3.1 测试线程安全性（多线程并发写入/读取）
- [ ] 1.3.2 测试事件入队/出队正确性
- [ ] 1.3.3 测试 Client 线程调用 `entity.QueueEvent<T>()` 的线程安全性
- [ ] 1.3.4 测试性能（确保无明显下降）

## 2. 常用事件定义

### 2.1 创建 CommonEvents.cs
- [x] 2.1.1 创建 `AstrumLogic/Events/CommonEvents.cs` 文件
- [x] 2.1.2 定义 `SetPositionEvent` 结构体
- [x] 2.1.3 定义 `SetRotationEvent` 结构体
- [x] 2.1.4 定义 `SetScaleEvent` 结构体
- [x] 2.1.5 定义 `SetHealthEvent` 结构体
- [x] 2.1.6 定义 `SetLevelEvent` 结构体
- [x] 2.1.7 定义 `LoadComponentDataEvent` 结构体（通用序列化）

### 2.2 实现事件处理器
- [ ] 2.2.1 在相应 Capability 中实现 `SetPositionEvent` 处理器
- [ ] 2.2.2 在相应 Capability 中实现 Stats 相关事件处理器
- [ ] 2.2.3 实现 `LoadComponentDataEvent` 通用处理器

## 3. 迁移 SinglePlayerGameMode

### 3.1 分析现有写入点
- [ ] 3.1.1 定位 `MonsterInfoComponent` 写入点（1处）
- [ ] 3.1.2 定位 `TransComponent` 写入点（1处）
- [ ] 3.1.3 定位 `LevelComponent` 写入点（1处）

### 3.2 实现迁移
- [ ] 3.2.1 迁移 MonsterInfo 写入点为 `entity.QueueEvent<SetMonsterInfoEvent>()`
- [ ] 3.2.2 迁移 Transform 写入点为 `entity.QueueEvent<SetPositionEvent>()`
- [ ] 3.2.3 迁移 Level 写入点为 `entity.QueueEvent<SetLevelEvent>()`

### 3.3 验证
- [ ] 3.3.1 测试 GM 命令功能正常
- [ ] 3.3.2 测试怪物生成功能正常
- [ ] 3.3.3 测试关卡切换功能正常

## 4. 迁移 PlayerDataManager

### 4.1 分析现有写入点
- [ ] 4.1.1 定位所有组件写入点（15处）
- [ ] 4.1.2 分类写入点（按组件类型）
- [ ] 4.1.3 评估批处理优化机会

### 4.2 实现迁移
- [ ] 4.2.1 迁移存档加载逻辑（使用 `LoadComponentDataEvent`）
- [ ] 4.2.2 迁移存档保存逻辑（如需要）
- [ ] 4.2.3 实现批处理优化（分批发送事件）

### 4.3 验证
- [ ] 4.3.1 测试存档加载功能正常
- [ ] 4.3.2 测试存档保存功能正常
- [ ] 4.3.3 测试大存档场景（性能）

## 5. 编译和集成测试

### 5.1 编译验证
- [x] 5.1.1 编译 `AstrumLogic.csproj` 无错误
- [x] 5.1.2 编译 `AstrumClient.csproj` 无错误
- [x] 5.1.3 编译 `AstrumProj.sln` 无错误
- [ ] 5.1.4 在 Unity 中执行 `Assets/Refresh`

### 5.2 集成测试
- [ ] 5.2.1 测试事件队列在实际游戏中运行
- [ ] 5.2.2 测试 GM 命令功能
- [ ] 5.2.3 测试存档加载/保存功能
- [ ] 5.2.4 测试调试工具功能

### 5.3 性能验证
- [ ] 5.3.1 使用 Unity Profiler 测量事件处理耗时
- [ ] 5.3.2 验证 Logic 帧率无明显下降
- [ ] 5.3.3 验证无 GC 分配或 GC 分配可接受

## 6. 文档和清理

### 6.1 代码文档
- [ ] 6.1.1 添加事件系统线程安全使用文档
- [ ] 6.1.2 添加常用事件定义示例
- [ ] 6.1.3 添加事件处理器实现示例
- [ ] 6.1.4 更新 `refactor-ecc-viewread-snapshots` 任务 2.3.2 状态

### 6.2 API 文档
- [ ] 6.2.1 文档化线程安全的 `entity.QueueEvent<T>()` API
- [ ] 6.2.2 文档化常用事件类型
- [ ] 6.2.3 说明事件延迟特性（1-2帧）

### 6.3 清理
- [ ] 6.3.1 移除临时调试代码
- [ ] 6.3.2 确保代码风格一致
- [ ] 6.3.3 更新 CHANGELOG（如有）
