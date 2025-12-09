# 场景编辑器设计

## 概述

场景编辑器用于在Unity编辑器中可视化编辑场景触发器（SceneTrigger）配置。支持场景选择、Trigger列表管理、参数编辑、场景可视化以及JSON导入导出功能。编辑器采用参数转换器架构，支持类型化的参数编辑和版本兼容的数据迁移。

## 架构设计

### 核心组件

```
SceneEditorWindow (主窗口)
    ├── SceneSelectionModule - 场景选择模块
    ├── TriggerListModule - Trigger列表模块
    ├── ParameterEditorModule - 参数编辑模块
    ├── SceneTriggerVisualizer - 场景可视化管理器
    └── SceneTriggerDataReader/Writer - 数据读写器
```

### 数据流

```
CSV表格 (Luban格式)
    ↓ 读取
SceneTriggerTableData / ConditionTableData / ActionTableData
    ↓ 转换
SceneTriggerEditorData (编辑器数据模型)
    ↓ 编辑
参数对象 (强类型，如 SpawnEntityParameters)
    ↓ 序列化
int[] / float[] / string[] (数组格式)
    ↓ 写入
CSV表格
```

### 参数转换器架构

**核心接口**: `ITriggerParameterConverter<T>`

- **序列化**: 参数对象 → 三个数组（intParams, floatParams, stringParams）
- **反序列化**: 三个数组 → 参数对象
- **JSON导出**: 参数对象 → JSON格式（包含版本信息）
- **JSON导入**: JSON格式 → 参数对象（支持版本迁移）

**注册机制**: `TriggerParameterConverterRegistry` 静态注册表，根据枚举类型查找转换器。

## 数据结构

### 编辑器数据模型

```csharp
SceneTriggerEditorData
    ├── TriggerId: int
    ├── Conditions: List<ConditionEditorData>
    │   ├── ConditionId: int
    │   ├── ConditionType: TriggerConditionType
    │   └── Parameters: object (强类型参数对象)
    └── Actions: List<ActionEditorData>
        ├── ActionId: int
        ├── ActionType: TriggerActionType
        ├── Parameters: object (强类型参数对象)
        └── VisualizerObject: GameObject (场景可视化对象)
```

### 参数对象示例

```csharp
// 动作参数
SpawnEntityParameters
    ├── EntityId: int
    ├── Count: int
    ├── Position: Vector3 (点位置)
    └── Range: Bounds? (范围位置，可选)

// 条件参数
DelayParameters
    └── DelaySeconds: float
```

### 表格映射

**SceneTriggerTable**: ID, ConditionList, ActionList  
**SceneTriggerConditionTable**: ID, ConditionType, IntParams, FloatParams, StringParams, Notes  
**SceneTriggerActionTable**: ID, ActionType, IntParams, FloatParams, StringParams, Notes

## 核心功能

### 场景选择

- 扫描 `Assets/Scenes/` 目录下的所有 `.unity` 文件
- 下拉选择场景
- 选择后自动打开场景（使用 `EditorSceneManager.OpenScene`）

### Trigger列表管理

- 从CSV读取所有Trigger配置
- 列表显示所有Trigger（显示ID）
- 点击选择Trigger，显示详细信息

### 参数编辑

- 根据 `TriggerConditionType` 和 `TriggerActionType` 动态获取转换器
- 使用转换器将数组参数反序列化为强类型参数对象
- 编辑器直接编辑参数对象（类型安全）
- 支持位置信息可视化编辑（通过场景中的Cube对象）

### 场景可视化

- 根据参数对象中的位置信息（实现 `IPositionInfoProvider` 接口）
- 在场景中创建临时Cube GameObject显示Trigger范围
- Cube颜色：橙色半透明（Color(1f, 0.5f, 0f, 0.3f)）
- 窗口关闭时自动清理所有临时对象

### JSON导入导出

**导出格式**:
```json
{
  "schema": "SpawnEntity",
  "version": 1,
  "data": {
    "entityId": 1001,
    "count": 3,
    "position": { "x": 10.0, "y": 0.0, "z": 5.0 }
  },
  "_metadata": { ... } // 可选
}
```

**导入流程**:
1. 解析JSON，检测版本号
2. 根据版本选择对应的导入方法
3. 字段映射（字段名保持不变，支持向后兼容）
4. 缺失字段使用默认值
5. 显示兼容性报告（警告、错误、迁移信息）

### 数据持久化

- 使用现有的 `LubanCSVWriter` 框架
- 全量保存：读取现有数据 → 合并修改 → 全量写入
- 支持自动备份（通过Git）

## 位置信息处理

### 位置信息提取

参数对象实现 `IPositionInfoProvider` 接口，提供位置信息：

```csharp
public interface IPositionInfoProvider
{
    PositionInfo GetPositionInfo();
}

// 示例实现
public class SpawnEntityParameters : IPositionInfoProvider
{
    public Vector3 Position { get; set; }
    public Bounds? Range { get; set; }
    
    public PositionInfo GetPositionInfo()
    {
        if (Range.HasValue)
            return new PositionInfo { Type = PositionType.Range, Range = Range };
        else
            return new PositionInfo { Type = PositionType.Point, Point = Position };
    }
}
```

### 可视化更新

- 参数编辑时实时更新场景中的Cube位置和大小
- 支持通过拖拽Cube来修改位置（可选功能）

## 版本兼容性

### 版本管理

- 每个转换器声明 `CurrentVersion`
- JSON导出时包含版本号
- JSON导入时检测版本，自动迁移

### 兼容性策略

1. **字段名保持不变**: 已存在的字段名不再变更，确保向后兼容
2. **容错导入**: 字段缺失使用默认值，类型不匹配尝试转换
3. **版本迁移**: 根据版本号选择对应的导入方法
4. **兼容性报告**: 显示迁移警告和修复建议

### 迁移示例

```csharp
// V1: 只有 position (点位置)
// V2: 支持 position 或 range (范围位置)

public T ImportFromJson(string json)
{
    int jsonVersion = jsonObj["version"]?.Value<int>() ?? 1;
    
    if (jsonVersion == 1)
    {
        // V1格式：只有position
        result.Position = ParsePosition(dataObj["position"]);
    }
    else if (jsonVersion == 2)
    {
        // V2格式：可能有range或position
        if (dataObj["range"] != null)
            result.Range = ParseRange(dataObj["range"]);
        else if (dataObj["position"] != null)
            result.Position = ParsePosition(dataObj["position"]);
    }
}
```

## 关键决策

### 参数转换器 vs 直接数组编辑

**问题**: 如何管理参数数组（intParams, floatParams, stringParams）？

**备选方案**:
1. 直接编辑数组（索引管理复杂，易出错）
2. 使用参数转换器（类型安全，易于维护）

**选择**: 方案2 - 参数转换器

**理由**:
- 类型安全：编译期检查
- 易于扩展：新增类型只需实现接口
- 代码清晰：参数结构一目了然
- 易于测试：转换逻辑可独立测试

**影响**:
- 需要为每种类型实现转换器
- 需要参数对象类定义
- 需要注册机制管理转换器

### JSON格式设计

**问题**: JSON导出格式如何设计？

**备选方案**:
1. 只保存参数对象（简洁）
2. 包含元数据和原始数组（完整但冗余）

**选择**: 混合格式 - 主要数据简洁，元数据可选

**理由**:
- 主要数据清晰易读
- 元数据用于调试和版本迁移
- 可选字段不影响核心功能

**影响**:
- JSON文件体积稍大（但可接受）
- 需要处理可选字段

### 场景可视化方案

**问题**: 如何在场景中显示Trigger位置？

**备选方案**:
1. 临时GameObject（可点击选择）
2. Gizmos绘制（不污染场景，但无法选择）
3. Editor-only组件（需要额外组件系统）

**选择**: 方案1 - 临时GameObject + Gizmos辅助

**理由**:
- 可点击选择，交互性好
- 可以附加更多信息
- 窗口关闭时自动清理

**影响**:
- 需要管理GameObject生命周期
- 需要设置hideFlags避免保存到场景

## 实现细节

### 转换器注册

```csharp
static TriggerParameterConverterRegistry()
{
    // 注册条件转换器
    RegisterCondition(TriggerConditionType.SceneStart, new SceneStartParameterConverter());
    RegisterCondition(TriggerConditionType.Delay, new DelayParameterConverter());
    
    // 注册动作转换器
    RegisterAction(TriggerActionType.SpawnEntity, new SpawnEntityParameterConverter());
    RegisterAction(TriggerActionType.PlayEffect, new PlayEffectParameterConverter());
}
```

### 参数编辑器动态绘制

```csharp
private void DrawActionEditor(ActionEditorData action)
{
    // 根据类型获取转换器
    var converter = TriggerParameterConverterRegistry.GetActionConverter(action.ActionType);
    
    // 反序列化为参数对象
    var parameters = converter.Deserialize(
        action.IntParams, 
        action.FloatParams, 
        action.StringParams
    );
    
    // 使用反射或动态类型绘制编辑器
    DrawParameterFields(parameters);
    
    // 编辑后序列化回数组
    converter.Serialize(parameters, 
        out action.IntParams, 
        out action.FloatParams, 
        out action.StringParams
    );
}
```

### 场景可视化更新

```csharp
private void UpdateVisualizers()
{
    foreach (var action in _selectedTrigger.Actions)
    {
        if (action.Parameters is IPositionInfoProvider posInfo)
        {
            var positionInfo = posInfo.GetPositionInfo();
            
            if (positionInfo.Type == PositionType.Point)
            {
                _visualizer.CreateOrUpdateVisualizer(action, positionInfo.Point.Value);
            }
            else if (positionInfo.Type == PositionType.Range)
            {
                _visualizer.CreateOrUpdateVisualizer(action, positionInfo.Range.Value);
            }
        }
    }
}
```

## 典型示例

### 创建新的Trigger类型

1. **定义参数类**:
```csharp
public class NewActionParameters : IPositionInfoProvider
{
    public int Param1 { get; set; }
    public float Param2 { get; set; }
    public Vector3 Position { get; set; }
    
    public PositionInfo GetPositionInfo() => 
        new PositionInfo { Type = PositionType.Point, Point = Position };
}
```

2. **实现转换器**:
```csharp
public class NewActionParameterConverter : ITriggerParameterConverter<NewActionParameters>
{
    public int CurrentVersion => 1;
    
    public void Serialize(NewActionParameters param, out int[] intParams, out float[] floatParams, out string[] stringParams)
    {
        intParams = new[] { param.Param1 };
        floatParams = new[] { param.Param2, param.Position.x, param.Position.y, param.Position.z };
        stringParams = new string[0];
    }
    
    public NewActionParameters Deserialize(int[] intParams, float[] floatParams, string[] stringParams)
    {
        return new NewActionParameters
        {
            Param1 = intParams[0],
            Param2 = floatParams[0],
            Position = new Vector3(floatParams[1], floatParams[2], floatParams[3])
        };
    }
    
    // ... 其他方法实现
}
```

3. **注册转换器**:
```csharp
// 在 TriggerParameterConverterRegistry 的静态构造函数中
RegisterAction(TriggerActionType.NewAction, new NewActionParameterConverter());
```

## 相关文档

- [参数转换器系统设计](Scene-Trigger-Parameter-Converter-Design%20参数转换器设计.md)
- [CSV框架](../CSV-Framework%20CSV框架.md)
- [配置系统](../../06-Configuration%20配置系统/Table-Config%20表格配置说明.md)

