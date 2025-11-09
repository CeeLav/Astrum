# MoveActionTable 开发进展

> 📊 **当前版本**: v0.1.0  
> 📅 **最后更新**: 2025-11-09  
> 👤 **负责人**: Lavender

## TL;DR（四象限）
- 状态/进度：📝 策划案完成，开发准备中
- 已达成：MoveActionTable 技术设计完成，核心约束明确
- 风险/阻塞：技能动作编辑器扩展与运行时整合尚未落地
- 下一步：补充配置表字段、实现 PopulateMoveActionFields、同步动画速度逻辑

## 版本历史

### v0.1.0 - 设计准备完成 (2025-11-09)
**状态**: 📝 策划案完成

**完成内容**:
- [x] 明确 MoveActionTable 设计目标与上下游
- [x] 梳理运行时整合与编辑器扩展需求
- [x] 形成阶段划分与任务清单

**待完成**:
- 运行时 PopulateMoveActionFields 实现
- MovementComponent/AnimationViewComponent 速率同步
- 技能动作编辑器导出 MoveActionTable 数据

**预计工时**: 8-10 天（含联调与测试）

---

## 当前阶段

**阶段名称**: Phase 0 - 设计准备  
**完成度**: 10%

**下一步计划**:
1. 扩展配置：在 `MoveActionTable.csv` 与 `ActionConfig` 中增加移动速度字段加载流程
2. 运行时能力：实现 `ActionCapability`/`MovementComponent` 速度落地与播放倍率计算
3. 编辑器导出：在 `SkillActionEditor` 中补充 MoveActionTable 写入逻辑

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

