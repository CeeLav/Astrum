# MoveActionTable 开发进展

> 📊 **当前版本**: v0.3.0  
> 📅 **最后更新**: 2025-11-09  
> 👤 **负责人**: Lavender

## TL;DR（四象限）
- 状态/进度：🚧 开发中 - 配置层、编辑器、运行时架构已完成，动画同步待实现
- 已达成：MoveActionTable 配置表、编辑器读写、MoveActionInfo 类、运行时加载
- 风险/阻塞：动画速度同步待实现（AnimationViewComponent 扩展）
- 下一步：实现动画速度倍率同步、测试验证完整流程

## 版本历史

### v0.3.0 - 运行时架构完成 (2025-11-09)
**状态**: 🚧 开发中

**完成内容**:
- [x] 创建 `MoveActionInfo` 类（继承自 `ActionInfo`）
- [x] 在 `ActionInfo` 中添加 Union(2) 注册
- [x] 实现 `ActionConfig.ConstructMoveActionInfo` 方法
- [x] 实现 `ActionConfig.PopulateMoveActionFields` 方法
- [x] 实现 `ActionConfig.LoadMoveRootMotionData` 方法
- [x] 根据 `ActionType` 自动创建对应的 ActionInfo 类型

**待完成**:
- AnimationViewComponent 动画速度倍率同步
- 完整流程测试验证

**预计工时**: 剩余 1-2 天（动画同步与测试）

### v0.2.0 - 配置层与编辑器完成 (2025-11-09)
**状态**: ✅ 已完成

**完成内容**:
- [x] MoveActionTable CSV 表结构（actionId, moveSpeed, rootMotionData）
- [x] MoveActionTableData 映射类与 Luban 配置
- [x] 编辑器读取逻辑（SkillActionDataReader 支持所有动作类型）
- [x] 编辑器写入逻辑（按类型分发到不同表）
- [x] RootMotionData 字段支持（与 SkillActionTable 一致）
- [x] 移动速度自动计算（基于 RootMotion 数据）

### v0.1.0 - 设计准备完成 (2025-11-09)
**状态**: ✅ 已完成

**完成内容**:
- [x] 明确 MoveActionTable 设计目标与上下游
- [x] 梳理运行时整合与编辑器扩展需求
- [x] 形成阶段划分与任务清单

---

## 当前阶段

**阶段名称**: Phase 2 - 运行时整合与动画同步  
**完成度**: 85%

**已完成**:
- ✅ MoveActionTable CSV 表结构定义
- ✅ 编辑器读取所有动作类型（ActionTable 驱动）
- ✅ 编辑器按类型分发写入（ActionTable/SkillActionTable/MoveActionTable）
- ✅ RootMotionData 字段支持
- ✅ MoveActionInfo 类架构（独立于 NormalActionInfo）
- ✅ ActionConfig 运行时加载 MoveActionTable 数据
- ✅ 基准速度与 RootMotionData 自动填充

**下一步计划**:
1. 动画同步：`AnimationViewComponent` 实现速度倍率调节
2. 能力层：`ActionCapability` 在动作切换时同步速度倍率
3. 测试验证：完整流程测试（配置→运行时→动画）

---

## 依赖状态

| 模块 | 状态 | 说明 |
|------|------|------|
| `ActionConfig` | ✅ 已完成 | 已实现 `ConstructMoveActionInfo` 与 `PopulateMoveActionFields` |
| `MoveActionInfo` | ✅ 已完成 | 独立的移动动作信息类，包含 RootMotionData |
| `MovementComponent` | ✅ 可用 | 现有速度逻辑可复用，基准速度通过 ActionInfo 传递 |
| `AnimationViewComponent` | ⏳ 需扩展 | 待新增 `SetAnimationSpeed`，支持倍率调节 |
| `ActionCapability` | ⏳ 需扩展 | 待添加动作切换时的速度倍率同步 |
| `SkillActionEditor` | ✅ 已完成 | 已支持所有动作类型的读写，按类型分发 |
| `TableConfig` | ✅ 可用 | 支持新表加载，需注册 `TbMoveActionTable` |

---

## 任务清单

### ✅ 已完成（v0.3.0）

- [x] 配置表：新增 `MoveActionTable.csv` 数据并接入 Luban 生成
- [x] 编辑器架构：实现组合模式，支持多类型动作扩展数据
- [x] 数据读取：实现 `MoveActionExtensionReader` 读取 MoveActionTable
- [x] 数据写入：实现 `MoveActionExtensionWriter` 写入 MoveActionTable
- [x] 速度计算：自动根据 RootMotionData 计算移动速度
- [x] 编辑器集成：在 `ActionDataDispatcher` 中集成导出流程
- [x] 触发帧修复：修复技能触发帧信息读取和显示问题
- [x] 运行时架构：创建 `MoveActionInfo` 类（独立于 NormalActionInfo）
- [x] 运行时加载：在 `ActionConfig` 中实现 `ConstructMoveActionInfo`
- [x] 数据填充：实现 `PopulateMoveActionFields` 与 `LoadMoveRootMotionData`
- [x] 类型判断：根据 `ActionType` 自动创建对应的 ActionInfo 类型

### 🔄 进行中（v0.4.0）

- [ ] 动画层：为 `AnimationViewComponent` 新增播放速率调节接口
- [ ] 能力层：扩展 `ActionCapability` 在动作切换时同步速度倍率
- [ ] 测试：编写表数据加载与速度倍率单元/集成测试

---

## 风险与阻塞

- **动画同步风险**：动画速率倍率需与逻辑速度实时同步，存在差帧风险。
- **表数据覆盖**：MoveActionTable 仅补充移动字段，需验证不会覆盖 `ActionTable` 基础数据。
- **编辑器工作流**：技能动作编辑器需支持所有动作类型，旧流程的兼容迁移待规划。

---

## 追溯

- 上游设计：[MoveActionTable 技术设计](../移动-位移/MoveActionTable%20技术设计.md)
- 目标代码：`Assets/Script/AstrumLogic/Managers/ActionConfig.cs`、`Assets/Script/AstrumLogic/Capabilities/ActionCapability.cs`、`Assets/Script/AstrumLogic/Components/MovementComponent.cs`、`Assets/Script/AstrumView/Components/AnimationViewComponent.cs`
- 编辑器入口：`Assets/Script/Editor/RoleEditor/` 内 `SkillActionEditor` 系列文件

---

*文档版本：v0.3.0*  
*创建时间：2025-11-09*  
*最后更新：2025-11-09*  
*状态：开发中*  
*Owner*: Lavender  
*变更摘要*: 
- v0.3.0: 完成运行时架构，创建 MoveActionInfo 类，实现 ActionConfig 加载逻辑
- v0.2.0: 完成编辑器层实现，包括组合模式架构重构、自动速度计算、触发帧修复
- v0.1.0: 新增 MoveActionTable 开发进展文档，记录设计准备阶段任务

