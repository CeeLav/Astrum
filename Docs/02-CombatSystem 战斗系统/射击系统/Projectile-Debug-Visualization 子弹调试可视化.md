# Projectile Debug Visualization 子弹调试可视化

## 概述

为子弹系统添加了调试可视化功能，通过 Unity Scene 视图的 Gizmos 实时显示子弹轨迹、速度方向和碰撞检测射线，方便开发和调试。

## 功能特性

### 1. 射线碰撞可视化
- **青色线段**：显示每帧进行射线检测的路径（从 `LastPosition` 到当前 `Position`）
- **小球标记**：在射线中点绘制小球，便于观察射线长度
- 实时反映子弹的实际碰撞检测范围

### 2. 速度方向可视化
- **黄色箭头**：显示子弹当前的飞行方向（基于 `CurrentVelocity`）
- 箭头长度固定为 0.5 单位，方便观察
- 支持曲线轨迹（抛物线、追踪等），实时更新方向

### 3. 位置标记
- **红色球体**：标记子弹当前位置
- 球体半径 0.08 单位，清晰可见

### 4. 信息标签
在子弹上方显示详细信息：
- **Projectile ID**：子弹配置 ID
- **Frame**：当前帧数 / 总生命周期
- **Speed**：当前速度大小
- **Pierce**：已穿透数 / 最大穿透数

## 使用方法

### 自动启用
子弹实体在编辑器模式下会自动附加 `CollisionDebugViewComponent`，无需手动配置。

### 控制开关
在 Unity Inspector 中选择子弹的 GameObject，可以在 `CollisionDebugViewComponent` 中控制：

```csharp
public bool ShowProjectileTrajectory = true;  // 显示子弹轨迹
public bool ShowLabels = true;                // 显示信息标签
```

### 颜色配置
可以自定义可视化颜色：

```csharp
public Color ProjectileTrajectoryColor = new Color(1f, 1f, 0f, 0.8f); // 黄色（速度方向）
public Color ProjectileRaycastColor = new Color(0f, 1f, 1f, 0.8f);    // 青色（射线检测）
```

## 技术实现

### 组件结构
```
ProjectileViewArchetype
├── TransViewComponent           // 位置同步
├── ProjectileViewComponent      // 特效管理
└── CollisionDebugViewComponent  // 调试可视化（仅编辑器）
    └── CollisionGizmosDrawer    // Gizmos 绘制 MonoBehaviour
```

### 关键方法
- `DrawProjectileTrajectoryGizmos()`：绘制子弹轨迹
  - 绘制射线检测路径（LastPosition → Position）
  - 绘制速度方向箭头
  - 绘制位置标记和信息标签

### 数据来源
- `ProjectileComponent.LastPosition`：上一帧位置（射线起点）
- `TransComponent.Position`：当前位置（射线终点）
- `ProjectileComponent.CurrentVelocity`：当前速度（方向箭头）
- `ProjectileComponent.ElapsedFrames`、`LifeTime`、`PierceCount` 等：信息标签

## 调试场景

### 直线弹道
- 射线路径应该是连续的直线段
- 速度方向箭头应该保持一致

### 抛物线弹道
- 射线路径会呈现弧线
- 速度方向箭头会随重力逐渐向下倾斜

### 追踪弹道
- 射线路径会随目标移动而弯曲
- 速度方向箭头会动态调整指向目标

### 穿透检测
- Pierce 计数器会在命中后递增
- 达到 PierceCount 上限后子弹会被标记销毁

## 注意事项

1. **仅编辑器模式**：调试可视化功能仅在 `UNITY_EDITOR` 宏下编译，不会影响发布版本性能
2. **Scene 视图可见**：Gizmos 仅在 Scene 视图中显示，Game 视图不可见
3. **性能影响**：大量子弹同时存在时，Gizmos 绘制可能影响编辑器性能，可通过开关控制
4. **坐标转换**：所有坐标从逻辑层的 `TSVector` 转换为 Unity 的 `Vector3`

## 扩展建议

### 轨迹历史
可以添加轨迹历史记录功能，绘制子弹的完整飞行路径：

```csharp
private List<Vector3> _trajectoryHistory = new List<Vector3>();
private int _maxHistoryPoints = 50;

// 在 OnUpdate 中记录
_trajectoryHistory.Add(currentPos);
if (_trajectoryHistory.Count > _maxHistoryPoints)
    _trajectoryHistory.RemoveAt(0);

// 在 Gizmos 中绘制
for (int i = 1; i < _trajectoryHistory.Count; i++)
{
    Gizmos.DrawLine(_trajectoryHistory[i - 1], _trajectoryHistory[i]);
}
```

### 命中点标记
可以在子弹命中时记录命中点，并持续显示一段时间：

```csharp
private struct HitMark
{
    public Vector3 Position;
    public float Timestamp;
}

private List<HitMark> _hitMarks = new List<HitMark>();
```

## 相关文件

- `CollisionDebugViewComponent.cs`：调试可视化组件
- `CollisionGizmosDrawer.cs`：Gizmos 绘制 MonoBehaviour
- `ProjectileViewArchetype.cs`：子弹视图原型（包含调试组件）
- `ProjectileComponent.cs`：子弹逻辑组件（提供调试数据）

