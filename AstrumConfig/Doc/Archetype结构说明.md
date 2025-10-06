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
- Role = BaseUnit ∪ Action ∪ Controllable + 自身差异（如背包）
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

版本：v1.0
位置：`AstrumConfig/Doc/Archetype结构说明.md`

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

## 9. ViewArchetype 组合规则与合成算法

为避免在视图层重复维护一套合并关系，复用“逻辑原型”的合并展开结果；ViewArchetype 仅做“增量合并”。

### 9.1 基本约定

- 绑定：每个 ViewArchetype 通过 `[ViewArchetype("LogicArchetypeName")]` 绑定一个或多个逻辑原型名。
- 增量：ViewArchetype 仅声明 `ViewType?` 与 `ViewComponents[]`。不引入任何策略位。

### 9.2 合成算法（给定逻辑原型名 L）

1) 从逻辑侧获取 L 的“合并展开序列”（自底向上：父 → 子 → L）。
2) 依次处理序列中每一个逻辑名：
   - 若存在对应 ViewArchetype：将该节点声明的 `ViewComponents` 与当前累计集合做 HashSet 并集（纯增量、去重）。
3) 得到最终的 `ViewType` 与去重后的 `ViewComponents`，据此创建 `EntityView` 并自动挂载组件。
4) 若整条链上均无 ViewArchetype，视为无需创建视图（逻辑可独立运行）。

### 9.4 示例（组合演示）

- 逻辑：`Role = BaseUnit ∪ Combatant ∪ Controllable`
  - 视图（ViewArchetype 对应一个逻辑 Archetype，并列出其包含的 ViewComponents）：
    - BaseUnitViewArchetype (BaseUnit)：{ TransViewComponent, ModelViewComponent }
    - ActionViewArchetype (Action)：{ AnimationViewComponent }
    - RoleViewArchetype (Role)：{ ModelViewComponent }

### 9.5 实施提示

- 视图注册器只在 AstrumView 程序集中扫描，保持单向依赖。
- 合成过程在 `EntityViewFactory.CreateIfAny(...)` 内部完成；逻辑侧仅提供最终 `logicArchetypeName` 与 `entityId`。
- 缺省路径：若最终无 `ViewType`，可设工程级默认（如 `EntityView`）或直接不创建视图。
