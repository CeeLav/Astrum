# Astrum

> 🎮 俯视角动作游戏 | Top-Down Action Game

一个基于 Unity + .NET 的多人在线动作游戏项目，采用 ECC 架构和确定性回滚网络同步。

---

## 🚀 快速开始

- **新手入门**: [QUICK_START.md](QUICK_START.md)
- **项目架构**: [PROJECT_OVERVIEW.md](PROJECT_OVERVIEW.md)
- **完整文档**: [Docs/](Docs/)

---

## 📚 核心文档

### 🎮 游戏设计
- [游戏主策划案](Docs/01-GameDesign%20游戏设计/Game-Design%20游戏主策划案.md) - 游戏类型、玩法、操作
- [房间系统](Docs/01-GameDesign%20游戏设计/Room-System%20房间系统.md) - 多人房间管理

### ⚔️ 战斗系统
- [技能系统](Docs/02-CombatSystem%20战斗系统/Skill-System%20技能系统.md) - 技能设计架构
- [技能效果运行时](Docs/02-CombatSystem%20战斗系统/Skill-Effect-Runtime%20技能效果运行时.md) - 效果执行系统
- [动作系统](Docs/02-CombatSystem%20战斗系统/Action-System%20动作系统.md) - 动作帧和取消系统

### 🎯 物理系统
- [物理碰撞检测](Docs/03-PhysicsSystem%20物理系统/Physics-Design%20物理碰撞检测设计.md) - BEPU Physics 集成
- [开发进展](Docs/03-PhysicsSystem%20物理系统/Physics-Progress%20物理系统开发进展.md) - v0.3.0

### 🛠️ 编辑器工具
- [CSV框架](Docs/04-EditorTools%20编辑器工具/CSV-Framework%20CSV框架.md) - 配置表快速参考
- [Luban使用指南](Docs/04-EditorTools%20编辑器工具/Luban-Guide%20Luban使用指南.md) - 配置生成工具

### 🏗️ 核心架构
- [ECC结构说明](Docs/05-CoreArchitecture%20核心架构/ECC-System%20ECC结构说明.md) - Entity-Component-Capability
- [序列化最佳实践](Docs/05-CoreArchitecture%20核心架构/Serialization-Best-Practices%20序列化最佳实践.md) - 回滚系统设计

---

## 🛠️ 开发状态

| 系统 | 状态 | 完成度 | 文档 |
|------|------|--------|------|
| **技能效果运行时** | ✅ 完成 | 90% | [查看](Docs/02-CombatSystem%20战斗系统/_status%20开发进展/Skill-Effect-Progress%20技能效果开发进展.md) |
| **物理系统** | ✅ 完成 | 80% | [查看](Docs/03-PhysicsSystem%20物理系统/Physics-Progress%20物理系统开发进展.md) |
| **房间系统** | ✅ 完成 | 100% | [查看](Docs/01-GameDesign%20游戏设计/Room-System%20房间系统.md) |
| **动作系统** | ✅ 完成 | 80% | - |
| **测试工程（单机）** | ⏳ 计划中 | 0% | - |
| **技能动作编辑器** | 📝 策划完成 | 0% | - |

---

## 📁 项目结构

```
Astrum/
├── Docs/                   # 📚 文档中心
├── AstrumProj/             # 🎮 Unity 客户端
├── AstrumServer/           # 🖥️ .NET 服务器
├── AstrumTest/             # 🧪 单元测试
├── AstrumConfig/           # ⚙️ 配置数据
│   ├── Tables/             # 配置表
│   └── Proto/              # 协议定义
└── AstrumTool/             # 🔧 工具集
```

---

## 🎯 技术栈

### 客户端
- **Unity 2022.3 LTS** - 游戏引擎
- **TrueSync (FP Math)** - 确定性物理
- **BEPU Physics v1** - 碰撞检测
- **MemoryPack** - 高性能序列化

### 服务器
- **.NET 9.0** - 服务器框架
- **Protocol Buffers** - 网络协议
- **MemoryPack** - 序列化

### 工具链
- **Luban** - 配置表生成
- **UIGenerator** - UI代码生成
- **Odin Inspector** - Unity编辑器扩展

---

## 🎮 核心特性

- ✅ **ECC 架构** - 数据与逻辑分离，高度模块化
- ✅ **回滚网络同步** - 确定性物理 + 状态回滚
- ✅ **技能系统** - Skill → Action → Effect 三层架构
- ✅ **物理碰撞检测** - BEPU Physics 集成
- ✅ **配置驱动** - Luban 配置表生成
- ✅ **编辑器工具链** - 角色编辑器、动作编辑器

---

## 📖 文档导航

完整文档请访问 **[Docs/](Docs/)** 目录

---

## 📄 许可证

MIT License

---

**最后更新**: 2025-10-10  
**项目状态**: 🚧 开发中
