# Astrum 项目快速开始指南

## 项目概述

Astrum 是一个基于 Unity 的多人网络游戏项目，采用客户端-服务器架构，使用 Protocol Buffers 定义网络协议，MemoryPack 进行序列化。

## 项目结构

```
Astrum/
├── AstrumProj/                 # Unity 客户端项目
│   ├── Assets/Script/
│   │   ├── AstrumClient/       # 客户端核心逻辑
│   │   │   ├── Managers/       # 管理器类
│   │   │   └── UI/            # 用户界面
│   │   ├── Generated/          # 自动生成的协议代码
│   │   └── Editor/             # 编辑器脚本
├── AstrumServer/               # .NET 服务器项目
│   ├── AstrumServer/
│   │   ├── Core/              # 服务器核心逻辑
│   │   ├── Managers/          # 管理器类
│   │   ├── Network/           # 网络模块
│   │   └── Generated/         # 自动生成的协议代码
├── AstrumTool/                 # 协议生成工具
│   └── Proto2CS/              # Proto 到 C# 代码生成器
├── AstrumConfig/               # 配置文件
│   └── Proto/                  # Protocol Buffer 定义文件
└── AstrumTest/                 # 测试项目
    └── AstrumTest/             # 单元测试和集成测试
```

## 核心功能

### 1. 网络通讯系统
- **协议定义**: 使用 Protocol Buffers 定义网络消息格式
- **序列化**: MemoryPack 高性能序列化库
- **网络层**: 基于 Unity Network 程序集的 TCP 服务
- **消息路由**: 类型安全的消息分发机制

### 2. 房间系统
- **房间管理**: 创建、加入、离开、删除房间
- **玩家管理**: 用户登录、会话管理、在线状态
- **实时同步**: 房间状态变更通知

### 3. 用户系统
- **身份管理**: 用户登录、登出、状态维护
- **会话管理**: 网络连接与用户身份绑定
- **权限控制**: 基于登录状态的访问控制

## 技术架构

### 客户端架构
```
GameApplication (主应用)
├── NetworkManager (网络管理)
├── UserManager (用户管理)
├── RoomSystemManager (房间系统)
└── GamePlayManager (游戏逻辑)
```

### 服务器架构
```
GameServer (主服务)
├── ServerNetworkManager (网络管理)
├── UserManager (用户管理)
└── RoomManager (房间管理)
```

### 消息流程
1. **客户端**: 创建具体消息对象 → 序列化 → 发送
2. **网络传输**: TCP 连接传输字节数据
3. **服务器**: 接收字节数据 → 反序列化 → 类型路由 → 具体处理器

## 协议定义



## 开发工作流程

### 1. 修改协议
1. 编辑 `AstrumConfig/Proto/*.proto` 文件
2. 运行协议生成工具: `cd AstrumTool/Proto2CS && dotnet run`
3. 代码自动生成到客户端和服务器端

### 2. 添加新功能
1. 在 `.proto` 文件中定义新消息类型
2. 重新生成代码
3. 在客户端和服务器端实现对应的处理逻辑
4. 编写测试用例验证功能

### 3. 测试验证
1. 编译客户端: `cd AstrumProj && dotnet build`
2. 编译服务器: `cd AstrumServer/AstrumServer && dotnet build`
3. 运行测试: `cd AstrumTest/AstrumTest && dotnet test`

## 关键修复记录

### 1. 协议数据统一
- **问题**: 客户端和服务器端使用不同的消息结构
- **解决**: 修改 `.proto` 文件，重新生成代码，确保两端一致性

### 2. 消息类型常量
- **问题**: 测试代码中的消息类型常量值不匹配
- **解决**: 更新测试代码使用正确的常量值

### 3. 消息处理机制
- **问题**: 客户端发送 NetworkMessage 包装，服务器直接处理具体类型
- **解决**: 重构消息处理，移除 NetworkMessage 包装，直接使用具体消息类型

## 运行说明

### 启动服务器
```bash
cd AstrumServer/AstrumServer
dotnet run
```

### 启动客户端
1. 打开 Unity 项目 `AstrumProj`
2. 运行场景，客户端会自动连接服务器

### 运行测试
```bash
cd AstrumTest/AstrumTest
dotnet test
```

## 注意事项

1. **协议修改**: 永远不要直接修改 `Generated/` 目录下的文件
2. **代码生成**: 修改协议后必须重新运行生成工具
3. **消息类型**: 使用具体的消息类型，避免使用通用的 NetworkMessage 包装
4. **测试覆盖**: 新功能必须包含相应的测试用例

## 联系方式

如有问题或建议，请通过项目仓库提交 Issue 或 Pull Request。

