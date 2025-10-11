# 🎯 单机模式设计文档

**版本**: v0.1.0  
**状态**: 🚧 开发中  
**最后更新**: 2025-10-10

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
- 游戏生命周期管理
- Room 和 Stage 的创建和销毁
- 单机/联机模式的统一入口

**关键接口**:
```csharp
public void StartSinglePlayerGame(string gameSceneName);
public Stage MainStage { get; }
public Room MainRoom { get; }
```

#### 3. GameLauncher
**路径**: `AstrumClient/Core/GameLauncher.cs`

**职责**:
- 单机游戏的启动逻辑
- Room、World、Stage 的初始化
- 玩家角色的创建

**关键接口**:
```csharp
public void StartSinglePlayerGame(string gameSceneName);
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
玩家点击"开始游戏"
        ↓
[1] GameConfig.SetSinglePlayerMode(true)
        ↓
[2] GamePlayManager.StartSinglePlayerGame("GameScene")
        ↓
[3] 创建 Room(id=1, "SinglePlayerRoom")
        ↓
[4] 创建 World 并关联到 Room
        ↓
[5] Room.Initialize()
    - 初始化 FrameSyncManager（本地模式）
    - 注册系统（移动、战斗、物理等）
        ↓
[6] 创建 Stage("GameStage")
        ↓
[7] Stage.Initialize()
    - 初始化 ViewWorld
    - 注册 ViewComponent 类型
        ↓
[8] 切换到游戏场景 (LoadSceneAsync)
        ↓
[9] 场景加载完成回调
    - Stage.SetActive(true)
    - Stage.OnEnter()
        ↓
[10] 创建玩家实体
    - Room.AddPlayer(playerId)
    - 加载角色配置
    - 创建 Entity + Components
    - 创建对应的 EntityView
        ↓
[11] 开始本地帧循环
    - Unity Update() 驱动
    - 每帧收集本地输入
    - 执行帧逻辑
    - 同步到表现层
        ↓
游戏运行中...
```

### 详细步骤

#### Step 1-2: 模式设置和启动入口
```csharp
// 在主菜单或游戏启动时
GameConfig.Instance.SetSinglePlayerMode(true);
GamePlayManager.Instance.StartSinglePlayerGame("GameScene");
```

#### Step 3-5: 创建逻辑层环境
```csharp
// GamePlayManager.StartSinglePlayerGame()
var room = new Room(1, "SinglePlayerRoom");
var world = new World();
room.MainWorld = world;
room.Initialize(); // 初始化帧同步和系统
```

**关键点**:
- Room 不连接服务器，使用本地帧同步
- FrameSyncManager 设置为本地模式（无网络延迟）
- 所有输入直接在本地处理

#### Step 6-8: 创建表现层和场景切换
```csharp
Stage gameStage = new Stage("GameStage", "游戏场景");
gameStage.Initialize();
gameStage.SetRoom(room);

SceneManager.LoadSceneAsync("GameScene", () => {
    gameStage.SetActive(true);
    gameStage.OnEnter();
});
```

#### Step 9-10: 创建玩家角色
```csharp
// 场景加载完成后
long playerId = room.AddPlayer();
Vector3 spawnPosition = new Vector3(-5f, 0.5f, 0f);

// 从配置表加载角色数据
var roleConfig = ConfigManager.Instance.GetRoleConfig(1001); // 骑士
var entity = EntityFactory.CreateRoleEntity(world, roleConfig, spawnPosition);

// 创建对应的表现层视图
var entityView = gameStage.CreateEntityView(entity);
```

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
// 简化的帧循环伪代码
public class GameLoop : MonoBehaviour
{
    private Room _room;
    private Stage _stage;
    
    void Update()
    {
        if (_room == null) return;
        
        // 1. 收集输入
        var input = InputManager.Instance.GetCurrentInput();
        
        // 2. 执行逻辑帧
        _room.Tick(input);
        
        // 3. 同步到表现层
        _stage.Tick();
    }
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

## 下一步开发计划

### Phase 1: 基础架构 ✅
- ✅ GameConfig 单机模式标记
- ✅ GameLauncher 启动流程
- ✅ Room/World 本地创建
- ✅ Stage 表现层同步

### Phase 2: 核心玩法 ✅
- ✅ 玩家角色创建
- ✅ 移动和操作
- ✅ 战斗系统
- ✅ 技能效果

### Phase 3: AI系统 🚧
- 🚧 AIController 框架
- 📝 基础AI决策树
- 📝 AI行为库
- 📝 难度分级

### Phase 4: 关卡系统 📝
- 📝 关卡配置表设计
- 📝 关卡加载器
- 📝 敌人生成器
- 📝 关卡事件系统

### Phase 5: 存档系统 📝
- 📝 存档数据结构
- 📝 序列化/反序列化
- 📝 自动保存机制
- 📝 存档管理UI

---

## 相关文档

- [单机模式开发进展](_status%20开发进展/Single-Player-Progress%20单机模式开发进展.md) - 详细开发记录
- [联机模式](Network-Multiplayer%20联机模式.md) - 对比参考
- [战斗系统](../02-CombatSystem%20战斗系统/) - 共享的战斗逻辑
- [ECC架构](../05-CoreArchitecture%20核心架构/ECC-System%20ECC结构说明.md) - 逻辑层架构
- [帧同步]() - 帧同步机制（待补充）

---

**最后更新**: 2025-10-10  
**维护者**: 开发团队



