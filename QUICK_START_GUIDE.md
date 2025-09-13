# Astrum 项目快速开始指南

## 项目概述

Astrum 是一个基于 Unity 的多人网络游戏项目，采用客户端-服务器架构，使用 Protocol Buffers 定义网络协议，MemoryPack 进行序列化。项目包含完整的房间系统、用户管理系统和基于Prefab的UI代码生成工具。

## 项目结构

```
Astrum/
├── AstrumProj/                 # Unity 客户端项目
│   ├── Assets/
│   │   ├── Script/
│   │   │   ├── AstrumClient/   # 客户端核心逻辑
│   │   │   │   ├── Core/       # 核心应用逻辑
│   │   │   │   ├── Managers/   # 管理器类（网络、用户、房间、UI等）
│   │   │   │   └── UI/         # UI界面逻辑
│   │   │   │       └── Generated/ # 自动生成的UI代码
│   │   │   ├── AstrumLogic/    # 游戏逻辑层
│   │   │   ├── AstrumView/     # 视图层
│   │   │   ├── CommonBase/     # 公共基础库
│   │   │   ├── Network/        # 网络通信层
│   │   │   ├── Generated/      # 自动生成的协议代码
│   │   │   └── Editor/         # 编辑器脚本
│   │   │       └── UIGenerator/ # UI代码生成工具
│   │   ├── ArtRes/             # 美术资源
│   │   │   └── UI/             # UI Prefab资源
│   │   └── Resources/          # 运行时资源
│   │       └── UI/             # UI Prefab资源（运行时）
│   ├── AstrumProj.sln         # Unity项目解决方案
│   └── README.md              # 项目说明
├── AstrumServer/               # .NET 9.0 服务器项目
│   ├── AstrumServer/
│   │   ├── Core/              # 服务器核心逻辑
│   │   ├── Managers/          # 管理器类（用户、房间）
│   │   ├── Network/           # 网络模块
│   │   ├── Generated/         # 自动生成的协议代码
│   │   └── Program.cs         # 服务器入口
│   ├── AstrumServer.sln       # 服务器解决方案
│   └── README.md              # 服务器说明
├── AstrumTool/                 # 开发工具
│   └── Proto2CS/              # Proto 到 C# 代码生成器
│       └── README.md          # 工具使用说明
├── AstrumConfig/               # 配置和协议定义
│   ├── Proto/                  # Protocol Buffer 定义文件
│   ├── UI/                     # UI配置文件（已废弃，改用Prefab）
│   └── Doc/                    # 项目文档
│       └── 房间系统策划案.md   # 房间系统设计文档
├── AstrumTest/                 # 测试项目
│   └── AstrumTest/             # 单元测试和集成测试
├── start_server.bat           # 服务器启动脚本
└── QUICK_START_GUIDE.md       # 本文件
```

## 核心功能

### 1. 网络通讯系统
- **协议定义**: 使用 Protocol Buffers 定义网络消息格式
- **序列化**: MemoryPack 高性能序列化库
- **网络层**: 基于 TCP Socket 的客户端-服务器通信
- **消息路由**: 类型安全的消息分发机制
- **连接管理**: 自动连接、重连、心跳检测

### 2. 房间系统（已实现）
- **房间管理**: 创建、加入、离开、删除房间
- **玩家管理**: 用户登录、会话管理、在线状态
- **实时同步**: 房间状态变更通知
- **UI界面**: 登录界面、房间列表界面、房间详情界面
- **自动刷新**: 房间列表每5秒自动更新

### 3. 用户系统
- **身份管理**: 用户登录、登出、状态维护
- **会话管理**: 网络连接与用户身份绑定
- **权限控制**: 基于登录状态的访问控制
- **自动登录**: 连接成功后自动发送登录请求

### 4. UI代码生成系统
- **基于Prefab**: 从Unity Prefab自动生成C# UI逻辑类
- **UIRefs组件**: 自动管理UI元素引用
- **编辑器集成**: Unity编辑器中的可视化工具
- **代码分离**: 生成代码和手动编辑代码分离

## 技术架构

### 客户端架构
```
GameApplication (主应用)
├── NetworkManager (网络管理)
├── UserManager (用户管理)
├── RoomSystemManager (房间系统)
├── UIManager (UI管理)
└── GamePlayManager (游戏逻辑)
```

### 服务器架构
```
GameServer (主服务)
├── ServerNetworkManager (网络管理)
├── UserManager (用户管理)
└── RoomManager (房间管理)
```

### UI系统架构
```
UIManager (UI管理器)
├── UIRefs (UI引用组件)
├── LoginView (登录界面)
├── RoomListView (房间列表界面)
└── RoomDetailView (房间详情界面)
```

### 消息流程
1. **客户端**: 创建具体消息对象 → MemoryPack序列化 → TCP发送
2. **网络传输**: TCP 连接传输字节数据
3. **服务器**: 接收字节数据 → MemoryPack反序列化 → 类型路由 → 具体处理器
4. **响应**: 服务器处理 → 生成响应消息 → 序列化 → 发送回客户端

## 协议定义

### 协议文件结构
```
AstrumConfig/Proto/
├── networkcommon_C_1000.proto    # 通用网络消息（客户端）
├── gamemessages_C_2000.proto     # 游戏消息（客户端）
├── game_S_3000.proto             # 游戏服务端协议
└── connectionstatus_C_4000.proto # 连接状态协议（客户端）
```

### 协议命名规范
- `name_C_1.proto`: 仅客户端使用
- `name_S_1.proto`: 仅服务端使用  
- `name_CS_1.proto`: 客户端和服务端都使用
- 数字为起始opcode编号

### 主要消息类型
- **用户相关**: `LoginRequest/Response`, `UserInfo`
- **房间相关**: `CreateRoomRequest/Response`, `JoinRoomRequest/Response`, `LeaveRoomRequest/Response`, `GetRoomListRequest/Response`, `RoomInfo`
- **游戏相关**: `GameRequest/Response`, `PlayerInfo`
- **网络相关**: `HeartbeatMessage/Response`, `RoomUpdateNotification`

## 开发工作流程

### 1. 修改协议
1. 编辑 `AstrumConfig/Proto/*.proto` 文件
2. 运行协议生成工具: `cd AstrumTool/Proto2CS && dotnet run`
3. 代码自动生成到客户端和服务器端

### 2. 创建UI界面
1. 在Unity中创建UI Prefab（放在 `Assets/ArtRes/UI/` 目录）
2. 打开UI Generator工具：`Tools > UI Generator`
3. 选择Prefab并生成UI代码
4. 在生成的代码中添加业务逻辑

### 3. 添加新功能
1. 在 `.proto` 文件中定义新消息类型
2. 重新生成代码
3. 在客户端和服务器端实现对应的处理逻辑
4. 编写测试用例验证功能

### 4. 测试验证
1. 编译客户端: `cd AstrumProj && dotnet build AstrumProj.sln`
2. 编译服务器: `cd AstrumServer && dotnet build AstrumServer.sln`
3. 运行测试: `cd AstrumTest/AstrumTest && dotnet test`

## 关键修复记录

### 1. 协议数据统一
- **问题**: 客户端和服务器端使用不同的消息结构
- **解决**: 修改 `.proto` 文件，重新生成代码，确保两端一致性

### 2. UI生成系统优化
- **问题**: 基于JSON的UI生成系统不够灵活
- **解决**: 改为基于Prefab的UI代码生成，支持Unity编辑器集成

### 3. 房间系统完善
- **问题**: 房间详情显示问题，UI切换异常
- **解决**: 修复房间信息显示，完善UI切换逻辑，取消人数限制

### 4. 网络连接优化
- **问题**: 连接和登录流程复杂
- **解决**: 实现自动连接和自动登录，简化用户操作

### 5. 代码生成工具修复
- **问题**: UIRefs组件生成时类名不正确
- **解决**: 修复UIRefsGenerator，确保生成的类名包含"View"后缀

## 运行说明

### 快速启动（推荐）
使用提供的启动脚本：
```bash
# Windows
start_server.bat

# 或者手动启动
cd AstrumServer
dotnet run
```

### 启动服务器
```bash
cd AstrumServer
dotnet build AstrumServer.sln
dotnet run --project AstrumServer/AstrumServer.csproj
```

### 启动客户端
1. 打开 Unity 项目 `AstrumProj`
2. 在Unity编辑器中运行场景
3. 客户端会自动连接服务器并显示登录界面
4. 登录成功后自动跳转到房间列表界面

### 运行测试
```bash
cd AstrumTest/AstrumTest
dotnet test
```

### 编译项目
```bash
# 编译客户端
cd AstrumProj
dotnet build AstrumProj.sln

# 编译服务器
cd AstrumServer
dotnet build AstrumServer.sln
```

## 注意事项

1. **协议修改**: 永远不要直接修改 `Generated/` 目录下的文件
2. **代码生成**: 修改协议后必须重新运行生成工具
3. **UI开发**: 使用UI Generator工具生成UI代码，不要手动创建
4. **消息类型**: 使用具体的消息类型，避免使用通用的 NetworkMessage 包装
5. **测试覆盖**: 新功能必须包含相应的测试用例
6. **资源路径**: UI Prefab放在 `Assets/ArtRes/UI/` 目录，运行时资源放在 `Assets/Resources/UI/`
7. **编译顺序**: 先编译协议代码，再编译客户端和服务器

## 开发环境要求

- **Unity**: 2022.3 LTS 或更高版本
- **.NET**: 9.0 SDK
- **操作系统**: Windows 10/11, macOS, Linux
- **IDE**: Visual Studio 2022, JetBrains Rider, 或 Visual Studio Code

## 常见问题

### Q: 编译时出现找不到类型错误
A: 检查是否已运行协议生成工具，确保 `Generated/` 目录下有最新的代码

### Q: UI界面不显示
A: 检查UI Prefab是否正确放置在 `Assets/ArtRes/UI/` 目录，并确保已生成UI代码

### Q: 服务器连接失败
A: 确保服务器已启动，检查端口8888是否被占用

### Q: 房间列表为空
A: 检查网络连接，确保客户端已成功登录

## 联系方式

如有问题或建议，请通过项目仓库提交 Issue 或 Pull Request。

