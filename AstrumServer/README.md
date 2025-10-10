# AstrumServer

> 🖥️ 服务器项目 | Server Project

基于 .NET 9.0 的 Astrum 游戏服务器。

---

## 🚀 快速启动

### 方式1: 使用启动脚本（推荐）
```bash
# 从项目根目录
./start_server.bat
```

### 方式2: 手动启动
```bash
cd AstrumServer/AstrumServer
dotnet run
```

---

## 🏗️ 服务器架构

```
AstrumServer/
├── AstrumServer/           # 主服务器项目
│   ├── Program.cs          # 入口
│   ├── GameServer.cs       # 游戏服务器
│   ├── Managers/           # 管理器
│   │   ├── UserManager.cs  # 用户管理
│   │   └── RoomManager.cs  # 房间管理
│   └── Handlers/           # 消息处理器
└── AstrumServer.sln
```

---

## ⚙️ 配置

### 默认配置
- **端口**: 8080
- **协议**: TCP
- **序列化**: MemoryPack + Protocol Buffers

### 依赖项
- .NET 9.0 Runtime
- MemoryPack
- Protocol Buffers

---

## 📚 详细文档

服务器的详细配置和使用说明，请查看：

- [服务器配置](../Docs/07-Development%20开发指南/Server-Setup%20服务器配置.md)
- [服务器使用说明](../Docs/07-Development%20开发指南/Server-Usage%20服务器使用说明.md)
- [LogicCore集成](../Docs/07-Development%20开发指南/LogicCore-Integration%20LogicCore集成说明.md)
- [服务器依赖](../Docs/07-Development%20开发指南/Server-Dependencies%20服务器依赖.md)

---

## 🔗 相关链接

- [项目首页](../README.md)
- [房间系统](../Docs/01-GameDesign%20游戏设计/Room-System%20房间系统.md)
- [网络系统](../Docs/08-Technical%20技术参考/)

---

**最后更新**: 2025-10-10

