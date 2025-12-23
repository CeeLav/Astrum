# Design: 实体运行时检查器编辑器窗口

## Context

在 ECC 架构中，实体（Entity）是游戏对象的核心，包含：
- **组件（Component）** - 存储数据，通过 `Entity.Components` 字典访问
- **Capability** - 包含逻辑，通过 `Entity.CapabilityStates` 字典和 `CapabilitySystem` 管理
- **禁用 Tag** - 通过 `Entity.DisabledTags` 字典管理被禁用的 Capability Tag 及其禁用发起者
- **逻辑层消息队列** - 通过 `Entity.EventQueue` 存储待处理的逻辑事件（EntityEvent）
- **视图层消息队列** - 通过 `Entity.ViewEventQueue` 存储待处理的视图事件（ViewEvent）

开发人员需要在运行时查看这些信息以进行调试和开发。

## Goals / Non-Goals

### Goals

1. **实时查看实体状态** - 在运行时查看任意实体的组件数据和 Capability 状态
2. **消息日志追踪** - 查看发送给实体的消息，便于调试事件流
3. **易于使用** - 简单的实体选择机制（ID 输入或场景选择）
4. **性能友好** - 不影响运行时性能，仅在 Editor 模式下运行

### Non-Goals

1. **不修改实体数据** - 这是只读检查器，不支持编辑
2. **不支持历史回放** - 不记录历史状态，只显示当前状态
3. **不支持多实体对比** - 一次只查看一个实体
4. **不集成到游戏内 UI** - 这是 Editor 工具，不是游戏内功能

## Architecture

### 窗口结构

```
EntityRuntimeInspectorWindow (EditorWindow)
├── EntitySelectorModule      # 实体选择模块
│   ├── Entity ID 输入框
│   └── 实体信息显示（名称、配置ID等）
├── ComponentViewModule       # 组件展示模块
│   └── 组件列表（可折叠）
│       └── 每个组件的字段展示
├── CapabilityViewModule      # Capability 展示模块
│   ├── Capability 列表（可折叠）
│   │   ├── 激活状态
│   │   ├── 优先级
│   │   ├── 标签
│   │   └── 持续时间
│   └── 禁用 Tag 列表
│       └── Tag -> 禁用发起者实体ID集合
└── MessageLogModule          # 消息日志模块
    ├── 逻辑层消息队列（EntityEvent）
    │   └── 消息列表（滚动视图）
    │       └── 帧号 + 消息类型 + 消息内容
    └── 视图层消息队列（ViewEvent）
        └── 消息列表（滚动视图）
            └── 时间戳 + 消息类型 + 消息内容
```

### 数据访问流程

```
EditorWindow
    ↓
获取 World 实例（通过 Room.MainWorld 或 GameMode）
    ↓
World.GetEntity(entityId)
    ↓
Entity
    ├── Components (Dictionary<int, BaseComponent>)
    ├── CapabilityStates (Dictionary<int, CapabilityState>)
    ├── DisabledTags (Dictionary<CapabilityTag, HashSet<long>>)
    ├── EventQueue (Queue<EntityEvent>) - 逻辑层消息队列
    └── ViewEventQueue (Queue<ViewEvent>) - 视图层消息队列
```

### 刷新机制

- **手动刷新** - 用户点击刷新按钮
- **自动刷新** - 可配置的刷新间隔（默认 0.5 秒）
- **节流机制** - 避免过于频繁的刷新导致 Editor 卡顿

## UI 设计草图

### 窗口布局（ASCII 艺术）

```
┌─────────────────────────────────────────────────────────────┐
│ Entity Runtime Inspector                          [×]        │
├─────────────────────────────────────────────────────────────┤
│ [工具栏] [刷新] [自动刷新: ☑] [刷新间隔: 0.5s]              │
├─────────────────────────────────────────────────────────────┤
│ ┌─ 实体选择 ─────────────────────────────────────────────┐  │
│ │ Entity ID: [________] [查找] [从场景选择]              │  │
│ │ 实体名称: Player_001                                   │  │
│ │ 配置ID: 1001 | 创建时间: 2025-12-06 20:00:00          │  │
│ └────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│ ┌─ 组件数据 ─────────────────────────────────────────────┐  │
│ │ ▼ TransComponent                                        │  │
│ │   Position: (10.5, 0, 5.2)                             │  │
│ │   Rotation: (0, 90, 0)                                  │  │
│ │   Scale: (1, 1, 1)                                     │  │
│ │ ▼ ActionComponent                                       │  │
│ │   CurrentActionId: 1001                                │  │
│ │   CurrentFrame: 15                                      │  │
│ │   ...                                                   │  │
│ │ ▶ MovementComponent (点击展开)                         │  │
│ └────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│ ┌─ Capability 状态 ─────────────────────────────────────┐  │
│ │ ▼ ActionCapability [激活] [优先级: 100] [标签: Control]│  │
│ │   激活持续时间: 30 帧                                    │  │
│ │ ▼ MovementCapability [激活] [优先级: 50] [标签: Move] │  │
│ │   激活持续时间: 120 帧                                   │  │
│ │ ▶ SkillCapability [禁用] [优先级: 80] [标签: Skill]  │  │
│ │   禁用持续时间: 5 帧                                     │  │
│ │                                                         │  │
│ │ 被禁用的 Tag:                                           │  │
│ │   • UserInputMovement (禁用者: 1001, 1002)            │  │
│ │   • Attack (禁用者: 2001)                              │  │
│ └────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│ ┌─ 消息日志 ─────────────────────────────────────────────┐  │
│ │ [逻辑层消息队列]                                         │  │
│ │ [滚动区域]                                              │  │
│ │ [Frame: 100] HitEvent: TargetId=2001, Damage=100     │  │
│ │ [Frame: 101] SkillTriggerEvent: SkillId=3001         │  │
│ │ [Frame: 102] BuffApplyEvent: BuffId=4001             │  │
│ │ ...                                                     │  │
│ │ [视图层消息队列]                                         │  │
│ │ [滚动区域]                                              │  │
│ │ [20:00:05.123] VFXTriggerEvent: ActionId=1001         │  │
│ │ [20:00:05.125] AnimationEvent: ClipName=Attack         │  │
│ │ [20:00:05.130] ViewUpdateEvent: Position=(10,0,5)    │  │
│ │ ...                                                     │  │
│ │ [清空日志]                                               │  │
│ └────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### PlantUML 架构图

```plantuml
@startuml EntityRuntimeInspector Architecture
!theme plain
skinparam backgroundColor #FFFFFF
skinparam componentStyle rectangle

package "Unity Editor" {
  component "EntityRuntimeInspectorWindow" as Window {
    component "EntitySelectorModule" as Selector
    component "ComponentViewModule" as ComponentView
    component "CapabilityViewModule" as CapabilityView
    component "MessageLogModule" as MessageLog
  }
}

package "Runtime Logic" {
  component "World" as World {
    component "Entities Dictionary" as Entities
    component "CapabilitySystem" as CapSys
  }
  component "Entity" as Entity {
    component "Components" as Components
    component "CapabilityStates" as CapStates
    component "DisabledTags" as DisabledTags
    component "EventQueue" as LogicEventQueue
    component "ViewEventQueue" as ViewEventQueue
  }
}

Window --> World : GetEntity(id)
World --> Entities : Lookup
Entities --> Entity : Return
Entity --> Components : Read
Entity --> CapStates : Read
Entity --> DisabledTags : Read
Entity --> LogicEventQueue : Read
Entity --> ViewEventQueue : Read
CapSys --> CapStates : Query State

Selector --> World : Query Entity
ComponentView --> Components : Display Fields
CapabilityView --> CapStates : Display State
CapabilityView --> DisabledTags : Display Tags
CapabilityView --> CapSys : Query Capability Info
MessageLog --> LogicEventQueue : Display Logic Messages
MessageLog --> ViewEventQueue : Display View Messages

@enduml
```

## Decisions

### Decision 1: 使用 EditorWindow 而非 Inspector 扩展

**选择**: 使用独立的 `EditorWindow` 创建新窗口

**理由**:
- 需要显示的信息较多，独立窗口有更多空间
- 可以自定义布局，不受 Inspector 限制
- 可以同时显示多个模块（组件、Capability、消息）

**替代方案**: 扩展 Unity Inspector
- ❌ 空间受限
- ❌ 难以自定义复杂布局

### Decision 2: 使用反射展示组件数据

**选择**: 使用 C# 反射获取组件字段值

**理由**:
- 组件类型多样，难以为每种组件编写专门的展示代码
- 反射可以自动发现所有字段
- 仅在 Editor 模式下使用，性能影响可接受

**替代方案**: 为每个组件实现 `IInspectorDisplayable` 接口
- ❌ 需要修改所有组件类
- ❌ 维护成本高

**缓解措施**:
- 缓存反射结果（Type -> FieldInfo[]）
- 限制刷新频率
- 使用 `EditorGUIUtility.isProSkin` 优化显示

### Decision 3: 消息日志使用队列快照

**选择**: 每次刷新时复制 `EventQueue` 和 `ViewEventQueue` 的内容进行显示

**理由**:
- `EventQueue`（逻辑层）和 `ViewEventQueue`（视图层）都是运行时队列，会被系统消费
- 需要保留消息历史用于查看
- 快照机制简单可靠
- 两个队列分开显示，便于区分逻辑层和视图层消息

**替代方案**: 订阅消息事件
- ❌ 需要修改消息系统
- ❌ 增加系统复杂度

**缓解措施**:
- 限制日志条数（如最多 100 条）
- 提供清空日志功能
- 使用滚动视图优化显示
- 逻辑层消息显示帧号，视图层消息显示时间戳

### Decision 4: 自动刷新使用 EditorApplication.update

**选择**: 使用 `EditorApplication.update` 回调实现自动刷新

**理由**:
- Unity 标准机制
- 与 Editor 生命周期集成良好
- 可以控制刷新频率

**替代方案**: 使用协程（Coroutine）
- ❌ EditorWindow 不支持协程
- ❌ 需要额外的 MonoBehaviour

## Risks / Trade-offs

### Risk 1: 反射性能开销

**风险**: 频繁使用反射可能导致 Editor 卡顿

**缓解**:
- 缓存反射结果（Type -> FieldInfo[]）
- 限制刷新频率（默认 0.5 秒）
- 仅在数据变化时更新显示

### Risk 2: 运行时访问 World 的线程安全

**风险**: 在 Editor 刷新时访问运行时 World 可能导致数据不一致

**缓解**:
- 添加空值检查
- 使用 try-catch 捕获异常
- 显示友好的错误提示

### Risk 3: 消息日志内存占用

**风险**: 长时间运行可能积累大量消息

**缓解**:
- 限制日志条数（如最多 100 条）
- 提供清空日志功能
- 使用循环缓冲区

## Implementation Details

### 实体选择机制

```csharp
// 伪代码
private long _selectedEntityId = 0;
private Entity _selectedEntity = null;

private void OnEntityIdInputChanged(string idString)
{
    if (long.TryParse(idString, out long entityId))
    {
        _selectedEntityId = entityId;
        RefreshEntity();
    }
}

private void RefreshEntity()
{
    var world = GetWorld();
    if (world == null)
    {
        _selectedEntity = null;
        return;
    }
    
    _selectedEntity = world.GetEntity(_selectedEntityId);
    Repaint();
}
```

### 组件数据展示

```csharp
// 伪代码
private void DrawComponentView(Entity entity)
{
    if (entity == null) return;
    
    foreach (var component in entity.Components.Values)
    {
        EditorGUILayout.LabelField(component.GetType().Name, EditorStyles.boldLabel);
        
        var fields = GetCachedFields(component.GetType());
        foreach (var field in fields)
        {
            var value = field.GetValue(component);
            EditorGUILayout.LabelField(field.Name, value?.ToString() ?? "null");
        }
    }
}
```

### Capability 状态展示

```csharp
// 伪代码
private void DrawCapabilityView(Entity entity)
{
    if (entity == null || entity.World?.CapabilitySystem == null) return;
    
    var capSystem = entity.World.CapabilitySystem;
    foreach (var kvp in entity.CapabilityStates)
    {
        var typeId = kvp.Key;
        var state = kvp.Value;
        var capability = capSystem.GetCapabilityByTypeId(typeId);
        
        if (capability != null)
        {
            EditorGUILayout.LabelField(capability.GetType().Name);
            EditorGUILayout.LabelField($"激活: {state.IsActive}");
            EditorGUILayout.LabelField($"优先级: {capability.Priority}");
            EditorGUILayout.LabelField($"标签: {string.Join(", ", capability.Tags)}");
        }
    }
}
```

### 消息日志展示

```csharp
// 伪代码
private List<EntityEvent> _logicMessageLog = new List<EntityEvent>();
private List<ViewEvent> _viewMessageLog = new List<ViewEvent>();

private void UpdateMessageLog(Entity entity)
{
    if (entity == null) return;
    
    // 更新逻辑层消息队列
    if (entity.EventQueue != null)
    {
        var logicSnapshot = entity.EventQueue.ToArray();
        _logicMessageLog.AddRange(logicSnapshot);
        if (_logicMessageLog.Count > 100)
        {
            _logicMessageLog.RemoveRange(0, _logicMessageLog.Count - 100);
        }
    }
    
    // 更新视图层消息队列
    if (entity.ViewEventQueue != null)
    {
        var viewSnapshot = entity.ViewEventQueue.ToArray();
        _viewMessageLog.AddRange(viewSnapshot);
        if (_viewMessageLog.Count > 100)
        {
            _viewMessageLog.RemoveRange(0, _viewMessageLog.Count - 100);
        }
    }
}

private void DrawMessageLog()
{
    EditorGUILayout.LabelField("消息日志", EditorStyles.boldLabel);
    
    // 逻辑层消息队列
    EditorGUILayout.LabelField("逻辑层消息队列", EditorStyles.miniLabel);
    _logicScrollPosition = EditorGUILayout.BeginScrollView(_logicScrollPosition, GUILayout.Height(150));
    foreach (var msg in _logicMessageLog)
    {
        EditorGUILayout.LabelField($"[Frame: {msg.Frame}] {msg.EventType.Name}: {msg.EventData}");
    }
    EditorGUILayout.EndScrollView();
    
    EditorGUILayout.Space(5);
    
    // 视图层消息队列
    EditorGUILayout.LabelField("视图层消息队列", EditorStyles.miniLabel);
    _viewScrollPosition = EditorGUILayout.BeginScrollView(_viewScrollPosition, GUILayout.Height(150));
    foreach (var msg in _viewMessageLog)
    {
        EditorGUILayout.LabelField($"[{DateTime.Now:HH:mm:ss.fff}] {msg.GetType().Name}: {msg}");
    }
    EditorGUILayout.EndScrollView();
    
    if (GUILayout.Button("清空日志"))
    {
        _logicMessageLog.Clear();
        _viewMessageLog.Clear();
    }
}
```

### 禁用 Tag 展示

```csharp
// 伪代码
private void DrawDisabledTags(Entity entity)
{
    if (entity?.DisabledTags == null || entity.DisabledTags.Count == 0)
    {
        EditorGUILayout.LabelField("被禁用的 Tag: 无", EditorStyles.miniLabel);
        return;
    }
    
    EditorGUILayout.LabelField("被禁用的 Tag:", EditorStyles.miniLabel);
    EditorGUI.indentLevel++;
    foreach (var kvp in entity.DisabledTags)
    {
        var tag = kvp.Key;
        var disabledBy = kvp.Value; // HashSet<long>
        var disabledByStr = string.Join(", ", disabledBy);
        EditorGUILayout.LabelField($"• {tag} (禁用者: {disabledByStr})", EditorStyles.miniLabel);
    }
    EditorGUI.indentLevel--;
}
```

## Open Questions

1. **World 访问方式** - 如何从 EditorWindow 访问运行时 World？
   - 选项 A: 通过 `GameMode` 单例（如 `SinglePlayerGameMode.Instance`）
   - 选项 B: 通过 `Room` 管理器
   - 选项 C: 通过静态注册表
   - **建议**: 先尝试选项 A，如果不可用则使用选项 B

2. **组件字段过滤** - 是否显示所有字段，还是过滤掉某些字段（如 `EntityId`、`IsFromPool`）？
   - **建议**: 默认显示所有字段，但提供过滤选项

3. **Capability 详细信息** - 已确定显示的信息项（激活状态、优先级、标签、持续时间），无需额外状态

4. **消息时间戳** - ViewEvent 是否有时间戳字段？如果没有，如何显示时间？
   - **建议**: 使用 Editor 的当前时间作为显示时间戳
   - **逻辑层消息**: 使用 EntityEvent.Frame 字段显示帧号
   - **视图层消息**: 使用 Editor 的当前时间作为显示时间戳

