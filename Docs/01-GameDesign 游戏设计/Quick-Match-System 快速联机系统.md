# 快速联机系统策划案

## 1. 系统概述
快速联机是一个自动匹配系统，允许玩家快速找到对手并开始游戏，无需手动创建或浏览房间。系统采用先到先得的匹配策略，当等待队列中有足够的玩家时自动创建房间并开始游戏。

**设计目标**：
- 降低联机门槛，简化联机流程
- 快速匹配，减少等待时间
- 自动房间创建和管理
- 良好的用户体验和反馈

## 2. 核心机制

### 2.1 匹配流程
```
玩家登录成功 → 点击"快速联机" → 进入等待队列 → 自动匹配 → 创建房间 → 开始游戏
                                   ↓
                              匹配中可取消
                                   ↓
                            超时1分钟自动退出
```

### 2.2 匹配策略
- **匹配规则**：先到先得，有人在等待就立即配对
- **房间配置**：
  - 最大玩家数：4人
  - 房间名称：自动生成（格式：`QuickMatch_[时间戳]`）
  - 房间类型：快速匹配房间
- **匹配触发**：
  - 当等待队列中有2人时，立即创建房间并将两人加入
  - 房间创建后继续接受匹配玩家加入（最多4人）
  - 预留中途加入机制（暂不实现，需要游戏数据快照支持）

### 2.3 超时机制
- **超时时间**：1分钟（60秒）
- **超时处理**：
  - 客户端：返回登录界面默认状态，按钮恢复为"快速联机"
  - 服务器：将玩家从等待队列移除
  - 通知：向玩家发送超时通知消息

### 2.4 取消匹配
- **触发方式**：点击"取消匹配"按钮
- **处理流程**：
  1. 客户端发送取消匹配请求
  2. 服务器从等待队列移除玩家
  3. 服务器返回取消成功响应
  4. 客户端按钮恢复为"快速联机"

## 3. 数据结构设计

### 3.1 匹配队列项 (MatchmakingEntry)
```protobuf
// 服务器端内部数据结构（不需要网络传输）
struct MatchmakingEntry
{
    string UserId;              // 用户ID
    string DisplayName;         // 显示名称
    long EnterQueueTime;        // 进入队列时间（Unix毫秒）
    long TimeoutTime;           // 超时时间（Unix毫秒）
}
```

### 3.2 快速匹配房间信息
```protobuf
// 扩展 RoomInfo，添加快速匹配标识
message RoomInfo
{
    // ... 现有字段 ...
    bool IsQuickMatch = 8;      // 是否为快速匹配房间
}
```

## 4. 网络协议设计

### 4.1 客户端请求消息

#### 快速匹配请求
```protobuf
message QuickMatchRequest
{
    int64 Timestamp = 1;        // 时间戳
}
```

#### 取消匹配请求
```protobuf
message CancelMatchRequest
{
    int64 Timestamp = 1;        // 时间戳
}
```

### 4.2 服务器响应消息

#### 快速匹配响应
```protobuf
message QuickMatchResponse
{
    bool Success = 1;           // 是否成功
    string Message = 2;         // 响应消息
    int64 Timestamp = 3;        // 时间戳
    int32 QueuePosition = 4;    // 队列位置（0表示第一个）
    int32 QueueSize = 5;        // 当前队列人数
}
```

#### 取消匹配响应
```protobuf
message CancelMatchResponse
{
    bool Success = 1;           // 是否成功
    string Message = 2;         // 响应消息
    int64 Timestamp = 3;        // 时间戳
}
```

### 4.3 服务器通知消息

#### 匹配成功通知
```protobuf
message MatchFoundNotification
{
    RoomInfo Room = 1;          // 房间信息
    int64 Timestamp = 2;        // 时间戳
    repeated string PlayerIds = 3; // 所有匹配到的玩家ID列表
}
```

#### 匹配超时通知
```protobuf
message MatchTimeoutNotification
{
    string Message = 1;         // 超时消息
    int64 Timestamp = 2;        // 时间戳
    int32 WaitTimeSeconds = 3;  // 等待时长（秒）
}
```

## 5. UI界面设计

### 5.1 登录界面 (LoginView) 按钮状态

| 状态 | 按钮文本 | 可交互 | 说明 |
|------|---------|--------|------|
| 未连接 | "连接服务器" | ✅ | 初始状态 |
| 连接中 | "连接中..." | ❌ | 正在建立连接 |
| 已连接未登录 | "已连接" | ❌ | 等待登录响应 |
| 登录成功 | "快速联机" | ✅ | **新增状态**，可开始匹配 |
| 匹配中 | "取消匹配" | ✅ | **新增状态**，可取消匹配 |
| 匹配成功 | "进入房间中..." | ❌ | 即将进入房间 |

### 5.2 状态转换流程
```
未连接 → [点击] → 连接中 → [连接成功] → 已连接未登录 → [登录成功] → 快速联机
                                                                      ↓
                                                                  [点击]
                                                                      ↓
    [匹配成功] ← 进入房间中 ← [匹配成功通知] ← 匹配中 ← [发送匹配请求]
                                                ↓
                                          [点击或超时]
                                                ↓
                                            快速联机
```

### 5.3 反馈信息
- **连接状态文本**（connectionStatusText）：
  - 显示当前状态的详细信息
  - 例如："等待匹配中... (队列位置: 1/5)"
  - 匹配成功："匹配成功！正在进入房间..."
  - 匹配超时："匹配超时，请重试"

## 6. 客户端功能逻辑

### 6.1 按钮点击处理
```csharp
private void OnConnectButtonClicked()
{
    switch (currentState)
    {
        case LoginState.Connected:      // 显示"快速联机"
            StartQuickMatch();          // 发送快速匹配请求
            break;
        
        case LoginState.Matching:       // 显示"取消匹配"
            CancelQuickMatch();         // 发送取消匹配请求
            break;
        
        default:
            ConnectToServer();          // 原有的连接逻辑
            break;
    }
}
```

### 6.2 匹配状态管理
```csharp
enum LoginState
{
    Disconnected,       // 未连接
    Connecting,         // 连接中
    Connected,          // 已连接已登录（显示"快速联机"）
    Matching,           // 匹配中（显示"取消匹配"）
    MatchFound,         // 匹配成功（准备进入房间）
}
```

### 6.3 事件订阅
- `OnQuickMatchResponse`：处理快速匹配响应
- `OnCancelMatchResponse`：处理取消匹配响应
- `OnMatchFoundNotification`：处理匹配成功通知
- `OnMatchTimeoutNotification`：处理匹配超时通知

### 6.4 超时倒计时显示
- 显示剩余等待时间："等待匹配中... (45秒)"
- 客户端每秒更新一次显示
- 服务器负责实际的超时判定

## 7. 服务器功能逻辑

### 7.1 匹配管理器 (MatchmakingManager)

#### 核心功能
- **队列管理**：维护等待匹配的玩家队列
- **自动匹配**：检测队列并自动配对玩家
- **超时检测**：定期检查队列中玩家的等待时间
- **房间创建**：匹配成功后自动创建房间并通知玩家

#### 关键方法
```csharp
public class MatchmakingManager
{
    // 加入匹配队列
    bool EnqueuePlayer(string userId, string displayName);
    
    // 离开匹配队列
    bool DequeuePlayer(string userId);
    
    // 检查并执行匹配
    void CheckAndMatchPlayers();
    
    // 检查超时
    void CheckTimeouts();
    
    // 获取队列信息
    (int position, int total) GetQueueInfo(string userId);
}
```

### 7.2 匹配逻辑
```csharp
void CheckAndMatchPlayers()
{
    // 如果队列中有2个或以上玩家
    if (queue.Count >= 2)
    {
        // 取出前2个玩家（先进先出）
        var players = queue.Take(2).ToList();
        
        // 创建快速匹配房间
        var room = CreateQuickMatchRoom(players);
        
        // 将玩家从队列移除并加入房间
        foreach (var player in players)
        {
            queue.Remove(player);
            JoinRoomForPlayer(player, room.Id);
        }
        
        // 通知所有匹配玩家
        NotifyMatchFound(players, room);
    }
}
```

### 7.3 超时处理
```csharp
void CheckTimeouts()
{
    var now = TimeInfo.Instance.ClientNow();
    var timeoutPlayers = new List<string>();
    
    foreach (var entry in queue)
    {
        if (now >= entry.TimeoutTime)
        {
            timeoutPlayers.Add(entry.UserId);
        }
    }
    
    foreach (var userId in timeoutPlayers)
    {
        // 从队列移除
        DequeuePlayer(userId);
        
        // 发送超时通知
        NotifyMatchTimeout(userId);
    }
}
```

### 7.4 房间自动创建
```csharp
RoomInfo CreateQuickMatchRoom(List<MatchmakingEntry> players)
{
    var roomName = $"QuickMatch_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    var creatorId = players[0].UserId;
    var maxPlayers = 4;
    
    var room = roomManager.CreateRoom(creatorId, roomName, maxPlayers);
    room.IsQuickMatch = true;  // 标记为快速匹配房间
    
    return room;
}
```

## 8. 系统流程设计

### 8.1 快速匹配流程
1. 玩家点击"快速联机"按钮
2. 客户端发送 `QuickMatchRequest` 到服务器
3. 服务器将玩家加入匹配队列
4. 服务器返回 `QuickMatchResponse`（包含队列位置信息）
5. 客户端更新按钮为"取消匹配"，显示等待状态
6. 服务器定期检查队列：
   - 如果有2人或以上：立即执行匹配
   - 如果玩家等待超过1分钟：发送超时通知
7. 匹配成功：
   - 服务器创建房间并将玩家加入
   - 服务器发送 `MatchFoundNotification` 给所有匹配玩家
   - 客户端自动进入房间（类似于 JoinRoomResponse）
8. 匹配超时：
   - 服务器发送 `MatchTimeoutNotification`
   - 客户端返回登录界面，按钮恢复为"快速联机"

### 8.2 取消匹配流程
1. 玩家点击"取消匹配"按钮
2. 客户端发送 `CancelMatchRequest` 到服务器
3. 服务器从队列中移除玩家
4. 服务器返回 `CancelMatchResponse`
5. 客户端按钮恢复为"快速联机"，清除等待状态

### 8.3 匹配成功流程
1. 服务器检测到队列中有2人或以上
2. 服务器自动创建房间（4人上限）
3. 将匹配到的玩家加入房间
4. 发送 `MatchFoundNotification` 给所有玩家
5. 客户端接收通知：
   - 更新按钮为"进入房间中..."
   - 切换到房间详情界面（RoomDetailView）
   - 显示房间信息和其他玩家

### 8.4 异常处理流程

#### 玩家断线
- **匹配中断线**：服务器自动从队列移除
- **房间中断线**：按现有房间系统处理（从房间移除）

#### 服务器重启
- 所有匹配队列清空
- 玩家需要重新发起匹配

#### 网络延迟
- 客户端有3秒的请求超时
- 超时后允许玩家重试

## 9. 配置参数

### 9.1 匹配配置
```csharp
public static class MatchmakingConfig
{
    // 匹配超时时间（毫秒）
    public const long MATCH_TIMEOUT_MS = 60000;  // 1分钟
    
    // 最小匹配人数
    public const int MIN_MATCH_PLAYERS = 2;
    
    // 快速匹配房间最大人数
    public const int QUICK_MATCH_MAX_PLAYERS = 4;
    
    // 队列检查间隔（毫秒）
    public const int QUEUE_CHECK_INTERVAL_MS = 1000;  // 1秒
    
    // 超时检查间隔（毫秒）
    public const int TIMEOUT_CHECK_INTERVAL_MS = 5000;  // 5秒
}
```

### 9.2 UI配置
```csharp
public static class MatchmakingUIConfig
{
    // 按钮文本
    public const string BTN_TEXT_QUICK_MATCH = "快速联机";
    public const string BTN_TEXT_CANCEL_MATCH = "取消匹配";
    public const string BTN_TEXT_ENTERING_ROOM = "进入房间中...";
    
    // 状态文本
    public const string STATUS_WAITING = "等待匹配中...";
    public const string STATUS_MATCH_FOUND = "匹配成功！正在进入房间...";
    public const string STATUS_MATCH_TIMEOUT = "匹配超时，请重试";
    public const string STATUS_MATCH_CANCELLED = "已取消匹配";
}
```

## 10. 技术实现要点

### 10.1 线程安全
- 匹配队列使用 `ConcurrentQueue` 或加锁保护
- 服务器主循环中定期检查队列和超时

### 10.2 性能优化
- 队列检查和超时检查使用不同的间隔
- 避免频繁创建和销毁对象
- 使用对象池管理匹配通知消息

### 10.3 日志记录
- 记录所有匹配相关事件：加入队列、匹配成功、超时、取消
- 记录房间创建和玩家加入信息
- 便于调试和监控系统运行状态

### 10.4 扩展性预留

#### 中途加入机制
- 房间标记为 `IsQuickMatch = true`
- 游戏开始后继续接受匹配玩家加入（最多4人）
- **前提条件**：需要实现游戏数据快照系统
- **暂不实现**：当前版本仅支持匹配阶段加入

#### 匹配策略扩展
- 预留匹配策略接口 `IMatchmakingStrategy`
- 未来可支持：
  - 技能等级匹配
  - 延迟匹配（Ping值）
  - 组队匹配
  - 段位匹配

#### 房间配置扩展
- 预留房间配置参数
- 未来可支持：
  - 自定义人数
  - 游戏模式选择
  - 地图选择

## 11. 测试要点

### 11.1 功能测试
- ✅ 单人匹配 → 等待 → 第二人加入 → 匹配成功
- ✅ 匹配中取消 → 成功退出队列
- ✅ 匹配超时（1分钟）→ 自动退出队列
- ✅ 多人同时匹配 → 按顺序配对
- ✅ 匹配成功后房间状态正确

### 11.2 边界测试
- ⚠️ 玩家在匹配中断线 → 自动从队列移除
- ⚠️ 服务器重启 → 队列清空，玩家可重新匹配
- ⚠️ 网络延迟 → 超时重试机制
- ⚠️ 队列已满 → 返回错误信息（暂不实现队列上限）

### 11.3 压力测试
- 🔧 100人同时匹配 → 系统稳定性
- 🔧 频繁取消和重新匹配 → 无内存泄漏
- 🔧 长时间运行 → 超时检测正常工作

## 12. 与现有系统的集成

### 12.1 房间系统集成
- 快速匹配创建的房间与手动创建的房间共用 `RoomManager`
- 通过 `IsQuickMatch` 标识区分房间类型
- 玩家可以正常离开快速匹配房间

### 12.2 网络系统集成
- 复用现有的 `NetworkManager` 和消息处理机制
- 添加新的事件类型和消息处理器
- 保持与现有协议的兼容性

### 12.3 UI系统集成
- 修改现有 `LoginView`，不创建新界面
- 复用现有 `RoomDetailView` 显示快速匹配房间
- 保持UI风格统一

## 13. 未来优化方向

### 13.1 短期优化（v1.1）
- 显示匹配倒计时
- 显示队列人数
- 添加匹配音效反馈

### 13.2 中期优化（v1.2）
- 实现中途加入机制
- 支持好友组队匹配
- 添加匹配历史记录

### 13.3 长期优化（v2.0）
- 技能评级匹配
- 赛季和排位匹配
- 匹配质量分析和优化

---

**文档版本**: v1.0  
**创建时间**: 2025-10-13  
**文档状态**: 📝 策划完成，待开发  
**预计开发周期**: 2-3天

---

**相关文档**:
- [房间系统](Room-System%20房间系统.md)
- [快速联机开发进展](Quick-Match-Progress%20快速联机开发进展.md)
- [游戏主策划案](Game-Design%20游戏主策划案.md)

