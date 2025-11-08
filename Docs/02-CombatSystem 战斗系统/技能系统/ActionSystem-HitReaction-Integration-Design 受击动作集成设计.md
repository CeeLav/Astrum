# 受击动作集成设计

> 📖 **版本**: v1.0.0  
> 📅 **最后更新**: 2025-11-08  
> 👥 **面向读者**: 战斗逻辑程序、动作系统程序、配置工程师  
> 🎯 **目标**: 明确受击事件驱动动作播放的技术方案，指导代码实现与配置扩展

---

## 概述

受击体系在 v1.2.1 之后完成了“Handler 只读、Capability 执行”的完全封装。当前 `HitReactionCapability` 仍仅负责表现事件和日志，占位的动作播放逻辑没有接入动作系统；同时实体基础配置缺乏受击动作字段，导致无法按职业或模型定制受击表现。本设计旨在：

- 扩展 Action 系统，使其能接收受击事件等外部来源的动作预约；
- 在 `EntityBaseTable` 中新增 `HitAction` 字段，提供配置驱动的受击动作；
- 当 `HitReactionCapability` 收到受击事件时，按照攻击方向调整实体朝向，并通过 Action 系统切换到对应受击动作。

---

## 现状分析

### ActionCapability

- 仅支持两类预订单来源：动作自身 `AutoNextAction` 与输入命令匹配的候选动作；
- 没有面向外部的统一入口来追加预订单动作；
- 预订单列表 `PreorderActions` 每帧由 `CheckActionCancellation` 重建，外部无法安全插入高优先级动作。

### HitReactionCapability

- 只在 `PlayHitAction` 中留有 TODO，占位调用；
- 未与 `ActionComponent`/Action 系统协同，也没有朝向调整逻辑；
- 受击表现依赖配置缺位，无法针对实体差异化。

### EntityBaseTable

- 当前字段列表：`IdleAction`, `WalkAction`, `RunAction`, `JumpAction`, `BirthAction`, `DeathAction`;
- 缺少受击类动作字段，无法从配置层面定制；
- Luban 自动生成代码与 CSV 表结构需同步调整。

---

## 需求解构

1. **功能需求**
   - `HitReactionCapability` 接收 `HitReactionEvent` 后：
     1. 依据实体配置取得受击动作 ID；
     2. 将动作预约推送到动作系统（支持优先级）；
     3. 更新实体朝向，使其面向攻击方向；
     4. 触发表现（特效、音效）。
2. **数据需求**
   - `EntityBaseTable` 新增整数型 `HitAction` 字段，默认值为 0（表示未配置）；
   - 如果实体未配置受击动作或 Action 系统不存在相应动作，则回退逻辑需明确。
3. **表现需求**
   - 受击动作优先级如何与现有动作（移动、攻击、技能）冲突时处理；
   - 朝向调整需保证与移动/击退等能力兼容，避免抖动。

---

## 架构设计

### 流程概览

```
HitReactionEvent (casterId, hitDirection, effectId, ...)
        ↓
HitReactionCapability.OnHitReaction
        ├─ 计算面向方向 → 更新 TransComponent.Yaw
        ├─ 查询 EntityConfig.HitAction
        ├─ 组装受击预订单信息
        └─ 调用 ActionCapability.EnqueueExternalAction(...)
                ↓
        ActionCapability.SelectActionFromCandidates
                ↓
        切换到受击动作 & 更新时间轴
```

### 组件职责扩展

#### ActionCapability（受击动作注入）
- **职责**: 提供统一的外部预约接口，兼容受击、硬直等控制类事件；
- **新增能力**:
  - 持久化外部来源的 `PreorderActionInfo` 列表；
  - 每帧在内部候选生成后合并外部预约，并支持按来源设置优先级；
  - 提供防重复 / 清理机制（例如同一来源重复注入时覆盖）。

#### HitReactionCapability（受击驱动逻辑）
- **职责**: 响应受击事件，协调动作系统与表现层；
- **新增流程**:
  - 校验实体具备 `ActionComponent` 与 `TransComponent`;
  - 调用按钮：`UpdateFacingDirection(hitDirection)`；
  - 查询 `entity.EntityConfig?.HitAction`，无配置或 Action 不存在时仅记录日志；
  - 构造 `PreorderActionInfo`，设置来源标签（如 `HitReaction`）、高优先级、即时切换；
  - 通过 `CapabilitySystem` 获取 `ActionCapability` 并调用外部预约接口。

### 数据同步

受击动作 ID 最终由战斗策划配置 `EntityBaseTable.csv`，Luban 生成 `EntityBaseTable.cs`，在运行时通过 `Entity.EntityConfig.HitAction` 统一访问。Action 系统无需直接依赖 CSV，只通过实体配置获取。

---

## 数据与配置设计

### EntityBaseTable CSV 调整

| 字段名      | 类型 | 默认值 | 说明             |
|-------------|------|--------|------------------|
| `HitAction` | int  | 0      | 受击动作 ID，0 表示未配置 |

- **兼容策略**: 新增字段追加在 `DeathAction` 之后，保持向后兼容；旧数据需补零。
- **生成流程**: 更新 CSV → 运行 Luban 生成工具 → 同步客户端/服务器生成代码。

### 运行时代码读取

- `Entity.EntityConfig.HitAction` 直接读取；
- `ActionCapability` 在 `LoadAvailableActions` 时，如 `HitAction > 0`，尝试预加载该动作；
- 未配置或加载失败时记录 Warning，并在受击时忽略切换。

---

## 实现细节

### ActionCapability 扩展点

```csharp
// 伪代码概览
public void EnqueueExternalAction(Entity entity, ExternalActionRequest request)
{
    var actionComponent = GetComponent<ActionComponent>(entity);
    if (actionComponent == null) return;

    // 根据来源去重/覆盖
    actionComponent.ExternalPreorders[request.SourceTag] = new PreorderActionInfo
    {
        ActionId = request.ActionId,
        Priority = request.Priority,
        TransitionFrames = request.TransitionFrames,
        FromFrame = request.FromFrame,
        FreezingFrames = request.FreezingFrames
    };
}
```

- **外部存储结构**: `Dictionary<string, PreorderActionInfo>`（Key 可使用来源枚举/字符串，如 `"HitReaction"`）；
- **合并策略**: 在 `CheckActionCancellation` 后，`SelectActionFromCandidates` 前，将 `ExternalPreorders` 的值批量加入 `PreorderActions` 并立即清空或按 `Keep` 标记保留；
- **优先级策略**: 受击动作建议使用高于普通移动/攻击动作的优先级（例如固定值 900+），确保即时切换；
- **安全性**: 避免 ActionId=0 或未在 `AvailableActions` 中的 ID 导致异常，调用方需事先校验。

### HitReactionCapability 新逻辑

```csharp
// 伪代码概要
private void HandleHitAction(Entity entity, HitReactionEvent evt)
{
    if (!TryGetHitActionId(entity, out var actionId)) return;
    UpdateFacing(entity, evt.HitDirection);

    var preorder = new ExternalActionRequest
    {
        SourceTag = "HitReaction",
        ActionId = actionId,
        Priority = HitActionPriority, // 常量
        TransitionFrames = HitActionTransitionFrames,
        FromFrame = 0,
        FreezingFrames = DefaultHitFreeze
    };

    ActionCapability.EnqueueExternalAction(entity, preorder);
}
```

- **朝向调整**: 使用 `TransComponent` 或 `PositionComponent` 中的朝向数据，计算水平面的 `TSVector`，忽略竖直分量，调用统一的朝向更新方法（例如设置 `TransComponent.Forward`/Yaw）。需与 Knockback 位移兼容，避免重复归一化；
- **回退逻辑**: 若动作组件或配置缺失，记录日志并继续执行特效/音效逻辑；
- **事件触发**: `OnHitReaction` 中先执行动作与朝向，再触发特效和音效，保证表现连续性。

---

## 测试策略

1. **单元测试**
   - ActionCapability 外部预约接口：重复注入、非法动作 ID、优先级排序；
   - HitReactionCapability 处理逻辑：无 ActionComponent、无 HitAction、合法流程。
2. **集成测试**
   - 模拟受击事件，验证实体动作切换与朝向调整；
   - 与 KnockbackCapability 同时生效，确认移动输入被正确覆盖；
   - 游戏内实测受击表现是否按配置播放。
3. **配置验证**
   - 更新 CSV 后运行 Luban 校验；
   - 确认客户端/服务器生成代码一致；
   - 使用测试实体（例如 entityId=1001）填入受击动作 ID，观测运行效果。

---

## 关键决策与取舍

- **外部动作预约放在 ActionCapability**  
  - 备选方案：HitReactionCapability 直接操作 ActionComponent.CurrentAction  
  - 选定方案：ActionCapability 提供统一入口，保持动作切换逻辑集中，避免重复实现优先级排序  
  - 影响：Action 系统可复用该入口处理硬直、击飞等控制效果。

- **朝向调整在逻辑层完成**  
  - 保证帧同步世界朝向一致，减少 View 层同步成本；由 `TransComponent` 驱动动画根节点。

---

## 相关文档

- [Hit-Reaction-And-Knockback 受击与击退](../技能效果/Hit-Reaction-And-Knockback%20受击与击退.md)
- [Action-System 动作系统](Action-System%20动作系统.md)
- [Event-Queue-System 事件队列系统](../../05-CoreArchitecture%20核心架构/事件/Event-Queue-System%20事件队列系统.md)

---

**文档版本**: v1.0.0  
**创建时间**: 2025-11-08  
**最后更新**: 2025-11-08  
**状态**: 设计中  
**Owner**: Lavender Combat Logic Team  
**变更摘要**: 定义受击事件驱动动作播放的技术实现方案与配置扩展。


