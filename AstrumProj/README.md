## AstrumProj - Client Runtime Notes

### 技能系统运行时（阶段3-5）

- 数据模型：`SkillInfo` 仅保存基础信息与 `SkillActionIds`；等级由 `CurrentLevel` 表示。
- 构造动作：注册阶段通过 `SkillConfigManager.CreateSkillActionInstance(actionId, level)` 构造全新的 `SkillActionInfo`，加入 `ActionComponent.AvailableActions`。
- 解析触发帧：`SkillConfigManager.ParseTriggerFrames()` 按 BaseEffectId + Level 获取效果值（后续可扩展插值）。
- 组件/能力：`SkillComponent`（存储技能），`SkillCapability`（学习、初始技能加载、动作注册/注销）。
- 原型：`RoleArchetype` 已集成技能组件与能力；从 `RoleBaseTable` 的 `LightAttackSkillId`、`HeavyAttackSkillId`、`Skill1Id`、`Skill2Id` 自动学习。
- 序列化：`BaseComponent.MemoryPack.cs`/`Capability.MemoryPack.cs` 已注册对应 Union。


