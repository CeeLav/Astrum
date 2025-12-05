# 实施任务清单

## 1. Phase 1: 消除 LINQ ToList() 分配

- [ ] 1.1 在 SkillExecutorCapability 添加 `_triggerBuffer` 实例字段
- [ ] 1.2 修改 ProcessFrame() 方法，使用预分配缓冲区替代 ToList()
- [ ] 1.3 将 LINQ Where() 改为手动 for 循环过滤
- [ ] 1.4 添加 ProfileScope 监控性能
- [ ] 1.5 编译验证无错误

## 2. Phase 2: 优化循环遍历

- [ ] 2.1 修改 ProcessFrame() 中的 foreach 为 for 循环
- [ ] 2.2 修改 HandleCollisionTrigger() 中的 foreach 为 for 循环
- [ ] 2.3 修改 HandleDirectTrigger() 中的 foreach 为 for 循环（如有）
- [ ] 2.4 修改 HandleConditionTrigger() 中的 foreach 为 for 循环（如有）
- [ ] 2.5 编译验证无错误

## 3. Phase 3: 复用 CollisionFilter 对象

- [ ] 3.1 在 SkillExecutorCapability 添加 `_collisionFilter` 实例字段
- [ ] 3.2 修改 HandleCollisionTrigger() 方法，复用 filter 对象
- [ ] 3.3 确保每次使用前正确清空 ExcludedEntityIds
- [ ] 3.4 添加注释说明复用逻辑
- [ ] 3.5 编译验证无错误

## 4. Phase 4: VFX 事件对象池（可选）

- [ ] 4.1 评估 VFX 触发频率（通过 Profiler）
- [ ] 4.2 如果频率 > 20 次/帧，实施对象池：
  - [ ] 4.2.1 修改 VFXTriggerEventData 实现 IPool 接口
  - [ ] 4.2.2 添加 Create() 工厂方法和 Reset() 方法
  - [ ] 4.2.3 修改 VFXTriggerEvent 实现 IPool 接口
  - [ ] 4.2.4 添加 Create() 工厂方法和 Reset() 方法
  - [ ] 4.2.5 修改 ProcessVFXTrigger() 使用对象池
  - [ ] 4.2.6 确保正确回收对象
- [ ] 4.3 如果频率 < 20 次/帧，跳过此阶段

## 5. 测试验证

- [ ] 5.1 编译项目（dotnet build AstrumProj.sln）
- [ ] 5.2 刷新 Unity（Assets/Refresh）
- [ ] 5.3 运行单元测试（如有）
- [ ] 5.4 Unity Profiler 验证 GC 分配 < 1 KB/帧
- [ ] 5.5 Unity Profiler 验证 GC.Alloc 次数 < 50 次/帧
- [ ] 5.6 功能测试：技能释放正常
- [ ] 5.7 功能测试：VFX 触发正常
- [ ] 5.8 功能测试：碰撞检测正常
- [ ] 5.9 功能测试：效果触发正常
- [ ] 5.10 内存泄漏测试：运行 30 分钟后内存稳定

## 6. 文档更新

- [ ] 6.1 更新 proposal.md 状态为"已完成"
- [ ] 6.2 记录实际性能提升数据
- [ ] 6.3 创建 IMPLEMENTATION_SUMMARY.md（如需要）
- [ ] 6.4 更新相关技术文档（如需要）

## 7. 验证和归档

- [ ] 7.1 运行 `openspec-chinese validate optimize-skillexecutor-gc --strict`
- [ ] 7.2 修复所有验证错误
- [ ] 7.3 请求用户审批
- [ ] 7.4 归档变更（用户批准后）

## 依赖关系

- Phase 1-3 可以并行实施
- Phase 4 依赖 Phase 1-3 完成后的性能评估
- 测试验证依赖所有 Phase 完成

## 预计工时

| 阶段 | 预计时间 | 优先级 |
|------|---------|--------|
| Phase 1 | 0.5 天 | 高 |
| Phase 2 | 0.5 天 | 高 |
| Phase 3 | 0.5 天 | 中 |
| Phase 4 | 1 天 | 低（可选）|
| 测试验证 | 0.5 天 | 高 |
| 文档更新 | 0.5 天 | 中 |
| **总计** | **2-3.5 天** | - |

