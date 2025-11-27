# ⚔️ 战斗系统文档

## 📖 文档列表

### [战斗系统总览](Combat-System%20战斗系统总览.md)
战斗系统的整体设计和架构

### [技能系统](技能系统/Skill-System%20技能系统.md)
Skill → SkillAction → SkillEffect 三层架构，包含配置表设计和流程示例

### [动作系统](技能系统/Action-System%20动作系统.md)
通用动作系统，支持取消标签、帧事件、动作流转和优先级管理

### [技能效果运行时](技能系统/Skill-Effect-Runtime%20技能效果运行时.md)
运行时执行架构：SkillExecutorCapability、HitManager、SkillEffectManager、DamageCalculator、EffectHandler体系

### [动画系统](技能系统/Animation-System%20动画系统.md)
动画系统实现总结

### [数值系统](数值系统/Stats-System%20数值系统.md)
三层属性架构（Base → Derived → Dynamic）、7个核心组件、完整伤害计算公式、Buff和成长系统

**子文档**:
- [属性组件详细设计](数值系统/Stats-Components%20属性组件.md)
- [伤害计算公式](数值系统/Damage-Calculation%20伤害计算.md)
- [配置表设计](数值系统/Stats-Config-Tables%20配置表.md)

### [受击与击退](技能效果/Hit-Reaction-And-Knockback%20受击与击退.md)
受击反馈和击退效果：HitReactionCapability、KnockbackCapability、基于事件队列的处理流程

### [射击系统](射击系统/Projectile-Shooting-System%20射击系统技术设计.md)
事件驱动的弹道生成、射线判定+穿透体系、轨迹系统

### [输入系统](输入系统/Input-System%20输入系统架构设计.md)
输入系统架构设计

**子文档**:
- [快速开始](输入系统/快速开始.md)
- [鼠标位置处理技术设计](输入系统/鼠标位置处理技术设计.md)

### [物理系统](物理系统/Physics-Design%20物理碰撞检测设计.md)
基于 BEPU Physics v1 的碰撞检测系统，HitManager 统一管理所有攻击碰撞盒

**子文档**:
- [物理系统开发进展](物理系统/Physics-Progress%20物理系统开发进展.md)
- [待完成功能清单](物理系统/Todo-List%20待完成功能清单.md)
- [测试用例清单](物理系统/Test-Cases%20测试用例清单.md)

---

## 🚀 开发进展

> 📊 详细进展请查看：[开发进展汇总](_status%20开发进展/README.md)

**当前状态**:
- ✅ 技能效果系统：v0.6.0 (Beta - 战斗系统完整运行)
- 📝 数值系统：v0.1.0 (策划案完成)
- 📝 受击与击退：v1.1 (设计完成，双模式事件)
- 📝 射击系统：v0.1.0 (设计完成)
- 📝 技能位移：v0.0.0 (准备开发)
- 📝 移动动作表：v0.1.0 (策划案完成)

---

## 🔗 相关文档

- [核心架构](../05-CoreArchitecture%20核心架构/) - ECC架构、事件队列、帧同步
- [编辑器工具](../04-EditorTools%20编辑器工具/) - 技能动作编辑器
- [游戏设计](../01-GameDesign%20游戏设计/) - 战斗概览、游戏循环

---

## 📊 系统状态

| 模块 | 状态 | 完成度 |
|------|------|--------|
| 技能系统 | ✅ 完成 | 90% |
| 动作系统 | ✅ 完成 | 80% |
| 技能效果运行时 | ✅ 完成 | 90% |
| 动画系统 | ✅ 完成 | 70% |
| 数值系统 | 📝 设计完成 | 10% |
| 受击与击退 | 📝 设计完成 | 10% |
| 射击系统 | 📝 设计完成 | 10% |
| 输入系统 | ✅ 完成 | 80% |
| 物理系统 | ✅ 完成 | 80% |

---

**返回**: [文档中心](../README.md)

