# MoveActionTable 开发进展

> 📊 **当前版本**: v0.2.0  
> 📅 **最后更新**: 2025-11-09  
> 👤 **负责人**: Lavender

## TL;DR（四象限）
- 状态/进度：🚧 开发中 - 配置层与编辑器已完成，运行时整合进行中
- 已达成：MoveActionTable 配置表、编辑器读写、RootMotionData 支持
- 风险/阻塞：运行时 ActionConfig 加载与动画速度同步待实现
- 下一步：实现 PopulateMoveActionFields、同步动画速度逻辑、测试验证

## 版本历史

### v0.2.0 - 配置层与编辑器完成 (2025-11-09)
**状态**: 🚧 开发中

**完成内容**:
- [x] MoveActionTable CSV 表结构（actionId, moveSpeed, rootMotionData）
- [x] MoveActionTableData 映射类与 Luban 配置
- [x] 编辑器读取逻辑（SkillActionDataReader 支持所有动作类型）
- [x] 编辑器写入逻辑（按类型分发到不同表）
- [x] RootMotionData 字段支持（与 SkillActionTable 一致）
- [x] 移动速度自动计算（基于 RootMotion 数据）

**待完成**:
- 运行时 ActionConfig.PopulateMoveActionFields 实现
- MovementComponent 基准速度应用
- AnimationViewComponent 动画速度倍率同步

**预计工时**: 剩余 3-4 天（运行时整合与测试）

### v0.1.0 - 设计准备完成 (2025-11-09)
**状态**: ✅ 已完成

**完成内容**:
- [x] 明确 MoveActionTable 设计目标与上下游
- [x] 梳理运行时整合与编辑器扩展需求
- [x] 形成阶段划分与任务清单

---

## 当前阶段

**阶段名称**: Phase 1 - 配置层与编辑器实现  
**完成度**: 70%

**已完成**:
- ✅ MoveActionTable CSV 表结构定义
- ✅ 编辑器读取所有动作类型（ActionTable 驱动）
- ✅ 编辑器按类型分发写入（ActionTable/SkillActionTable/MoveActionTable）
- ✅ RootMotionData 字段支持

**下一步计划**:
1. 运行时加载：实现 `ActionConfig.PopulateMoveActionFields`
2. 速度应用：`MovementComponent` 接入基准速度
3. 动画同步：`AnimationViewComponent` 实现速度倍率调节

---

## 依赖状态

| 模块 | 状态 | 说明 |
|------|------|------|
| `ActionConfig`/`ActionCapability` | ⏳ 需扩展 | 待添加 `PopulateMoveActionFields` 与播放速度同步 |
| `MovementComponent` | ✅ 可用 | 现有速度逻辑可复用，需接入基准速度 |
| `AnimationViewComponent` | ⏳ 需扩展 | 待新增 `SetAnimationSpeed`，支持倍率调节 |
| `SkillActionEditor` | ⏳ 需扩展 | 导出流程未写入 MoveActionTable |
| `TableConfig` | ✅ 可用 | 支持新表加载，需注册 `TbMoveActionTable` |

---

## 任务清单

- [ ] 配置表：新增 `MoveActionTable.csv` 数据并接入 Luban 生成
- [ ] 运行时：在 `ActionConfig` 中实现 `PopulateMoveActionFields`
- [ ] 逻辑层：在 `MovementComponent` 读取并应用基准速度
- [ ] 动画层：为 `AnimationViewComponent` 新增播放速率调节接口
- [ ] 能力层：扩展 `ActionCapability` 在动作切换时同步速度倍率
- [ ] 编辑器：在 `SkillActionEditor` 导出流程写入 MoveActionTable
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

*文档版本：v0.1.0*  
*创建时间：2025-11-09*  
*最后更新：2025-11-09*  
*状态：策划案*  
*Owner*: Lavender  
*变更摘要*: 新增 MoveActionTable 开发进展文档，记录设计准备阶段任务。

