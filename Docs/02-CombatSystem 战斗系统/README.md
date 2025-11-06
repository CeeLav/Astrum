# ⚔️ 战斗系统文档

## 📖 策划文档

### [战斗系统总览](Combat-System%20战斗系统总览.md)
战斗系统的整体设计和架构

### [技能系统](Skill-System%20技能系统.md)
完整的技能系统设计：Skill → SkillAction → SkillEffect 三层架构

**核心内容**:
- 技能概念层 (Skill)
- 技能执行层 (SkillAction)
- 技能效果层 (SkillEffect)
- 配置表设计
- 技能流程示例

### [动作系统](Action-System%20动作系统.md)
通用动作系统设计，支持取消标签、帧事件等

**核心内容**:
- 动作帧时间轴
- 取消标签系统
- 动作流转逻辑
- 优先级管理

### [技能效果运行时](Skill-Effect-Runtime%20技能效果运行时.md)
技能效果的运行时执行架构

**核心内容**:
- SkillExecutorCapability - 技能动作执行
- HitManager - 碰撞检测管理
- SkillEffectManager - 效果管理
- DamageCalculator - 伤害计算
- EffectHandler 体系

### [动画系统](Animation-System%20动画系统.md)
动画系统实现总结

### [数值系统](数值系统/Stats-System%20数值系统.md) ⭐ 新增
完整的数值体系设计：属性、战斗、成长、Buff

**核心内容**:
- 三层属性架构（Base → Derived → Dynamic）
- 7个核心组件（BaseStats, DerivedStats, DynamicStats, Buff, State, Level, Growth）
- 完整伤害计算公式（防御、抗性、暴击、命中、格挡）
- Buff系统和成长系统
- 配置表扩展方案（RoleBaseTable, RoleGrowthTable, BuffTable）

**子文档**:
- [属性组件详细设计](数值系统/Stats-Components%20属性组件.md) - 7个核心组件实现
- [伤害计算公式](数值系统/Damage-Calculation%20伤害计算.md) - 完整伤害计算流程
- [配置表设计](数值系统/Stats-Config-Tables%20配置表.md) - 配置表扩展和数值规则

### [受击与击退](技能效果/Hit-Reaction-And-Knockback%20受击与击退.md) ⭐ 新增
受击反馈和击退效果的运行时实现

**核心内容**:
- HitReactionCapability - 受击处理
- KnockbackCapability - 击退位移
- KnockbackComponent - 击退数据
- 基于事件队列的处理流程

---

## 🚀 开发进展

- [技能效果运行时开发进展](_status%20开发进展/Skill-Effect-Progress%20技能效果开发进展.md) - ✅ **v0.6.0** (Beta - 战斗系统完整运行)
- [数值系统开发进展](_status%20开发进展/Stats-System-Progress%20数值系统开发进展.md) - 📝 **v0.1.0** (策划案完成) ⭐ 新增

**当前状态**:
- ✅ 技能效果系统：Phase 1-6 完成，核心战斗流程完全跑通，实战验证成功 🎉
- 📝 数值系统：策划案完成，待开始实现（预计48-60小时）

---

## 🔗 相关文档

- [物理系统](../03-PhysicsSystem%20物理系统/) - 碰撞检测依赖
- [编辑器工具](../04-EditorTools%20编辑器工具/) - 技能动作编辑器
- [核心架构](../05-CoreArchitecture%20核心架构/) - ECC架构

---

## 📊 系统状态

| 模块 | 状态 | 完成度 |
|------|------|--------|
| 技能系统 | ✅ 完成 | 90% |
| 动作系统 | ✅ 完成 | 80% |
| 技能效果运行时 | ✅ 完成 | 90% |
| 动画系统 | ✅ 完成 | 70% |
| 数值系统 | 📝 设计完成 | 10% |

---

**返回**: [文档中心](../README.md)

