# Astrum ECC（Entity-Component-Capability）结构说明

> 适用范围：Astrum 客户端整体实体系统。本文档面向程序、策划与技术美术，旨在阐明实体-组件-能力的总体架构、数据流、初始化流程、与配置表/视图/帧同步/动作系统的对接方式，以及后续扩展与迁移建议。

## 1. 概述

ECC 结构将实体职责拆分为三层：
- 实体（Entity）：标识与容器，提供生命周期与组件/能力管理接口。
- 组件（Component）：纯数据或轻逻辑的状态单元，彼此解耦，支撑能力运行。
- 能力（Capability）：行为执行单元，基于组件数据做决策与更新（如移动、动作、战斗）。

该架构的目标：易于组合、可配置、低耦合、便于并行/帧同步、支持热更表驱动。

## 2. 系统分层与数据流

顶层数据流（逻辑域）：
1) 初始化：加载配置表（Luban 生成的 `Tables`），建立资源/动作/实体基表映射。
2) 实体创建：根据表或类型绑定确定 Archetype（原型组合），从而获得最终组件/能力集合，并装配到实体。
3) 帧同步：输入采集与帧推进（`LSController`），能力在 Tick 中读取组件状态、更新组件数据。
4) 视图同步：视图组件读取逻辑组件状态，进行渲染/动画/特效播放。

## 3. 核心概念

- Entity：运行时对象，拥有唯一 `UniqueId`、名称、活跃状态，维护 `Components` 与 `Capabilities` 集合，负责驱动 Tick 与序列化（如 MemoryPack）。
- Component：面向数据的模块，例如 `PositionComponent`、`MovementComponent`、`HealthComponent`、`ActionComponent`、`LSInputComponent` 等。
- Capability：面向行为的模块，例如 `MovementCapability`、`ActionCapability`、后续战斗/AI/控制等。能力的 `Tick/Initialize/CanExecute` 基于组件状态。
- Archetype：实体“原型”声明。通过合并基础原型（如 BaseUnit、Combatant、Controllable）形成最终组件/能力集合（如 Role、Monster）。

## 4. Archetype 机制（声明式组合）

目的：将“实体需要哪些组件/能力”的决策，从实体类硬编码迁移为声明式组合（可复用、可扩展、可替换）。

要点：
- 每个 Archetype 类只声明“本体差异”与需要合并的父 Archetype 名（`Merge`）。
- 启动期注册并递归合并，最终得到“去重后的 Components/Capabilities”。
- 对外提供 `Get(name)` 返回 `ArchetypeData`，用于实体装配。

示例（逻辑关系，不含代码）：
- BaseUnit：Position/Movement/Health + MovementCapability
- Combatant：Action + ActionCapability
- Controllable：LSInput + ControlCapability（如存在）
- Role = BaseUnit ∪ Combatant ∪ Controllable + （自身差异）
- Monster = BaseUnit ∪ Combatant + AI 差异

使用策略：
- 类型绑定：给具体 `Entity` 子类绑定一个 Archetype 名（如 `Unit` 绑定 "Role"）。
- 数据绑定：在实体配置表中提供 `ArchetypeName` 字段，在创建时按配置选择。

## 5. 实体生命周期

1) 创建：
   - 决定 Archetype（优先级：显式参数 > 类型绑定 > 配置表 > 旧逻辑回退）。
   - 构造实体，批量 `AddComponent(Type)`、`AddCapability(Type)` 并 `Initialize()`。

2) 运行：
   - 帧同步驱动（`LSController.Tick`），按优先级/条件调用能力 `Tick`，更新组件状态。
   - 视图层订阅或轮询组件数据，更新模型/动画/特效/UI。

3) 销毁：
   - 逆序释放能力与组件，回收资源，解除表/资源引用。

## 6. 组件层（数据）

常见组件：
- PositionComponent：位置/朝向/高度（与 TrueSync `TSVector` 对接）。
- MovementComponent：速度、可移动标志、移动状态。
- HealthComponent：生命、护盾、死亡标志。
- ActionComponent：动作系统状态、当前动作、队列与候选。
- LSInputComponent：帧同步输入（Q31.32 → FP 转换）。

设计准则：
- 组件尽量保持“无副作用/轻逻辑”，避免直接做系统级调用（如加载资源）。
- 避免组件间互相引用，跨组件依赖由能力在运行期取用。

## 7. 能力层（行为）

常见能力：
- MovementCapability：读取 `LSInputComponent`、`MovementComponent`、`PositionComponent` 实现移动与阈值判断。
- ActionCapability：管理动作的初始化、选择、取消、推进，与配置表 `ActionTable`、角色/技能表对接。
- 其它（可扩展）：Combat/AI/Control/Skill/AbilityGraph等。

设计准则：
- 能力使用 `GetOwnerComponent<T>()/OwnerHasComponent<T>()` 检查前置条件，`CanExecute()` 早返回。
- 能力严禁静态跨实体状态，确保帧同步可重放与 MemoryPack 序列化正确。

## 8. 配置表映射（Luban）

主要表（参考 `AstrumConfig/Doc/表格配置说明.md`）：
- EntityBaseTable：实体基础配置（模型、动作等通用字段）。
- RoleBaseTable/RoleGrowthTable：角色属性与成长、技能绑定。
- ActionTable：动作定义（动画、持续、命令、优先级、取消标签、被取消标签等）。
- SkillTable/SkillEffectTable/SkillActionTable/SkillUpgradeTable：技能相关。

建议新增或复用字段：
- ArchetypeName：为实体行指明原型组合，驱动创建时的组件/能力装配。

加载流程：
1) 启动时加载 `Tables`（Luban 生成），完成 `ResolveRef()`。
2) 实体创建前，依据表中 `EntityId/ArchetypeName/模型/动作` 准备装配参数。
3) 能力在 Initialize/Tick 中查询与缓存所需表项（如 ActionInfo）。

## 9. 视图层对接

视图组件（ViewComponent）与逻辑组件映射：
- TransViewComponent ← PositionComponent（位置）/MovementComponent（运动状态）。
- AnimationViewComponent ← ActionComponent（动作名/状态），结合 Animancer 播放；Animator root motion 关闭，逻辑主导位移。
- 其它：血条、选中指示、特效挂点根据逻辑状态更新。

对齐策略：
- 视图层不直接依赖能力；读取逻辑组件派生的“可视状态”（例如是否移动、当前动作名）。
- 严格区分逻辑与表现，必要时通过事件或轻量桥接同步。

## 10. 帧同步与输入

- 帧驱动：`LSController` 维护 `AuthorityFrame/PredictionFrame/FrameBuffer`，推进 Tick。
- 输入：`InputManager` 采集 → `LSInputComponent` 存储为帧数据（Q31.32），在能力（如 Movement）中转换为 `FP` 并应用。
- 可预判与回滚：状态快照通过 MemoryPack 序列化 `World/Entity` 进行保存与加载。

关键约束：
- 能力与组件的更新必须确定性（固定时间步长、TrueSync 类型、无非确定性 API）。
- 不允许视图/IO 影响逻辑状态。

## 11. 网络与序列化

- 序列化：采用 MemoryPack，对 `World/Entity/Component/Capability` 的必要字段进行序列化。
- 网络：输入转发与帧状态同步遵循房间/控制器协议，实体的可还原性依赖组件/能力的数据完备。

## 12. 扩展与命名/目录规范

- 目录建议：
  - `Assets/Script/AstrumLogic/Core`：Entity/World/Factory 等核心。
  - `Assets/Script/AstrumLogic/Components`：组件实现。
  - `Assets/Script/AstrumLogic/Capabilities`：能力实现。
  - `Assets/Script/AstrumLogic/Archetypes`：原型声明、内置组合与注册器。
  - `Assets/Script/AstrumView/*`：视图与桥接。

- 命名建议：
  - 组件以 `*Component` 结尾；能力以 `*Capability` 结尾；原型以 `*Archetype` 结尾。
  - 原型名保持业务语义一致（如 BaseUnit/Combatant/Controllable/Role/Monster）。

## 13. 调试与排错

- 组件/能力缺失：能力 `Initialize/CanExecute` 打印缺失列表，定位装配环节（Archetype 或 Factory）。
- 表配置错误：Luban 生成时检查；运行期校验关键字段（如 `ArchetypeName` 是否存在）。
- 非确定性：检查使用的数学/时间/随机 API；统一用 TrueSync 类型与固定时间步。
- 视图不同步：核对逻辑 → 视图桥接链路，确保逻辑状态已更新并被正确消费。

## 14. 迁移建议（从硬编码到 Archetype）

1) 引入 Archetype 基础设施：Attribute + Registry + 内置原型（BaseUnit/Combatant/Controllable/Role/Monster）。
2) 新增 `EntityFactory`：从 Archetype 扁平集合装配组件/能力；保留旧路径回退。
3) 给关键实体（如 `Unit`）添加类型绑定到 "Role"，不改原硬编码方法（先兼容）。
4) 将调用点逐步切换为 `EntityFactory.Create(..., archetypeName?)`。
5) 如需表驱动，在 `EntityBaseTable` 增加 `ArchetypeName` 并接入加载与创建流程。
6) 验证移动/动作/动画链路一致后，删除实体类中硬编码的 `GetRequired*`。

## 15. 术语表

- ECC：Entity-Component-Capability 架构。
- Archetype：原型声明，用于组合组件/能力的高层抽象。
- Luban：表生成与运行时读取工具链。
- TrueSync：定点数学库，保障确定性。
- MemoryPack：高性能序列化库。

---

版本：v1.0  
位置：`AstrumConfig/Doc/ECC结构说明.md`  
关联策划案：`AstrumConfig/Doc/Archetype结构说明.md`（原型系统的详细规则与视图原型映射，请见该文档）  
说明：本说明覆盖整个 ECC 系统，并为后续优化（如 Archetype 改造）提供一致的上下文与规范依据。


