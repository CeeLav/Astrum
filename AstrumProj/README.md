# AstrumProj

> 🎮 Unity 客户端项目 | Unity Client Project

Astrum 游戏的 Unity 客户端，使用 Unity 2022.3 LTS。

---

## 🏗️ 项目结构

```
AstrumProj/
├── Assets/
│   └── Script/
│       ├── AstrumLogic/      # 逻辑层（游戏核心逻辑）
│       ├── AstrumView/       # 表现层（Unity渲染、UI、动画）
│       ├── AstrumClient/     # 客户端层（网络、输入）
│       ├── CommonBase/       # 公共基础库
│       ├── Network/          # 网络通信
│       └── Editor/           # 编辑器工具
│           ├── UIGenerator/  # UI代码生成器
│           ├── ASLogger/     # 日志系统
│           └── RoleEditor/   # 角色编辑器
├── ProjectSettings/          # Unity项目设置
└── Packages/                 # Unity包依赖
```

---

## 🛠️ 主要技术栈

### 核心框架
- **Unity 2022.3 LTS** - 游戏引擎
- **TrueSync (FP Math)** - 确定性物理
- **BEPU Physics v1** - 碰撞检测
- **MemoryPack** - 高性能序列化

### 第三方插件
- **Odin Inspector** - 编辑器扩展
- **Animancer** - 动画系统
- **TextMeshPro** - 文本渲染

---

## 🚀 快速开始

### 打开项目
1. 使用 Unity 2022.3 LTS 打开 `AstrumProj/` 目录
2. 等待 Unity 导入资源
3. 打开场景：`Assets/Scenes/StartScene.unity`

### 连接服务器
1. 启动服务器（参见 [AstrumServer](../AstrumServer/)）
2. 在 Unity 中运行游戏
3. 进入登录界面，自动连接服务器

---

## 📚 编辑器工具

### UIGenerator
自动生成 UI 代码和 UIRefs 组件

**文档**: `Assets/Script/Editor/UIGenerator/README.md`

### 角色编辑器
编辑角色碰撞盒和属性

**位置**: Unity菜单 → Tools → Astrum → Role Editor

### 动作编辑器
编辑动作时间轴和帧事件

**位置**: Unity菜单 → Tools → Astrum → Action Editor

---

## 📖 详细文档

项目的详细文档，请查看：

- [核心架构](../Docs/05-CoreArchitecture%20核心架构/) - ECC架构、Archetype系统
- [战斗系统](../Docs/02-CombatSystem%20战斗系统/) - 技能、动作系统
- [物理系统](../Docs/03-PhysicsSystem%20物理系统/) - 碰撞检测
- [编辑器工具](../Docs/04-EditorTools%20编辑器工具/) - 编辑器使用
- [技术参考](../Docs/08-Technical%20技术参考/) - 技术实现细节

---

## 🔗 相关链接

- [项目首页](../README.md)
- [快速开始](../QUICK_START.md)
- [项目架构](../PROJECT_OVERVIEW.md)

---

**最后更新**: 2025-10-10
