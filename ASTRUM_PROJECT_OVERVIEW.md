# Astrum 项目概述

> 📖 **完整文档**: 请查看 [README.md](README.md) 获取最新的项目文档和快速开始指南

## 项目简介

Astrum 是一个基于 Unity 的多人游戏项目，采用客户端-服务器架构，使用自定义的网络框架进行通信。项目的核心特点是基于 Session 的网络通信、智能日志管理系统和帧同步游戏逻辑。

**项目版本**: Unity 6000.2.0b7  
**架构模式**: 客户端-服务器架构  
**网络框架**: 基于 Session 的 TCP 通信  
**同步方式**: 帧同步 (Frame Sync)  
**日志系统**: ASLogger 智能日志管理

## 项目结构

```
Astrum/
├── AstrumClient.puml           # 客户端架构图
├── AstrumView.puml            # 视图层架构图
├── ECC.puml                   # ECC 架构图
├── AstrumConfig/              # 配置文件
│   └── Proto/                 # Protocol Buffers 定义
├── AstrumLogic/               # 逻辑核心 (.NET 项目)
│   ├── AstrumLogic.sln       # 解决方案文件
│   ├── BattleSimulator/      # 战斗模拟器
│   ├── CommonBase/           # 公共基础库
│   ├── LogicCore.Tests/      # 单元测试
│   └── NetworkTest/          # 网络测试
├── AstrumProj/               # Unity 主项目
│   ├── Assets/
│   │   ├── Script/
│   │   │   ├── AstrumClient/ # 客户端脚本
│   │   │   ├── AstrumLogic/  # 逻辑层脚本
│   │   │   ├── AstrumView/   # 视图层脚本
│   │   │   ├── CommonBase/   # 公共基础脚本
│   │   │   ├── Network/      # 网络层脚本
│   │   │   └── External/     # 外部依赖
│   │   ├── Scenes/           # 游戏场景
│   │   └── Settings/         # 项目设置
├── AstrumServer/             # 服务器项目 (.NET)
└── AstrumTool/               # 开发工具
    └── Proto2CS/             # Protocol Buffers 转 C# 工具
```

## 核心架构

### 1. 网络层架构 (最近重构)

**重要变更**: 网络模块已从复杂的 RPC 系统重构为简单的基于 Session 的通信模式。

#### 核心组件:
- **NetworkManager**: 网络管理器，负责连接管理和消息分发
- **Session**: 网络会话，负责具体的消息发送和接收
- **TService**: TCP 服务，底层网络通信实现
- **MessageObject**: 消息基类，所有网络消息的基础

#### 网络通信流程:
```
Client -> NetworkManager -> Session -> TService -> TCP -> Server
```

#### 关键文件:
- `AstrumProj/Assets/Script/AstrumClient/Managers/NetworkManager.cs` - 网络管理器
- `AstrumProj/Assets/Script/Network/Session.cs` - 网络会话
- `AstrumProj/Assets/Script/Network/TService.cs` - TCP 服务
- `AstrumProj/Assets/Script/Network/Generated/` - 自动生成的网络消息

### 2. 客户端架构

#### 管理器系统:
- **GameApplication**: 应用程序入口，管理器的生命周期
- **NetworkManager**: 网络通信管理
- **GamePlayManager**: 游戏玩法管理
- **ResourceManager**: 资源管理
- **SceneManager**: 场景管理
- **UIManager**: UI 管理
- **AudioManager**: 音频管理
- **InputManager**: 输入管理

#### 核心模式:
- **Singleton**: 所有管理器都是单例模式
- **事件驱动**: 使用事件系统进行模块间通信
- **分层架构**: Client -> Logic -> View 三层分离

### 3. 游戏逻辑架构

#### 核心概念:
- **Room**: 游戏房间，包含游戏状态和玩家信息
- **Stage**: 游戏舞台，管理游戏对象和场景
- **World**: 游戏世界，ECS 系统的容器
- **Entity**: 游戏实体，ECS 架构中的实体

#### 同步机制:
- **帧同步**: 使用 FrameSync 进行游戏状态同步
- **输入预测**: 客户端输入预测和服务器验证
- **状态回滚**: 支持状态回滚和重放

### 4. 消息系统

#### 消息类型:
- **NetworkMessage**: 基础网络消息
- **GameNetworkMessage**: 游戏专用消息
- **ConnectionStatus**: 连接状态消息

#### 消息处理:
- **事件驱动**: 通过 `OnMessageReceived` 事件处理消息
- **类型分发**: 根据消息类型自动分发到对应处理器
- **异步处理**: 支持异步消息发送和处理

## 最新重要更新 (2024年12月)

### 🚀 ASLogger 系统优化

**主要改进**:
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

### 🔧 网络模块重构

**主要变更**:
1. **简化网络架构**
   - 移除复杂的 RPC 系统
   - 统一使用 Session 通信模式
   - 简化 NetworkManager 接口

2. **统一消息处理**
   - 所有消息通过 `OnMessageReceived` 事件处理
   - 类型安全的消息分发机制
   - 改进的错误处理和日志记录

## 开发工具和测试

### 🛠️ 开发工具
- **Proto2CS**: Protocol Buffers 到 C# 代码生成工具
- **UI Generator**: 基于 Prefab 的 UI 代码生成工具
- **ASLogger Manager**: 智能日志管理系统 (`Tools > ASLogger Manager`)

### 🧪 测试系统
- **Unity Editor 测试**: `AstrumProj/Assets/Editor/NetworkTestRunner.cs`
- **编译检查**: 自动验证项目编译状态
- **网络功能测试**: 验证网络连接和消息处理
- **性能测试**: 测试系统性能和资源使用

### 📝 日志管理
1. 打开ASLogger Manager：`Tools > ASLogger Manager`
2. 点击"刷新扫描"自动识别项目中的日志
3. 配置日志的启用/禁用状态
4. 支持两种日志格式：
   ```csharp
   ASLogger.Instance.Debug("消息");
   ASLogger.Instance.Log(LogLevel.Debug, "消息");
   ```

## 技术栈

### Unity 相关
- **Unity 版本**: 6000.2.0b7
- **渲染管线**: URP (Universal Render Pipeline)
- **输入系统**: Unity Input System
- **UI 系统**: UI Toolkit + UGUI

### .NET 相关
- **.NET 版本**: .NET 8.0
- **序列化**: MemoryPack
- **网络**: 自定义 TCP 实现
- **测试**: NUnit

### 第三方库
- **MemoryPack**: 高性能序列化
- **MongoDB.Bson**: BSON 序列化
- **Newtonsoft.Json**: JSON 处理

## 配置和设置

### 网络配置
- **默认端口**: 8888
- **协议**: TCP
- **序列化**: MemoryPack
- **消息格式**: 自定义二进制协议

### 项目设置
- **脚本后端**: IL2CPP
- **API 兼容性**: .NET Standard 2.1
- **目标平台**: Windows Standalone

## 常见问题

### Q: 编译时出现找不到类型错误
A: 检查是否已运行协议生成工具，确保 `Generated/` 目录下有最新的代码

### Q: UI界面不显示
A: 检查UI Prefab是否正确放置在 `Assets/ArtRes/UI/` 目录，并确保已生成UI代码

### Q: 服务器连接失败
A: 确保服务器已启动，检查端口8888是否被占用

### Q: 日志没有被识别
A: 检查日志格式是否正确，使用 `Tools > ASLogger Manager` 刷新扫描

## 开发指南

### 添加新的网络消息
1. 在 `AstrumConfig/Proto/` 目录定义 `.proto` 文件
2. 使用 `Proto2CS` 工具生成 C# 代码
3. 在消息处理器中添加对应的处理逻辑

### 管理日志系统
1. 使用ASLogger Manager：`Tools > ASLogger Manager`
2. 刷新扫描自动识别项目中的日志
3. 配置日志的启用/禁用状态

### 创建UI界面
1. 在Unity中创建UI Prefab（放在 `Assets/ArtRes/UI/` 目录）
2. 打开UI Generator工具：`Tools > UI Generator`
3. 选择Prefab并生成UI代码

## 依赖结构 (最新更新)

### 服务器依赖客户端程序集

**重要变更**: 服务器项目现在直接依赖Unity客户端工程的三个核心程序集：

```
AstrumServer (.NET 9.0)
├── AstrumLogic.dll (Unity程序集)
├── CommonBase.dll (Unity程序集)  
├── Network.dll (Unity程序集)
├── MemoryPack (NuGet包)
├── Newtonsoft.Json (NuGet包)
└── System.Numerics.Vectors (NuGet包)
```

**依赖路径**:
- `AstrumLogic.dll`: `AstrumProj\Library\ScriptAssemblies\AstrumLogic.dll`
- `CommonBase.dll`: `AstrumProj\Library\ScriptAssemblies\CommonBase.dll`
- `Network.dll`: `AstrumProj\Library\ScriptAssemblies\Network.dll`

**优势**:
1. **代码复用**: 服务器和客户端共享核心逻辑代码
2. **类型一致**: 网络消息和数据结构保持一致
3. **简化维护**: 减少重复代码，统一维护

**注意事项**:
- 需要先编译Unity项目生成程序集dll文件
- 服务器项目依赖Unity程序集的编译输出
- MemoryPack序列化在跨平台时可能需要特殊处理

## 构建和部署

### 构建顺序
```
1. Unity项目编译 → 生成程序集dll
2. 服务器项目编译 → 引用Unity程序集
3. 部署服务器 → 包含所有依赖
```

### 快速启动
```bash
# 启动服务器
start_server.bat

# 或手动启动
cd AstrumServer && dotnet run
```

---

> 📚 **完整文档**: 查看 [README.md](README.md) 获取完整的项目文档、快速开始指南和技术架构说明

**最后更新**: 2024年12月 (ASLogger系统优化完成)  
**文档版本**: 2.0  
**适用版本**: Unity 6000.2.0b7, .NET 9.0
