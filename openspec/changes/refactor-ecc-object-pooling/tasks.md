## 1. 接口和基础设施

- [ ] 1.1 在 Entity 类中实现 IPool 接口
  - [ ] 添加 `IsFromPool` 属性实现
  - [ ] 添加空的 `Reset()` 方法占位
- [ ] 1.2 在 BaseComponent 类中实现 IPool 接口
  - [ ] 添加 `IsFromPool` 属性实现
  - [ ] 添加空的 `Reset()` 方法占位
- [ ] 1.3 扩展 ObjectPool 支持容量配置
  - [ ] 添加 `GetPoolCapacity(Type type)` 方法根据类型返回容量
  - [ ] 修改 `AddPoolFunc` 使用动态容量
  - [ ] Entity 类型默认容量设置为 500
  - [ ] Component 类型默认容量设置为 1000

## 2. Reset 方法实现

- [ ] 2.1 实现 Entity.Reset 方法
  - [ ] 重置基础字段（UniqueId、Name、IsDestroyed、CreationTime 等）
  - [ ] 清空 Components 列表
  - [ ] 清空 ChildrenIds 列表
  - [ ] 清空 CapabilityStates 字典
  - [ ] 清空 DisabledTags 字典
  - [ ] 清空 ActiveSubArchetypes 列表
  - [ ] 清空 ComponentRefCounts 和 CapabilityRefCounts 字典
  - [ ] 重置 ParentId 为 -1
  - [ ] 重置 EntityConfigId 为 0
  - [ ] 清空脏组件标记集合
- [ ] 2.2 实现 BaseComponent.Reset 方法
  - [ ] 重置 EntityId 为 0
  - [ ] 调用虚方法允许子类扩展
- [ ] 2.3 为所有 Component 子类实现 Reset 方法
  - [ ] PositionComponent.Reset
  - [ ] MovementComponent.Reset
  - [ ] HealthComponent.Reset
  - [ ] ActionComponent.Reset
  - [ ] LSInputComponent.Reset
  - [ ] BaseStatsComponent.Reset
  - [ ] DynamicStatsComponent.Reset
  - [ ] DerivedStatsComponent.Reset
  - [ ] GrowthComponent.Reset
  - [ ] BuffComponent.Reset
  - [ ] 其他 Component 子类
- [ ] 2.4 添加 Reset 方法单元测试
  - [ ] 测试 Entity.Reset 重置所有字段
  - [ ] 测试 Component.Reset 重置所有字段
  - [ ] 测试 Reset 后对象状态正确

## 3. 集合预分配优化

- [ ] 3.1 Entity 内部集合预分配容量
  - [ ] Components 列表预分配容量 8
  - [ ] ChildrenIds 列表预分配容量 4
  - [ ] CapabilityStates 字典预分配容量 4
  - [ ] DisabledTags 字典预分配容量 2
  - [ ] ActiveSubArchetypes 列表预分配容量 2
  - [ ] ComponentRefCounts 字典预分配容量 8
  - [ ] CapabilityRefCounts 字典预分配容量 4
- [ ] 3.2 验证集合重置使用 Clear 而非重新创建
  - [ ] Reset 方法中使用 Clear() 清空集合
  - [ ] 确保集合对象本身不被销毁

## 4. Factory 改造

- [ ] 4.1 EntityFactory 改用 ObjectPool
  - [ ] CreateByArchetype 方法使用 `ObjectPool.Instance.Fetch<T>()` 获取 Entity
  - [ ] 移除 `new T()` 直接创建
  - [ ] 确保 Entity 获取后调用 Reset（或确保对象池返回已重置对象）
  - [ ] 验证 Entity 的基本属性设置流程
- [ ] 4.2 ComponentFactory 改用 ObjectPool
  - [ ] CreateComponentFromType 方法使用 `ObjectPool.Instance.Fetch(type)` 获取 Component
  - [ ] 移除 `Activator.CreateInstance` 直接创建
  - [ ] 移除 `new T()` 直接创建
  - [ ] 确保 Component 获取后已重置状态
- [ ] 4.3 添加 Factory 单元测试
  - [ ] 测试 EntityFactory 创建 Entity 来自对象池
  - [ ] 测试 ComponentFactory 创建 Component 来自对象池
  - [ ] 测试多次创建和回收流程

## 5. 销毁逻辑改造

- [ ] 5.1 EntityFactory.DestroyEntity 改为回收
  - [ ] 调用 Entity.Reset 重置状态
  - [ ] 回收所有 Component 至对象池
  - [ ] 使用 `ObjectPool.Instance.Recycle(entity)` 回收 Entity
  - [ ] 移除原有的直接销毁逻辑
- [ ] 5.2 World.DestroyEntity 适配回收逻辑
  - [ ] 确保在回收前发布销毁事件
  - [ ] 确保从 World.Entities 字典中移除
  - [ ] 调用 EntityFactory.DestroyEntity 进行回收
- [ ] 5.3 Entity.RemoveComponent 改为回收 Component
  - [ ] Component 移除时调用 Reset
  - [ ] 使用 `ObjectPool.Instance.Recycle(component)` 回收
- [ ] 5.4 添加销毁和回收单元测试
  - [ ] 测试 Entity 销毁后回收至对象池
  - [ ] 测试 Component 回收后状态已重置
  - [ ] 测试多次创建和销毁流程

## 6. 序列化兼容性

- [ ] 6.1 检查 MemoryPack 序列化代码
  - [ ] 查找所有 Entity 序列化/反序列化代码
  - [ ] 查找所有 Component 序列化/反序列化代码
  - [ ] 确保使用 `ObjectPool.Instance.Fetch<T>(isFromPool: false)` 创建新对象
- [ ] 6.2 添加序列化兼容性测试
  - [ ] 测试 Entity 序列化/反序列化后状态正确
  - [ ] 测试 Component 序列化/反序列化后状态正确
  - [ ] 测试序列化后的对象不来自对象池
  - [ ] 测试反序列化后可以正常使用对象池

## 7. 性能测试和验证

- [ ] 7.1 GC 分配测试
  - [ ] 创建测试场景模拟高频 Entity/Component 创建销毁
  - [ ] 使用 Unity Profiler 测量 GC 分配
  - [ ] 对比改造前后的 GC 分配量
  - [ ] 验证 GC 分配减少 80% 以上
- [ ] 7.2 GC 暂停时间测试
  - [ ] 使用 Unity Profiler 测量 GC 暂停时间
  - [ ] 对比改造前后的 GC 暂停时间
  - [ ] 验证 GC 暂停时间减少 70% 以上
- [ ] 7.3 功能回归测试
  - [ ] 运行完整的战斗系统测试
  - [ ] 验证 Entity 和 Component 功能正常
  - [ ] 验证序列化和回滚功能正常
  - [ ] 验证所有单元测试通过

## 8. 文档和代码审查

- [ ] 8.1 更新相关文档
  - [ ] 更新 Entity 创建和销毁流程文档
  - [ ] 更新 Component 创建和销毁流程文档
  - [ ] 更新对象池使用规范文档
- [ ] 8.2 代码审查
  - [ ] 审查所有 Reset 方法实现是否完整
  - [ ] 审查对象池使用是否正确
  - [ ] 审查是否有内存泄漏风险
  - [ ] 审查是否有循环引用问题

