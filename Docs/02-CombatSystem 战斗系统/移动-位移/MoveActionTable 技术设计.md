# MoveActionTable 技术设计

## 概述

`MoveActionTable` 用于对 `ActionTable` 中标记为 `move` 类型的动作补充移动速度信息，使逻辑移动能力与动画展示能够一一对应。表数据位于 `AstrumConfig/Tables/Datas/Entity/`，与 `ActionTable` 共享 `actionId` 作为主键。运行时由 `TableConfig` 加载，`ActionConfig` 在构建动作信息时读取该表，为 `MovementComponent` 与动画系统提供基础速度参考。

**技能动作编辑器统一入口**：后续所有动作统一通过技能动作编辑器（`SkillActionEditor`）维护。该编辑器放开对所有 `ActionType` 的编辑能力，并在导出时根据动作类型调用对应的写入逻辑；`move` 类型动作在此流程中生成 `MoveActionTable` 数据。

- **上游**：技能动作编辑器（统一编辑入口，导出动作数据时写入 MoveActionTable）
- **下游**：`MovementComponent`（逻辑移动速度）、`AnimationViewComponent`（动画播放速度）、`ActionCapability`（动作状态机）

## 架构设计

### 数据流

```text
SkillActionEditor (所有动作)
        │ 导出
        ├─ ActionTable.csv
        ├─ SkillActionTable.csv
        ▼
   (ActionType=move)
        │
        ▼
MoveActionTable.csv (Entity)
        │ Luban 生成
        ▼
cfg.Entity.TbMoveActionTable
        │ TableConfig 初始化
        ▼
ActionConfig.PopulateMoveActionFields
        │
        ├─ MovementComponent.Speed 基准值
        └─ AnimationViewComponent.SetAnimationSpeed(实际速度 / 基准速度)
```

### 关键约束

- `MoveActionTable` 不覆盖 `ActionTable`，仅补充移动相关字段。
- `actionId` 必须与 `ActionTable` 中的对应动作一致；缺失行视为无速度配置。
- 移动速度以整数存储（缩放 1000），运行时转换为 `TrueSync.FP`。
- 技能动作编辑器负责所有动作导出，按照 `ActionType` 分派扩展逻辑。

## MoveActionTable 字段定义

| 字段名 | 类型 | 说明 | 默认值 |
|---|---|---|---|
| actionId | int | 与 `ActionTable` 同步的动作 ID | 0 |
| moveSpeed | int | 基础移动速度，存储为 `floor(speed * 1000)`（逻辑单位：米/秒） | 0 |

**速度换算**：

```text
FP baseSpeed = (FP)moveSpeed / 1000;
```

当表中速度为 0 或缺失时，保留现有 `MovementComponent.Speed`（通常由角色成长表或默认值提供）。

## 运行时整合

### ActionConfig 扩展

1. `TableConfig` 初始化时加载 `TbMoveActionTable`。
2. 在 `ActionConfig.GetAction`/`PopulateBaseActionFields` 之后新增 `PopulateMoveActionFields`：
   - 查询 `MoveActionTable`，若存在条目，则将 `moveSpeed` 转换为 `FP`，缓存到动作信息或传递至移动组件。
   - 若实体当前速度与基准速度不一致，计算播放速度倍率：

```text
float playbackSpeed = actualSpeed / baseSpeed;
```

3. 将倍率传递给 `AnimationViewComponent`（需在后续实现 `SetAnimationSpeed`），并在动作切换时刷新。
4. 边界处理：若 `baseSpeed` 为 0，则播放速度保持 1，逻辑速度不调整。

### MovementComponent 协调

- `MovementCapability` 维持现有输入驱动逻辑，从配置获得的 `FP` 速度作为基准值。
- 若角色被动修改速度（Buff、装备等），需在动画侧同步倍率，使表现与逻辑一致。

## 编辑器支持

### SkillActionEditor 扩展

- 技能动作编辑器成为所有动作的统一编辑工具：
  - 保留现有 `ActionEditorData`/`SkillActionEditorData` 数据结构，界面开放 `ActionType` 选择不限于 `skill`。
  - 导出流程统一由 `SkillActionDataWriter`（或升级版本）处理，根据 `ActionType` 决定目标表：
    - `move` → 写入 `ActionTable` 基础字段，并调用 `MoveActionExporter` 写入 `MoveActionTable`。
    - `skill` → 同时写入 `SkillActionTable`（含 Root Motion 数据等）。
    - 其他类型 → 仅写入基础 `ActionTable`。

### MoveActionTable 写入流程

- 当 `ActionType == "move"`：
  1. 通过 `AnimationRootMotionExtractor` 获取 `RootMotionDataArray` 或直接采样动画 delta。
  2. 计算总位移（取水平方向 `sqrt(x^2 + z^2)` 累加），总时长 = `Duration / 20` 秒。
  3. 平均速度 `speed = totalDistance / totalTime`。
  4. 写入 `moveSpeed = Mathf.FloorToInt(speed * 1000)`；若 `Duration` 为 0，则记录 0 并提示检查动画设置。
- 编辑器界面提供只读展示，显示检测到的平均速度、占位动画路径、导出状态，方便校验。

### 迁移策略

- 第一阶段：技能动作编辑器允许打开所有动作，但保留旧入口作为兼容，提示开发者迁移。
- 第二阶段：逐步将非技能动作导入技能动作编辑器并验证导出流程。
- 最终阶段：关闭旧动作编辑器入口，仅保留技能动作编辑器作为统一界面。

## 数据验证与测试

1. **配置检查**：导出后通过 Luban 生成最新表；确认生成的 `MoveActionTable` 包含所有 `move` 动作。
2. **运行时日志**：`ActionConfig` 在加载 `move` 动作时输出调试日志（可开关）确认基准速度与倍率。
3. **动画验证**：在编辑器或运行时观察移动动画播放速度，验证逻辑速度变化时动画同步调整。
4. **异常场景**：
   - 动画无位移：应生成 0 速度并提示。
   - 动画持续时间与动作持续帧不匹配：以 `Duration` 为准，确保动作裁剪后的速度一致。

## 关键决策与取舍

- **整数存储**：延续 `SkillActionTable.RootMotionData` 的缩放策略，避免浮点序列化误差。
- **平均速度算法**：选择总位移 / 总时间，兼顾多段曲线的统一表现；未来若需曲线速率，可在编辑器侧扩展。
- **统一编辑入口**：技能动作编辑器作为统一界面，降低工具维护成本，集中导出逻辑；短期需要兼容旧工作流。
- **播放速度调整**：统一由 `AnimationViewComponent` 控制，保持逻辑-视觉分离，便于后续扩展不同动作的速率曲线。

---

*文档版本：v1.1*  
*创建时间：2025-11-09*  
*最后更新：2025-11-09*  
*状态：策划案*  
*Owner*: Lavender  
*变更摘要*: 更新技能动作编辑器为统一入口并调整 MoveActionTable 写入流程。
