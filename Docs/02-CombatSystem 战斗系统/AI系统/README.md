# 🤖 AI 系统文档

## 📖 文档列表

### [AI 状态机设计](AI-StateMachine-Design%20AI状态机设计.md)
AI系统的核心架构与实现

**核心内容**:
- AICapability - AI决策层
- AIStateMachineComponent - 状态机数据组件
- 状态机实现方案

---

## 🎯 系统概述

AI系统采用状态机架构，通过组件和能力分离实现决策与状态管理：

- **AICapability**: 控制AI决策层，负责状态转换逻辑和执行行为
- **AIStateMachineComponent**: 存放AI状态机数据，记录当前状态和历史

---

## 📁 目录结构

```
Docs/AI 系统/
├── README.md                           # 本文件
└── AI-StateMachine-Design AI状态机设计.md  # 详细设计文档
```

---

## 🔗 相关文档

- [核心架构](../../05-CoreArchitecture%20核心架构/ECC/) - ECC架构说明
- [战斗系统总览](../Combat-System%20战斗系统总览.md) - AI可能需要的战斗相关组件

---

**返回**: [战斗系统文档](../README.md) | [文档中心](../../README.md)

