# 🎯 单机模式设计文档

**版本**: v0.3.0  
**状态**: ✅ 核心功能完成  
**最后更新**: 2025-10-11

---

## 📋 目录

1. [概述](#概述)
2. [设计目标](#设计目标)
3. [核心架构](#核心架构)
4. [启动流程](#启动流程)
5. [帧循环机制](#帧循环机制)
6. [AI系统](#ai系统)
7. [存档系统](#存档系统)
8. [关卡系统](#关卡系统)
9. [与联机模式的区别](#与联机模式的区别)

---

## 概述

单机模式是 Astrum 游戏的本地游玩方式，玩家无需连接服务器即可体验完整的游戏内容。该模式采用**本地帧同步**架构，与联机模式共享相同的逻辑层代码，确保游戏体验的一致性。

### 核心特性

- **无网络依赖** - 完全本地运行，0延迟
- **确定性逻辑** - 使用 TrueSync 确保逻辑确定性
- **AI对手** - 智能AI替代真实玩家
- **存档支持** - 本地存档系统
- **可暂停** - 支持游戏暂停和继续
- **调试友好** - 便于开发和测试

---

## 设计目标

### 开发阶段目标

1. **快速验证玩法** 🎮
   - 不依赖服务器和网络，快速迭代核心玩法
   - 提供完整的战斗、技能、物理系统测试环境

2. **逻辑层测试** 🧪
   - 作为逻辑层的主要测试环境
   - 验证 ECC 架构、帧同步、碰撞检测等核心系统

3. **内容开发基础** 📦
   - 关卡设计和测试
   - AI行为调试
   - 数值平衡

### 玩家体验目标

1. **完整游戏体验** ✅
   - 提供与联机模式相同质量的战斗体验
   - 支持所有角色、技能、装备系统

2. **流畅性能** ⚡
   - 60 FPS 稳定帧率
   - 即时响应，无延迟

3. **进度保存** 💾
   - 关卡进度自动保存
   - 角色成长数据持久化

---

## 核心架构

### 系统组件

```
┌─────────────────────────────────────────┐
│         GameApplication (入口)          │
└─────────────────────────────────────────┘
                    │
        ┌───────────┴───────────┐
        ↓                       ↓
┌───────────────┐       ┌──────────────┐
│  GameConfig   │       │GamePlayManager│
│  (配置管理)   │       │  (游戏管理)   │
└───────────────┘       └──────────────┘
                                │
                    ┌───────────┴───────────┐
                    ↓                       ↓
            ┌──────────────┐        ┌─────────────┐
            │     Room     │───────→│    World    │
            │  (游戏房间)   │        │ (ECS世界)   │
            └──────────────┘        └─────────────┘
                    │
                    ↓
            ┌──────────────┐
            │     Stage    │
            │  (表现舞台)   │
            └──────────────┘
```

### 核心类说明

#### 1. GameConfig
**路径**: `AstrumClient/Core/GameConfig.cs`

**职责**:
- 管理游戏模式状态（单机/联机）
- 游戏难度设置
- 全局配置管理

**关键接口**:
```csharp
public bool IsSinglePlayerMode { get; }
public void SetSinglePlayerMode(bool enabled);
public void SetDifficulty(GameDifficulty difficulty);
```

#### 2. GamePlayManager
**路径**: `AstrumClient/Managers/GamePlayManager.cs`

**职责**:
- 轻量级入口，委托给具体的 GameMode
- 模式选择和切换
- 统一的对外接口

**关键接口**:
```csharp
public void StartGame(string gameSceneName);
public void StartSinglePlayerGame(string gameSceneName);  // 兼容接口
public Stage MainStage { get; }  // 委托给 GameMode
public Room MainRoom { get; }    // 委托给 GameMode
```

#### 3. SinglePlayerGameMode
**路径**: `AstrumClient/Managers/GameModes/SinglePlayerGameMode.cs`

**职责**:
- 单机游戏的完整启动和运行逻辑
- Room、World、Stage 的创建和初始化
- 玩家角色的创建
- 本地帧循环驱动
- 权威帧模拟（模拟服务器推进）

**关键方法**:
```csharp
public void Initialize();
public void StartGame(string sceneName);
public void Update(float deltaTime);  // 本地帧循环
```

#### 4. Room (逻辑层)
**路径**: `LogicCore/Core/Room.cs`

**职责**:
- 游戏逻辑的容器
- 帧同步管理
- 玩家和实体的管理

**关键属性**:
```csharp
public World MainWorld { get; set; }
public int RoomId { get; }
public string RoomName { get; }
```

#### 5. World (逻辑层)
**路径**: `LogicCore/Core/World.cs`

**职责**:
- ECS 系统的容器
- 实体的创建和管理
- 系统的执行

#### 6. Stage (表现层)
**路径**: `AstrumView/Core/Stage.cs`

**职责**:
- Unity 场景管理
- 实体视图的创建和同步
- 表现层的生命周期

---

## 启动流程

### 完整流程图

```
玩家点击"单机游戏"按钮
        ↓
[1] GameConfig.SetSinglePlayerMode(true)
        ↓
[2] GamePlayManager.StartSinglePlayerGame("DungeonsGame")
        ↓
[3] EnsureCorrectGameMode()
    - 检测到需要创建 SinglePlayerGameMode
    - 创建并初始化 SinglePlayerGameMode
        ↓
[4] SinglePlayerGameMode.StartGame("DungeonsGame")
        ↓
[5] 创建 Room(id=1, "SinglePlayerRoom")
    - 创建 World 并关联到 Room
    - Room.Initialize() 创建 LSController
        ↓
[6] 创建 Stage("GameStage")
    - Stage.Initialize()
    - Stage.SetRoom(room)
        ↓
[7] 切换到游戏场景 (LoadSceneAsync)
        ↓
[8] 场景加载完成回调 OnGameSceneLoaded()
    - 关闭 Login UI
    - Stage.SetActive(true)
    - Stage.OnEnter()
    - 订阅 EntityView 创建事件
        ↓
[9] 创建玩家实体 CreatePlayer()
    - Room.AddPlayer() 创建 Entity
    - 设置 PlayerId 和 MainPlayerId
    - LSController.Start() ← 启动帧同步
    - 创建对应的 EntityView
        ↓
[10] 开始本地帧循环
    - Unity Update() 驱动
    - SinglePlayerGameMode.Update()
        - AuthorityFrame = PredictionFrame ← 模拟服务器
        - Room.Update() → LSController.Tick()
        - Stage.Update() → 更新表现层
        ↓
游戏运行中...
```

### 详细步骤

#### Step 1-2: 模式设置和启动入口
```csharp
// 在 LoginView 中点击"单机游戏"按钮
GameConfig.Instance.SetSinglePlayerMode(true);
GamePlayManager.Instance.StartSinglePlayerGame("DungeonsGame");
```

#### Step 3-4: 创建 SinglePlayerGameMode
```csharp
// GamePlayManager.StartGame() 内部
EnsureCorrectGameMode();  // 检测并创建 SinglePlayerGameMode
_currentGameMode.StartGame("DungeonsGame");  // 委托给 SinglePlayerGameMode
```

#### Step 5-6: 创建逻辑层环境
```csharp
// SinglePlayerGameMode.StartGame()
var room = new Room(1, "SinglePlayerRoom");
var world = new World();
room.MainWorld = world;
room.Initialize();  // 创建 LSController（但未启动）

Stage gameStage = new Stage("GameStage", "游戏场景");
gameStage.Initialize();
gameStage.SetRoom(room);
```

**关键点**:
- Room 不连接服务器，使用本地帧同步
- LSController 创建但未启动（等待玩家创建后再启动）
- 所有输入直接在本地处理

#### Step 7-8: 场景切换和加载
```csharp
SceneManager.LoadSceneAsync("DungeonsGame", () => {
    OnGameSceneLoaded();
});

// OnGameSceneLoaded()
gameStage.SetActive(true);
gameStage.OnEnter();
```

#### Step 9-10: 创建玩家并启动帧同步
```csharp
// CreatePlayer()
long playerId = room.AddPlayer();  // 创建玩家实体
room.MainPlayerId = playerId;  // 设置主玩家ID

// 启动本地帧同步控制器（关键！）
room.LSController.CreationTime = TimeInfo.Instance.ServerNow();
room.LSController.Start();  // IsRunning = true

// EntityView 会自动创建（通过事件）
```

**关键点**:
- ✅ 必须启动 LSController，否则帧循环不会执行
- ✅ 必须设置 MainPlayerId，否则输入不会被处理

---

## 帧循环机制

### 本地帧同步架构

单机模式使用**本地确定性帧同步**，与联机模式的区别在于：

| 特性 | 单机模式 | 联机模式 |
|------|----------|----------|
| 帧驱动 | Unity Update() | 服务器帧推送 |
| 输入源 | 本地 InputManager | 网络接收 |
| 延迟 | 0ms | 网络延迟 |
| 确定性 | 本地验证 | 服务器权威 |

### 帧循环流程

```
Unity Update()
    ↓
[1] InputManager.Tick()
    - 收集键盘/鼠标输入
    - 转换为 Input 对象
    ↓
[2] Room.Tick(input)
    - 执行逻辑帧
    - 更新所有 System
    ↓
[3] World.Tick()
    - MovementSystem.Tick()
    - CombatSystem.Tick()
    - PhysicsSystem.Tick()
    - SkillSystem.Tick()
    ↓
[4] Stage.Tick()
    - 同步实体位置/状态
    - 更新动画状态
    - 触发特效/音效
    ↓
[5] Render()
    - Unity 渲染
```

### 代码示例

```csharp
// SinglePlayerGameMode.Update() - 实际的帧循环代码
public void Update(float deltaTime)
{
    if (!IsRunning) return;
    
    // 单机模式：模拟服务器的权威帧推进（关键！）
    // 联机模式下，AuthorityFrame 由服务器通过 FrameSyncData 更新
    // 单机模式下，让 AuthorityFrame 跟随 PredictionFrame
    if (MainRoom?.LSController != null && MainRoom.LSController.IsRunning)
    {
        MainRoom.LSController.AuthorityFrame = MainRoom.LSController.PredictionFrame;
    }
    
    // 1. 更新 Room（执行逻辑帧）
    MainRoom?.Update(deltaTime);
    //   ├─ LSController.Tick()
    //   │   ├─ InputManager 已设置输入到 LSController
    //   │   ├─ PredictionFrame++
    //   │   ├─ GetOneFrameMessages() → 获取输入
    //   │   └─ Room.FrameTick(inputs) → 执行逻辑
    //   │       ├─ World.Update() → 更新所有实体
    //   │       └─ SkillEffectManager.Update()
    
    // 2. 更新 Stage（同步到表现层）
    MainStage?.Update(deltaTime);
    //   └─ 更新所有 EntityView
}
```

---

## AI系统

> **状态**: 📝 规划中

### 设计目标

- 提供挑战性的AI对手
- 模拟真实玩家的行为
- 支持多种难度级别

### AI架构（规划）

```
┌────────────────────────────────┐
│        AIController            │
│  (AI控制器，类似玩家输入)        │
└────────────────────────────────┘
                │
        ┌───────┴───────┐
        ↓               ↓
┌──────────────┐  ┌──────────────┐
│ DecisionTree │  │  Behavior    │
│   (决策树)    │  │   (行为树)   │
└──────────────┘  └──────────────┘
```

### AI输入生成

AI替代真实玩家，生成 `Input` 对象：

```csharp
public class AIController
{
    public Input GenerateInput(Entity aiEntity, World world)
    {
        // 1. 感知环境
        var enemies = ScanNearbyEnemies(aiEntity, world);
        var obstacles = ScanObstacles(aiEntity, world);
        
        // 2. 决策
        var decision = DecideAction(enemies, obstacles);
        
        // 3. 生成输入
        return new Input
        {
            PlayerId = aiEntity.UniqueId,
            MoveX = decision.MoveDirection.x,
            MoveY = decision.MoveDirection.y,
            Attack = decision.ShouldAttack,
            Skill = decision.SkillIndex
        };
    }
}
```

### 难度级别

| 难度 | 反应时间 | 决策质量 | 技能使用 |
|------|----------|----------|----------|
| 简单 | 慢 (500ms) | 基础 | 少 |
| 普通 | 中等 (300ms) | 中等 | 中等 |
| 困难 | 快 (100ms) | 高级 | 频繁 |
| 地狱 | 即时 (0ms) | 完美 | 最优 |

---

## 存档系统

> **状态**: 📝 规划中

### 存档内容

#### 1. 游戏进度
- 已完成的关卡
- 当前关卡进度
- 解锁的内容

#### 2. 角色数据
- 角色等级和经验
- 角色属性
- 已学习的技能
- 装备和道具

#### 3. 配置设置
- 游戏难度
- 音频设置
- 操作设置

### 存档格式

**推荐**: JSON + MemoryPack 二进制

```csharp
[MemoryPackable]
public partial class SaveData
{
    public int Version { get; set; } = 1;
    public DateTime SaveTime { get; set; }
    
    // 进度数据
    public int CurrentLevel { get; set; }
    public List<int> CompletedLevels { get; set; }
    
    // 角色数据
    public PlayerData PlayerData { get; set; }
    
    // 配置
    public GameSettings Settings { get; set; }
}
```

### 存档位置

```
Windows: %AppData%/Astrum/Saves/
Mac: ~/Library/Application Support/Astrum/Saves/
Linux: ~/.config/Astrum/Saves/
```

### 自动保存

- 关卡完成时
- 重要事件后（Boss击败、关键道具获得）
- 退出游戏时

---

## 关卡系统

> **状态**: 📝 规划中

### 关卡结构

```
关卡 (Level)
  ├── 关卡配置 (LevelConfig)
  │   ├── 地图场景
  │   ├── 出生点
  │   ├── 敌人波次
  │   └── 胜利条件
  │
  ├── 敌人生成器 (EnemySpawner)
  │   ├── 波次管理
  │   ├── 生成位置
  │   └── AI配置
  │
  └── 关卡事件 (LevelEvents)
      ├── 开始事件
      ├── 进度事件
      └── 结束事件
```

### 关卡配置表

```csharp
// 配置表定义
public class LevelConfig
{
    public int LevelId;
    public string LevelName;
    public string SceneName;
    public List<SpawnPoint> PlayerSpawnPoints;
    public List<EnemyWave> EnemyWaves;
    public VictoryCondition WinCondition;
    public int RewardExp;
    public List<int> RewardItems;
}
```

### 关卡流程

```
[开始关卡]
    ↓
加载关卡配置
    ↓
初始化场景
    ↓
生成玩家角色
    ↓
[关卡进行中]
    ├─→ 生成敌人波次
    ├─→ 检测胜利/失败条件
    └─→ 更新UI显示
    ↓
[关卡结束]
    ↓
结算奖励
    ↓
保存进度
    ↓
返回主菜单/下一关卡
```

---

## 与联机模式的区别

### 架构对比

| 组件 | 单机模式 | 联机模式 |
|------|----------|----------|
| **Room** | 本地创建 | 服务器创建 |
| **帧同步** | 本地驱动 | 服务器推送 |
| **输入** | 直接应用 | 上报服务器 |
| **对手** | AI控制 | 其他玩家 |
| **延迟** | 0ms | 网络延迟 |
| **暂停** | ✅ 支持 | ❌ 不支持 |
| **存档** | 本地文件 | 服务器数据库 |
| **调试** | 简单 | 复杂 |

### 代码复用

**共享的逻辑层代码** (100%复用):
- ✅ Entity-Component-Capability 架构
- ✅ 战斗系统 (技能、伤害、buff)
- ✅ 物理系统 (碰撞检测)
- ✅ 动作系统 (动作状态机)
- ✅ 帧同步逻辑 (FrameSyncManager)

**不同的客户端代码**:
- ❌ 输入处理方式
  - 单机: `InputManager` → `Room`
  - 联机: `InputManager` → `NetworkManager` → `Server` → `Room`
  
- ❌ 帧驱动方式
  - 单机: `Unity Update()` 驱动
  - 联机: 服务器帧推送驱动

- ❌ 对手来源
  - 单机: `AIController` 生成输入
  - 联机: 其他玩家的网络输入

### 切换逻辑

通过 `GameConfig.IsSinglePlayerMode` 区分：

```csharp
// 在启动游戏时
if (GameConfig.Instance.IsSinglePlayerMode)
{
    // 单机模式
    GamePlayManager.Instance.StartSinglePlayerGame("GameScene");
}
else
{
    // 联机模式
    NetworkManager.Instance.Connect(serverAddress);
    // ... 后续登录、匹配、进入房间流程
}
```

---

## 技术限制和注意事项

### 关键实现细节（v0.3.0）

#### 1. LSController 的启动时机 ⚠️

**联机模式**:
```csharp
// 服务器通知帧同步开始时启动
OnFrameSyncStartNotification(notification)
{
    MainRoom.LSController.CreationTime = notification.startTime;  // 服务器时间
    MainRoom.LSController.Start();
}
```

**单机模式**:
```csharp
// 创建玩家时立即启动
CreatePlayer()
{
    MainRoom.LSController.CreationTime = TimeInfo.Instance.ServerNow();  // 本地时间
    MainRoom.LSController.Start();
}
```

#### 2. 权威帧的更新机制 ⚠️

**联机模式**:
```csharp
// 服务器通过 FrameSyncData 下发
OnFrameSyncData(frameData)
{
    MainRoom.LSController.AuthorityFrame = frameData.authorityFrame;
}
```

**单机模式**:
```csharp
// 每帧手动同步，模拟服务器推进
Update(deltaTime)
{
    MainRoom.LSController.AuthorityFrame = MainRoom.LSController.PredictionFrame;
}
```

**为什么需要这样做？**

`LSController.Tick()` 中有预测上限检查：
```csharp
if (PredictionFrame - AuthorityFrame > MaxPredictionFrames)
    return;  // 停止执行
```

- 联机模式：允许预测 5 帧（网络延迟补偿）
- 单机模式：差距应该是 0（无延迟，立即确认）

#### 3. 输入处理流程

**共享部分**（单机和联机相同）:
```csharp
InputManager.Update()
    ↓
CollectLSInput(playerId) → 收集键盘输入
    ↓
LSController.SetPlayerInput(playerId, input)
    ↓
LSController.Tick()
    ↓
Room.FrameTick(inputs)
```

**差异部分**:

**联机**: `FrameDataUploadEventData` → `MultiplayerGameMode` 订阅 → 发送到服务器
**单机**: `FrameDataUploadEventData` → 无人订阅 → 自动忽略 ✅

这是**事件驱动架构的优势**：逻辑层只负责发布事件，客户端层决定是否处理。

---

### 确定性要求

即使是单机模式，也必须保持逻辑的**确定性**，原因：

1. **回放系统** - 支持录像回放
2. **调试** - 可重现bug
3. **代码一致性** - 与联机模式共享代码

**关键原则**:
- ✅ 使用 TrueSync (FP) 而非 float
- ✅ 避免 Random.Range()，使用确定性随机
- ✅ 避免 Time.deltaTime，使用固定时间步

### 性能优化

单机模式无网络延迟，但仍需注意：

- 保持 60 FPS
- Entity 数量控制（建议 < 100）
- 物理查询优化
- GC 压力控制

### 调试建议

1. **Gizmos可视化** - 已实现碰撞盒可视化
2. **日志分级** - 使用 ASLogger 的不同级别
3. **帧回放** - 记录输入序列，支持重放
4. **性能分析** - Unity Profiler

---

## 架构改进（v0.3.0）

### GameMode 模式分离架构

**核心改进**:
- ✅ 单机和联机逻辑完全解耦
- ✅ GamePlayManager 职责清晰（只做委托）
- ✅ 延迟创建 GameMode（按需加载）
- ✅ 支持模式动态切换

**新文件结构**:
```
AstrumClient/Managers/
├── GamePlayManager.cs          (460 行，轻量级入口)
└── GameModes/
    ├── IGameMode.cs            (游戏模式接口)
    ├── SinglePlayerGameMode.cs (单机模式实现)
    └── MultiplayerGameMode.cs  (联机模式实现)
```

**详细设计**: 查看 [GamePlayManager 重构方案](../../07-Development%20开发指南/GamePlayManager-Refactoring%20重构方案.md)

---

## 下一步开发计划

### Phase 1: 基础架构 ✅
- ✅ GameConfig 单机模式标记
- ✅ SinglePlayerGameMode 完整实现
- ✅ Room/World 本地创建
- ✅ Stage 表现层同步
- ✅ LSController 本地驱动
- ✅ 权威帧模拟

### Phase 2: 核心玩法 ✅
- ✅ 玩家角色创建
- ✅ 移动和操作
- ✅ 战斗系统
- ✅ 技能效果
- ✅ 帧循环驱动
- ✅ 完整测试通过

### Phase 3-5: 暂不开发 📝
- 📝 AI系统 - 暂不开发
- 📝 关卡系统 - 暂不开发
- 📝 存档系统 - 暂不开发

**当前状态**: 单机模式核心功能已完成，可进行完整的游戏测试和玩法验证。

---

## 相关文档

- [单机模式开发进展](_status%20开发进展/Single-Player-Progress%20单机模式开发进展.md) - 详细开发记录
- [联机模式](Network-Multiplayer%20联机模式.md) - 对比参考
- [战斗系统](../02-CombatSystem%20战斗系统/) - 共享的战斗逻辑
- [ECC架构](../05-CoreArchitecture%20核心架构/ECC-System%20ECC结构说明.md) - 逻辑层架构
- [帧同步]() - 帧同步机制（待补充）

---

**最后更新**: 2025-10-11  
**维护者**: 开发团队  
**版本**: v0.3.0

---

## 更新日志

### v0.3.0 (2025-10-11)
- ✅ 架构重构：GameMode 模式分离
- ✅ 修复 LSController 启动问题
- ✅ 修复权威帧驱动问题
- ✅ 完整测试通过

### v0.2.0 (2025-10-10)
- ✅ 战斗系统完整集成
- ✅ 物理系统完善

### v0.1.0
- ✅ 基础架构搭建



