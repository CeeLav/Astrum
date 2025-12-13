---
name: 技能位移表现
overview: 在 PredictedMovementViewComponent 内新增 SkillRootMotion 模式：技能动作期间用 Animator.deltaPosition 产生视觉偏移，逻辑位置仍为权威，并保留逻辑帧停更时惯性前进与横向纠偏规则。
todos:
  - id: add_rootmotion_state
    content: 在 PredictedMovementViewComponent 中添加 RootMotion 相关状态变量和参数
    status: pending
  - id: add_action_component_dependency
    content: 添加 ActionComponent 到依赖组件列表并同步数据
    status: pending
  - id: implement_mode_determination
    content: 实现技能动作模式判定逻辑
    status: pending
  - id: implement_get_animator
    content: 实现 GetAnimator 方法，支持从 AnimationViewComponent 获取 Animator
    status: pending
  - id: update_oninitialize
    content: 更新 OnInitialize 方法，初始化新的状态变量
    status: pending
  - id: implement_update_rootmotion_advanced
    content: 实现逻辑帧推进时的 RootMotion 处理
    status: pending
  - id: implement_update_rootmotion_freeze
    content: 实现逻辑帧停更时的 RootMotion 处理（惯性前进 + 横向纠偏）
    status: pending
  - id: update_update_method
    content: 更新 OnUpdate 方法，整合 RootMotion 逻辑和原有移动逻辑
    status: pending
  - id: update_onreset
    content: 更新 OnReset 方法，正确重置 RootMotion 相关状态
    status: pending
  - id: test_and_tune
    content: 测试技能位移表现效果，调整参数值
    status: pending
---

# 技能位移表现（RootMotion）集成计划

## 目标

在不新增额外 ViewComponent 的前提下，把技能位移期间的表现逻辑集成进 `PredictedMovementViewComponent`：

- 技能动作期间：使用 `Animator.deltaPosition` 作为视觉偏移来源（参考原 `TransViewComponent` 的 RootMotion 逻辑）
- 保持逻辑权威位置：最终位置 = `posLogic + visualOffset`
- 继续遵守既有约束：
  - `IsMoving=false` 时不更新位置（除 `hardSnapDistance` 例外）
  - 逻辑帧停更但 `IsMoving=true` 时继续惯性前进，纠偏不后拉

## 需要修改的文件

- **实现**：`AstrumProj/Assets/Script/AstrumView/Components/PredictedMovementViewComponent.cs`
- **参考逻辑**（不改动，仅参考）：`AstrumProj/Assets/Script/AstrumView/Components/TransViewComponent.cs`

## 实现步骤（详细拆分）

### 1. 添加 RootMotion 相关状态变量和参数

在 `PredictedMovementViewComponent` 类中添加以下内容：

```csharp
// RootMotion 相关状态
private Vector3 _visualOffset;
private bool _isInSkillMotion;

// 动画相关
private Animator _cachedAnimator;

// RootMotion 参数（可在 Inspector 中调整）
public float motionBlendWeight = 1.0f;  // 动画 RootMotion 权重
```

### 2. 添加 ActionComponent 依赖

更新组件依赖列表：

```csharp
public override int[] GetWatchedComponentIds()
{
    return new[] { MovementComponent.ComponentTypeId, ActionComponent.ComponentTypeId };
}
```

添加 ActionComponent 数据同步：

```csharp
public override void SyncDataFromComponent(int componentTypeId)
{
    if (OwnerEntity == null) return;

    if (componentTypeId == MovementComponent.ComponentTypeId)
    {
        var move = OwnerEntity.GetComponent<MovementComponent>();
        if (move != null)
        {
            _isMovingLogicCached = move.IsMoving;
        }
    }
    else if (componentTypeId == ActionComponent.ComponentTypeId)
    {
        // 检查是否正在执行技能动作
        var actionComp = OwnerEntity.GetComponent<ActionComponent>();
        if (actionComp != null && actionComp.CurrentAction != null)
        {
            _isInSkillMotion = (actionComp.CurrentAction.SkillExtension != null);
        }
        else
        {
            _isInSkillMotion = false;
        }
    }
}
```

### 3. 实现技能动作模式判定逻辑

添加模式判定方法：

```csharp
/// <summary>
/// 判定当前是否处于技能动作模式
/// </summary>
private bool IsSkillMotionActive()
{
    if (!_isMovingLogicCached) return false;
    
    // 如果已经通过 SyncDataFromComponent 同步了状态，直接使用
    if (_isInSkillMotion) return true;
    
    // 兜底检查：直接从 ActionComponent 获取状态
    var entity = OwnerEntity;
    if (entity == null) return false;
    
    var actionComp = entity.GetComponent<ActionComponent>();
    if (actionComp != null && actionComp.CurrentAction != null)
    {
        return (actionComp.CurrentAction.SkillExtension != null);
    }
    
    return false;
}
```

### 4. 实现 GetAnimator 方法

```csharp
/// <summary>
/// 获取 Animator 引用（优先从 AnimationViewComponent 获取）
/// </summary>
private Animator GetAnimator()
{
    if (_cachedAnimator != null) return _cachedAnimator;
    
    if (_ownerEntityView == null) return null;
    
    // 方式1：从 AnimationViewComponent 获取（推荐）
    var animViewComponent = _ownerEntityView.ViewComponents
        .FirstOrDefault(c => c is AnimationViewComponent) as AnimationViewComponent;
    
    if (animViewComponent != null)
    {
        var animator = animViewComponent.GetAnimator();
        if (animator != null)
        {
            _cachedAnimator = animator;
            return animator;
        }
    }
    
    // 方式2：直接从 GameObject 获取
    _cachedAnimator = _ownerEntityView.GameObject.GetComponent<Animator>();
    return _cachedAnimator;
}
```

### 5. 更新 OnInitialize 方法

```csharp
protected override void OnInitialize()
{
    // 保留原有初始化逻辑...
    
    // 初始化 RootMotion 相关状态
    _visualOffset = Vector3.zero;
    _isInSkillMotion = false;
    _cachedAnimator = null;
    
    // 其他原有初始化代码...
}
```

### 6. 实现 RootMotion 处理逻辑（拆分 Update）

#### 6.1 逻辑帧推进时的 RootMotion 处理

```csharp
/// <summary>
/// 逻辑帧推进时的 RootMotion 处理
/// </summary>
private void UpdateRootMotionWhenLogicAdvanced(float deltaTime, Vector3 posLogic)
{
    // 更新视觉偏移：插值到当前逻辑点并转换为 offset
    Vector3 interpolatedPos = Vector3.Lerp(_posVisual, posLogic, posFixLerp * deltaTime);
    _visualOffset = interpolatedPos - posLogic;
    
    // 应用动画位移
    Animator animator = GetAnimator();
    if (animator != null && animator.enabled)
    {
        Vector3 animDelta = animator.deltaPosition * motionBlendWeight;
        _visualOffset += animDelta;
    }
    
    // 限制最大视觉偏移
    if (_visualOffset.magnitude > hardSnapDistance)
    {
        _visualOffset = _visualOffset.normalized * hardSnapDistance;
    }
    
    // 计算最终视觉位置
    _posVisual = posLogic + _visualOffset;
    
    // 更新位置
    _ownerEntityView.SetWorldPosition(_posVisual);
}
```

#### 6.2 逻辑帧停更时的 RootMotion 处理

```csharp
/// <summary>
/// 逻辑帧停更时的 RootMotion 处理（惯性前进 + 横向纠偏）
/// </summary>
private void UpdateRootMotionWhenLogicFrozen(float deltaTime, Vector3 posLogic)
{
    // 保持惯性前进
    _posVisual += _dirVisual * _speedVisual * deltaTime;
    
    // 应用动画位移
    Animator animator = GetAnimator();
    if (animator != null && animator.enabled)
    {
        Vector3 animDelta = animator.deltaPosition * motionBlendWeight;
        _posVisual += animDelta;
    }
    
    // 横向纠偏（仅修正垂直于运动方向的误差）
    var error = posLogic - _posVisual;
    var errorPerp = error - _dirVisual * Vector3.Dot(error, _dirVisual);
    
    if (errorPerp.sqrMagnitude > 1e-8f)
    {
        // 应用横向纠偏
        _posVisual += errorPerp.normalized * wCorrectionDirection * deltaTime;
        
        // 更新视觉偏移
        _visualOffset = _posVisual - posLogic;
    }
    
    // 极端情况处理：偏差过大时重置
    if ((_posVisual - posLogic).magnitude > hardSnapDistance)
    {
        _posVisual = posLogic;
        _visualOffset = Vector3.zero;
    }
    
    // 更新位置
    _ownerEntityView.SetWorldPosition(_posVisual);
}
```

### 7. 更新 OnUpdate 方法（整合所有逻辑）

```csharp
protected override void OnUpdate(float deltaTime)
{
    if (!_isEnabled || _ownerEntityView == null) return;

    var entity = OwnerEntity;
    if (entity == null) return;

    var trans = entity.GetComponent<TransComponent>();
    if (trans == null) return;

    // 内部状态同步
    _posVisual = _ownerEntityView.GetWorldPosition();

    var posLogic = ToVector3(trans.Position);
    var logicFrame = entity.World?.CurFrame ?? _lastLogicFrameSeen;
    var logicFrameAdvanced = logicFrame != _lastLogicFrameSeen;

    // 更新速度缓存
    var moveComp = entity.GetComponent<MovementComponent>();
    if (moveComp != null)
    {
        _cachedSpeedLogic = moveComp.Speed.AsFloat();
    }

    // 应用旋转（始终从逻辑层同步）
    ApplyRotationFromLogic(trans);

    // 静止状态处理
    if (!_isMovingLogicCached)
    {
        if ((_posVisual - posLogic).magnitude > hardSnapDistance)
        {
            _posVisual = posLogic;
            _visualOffset = Vector3.zero;
            _ownerEntityView.SetWorldPosition(_posVisual);
        }
        return;
    }

    // 检查是否处于技能动作模式
    if (IsSkillMotionActive())
    {
        // 技能动作模式：使用 RootMotion 逻辑
        if (logicFrameAdvanced)
        {
            // 逻辑帧推进时的 RootMotion 处理
            UpdateRootMotionWhenLogicAdvanced(deltaTime, posLogic);
            
            // 更新逻辑帧缓存
            _lastPosLogicSeen = posLogic;
            _lastLogicFrameSeen = logicFrame;
            _speedVisual = _cachedSpeedLogic;
        }
        else
        {
            // 逻辑帧停更时的 RootMotion 处理
            UpdateRootMotionWhenLogicFrozen(deltaTime, posLogic);
        }
    }
    else
    {
        // 普通移动模式：使用原有逻辑
        if (logicFrameAdvanced)
        {
            // 原有逻辑帧推进处理...
        }
        else
        {
            // 原有逻辑帧停更处理...
        }
    }
}
```

### 8. 更新 OnReset 方法

```csharp
protected override void OnReset()
{
    // 保持原有 OnReset 逻辑...
    
    // 重置 RootMotion 相关状态
    _visualOffset = Vector3.zero;
    _isInSkillMotion = false;
    _cachedAnimator = null;
    
    // 其他原有重置代码...
}
```

### 9. 测试与调优

1. 运行项目，测试技能动作期间的位移表现
2. 调整参数（`motionBlendWeight`, `hardSnapDistance` 等）以获得最佳效果
3. 验证以下场景：
   - 技能动作期间的平滑位移
   - 逻辑帧停更时的惯性前进和横向纠偏
   - 技能结束后回归正常移动
   - 极端情况下的位置修正

## 代码注意事项

1. **保留原有逻辑**：确保普通移动模式的逻辑保持不变
2. **逻辑权威**：始终以逻辑层位置为最终权威，视觉偏移仅作为表现效果
3. **性能优化**：缓存 Animator 引用，避免每帧重复查找
4. **兼容性**：确保与现有 AnimationViewComponent 正确交互
5. **参数可配置**：关键参数应支持在 Inspector 中调整

## 预期效果

- 技能动作期间：角色位移由动画 RootMotion 驱动，视觉表现更流畅自然
- 普通移动期间：保持原有的预测移动逻辑
- 逻辑帧停更时：继续惯性前进并进行横向纠偏，避免后拉
- 整体效果：技能位移表现与逻辑位置完美结合，提供更好的游戏体验