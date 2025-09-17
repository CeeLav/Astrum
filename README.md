# Astrum 项目文档

## 项目概述

Astrum 是一个基于 Unity 的多人网络游戏项目，采用客户端-服务器架构，使用自定义的网络框架进行通信。项目的核心特点是基于 Session 的网络通信和帧同步游戏逻辑。

**项目版本**: Unity 6000.2.0b7  
**架构模式**: 客户端-服务器架构  
**网络框架**: 基于 Session 的 TCP 通信  
**同步方式**: 帧同步 (Frame Sync)

## 快速导航

### 📚 核心文档
- [快速开始指南](QUICK_START_GUIDE.md) - 新手入门教程
- [项目概览](ASTRUM_PROJECT_OVERVIEW.md) - 项目整体介绍和技术架构

### 🛠️ 开发工具
- [Proto2CS 工具](AstrumTool/Proto2CS/README.md) - Protocol Buffers 代码生成工具
- [UI 生成器](AstrumProj/UIGenerator_Enhancement_Test.md) - UI 代码自动生成工具
- [部分类生成指南](AstrumProj/Partial_Class_Generation_Guide.md) - 代码生成最佳实践

### 🖥️ 服务器相关
- [服务器说明](AstrumServer/README.md) - 服务器项目介绍
- [LogicCore 集成](AstrumServer/LogicCore集成说明.md) - 逻辑核心集成说明
- [服务器依赖配置](服务器依赖配置说明.md) - 依赖配置详解

### 🎮 游戏系统
- [房间系统策划](AstrumConfig/Doc/房间系统策划案.md) - 房间系统设计文档
- [Unity LogHandler](AstrumProj/Assets/Script/AstrumClient/Examples/README_UnityLogHandler.md) - 日志系统使用指南

### 📊 架构图
- [客户端架构](AstrumClient.puml) - 客户端架构图
- [视图层架构](AstrumView.puml) - 视图层架构图
- [ECC 架构](ECC.puml) - ECS 架构图
- [游戏客户端流程](GameClientFlow.puml) - 客户端流程图
- [逻辑核心架构](LogicCore.puml) - 逻辑核心架构图

## 最新更新 (2024年12月)

### 🚀 ASLogger 系统优化

#### 主要改进
1. **扩展 RoslynLogScanner 支持**
   - 新增对 `ASLogger.Instance.Log(LogLevel, ...)` 格式的支持
   - 删除了基于正则表达式的 LogScanner，统一使用 Roslyn 扫描器
   - 提高了日志识别的准确性和性能

2. **智能状态缓存机制**
   - 刷新扫描时自动缓存现有日志的启用/禁用状态
   - 使用多级匹配策略（ID → FallbackID → 文件路径+行号+消息）
   - 确保用户手动配置的状态在刷新后不会丢失

3. **改进的日志格式支持**
   ```csharp
   // 现在支持两种格式：
   ASLogger.Instance.Debug("消息");
   ASLogger.Instance.Log(LogLevel.Debug, "消息");
   ```

#### 技术细节
- **扫描器升级**: 从正则表达式改为基于 Roslyn 的语法分析
- **状态保持**: 实现智能的状态缓存和恢复机制
- **性能优化**: 提高大型项目的扫描性能

### 🔧 网络模块重构

#### 主要变更
1. **简化网络架构**
   - 移除复杂的 RPC 系统
   - 统一使用 Session 通信模式
   - 简化 NetworkManager 接口

2. **统一消息处理**
   - 所有消息通过 `OnMessageReceived` 事件处理
   - 类型安全的消息分发机制
   - 改进的错误处理和日志记录

## 项目结构

```
Astrum/
├── README.md                      # 本文档 - 项目总览
├── ASTRUM_ARCHITECTURE.md         # 技术架构详解
├── ASTRUM_PROJECT_OVERVIEW.md     # 项目概览
├── QUICK_START_GUIDE.md          # 快速开始指南
├── AstrumProj/                   # Unity 客户端项目
│   ├── Assets/Script/
│   │   ├── AstrumClient/         # 客户端核心逻辑
│   │   ├── AstrumLogic/          # 游戏逻辑层
│   │   ├── AstrumView/           # 视图层
│   │   ├── CommonBase/           # 公共基础库
│   │   ├── Network/              # 网络通信层
│   │   └── Editor/               # 编辑器脚本
│   ├── Partial_Class_Generation_Guide.md
│   ├── TextMeshPro_To_Text_Conversion_Guide.md
│   └── UIGenerator_Enhancement_Test.md
├── AstrumServer/                 # .NET 9.0 服务器项目
│   ├── README.md
│   ├── LogicCore集成说明.md
│   ├── 使用说明.md
│   └── test_logiccore.ps1
├── AstrumTool/                   # 开发工具
│   ├── Proto2CS/                 # Protocol Buffers 转 C# 工具
│   └── README.md
├── AstrumConfig/                 # 配置和协议定义
│   ├── Proto/                    # Protocol Buffer 定义文件
│   ├── Doc/                      # 项目文档
│   │   └── 房间系统策划案.md
│   └── README.md
├── AstrumTest/                   # 测试项目
├── 服务器依赖配置说明.md
└── start_server.bat              # 服务器启动脚本
```

## 核心功能

### 1. 网络通讯系统
- **协议定义**: 使用 Protocol Buffers 定义网络消息格式
- **序列化**: MemoryPack 高性能序列化库
- **网络层**: 基于 TCP Socket 的客户端-服务器通信
- **消息路由**: 类型安全的消息分发机制

### 2. 房间系统
- **房间管理**: 创建、加入、离开、删除房间
- **玩家管理**: 用户登录、会话管理、在线状态
- **实时同步**: 房间状态变更通知
- **UI界面**: 完整的房间系统界面

### 3. 日志系统 (ASLogger)
- **多格式支持**: 支持多种日志调用格式
- **智能扫描**: 基于 Roslyn 的代码分析
- **状态管理**: 智能的启用/禁用状态保持
- **编辑器集成**: Unity 编辑器中的可视化管理工具

### 4. UI代码生成系统
- **基于Prefab**: 从Unity Prefab自动生成C# UI逻辑类
- **UIRefs组件**: 自动管理UI元素引用
- **编辑器集成**: Unity编辑器中的可视化工具

## 快速开始

### 1. 环境要求
- Unity 2022.3 LTS 或更高版本
- .NET 9.0 SDK
- Windows 10/11, macOS, Linux

### 2. 启动服务器
```bash
# 使用启动脚本
start_server.bat

# 或手动启动
cd AstrumServer
dotnet run
```

### 3. 启动客户端
1. 打开 Unity 项目 `AstrumProj`
2. 在Unity编辑器中运行场景
3. 客户端会自动连接服务器

### 4. 编译项目
```bash
# 编译客户端
cd AstrumProj
dotnet build AstrumProj.sln

# 编译服务器
cd AstrumServer
dotnet build AstrumServer.sln
```

## 开发工作流程

### 修改协议
1. 编辑 `AstrumConfig/Proto/*.proto` 文件
2. 运行协议生成工具: `cd AstrumTool/Proto2CS && dotnet run`
3. 代码自动生成到客户端和服务器端

### 创建UI界面
1. 在Unity中创建UI Prefab（放在 `Assets/ArtRes/UI/` 目录）
2. 打开UI Generator工具：`Tools > UI Generator`
3. 选择Prefab并生成UI代码

### 管理日志系统
1. 打开ASLogger Manager：`Tools > ASLogger Manager`
2. 点击"刷新扫描"自动识别项目中的日志
3. 配置日志的启用/禁用状态
4. 日志状态会在刷新后自动保持

## 技术栈

### Unity 相关
- **Unity 版本**: 6000.2.0b7
- **渲染管线**: URP (Universal Render Pipeline)
- **输入系统**: Unity Input System
- **UI 系统**: UI Toolkit + UGUI

### .NET 相关
- **.NET 版本**: .NET 9.0 (服务器)
- **序列化**: MemoryPack
- **网络**: 自定义 TCP 实现
- **测试**: NUnit

### 第三方库
- **MemoryPack**: 高性能序列化
- **MongoDB.Bson**: BSON 序列化
- **Newtonsoft.Json**: JSON 处理

## 常见问题

### Q: 编译时出现找不到类型错误
A: 检查是否已运行协议生成工具，确保 `Generated/` 目录下有最新的代码

### Q: UI界面不显示
A: 检查UI Prefab是否正确放置在 `Assets/ArtRes/UI/` 目录，并确保已生成UI代码

### Q: 服务器连接失败
A: 确保服务器已启动，检查端口8888是否被占用

### Q: 日志没有被识别
A: 检查日志格式是否正确，使用 `Tools > ASLogger Manager` 刷新扫描

## 贡献指南

1. 查看相关文档了解项目结构
2. 运行测试确保功能正常
3. 遵循项目的代码规范和架构模式
4. 更新相关文档

## 联系方式

如有问题或建议，请通过项目仓库提交 Issue 或 Pull Request。

---

**最后更新**: 2024年12月 (ASLogger系统优化完成)  
**文档版本**: 2.0  
**适用版本**: Unity 6000.2.0b7, .NET 9.0
