# Astrum 项目快速开始指南

> 📖 **完整文档**: 请查看 [README.md](README.md) 获取完整的项目文档和架构说明

## 项目概述

Astrum 是一个基于 Unity 的多人网络游戏项目，采用客户端-服务器架构，使用 Protocol Buffers 定义网络协议，MemoryPack 进行序列化。项目包含完整的房间系统、用户管理系统、智能日志管理系统和基于Prefab的UI代码生成工具。

## 🚀 最新优化 (2024年12月)

### ASLogger 系统升级
- **智能扫描**: 基于 Roslyn 的代码分析，支持 `ASLogger.Instance.Log(LogLevel, ...)` 格式
- **状态保持**: 刷新扫描时自动保持日志的启用/禁用状态
- **编辑器集成**: Unity 编辑器中的可视化管理工具 (`Tools > ASLogger Manager`)

### 网络架构简化
- **Session 通信**: 移除复杂的 RPC 系统，统一使用 Session 通信模式
- **消息处理**: 统一的消息分发机制和错误处理

### UI 代码生成
- **基于 Prefab**: 从 Unity Prefab 自动生成 C# UI 逻辑类
- **编辑器工具**: `Tools > UI Generator` 可视化工具

### 资源管理优化
- **解决启动卡顿**: 手动生成YooAsset资源清单，避免运行时卡顿
- **场景加载修复**: 修复了YooAsset场景加载失败问题，支持多种地址格式
- **编辑器工具**: `Astrum/资源管理/` 菜单，支持生成、清理、打开资源清单
- **平台支持**: 自动检测当前构建平台，支持多平台开发

> 📖 **完整功能**: 查看 [README.md](README.md) 了解所有核心功能

## 环境要求

- **Unity**: 2022.3 LTS 或更高版本
- **.NET**: 9.0 SDK
- **操作系统**: Windows 10/11, macOS, Linux
- **IDE**: Visual Studio 2022, JetBrains Rider, 或 Visual Studio Code

> 🏗️ **技术架构**: 查看 [ASTRUM_PROJECT_OVERVIEW.md](ASTRUM_PROJECT_OVERVIEW.md) 了解详细架构

## 快速开始

### 1. 启动项目
```bash
# 启动服务器
start_server.bat

# 或手动启动
cd AstrumServer && dotnet run
```

### 2. 打开客户端
1. 打开 Unity 项目 `AstrumProj`
2. 在Unity编辑器中运行场景
3. 客户端会自动连接服务器并显示登录界面

### 3. 管理日志系统
1. 打开ASLogger Manager：`Tools > ASLogger Manager`
2. 点击"刷新扫描"自动识别项目中的日志
3. 配置日志的启用/禁用状态
4. 支持两种日志格式：
   ```csharp
   ASLogger.Instance.Debug("消息");
   ASLogger.Instance.Log(LogLevel.Debug, "消息");
   ```

### 4. 生成资源清单（重要！）
**解决游戏启动卡顿问题**
1. 在Unity编辑器中，点击菜单 `Astrum/资源管理/生成资源清单`
2. 等待生成完成（首次生成可能需要几分钟）
3. 生成成功后，游戏启动将不再卡顿
4. 当项目资源发生变化时，需要重新生成资源清单

> 💡 **提示**: 这是解决YooAsset EditorSimulateMode启动卡顿的关键步骤！

### 5. 创建UI界面
1. 在Unity中创建UI Prefab（放在 `Assets/ArtRes/UI/` 目录）
2. 打开UI Generator工具：`Tools > UI Generator`
3. 选择Prefab并生成UI代码

### 6. 修改协议
1. 编辑 `AstrumConfig/Proto/*.proto` 文件
2. 运行协议生成工具: `cd AstrumTool/Proto2CS && dotnet run`
3. 代码自动生成到客户端和服务器端

### 7. 编辑配置表（新功能）
1. 使用角色编辑器：`Tools > Role & Skill Editor > Character Editor`（开发中）
2. 或直接编辑CSV文件并通过框架读写
3. 详见 [Luban CSV框架使用指南](AstrumConfig/Doc/Luban_CSV框架使用指南.md)

## 编译和测试

```bash
# 编译客户端
cd AstrumProj && dotnet build AstrumProj.sln

# 编译服务器
cd AstrumServer && dotnet build AstrumServer.sln

# 运行测试
cd AstrumTest/AstrumTest && dotnet test
```

## 常见问题

### Q: 编译时出现找不到类型错误
A: 检查是否已运行协议生成工具，确保 `Generated/` 目录下有最新的代码

### Q: UI界面不显示
A: 检查UI Prefab是否正确放置在 `Assets/ArtRes/UI/` 目录，并确保已生成UI代码

### Q: 服务器连接失败
A: 确保服务器已启动，检查端口8888是否被占用

### Q: 日志没有被识别
A: 检查日志格式是否正确，使用 `Tools > ASLogger Manager` 刷新扫描

### Q: 游戏启动时卡顿很久
A: 这是YooAsset EditorSimulateMode的常见问题，需要先生成资源清单：
   1. 点击菜单 `Astrum/资源管理/生成资源清单`
   2. 等待生成完成后再次启动游戏

### Q: 场景加载失败 "Failed to load scene! The location is invalid"
A: 这个问题已经修复，ResourceManager现在支持多种场景地址格式：
   1. 优先尝试完整路径：`Assets/Scenes/场景名.unity`
   2. 备用场景名：`场景名`
   3. 带扩展名：`场景名.unity`
   如果仍有问题，检查场景是否在Build Settings中

### Q: 资源清单生成失败
A: 检查以下几点：
   1. 确保项目已启用YooAsset
   2. 检查Console窗口的错误信息
   3. 确认项目资源没有损坏
   4. 确保场景文件在`Assets/Scenes/`目录下

## 注意事项

1. **协议修改**: 永远不要直接修改 `Generated/` 目录下的文件
2. **代码生成**: 修改协议后必须重新运行生成工具
3. **UI开发**: 使用UI Generator工具生成UI代码，不要手动创建
4. **日志管理**: 使用ASLogger Manager管理日志，支持状态保持
5. **资源管理**: 首次使用或资源更新后，必须生成资源清单避免启动卡顿
6. **场景加载**: ResourceManager已优化，支持多种场景地址格式，优先使用完整路径
7. **编译顺序**: 先编译协议代码，再编译客户端和服务器

---

> 📚 **更多信息**: 查看 [README.md](README.md) 获取完整的项目文档和架构说明

