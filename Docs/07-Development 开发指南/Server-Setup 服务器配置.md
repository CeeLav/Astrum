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

## 帧同步输入收集与下发策略

为保证各客户端接收的帧数据一致且可复现，服务器在帧同步阶段采用如下策略：

- 接收与登记
  - 当客户端上报 `SingleInput` 时，服务器提取其中的 `Input.PlayerId`（若 `SingleInput.PlayerID != 0` 则优先使用该值），并存入房间状态。
  - 仅登记非零 `PlayerId` 到“历史上报玩家ID集合”（用于后续下发对齐）。
  - 输入会按帧号缓存在 `RoomFrameSyncState` 的 `_frameInputs` 中，服务器会将过期帧号调整为 `AuthorityFrame + 1`，过远未来帧号上限为 `AuthorityFrame + MAX_CACHE_FRAMES`。

- 收集一帧的输入（下发前）
  - 针对“历史上报玩家ID集合”中的每个 `PlayerId`：
    - 若本帧收到了该玩家的实际输入，则使用实际输入；
    - 若本帧未收到，则为该 `PlayerId` 生成一个“默认空输入”（移动为0、技能为false、攻击为false、出生信息为0，帧号为当前权威帧）。
  - 始终排除 `PlayerId = 0` 的无效输入。

- 下发
  - 将上述收集结果打包为 `FrameSyncData` 下发给房间内所有在线玩家，保证“曾上报过的玩家”在每帧均有条目（即便为空输入）。

- 日志
  - 收到输入时：输出玩家ID、帧号及有效内容（移动/出生/攻击/技能）。
  - 收集帧时：输出收集到的玩家ID列表；若某历史玩家本帧未上报，会记录“填充默认空输入”。
  - 发送帧时：输出帧号与输入条目数，并按玩家输出其有效输入摘要。

- 目的
  - 解决“玩家数量不一致/某玩家缺行”的问题；
  - 保持帧序列在全体客户端的确定性与可复现性；
  - 便于排查因未上报导致的行为停滞（通过空输入显式呈现）。

## 下一步开发

可以考虑添加以下功能：
- 游戏逻辑处理
- 数据库集成
- 配置文件支持
- 更复杂的消息协议
- 房间/大厅系统 

## 网络管理器架构

项目采用接口抽象的网络管理器设计，支持两种模式：

### 网络模式 vs 本地模式

- **网络模式**：使用 `ServerNetworkManager` 进行真实 TCP 网络通信
- **本地模式**：使用 `LocalServerNetworkManager` 进行内存模拟，用于测试

### 核心组件

- `IServerNetworkManager`：网络管理器接口
- `ServerNetworkManager`：网络模式实现（真实TCP）
- `LocalServerNetworkManager`：本地模式实现（内存模拟）
- `NetworkManagerFactory`：工厂类，根据模式创建相应的管理器

## 如何添加服务器测试用例（单进程）

为避免多进程/端口占用导致的不稳定性，项目提供了本地模式网络管理器，支持在 Test 工程内单进程运行服务器逻辑并编写健壮性测试。

### 1. 使用本地模式网络管理器

```csharp
// 创建本地模式网络管理器
var localNetworkManager = new LocalServerNetworkManager();
var userManager = new UserManager();
var roomManager = new RoomManager();

// 创建游戏服务器，使用本地模式
var gameServer = new GameServer(localNetworkManager, userManager, roomManager, null);
```

### 2. 测试用例编写

```csharp
[Fact]
public async Task LocalNetworkManager_ShouldSupportDirectInteraction()
{
    // 初始化网络管理器
    await localNetworkManager.InitializeAsync(8888);

    // 模拟客户端连接
    var sessionId = localNetworkManager.SimulateConnect();

    // 模拟客户端发送消息
    var loginRequest = LoginRequest.Create();
    loginRequest.DisplayName = "TestUser";
    localNetworkManager.SimulateReceive(sessionId, loginRequest);

    // 更新网络状态，处理消息
    localNetworkManager.Update();

    // 验证服务器响应
    var pendingMessages = localNetworkManager.GetPendingMessages(sessionId);
    Assert.Contains(pendingMessages, msg => msg is LoginResponse);
}
```

### 3. 本地模式专用方法

- `SimulateConnect()`：模拟客户端连接
- `SimulateDisconnect(sessionId, abrupt)`：模拟客户端断开
- `SimulateReceive(sessionId, message)`：模拟客户端发送消息
- `GetPendingMessages(sessionId)`：获取待发送消息
- `ClearPendingMessages(sessionId)`：清空待发送消息
- `GetPendingMessageStats()`：获取消息统计

### 4. 运行测试

```bash
cd AstrumTest/AstrumTest
dotnet test --filter FullyQualifiedName~NetworkManagerInterfaceTests
```

### 5. 测试用例示例

参考 `NetworkManagerInterfaceTests.cs` 中的完整测试用例，包括：
- 直接交互测试
- 房间操作测试
- 异常断开测试
- 消息广播测试
- 消息统计测试

### 6. 工厂模式使用

```csharp
// 根据模式创建网络管理器
var networkManager = NetworkManagerFactory.Create(NetworkManagerMode.Local);
var localManager = NetworkManagerFactory.CreateLocal();
var networkManager = NetworkManagerFactory.CreateNetwork();

// 根据环境变量创建
var manager = NetworkManagerFactory.CreateFromEnvironment();
```

提示：本地模式完全封装了服务器逻辑，测试用例只能通过 `LocalServerNetworkManager` 与服务器交互，无法直接访问服务器内部代码，确保了良好的封装性。
