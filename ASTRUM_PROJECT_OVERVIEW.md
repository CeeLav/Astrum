# Astrum 项目概述

## 项目简介

Astrum 是一个基于 Unity 的多人游戏项目，采用客户端-服务器架构，使用自定义的网络框架进行通信。项目的核心特点是基于 Session 的网络通信和帧同步游戏逻辑。

**项目版本**: Unity 6000.2.0b7  
**架构模式**: 客户端-服务器架构  
**网络框架**: 基于 Session 的 TCP 通信  
**同步方式**: 帧同步 (Frame Sync)

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

## 最近的重要变更

### 网络模块重构 (最新)

**变更原因**: 简化网络架构，移除复杂的 RPC 系统，专注于 Session 通信

**主要变更**:
1. **移除 RPC 系统**: 删除了所有 RPC 相关代码和方法
2. **简化 NetworkManager**: 
   - 移除 `RegisterHandler` 方法
   - 添加 `OnMessageReceived` 事件
   - 简化连接管理逻辑
3. **强化 Session**: Session 成为网络通信的核心
4. **统一消息处理**: 所有消息通过统一的事件接口处理

**影响的文件**:
- `NetworkManager.cs` - 大幅简化，移除 RPC 相关代码
- `GameApplication.cs` - 更新消息处理器注册方式
- `GamePlayManager.cs` - 适配新的消息处理机制

### 业务逻辑适配

**变更内容**:
1. **API 更新**: `IsConnectedProperty` -> `IsConnected()`
2. **事件订阅**: 从方法注册改为事件订阅
3. **消息分发**: 统一的消息分发机制
4. **错误处理**: 改进的错误处理和日志记录

## 开发和测试

### 命令行测试系统

项目包含完整的命令行测试系统:

**测试脚本**:
- `Test-UnityNetwork.ps1` - PowerShell 测试脚本 (推荐)
- `run_unity_tests.bat` - 批处理测试脚本
- `unity_test.bat` - 通用 Unity 测试脚本

**测试类**:
- `AstrumProj/Assets/Editor/NetworkTestRunner.cs` - Unity 编辑器测试类

**测试功能**:
- 编译检查
- 网络功能测试
- 性能测试
- 环境清理

**使用方法**:
```powershell
# 运行所有测试
.\Test-UnityNetwork.ps1 -TestType all

# 编译检查
.\Test-UnityNetwork.ps1 -TestType compile

# 网络测试
.\Test-UnityNetwork.ps1 -TestType network
```

### 开发工具

- **Proto2CS**: Protocol Buffers 到 C# 代码生成工具
- **BattleSimulator**: 战斗逻辑模拟器
- **NetworkTest**: 网络功能测试项目

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

## 常见问题和解决方案

### 1. 网络连接问题
- **问题**: 连接失败或超时
- **解决**: 检查服务器状态，确认端口开放
- **调试**: 查看 `network_test.log` 日志

### 2. 编译错误
- **问题**: 脚本编译失败
- **解决**: 运行 `NetworkTestRunner.CheckForErrors`
- **调试**: 查看 Unity Console 或 `compile_check.log`

### 3. 消息处理问题
- **问题**: 消息未正确处理
- **解决**: 检查事件订阅和消息类型匹配
- **调试**: 启用网络调试日志

## 开发指南

### 添加新的网络消息
1. 在 `Proto/` 目录定义 `.proto` 文件
2. 使用 `Proto2CS` 工具生成 C# 代码
3. 在消息处理器中添加对应的处理逻辑
4. 更新客户端和服务器的消息分发逻辑

### 添加新的管理器
1. 继承 `Singleton<T>` 基类
2. 实现 `Initialize()` 和 `Shutdown()` 方法
3. 在 `GameApplication` 中注册管理器
4. 添加相应的更新逻辑

### 网络调试
1. 启用网络日志: 设置日志级别为 Debug
2. 使用网络测试工具: 运行 `NetworkTestRunner`
3. 监控网络状态: 检查 `ConnectionStatus`
4. 分析消息流: 查看消息发送和接收日志

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

## 部署说明

### 客户端部署
1. 使用 Unity 构建系统生成可执行文件
2. 包含必要的依赖库和资源文件
3. 配置网络连接参数

### 服务器部署
1. **首先编译Unity项目**: 确保生成最新的程序集dll文件
2. 编译 `AstrumServer` 项目
3. 确保Unity程序集dll文件可访问
4. 配置数据库连接和网络端口
5. 部署到目标服务器环境

### 构建顺序
```
1. Unity项目编译 → 生成程序集dll
2. 服务器项目编译 → 引用Unity程序集
3. 部署服务器 → 包含所有依赖
```

## 性能优化

### 网络优化
- 使用 MemoryPack 进行高效序列化
- 实现消息压缩和批处理
- 优化连接池和内存管理

### 游戏逻辑优化
- ECS 架构提供高性能的实体管理
- 帧同步减少网络通信开销
- 客户端预测减少延迟感知

## 未来规划

### 短期目标
- [ ] 完善网络重连机制
- [ ] 增加更多的单元测试
- [ ] 优化消息处理性能
- [ ] 改进错误处理和日志系统

### 长期目标
- [ ] 支持更多的网络协议 (UDP, WebSocket)
- [ ] 实现分布式服务器架构
- [ ] 添加更多的游戏功能模块
- [ ] 提供完整的开发文档和示例

## 联系信息

如果在开发过程中遇到问题，可以：
1. 查看项目日志文件
2. 运行相应的测试脚本
3. 检查代码注释和文档
4. 使用调试工具进行问题定位

---

**最后更新**: 2024年12月 (网络模块重构完成)  
**文档版本**: 1.0  
**适用版本**: Unity 6000.2.0b7, .NET 8.0
