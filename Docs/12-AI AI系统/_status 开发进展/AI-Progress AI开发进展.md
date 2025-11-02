# AI 开发进展

> 📊 当前版本: v0.2.0  
> 📅 最后更新: 2025-11-02  
> 👤 负责人: 待指定

## TL;DR（四象限）
- 状态/进度：核心功能已实现，AI 状态机基本可用；物理碰撞检测已修复
- 已达成：SubArchetype 运行时 API、引用计数机制、AI 三状态完整实现（Idle/Move/Battle）、物理世界同步修复
- 风险/阻塞：性能优化（增量快照）待后续迭代；测试覆盖待补充
- 下一步：集成测试、性能优化、AI 行为调优

---

## 版本历史

### v0.2.0 - 核心功能实现（2025-11-02）
- ✅ 实现 SubArchetype 运行时 API（引用计数机制）
- ✅ 实现 Entity 组件/能力引用计数（ComponentRefCounts / CapabilityRefCounts）
- ✅ 帧末批量应用子原型变更（EnqueueSubArchetypeChange）
- ✅ 序列化引用计数数据并支持回放恢复
- ✅ 实现 AIStateMachineComponent（状态机数据组件）
- ✅ 实现 AIFSMCapability（状态调度与切换）
- ✅ 实现 IdleStateCapability（待机状态：搜索敌人、随机漫游）
- ✅ 实现 MoveStateCapability（移动状态：追击敌人、前往漫游点）
- ✅ 实现 BattleStateCapability（战斗状态：攻击冷却、距离管理）
- ✅ 创建 AIArchetype（AI 子原型定义）
- ✅ SinglePlayerGameMode.CreateRoleWithAI（创建带 AI 的角色）
- ✅ 修复物理世界注册与位置同步问题
- ✅ 修复碰撞检测问题（NarrowPhase 精确检测、BroadPhase AABB 更新）
- ✅ 清理不必要的调试日志

### v0.1.0 - 初始化（2025-11-02）
- 新增 ECC 子原型机制（文档）：Archetype 3.4/3.5/3.6，ECC 4A/14
- 新增 AI 原型（子原型）设计：AIStateMachineComponent + AIFSMCapability + 状态能力
- 建立本开发进展文档与实施清单

---

## 当前阶段
- 阶段名称：核心功能实现与调试修复
- 完成度：70%

**已实现功能**
- AI 状态机完整实现（Idle → Move → Battle）
- 敌人搜索与追击逻辑
- 攻击冷却机制
- 物理碰撞检测与伤害判定
- 子原型运行时动态挂载/卸载

**待完善项**
- 集成测试与边界情况验证
- 性能优化（Archetype 合并快照增量更新）
- AI 行为参数调优

---

## 开发清单

### 核心架构（已完成）
- [x] 实现 SubArchetype 运行时 API（Attach/Detach/Query）
- [x] 为 Entity 维护 activeSubArchetypes 集合与快照（引用计数机制）
- [x] 帧末批量应用子原型变更，保证确定性
- [x] 序列化 activeSubArchetypes 并在回放恢复（ComponentRefCounts / CapabilityRefCounts）
- [x] 实现去重与多实例冲突校验（组件/能力）

### AI 功能实现（已完成）
- [x] 实现 AIStateMachineComponent（数据）
- [x] 实现 AIFSMCapability（调度与状态切换）
- [x] 实现 IdleStateCapability 与 MoveStateCapability 骨架（完整逻辑实现）
- [x] 实现 BattleStateCapability（完整逻辑实现）
- [x] Attach/Detach AI 时初始化/清理状态机与默认状态
- [x] 创建 AIArchetype（子原型定义）
- [x] SinglePlayerGameMode.CreateRoleWithAI（便捷创建方法）

### 物理与碰撞（已完成）
- [x] 修复物理世界实体注册问题
- [x] 修复实体位置同步问题（UpdateEntityPosition）
- [x] 修复碰撞检测问题（NarrowPhase 精确检测）
- [x] 修复 BroadPhase AABB 更新问题

### 待办事项
- [ ] 增量更新 Archetype 合并快照（主原型+子原型）
- [ ] 集成测试：Attach/Detach、序列化恢复、冲突处理
- [ ] GMTool/命令：非交互触发 Attach/Detach（测试用）
- [ ] AI 行为参数调优（距离阈值、冷却时间、漫游逻辑）

---

## 追溯（Traceability）
- ECC：`Docs/05-CoreArchitecture 核心架构/ECC/Archetype-System Archetype结构说明.md`（3.4/3.5/3.6）
- ECC：`Docs/05-CoreArchitecture 核心架构/ECC/ECC-System ECC结构说明.md`（4A、14）
- AI：`Docs/12-AI AI系统/AI-StateMachine-Design AI状态机设计.md`（AI 原型子原型章节）

---

*文档版本：v0.2.0*  
*Owner*: 待指定  
*变更摘要*: 核心功能实现完成，AI 状态机可用；物理碰撞检测修复

