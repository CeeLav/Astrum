# Astrum 项目技术架构

## 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    Astrum 游戏系统                           │
├─────────────────────────────────────────────────────────────┤
│  Unity Client (AstrumProj)                                  │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ │
│  │   View Layer    │ │  Client Layer   │ │  Logic Layer    │ │
│  │   (AstrumView)  │ │ (AstrumClient)  │ │ (AstrumLogic)   │ │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘ │
│           │                   │                   │         │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              Network Layer (Session-based)              │ │
│  └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                         TCP/IP                              │
├─────────────────────────────────────────────────────────────┤
│  .NET Server (AstrumServer)                                 │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ │
│  │  Game Logic     │ │  Network Layer  │ │   Data Layer    │ │
│  │   (Rooms)       │ │   (Sessions)    │ │  (Persistence)  │ │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## 网络架构详图 (重构后)

```
Client Side:
┌─────────────────────────────────────────────────────────────┐
│                   NetworkManager                            │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │                    Events                               │ │
│  │  • OnConnected    • OnDisconnected                     │ │
│  │  • OnConnectionStatusChanged  • OnMessageReceived      │ │
│  └─────────────────────────────────────────────────────────┘ │
│                           │                                 │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │                   Session                               │ │
│  │  • Id: long                                             │ │
│  │  • RemoteAddress: IPEndPoint                           │ │
│  │  • Send(IMessage): void                                 │ │
│  │  • LastSendTime / LastRecvTime                         │ │
│  └─────────────────────────────────────────────────────────┘ │
│                           │                                 │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │                  TService                               │ │
│  │  • TCP Socket Management                               │ │
│  │  • Message Serialization                               │ │
│  │  • Connection Handling                                 │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                         TCP Connection
                              │
Server Side:
┌─────────────────────────────────────────────────────────────┐
│                 AstrumServer (.NET 9.0)                    │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ │
│  │    Managers     │ │     Models      │ │      Room       │ │
│  │                 │ │                 │ │   Management    │ │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘ │
│                           ↑                                 │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │           Unity程序集依赖 (新增)                        │ │
│  │  • AstrumLogic.dll   • CommonBase.dll                  │ │
│  │  • Network.dll       • MemoryPack                      │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## 客户端管理器架构

```
GameApplication (入口点)
├── NetworkManager (网络管理)
│   ├── Session (网络会话)
│   ├── TService (TCP服务)
│   └── Events (网络事件)
├── GamePlayManager (游戏逻辑管理)
│   ├── Room Management (房间管理)
│   ├── User Management (用户管理)
│   └── Game State (游戏状态)
├── ResourceManager (资源管理)
├── SceneManager (场景管理)
├── UIManager (UI管理)
├── AudioManager (音频管理)
└── InputManager (输入管理)
```

## 消息流程图

```
Message Sending Flow:
GamePlayManager → NetworkManager → Session → TService → TCP → Server

Message Receiving Flow:
Server → TCP → TService → NetworkManager → Event → GameApplication/GamePlayManager

Event Flow:
NetworkManager.OnMessageReceived
├── GameApplication.OnNetworkMessageReceived (全局消息)
└── GamePlayManager.OnNetworkMessageReceived (游戏消息)
```

## 数据流架构

```
User Input → InputManager → GamePlayManager → NetworkManager → Server
                ↓
        Local Game State Update
                ↓
         View Layer Update
                ↓
            UI Rendering

Server Response → NetworkManager → GamePlayManager → Game State Update
                                        ↓
                                 View Layer Update
                                        ↓
                                   UI Rendering
```

## ECS 架构 (LogicCore)

```
World (游戏世界)
├── EntityManager (实体管理器)
│   └── Entity[] (实体数组)
├── ComponentManager (组件管理器)
│   └── Component[][] (组件数据)
└── SystemManager (系统管理器)
    ├── UpdateSystems (更新系统)
    ├── RenderSystems (渲染系统)
    └── NetworkSystems (网络系统)
```

## 帧同步架构

```
Client Input → InputBuffer → FrameSync → Server Validation
                ↓
         Local Prediction
                ↓
          State Update
                ↓
        Rollback (if needed)
```

## 文件结构映射

```
AstrumProj/Assets/Script/
├── AstrumClient/          # 客户端层
│   ├── Managers/          # 管理器
│   │   ├── NetworkManager.cs      # 网络管理器 ⭐
│   │   ├── GamePlayManager.cs     # 游戏逻辑管理器 ⭐
│   │   └── ...
│   ├── Core/              # 核心组件
│   │   └── GameApplication.cs     # 应用程序入口 ⭐
│   └── Examples/          # 示例代码
├── Network/               # 网络层 ⭐
│   ├── Session.cs         # 网络会话 ⭐
│   ├── TService.cs        # TCP服务 ⭐
│   ├── AService.cs        # 抽象服务基类
│   ├── MessageHandler.cs  # 消息处理器
│   └── Generated/         # 自动生成的消息类
├── CommonBase/            # 公共基础库
│   ├── Singleton.cs       # 单例基类
│   ├── ASLogger.cs        # 日志系统
│   └── Object/            # 对象系统
└── AstrumLogic/          # 逻辑层
    ├── Core/             # 核心逻辑
    ├── FrameSync/        # 帧同步
    └── ...
```

## 关键设计模式

### 1. 单例模式 (Singleton)
```csharp
public class NetworkManager : Singleton<NetworkManager>
{
    // 所有管理器都继承此模式
}
```

### 2. 事件驱动模式
```csharp
public event Action<NetworkMessage> OnMessageReceived;
// 通过事件实现松耦合通信
```

### 3. 工厂模式
```csharp
NetworkMessage.Create() // 对象池工厂
Session.Initialize()    // 初始化工厂
```

### 4. 观察者模式
```csharp
networkManager.OnConnected += OnConnectedHandler;
// 状态变化通知
```

## 性能优化策略

### 1. 网络优化
- **对象池**: 消息对象复用
- **序列化**: MemoryPack 高性能序列化
- **连接池**: TCP 连接复用
- **批处理**: 消息批量发送

### 2. 内存优化
- **ECS架构**: 数据局部性优化
- **对象池**: 减少GC压力
- **延迟加载**: 按需加载资源

### 3. 渲染优化
- **URP管线**: 现代渲染管线
- **批处理**: 绘制调用优化
- **LOD系统**: 细节层次优化

## 测试架构

```
Testing Framework:
├── Unit Tests (LogicCore.Tests/)
│   ├── Network Tests
│   ├── Logic Tests
│   └── Component Tests
├── Integration Tests (NetworkTestRunner.cs)
│   ├── Client-Server Communication
│   ├── Message Flow Tests
│   └── Performance Tests
└── Command Line Tools
    ├── Test-UnityNetwork.ps1    # PowerShell测试脚本
    ├── run_unity_tests.bat      # 批处理测试脚本
    └── NetworkTestRunner.cs     # Unity编辑器测试类
```

## 依赖架构 (最新更新)

```
项目依赖关系:

Unity Client Project (AstrumProj)
├── Assets/Script/
│   ├── AstrumLogic/     → AstrumLogic.dll
│   ├── CommonBase/      → CommonBase.dll
│   └── Network/         → Network.dll
│
├── Library/ScriptAssemblies/ (编译输出)
│   ├── AstrumLogic.dll  ←─┐
│   ├── CommonBase.dll   ←─┼─ 服务器依赖这些dll
│   └── Network.dll      ←─┘
│
.NET Server Project (AstrumServer)
├── 直接引用Unity程序集:
│   ├── AstrumLogic.dll (帧同步逻辑)
│   ├── CommonBase.dll (日志、对象池等)
│   └── Network.dll (网络消息定义)
├── NuGet依赖:
│   ├── MemoryPack (序列化)
│   ├── Newtonsoft.Json (JSON处理)
│   └── System.Numerics.Vectors
└── .NET框架依赖
```

**构建流程**:
```
1. Unity编译 → 生成程序集dll文件
2. 服务器编译 → 链接Unity程序集
3. 运行时 → 共享相同的类型定义
```

## 部署架构

```
Development Environment:
├── Unity Editor (6000.2.0b7)
├── Visual Studio / Rider
├── Git Repository
└── Local Test Server

Production Environment:
├── Unity Client (Windows Standalone)
├── .NET Server (Linux/Windows Server)
│   └── Unity程序集dll文件 (部署时需要)
├── Database (MongoDB/SQL Server)
└── Load Balancer
```

## 监控和日志

```
Logging System:
├── Client Logs
│   ├── Unity Console
│   ├── File Logs (*.log)
│   └── Network Debug Logs
└── Server Logs
    ├── Application Logs
    ├── Network Traffic Logs
    └── Error Logs

Monitoring:
├── Connection Status
├── Message Flow Rate
├── Performance Metrics
└── Error Tracking
```

---

**说明**: 
- ⭐ 标记表示最近重构的关键文件
- 虚线表示异步通信
- 实线表示同步调用
- 双向箭头表示双向通信
