# AI 开发进展

> 📊 当前版本: v0.1.0  
> 📅 最后更新: 2025-11-02  
> 👤 负责人: 待指定

## TL;DR（四象限）
- 状态/进度：规范完成，文档落地；实现未开始
- 已达成：子原型机制文档；AI 原型设计；进展文档建立
- 风险/阻塞：运行时卸载安全与确定性、序列化回放一致性待验证
- 下一步：实现运行时 API 与合并快照；落地 FSM 能力与状态能力骨架

---

## 版本历史

### v0.1.0 - 初始化（2025-11-02）
- 新增 ECC 子原型机制（文档）：Archetype 3.4/3.5/3.6，ECC 4A/14
- 新增 AI 原型（子原型）设计：AIStateMachineComponent + AIFSMCapability + 状态能力
- 建立本开发进展文档与实施清单

---

## 当前阶段
- 阶段名称：设计落地与实施准备
- 完成度：10%

**下一步计划**
1. 实现 SubArchetype 运行时 API（Attach/Detach/Query）
2. 为 Entity 维护 activeSubArchetypes 集合与快照
3. 帧末批量应用子原型变更（统一 Apply）
4. 实现 AIStateMachineComponent / AIFSMCapability / Idle/Move 状态能力骨架

---

## 开发清单（待办）
- [ ] 实现 SubArchetype 运行时 API（Attach/Detach/Query）
- [ ] 为 Entity 维护 activeSubArchetypes 集合与快照
- [ ] 帧末批量应用子原型变更，保证确定性
- [ ] 序列化 activeSubArchetypes 并在回放恢复
- [ ] 实现去重与多实例冲突校验（组件/能力）
- [ ] 增量更新 Archetype 合并快照（主原型+子原型）
- [ ] 实现 AIStateMachineComponent（数据）
- [ ] 实现 AIFSMCapability（调度与状态切换）
- [ ] 实现 IdleStateCapability 与 MoveStateCapability 骨架
- [ ] Attach/Detach AI 时初始化/清理状态机与默认状态
- [ ] 集成测试：Attach/Detach、序列化恢复、冲突处理
- [ ] GMTool/命令：非交互触发 Attach/Detach（测试用）
- [ ] 日志与调试指标（变更、当前状态）

---

## 追溯（Traceability）
- ECC：`Docs/05-CoreArchitecture 核心架构/ECC/Archetype-System Archetype结构说明.md`（3.4/3.5/3.6）
- ECC：`Docs/05-CoreArchitecture 核心架构/ECC/ECC-System ECC结构说明.md`（4A、14）
- AI：`Docs/12-AI AI系统/AI-StateMachine-Design AI状态机设计.md`（AI 原型子原型章节）

---

*文档版本：v0.1.0*  
*Owner*: 待指定  
*变更摘要*: 初始化 AI 开发进展与实施清单

