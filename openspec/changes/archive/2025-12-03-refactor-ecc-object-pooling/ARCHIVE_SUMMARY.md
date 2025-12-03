# 归档总结

**归档日期**：2025-12-03  
**变更 ID**：`refactor-ecc-object-pooling`  
**状态**：📝 设计文档完成，等待实施

---

## 状态说明

⚠️ **此变更为设计阶段文档，尚未实施**

归档原因：
- 设计文档已完成
- 等待合适的实施时机
- 作为未来参考保留

---

## 设计概览

### 目标
将整个 ECC 体系（Entity、Component、Capability）改为对象池管理，实现无 GC 目标。

### 核心改动
1. **Entity 对象池化**
   - 实现 `IPool` 接口
   - 添加 `Reset()` 方法
   - EntityFactory 使用 ObjectPool 创建/回收

2. **Component 对象池化**
   - BaseComponent 实现 `IPool` 接口
   - 所有 Component 子类实现 `Reset()` 方法
   - ComponentFactory 使用 ObjectPool 创建/回收

3. **World 对象池化**
   - World 实现 `IPool` 接口
   - 添加 `Reset()` 方法
   - Room/GameMode 使用对象池创建 World

4. **内部集合优化**
   - Entity 内部 List/Dictionary 使用对象池
   - 或预分配容量避免扩容

---

## 预期收益

### 性能提升
- **GC 分配**：减少 80% 以上
- **GC 暂停时间**：减少 70% 以上
- **缓存友好性**：对象复用，内存布局更紧凑
- **帧率稳定性**：显著降低 GC 引起的帧率波动

### 特别适用场景
- 高频创建/销毁实体（技能特效、弹道、临时实体）
- 战斗场景中的大量实体更新
- 服务器端高并发战斗逻辑

---

## 设计要点

### 1. Reset() 方法设计原则
```csharp
public class MyComponent : BaseComponent
{
    private List<int> _data = new List<int>();
    
    public override void Reset()
    {
        base.Reset();
        
        // 清空可变状态
        _data.Clear();
        
        // 重置标志位
        _isDirty = false;
        
        // 不清空容器本身（复用内存）
    }
}
```

**原则**：
- ✅ 清空可变状态
- ✅ 重置标志位和引用
- ✅ 保留容器结构（List、Dictionary）复用内存
- ❌ 不重新创建容器

### 2. IPool 接口设计
```csharp
public interface IPool
{
    void OnGet();      // 从池中取出时调用
    void OnReturn();   // 返回池中时调用
}
```

### 3. EntityFactory 改造
```csharp
// 之前
var entity = new Entity(world, entityId, archetype);

// 之后
var entity = ObjectPool<Entity>.Get();
entity.Initialize(world, entityId, archetype);
```

### 4. MemoryPack 兼容性
需要确保：
- MemoryPack 反序列化时使用对象池创建对象
- 反序列化的对象回收时调用 Reset() 方法

---

## 实施任务（参考）

### Phase 1: 基础设施（2-3小时）
- [ ] Entity 实现 IPool 接口
- [ ] BaseComponent 实现 IPool 接口
- [ ] World 实现 IPool 接口
- [ ] 添加 Reset() 方法骨架

### Phase 2: Factory 改造（3-4小时）
- [ ] EntityFactory 使用 ObjectPool
- [ ] ComponentFactory 使用 ObjectPool
- [ ] 测试创建/回收流程

### Phase 3: Component 子类实施（4-6小时）
- [ ] 实现所有 Component 子类的 Reset() 方法
- [ ] 测试各个 Component 的复用逻辑

### Phase 4: 内部集合优化（2-3小时）
- [ ] Entity 内部集合对象池化
- [ ] 预分配容量优化

### Phase 5: 测试与优化（3-4小时）
- [ ] 编写单元测试
- [ ] 性能测试（GC 分配、暂停时间）
- [ ] 内存泄漏检测
- [ ] 边界情况测试

**总预估**：14-20 小时（约 2-3 个工作日）

---

## 技术风险

### 1. Reset() 实现遗漏
**风险**：某些 Component 的 Reset() 实现不完整，导致状态残留  
**缓解**：
- 编写完整的单元测试
- 添加 Reset() 检查工具
- Code Review 重点检查

### 2. MemoryPack 兼容性
**风险**：MemoryPack 反序列化可能绕过对象池  
**缓解**：
- 修改 MemoryPack 反序列化逻辑
- 添加对象池适配层
- 测试回滚流程

### 3. 内存泄漏
**风险**：对象池持有对象引用，导致内存无法释放  
**缓解**：
- 合理设置对象池容量上限
- 添加对象池监控
- 定期清理长时间未使用的对象

### 4. 线程安全
**风险**：多线程环境下对象池操作可能不安全  
**缓解**：
- ObjectPool 使用线程安全实现
- 文档说明线程安全约束

---

## 参考设计

### 类似系统
- Unity ECS：Entity 和 Component 完全对象池化
- Unity GameObject Pool：GameObject 复用机制
- Astrum ViewComponent：已实现对象池优化（参考 EntityView Event Queue）

### 对象池最佳实践
1. **固定状态保留**：对象结构、容器、Handler
2. **可变状态清空**：数据、标志位、引用
3. **容量预分配**：避免运行时扩容
4. **监控统计**：池大小、命中率、GC 分配

---

## 相关变更

### 已完成变更（可参考）
- `add-entityview-event-queue`：ViewComponent 对象池优化
  - _eventHandlersRegistered 标志位
  - RegisterViewEventHandlers 只调用一次
  - Destroy 时不清空回调

### 依赖关系
- 无依赖，可独立实施
- 实施后可能影响序列化/反序列化流程

---

## 何时实施？

**建议时机**：
1. ✅ 完成 EntityView Event Queue（已完成）
2. ⏳ 完成 ASProfiler 系统（可用于性能对比）
3. ⏳ 战斗系统性能优化专项时
4. ⏳ 准备进行大规模战斗测试前

**优先级**：中等

当前战斗场景如果 GC 压力不大，可以暂缓实施。如果出现以下情况，应尽快实施：
- 战斗场景 GC 分配超过 100KB/s
- 出现明显的 GC 暂停（>10ms）
- 帧率不稳定，与 GC 相关

---

## 总结

📝 **ECC 对象池化设计文档已完成**

**设计亮点**：
- ✅ 完整的对象池化方案
- ✅ 详细的 Reset() 设计原则
- ✅ 清晰的实施任务和预估
- ✅ 全面的风险评估和缓解措施

**状态**：
- 设计文档完成
- 等待合适的实施时机
- 作为未来参考保留

**下一步**：
- 使用 ASProfiler 监控当前 GC 情况
- 根据性能数据决定是否实施
- 如需实施，按照 tasks.md 执行

---

**归档路径**：`openspec/changes/archive/2025-12-03-refactor-ecc-object-pooling/`

