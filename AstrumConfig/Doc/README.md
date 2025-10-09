# 📚 Astrum 项目文档导航

> **版本**: v2.0 - 按功能模块重组  
> **最后更新**: 2025-10-09

---

## 📂 文档结构

本文档库按**功能模块**组织，便于查找和维护。

```
Doc/
├── Game-Design/          # 游戏设计
├── Core-Architecture/    # 核心架构
├── Combat-System/        # 战斗系统
├── Editor-Tools/         # 编辑器工具
├── Scene-Management/     # 场景管理
└── Configuration/        # 配置系统
```

---

## 🎮 游戏设计

> 游戏整体设计和核心玩法

| 文档 | 描述 |
|------|------|
| [Astrum游戏主策划案](./Game-Design/Astrum游戏主策划案.md) | 游戏核心玩法、角色系统、战斗机制 |
| [ECC结构说明](./Game-Design/ECC结构说明.md) | Entity-Component-Capability 架构说明 |
| [房间系统策划案](./Game-Design/房间系统策划案.md) | 房间管理和多人游戏支持 |

---

## 🏗️ 核心架构

> 底层技术架构和设计模式

| 文档 | 描述 |
|------|------|
| [Archetype结构说明](./Core-Architecture/Archetype结构说明.md) | 原型系统和组合机制 |

**延伸阅读**：
- [ECC结构说明](./Game-Design/ECC结构说明.md) - ECC 是整个游戏的架构基础

---

## ⚔️ 战斗系统

> 动作、战斗、技能完整链条

### 核心系统文档

| 文档 | 描述 |
|------|------|
| [动作系统策划案](./Combat-System/ActionSystem/动作系统策划案.md) | 动作管理、状态机、取消系统 |
| [战斗系统策划案](./Combat-System/战斗系统策划案.md) | 攻击判定、受击判定、打击感 |
| [技能系统策划案](./Combat-System/技能系统策划案.md) | 技能逻辑、效果、数据结构 |
| [动画系统实现总结](./Combat-System/动画系统实现总结.md) | Animancer 动画系统实现 |

### 流程图和架构图

| 图表 | 描述 |
|------|------|
| [ActionSystem.puml](./Combat-System/ActionSystem/ActionSystem.puml) | 动作系统类图 |
| [ActionCancelSystem.puml](./Combat-System/ActionSystem/ActionCancelSystem.puml) | 取消系统流程图 |
| [ActionSystemFlow.puml](./Combat-System/ActionSystem/ActionSystemFlow.puml) | 动作系统完整流程 |

**系统依赖关系**：
```
动作系统 ──提供基础动作管理──> 战斗系统 ──提供战斗判定──> 技能系统
    │                            │
    └──────── 共享动画系统 ────────┘
```

---

## 🛠️ 编辑器工具

> 开发工具链和编辑器

### 动作编辑器

| 文档 | 描述 |
|------|------|
| [动作编辑器策划案](./Editor-Tools/动作编辑器/动作编辑器策划案.md) | 编辑器设计和功能规划 |
| [动作编辑器开发进展](./Editor-Tools/动作编辑器/动作编辑器开发进展.md) | 开发进度和已完成功能 |

### 技能编辑器

| 文档 | 描述 |
|------|------|
| [技能编辑器开发进展](./Editor-Tools/技能编辑器开发进展.md) | 技能编辑器开发状态 |

---

## ⚙️ 配置系统

> 配置表和数据管理框架

| 文档 | 描述 |
|------|------|
| [Luban_CSV框架使用指南](./Editor-Tools/Luban_CSV框架使用指南.md) | CSV 配置框架详细说明 |
| [CSV框架快速参考](./Editor-Tools/CSV框架快速参考.md) | 快速上手参考 |

**其他配置文档**：
- [表格配置说明](./表格配置说明.md) - 配置表总览

---

## 🗂️ 其他资源

| 文档 | 描述 |
|------|------|
| [备忘](./备忘.md) | 设计思考和临时记录 |

---

## 🔍 快速查找

### 按开发阶段查找

- **原型阶段**: [游戏主策划案](./Game-Design/Astrum游戏主策划案.md) → [ECC架构](./Game-Design/ECC结构说明.md)
- **战斗开发**: [动作系统](./Combat-System/ActionSystem/动作系统策划案.md) → [战斗系统](./Combat-System/战斗系统策划案.md) → [技能系统](./Combat-System/技能系统策划案.md)
- **编辑器开发**: [动作编辑器](./Editor-Tools/动作编辑器/动作编辑器策划案.md) → [技能编辑器](./Editor-Tools/技能编辑器开发进展.md)
- **配置管理**: [CSV框架](./Editor-Tools/Luban_CSV框架使用指南.md)

### 按职能查找

- **策划**: [游戏设计](#-游戏设计) | [战斗系统](#️-战斗系统)
- **程序**: [核心架构](#️-核心架构) | [配置系统](#️-配置系统)
- **工具**: [编辑器工具](#️-编辑器工具)

---

## 📝 文档规范

### 文档命名

- 策划案：`XXX系统策划案.md`
- 实现总结：`XXX系统实现总结.md`
- 开发进展：`XXX开发进展.md`
- 使用指南：`XXX使用指南.md`

### 引用规范

使用**相对路径**引用其他文档：
```markdown
[动作系统策划案](../Combat-System/ActionSystem/动作系统策划案.md)
```

### 更新规范

- 修改文档时，更新文档头部的"最后更新"日期
- 添加新功能时，在对应的"开发进展"文档中记录
- 架构变更时，同步更新相关的架构说明文档

---

## 🚀 开始使用

1. **新手入门**: 阅读 [游戏主策划案](./Game-Design/Astrum游戏主策划案.md)
2. **了解架构**: 阅读 [ECC结构说明](./Game-Design/ECC结构说明.md)
3. **开发战斗**: 按顺序阅读 [战斗系统](#️-战斗系统) 文档
4. **使用工具**: 参考 [编辑器工具](#️-编辑器工具) 文档

---

**维护者**: Astrum Team  
**文档版本**: v2.0  
**创建日期**: 2025-10-09

