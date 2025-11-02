# AI 状态机设计

## 概述

初版AI系统采用状态机架构实现，通过组件和能力分离的方式管理AI行为。

## 架构设计

### AICapability（AI决策层）

**职责**: 控制AI的决策逻辑和状态转换

- 读取 `AIStateMachineComponent` 中的状态数据
- 根据游戏状态（敌人位置、血量、技能CD等）进行决策
- 执行状态转换，更新 `AIStateMachineComponent`
- 在 Tick 中驱动AI行为

**设计要点**:
- 继承自 `BaseCapability`
- 使用 `GetOwnerComponent<AIStateMachineComponent>()` 获取状态机数据
- 通过状态机组件更新当前状态
- 可读取其他组件（如 `PositionComponent`、`HealthComponent`）辅助决策

### AIStateMachineComponent（状态机数据组件）

**职责**: 存放AI状态机的运行时数据

- 记录当前状态类型（枚举或字符串）
- 存储状态切换时间戳（用于超时判断）
- 保存状态相关的参数（如巡逻点、目标ID等）
- 记录状态历史（可选，用于调试）

**设计要点**:
- 继承自 `BaseComponent`
- 纯数据组件，不包含逻辑
- 支持序列化（MemoryPack）
- 状态定义可通过配置表扩展

## AI 原型（子原型 SubArchetype "AI"）

> 目标：将 AI 作为可挂载/卸载的子原型，按需启用或关闭 AI 行为。

- 组成：
  - 组件：`AIStateMachineComponent`
  - 能力：`AIFSMCapability`（调度器） + 若干 `*StateCapability`（如 `IdleStateCapability`、`MoveStateCapability`）
- 职责边界：
  - `AIFSMCapability` 统一评估条件、切换状态并调用对应 `StateCapability`
  - 各 `StateCapability` 仅实现本状态的进入/执行/退出与条件判断
- 生命周期：
  - 挂载：`AttachSubArchetype(entity, "AI")` 初始化状态机（默认状态如 Idle）
  - 卸载：`DetachSubArchetype(entity, "AI")` 清理状态与临时数据
- 与 ECC：默认不影响视图；如需调试可视化，另行定义 View 子原型

最小用法：
```
// 进入战斗
AttachSubArchetype(entity, "AI");

// 剧情接管（关闭AI）
DetachSubArchetype(entity, "AI");
```

## 状态机流程

```
AIStateMachineComponent (数据)
    ↓
AICapability.Tick() (决策)
    ↓
评估条件 → 状态转换 → 执行行为
    ↓
更新 AIStateMachineComponent
```

## 典型状态示例

- **Idle**: 待机状态
- **Patrol**: 巡逻状态
- **Chase**: 追击状态
- **Attack**: 攻击状态
- **Retreat**: 撤退状态

## 扩展方向

- 支持行为树混合使用
- 支持配置表驱动状态转换条件
- 支持状态优先级和中断机制

