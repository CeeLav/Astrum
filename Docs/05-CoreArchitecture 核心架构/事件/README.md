# 事件系统文档

## 📖 设计文档

### [事件队列系统](Event-Queue-System%20事件队列系统.md) ⭐ 新增
全局事件队列架构设计，替代传统的订阅-发布模式

**核心内容**:
- EntityEventQueue - 全局事件队列
- 拉取模式 vs 推送模式
- Capability 主动消费事件
- 性能优化：对象池、批量处理
- 迁移指南：从 EventSystem 迁移到 EntityEventQueue

**设计优势**:
- ✅ 无需管理订阅/取消订阅
- ✅ 避免内存泄漏
- ✅ 更好的性能
- ✅ 清晰的职责分离

---

## 🎯 使用场景

### Logic层事件（使用 EntityEventQueue）
- 技能效果（伤害、治疗、击退、Buff、Debuff）
- 实体状态变化
- 战斗逻辑事件
- AI决策事件

### View层事件（使用 EventSystem）
- UI交互事件
- 特效播放事件
- 音效触发事件
- 视觉反馈事件

---

## 🔗 相关文档

- [受击与击退](../../02-CombatSystem%20战斗系统/技能效果/Hit-Reaction-And-Knockback%20受击与击退.md) - 事件消费示例
- [ECC系统](../ECC/ECC-System%20ECC结构说明.md) - Capability架构

---

**返回**: [核心架构](../README.md)

