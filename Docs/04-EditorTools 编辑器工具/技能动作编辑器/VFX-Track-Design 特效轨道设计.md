# 特效轨道设计

## 概述

在技能动作编辑器中实现特效轨道功能，支持在编辑器中编辑和预览特效，并在运行时基于触发帧播放特效。特效触发帧与技能效果触发帧统一存储在 `triggerFrames` 字段中，使用 JSON 格式。

## 架构设计

### 编辑器端架构

```
TimelineEditorModule
    ├── VFXTrackRenderer (已有)
    │   ├── RenderEvent - 渲染事件条
    │   └── EditEvent - 编辑事件属性
    ├── AnimationPreviewModule (扩展)
    │   └── VFXPreviewManager - 特效预览管理器
    │       ├── LoadVFX - 加载特效资源
    │       ├── PlayVFX - 播放特效
    │       ├── StopVFX - 停止特效
    │       └── UpdateVFX - 更新特效状态（跟随、位置等）
    └── SkillActionEditorData (扩展)
        └── SyncFromTimelineEvents - 统一同步所有触发帧到 JSON
```

### 运行时架构

```
Logic层 (AstrumLogic):
SkillExecutorCapability (扩展)
    └── ProcessFrame - 处理所有触发帧
        ├── 解析 triggerFrames JSON
        ├── 按类型分发处理
        │   ├── SkillEffect → ProcessSkillEffectTriggers
        │   └── VFX → ProcessVFXTriggers
        └── 发布 VFXTriggerEventData 事件

View层 (AstrumView):
VFXManager (新建)
    ├── 订阅 VFXTriggerEventData 事件
    ├── PlayVFX(EntityView, VFXData) - 播放特效
    ├── StopVFX - 停止特效
    └── UpdateVFX - 更新特效状态
```

## 数据结构

### 统一触发帧数据结构（重构）

**职责**: 统一存储所有类型的触发帧（技能效果、特效、音效等）

**JSON 格式**:
```json
[
  {
    "frame": 10,
    "type": "SkillEffect",
    "triggerType": "Collision",
    "effectId": 4001,
    "collisionInfo": "Box:5x2x1"
  },
  {
    "startFrame": 5,
    "endFrame": 10,
    "type": "VFX",
    "resourcePath": "Effects/SwordHit",
    "positionOffset": [0, 1, 0],
    "rotation": [0, 90, 0],
    "scale": 1.5,
    "playbackSpeed": 1.2,
    "followCharacter": true,
    "loop": false
  },
  {
    "frame": 20,
    "type": "SkillEffect",
    "triggerType": "Direct",
    "effectId": 4002
  },
  {
    "frame": 25,
    "type": "SFX",
    "resourcePath": "Sounds/SwordSwing",
    "volume": 0.8
  }
]
```

**字段说明**:
- **帧号字段**:
  - `frame` (int) - 单帧触发（与 `startFrame`/`endFrame` 互斥）
  - `startFrame` (int) - 多帧范围起始帧（与 `frame` 互斥）
  - `endFrame` (int) - 多帧范围结束帧（与 `frame` 互斥）
- **通用字段**:
  - `type` (string) - 触发类型：`SkillEffect`（技能效果）、`VFX`（特效）、`SFX`（音效）等
- **类型特定字段**:
  - **SkillEffect**（技能效果）:
    - `triggerType` (string) - 触发方式：`Collision`（碰撞触发）、`Direct`（直接触发）、`Condition`（条件触发）
    - `effectId` (int) - 效果ID
    - `collisionInfo` (string) - 碰撞盒信息（仅 Collision 类型使用）
  - **VFX**（特效）:
    - `resourcePath` (string) - 特效资源路径
    - `positionOffset` (float[]) - 位置偏移
    - `rotation` (float[]) - 旋转
    - `scale` (float) - 缩放
    - `playbackSpeed` (float) - 播放速度
    - `followCharacter` (bool) - 是否跟随角色
    - `loop` (bool) - 是否循环播放
  - **SFX**（音效）:
    - `resourcePath` (string) - 音效资源路径
    - `volume` (float) - 音量

### TriggerFrameData（重构）

**职责**: 编辑器端统一触发帧数据结构

```csharp
[Serializable]
public class TriggerFrameData
{
    // 帧范围（二选一）
    public int? Frame;           // 单帧触发
    public int? StartFrame;      // 多帧范围起始
    public int? EndFrame;        // 多帧范围结束
    
    // 触发类型（顶层类型）
    public string Type;          // SkillEffect, VFX, SFX 等
    
    // 技能效果字段（type == "SkillEffect" 时使用）
    public string TriggerType;  // Collision, Direct, Condition（技能效果内部的触发方式）
    public int? EffectId;
    public string CollisionInfo; // 仅 Collision 类型使用
    
    // 特效字段（type == "VFX" 时使用）
    public string ResourcePath;
    public Vector3? PositionOffset;
    public Vector3? Rotation;
    public float? Scale;
    public float? PlaybackSpeed;
    public bool? FollowCharacter;
    public bool? Loop;
    
    // 音效字段（type == "SFX" 时使用）
    public float? Volume;
    
    // 辅助属性
    public bool IsSingleFrame => Frame.HasValue || (StartFrame == EndFrame);
}
```

### VFXEventData（已有，编辑器使用）

**职责**: 编辑器时间轴中存储特效事件数据

**字段**: 与现有结构保持一致，用于时间轴编辑

## 流程设计

### 编辑器预览流程

```
用户播放动画
    ↓
AnimationPreviewModule.Update
    ↓
解析 triggerFrames JSON
    ↓
按类型过滤当前帧的触发
    ↓
VFX 类型 → VFXPreviewManager.PlayVFX
    ├── 加载特效资源（Prefab）
    ├── 实例化特效
    ├── 设置变换参数（位置、旋转、缩放）
    ├── 设置播放参数（速度、循环）
    └── 如果跟随角色，绑定到预览模型
    ↓
每帧更新
    ├── 更新特效位置（如果跟随）
    ├── 检查特效是否结束
    └── 清理已结束的特效
```

### 数据保存流程

```
用户保存动作
    ↓
SkillActionEditorData.SyncFromTimelineEvents
    ├── 收集所有轨道事件（SkillEffect, VFX, SFX 等）
    ├── 转换为统一的 TriggerFrameData 列表
    └── 序列化为 JSON 字符串存储到 triggerFrames
    ↓
SkillActionDataWriter.WriteSkillActionTableCSV
    └── 写入 triggerFrames JSON 到 CSV
```

### 运行时播放流程

```
SkillExecutorCapability.Tick
    ↓
检查当前帧
    ↓
解析 SkillActionInfo.TriggerFrames JSON
    ↓
按类型分发处理
    ├── SkillEffect → ProcessSkillEffectTriggers
    │   └── 根据 triggerType 进一步分发（Collision/Direct/Condition）
    └── VFX → ProcessVFXTriggers
        ├── 查找当前帧的 VFX 触发
        └── 发布 VFXTriggerEventData 事件
            ↓
View层 VFXManager 监听事件
    ├── 通过 EntityId 获取 EntityView
    ├── 加载特效资源
    ├── 实例化特效
    ├── 设置变换参数
    ├── 设置播放参数
    └── 如果跟随，绑定到 EntityView
```

## 关键决策与取舍

**问题**: VFX 数据如何存储到配置表？

**备选**:
1. 使用独立的 VFXFrames 字段（字符串格式）
2. 整合到 triggerFrames 中，使用 JSON 格式
3. 新增独立表存储 VFX 事件

**选择**: 方案2 - 整合到 triggerFrames，使用 JSON 格式

**理由**:
- 统一所有触发帧类型的管理，避免数据分散
- JSON 格式更易扩展，支持复杂参数结构
- 便于后续添加新的触发类型（如 SFX、CameraShake 等）
- 编辑器端和运行时使用统一的数据结构

**影响**:
- 需要重构现有的字符串解析逻辑
- 需要手动转换现有表格数据为JSON格式
- JSON 格式更易读，但文件稍大

**问题**: 编辑器预览如何实现？

**备选**:
1. 在 PreviewRenderUtility 中渲染特效
2. 在 Scene 视图中实时预览
3. 使用独立的预览窗口

**选择**: 方案1 - 在 PreviewRenderUtility 中渲染

**理由**:
- 与现有动画预览集成
- 无需额外窗口，用户体验一致
- 实现简单

**影响**:
- 需要确保特效在预览环境中正确渲染
- 可能需要处理特效的层级和遮挡

## 实现细节

### VFXPreviewManager（新建）

**职责**: 管理编辑器中的特效预览

**核心方法**:
```csharp
public class VFXPreviewManager
{
    private Dictionary<string, GameObject> _activeVFX = new Dictionary<string, GameObject>();
    private GameObject _previewInstance; // 预览模型引用
    
    public void PlayVFX(int frame, VFXEventData data, GameObject target)
    {
        // 加载特效资源
        // 实例化特效
        // 设置参数
        // 绑定到目标
    }
    
    public void StopVFX(int frame)
    {
        // 停止并清理特效
    }
    
    public void Update(float deltaTime)
    {
        // 更新所有活跃特效
        // 清理已结束的特效
    }
    
    public void ClearAll()
    {
        // 清理所有特效
    }
}
```

### VFXTriggerEventData（新建，事件数据）

**职责**: Logic 层向 View 层传递特效触发信息

**位置**: `AstrumProj/Assets/Script/CommonBase/Events/` 或 `AstrumView/Events/`

```csharp
public class VFXTriggerEventData
{
    public long EntityId;           // 实体ID
    public string ResourcePath;     // 特效资源路径
    public Vector3 PositionOffset;  // 位置偏移
    public Vector3 Rotation;         // 旋转
    public float Scale;              // 缩放
    public float PlaybackSpeed;      // 播放速度
    public bool FollowCharacter;    // 是否跟随角色
    public bool Loop;                // 是否循环
}
```

### VFXManager（新建，View层）

**职责**: 管理运行时的特效播放

**位置**: `AstrumProj/Assets/Script/AstrumView/Managers/VFXManager.cs`

**核心方法**:
```csharp
namespace Astrum.View.Managers
{
    public class VFXManager : Singleton<VFXManager>
    {
        private Dictionary<int, GameObject> _activeVFX = new Dictionary<int, GameObject>();
        private Stage _currentStage;
        
        public void Initialize(Stage stage)
        {
            _currentStage = stage;
            // 订阅特效触发事件
            EventSystem.Instance.Subscribe<VFXTriggerEventData>(OnVFXTriggered);
        }
        
        private void OnVFXTriggered(VFXTriggerEventData eventData)
        {
            // 通过 EntityId 获取 EntityView
            var entityView = _currentStage.GetEntityView(eventData.EntityId);
            if (entityView != null)
            {
                PlayVFX(entityView, eventData);
            }
        }
        
        public void PlayVFX(EntityView target, VFXTriggerEventData data)
        {
            // 加载特效资源
            // 实例化特效
            // 设置参数
            // 绑定到 EntityView
        }
        
        public void StopVFX(int instanceId)
        {
            // 停止并清理特效
        }
        
        public void Update()
        {
            // 更新所有活跃特效
            // 清理已结束的特效
        }
    }
}
```

### SkillActionEditorData 扩展（重构）

**重构 SyncFromTimelineEvents 方法**:
```csharp
public void SyncFromTimelineEvents(List<TimelineEvent> events)
{
    var triggerFrames = new List<TriggerFrameData>();
    
    foreach (var evt in events)
    {
        TriggerFrameData data = null;
        
        switch (evt.TrackType)
        {
            case "SkillEffect":
                data = ConvertSkillEffectToTriggerFrame(evt);
                break;
            case "VFX":
                data = ConvertVFXToTriggerFrame(evt);
                break;
            case "SFX":
                data = ConvertSFXToTriggerFrame(evt);
                break;
            // ... 其他类型
        }
        
        // 如果单帧事件，使用 Frame 字段；多帧事件使用 StartFrame/EndFrame
        if (evt.IsSingleFrame())
        {
            data.Frame = evt.StartFrame;
        }
        else
        {
            data.StartFrame = evt.StartFrame;
            data.EndFrame = evt.EndFrame;
        }
        
        if (data != null)
            triggerFrames.Add(data);
    }
    
    // 序列化为 JSON
    TriggerFrames = JsonUtility.ToJson(triggerFrames);
}

private TriggerFrameData ConvertSkillEffectToTriggerFrame(TimelineEvent evt)
{
    var effectData = evt.GetEventData<SkillEffectEventData>();
    return new TriggerFrameData
    {
        Type = "SkillEffect",
        TriggerType = effectData.TriggerType, // Collision, Direct, Condition
        EffectId = effectData.EffectId,
        CollisionInfo = effectData.CollisionInfo
    };
}

private TriggerFrameData ConvertVFXToTriggerFrame(TimelineEvent evt)
{
    var vfxData = evt.GetEventData<VFXEventData>();
    return new TriggerFrameData
    {
        Type = "VFX",
        ResourcePath = vfxData.ResourcePath,
        PositionOffset = vfxData.PositionOffset,
        Rotation = vfxData.Rotation,
        Scale = vfxData.Scale,
        PlaybackSpeed = vfxData.PlaybackSpeed,
        FollowCharacter = vfxData.FollowCharacter,
        Loop = vfxData.Loop
    };
}
```

**新增 BuildTimelineFromTriggerFrames 方法**:
```csharp
public List<TimelineEvent> BuildTimelineFromTriggerFrames()
{
    var events = new List<TimelineEvent>();
    
    // 解析 JSON
    var triggerFrames = JsonUtility.FromJson<List<TriggerFrameData>>(TriggerFrames);
    
    foreach (var data in triggerFrames)
    {
        TimelineEvent evt = null;
        
        switch (data.Type)
        {
            case "SkillEffect":
                evt = ConvertSkillEffectToTimelineEvent(data);
                break;
            case "VFX":
                evt = ConvertVFXToTimelineEvent(data);
                break;
            case "SFX":
                evt = ConvertSFXToTimelineEvent(data);
                break;
            // ... 其他类型
        }
        
        if (evt != null)
            events.Add(evt);
    }
    
    return events;
}
```

### SkillExecutorCapability 扩展（重构）

**重构 ProcessFrame 方法**:
```csharp
private void ProcessFrame(Entity caster, SkillActionInfo skillAction, int currentFrame)
{
    // 解析 JSON 格式的 triggerFrames
    var triggerFrames = ParseTriggerFramesJSON(skillAction.TriggerFrames);
    
    // 过滤当前帧的触发
    var triggersAtFrame = triggerFrames
        .Where(t => IsFrameInRange(currentFrame, t))
        .ToList();
    
    foreach (var trigger in triggersAtFrame)
    {
        switch (trigger.Type)
        {
            case "SkillEffect":
                ProcessSkillEffectTrigger(caster, skillAction, trigger);
                break;
            case "VFX":
                ProcessVFXTrigger(caster, trigger);
                break;
            case "SFX":
                ProcessSFXTrigger(trigger);
                break;
        }
    }
}

private void ProcessSkillEffectTrigger(Entity caster, SkillActionInfo skillAction, TriggerFrameData trigger)
{
    // 根据技能效果的触发方式进一步分发
    switch (trigger.TriggerType)
    {
        case "Collision":
            ProcessCollisionTrigger(caster, skillAction, trigger);
            break;
        case "Direct":
            ProcessDirectTrigger(caster, trigger);
            break;
        case "Condition":
            ProcessConditionTrigger(caster, skillAction, trigger);
            break;
    }
}

private void ProcessVFXTrigger(Entity caster, TriggerFrameData trigger)
{
    // 发布特效触发事件，由 View 层的 VFXManager 处理
    var eventData = new VFXTriggerEventData
    {
        EntityId = caster.UniqueId,
        ResourcePath = trigger.ResourcePath,
        PositionOffset = trigger.PositionOffset ?? Vector3.zero,
        Rotation = trigger.Rotation ?? Vector3.zero,
        Scale = trigger.Scale ?? 1.0f,
        PlaybackSpeed = trigger.PlaybackSpeed ?? 1.0f,
        FollowCharacter = trigger.FollowCharacter ?? false,
        Loop = trigger.Loop ?? false
    };
    
    EventSystem.Instance.Publish(eventData);
}
```

## 数据序列化格式

### TriggerFrames JSON 格式（统一）

**完整 JSON 结构**:
```json
[
  {
    "frame": 10,
    "type": "SkillEffect",
    "triggerType": "Collision",
    "effectId": 4001,
    "collisionInfo": "Box:5x2x1"
  },
  {
    "startFrame": 5,
    "endFrame": 10,
    "type": "VFX",
    "resourcePath": "Effects/SwordHit",
    "positionOffset": [0, 1, 0],
    "rotation": [0, 90, 0],
    "scale": 1.5,
    "playbackSpeed": 1.2,
    "followCharacter": true,
    "loop": false
  },
  {
    "frame": 20,
    "type": "SkillEffect",
    "triggerType": "Direct",
    "effectId": 4002
  },
  {
    "frame": 25,
    "type": "SFX",
    "resourcePath": "Sounds/SwordSwing",
    "volume": 0.8
  }
]
```

**字段规则**:
- 单帧触发：使用 `frame` 字段
- 多帧范围：使用 `startFrame` 和 `endFrame` 字段
- `type` 字段必需，顶层类型：`SkillEffect`（技能效果）、`VFX`（特效）、`SFX`（音效）等
- **SkillEffect 类型**：必须包含 `triggerType` 字段（`Collision`、`Direct`、`Condition`）
- 类型特定字段按需设置，未设置的使用默认值

**示例（仅 VFX）**:
```json
[
  {
    "frame": 10,
    "type": "VFX",
    "resourcePath": "Effects/SwordHit"
  },
  {
    "startFrame": 20,
    "endFrame": 30,
    "type": "VFX",
    "resourcePath": "Effects/Explosion",
    "positionOffset": [0, 1, 0],
    "scale": 2.0
  },
  {
    "frame": 40,
    "type": "VFX",
    "resourcePath": "Effects/Charge",
    "followCharacter": true,
    "loop": true
  }
]
```

### 数据格式转换

**说明**: 由于数据量较少，不实现自动兼容逻辑。现有表格数据需要手动转换为 JSON 格式。

**转换步骤**:
1. 读取现有 CSV 中的 triggerFrames 字段（字符串格式）
2. 解析字符串格式数据
3. 转换为 JSON 格式
4. 更新 CSV 文件
5. 验证数据正确性

## 编辑器集成

### AnimationPreviewModule 扩展

**新增功能**:
- 监听时间轴播放头位置
- 检测 VFX 事件触发
- 调用 VFXPreviewManager 播放特效
- 清理已结束的特效

**关键代码**:
```csharp
private VFXPreviewManager _vfxPreviewManager;

private void UpdateVFXPreview()
{
    // 获取当前帧的所有 VFX 事件
    var vfxEvents = GetVFXEventsAtFrame(_currentFrame);
    
    foreach (var evt in vfxEvents)
    {
        var data = evt.GetEventData<VFXEventData>();
        _vfxPreviewManager.PlayVFX(_currentFrame, data, _previewInstance);
    }
    
    // 更新特效状态
    _vfxPreviewManager.Update(Time.deltaTime);
}
```

## 运行时集成

### SkillExecutorCapability 扩展

**修改 ProcessFrame 方法**:
```csharp
private void ProcessFrame(Entity caster, SkillActionInfo skillAction, int currentFrame)
{
    // 处理技能效果触发（已有）
    ProcessTriggerEffects(caster, skillAction, currentFrame);
    
    // 处理特效触发（新增）
    ProcessVFXTriggers(caster, skillAction, currentFrame);
}
```

## 相关文档

- [技能效果运行时](../../02-CombatSystem%20战斗系统/技能系统/Skill-Effect-Runtime%20技能效果运行时.md)
- [编辑器工具文档](../README.md)
- [文档编写规范](../../README.md)

---

---

*文档版本：v1.3*  
*创建时间：2025-01-19*  
*最后更新：2025-01-19*  
*状态：设计完成*  
*Owner*: AI Assistant  
*变更摘要*: 修正 VFXManager 为 View 层组件，使用 EntityView 作为目标，通过事件系统进行 Logic 层到 View 层的通信

