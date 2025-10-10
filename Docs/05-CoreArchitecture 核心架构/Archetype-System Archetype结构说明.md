# Archetype 结构说明（策划案）

> 目的：规范“原型（Archetype）”如何声明与组合，明确它与实体（Entity）、组件（Component）、能力（Capability）以及视图原型（ViewArchetype）的关系与用法。本文用于策划与程序协同，作为系统级的策划案，供 ECC 文档引用。

## 1. 定义

- Archetype（逻辑原型）：用于声明一个实体应当具备的“组件/能力”的组合关系，支持多个基础原型合并为一个业务原型（如 Role、Monster）。
- ViewArchetype（视图原型）：用于声明一个实体在视图层应当使用的 `EntityView` 类型以及需要的视图组件集合。它由 AstrumView 程序集实现，并通过 Attribute 与逻辑原型名建立映射。

## 2. 设计目标

- 声明式组合：通过属性（Attribute）标注与“合并”列表定义原型，减少在实体类中硬编码。
- 可复用：通用基础原型（BaseUnit、Combatant、Controllable）被业务原型复用。
- 去重与稳定：合并时对组件/能力去重，避免钻石结构重复。
- 视图解耦：逻辑原型与视图原型分属不同程序集，采用单向依赖（View 依赖 Logic）。

## 3. 逻辑原型（Archetype）

### 3.1 声明方式

- 使用 `ArchetypeAttribute("Name", params string[] merge)` 声明名称与合并的父原型名称。
- 子类仅声明“自身的组件/能力增量”，不写完整并集。

### 3.2 注册与合并

- 启动时扫描各程序集中的 Archetype 类型，收集 `Name`、`Merge`、自身 `Components/Capabilities`。
- 递归合并 `Merge` 链条，使用 `HashSet<Type>` 去重，得到扁平化的 `Components/Capabilities`。
- 异常约束：缺失父原型时报错并阻断；循环合并通过 `visited` 防止死循环但需在策划案中禁止。

### 3.3 示例原型关系（示意）

- BaseUnit：Position、Movement + MovementCapability
- Action：Action + ActionCapability
- Controllable：LSInput + ControlCapability（如存在）
- Role = BaseUnit ∪ Action ∪ Controllable + 自身差异
- Monster = BaseUnit ∪ Action + 自身差异（AI）

## 4. 视图原型（ViewArchetype）

### 4.1 作用

- 规定某个逻辑原型在视图层对应的 `EntityView` 类型与必备 `ViewComponent` 列表。
- 不是所有逻辑原型都必须有视图原型；无视图原型时，不创建对应 `EntityView`。

### 4.2 声明与映射

- 使用 `ViewArchetypeAttribute("LogicArchetypeName")`（可扩展为多个名称）建立关联。
- `ViewArchetype` 仅声明 `ViewComponents` 用于视图组件并集合成；不负责确定具体 `EntityView` 类型。

### 4.3 创建时机（运行期）

1) 逻辑创建实体（由 Archetype 扁平集合装配组件/能力）
2) 触发“视图装配钩子”，传入 `entity` 与 `logicArchetypeName`
3) 视图侧查询 `ViewArchetypeRegistry`：
   - 若存在映射：按并集合成的 `ViewComponents` 自动挂载到一个选定的 `EntityView` 实例上（`EntityView` 类型由工程侧单独规则决定，如按实体类型映射或统一默认）
   - 若不存在映射：跳过视图组件装配（可不创建视图）

## 5. 使用规范

- 原型命名：保持业务语义清晰（如 Role、Monster、NPC 等）。
- 合并关系：优先复用基础原型；避免循环定义；尽量保持扁平化组合层级不超过 3 层。
- 视图映射：仅为“可直接落地展示”的原型配置 ViewArchetype（例如 Role/Monster）。纯逻辑原型无需视图映射。
- 表驱动：如需从配置表控制原型，新增 `ArchetypeName` 字段，并通过工厂在运行时选择。

## 6. 与 ECC 的关系

- ECC 文档描述实体-组件-能力的总体架构与数据流；
- 本文档专注于“原型层”的声明与组合规则（逻辑原型 + 视图原型），ECC 在“实体创建流程”章节引用本规范。

---

---

版本：v1.1 (更新日期：2025-10-07)位置：`AstrumConfig/Doc/Archetype结构说明.md`变更记录：

- v1.1: 更新 ViewArchetype 合成算法为实际实现（使用 `ArchetypeManager.GetMergeChain()`）
- v1.1: 补充完整的装配流程和示例
- v1.0: 初始版本

## 10. 实现状态与测试

### 10.1 已完成功能 ✅

**逻辑层 (AstrumLogic):**

- ✅ `ArchetypeManager`: 完整的注册、合并、查询功能
- ✅ `ArchetypeAttribute`: 支持名称和合并列表声明
- ✅ 内置 Archetype: `BaseUnit`, `Action`, `Controllable`, `Role` 等
- ✅ `EntityFactory.CreateByArchetype()`: 严格从 `ArchetypeInfo` 装配
- ✅ `EntityFactory.CreateFromConfig()`: 表驱动的实体创建
- ✅ `World.CreateEntityByConfig()`: 推荐的统一入口

**视图层 (AstrumView):**

- ✅ `ViewArchetypeManager`: 使用逻辑侧合并链的视图原型管理
- ✅ `ViewArchetypeAttribute`: 建立逻辑 Archetype 名称映射
- ✅ 内置 ViewArchetype: `BaseUnitViewArchetype`, `ActionViewArchetype`, `RoleViewArchetype` 等
- ✅ `EntityViewFactory`: 完整的 EntityView 创建和装配流程
- ✅ `EntityView.BuildViewComponents()`: 接受类型数组并装配组件

**配置与数据:**

- ✅ `EntityBaseTable.ArchetypeName`: 配置表支持
- ✅ 测试用例: `EntityCreationTests` 验证核心功能

### 10.2 使用示例

**创建实体（推荐方式）:**

```csharp
// 在配置表中设置 EntityId=1001, ArchetypeName="Role"
var entity = world.CreateEntityByConfig(1001);
// 自动装配: PositionComponent, MovementComponent, ActionComponent, LSInputComponent
//          + MovementCapability, ActionCapability, ControlCapability
// 自动创建视图: TransViewComponent, ModelViewComponent, AnimationViewComponent
```

**添加新的 Archetype:**

```csharp
// 1. 创建逻辑 Archetype
[Archetype("Monster", "BaseUnit", "Action")]
public class MonsterArchetype : Archetype
{
    private static readonly Type[] _caps = { typeof(AICapability) };
    public override Type[] Capabilities => _caps;
}

// 2. 创建对应的 ViewArchetype（可选）
[ViewArchetype("Monster")]
public class MonsterViewArchetype : ViewArchetype
{
    private static readonly Type[] _comps = { typeof(HealthBarViewComponent) };
    public override Type[] ViewComponents => _comps;
}

// 3. 在配置表中使用
// EntityId=2001, ArchetypeName="Monster"
```

### 10.3 调试技巧

1. **检查 Archetype 注册**：在 `ArchetypeManager.Initialize()` 后打印 `_nameToInfo` 内容
2. **检查合并链**：调用 `ArchetypeManager.Instance.GetMergeChain("Role")` 查看展开序列
3. **检查 ViewComponent 装配**：在 `EntityViewFactory.AssembleViewComponents()` 中打断点
4. **验证组件是否装配**：使用 `entity.HasComponent<T>()` 和 `entityView.GetViewComponent<T>()`

## 8. 示例（可直接参考的最小实现）

### 8.1 逻辑 Archetype 示例（AstrumLogic 程序集）

```csharp
// 仅作示例，实际命名空间与目录以项目为准
using System;
using Game.Archetypes; // 示例命名空间：包含 Archetype/ArchetypeAttribute/ArchetypeRegistry
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;

[Archetype("BaseUnit")]
public class BaseUnitArchetype : Archetype
{
    private static readonly Type[] comps =
    {
        typeof(PositionComponent),
        typeof(MovementComponent),
    };
    private static readonly Type[] caps =
    {
        typeof(MovementCapability),
    };
    public override Type[] Components => comps;
    public override Type[] Capabilities => caps;
}

[Archetype("Action")]
public class ActionArchetype : Archetype
{
    private static readonly Type[] comps = { typeof(ActionComponent) };
    private static readonly Type[] caps = { typeof(ActionCapability) };
    public override Type[] Components => comps;
    public override Type[] Capabilities => caps;
}

// 玩家角色：组合 BaseUnit + Combatant
[Archetype("Role", "BaseUnit", "Action")]
public class RoleArchetype : Archetype
{
    // 可按需添加自身差异组件（示例：无额外项）
}
```

创建实体时使用（伪代码）：

```csharp
var data = ArchetypeRegistry.Get("Role");
var entity = new Entity();
foreach (var c in data.Components) entity.AddComponent(c);
foreach (var a in data.Capabilities) entity.AddCapability(a);
entity.Initialize();
```

### 8.2 ViewArchetype 示例（AstrumView 程序集）

```csharp
// 示例接口/基类命名以策划案为准
using System;
using Astrum.View.Core;          // EntityView / Stage
using Astrum.View.Components;    // ModelViewComponent / TransViewComponent / AnimationViewComponent

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ViewArchetypeAttribute : Attribute
{
    public string LogicArchetypeName { get; }
    public ViewArchetypeAttribute(string logicArchetypeName)
    {
        LogicArchetypeName = logicArchetypeName;
    }
}

public abstract class ViewArchetype
{
    public virtual Type[] ViewComponents => Array.Empty<Type>();
}

// BaseUnit 对应的视图原型：提供模型与位移表现
[ViewArchetype("BaseUnit")]
public class BaseUnitViewArchetype : ViewArchetype
{
    private static readonly Type[] viewComps =
    {
        typeof(ModelViewComponent),
        typeof(TransViewComponent)
    };
    public override Type[] ViewComponents => viewComps;
}

// Action 对应的视图原型：提供动画表现
[ViewArchetype("Action")]
public class ActionViewArchetype : ViewArchetype
{
    private static readonly Type[] viewComps =
    {
        typeof(AnimationViewComponent)
    };
    public override Type[] ViewComponents => viewComps;
}

// 视图注册器（仅示意）：运行时扫描 AstrumView 程序集，建立映射
public static class ViewArchetypeRegistry
{
    // Dictionary<string, ViewArchetypeData>，key = LogicArchetypeName
    // ViewArchetypeData 包含 ViewType、ViewComponents
    public static bool TryGet(string logicArchetypeName, out ViewArchetypeData data) { /* ... */ data = default; return false; }
}

public struct ViewArchetypeData
{
    public Type ViewType;
    public Type[] ViewComponents;
}

// 工厂（仅示意）：若存在映射则创建 EntityView 并装配组件
public static class EntityViewFactory
{
    public static EntityView CreateIfAny(long entityId, string logicArchetypeName, Stage stage)
    {
        if (!ViewArchetypeRegistry.TryGet(logicArchetypeName, out var data))
            return null; // 非必选视图

        var view = (EntityView)Activator.CreateInstance(data.ViewType);
        view.Initialize(entityId, stage);

        // 根据原型的 ViewComponents 自动挂载
        foreach (var vc in data.ViewComponents)
        {
            if (typeof(ViewComponent).IsAssignableFrom(vc))
            {
                var instance = (ViewComponent)Activator.CreateInstance(vc);
                view.AddViewComponent(instance);
            }
        }
        return view;
    }
}
```

运行链路梳理：

1) 逻辑：依据 `ArchetypeRegistry.Get("Role")` 装配实体组件/能力 → 实体创建完成；
2) 视图：传入 `logicArchetypeName = "Role"` 调用 `EntityViewFactory.CreateIfAny(...)`；
3) 若存在 `RoleViewArchetype` 映射，则创建 `UnitView` 并自动挂载 `Model/Trans/Animation` 视图组件。

## 9. ViewArchetype 组合规则与合成算法（实际实现）

为避免在视图层重复维护一套合并关系，复用"逻辑原型"的合并展开结果；ViewArchetype 仅做"增量合并"。

### 9.1 基本约定

- **绑定**：每个 ViewArchetype 通过 `[ViewArchetype("LogicArchetypeName")]` 绑定一个逻辑原型名
- **增量**：ViewArchetype 仅声明 `ViewComponents[]`，不需要维护合并关系
- **单向依赖**：视图层可以访问逻辑层（`ArchetypeManager`），但逻辑层不依赖视图层

### 9.2 实际实现的合成算法

**核心机制：** `ViewArchetypeManager` 直接调用 `ArchetypeManager.GetMergeChain()` 获取完整的合并链。

```csharp
public bool TryGetComponents(string logicArchetypeName, out Type[] viewComponents)
{
    var result = new HashSet<Type>();
  
    // 1. 从逻辑侧获取合并展开序列（从父到子，有序）
    var mergeChain = ArchetypeManager.Instance.GetMergeChain(logicArchetypeName);
  
    // 2. 遍历合并链，累加每个节点的 ViewComponents
    foreach (var nameInChain in mergeChain)
    {
        if (_logicNameToViewComponents.TryGetValue(nameInChain, out var components))
        {
            result.UnionWith(components); // HashSet 自动去重
        }
    }
  
    viewComponents = result.ToArray();
    return result.Count > 0;
}
```

**关键优势：**

- 不需要在视图侧重复实现合并逻辑
- 自动与逻辑侧的 Archetype 结构保持同步
- 如果逻辑 Archetype 调整合并关系，视图自动生效

### 9.3 完整装配流程

```
1. Entity 创建完成，发布 EntityCreatedEvent
   ↓
2. Stage.OnEntityCreated() 接收事件
   ↓
3. EntityViewFactory.CreateEntityView(entityId, stage)
   ↓
4. EntityView.Initialize(entityId, stage) - 创建 GameObject
   ↓
5. EntityViewFactory.AssembleViewComponents(entityView, stage)
   ↓
6. 通过 stage.Room.MainWorld.GetEntity(entityId) 获取 Entity
   ↓
7. 读取 entity.EntityConfig.ArchetypeName
   ↓
8. ViewArchetypeManager.TryGetComponents(archetypeName, out viewComponentTypes)
   ↓ 内部调用 ArchetypeManager.GetMergeChain(archetypeName)
   ↓ 遍历合并链，收集所有 ViewComponents
   ↓
9. EntityView.BuildViewComponents(viewComponentTypes)
   ↓
10. 遍历类型数组，创建 ViewComponent 实例并 AddViewComponent()
```

### 9.4 示例（实际运行）

假设配置：

- **逻辑 Archetype**:

  - `BaseUnit`: `[PositionComponent, MovementComponent]`
  - `Action`: `[ActionComponent]`
  - `Controllable`: `[LSInputComponent]`
  - `Role`: 合并 `[BaseUnit, Action, Controllable]`
- **ViewArchetype**:

  - `BaseUnitViewArchetype("BaseUnit")`: `[TransViewComponent, ModelViewComponent]`
  - `ActionViewArchetype("Action")`: `[AnimationViewComponent]`
  - `RoleViewArchetype("Role")`: `[]` (无额外 ViewComponent)

**创建 Role 实体时的 ViewComponent 合并结果：**

1. `ArchetypeManager.GetMergeChain("Role")` 返回: `["BaseUnit", "Action", "Controllable", "Role"]`
2. 遍历合并链：
   - `BaseUnit` → 添加 `TransViewComponent`, `ModelViewComponent`
   - `Action` → 添加 `AnimationViewComponent`
   - `Controllable` → 无对应 ViewArchetype，跳过
   - `Role` → 无额外 ViewComponents
3. 最终结果（去重后）: `[TransViewComponent, ModelViewComponent, AnimationViewComponent]`

### 9.5 实施关键点

1. **初始化顺序**：必须先初始化 `ArchetypeManager`，再初始化 `ViewArchetypeManager`

   ```csharp
   // GameApplication.InitializeManagers()
   ArchetypeManager.Instance.Initialize();
   ViewArchetypeManager.Instance.Initialize();
   ```
2. **程序集扫描**：

   - `ArchetypeManager` 扫描所有程序集中的 Archetype 类
   - `ViewArchetypeManager` 仅扫描当前程序集（AstrumView）中的 ViewArchetype 类
3. **缺省处理**：

   - 如果逻辑 Archetype 没有对应的 ViewArchetype，不会报错，只是不创建 ViewComponents
   - 所有 EntityView 都使用默认的 `EntityView` 类型（或其子类），由 `EntityViewFactory` 决定
4. **性能考虑**：

   - 合并链在 `ArchetypeManager.Initialize()` 时已计算完成
   - `GetMergeChain()` 直接返回缓存结果，无运行时开销
   - ViewComponent 类型数组在第一次查询后可以缓存
