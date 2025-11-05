# 特效轨道设计

## 概述

在技能动作编辑器中实现特效轨道功能，支持在编辑器中编辑和预览特效，并在运行时基于触发帧播放特效。

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
        └── VFXEvents - VFX事件列表
```

### 运行时架构

```
SkillExecutorCapability (扩展)
    └── ProcessVFXTriggers - 处理特效触发帧
        ├── 解析 VFXFrames 字符串
        ├── 检查当前帧是否有特效触发
        └── 调用 VFXManager 播放特效

VFXManager (新建)
    ├── PlayVFX - 播放特效
    ├── StopVFX - 停止特效
    └── UpdateVFX - 更新特效状态
```

## 数据结构

### VFXEventData（已有，需扩展）

**职责**: 存储特效事件的所有配置参数

**字段**:
- `ResourcePath` - 特效资源路径（已存在）
- `PositionOffset` - 位置偏移（已存在）
- `Rotation` - 旋转（已存在）
- `Scale` - 缩放（已存在）
- `PlaybackSpeed` - 播放速度（已存在）
- `FollowCharacter` - 是否跟随角色（已存在）
- `Loop` - 是否循环播放（已存在）
- `Note` - 备注（已存在）

**设计要点**:
- 复用现有 VFXEventData 结构
- 编辑器预览和运行时使用相同数据结构

### VFXFrameData（新建）

**职责**: 运行时特效触发帧数据

```csharp
public class VFXFrameData
{
    public int Frame;              // 触发帧
    public string ResourcePath;    // 特效资源路径
    public Vector3 PositionOffset; // 位置偏移
    public Vector3 Rotation;       // 旋转
    public float Scale;            // 缩放
    public float PlaybackSpeed;    // 播放速度
    public bool FollowCharacter;   // 是否跟随角色
    public bool Loop;              // 是否循环
}
```

### SkillActionTable 扩展

**新增字段**: `VFXFrames` (string)

**格式**: `"Frame10:VFX:path/to/effect1,Frame20:VFX:path/to/effect2"`

**序列化格式**:
```
Frame{帧号}:VFX:{资源路径}:{位置偏移}:{旋转}:{缩放}:{播放速度}:{跟随}:{循环}
```

**简化格式**（仅必需字段）:
```
Frame{帧号}:VFX:{资源路径}
```

可选参数使用 JSON 扩展格式：
```
Frame{帧号}:VFX:{资源路径}|{JSON参数}
```

## 流程设计

### 编辑器预览流程

```
用户播放动画
    ↓
AnimationPreviewModule.Update
    ↓
检查当前帧是否有 VFX 事件开始
    ↓
VFXPreviewManager.PlayVFX
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
    ├── 提取 VFX 轨道事件
    ├── 转换为 VFXFrameData 列表
    └── 序列化为 VFXFrames 字符串
    ↓
SkillActionDataWriter.WriteSkillActionTableCSV
    └── 写入 VFXFrames 字段到 CSV
```

### 运行时播放流程

```
SkillExecutorCapability.Tick
    ↓
检查当前帧
    ↓
解析 SkillActionInfo.VFXFrames
    ↓
查找当前帧的 VFX 触发
    ↓
VFXManager.PlayVFX
    ├── 加载特效资源
    ├── 实例化特效
    ├── 设置变换参数
    ├── 设置播放参数
    └── 如果跟随，绑定到角色实体
```

## 关键决策与取舍

**问题**: VFX 数据如何存储到配置表？

**备选**:
1. 使用字符串格式（类似 TriggerFrames）
2. 使用 JSON 字段存储完整事件列表
3. 新增独立表存储 VFX 事件

**选择**: 方案1 - 使用字符串格式

**理由**:
- 与现有 TriggerFrames 格式保持一致
- 简化配置表结构，无需新增字段类型
- 易于解析和维护

**影响**:
- 需要设计紧凑的字符串格式
- 参数较多时使用 JSON 扩展格式

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

### VFXManager（新建，运行时）

**职责**: 管理运行时的特效播放

**核心方法**:
```csharp
public class VFXManager
{
    private Dictionary<int, GameObject> _activeVFX = new Dictionary<int, GameObject>();
    
    public void PlayVFX(Entity target, VFXFrameData data)
    {
        // 加载特效资源
        // 实例化特效
        // 设置参数
        // 绑定到实体
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
```

### SkillActionEditorData 扩展

**新增方法**:
```csharp
public void SyncVFXFromTimelineEvents(List<TimelineEvent> events)
{
    // 提取 VFX 轨道事件
    // 转换为 VFXFrameData 列表
    // 序列化为 VFXFrames 字符串
}

public List<TimelineEvent> BuildVFXTimelineFromFrames()
{
    // 解析 VFXFrames 字符串
    // 转换为 TimelineEvent 列表
}
```

### SkillExecutorCapability 扩展

**新增方法**:
```csharp
private void ProcessVFXTriggers(Entity caster, SkillActionInfo skillAction, int currentFrame)
{
    // 解析 VFXFrames
    // 查找当前帧的触发
    // 调用 VFXManager.PlayVFX
}
```

## 数据序列化格式

### VFXFrames 字符串格式

**基础格式**:
```
Frame{帧号}:VFX:{资源路径}
```

**完整格式**（包含所有参数）:
```
Frame{帧号}:VFX:{资源路径}|{JSON参数}
```

**JSON参数格式**:
```json
{
    "positionOffset": [0, 1, 0],
    "rotation": [0, 90, 0],
    "scale": 1.5,
    "playbackSpeed": 1.2,
    "followCharacter": true,
    "loop": false
}
```

**示例**:
```
Frame10:VFX:Effects/SwordHit
Frame20:VFX:Effects/Explosion|{"positionOffset":[0,1,0],"scale":2.0}
Frame30:VFX:Effects/Charge|{"followCharacter":true,"loop":true}
```

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

*文档版本：v1.0*  
*创建时间：2025-01-19*  
*最后更新：2025-01-19*  
*状态：设计完成*  
*Owner*: AI Assistant  
*变更摘要*: 创建特效轨道技术设计文档

