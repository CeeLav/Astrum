# 🏗️ 核心架构文档

## 📖 文档列表

### [数值系统](Stats-System%20数值系统.md) ⭐ 新增

完整的数值体系设计：属性、战斗、成长、Buff

**核心内容**:
- 三层属性架构（Base → Derived → Dynamic）
- 7个核心组件详细设计
- 完整伤害计算公式
- Buff系统和成长系统
- 配置表扩展方案

### [ECC结构说明](ECC-System%20ECC结构说明.md)

Entity-Component-Capability 架构设计

**核心架构图**: [Diagrams 架构图/ECC.puml](Diagrams%20架构图/ECC.puml)

**核心概念**:

- **Entity (实体)** - 游戏对象的容器
- **Component (组件)** - 存储数据
- **Capability (能力)** - 处理逻辑

**优势**:

- 数据与逻辑分离
- 高度模块化
- 易于扩展和维护

### [Archetype结构说明](Archetype-System%20Archetype结构说明.md)

实体原型系统，用于统一创建和管理实体

**功能**:

- 预定义实体模板
- 自动装配组件和能力
- 类型安全的创建流程

### [序列化最佳实践](Serialization-Best-Practices%20序列化最佳实践.md)

回滚系统中的序列化设计规范

**关键原则**:

- **ID引用代替对象引用** - 避免重复序列化
- **Dictionary优于List** - 保证引用一致性
- **[MemoryPackIgnore]标记** - 控制序列化范围
- **访问器模式** - 通过ID查找对象

**解决的问题**:

- 回滚后对象引用不一致
- 序列化大小优化
- 数据完整性保证

### [序列化检查清单](Serialization-Checklist%20序列化检查清单.md)

代码审查时的快速检查清单

**检查项**:

- ✅ 是否使用ID引用
- ✅ 集合类型是否合理
- ✅ 是否正确标记忽略字段
- ✅ 是否实现访问器
- ✅ 是否添加验证日志

---

## 🎨 架构图

位于 [Diagrams 架构图/](Diagrams%20架构图/) 目录，使用 PlantUML 格式：

- **ECC.puml** - ECC架构总览
- **AstrumClient.puml** - 客户端架构
- **AstrumView.puml** - 表现层架构
- **LogicCore.puml** - 逻辑核心架构
- **GameClientFlow.puml** - 游戏客户端流程

**查看方式**:
- VS Code: 安装 PlantUML 插件
- IDEA: 内置 PlantUML 支持
- 在线: [PlantUML Web Server](http://www.plantuml.com/plantuml/)

---

## 🔗 相关文档

- [技能效果运行时](../02-CombatSystem%20战斗系统/Skill-Effect-Runtime%20技能效果运行时.md) - ECC架构的应用实例
- [技能效果开发进展](../02-CombatSystem%20战斗系统/_status%20开发进展/Skill-Effect-Progress%20技能效果开发进展.md) - v0.5.0 修复的序列化问题

---

## 💡 关键成果

### 架构优势

- ✅ 数据与逻辑完全分离
- ✅ 组件高度可复用
- ✅ 能力灵活组合
- ✅ 支持回滚系统
- ✅ 易于测试和维护

---

**返回**: [文档中心](../README.md)
