# 项目上下文

## Purpose
Astrum 是一个基于 Unity + .NET 的多人在线动作游戏项目，采用 ECC（Entity-Component-Capability）架构和确定性回滚网络同步。项目核心目标是实现高性能、可扩展的多人游戏系统，支持帧同步、状态回滚和确定性物理模拟。

## 技术栈

### 客户端
- **Unity 2022.3 LTS** - 游戏引擎
- **TrueSync (FP Math)** - 确定性物理数学库
- **BEPU Physics v1** - 碰撞检测引擎
- **MemoryPack** - 高性能序列化库
- **UI Toolkit + UGUI** - UI 系统
- **Unity Input System** - 输入系统
- **URP (Universal Render Pipeline)** - 渲染管线
- **Odin Inspector** - Unity 编辑器扩展
- **Animancer** - 动画系统

### 服务器
- **.NET 9.0** - 服务器框架
- **Protocol Buffers** - 网络协议定义
- **MemoryPack** - 序列化库
- **NUnit** - 单元测试框架

### 工具链
- **Luban** - 配置表生成工具
- **UIGenerator** - UI 代码生成工具（基于 Prefab）
- **Proto2CS** - Protocol Buffers 到 C# 代码生成
- **ASLogger** - 智能日志管理系统

## 项目约定

### 代码风格

**命名规范**:
- **类名、接口名**: PascalCase（大驼峰）
- **方法名、属性名**: PascalCase
- **字段名、变量名**: camelCase（小驼峰）
- **私有字段**: camelCase，使用 `private` 修饰符
- **常量**: PascalCase 或 UPPER_CASE
- **UI Prefab**: PascalCase（如 `Login.prefab`）
- **UI 类**: UI名称 + "View"后缀（如 `LoginView.cs`）

**代码格式**:
- 大括号与语句同一行：`if (condition) { }`
- 使用 Region 组织代码（Fields, Properties, Methods, Events 等）
- 文件作用域命名空间（C# 10+）：`namespace Astrum.Client.UI.Generated;`
- using 语句顺序：System → Unity → 第三方库 → 项目命名空间

**访问修饰符**:
- 字段默认 `private`，需要序列化时使用 `[SerializeField]`
- 生命周期回调：`protected virtual`
- 事件处理：`private`
- 业务逻辑方法：根据需求选择 `private` 或 `public`

**代码组织**:
- 使用 Region 分区：`#region Fields`, `#region Methods` 等
- 相关文件放在一起（如 `LoginView.cs` 和 `LoginView.designer.cs`）
- 避免过深的目录嵌套

### 架构模式

**核心架构**:
- **ECC 架构** (Entity-Component-Capability) - 数据与逻辑分离，高度模块化
- **客户端-服务器架构** - 分离的客户端和服务器项目
- **分层架构** - Client → Logic → View 三层分离

**网络同步**:
- **帧同步 (Frame Sync)** - 基于帧的游戏状态同步
- **确定性物理** - 使用 TrueSync FP Math 确保客户端和服务器计算结果一致
- **状态回滚** - 支持状态回滚和重放，用于网络延迟补偿
- **输入预测** - 客户端输入预测和服务器验证

**设计模式**:
- **Singleton** - 所有管理器都是单例模式（GameApplication, NetworkManager, UIManager 等）
- **事件驱动** - 使用事件系统进行模块间通信
- **工厂模式** - 用于实体和组件的创建
- **观察者模式** - 用于事件订阅和通知

**网络通信**:
- **基于 Session 的通信** - 简化的网络通信模式，统一使用 Session 发送和接收消息
- **Protocol Buffers** - 网络协议定义和代码生成
- **MemoryPack** - 高性能二进制序列化

### 测试策略

**测试分类**:
- **Unit（单元测试）**: 纯函数，无外部依赖，速度极快（<10ms）
- **Component（组件测试）**: 测试单个模块或系统，可以有少量依赖，速度较快（10-100ms）
- **Integration（集成测试）**: 测试完整的游戏流程，多个系统协同工作，速度较慢（100ms+）

**测试框架**:
- **NUnit** - 主要测试框架
- **测试项目**: `AstrumTest/` 目录，包含 Shared、Client、Server、E2E 四个子项目

**测试要求**:
- 新功能开发时优先编写单元测试（TDD）
- 提交前运行所有相关测试
- 确保确定性测试（物理、数值计算等）

### Git 工作流

**分支策略**:
- 功能分支命名：`task/[TASK_IDENTIFIER]_[TASK_DATE_AND_NUMBER]`
- 主分支：`main` 或 `master`

**提交约定**:
- 提交消息应清晰描述变更内容
- 提交前确保代码编译通过
- 提交前运行相关测试

**代码审查**:
- 重要变更需要代码审查
- 确保符合代码风格规范
- 检查是否包含必要的文档更新

## 领域上下文

**游戏类型**: 俯视角动作游戏（Top-Down Action Game）

**核心系统**:
- **技能系统** - Skill → Action → Effect 三层架构
- **动作系统** - 动作帧和取消系统
- **物理系统** - BEPU Physics 集成，确定性碰撞检测
- **房间系统** - 多人房间管理
- **快速匹配系统** - 快速联机匹配

**关键概念**:
- **Room**: 游戏房间，包含游戏状态和玩家信息
- **Stage**: 游戏舞台，管理游戏对象和场景
- **World**: 游戏世界，ECS 系统的容器
- **Entity**: 游戏实体，ECC 架构中的实体
- **Component**: 组件，存储实体数据
- **Capability**: 能力，包含实体逻辑

**网络通信**:
- 默认端口：8888
- 协议：TCP
- 序列化：MemoryPack（二进制）
- 消息格式：自定义二进制协议

## 重要约束

**技术约束**:
- Unity 版本必须为 2022.3 LTS 或兼容版本
- 服务器必须使用 .NET 9.0
- 客户端和服务器必须使用相同的协议定义（`.proto` 文件）
- 确定性计算必须使用 TrueSync FP Math，不能使用 Unity 的浮点数学
- 序列化必须使用 MemoryPack，确保性能和兼容性

**代码约束**:
- **禁止直接修改 Generated 目录下的文件** - 这些文件由代码生成工具自动生成
- **协议修改流程** - 修改 `.proto` 文件后必须运行 Proto2CS 工具重新生成代码
- **UI 代码生成** - 使用 UIGenerator 工具生成 UI 代码，不要手动创建
- **日志管理** - 使用 ASLogger Manager 管理日志，支持状态保持

**开发约束**:
- 测试时不要破坏生产环境代码
- 使用隔离的测试环境和 Mock/Stub
- 编译前先使用 `Assets/Refresh` 刷新 Unity
- 编译 sln 以处理报错时，使用 `dotnet build AstrumProj.sln`
- Unity 项目的报错优先通过 MCP 获取

**文档约束**:
- 编写技术文档时注意先阅读 `Docs/README.md` 确认文档规范
- 没有用户要求的情况下不要编写 README
- 开发的功能如有开发进展文档，需要及时更新

## 外部依赖

**代码仓库**:
- **AstrumArtRes** - 美术资源独立仓库，需要拉取到 `AstrumProj/Assets/ArtRes` 目录
  - 使用 Git Submodule 管理（推荐）
  - 或直接 git clone 到指定目录

**第三方服务**:
- 无外部 API 依赖（纯本地/局域网游戏）

**开发工具依赖**:
- **Visual Studio 2022** 或 **JetBrains Rider** 或 **Visual Studio Code** - IDE
- **Unity Hub** - Unity 项目管理
- **Node.js** - 工具链支持（如 openspec-chinese）
