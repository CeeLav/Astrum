# 木桩模板系统设计

> 📖 **版本**: v0.1 | **最后更新**: 2025-11-07  
> 🎯 **适用范围**: 技能动作编辑器中的命中调试目标（木桩）功能  
> 👥 **面向读者**: 编辑器工具开发、战斗调试、技术美术  
> ✅ **目标**: 支持在技能动作编辑器内按模板快速布置可配置的木桩用于命中/击退/特效调试

**TL;DR**
- 编辑器新增“木桩模板”概念，模板集合存放在 `Assets/Script/Editor/RoleEditor/Data` 目录下的 ScriptableObject 中。
- 预览面板接入模板选择器，可在不离开技能编辑流程的情况下快速切换木桩布局。
- 模板可视化编辑支持拖拽、偏移、旋转、缩放以及颜色标记，实时反映在 `AnimationPreviewModule` 预览结果中。
- 模板切换不影响技能数据保存，仅作用于调试场景，同步保存在本地配置文件中以便共享。
- Timeline 触发器的命中、击退、特效播放通过“木桩交互层”代理到木桩实例，记录击退路径并在木桩节点上触发命中特效。

## 1. 概述

技能动作编辑器当前缺少可复用的命中调试对象，导致命中判定、击退距离、特效播放等验证流程依赖临时摆放的 GameObject，效率低且难以复现。本设计引入“木桩模板”功能，为编辑器提供标准化的目标集布置能力，并允许开发者在不同模板之间快速切换。

**设计理念**
- **非侵入性**：模板仅影响编辑器预览，不写入技能配置表。
- **模块化**：模板数据独立于技能数据，可在多个技能之间复用。
- **可视化编辑**：在预览面板中实时展示和调整木桩位置，提升调试效率。
- **可扩展**：模板数据结构允许未来扩展至多目标、移动目标等形态。

**系统边界**
- ✅ 负责：模板数据定义、模板选择与编辑、预览中的实例化与交互。
- ❌ 不负责：运行时加载木桩、正式关卡布置、AI/战斗逻辑改动。

## 2. 核心能力

### 2.1 模板管理
- 模板集合（ScriptableObject）用于集中存储多个模板。
- 提供创建、复制、删除模板的工具 UI。
- 支持导出/导入 JSON，实现团队共享。

### 2.2 模板编辑
- 在配置面板新增“木桩模板”折叠区，使用列表编辑模板内的每个木桩实例。
- 支持设置目标别名、偏移、旋转、缩放及调试颜色。
- 允许为单个木桩指定自定义 Prefab（默认使用内置木桩）。

### 2.3 模板应用
- 预览面板顶部提供模板选择下拉框和“立即刷新”按钮。
- 切换模板时，`AnimationPreviewModule` 会重新生成木桩实例并绑定到场景锚点。
- 允许单独隐藏/显示木桩以对比效果。

## 3. 架构设计

### 3.1 组件结构

```
SkillActionEditorWindow
    ├── SkillActionConfigModule
    │   └── HitDummyTemplateInspector (新) - 模板列表与属性编辑
    ├── AnimationPreviewModule
    │   └── HitDummyPreviewController (新) - 实例化与同步木桩
    ├── TimelineEditorModule (保持) - 提供当前帧、碰撞信息
    └── SkillHitDummyTemplateService (新) - 读写模板集合

Assets (数据)
    └── SkillHitDummyTemplateCollection.asset - 默认模板集合
```

### 3.2 数据流

```
模板集合 ScriptableObject
    ↓ 加载 (SkillHitDummyTemplateService)
模板选择 UI (ConfigModule)
    ↓ 选择事件
HitDummyPreviewController
    ↓ 根据模板实例化木桩 GO
AnimationPreviewModule
    ↓ 渲染木桩 + 角色 + 特效
测试者观察命中/击退效果
```

### 3.3 代码改动点（上游 → 下游）
- `SkillActionEditorWindow.cs`：引入模板工具条、生命周期挂钩。
- `SkillActionConfigModule.cs`：新增模板折叠区，连接 Odin UI。
- `AnimationPreviewModule.cs`：托管 `HitDummyPreviewController`，在 `SetEntity` / `SetFrame` 时同步目标。
- `Services` 目录：新增 `SkillHitDummyTemplateService`、`HitDummyPreviewController`、`HitDummyInteractionEvaluator`。
- `Data` 目录：新增 `SkillHitDummyTemplateCollection.cs` 定义（仅在文档阶段规划，尚未提交）。

## 4. 数据结构

### 4.1 模板集合 (`SkillHitDummyTemplateCollection`)
- `Templates: List<SkillHitDummyTemplate>`：模板列表。
- 提供静态 `CreateDefault()` 方法以生成初始模板。

### 4.2 模板 (`SkillHitDummyTemplate`)
- `TemplateId: string`：唯一标识（GUID）。
- `DisplayName: string`：展示名称。
- `Notes: string`：备注信息。
- `BasePrefab: GameObject`：默认引用的木桩 Prefab，可为空。
- `FollowAnchorPosition: bool`：是否跟随角色位置移动。
- `FollowAnchorRotation: bool`：是否对齐角色朝向。
- `LockY: bool`：是否将 Y 值约束到地面。
- `RootOffset/RootRotation/RootScale: Vector3`：整体偏移/旋转/缩放。
- `Placements: List<SkillHitDummyPlacement>`：具体木桩实例列表。

### 4.3 木桩实例 (`SkillHitDummyPlacement`)
- `Name: string`：显示名称和场景对象名。
- `OverridePrefab: GameObject`：单独覆盖 Prefab。
- `Position/Rotation/Scale: Vector3`：相对坐标。
- `DebugColor: Color`：Gizmos/材质调试颜色。

## 5. 实现流程

### 5.1 初始化
1. 启动编辑器时，`SkillHitDummyTemplateService` 尝试从固定路径加载集合：
   - `Assets/Script/Editor/RoleEditor/Data/SkillHitDummyTemplateCollection.asset`
2. 若不存在则创建默认集合并提示用户保存。
3. `SkillActionEditorWindow.OnEnable` 注册模板事件并向预览模块注入控制器。

### 5.2 模板选择
1. 配置面板展示模板下拉框（结合 Odin）与“编辑模板”按钮。
2. 当选择变化时触发 `OnTemplateChanged`：
   - 保存最近一次选择到 `EditorPrefs`，下次启动自动恢复。
   - 通知 `HitDummyPreviewController` 重新构建木桩。

### 5.3 模板编辑
1. “编辑模板”按钮打开 `SkillHitDummyTemplateEditorWindow`（新建）。
2. 在列表中支持：新增、复制、删除模板。
3. 通过 `ReorderableList` 编辑 `Placements`，右侧 Inspector 调整属性。
4. 支持导入导出 JSON 以便共享。

### 5.4 预览同步
1. `HitDummyPreviewController` 在以下时机刷新：
   - 模板选择变更。
   - 预览实体 `SetEntity`。
   - 时间轴帧变化时需要跟随角色（若 `FollowAnchorPosition` 为 true）。
2. 刷新逻辑：
   - 销毁旧木桩实例。
   - 计算锚点（角色根节点或世界原点）。
   - 对每个 `Placement` 实例化 GameObject，应用偏移和调试材质。
3. 若启用了 LockY，将 Y 设置为地面高度（Y=0）。

### 5.5 木桩交互逻辑
1. `HitDummyPreviewController` 为每个木桩挂载 `HitDummyTargetMarker`（新组件），暴露以下接口：
   - `ApplyKnockback(Vector3 direction, float distance)`：执行并记录击退位移，同时绘制箭头与距离数值。
   - `OnSkillEffectTriggered(TriggerFrameData frameData)`：接收技能效果帧信息，刷新命中标记、数值面板。
   - `PlayHitVFX(string resourcePath, Vector3 localOffset)`：调用 `VFXPreviewManager` 在木桩局部坐标系中播放特效。
2. `HitDummyInteractionEvaluator.ProcessFrame(...)`（新服务）负责在每帧解析并调用接口：
   - 输入：当前帧的 `TriggerFrameData` 列表、角色与木桩的世界 Transform。
   - 输出：命中木桩集合、击退方向/距离、需要播放的特效/音效。
3. 命中判定：
   - 按技能效果的 `collisionInfo` 计算碰撞体（球/盒/胶囊）。
   - 与每个木桩的包围盒相交测试，找到命中木桩并计算命中点。
4. 击退应用：
   - 若效果类型包含击退（`EffectType == 3` 或存在 `KnockbackDistance` 扩展字段），Evaluator 根据命中点朝向木桩中心计算单位向量。
   - 调用 `ApplyKnockback`，木桩在预览中即时移动；控制面板展示击退距离、方向角度。
5. 命中特效：
   - 当 `TriggerFrameData` 包含 `type == "VFX"` 或技能效果携带 `HitVFX` 字段时，Evaluator 将请求转发至 `VFXPreviewManager`。
   - 若特效要求跟随木桩，使用 `FollowCharacter=false` 但将父物体设置为木桩 Transform，实现视效固定在木桩位置。
6. 调试反馈：
   - 木桩在命中帧高亮显示（材质变色/启用光环）。
   - 控制面板新增“命中详情”区块，列出命中帧、击退距离、播放的特效资源、命中木桩名称。
   - 提供“重置木桩状态”按钮，快速复位被击退的木桩。

### 5.6 调试辅助
- 在预览窗口添加“显示木桩 Gizmos”勾选项。
- 自动为木桩添加 `HitDummyTargetMarker` 脚本，用于测试击退方向/距离。
- 提供 `Copy World Position` 按钮，便于将调试结果同步到技能表或脚本。

## 6. 关键决策与取舍（轻量 ADR）
- **问题**：模板数据存放在哪？
  - 备选：使用 JSON 文件、ScriptableObject、嵌入技能数据。
  - 选择：ScriptableObject。
  - 影响：可视化编辑、集成 Odin Inspector 更方便；需要团队同步该资产。

- **问题**：模板是否写入技能配置表？
  - 备选：写入 `triggerFrames` 附加字段、仅编辑器使用。
  - 选择：仅编辑器使用。
  - 影响：避免污染表结构，但需要在团队内同步模板资源。

- **问题**：木桩实例化方式？
  - 备选：使用拖拽 Prefab、使用代码生成 Primitive。
  - 选择：默认程序生成圆柱木桩，支持自定义 Prefab。
  - 影响：对资源库依赖最少，保证模板即开即用。

## 7. 上下游关联
- 上游需求：用户提出“在技能动作编辑器中增加可配置木桩调试命中/击退/特效”的需求。
- 下游实现（计划修改文件）：
  - `Assets/Script/Editor/RoleEditor/Data/SkillHitDummyTemplateCollection.cs`
  - `Assets/Script/Editor/RoleEditor/Services/SkillHitDummyTemplateService.cs` (新)
  - `Assets/Script/Editor/RoleEditor/Services/HitDummyPreviewController.cs` (新)
  - `Assets/Script/Editor/RoleEditor/Modules/AnimationPreviewModule.cs`
  - `Assets/Script/Editor/RoleEditor/Modules/SkillActionConfigModule.cs`
  - `Assets/Script/Editor/RoleEditor/Windows/SkillActionEditorWindow.cs`
- 相关文档：
  - `[VFX-Track-Design 特效轨道设计](VFX-Track-Design 特效轨道设计.md)`
  - `[动画根节点位移提取方案](动画根节点位移提取方案.md)`

---

*文档版本：v0.1*  
*创建时间：2025-11-07*  
*最后更新：2025-11-07*  
*状态：策划案*  
*Owner*: Lavender Dev Tools  
*变更摘要*: 首次定义技能动作编辑器木桩模板系统设计方案


