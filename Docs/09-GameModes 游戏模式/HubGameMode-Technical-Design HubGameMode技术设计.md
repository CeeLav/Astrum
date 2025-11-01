# HubGameMode 技术设计方案

**版本**: v0.1.0  
**状态**: 📝 设计阶段  
**创建日期**: 2025-01-15

---

## 目录

1. [概述](#概述)
2. [设计目标](#设计目标)
3. [核心功能](#核心功能)
4. [架构设计](#架构设计)
5. [详细设计](#详细设计)
6. [IGameMode 接口重构](#igamemode-接口重构)
7. [与现有模式的集成](#与现有模式的集成)
8. [UI界面设计](#ui界面设计)
9. [状态管理](#状态管理)
10. [开发计划](#开发计划)
11. [技术细节](#技术细节)

---

## 概述

### 背景

根据游戏设计，**Hub（基地）** 是玩家的主世界基地，承担以下核心功能：
- **探索入口**：进入搜打撤副本探索
- **角色养成**：角色升级、属性提升
- **塔防场景**：未来将支持基地塔防战斗（v0.3计划）
- **资源管理**：星能碎片、材料等资源的查看和管理

当前游戏流程存在断点：玩家完成单机战斗后没有返回到基地，无法形成完整的游戏循环。

### 定位

`HubGameMode` 是玩家在主世界的管理模式，与 `SinglePlayerGameMode`（探索副本）和 `MultiplayerGameMode`（联机对战）平级，形成以下游戏模式架构：

```
LoginGameMode (登录/匹配)
    ↓
HubGameMode (基地管理)
    ├─→ SinglePlayerGameMode (单机探索)
    └─→ MultiplayerGameMode (联机对战)
```

### 设计原则

1. **简化先行**：初期使用空场景 + UI界面实现核心功能
2. **完整循环**：形成 Hub → 探索 → 结算 → Hub 的完整循环
3. **易于扩展**：为后续塔防、建造等功能预留接口
4. **统一架构**：遵循现有 GameMode 架构和事件体系

---

## 设计目标

### 短期目标（v0.1）

1. ✅ **探索入口**：从 Hub 启动单机探索
2. ✅ **返回机制**：探索完成后返回 Hub
3. ✅ **基础UI**：Hub 主界面和基本交互
4. ✅ **数据持久化**：玩家进度和养成数据保存

### 中期目标（v0.2-v0.3）

1. 📝 **角色养成**：属性升级、技能解锁
2. 📝 **资源展示**：星能碎片、材料统计
3. 📝 **塔防入口**：进入夜潮防守场景

### 长期目标（v0.4+）

1. 📝 **建造系统**：基地设施建设
2. 📝 **昼夜循环**：基地内时间管理
3. 📝 **多人协作**：多人基地共同建设

---

## 核心功能

### 1. 探索入口

玩家从 Hub 点击"开始探索"进入单机副本：
- 切换到 SinglePlayerGameMode
- 加载 DungeonsGame 场景
- 开始战斗循环

### 2. 返回机制

探索完成后：
- ~~显示结算界面（暂不实现）~~
- ~~应用养成数据（暂不实现）~~
- 直接返回 Hub 界面

**注意**：结算功能将在后续版本中实现。

### 3. 角色养成（预留）

初期暂不实现具体养成功能，仅预留接口：
- ~~HP 升级（暂不实现）~~
- ~~ATK 升级（暂不实现）~~

### 4. 资源展示

显示玩家当前拥有的资源：
- 星能碎片（主要货币）
- 其他材料（预留接口）

### 5. 游戏循环

完整流程（简化版）：
```
1. HubGameMode 初始化
2. 加载 HubScene 场景
3. 显示 Hub UI
4. 用户点击"开始探索"
5. 切换到 SinglePlayerGameMode，启动战斗
6. 战斗完成，直接返回 HubGameMode
7. 循环回到步骤 2
```

**注意**：结算流程（步骤 5.5）暂不实现。

---

## 架构设计

### 类图

```
┌─────────────────────────────────────────┐
│              GameDirector                │
│  - CurrentGameMode: IGameMode           │
│  + SwitchGameMode(IGameMode)            │
└────────────────────┬────────────────────┘
                     │
                     │ manages
                     ▼
┌─────────────────────────────────────────┐
│           BaseGameMode                   │
│  - _currentState: GameModeState         │
│  + ChangeState(GameModeState)           │
│  + OnGameEvent(EventData)               │
└────────────────────┬────────────────────┘
                     │
                     │ inherits
                     ▼
┌─────────────────────────────────────────┐
│          HubGameMode                     │
│  - MainRoom: Room? (null)               │
│  - MainStage: Stage? (null)             │
│  - PlayerId: long                       │
│  - ModeName: "Hub"                      │
│                                          │
│  + Initialize()                         │
│  + StartGame(string)                    │
│  + Update(float)                        │
│  + Shutdown()                           │
│                                          │
│  + StartExploration()                   │
│  - ShowHubUI()                          │
│  - HideHubUI()                          │
│  - LoadPlayerData()                     │
│  - SavePlayerData()                     │
└────────────────────┬────────────────────┘
                     │
                     │ notifies
                     ▼
┌─────────────────────────────────────────┐
│             HubView                      │
│  - startExplorationButton: Button       │
│  - characterStatsPanel: Panel           │
│  - resourcesPanel: Panel                │
│                                          │
│  + UpdatePlayerStats()                  │
│  + UpdateResources()                    │
│  - OnStartExplorationClicked()          │
└─────────────────────────────────────────┘
```

### 模式切换流程

```
HubGameMode (Ready)
    ↓ OnStartExplorationClicked()
    ├─→ HideHubUI()
    ├─→ SavePlayerData()
    └─→ SwitchGameMode(SinglePlayerGameMode)
            ↓
        SinglePlayerGameMode (Playing)
            ↓ OnGameComplete()
            ├─→ ShowSettlementUI()
            ├─→ ApplyRewards()
            └─→ SwitchGameMode(HubGameMode)
                    ↓
                HubGameMode (Ready)
```

### 数据流

```
PlayerDataManager (全局单例)
    ↓ LoadProgressData()
PlayerProgressData (本地存储)
    ├─→ Level: int
    ├─→ Exp: int
    ├─→ RoleId: int
    ├─→ StarFragments: int
    ├─→ AllocatedXxxPoints: int
    └─→ AvailableStatPoints: int

GameMode 创建 Entity 时
    ↓ 读取 PlayerProgressData
    ↓ 应用到 LevelComponent, GrowthComponent
    ↓ 计算 DerivedStats
    ↓ Entity 拥有正确的属性
```

---

## 详细设计

### 数据分层讨论

根据数值系统设计，玩家数据分为三层：

1. **基础数据（需持久化）**：来自 `LevelComponent`、`GrowthComponent` 等
   - 当前等级 `CurrentLevel`
   - 当前经验 `CurrentExp`
   - 属性点分配 `AllocatedXxxPoints`
   - 角色 ID `RoleId`

2. **派生数据（计算得出）**：来自 `DerivedStatsComponent`
   - 最大生命值 `MaxHP`
   - 攻击力 `ATK`
   - 这些数据由配置表、等级、加点计算得出，**不需要保存**

3. **动态数据（实时变化）**：来自 `DynamicStatsComponent`
   - 当前生命值 `CurrentHP`
   - 当前法力值 `CurrentMana`
   - 战斗中的实时状态，**不需要保存**

**设计决策**：
- 方案 A：GameMode 内保存/加载
- 方案 B：独立的 PlayerDataManager 统一管理
- **推荐**：方案 B，因为玩家数据会在多个 GameMode 间共享，统一管理更清晰

### PlayerData 数据结构（方案 B）

```csharp
/// <summary>
/// 玩家进度数据（持久化）
/// </summary>
[MemoryPackable]
public partial class PlayerProgressData
{
    /// <summary>当前等级</summary>
    public int Level { get; set; } = 1;
    
    /// <summary>当前经验值</summary>
    public int Exp { get; set; } = 0;
    
    /// <summary>角色ID</summary>
    public int RoleId { get; set; } = 1001;
    
    /// <summary>可分配的属性点</summary>
    public int AvailableStatPoints { get; set; } = 0;
    
    /// <summary>已分配的攻击点</summary>
    public int AllocatedAttackPoints { get; set; } = 0;
    
    /// <summary>已分配的防御点</summary>
    public int AllocatedDefensePoints { get; set; } = 0;
    
    /// <summary>已分配的生命点</summary>
    public int AllocatedHealthPoints { get; set; } = 0;
    
    /// <summary>已分配的速度点</summary>
    public int AllocatedSpeedPoints { get; set; } = 0;
    
    /// <summary>星能碎片（资源）</summary>
    public int StarFragments { get; set; } = 0;
    
    /// <summary>其他资源（可扩展）</summary>
    public Dictionary<string, int> Resources { get; set; } = new();
    
    // 注意：ExpToNextLevel 不需要保存，由配置表计算得出
}
```

### HubGameMode 核心实现（简化版）

```csharp
/// <summary>
/// Hub 游戏模式 - 主世界基地管理
/// </summary>
public class HubGameMode : BaseGameMode
{
    // 继承属性
    public override Room MainRoom { get; set; }        // Hub 不使用 Room
    public override Stage MainStage { get; set; }      // Hub 不使用 Stage
    public override long PlayerId { get; set; }
    public override string ModeName => "Hub";
    public override bool IsRunning { get; set; }
    
    // Hub 特定场景名称
    private const string HubSceneName = "HubScene";
    
    // 生命周期方法
    public override void Initialize()
    {
        ASLogger.Instance.Info("HubGameMode: 初始化 Hub 模式");
        ChangeState(GameModeState.Initializing);
        
        // 订阅事件
        SubscribeToEvents();
        
        // Hub 模式不需要提前加载数据
        // 数据由 PlayerDataManager 统一管理
        
        ChangeState(GameModeState.Ready);
        IsRunning = true;
        
        ASLogger.Instance.Info("HubGameMode: 初始化完成");
    }
    
    public override void Update(float deltaTime)
    {
        // Hub 模式主要是 UI 界面，无需每帧更新
        // 未来添加昼夜循环等功能时再实现
    }
    
    public override void Shutdown()
    {
        ASLogger.Instance.Info("HubGameMode: 关闭 Hub 模式");
        ChangeState(GameModeState.Ending);
        
        // 取消订阅事件
        UnsubscribeFromEvents();
        
        IsRunning = false;
        
        ChangeState(GameModeState.Finished);
    }
    
    private void SubscribeToEvents()
    {
        EventSystem.Instance.Subscribe<StartExplorationRequestEventData>(OnStartExplorationRequested);
    }
    
    private void UnsubscribeFromEvents()
    {
        EventSystem.Instance.Unsubscribe<StartExplorationRequestEventData>(OnStartExplorationRequested);
    }
    
    private void OnStartExplorationRequested(StartExplorationRequestEventData eventData)
    {
        ASLogger.Instance.Info("HubGameMode: 收到开始探索请求");
        StartExploration();
    }
    
    // Hub 特定方法
    private void StartExploration()
    {
        ASLogger.Instance.Info("HubGameMode: 启动探索");
        
        try
        {
            // 切换到 SinglePlayerGameMode
            GameDirector.Instance.SwitchGameMode(GameModeType.SinglePlayer);
            
            // StartGame 会在后续调用
            GameDirector.Instance.StartGame("DungeonsGame");
            
            ASLogger.Instance.Info("HubGameMode: 探索启动成功");
        }
        catch (Exception ex)
        {
            ASLogger.Instance.Error($"HubGameMode: 启动探索失败 - {ex.Message}");
        }
    }
}
```

### PlayerDataManager 设计（推荐）

```csharp
/// <summary>
/// 玩家数据管理器 - 统一管理玩家进度数据
/// </summary>
public class PlayerDataManager : Singleton<PlayerDataManager>
{
    private PlayerProgressData _progressData;
    
    /// <summary>当前玩家进度数据</summary>
    public PlayerProgressData ProgressData => _progressData;
    
    public void Initialize()
    {
        LoadProgressData();
    }
    
    public void SaveProgressData()
    {
        if (_progressData == null)
        {
            ASLogger.Instance.Warning("PlayerDataManager: 进度数据为空，无法保存");
            return;
        }
        
        SaveSystem.SavePlayerProgressData(_progressData);
    }
    
    public void LoadProgressData()
    {
        _progressData = SaveSystem.LoadPlayerProgressData();
        if (_progressData == null)
        {
            _progressData = CreateDefaultProgressData();
        }
    }
    
    private PlayerProgressData CreateDefaultProgressData()
    {
        return new PlayerProgressData
        {
            Level = 1,
            Exp = 0,
            RoleId = 1001, // 默认角色ID
            AvailableStatPoints = 0,
            StarFragments = 0
        };
    }
}
```

### 事件定义

```csharp
/// <summary>
/// 开始探索请求事件
/// </summary>
public class StartExplorationRequestEventData : EventData
{
    // 暂无额外数据
}

/// <summary>
/// 玩家数据变化事件
/// </summary>
public class PlayerDataChangedEventData : EventData
{
    public PlayerProgressData ProgressData { get; set; }
}
```

---

## UI界面设计

### HubView 界面元素（简化版）

```
HubView
├── Header (顶部)
│   ├── PlayerNameText
│   └── LevelText
│
├── MainPanel (主面板)
│   ├── StartExplorationButton (开始探索) ⭐ 核心入口
│   └── ViewResourcesButton (查看资源) - 可选
│
└── ResourcesPanel (资源面板) - 可折叠
    └── StarFragments: Count
```

### UI 交互流程

1. **进入 Hub**：HubGameMode 显示 HubView UI
2. **开始探索**：点击"开始探索"按钮 → 切换到 SinglePlayerGameMode
3. **查看资源**：点击资源按钮显示当前拥有的星能碎片等
4. **返回 Hub**：探索完成后自动返回 Hub（暂无结算UI）

### UI 与 GameMode 解耦

**设计原则**：
- GameMode 不持有 HubView 的引用
- HubView 通过事件监听 GameMode 状态变化
- HubView 通过 PlayerDataManager 获取数据

```csharp
// HubView 示例
public partial class HubView : UIRefs
{
    // GameMode 显示 UI 后自动创建
    // 不需要 GameMode 持有引用
    private void OnInitialize()
    {
        // 监听玩家数据变化事件
        EventSystem.Instance.Subscribe<PlayerDataChangedEventData>(OnPlayerDataChanged);
        
        // 监听开始探索点击
        startExplorationButton.onClick.AddListener(OnStartExplorationClicked);
        
        // 初始化时显示数据
        RefreshUI();
    }
    
    private void RefreshUI()
    {
        var progressData = PlayerDataManager.Instance.ProgressData;
        levelText.text = $"等级: {progressData.Level}";
        starFragmentsText.text = $"星能碎片: {progressData.StarFragments}";
    }
    
    private void OnStartExplorationClicked()
    {
        // 直接通知 GameDirector，不通过引用
        EventSystem.Instance.Publish(new StartExplorationRequestEventData());
    }
}
```

---

## 状态管理

### GameModeState 使用

Hub 模式使用的状态：
- `Initializing`：初始化中
- `Ready`：准备就绪，等待用户操作
- `Ending`：退出中（清理资源）
- `Finished`：已结束

**注意**：Hub 模式不需要内部状态，简单清晰即可。

---

## IGameMode 接口重构

### 接口合并提案

**当前问题**：
- `Initialize()` 和 `StartGame(sceneName)` 职责不够清晰
- 场景切换逻辑分散

**重构方案**：
将 `Initialize()` 和 `StartGame()` 合并为类似状态机的接口：

```csharp
public interface IGameMode
{
    // 原有接口保持不变
    void Initialize();
    
    /// <summary>
    /// 模式进入时调用（替代 StartGame）
    /// 当 GameDirector 切换模式后自动调用
    /// </summary>
    /// <param name="sceneName">场景名称（可选）</param>
    void OnModeEnter(string sceneName = null);
    
    void Update(float deltaTime);
    void Shutdown();
    
    // ... 其他接口
}
```

**实现示例**：

```csharp
// HubGameMode
public override void Initialize()
{
    ASLogger.Instance.Info("HubGameMode: 初始化");
    ChangeState(GameModeState.Initializing);
    // 基础初始化
    ChangeState(GameModeState.Ready);
}

public override void OnModeEnter(string sceneName = null)
{
    ASLogger.Instance.Info($"HubGameMode: 进入模式，场景: {sceneName ?? "HubScene"}");
    
    // 加载 Hub 场景
    if (sceneName != null)
    {
        SceneManager.Instance?.LoadSceneAsync(sceneName, OnHubSceneLoaded);
    }
    
    // 显示 Hub UI
    UIManager.Instance?.ShowUI("Hub");
}

private void OnHubSceneLoaded()
{
    ASLogger.Instance.Info("HubGameMode: Hub 场景加载完成");
}
```

**注意**：接口重构影响所有 GameMode，需要同时修改 LoginGameMode、SinglePlayerGameMode、MultiplayerGameMode。建议作为独立的重构任务。

---

## 与现有模式的集成

### 启动流程（接口重构前）

```
GameDirector.Initialize()
    ↓
GameDirector.SwitchGameMode(GameModeType.Login)
    ↓
LoginGameMode.Initialize()
    ↓ (用户选择单机游戏)
GameDirector.SwitchGameMode(GameModeType.Hub)
    ↓
HubGameMode.Initialize() → 切换到 Hub 场景
    ↓ (用户点击开始探索)
HubGameMode.StartExploration() 
    ↓
GameDirector.SwitchGameMode(GameModeType.SinglePlayer)
    ↓
SinglePlayerGameMode.Initialize()
    ↓
GameDirector.StartGame("DungeonsGame") → 切换到战斗场景
    ↓ (探索完成)
GameDirector.SwitchGameMode(GameModeType.Hub)
    ↓
回到 Hub
```

### GameModeType 扩展

需要在 `GameDirector` 中添加 Hub 模式：

```csharp
public enum GameModeType
{
    Login,
    Hub,            // 新增
    SinglePlayer,
    Multiplayer
}

public void SwitchGameMode(GameModeType gameModeType)
{
    IGameMode newGameMode = gameModeType switch
    {
        GameModeType.Login => new LoginGameMode(),
        GameModeType.Hub => new HubGameMode(),          // 新增
        GameModeType.SinglePlayer => new SinglePlayerGameMode(),
        GameModeType.Multiplayer => new MultiplayerGameMode(),
        _ => throw new System.ArgumentException($"Unknown GameMode type: {gameModeType}")
    };
    
    SwitchGameMode(newGameMode);
}
```

### 与 SinglePlayerGameMode 的交互

**当前版本**：探索完成后不需要复杂的结算逻辑，直接返回 Hub。

**未来版本**：探索结算功能将在后续开发中实现。

```csharp
// 在 SinglePlayerGameMode 中（未来版本）
private void OnGameEnd()
{
    // TODO: 结算逻辑
    // 1. 收集结算数据
    // 2. 应用奖励
    // 3. 保存进度
    
    // 切换回 Hub 模式
    GameDirector.Instance.SwitchGameMode(GameModeType.Hub);
}
```

---

## 开发计划

### Phase 1: 基础架构（3-4天）

- [ ] 创建 `HubGameMode.cs`
- [ ] 创建 `HubView.cs` 和预制体
- [ ] 创建 `PlayerProgressData.cs` 数据结构
- [ ] 创建 `PlayerDataManager.cs`（独立模块）
- [ ] 扩展 `GameModeType` 枚举
- [ ] 扩展 `SaveSystem` 支持 PlayerProgressData 持久化

### Phase 2: 场景和UI（2-3天）

- [ ] 创建 HubScene 空场景
- [ ] 设计并实现 HubView UI 预制体
- [ ] 实现开始探索按钮
- [ ] 实现资源显示（星能碎片）

### Phase 3: 集成与测试（2-3天）

- [ ] 集成到 GameDirector
- [ ] 集成 PlayerDataManager
- [ ] 修复 SinglePlayerGameMode 的返回逻辑
- [ ] 测试 Hub ↔ 探索 循环
- [ ] 测试数据持久化

### Phase 4: 场景切换优化（1-2天）

- [ ] 完善场景切换逻辑
- [ ] 添加加载进度提示
- [ ] 优化切换动画
- [ ] 性能优化

---

## 技术细节

### 持久化方案

`SaveSystem` 目前还未实现，但可以参考现有序列化模式：

```csharp
public static class SaveSystem
{
    private static string PlayerProgressDataPath => 
        Path.Combine(Application.persistentDataPath, "PlayerProgressData.dat");
    
    public static PlayerProgressData LoadPlayerProgressData()
    {
        if (!File.Exists(PlayerProgressDataPath))
            return null;
        
        var bytes = File.ReadAllBytes(PlayerProgressDataPath);
        return MemoryPackSerializer.Deserialize<PlayerProgressData>(bytes);
    }
    
    public static void SavePlayerProgressData(PlayerProgressData data)
    {
        var bytes = MemoryPackSerializer.Serialize(data);
        File.WriteAllBytes(PlayerProgressDataPath, bytes);
    }
}
```

**说明**：
- 使用 MemoryPack 序列化
- 文件存储在 `Application.persistentDataPath`
- 跨平台兼容（Unity 自动处理路径差异）

### 场景切换

HubGameMode 需要场景切换支持：

```csharp
public override void OnModeEnter(string sceneName = null)
{
    // 如果指定了场景名，加载该场景
    if (!string.IsNullOrEmpty(sceneName))
    {
        SceneManager.Instance?.LoadSceneAsync(sceneName, () => {
            OnHubSceneLoaded();
        });
    }
    else
    {
        // 默认加载 HubScene
        SceneManager.Instance?.LoadSceneAsync("HubScene", () => {
            OnHubSceneLoaded();
        });
    }
    
    // 显示 Hub UI
    UIManager.Instance?.ShowUI("Hub");
}

private void OnHubSceneLoaded()
{
    ASLogger.Instance.Info("HubGameMode: Hub 场景加载完成");
    ChangeState(GameModeState.Ready);
}
```

---

## 已知限制与风险

### 技术限制

1. **SaveSystem 未实现**：需要先实现 SaveSystem 基础功能
2. **接口重构待定**：`OnModeEnter` 接口重构影响所有 GameMode，需要统一规划
3. **场景切换**：HubScene 需要创建，确保场景切换流程正确

### 设计限制

1. **简化版**：初期功能非常简化，仅实现核心循环
2. **无结算功能**：探索结算功能暂不实现
3. **UI 体验**：初期 UI 可能较为简陋，需要持续迭代

### 风险评估

1. **接口变化**：接口重构可能影响现有 LoginGameMode 和 SinglePlayerGameMode
2. **数据管理**：PlayerDataManager 需要与现有系统整合
3. **性能影响**：频繁的场景切换和 UI 显示/隐藏

---

## 后续扩展方向

1. **角色养成系统**：
   - 技能系统
   - 装备系统
   - 天赋系统

2. **基地建设**：
   - 设施建造
   - 布局管理
   - 自动化系统

3. **塔防系统**：
   - 夜潮防守
   - 塔防关卡
   - 防守奖励

4. **多人协作**：
   - 多人基地
   - 协作建设
   - 资源共享

---

## 参考文档

- [GameMode 系统扩展计划](归档/GameMode-Extension-Plan%20GameMode扩展计划.md)
- [LoginGameMode 技术设计](LoginGameMode-Technical-Design%20LoginGameMode技术设计.md)
- [单机模式设计](Single-Player%20单机模式.md)
- [单机闭环 v0.1 计划](../../项目管理/SinglePlayer-Loop/SinglePlayer-Loop-v0.1.md)
- [数值系统设计](../02-CombatSystem%20战斗系统/数值系统/Stats-System%20数值系统.md)

---

## 核心设计决策总结

### 1. 数据管理方式

**决策**：使用独立的 PlayerDataManager 统一管理玩家进度数据

**理由**：
- 玩家数据在多个 GameMode 间共享（Hub、探索、塔防）
- 避免重复的保存/加载逻辑
- 数据一致性更容易保证

### 2. 持久化数据范围

**决策**：只持久化基础进度数据，不保存派生属性

**理由**：
- 派生属性（MaxHP、ATK）可由配置表 + 等级 + 加点计算
- 动态属性（CurrentHP、CurrentMana）仅存在于战斗中
- 减少存储空间，避免数据不一致

### 3. UI 与 GameMode 解耦

**决策**：GameMode 不持有 HubView 引用，通过事件通信

**理由**：
- 符合单一职责原则
- UI 可以独立测试
- 减少循环引用

### 4. 接口简化

**决策**：HubGameMode 不实现内部状态，简化流程

**理由**：
- 初期功能简单，不需要复杂状态机
- 保持代码清晰易维护
- 未来需要时再扩展

### 5. 结算功能暂缓

**决策**：探索结算功能不在 v0.1 实现

**理由**：
- 先实现完整循环比结算细节更重要
- 避免过度设计
- 后续版本逐步完善

---

**维护者**: 开发团队  
**最后更新**: 2025-01-15
