# EntityView 和 UnitView 的 _requiredComponents 配置

## 概述

`EntityView` 和 `UnitView` 现在支持类似 `Entity` 和 `Unit` 的 `_requiredComponents` 配置功能，可以在初始化时自动挂载必需的视图组件。

## 架构设计

### 1. EntityView 基类

`EntityView` 基类提供了以下功能：

- `GetRequiredViewComponentTypes()`: 虚方法，返回需要的视图组件类型列表
- `BuildViewComponents()`: 根据组件类型自动创建和挂载视图组件
- 在 `Initialize()` 方法中自动调用 `BuildViewComponents()`

### 2. UnitView 实现

`UnitView` 继承自 `EntityView`，定义了必需的视图组件：

```csharp
private static readonly Type[] _requiredViewComponents = new Type[]
{
    typeof(MovementViewComponent),
    typeof(HealthViewComponent)
};

public override Type[] GetRequiredViewComponentTypes()
{
    return _requiredViewComponents;
}
```

## 视图组件系统

### 1. 基础视图组件

#### MovementViewComponent
- **功能**: 处理实体的移动表现
- **特性**: 
  - 平滑移动和旋转
  - 动画状态管理
  - 目标位置和旋转设置
- **数据同步**: 支持 `MovementData` 数据同步

#### HealthViewComponent
- **功能**: 处理实体的血量表现
- **特性**:
  - 血量条UI显示
  - 伤害和治疗特效
  - 血量变化动画
- **数据同步**: 支持 `HealthData` 数据同步

### 2. 组件生命周期

每个视图组件都遵循以下生命周期：

1. **初始化**: `OnInitialize()` - 组件创建和设置
2. **更新**: `OnUpdate(float deltaTime)` - 每帧更新逻辑
3. **数据同步**: `OnSyncData(object data)` - 与逻辑层数据同步
4. **销毁**: `OnDestroy()` - 清理资源

## 使用方法

### 1. 基本使用

```csharp
// 创建UnitView（会自动挂载必需的组件）
var unitView = EntityViewFactory.Instance.CreateEntityView("unit", entityId) as UnitView;

// 设置单位属性
unitView.SetUnitType("player");
unitView.SetInitialPosition(Vector3.zero);
unitView.SetMoveSpeed(5f);
```

### 2. 访问视图组件

```csharp
// 获取移动组件
var movementComponent = unitView.GetViewComponent<MovementViewComponent>();
if (movementComponent != null)
{
    movementComponent.SetTargetPosition(new Vector3(10f, 0f, 10f));
}

// 获取血量组件
var healthComponent = unitView.GetViewComponent<HealthViewComponent>();
if (healthComponent != null)
{
    healthComponent.SetHealth(80f, 100f);
}
```

### 3. 数据同步

```csharp
// 同步移动数据
var movementData = new MovementData(
    new Vector3(10f, 0f, 10f),
    Quaternion.Euler(0f, 45f, 0f),
    true,
    5f
);
movementComponent.SyncData(movementData);

// 同步血量数据
var healthData = new HealthData(80f, 100f, true);
healthComponent.SyncData(healthData);
```

### 4. 动态添加组件

```csharp
// 动态添加自定义组件
var customComponent = new CustomViewComponent();
unitView.AddViewComponent(customComponent);
```

## 扩展指南

### 1. 创建新的视图组件

```csharp
public class CustomViewComponent : ViewComponent
{
    protected override void OnInitialize()
    {
        // 初始化逻辑
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        // 更新逻辑
    }
    
    protected override void OnDestroy()
    {
        // 清理逻辑
    }
    
    protected override void OnSyncData(object data)
    {
        // 数据同步逻辑
    }
}
```

### 2. 为新的EntityView类型配置组件

```csharp
public class EnemyView : EntityView
{
    private static readonly Type[] _requiredViewComponents = new Type[]
    {
        typeof(MovementViewComponent),
        typeof(HealthViewComponent),
        typeof(AIViewComponent),        // 新增AI组件
        typeof(CombatViewComponent)     // 新增战斗组件
    };
    
    public override Type[] GetRequiredViewComponentTypes()
    {
        return _requiredViewComponents;
    }
}
```

### 3. 在EntityViewFactory中注册

```csharp
switch (entityType.ToLower())
{
    case "enemy":
        entityView = new EnemyView();
        break;
    // ... 其他类型
}
```

## 与逻辑层的对应关系

### 1. 组件映射

| 逻辑层组件 | 视图层组件 | 功能 |
|-----------|-----------|------|
| PositionComponent | MovementViewComponent | 位置和移动表现 |
| HealthComponent | HealthViewComponent | 血量UI和特效 |
| MovementComponent | MovementViewComponent | 移动动画和效果 |
| CombatComponent | CombatViewComponent | 战斗特效和UI |

### 2. 数据同步流程

```
逻辑层组件数据变化
    ↓
发布事件 (EntityUpdatedEventData)
    ↓
Stage接收事件
    ↓
EntityView.SyncWithEntity()
    ↓
视图组件.SyncData()
    ↓
更新视觉效果
```

## 性能考虑

### 1. 组件缓存

- 使用静态 `Type[]` 数组缓存组件类型，避免重复分配
- 组件实例在 `EntityView` 生命周期内复用

### 2. 更新优化

- 只有启用的组件才会执行更新逻辑
- 支持组件级别的启用/禁用控制

### 3. 内存管理

- 组件在 `EntityView` 销毁时自动清理
- 支持组件池化（可扩展）

## 示例代码

参考 `UnitViewExample.cs` 文件，其中包含了完整的使用示例：

- 创建和配置 UnitView
- 测试自动挂载的组件
- 组件功能测试
- 数据同步测试
- 动态组件管理

## 注意事项

1. **组件依赖**: 确保视图组件不依赖特定的逻辑层实现
2. **数据同步**: 使用事件系统进行数据同步，避免直接引用
3. **性能**: 合理使用组件，避免过度设计
4. **扩展性**: 设计组件时考虑未来的扩展需求
5. **测试**: 为每个组件编写相应的测试用例

## 最佳实践

1. **组件职责单一**: 每个组件只负责一个特定的视觉功能
2. **数据驱动**: 通过数据同步而不是直接调用来更新组件状态
3. **配置化**: 使用配置文件或编辑器设置来管理组件参数
4. **事件驱动**: 使用事件系统进行组件间通信
5. **资源管理**: 合理管理组件的资源分配和释放 