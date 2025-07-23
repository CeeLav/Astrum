# AstrumServer

Astrum游戏的服务器端项目，使用C#和.NET 9开发。

## 功能特性

- TCP服务器，监听端口8888
- 支持多客户端连接
- 异步处理客户端消息
- 日志记录功能
- 优雅关闭机制

## 运行要求

- .NET 9.0 SDK
- Windows/Linux/macOS

## 如何运行

1. 进入项目目录：
```bash
cd AstrumServer
```

2. 还原依赖包：
```bash
dotnet restore
```

3. 编译项目：
```bash
dotnet build
```

4. 运行服务器：
```bash
dotnet run
```

## 测试连接

可以使用任何TCP客户端工具连接到服务器：
- 地址：localhost
- 端口：8888

例如使用telnet：
```bash
telnet localhost 8888
```

## 项目结构

```
AstrumServer/
├── AstrumServer/
│   ├── Program.cs          # 主程序入口
│   ├── AstrumServer.csproj # 项目文件
│   └── obj/                # 编译输出
└── README.md              # 说明文档
```

## 开发说明

- 服务器使用BackgroundService模式运行
- 每个客户端连接都在独立的任务中处理
- 支持优雅关闭，会正确清理所有连接
- 使用结构化日志记录运行状态

## 下一步开发

可以考虑添加以下功能：
- 游戏逻辑处理
- 数据库集成
- 配置文件支持
- 更复杂的消息协议
- 房间/大厅系统 