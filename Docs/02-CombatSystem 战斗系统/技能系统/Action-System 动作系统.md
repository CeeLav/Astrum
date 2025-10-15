# 动作系统策划案

## 1. 系统概述

动作系统是Astrum项目中负责角色动作管理的核心系统，负责管理每个角色的当前动作和动作切换。该系统基于帧同步架构，每一帧都是一个重要的状态，支持动作取消、连招、攻击判定等动作游戏核心功能。

**设计理念**：
- 以"动作帧"为基本单位，而非"动作"为单位
- 基于CancelTag/BeCancelledTag的动作切换机制
- 动作与动画分离，动作逻辑主导，动画作为表现层
- 支持复杂的动作派生和条件触发
- 与Unity Animator集成，但保持逻辑控制权

## 2. 核心概念

### 2.1 动作与动画的区别
- **动作(Action)**：游戏逻辑层面的概念，包含碰撞盒、攻击判定、移动等逻辑信息
- **动画(Animation)**：表现层面的概念，仅负责视觉表现，依赖于动作逻辑

### 2.2 动作帧概念
- 每个动作由若干帧组成
- 每一帧都有独立的攻击盒、受击盒、位移、推力等属性
- 动作游戏与回合制游戏的根本区别在于对帧的重视程度

### 2.3 Cancel系统
- **CancelTag**：新动作可以取消其他动作的依据
- **BeCancelledTag**：当前动作可以被其他动作取消的依据
- **TempBeCancelledTag**：临时开启的取消点（如攻击命中后）

## 3. 数据结构设计

### 3.1 动作信息 (ActionInfo)

```csharp
/// <summary>
/// 动作信息 - 动作系统的核心数据结构（运行时数据）
/// 由表格数据和运行时数据共同组装而成
/// </summary>
[MemoryPackable]
public partial class ActionInfo
{
    /// <summary>动作唯一标识符</summary>
    public int Id { get; set; }
    
    /// <summary>动作类型标签（如"受伤动作"、"攻击动作"）</summary>
    public string Catalog { get; set; }
    
    /// <summary>取消信息 - 此动作可以取消其他动作的依据</summary>
    public List<CancelTag> CancelTags { get; set; } = new();
    
    /// <summary>被取消信息 - 此动作可以被其他动作取消的依据</summary>
    public List<BeCancelledTag> BeCancelledTags { get; set; } = new();
    
    /// <summary>临时被取消信息 - 动作过程中临时开启的取消点</summary>
    public List<TempBeCancelledTag> TempBeCancelledTags { get; set; } = new();
    
    /// <summary>动作命令 - 触发此动作的输入信息</summary>
    public List<ActionCommand> Commands { get; set; } = new();
    
    
    /// <summary>自然下一个动作ID</summary>
    public int AutoNextActionId { get; set; }
    
    /// <summary>切换到此动作时是否保持播放动画</summary>
    public bool KeepPlayingAnim { get; set; }
    
    /// <summary>是否自动终止动作</summary>
    public bool AutoTerminate { get; set; }
    
    /// <summary>攻击信息列表 - 详见[战斗系统策划案](../战斗系统策划案.md)</summary>
    public List<AttackInfo> Attacks { get; set; } = new();
    
    /// <summary>攻击阶段信息 - 详见[战斗系统策划案](../战斗系统策划案.md)</summary>
    public List<AttackBoxTurnOnInfo> AttackPhase { get; set; } = new();
    
    /// <summary>受击阶段信息 - 详见[战斗系统策划案](../战斗系统策划案.md)</summary>
    public List<BeHitBoxTurnOnInfo> DefensePhase { get; set; } = new();
    
    
    /// <summary>基础优先级</summary>
    public int Priority { get; set; }
}
```

### 3.2 取消标签 (CancelTag)

```csharp
/// <summary>
/// 取消标签 - 新动作可以取消其他动作的依据
/// </summary>
[MemoryPackable]
public partial class CancelTag
{
    /// <summary>取消标签名称</summary>
    public string Tag { get; set; }
    
    /// <summary>起始帧（百分比 0.0-1.0）</summary>
    public float StartFrom { get; set; }
    
    /// <summary>融合时间</summary>
    public float BlendIn { get; set; }
    
    /// <summary>优先级变化</summary>
    public int Priority { get; set; }
}
```

### 3.3 被取消标签 (BeCancelledTag)

```csharp
/// <summary>
/// 被取消标签 - 当前动作可以被其他动作取消的依据
/// </summary>
[MemoryPackable]
public partial class BeCancelledTag
{
    /// <summary>被取消标签名称列表</summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>生效范围（时间百分比）</summary>
    public Vector2 Range { get; set; }
    
    /// <summary>融合时间</summary>
    public float BlendOut { get; set; }
    
    /// <summary>优先级变化</summary>
    public int Priority { get; set; }
}
```

### 3.4 临时被取消标签 (TempBeCancelledTag)

```csharp
/// <summary>
/// 临时被取消标签 - 动作过程中临时开启的取消点
/// </summary>
[MemoryPackable]
public partial class TempBeCancelledTag
{
    /// <summary>临时标签ID</summary>
    public string Id { get; set; }
    
    /// <summary>被取消标签名称列表</summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>持续时间（秒）</summary>
    public float Time { get; set; }
    
    /// <summary>融合时间</summary>
    public float BlendOut { get; set; }
    
    /// <summary>优先级变化</summary>
    public int Priority { get; set; }
}
```

### 3.5 动作命令 (ActionCommand)

```csharp
/// <summary>
/// 动作命令 - 触发动作的输入信息
/// </summary>
[MemoryPackable]
public partial class ActionCommand
{
    /// <summary>按键序列</summary>
    public List<KeyMap> KeySequence { get; set; } = new();
    
    /// <summary>有效帧数</summary>
    public int ValidFrames { get; set; }
}
```


> **注意**：攻击信息、攻击盒、受击盒等战斗相关的数据结构定义已移至 [战斗系统策划案](../战斗系统策划案.md)，请参考该文档了解详细的战斗系统设计。



### 3.6 预订单动作信息 (PreorderActionInfo)

```csharp
/// <summary>
/// 预订单动作信息 - 动作切换的预约信息
/// </summary>
[MemoryPackable]
public partial class PreorderActionInfo
{
    /// <summary>目标动作ID</summary>
    public int ActionId { get; set; }
    
    /// <summary>优先级</summary>
    public int Priority { get; set; }
    
    /// <summary>融合帧数</summary>
    public int TransitionFrames { get; set; }
    
    /// <summary>起始帧</summary>
    public int FromFrame { get; set; }
    
    /// <summary>切换后硬直帧数</summary>
    public int FreezingFrames { get; set; }
}
```


## 4. 动作系统架构图

> 查看完整的架构设计图：[ActionSystem.puml](./ActionSystem.puml)

![动作系统类图](./ActionSystem.puml)

## 5. 动作系统核心类设计

### 5.1 动作组件 (ActionComponent)

```csharp
/// <summary>
/// 动作组件 - 存储实体的动作状态
/// </summary>
[MemoryPackable]
public partial class ActionComponent
{
    /// <summary>当前动作信息</summary>
    public ActionInfo CurrentAction { get; set; }
    
    /// <summary>当前动作进度（0.0-1.0）</summary>
    public float CurrentActionProgress { get; set; }
    
    /// <summary>当前动作帧</summary>
    public int CurrentFrame { get; set; }
    
    /// <summary>输入命令缓存</summary>
    public List<ActionCommand> InputCommands { get; set; } = new();
    
    /// <summary>预订单动作列表</summary>
    public List<PreorderActionInfo> PreorderActions { get; set; } = new();
    
    /// <summary>可用动作列表</summary>
    public List<ActionInfo> AvailableActions { get; set; } = new();
    
    /// <summary>是否正在执行动作</summary>
    public bool IsExecutingAction => CurrentAction != null;
    
    /// <summary>动作是否已结束（需要外部提供动作持续时间）</summary>
    public bool IsActionFinished(int actionDuration) => CurrentAction != null && CurrentFrame >= actionDuration;
}
```

### 5.2 动作能力 (ActionCapability)

```csharp
/// <summary>
/// 动作能力 - 管理单个实体的动作系统
/// </summary>
public class ActionCapability : Capability
{
    /// <summary>动作组件</summary>
    private ActionComponent _actionComponent;
    
    /// <summary>初始化动作能力</summary>
    public override void Initialize()
    {
        base.Initialize();
        
        // 获取或创建动作组件
        _actionComponent = OwnerEntity?.GetComponent<ActionComponent>();
        if (_actionComponent == null)
        {
            _actionComponent = new ActionComponent();
            OwnerEntity?.AddComponent(_actionComponent);
        }
        
        // 初始化配置管理器
        ActionConfigManager.Initialize();
        
        // 预加载所有可用的ActionInfo
        LoadAvailableActions();
    }
    
    /// <summary>加载所有可用动作</summary>
    private void LoadAvailableActions()
    {
        _actionComponent.AvailableActions.Clear();
        
        // 获取所有可用的动作ID（这里需要根据实际需求实现）
        var availableActionIds = GetAvailableActionIds();
        
        foreach (var actionId in availableActionIds)
        {
            var actionInfo = ActionConfigManager.Instance.GetAction(actionId, OwnerEntity?.Id ?? 0);
            if (actionInfo != null)
            {
                _actionComponent.AvailableActions.Add(actionInfo);
            }
        }
    }
    
    /// <summary>获取可用动作ID列表</summary>
    private List<int> GetAvailableActionIds()
    {
        // TODO: 根据实际需求实现
        // 例如：从配置、技能系统、装备等获取
        return new List<int>();
    }
    
    /// <summary>每帧更新</summary>
    public override void Tick()
    {
        if (!CanExecute()) return;
        
        // 1. 检查所有动作的取消条件
        CheckActionCancellation();
        
        // 2. 从候选列表选择动作
        SelectActionFromCandidates();
        
        // 3. 更新当前动作
        UpdateCurrentAction();
        
    }
    
    
    
    /// <summary>检查动作取消条件</summary>
    private void CheckActionCancellation()
    {
        if (_actionComponent.CurrentAction == null) return;
        
        // 清空预订单列表
        _actionComponent.PreorderActions.Clear();
        
        // 检查当前动作是否已结束
        var actionDuration = GetActionDuration();
        if (IsActionFinished(actionDuration))
        {
            // 添加默认下一个动作到预订单列表
            var nextActionId = _actionComponent.CurrentAction.AutoNextActionId;
            if (nextActionId > 0)
            {
                var nextAction = GetActionInfo(nextActionId);
                if (nextAction != null)
                {
                    _actionComponent.PreorderActions.Add(new PreorderActionInfo
                    {
                        ActionId = nextActionId,
                        Priority = nextAction.Priority,
                        TransitionFrames = 0,
                        FromFrame = 0,
                        FreezingFrames = 0
                    });
                }
            }
        }
        else
        {
            // 检查其他动作的取消条件
            var availableActions = GetAvailableActions();
            
            foreach (var action in availableActions)
        {
            if (CanCancelToAction(action))
            {
                if (HasValidCommand(action))
                {
                        _actionComponent.PreorderActions.Add(new PreorderActionInfo
                    {
                        ActionId = action.Id,
                        Priority = CalculatePriority(action),
                            TransitionFrames = 3,
                            FromFrame = 0,
                            FreezingFrames = 0
                        });
                    }
                }
            }
        }
    }
    
    /// <summary>从预订单列表选择动作</summary>
    private void SelectActionFromCandidates()
    {
        if (_actionComponent.PreorderActions.Count == 0) return;
        
        // 按优先级排序
        _actionComponent.PreorderActions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        
        // 选择优先级最高的动作
        var selectedAction = _actionComponent.PreorderActions[0];
        var actionInfo = ActionConfigManager.Instance.GetAction(selectedAction.ActionId, OwnerEntity?.Id ?? 0);
        
        if (actionInfo != null)
        {
        // 切换到新动作
        SwitchToAction(actionInfo, selectedAction);
        }
        
        // 清空候选列表
        _actionComponent.PreorderActions.Clear();
    }
    
    /// <summary>切换到指定动作</summary>
    private void SwitchToAction(ActionInfo actionInfo, PreorderActionInfo preorderInfo)
    {
        _actionComponent.CurrentAction = actionInfo;
        // TODO: 根据实际帧率计算动作进度
        _actionComponent.CurrentActionProgress = 0.0f;
        _actionComponent.CurrentFrame = preorderInfo.FromFrame;
    }
    
    /// <summary>更新当前动作</summary>
    private void UpdateCurrentAction()
    {
        if (_actionComponent.CurrentAction == null) return;
        
        // 按帧更新动作进度
        _actionComponent.CurrentFrame += 1;
        var actionDuration = GetActionDuration();
        if (actionDuration > 0)
        {
            _actionComponent.CurrentActionProgress = (float)_actionComponent.CurrentFrame / actionDuration;
        }
    }
    
    /// <summary>检查动作是否已结束</summary>
    private bool IsActionFinished(int actionDuration)
    {
        if (_actionComponent.CurrentAction == null) return false;
        
        return _actionComponent.CurrentFrame >= actionDuration;
    }
    
    /// <summary>获取动作信息</summary>
    private ActionInfo GetActionInfo(int actionId)
    {
        return ActionConfigManager.Instance.GetAction(actionId, OwnerEntity?.Id ?? 0);
    }
    
    /// <summary>获取可用动作列表</summary>
    private List<ActionInfo> GetAvailableActions()
    {
        // 从ActionComponent获取所有可用的ActionInfo
        return _actionComponent.AvailableActions;
    }
    
    /// <summary>切换到下一个动作</summary>
    private void SwitchToNextAction()
    {
        if (_actionComponent.CurrentAction?.AutoNextActionId == null) return;
        
        var nextAction = ActionConfigManager.Instance?.GetAction(_actionComponent.CurrentAction.AutoNextActionId, OwnerEntity?.Id ?? 0);
        if (nextAction != null)
        {
            var preorderInfo = new PreorderActionInfo
            {
                ActionId = nextAction.Id,
                Priority = nextAction.Priority,
                TransitionFrames = 3,
                FromFrame = 0,
                FreezingFrames = 0
            };
            
            SwitchToAction(nextAction, preorderInfo);
        }
    }
    
    /// <summary>获取动作持续时间</summary>
    private int GetActionDuration()
    {
        // TODO: 根据实际的动作系统实现
        // 应该从ActionInfo或配置中获取动作的帧数
        return 0;
    }
    
    /// <summary>检查是否可以取消到指定动作</summary>
    private bool CanCancelToAction(ActionInfo targetAction)
    {
        if (_actionComponent.CurrentAction == null || targetAction == null) return false;
        
        // 检查CancelTag是否匹配BeCancelledTag
        foreach (var cancelTag in targetAction.CancelTags)
        {
            foreach (var beCancelledTag in _actionComponent.CurrentAction.BeCancelledTags)
            {
                if (beCancelledTag.Tags.Contains(cancelTag.Tag))
                {
                    // 检查时间范围
                    if (IsInTimeRange(beCancelledTag.Range))
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>检查是否有有效命令</summary>
    private bool HasValidCommand(ActionInfo actionInfo)
    {
        foreach (var command in actionInfo.Commands)
        {
            if (IsCommandValid(command))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>计算动作优先级</summary>
    private int CalculatePriority(ActionInfo actionInfo)
    {
        return actionInfo.Priority;
    }
    
    /// <summary>检查是否在时间范围内</summary>
    private bool IsInTimeRange(Vector2 range)
    {
        return _actionComponent.CurrentActionProgress >= range.x && _actionComponent.CurrentActionProgress <= range.y;
    }
    
    /// <summary>检查命令是否有效</summary>
    private bool IsCommandValid(ActionCommand command)
    {
        // 检查按键序列是否匹配
        return CheckKeySequence(command.KeySequence);
    }
    
    /// <summary>检查按键序列</summary>
    private bool CheckKeySequence(List<KeyMap> keySequence)
    {
        // TODO: 实现按键序列检查逻辑
        // 应该检查输入命令是否匹配按键序列
        return false;
    }
    
    /// <summary>执行动作</summary>
    public void ExecuteAction(int actionId)
    {
        var actionInfo = ActionConfigManager.Instance?.GetAction(actionId, OwnerEntity?.Id ?? 0);
        if (actionInfo != null)
        {
            var preorderInfo = new PreorderActionInfo
            {
                ActionId = actionId,
                Priority = actionInfo.Priority,
                TransitionFrames = 3,
                FromFrame = 0,
                FreezingFrames = 0
            };
            
            SwitchToAction(actionInfo, preorderInfo);
        }
    }
    
    /// <summary>检查是否可以执行动作</summary>
    public bool CanExecuteAction(int actionId)
    {
        var actionInfo = ActionConfigManager.Instance?.GetAction(actionId, OwnerEntity?.Id ?? 0);
        return actionInfo != null && CanCancelToAction(actionInfo);
    }
}
```



## 6. 每帧动作系统工作流程

> 查看完整的工作流程图：[ActionSystemFlow.puml](./ActionSystemFlow.puml)

![动作系统流程图](./ActionSystemFlow.puml)

```
1. 检查所有动作
   ├── 遍历所有可用动作
   ├── 检查CancelTag是否匹配当前动作的BeCancelledTag
   ├── 检查输入命令是否满足
   └── 将符合条件的动作加入候选列表

2. 从候选列表得出结论
   ├── 按优先级排序候选动作
   ├── 选择优先级最高的动作
   └── 执行动作切换

3. 更新当前动作
   ├── 更新当前动作信息
   ├── 更新动作进度
   └── 处理动作结束逻辑

```

### 5.2 动作切换的渠道

1. **输入指令切换**：玩家输入触发动作切换
2. **攻击命中切换**：攻击命中后触发派生动作
3. **受击切换**：受到攻击后切换到受击动作
4. **自动切换**：动作自然结束后的下一个动作
5. **临时切换**：通过TempBeCancelledTag触发的临时动作

## 7. 与战斗系统的集成

> 查看取消系统机制图：[ActionCancelSystem.puml](./ActionCancelSystem.puml)

![动作取消系统](./ActionCancelSystem.puml)

动作系统通过以下方式与 [战斗系统](../战斗系统策划案.md) 集成：

1. **数据共享**：ActionInfo中包含攻击和受击信息
2. **事件通知**：通过动作事件触发战斗判定
3. **状态同步**：动作状态与战斗状态保持同步

> **详细实现**：请参考 [战斗系统策划案](../战斗系统策划案.md) 了解打击感系统、攻击判定、受击判定等具体实现。


## 8. 与现有系统的集成

### 8.1 与Capability系统集成

动作系统已经通过ActionCapability集成到现有的Capability系统中。每个实体通过ActionCapability管理自己的动作状态，ActionComponent存储动作数据。

**集成方式**：
1. **ActionCapability**：继承自Capability，管理单个实体的动作（可序列化）
2. **ActionComponent**：存储实体的动作状态数据（可序列化）
3. **ActionConfigManager**：单例模式，提供全局动作配置管理（不需要序列化）


## 9. 配置系统

> **注意**：以下配置示例中，`attacks`、`attackPhase`、`defensePhase` 等战斗相关配置的详细说明请参考 [战斗系统策划案](../战斗系统策划案.md)。

### 9.1 动作配置文件格式

```json
{
  "actions": [
    {
      "id": 1000,
      "catalog": "idle",
      "priority": 0,
      "autoNextActionId": 1000,
      "autoTerminate": false,
      "keepPlayingAnim": false,
      "cancelTags": [],
      "beCancelledTags": [
        {
          "tags": ["attack", "move"],
          "range": [0.0, 1.0],
          "blendOut": 0.1,
          "priority": 0
        }
      ],
      "tempBeCancelledTags": [],
      "commands": []
    },
    {
      "id": 1001,
      "catalog": "attack",
      "priority": 10,
      "autoNextActionId": 1000,
      "autoTerminate": true,
      "keepPlayingAnim": false,
      "cancelTags": [
        {
          "tag": "attack",
          "startFrom": 0.0,
          "blendIn": 0.1,
          "priority": 0
        }
      ],
      "beCancelledTags": [
        {
          "tags": ["attack", "special"],
          "range": [0.0, 0.3],
          "blendOut": 0.1,
          "priority": 0
        }
      ],
      "tempBeCancelledTags": [],
      "commands": [
        {
          "keySequence": [
            {
              "key": "Attack",
              "duration": 0.2
            }
          ],
          "validFrames": 18
        }
      ]
    }
  ]
}
```

## 10. 调试工具

### 10.1 动作系统调试器

```csharp
/// <summary>
/// 动作系统调试器 - 用于调试动作系统状态
/// </summary>
public class ActionSystemDebugger
{
    private ActionComponent _actionComponent;
    
    /// <summary>初始化调试器</summary>
    public void Initialize(ActionComponent actionComponent)
    {
        _actionComponent = actionComponent;
    }
    
    /// <summary>获取当前状态信息</summary>
    public string GetCurrentStateInfo()
    {
        if (_actionComponent == null) return "动作组件未初始化";
        
        var sb = new StringBuilder();
        sb.AppendLine("=== 动作系统状态 ===");
        sb.AppendLine($"当前动作: {_actionComponent.CurrentAction?.Id ?? "无"}");
        sb.AppendLine($"动作进度: {_actionComponent.CurrentActionProgress:F2}");
        sb.AppendLine($"当前帧: {_actionComponent.CurrentFrame}");
        sb.AppendLine($"预订单数量: {_actionComponent.PreorderActions.Count}");
        sb.AppendLine($"输入命令数量: {_actionComponent.InputCommands.Count}");
        sb.AppendLine($"是否正在执行动作: {_actionComponent.IsExecutingAction}");
        sb.AppendLine($"动作是否已结束: {_actionComponent.IsActionFinished(0)}");
        
        return sb.ToString();
    }
    
    /// <summary>获取动作列表信息</summary>
    public string GetActionListInfo()
    {
        if (_actionComponent?.AvailableActions == null) return "动作组件未初始化";
        
        var sb = new StringBuilder();
        sb.AppendLine("=== 可用动作列表 ===");
        
        foreach (var action in _actionComponent.AvailableActions)
        {
            sb.AppendLine($"- {action.Id} (类型: {action.Catalog}, 优先级: {action.Priority})");
        }
        
        return sb.ToString();
    }
    
    /// <summary>执行调试命令</summary>
    public void ExecuteDebugCommand(string command, params string[] args)
    {
        switch (command.ToLower())
        {
            case "switch":
                if (args.Length > 0 && _actionComponent != null && int.TryParse(args[0], out var actionId))
                {
                    // 切换到指定动作
                    var actionInfo = ActionConfigManager.Instance?.GetAction(actionId, 0);
                    if (actionInfo != null)
                    {
                        _actionComponent.CurrentAction = actionInfo;
                        _actionComponent.CurrentActionProgress = 0f;
                        _actionComponent.CurrentFrame = 0;
                    }
                }
                break;
            case "list":
                Console.WriteLine(GetActionListInfo());
                break;
            case "state":
                Console.WriteLine(GetCurrentStateInfo());
                break;
            case "reset":
                if (_actionComponent != null)
                {
                    _actionComponent.CurrentAction = null;
                    _actionComponent.CurrentActionProgress = 0f;
                    _actionComponent.CurrentFrame = 0;
                    _actionComponent.PreorderActions.Clear();
                    _actionComponent.InputCommands.Clear();
                }
                break;
        }
    }
}
```

### 10.2 动作配置管理器（工厂类）

```csharp
/// <summary>
/// 动作配置管理器 - 工厂类，用于组装ActionInfo（单例）
/// </summary>
public class ActionConfigManager
{
    /// <summary>单例实例</summary>
    public static ActionConfigManager Instance { get; private set; }
    
    /// <summary>动作表格数据缓存</summary>
    private Dictionary<int, ActionTableData> _actionTableData = new();
    
    /// <summary>初始化配置管理器</summary>
    public static void Initialize()
    {
        if (Instance == null)
        {
            Instance = new ActionConfigManager();
        }
        Instance.LoadActionTableData();
    }
    
    /// <summary>加载动作表格数据</summary>
    private void LoadActionTableData()
    {
        // 从Luban表格加载动作配置数据
        // 例如：_actionTableData = Tables.Action.DataList.ToDictionary(x => x.Id);
    }
    
    /// <summary>获取动作信息（工厂方法）</summary>
    /// <param name="actionId">动作ID</param>
    /// <param name="entityId">实体ID</param>
    /// <returns>组装后的ActionInfo</returns>
    public ActionInfo GetAction(int actionId, int entityId)
    {
        // 从表格获取基础数据
        if (!_actionTableData.TryGetValue(actionId, out var tableData))
        {
            return null;
        }
        
        // 组装ActionInfo
        var actionInfo = new ActionInfo
        {
            Id = actionId,
            Catalog = tableData.Catalog,
            Priority = tableData.Priority,
            AutoNextActionId = tableData.AutoNextActionId,
            KeepPlayingAnim = tableData.KeepPlayingAnim,
            AutoTerminate = tableData.AutoTerminate,
            Flip = tableData.Flip
        };
        
        // 从表格数据组装CancelTags
        actionInfo.CancelTags = tableData.CancelTags?.Select(ct => new CancelTag
        {
            Tag = ct.Tag,
            StartFrom = ct.StartFrom,
            BlendIn = ct.BlendIn,
            Priority = ct.Priority
        }).ToList() ?? new List<CancelTag>();
        
        // 从表格数据组装BeCancelledTags
        actionInfo.BeCancelledTags = tableData.BeCancelledTags?.Select(bt => new BeCancelledTag
        {
            Tags = bt.Tags,
            Range = bt.Range,
            BlendOut = bt.BlendOut,
            Priority = bt.Priority
        }).ToList() ?? new List<BeCancelledTag>();
        
        // 从表格数据组装TempBeCancelledTags
        actionInfo.TempBeCancelledTags = tableData.TempBeCancelledTags?.Select(tt => new TempBeCancelledTag
        {
            Id = tt.Id,
            Tags = tt.Tags,
            Time = tt.Time,
            BlendOut = tt.BlendOut,
            Priority = tt.Priority
        }).ToList() ?? new List<TempBeCancelledTag>();
        
        // 从表格数据组装Commands
        actionInfo.Commands = tableData.Commands?.Select(cmd => new ActionCommand
        {
            KeySequence = cmd.KeySequence,
            ValidFrames = cmd.ValidFrames
        }).ToList() ?? new List<ActionCommand>();
        
        // TODO: 根据entityId获取运行时数据，与表格数据合并
        // 例如：从实体的状态、buff等获取额外的动作修改
        
        return actionInfo;
    }
}
```




## 11. 总结

本动作系统策划案基于文章中的核心思想，结合Astrum项目的实际需求，设计了一套完整的动作游戏动作系统。主要特点包括：

1. **帧驱动的设计**：每一帧都是重要的状态，支持精确的动作控制
2. **统一的预订单机制**：所有动作切换（自动和手动）都通过预订单列表统一处理
3. **纯逻辑层架构**：所有代码运行在AstrumLogic中，不依赖Unity MonoBehaviour
4. **组件化设计**：ActionComponent存储状态和可用动作，ActionCapability管理逻辑，ActionConfigManager作为工厂类组装ActionInfo
5. **数据分离**：ActionInfo是运行时数据，由表格数据和实体状态共同组装，支持动态修改
6. **预加载优化**：所有可用动作在初始化时预加载，运行时性能更好
7. **优先级排序**：手动输入的动作可以覆盖默认下一个动作，提供更好的用户体验
8. **模块化架构**：与现有系统良好集成，易于扩展和维护

> **相关文档**：
> - [战斗系统策划案](../战斗系统策划案.md) - 攻击判定、受击判定、打击感系统
> - [房间系统策划案](../../Game-Design/房间系统策划案.md) - 房间管理和多人游戏支持

### 架构优势

- **组件化设计**：ActionComponent存储状态数据，ActionCapability管理业务逻辑，职责分离清晰
- **逻辑与表现完全分离**：动作逻辑在服务端运行，通过事件通知View层处理动画
- **网络友好**：纯C#类设计便于网络同步和状态管理
- **可测试性**：不依赖Unity环境，便于单元测试
- **可扩展性**：接口设计便于不同平台的实现
- **职责清晰**：逻辑层专注游戏逻辑，View层专注表现效果
- **实体驱动**：每个实体通过ActionCapability管理自己的动作，符合ECS架构理念

该系统为Astrum项目提供了实现动作游戏功能的基础框架，可以根据具体游戏需求进行进一步的定制和优化。

---

## 8. 实现总结

### 8.1 实现状态

**实现时间**：2024年12月  
**实现状态**：核心功能已完成，动画同步已实现  
**技术栈**：C# + Unity + MemoryPack + Luban + Animancer

### 8.2 已实现功能

#### 8.2.1 核心数据结构
- ✅ ActionInfo - 动作信息（运行时数据）
- ✅ CancelTag - 取消标签
- ✅ BeCancelledTag - 被取消标签  
- ✅ TempBeCancelledTag - 临时被取消标签
- ✅ ActionCommand - 动作命令
- ✅ PreorderActionInfo - 预订单动作信息

#### 8.2.2 组件系统
- ✅ ActionComponent - 动作组件
- ✅ ActionCapability - 动作能力
- ✅ AnimationViewComponent - 动画视图组件

#### 8.2.3 配置管理
- ✅ ActionConfigManager - 动作配置管理器
- ✅ Luban表格数据集成
- ✅ JSON配置解析

#### 8.2.4 动画同步
- ✅ Animancer集成
- ✅ 帧同步时间插值
- ✅ 动画预加载
- ✅ 循环动画检测

### 8.3 技术特性

#### 8.3.1 序列化支持
- **MemoryPack**：所有核心数据结构支持MemoryPack序列化
- **网络传输**：支持动作数据的网络同步
- **性能优化**：使用MemoryPack的高性能序列化

#### 8.3.2 帧同步支持
- **时间单位**：所有时间相关数据使用帧数（int）
- **帧同步**：与LSController的预测帧系统集成
- **时间插值**：支持渲染帧与逻辑帧之间的时间插值

#### 8.3.3 配置驱动
- **Luban表格**：使用Luban生成配置数据
- **JSON解析**：支持复杂的JSON配置解析
- **热更新**：支持配置数据的热更新

### 8.4 文件结构

```
AstrumLogic
├── ActionSystem/          # 动作系统核心数据结构
│   ├── ActionInfo.cs
│   ├── CancelTag.cs
│   ├── BeCancelledTag.cs
│   ├── TempBeCancelledTag.cs
│   ├── ActionCommand.cs
│   └── PreorderActionInfo.cs
├── Components/            # 组件层
│   └── ActionComponent.cs
├── Capabilities/          # 能力层
│   └── ActionCapability.cs
└── Managers/              # 管理层
    └── ActionConfigManager.cs

AstrumView
└── Components/            # 视图组件
    └── AnimationViewComponent.cs
```

### 8.5 已知问题与待优化

#### 8.5.1 优先级系统矛盾
**问题**：配置注释说"越小越高"，但排序逻辑是"越大越前"  
**影响**：可能导致动作优先级判断错误  
**建议**：修改排序逻辑为 `a.Priority.CompareTo(b.Priority)`

#### 8.5.2 动画循环检测
**问题**：走路动画在循环衔接处可能出现跳变  
**状态**：已添加详细日志用于问题诊断  
**建议**：根据日志分析结果进行优化

#### 8.5.3 错误处理
**现状**：已添加全面的错误日志  
**建议**：根据实际运行情况进一步完善错误处理逻辑

### 8.6 后续开发计划

#### 8.6.1 短期计划
1. 修复优先级系统矛盾
2. 优化动画循环衔接
3. 完善错误处理机制

#### 8.6.2 中期计划
1. 添加动作状态机
2. 实现动作连招系统
3. 添加动作特效系统

#### 8.6.3 长期计划
1. 支持动作编辑器
2. 实现动作回放系统
3. 添加动作AI系统

### 8.7 相关文档

- [动画系统实现总结.md](../动画系统实现总结.md) - 动画系统详细实现
- [ActionSystem.puml](./ActionSystem.puml) - 系统架构图
- [ActionCancelSystem.puml](./ActionCancelSystem.puml) - 取消系统流程图

---

*文档版本：v2.1*  
*创建时间：2025-01-22*  
*最后更新：2024-12-19*  
*状态：策划案完成，实现总结已添加，基于帧驱动*

