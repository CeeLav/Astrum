# MoveActionTable 开发进展

> 📊 **当前版本**: v0.5.0  
> 📅 **最后更新**: 2025-11-09  
> 👤 **负责人**: Lavender

## TL;DR（四象限）
- 状态/进度：✅ 核心功能完成 - 角色编辑器集成 EntityStatsTable 完成
- 已达成：角色编辑器可完整读写 15 个基础属性，数值转换自动化
- 风险/阻塞：AnimationViewComponent 应用倍率待实现（表现层）
- 下一步：实现 AnimationViewComponent 速度倍率应用、完整流程测试

## 版本历史

### v0.5.0 - 角色编辑器集成 EntityStatsTable (2025-11-09)
**状态**: ✅ 已完成

**完成内容**:
- [x] 创建 `EntityStatsTableData` 映射类（16 个字段）
- [x] 扩展 `RoleEditorData` 添加 11 个新属性字段
- [x] 修改 `RoleDataReader` 读取 `EntityStatsTable`
- [x] 修改 `RoleDataWriter` 写入 `EntityStatsTable`
- [x] 实现数值转换逻辑（编辑器 float ↔ 表格 int）
- [x] 添加 Odin Inspector UI 特性（分组、Range、InfoBox）

**架构改进**:
- ✅ 角色编辑器现在可以完整管理 `EntityStatsTable` 的所有属性
- ✅ 数值转换自动化：速度 ×1000、百分比 ×10、回复 ×1000
- ✅ UI 分组优化：基础属性、进阶属性（暴击/命中/格挡/抗性/资源）
- ✅ 向后兼容：缺失数据自动使用默认值

**新增字段**:
- 暴击系统：`BaseCritRate`、`BaseCritDamage`
- 命中闪避：`BaseAccuracy`、`BaseEvasion`
- 格挡系统：`BaseBlockRate`、`BaseBlockValue`
- 抗性系统：`PhysicalRes`、`MagicalRes`
- 资源系统：`BaseMaxMana`、`ManaRegen`、`HealthRegen`

**预计工时**: 已完成

## 版本历史

### v0.4.0 - 速度架构重构完成 (2025-11-09)
**状态**: ✅ 核心完成

**完成内容**:
- [x] 修正速度决定逻辑：移动速度由 `EntityStatsTable.baseSpeed` 决定
- [x] `MoveActionTable.moveSpeed` 作为动画参考速度（不影响实际移动）
- [x] 实现 `SyncCharacterSpeedToMovement()`：从 `DerivedStatsComponent.SPD` 同步到 `MovementComponent`
- [x] 修改 `UpdateMovementAndAnimationSpeed()`：正确计算动画倍率
- [x] 动画倍率公式：`实际速度 / 动画设计速度`
- [x] 支持 Buff 修饰速度后的动画同步

**架构改进**:
- ✅ 角色速度来源：`EntityStatsTable.baseSpeed` → `BaseStatsComponent.SPD` → `DerivedStatsComponent.SPD` → `MovementComponent.Speed`
- ✅ 动画参考速度：`MoveActionTable.moveSpeed` → `MoveActionInfo.BaseMoveSpeed`
- ✅ 动画倍率计算：每帧自动同步，支持 Buff 动态修改速度

**待完成**:
- AnimationViewComponent 应用 `AnimationSpeedMultiplier` 到动画播放
- 完整流程测试验证

**预计工时**: 剩余 1 天（表现层实现与测试）

### v0.3.0 - 运行时架构完成 (2025-11-09)
**状态**: ✅ 已完成

**完成内容**:
- [x] 创建 `MoveActionInfo` 类（继承自 `ActionInfo`）
- [x] 在 `ActionInfo` 中添加 Union(2) 注册
- [x] 实现 `ActionConfig.ConstructMoveActionInfo` 方法
- [x] 实现 `ActionConfig.PopulateMoveActionFields` 方法
- [x] 实现 `ActionConfig.LoadMoveRootMotionData` 方法
- [x] 根据 `ActionType` 自动创建对应的 ActionInfo 类型

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

### ✅ 已完成（v0.4.0）

- [x] 配置表：新增 `MoveActionTable.csv` 数据并接入 Luban 生成
- [x] 编辑器架构：实现组合模式，支持多类型动作扩展数据
- [x] 数据读取：实现 `MoveActionExtensionReader` 读取 MoveActionTable
- [x] 数据写入：实现 `MoveActionExtensionWriter` 写入 MoveActionTable
- [x] 速度计算：自动根据 RootMotionData 计算动画参考速度
- [x] 编辑器集成：在 `ActionDataDispatcher` 中集成导出流程
- [x] 触发帧修复：修复技能触发帧信息读取和显示问题
- [x] 运行时架构：创建 `MoveActionInfo` 类（独立于 NormalActionInfo）
- [x] 运行时加载：在 `ActionConfig` 中实现 `ConstructMoveActionInfo`
- [x] 数据填充：实现 `PopulateMoveActionFields` 与 `LoadMoveRootMotionData`
- [x] 类型判断：根据 `ActionType` 自动创建对应的 ActionInfo 类型
- [x] 速度架构：修正为 `EntityStatsTable.baseSpeed` 决定角色移动速度
- [x] 速度同步：实现 `SyncCharacterSpeedToMovement()` 从属性系统同步速度
- [x] 动画倍率：修改 `UpdateMovementAndAnimationSpeed()` 正确计算倍率
- [x] Buff 支持：动画倍率自动响应速度 Buff 修饰

### 🔄 进行中（v0.6.0）

- [ ] 动画层：为 `AnimationViewComponent` 应用 `AnimationSpeedMultiplier`
- [ ] 测试：编写完整流程测试（配置→属性→移动→动画）

### ✅ 已完成（v0.5.0）

- [x] 创建 `EntityStatsTableData` 映射类（16 个字段）
- [x] 扩展 `RoleEditorData` 添加 11 个新属性字段
- [x] 修改 `RoleDataReader` 读取 `EntityStatsTable`
- [x] 修改 `RoleDataWriter` 写入 `EntityStatsTable`
- [x] 实现数值转换逻辑（编辑器 float ↔ 表格 int）
- [x] 添加 Odin Inspector UI 特性（分组、Range、InfoBox）

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

*文档版本：v0.5.0*  
*创建时间：2025-11-09*  
*最后更新：2025-11-09*  
*状态：核心完成*  
*Owner*: Lavender  
*变更摘要*: 
- v0.5.0: 角色编辑器集成 EntityStatsTable，完整支持 15 个基础属性的读写
- v0.4.0: 修正速度架构，移动速度由 EntityStatsTable 决定，动画倍率正确计算
- v0.3.0: 完成运行时架构，创建 MoveActionInfo 类，实现 ActionConfig 加载逻辑
- v0.2.0: 完成编辑器层实现，包括组合模式架构重构、自动速度计算、触发帧修复
- v0.1.0: 新增 MoveActionTable 开发进展文档，记录设计准备阶段任务

