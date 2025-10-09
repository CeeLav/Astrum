## AstrumProj - Client Runtime Notes

### 技能系统运行时（阶段3-5）

- 数据模型：`SkillInfo` 仅保存基础信息与 `SkillActionIds`；等级由 `CurrentLevel` 表示。
- 构造动作：注册阶段通过 `SkillConfigManager.CreateSkillActionInstance(actionId, level)` 构造全新的 `SkillActionInfo`，加入 `ActionComponent.AvailableActions`。
- 解析触发帧：`SkillConfigManager.ParseTriggerFrames()` 按 BaseEffectId + Level 获取效果值（后续可扩展插值）。
- 组件/能力：`SkillComponent`（存储技能），`SkillCapability`（学习、初始技能加载、动作注册/注销）。
- 原型：`RoleArchetype` 已集成技能组件与能力；从 `RoleBaseTable` 的 `LightAttackSkillId`、`HeavyAttackSkillId`、`Skill1Id`、`Skill2Id` 自动学习。
- 序列化：`BaseComponent.MemoryPack.cs`/`Capability.MemoryPack.cs` 已注册对应 Union。

### 编辑器工具

#### 动作编辑器
- **入口**: `Tools > Action Editor`
- **功能**: 编辑 `ActionTable`，管理角色动作、动画、取消标签等
- **详细文档**: `../AstrumConfig/Doc/Editor-Tools/动作编辑器/`

#### 技能动作编辑器
- **入口**: `Tools > Astrum > Editors > Skill Action Editor`
- **功能**: 编辑 `SkillActionTable`，继承动作编辑器功能并扩展技能专属配置
  - 技能信息关联（SkillId、成本、冷却）
  - 攻击碰撞盒配置
  - 触发帧效果配置（后续支持时间轴可视化）
- **详细文档**: `../AstrumConfig/Doc/Editor-Tools/技能动作编辑器/`
- **开发状态**: Phase 1 完成（基础框架）


