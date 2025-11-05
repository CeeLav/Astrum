# SkillDisplacementCapability 技能位移能力

## 1. 概述

`SkillDisplacementCapability` 是专门处理技能释放时位移逻辑的能力组件。它基于动画根节点位移数据（Root Motion Data），在技能动作执行过程中逐帧应用位移和旋转，实现技能的前冲、后跳、翻滚等位移效果。

### 1.1 核心职责

- **技能位移应用**：根据技能动作配置的根节点位移数据，每逻辑帧应用增量位移
- **位移方向转换**：将动画的局部空间位移转换为世界空间位移（基于角色朝向）
- **旋转处理**：支持技能动作中的根节点旋转（如翻滚、旋转攻击）
- **与普通移动区分**：技能位移期间，可以禁用或混合普通移动能力

### 1.2 与现有能力的关系

| 能力 | 职责 | 关系 |
|------|------|------|
| `MovementCapability` | 处理普通移动（行走、跑步） | 并行运行，优先级冲突时技能位移优先 |
| `SkillExecutorCapability` | 处理技能触发帧逻辑（碰撞、特效） | 并行运行，配合使用 |
| `ActionCapability` | 管理动作状态和帧推进 | 依赖：读取当前动作和帧索引 |

## 2. 实现方案

### 2.1 数据结构

```csharp
namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 技能位移能力 - 处理技能释放时的位移逻辑
    /// </summary>
    [MemoryPackable]
    public partial class SkillDisplacementCapability : Capability
    {
        public SkillDisplacementCapability()
        {
            Priority = 150; // 优先级高于 MovementCapability (100)
        }
        
        // ... 实现方法 ...
    }
}
```

### 2.2 核心逻辑

#### 2.2.1 Tick 方法 - 每帧更新位移

```csharp
public override void Tick()
{
    if (!CanExecute()) return;
    
    // 1. 获取当前动作信息
    var actionComponent = GetOwnerComponent<ActionComponent>();
    if (actionComponent == null || actionComponent.CurrentAction == null)
    {
        return;
    }
    
    // 2. 检查当前动作是否是技能动作
    if (!(actionComponent.CurrentAction is SkillActionInfo skillAction))
    {
        return;
    }
    
    // 3. 检查是否有根节点位移数据
    if (skillAction.RootMotionData == null || 
        !skillAction.RootMotionData.HasMotion)
    {
        return;
    }
    
    // 4. 应用当前帧的位移
    int currentFrame = actionComponent.CurrentFrame;
    ApplyRootMotion(skillAction, currentFrame);
}
```

#### 2.2.2 ApplyRootMotion - 应用根节点位移

```csharp
/// <summary>
/// 应用动画根节点位移
/// </summary>
/// <param name="skillAction">技能动作信息</param>
/// <param name="currentFrame">当前帧索引</param>
private void ApplyRootMotion(SkillActionInfo skillAction, int currentFrame)
{
    // 1. 检查帧索引有效性
    if (currentFrame < 0 || 
        currentFrame >= skillAction.RootMotionData.Frames.Count)
    {
        return;
    }
    
    // 2. 获取当前帧的位移数据
    var frameData = skillAction.RootMotionData.Frames[currentFrame];
    
    // 3. 获取实体的变换组件
    var transComponent = GetOwnerComponent<TransComponent>();
    if (transComponent == null)
    {
        return;
    }
    
    // 4. 应用增量位移（局部空间转世界空间）
    TSVector worldDeltaPosition = TransformDeltaToWorld(
        frameData.DeltaPosition, 
        transComponent.Rotation
    );
    
    transComponent.Position = transComponent.Position + worldDeltaPosition;
    
    // 5. 应用增量旋转（如果动画包含根节点旋转）
    if (frameData.DeltaRotation != TSQuaternion.identity)
    {
        transComponent.Rotation = transComponent.Rotation * frameData.DeltaRotation;
    }
    
    // 6. 【物理世界同步】更新实体在物理世界中的位置
    if (OwnerEntity is AstrumEntity astrumEntity && astrumEntity.World != null)
    {
        astrumEntity.World.HitSystem?.UpdateEntityPosition(astrumEntity);
    }
}

/// <summary>
/// 将局部空间的增量位移转换为世界空间
/// </summary>
/// <param name="localDelta">局部空间位移（定点数）</param>
/// <param name="rotation">角色当前朝向（四元数）</param>
/// <returns>世界空间位移</returns>
private TSVector TransformDeltaToWorld(TSVector localDelta, TSQuaternion rotation)
{
    // 将局部位移旋转到世界空间
    return TSQuaternion.op_Multiply(rotation, localDelta);
}
```

#### 2.2.3 CanExecute - 执行条件检查

```csharp
/// <summary>
/// 检查是否可以执行技能位移
/// </summary>
public override bool CanExecute()
{
    if (!base.CanExecute()) return false;
    
    // 检查必需的组件是否存在
    return OwnerHasComponent<ActionComponent>() && 
           OwnerHasComponent<TransComponent>();
}
```

### 2.3 完整代码实现

```csharp
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using TrueSync;
using MemoryPack;
using AstrumEntity = Astrum.LogicCore.Core.Entity;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 技能位移能力 - 处理技能释放时的位移逻辑
    /// 基于动画根节点位移数据（Root Motion Data），在技能动作执行过程中逐帧应用位移和旋转
    /// </summary>
    [MemoryPackable]
    public partial class SkillDisplacementCapability : Capability
    {
        public SkillDisplacementCapability()
        {
            Priority = 150; // 优先级高于 MovementCapability (100)，确保技能位移优先
        }
        
        public override void Initialize()
        {
            base.Initialize();
            ASLogger.Instance.Debug($"SkillDisplacementCapability initialized for entity {OwnerEntity?.UniqueId}");
        }
        
        public override void Tick()
        {
            if (!CanExecute()) return;
            
            // 1. 获取当前动作信息
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null || actionComponent.CurrentAction == null)
            {
                return;
            }
            
            // 2. 检查当前动作是否是技能动作
            if (!(actionComponent.CurrentAction is SkillActionInfo skillAction))
            {
                return;
            }
            
            // 3. 检查是否有根节点位移数据
            if (skillAction.RootMotionData == null)
            {
                return;
            }
            
            // 检查数据有效性（HasMotion 属性或直接检查帧数）
            if (skillAction.RootMotionData.Frames == null || 
                skillAction.RootMotionData.Frames.Count == 0)
            {
                return;
            }
            
            // 4. 应用当前帧的位移
            int currentFrame = actionComponent.CurrentFrame;
            ApplyRootMotion(skillAction, currentFrame);
        }
        
        /// <summary>
        /// 应用动画根节点位移
        /// </summary>
        /// <param name="skillAction">技能动作信息</param>
        /// <param name="currentFrame">当前帧索引</param>
        private void ApplyRootMotion(SkillActionInfo skillAction, int currentFrame)
        {
            // 1. 检查帧索引有效性
            if (currentFrame < 0 || 
                currentFrame >= skillAction.RootMotionData.Frames.Count)
            {
                return;
            }
            
            // 2. 获取当前帧的位移数据
            var frameData = skillAction.RootMotionData.Frames[currentFrame];
            
            // 3. 获取实体的变换组件
            var transComponent = GetOwnerComponent<TransComponent>();
            if (transComponent == null)
            {
                return;
            }
            
            // 4. 应用增量位移（局部空间转世界空间）
            TSVector worldDeltaPosition = TransformDeltaToWorld(
                frameData.DeltaPosition, 
                transComponent.Rotation
            );
            
            transComponent.Position = transComponent.Position + worldDeltaPosition;
            
            // 5. 应用增量旋转（如果动画包含根节点旋转）
            if (frameData.DeltaRotation != TSQuaternion.identity)
            {
                transComponent.Rotation = transComponent.Rotation * frameData.DeltaRotation;
            }
            
            // 6. 【物理世界同步】更新实体在物理世界中的位置
            if (OwnerEntity is AstrumEntity astrumEntity && astrumEntity.World != null)
            {
                astrumEntity.World.HitSystem?.UpdateEntityPosition(astrumEntity);
            }
        }
        
        /// <summary>
        /// 将局部空间的增量位移转换为世界空间
        /// </summary>
        /// <param name="localDelta">局部空间位移（定点数）</param>
        /// <param name="rotation">角色当前朝向（四元数）</param>
        /// <returns>世界空间位移</returns>
        private TSVector TransformDeltaToWorld(TSVector localDelta, TSQuaternion rotation)
        {
            // 将局部位移旋转到世界空间
            return TSQuaternion.op_Multiply(rotation, localDelta);
        }
        
        /// <summary>
        /// 检查是否可以执行技能位移
        /// </summary>
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            
            // 检查必需的组件是否存在
            return OwnerHasComponent<ActionComponent>() && 
                   OwnerHasComponent<TransComponent>();
        }
        
        /// <summary>
        /// 检查技能位移是否正在激活（运行时获取）
        /// </summary>
        /// <returns>是否激活</returns>
        public bool IsDisplacementActive()
        {
            if (!CanExecute()) return false;
            
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null || actionComponent.CurrentAction == null)
            {
                return false;
            }
            
            if (!(actionComponent.CurrentAction is SkillActionInfo skillAction))
            {
                return false;
            }
            
            return skillAction.RootMotionData != null &&
                   skillAction.RootMotionData.Frames != null &&
                   skillAction.RootMotionData.Frames.Count > 0;
        }
        
        /// <summary>
        /// 获取当前技能动作ID（运行时获取）
        /// </summary>
        /// <returns>当前技能动作ID，如果没有激活则返回0</returns>
        public int GetCurrentSkillActionId()
        {
            if (!CanExecute()) return 0;
            
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null || actionComponent.CurrentAction == null)
            {
                return 0;
            }
            
            if (actionComponent.CurrentAction is SkillActionInfo skillAction)
            {
                return skillAction.Id;
            }
            
            return 0;
        }
    }
}
```

## 3. 与现有系统的集成

### 3.1 与 MovementCapability 的协调

**优先级机制**：
- `SkillDisplacementCapability` 优先级为 150
- `MovementCapability` 优先级为 100
- 系统会按优先级顺序执行，但两者可以并行运行

**协作方案**（可选扩展）：

```csharp
// 在 MovementCapability 中可以检查技能位移状态
var skillDisplacement = OwnerEntity?.GetCapability<SkillDisplacementCapability>();
if (skillDisplacement != null && skillDisplacement.IsDisplacementActive())
{
    // 技能位移激活时，可以禁用或减弱普通移动
    // 例如：只允许小幅调整方向，不允许大幅移动
}
```

### 3.2 与 ActionCapability 的依赖

`SkillDisplacementCapability` 依赖于 `ActionComponent` 提供：
- 当前动作信息（`CurrentAction`）
- 当前帧索引（`CurrentFrame`）

这些数据由 `ActionCapability` 在每逻辑帧更新。

### 3.3 与 SkillExecutorCapability 的配合

两个能力并行运行，互不干扰：

- **SkillDisplacementCapability**：处理位移逻辑
- **SkillExecutorCapability**：处理触发帧逻辑（碰撞检测、特效播放等）

### 3.4 Archetype 配置

在实体原型（Archetype）中添加此能力：

```csharp
// 例如：在 RoleArchetype 或 CombatantArchetype 中
.AddCapability<SkillDisplacementCapability>()
```

## 4. 数据流

### 4.1 数据来源

```
配置表 (SkillActionTable)
    ↓
ActionConfig.GetAction() 
    ↓
SkillActionInfo.RootMotionData (运行时数据，定点数)
    ↓
SkillDisplacementCapability.ApplyRootMotion()
    ↓
TransComponent.Position/Rotation (实体位置/朝向)
    ↓
物理世界同步 (HitSystem.UpdateEntityPosition)
```

### 4.2 位移应用流程

```
每逻辑帧 (20 FPS):
1. ActionCapability.Tick() → 更新 ActionComponent.CurrentFrame
2. SkillDisplacementCapability.Tick()
   ├─ 读取 ActionComponent.CurrentAction
   ├─ 检查是否为 SkillActionInfo
   ├─ 检查是否有 RootMotionData
   ├─ 读取当前帧的位移数据 (DeltaPosition, DeltaRotation)
   ├─ 局部空间转世界空间
   └─ 更新 TransComponent.Position/Rotation
3. MovementCapability.Tick() → 处理普通移动（如果允许）
```

## 5. 注意事项

### 5.1 位移方向处理

- **局部空间 vs 世界空间**：
  - 动画中的位移通常是局部空间的（相对于角色朝向）
  - 必须根据角色的当前朝向转换到世界空间
  - 使用 `TransformDeltaToWorld()` 方法转换

- **朝向转换时机**：
  - 位移应用时使用 `transComponent.Rotation` 作为当前朝向
  - 如果技能同时包含旋转，旋转应先于位移应用（或合并应用）

### 5.2 性能考虑

- **数据读取**：每逻辑帧读取一次位移数据，开销很小
- **数学计算**：四元数乘法（位移转换）和向量加法，性能开销可忽略
- **物理同步**：`HitSystem.UpdateEntityPosition()` 的开销需要评估

### 5.3 边界情况处理

1. **帧索引越界**：
   - 检查 `currentFrame` 是否在有效范围内
   - 越界时直接返回，不应用位移

2. **缺失位移数据**：
   - 如果技能动作没有配置位移数据，`RootMotionData` 为 `null`
   - 能力会优雅降级，不影响正常流程

3. **动作切换**：
   - 动作切换时，`IsDisplacementActive()` 会自动返回新动作的状态（运行时获取）
   - 新动作如果没有位移数据，不会应用位移，`IsDisplacementActive()` 返回 `false`

4. **旋转为 identity**：
   - 大多数技能不需要旋转，检查 `DeltaRotation != TSQuaternion.identity`
   - 避免不必要的四元数乘法计算

### 5.4 与视觉跟随方案的配合

参考 [技能动画视觉跟随方案](./技能动画视觉跟随方案.md)：

- **逻辑层**：`SkillDisplacementCapability` 更新 `TransComponent.Position`（权威位置）
- **视觉层**：`TransViewComponent` 通过视觉跟随机制平滑显示
- **分离原则**：逻辑层控制位移，视觉层平滑跟随，两者完全分离

## 6. 扩展方向

### 6.1 位移插值

如果需要更平滑的位移（帧间插值）：

```csharp
// 可以读取上一帧和当前帧的数据，进行插值
// 但需要记录上一帧的位移数据
```

### 6.2 位移混合

与普通移动混合：

```csharp
// 技能位移期间，允许小幅度的普通移动调整
// 例如：前进技能中可以左右微调方向
```

### 6.3 位移限制

添加位移边界检查：

```csharp
// 限制技能位移的最大距离
// 防止异常动画导致角色飞出地图
```

### 6.4 位移轨迹可视化

在编辑器中可视化位移轨迹：

```csharp
// 参考：动画根节点位移提取方案
// 可以在编辑器中绘制位移路径，方便策划调整
```

## 7. 测试建议

### 7.1 单元测试

- 测试局部空间到世界空间的转换
- 测试帧索引边界情况
- 测试旋转应用的正确性

### 7.2 集成测试

- 测试与 `MovementCapability` 的并行运行
- 测试与 `ActionCapability` 的依赖关系
- 测试物理世界同步是否正确

### 7.3 游戏测试

- 测试各种技能的前冲、后跳效果
- 测试翻滚、旋转攻击等包含旋转的技能
- 测试位移与碰撞的交互

## 8. 相关文档

- [技能动画视觉跟随方案](./技能动画视觉跟随方案.md) - 视觉层平滑跟随逻辑层位移
- [动画根节点位移提取方案](../04-EditorTools%20编辑器工具/技能动作编辑器/动画根节点位移提取方案.md) - 位移数据提取和存储
- [ECC 系统说明](../05-CoreArchitecture%20核心架构/ECC/ECC-System%20ECC结构说明.md) - 实体-组件-能力架构

---

*文档版本：v1.0*  
*创建时间：2025-01-16*  
*状态：方案设计完成，待实现*

