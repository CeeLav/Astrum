# 参数转换器系统设计

## 概述

参数转换器系统用于在强类型参数对象和数组格式（intParams, floatParams, stringParams）之间进行双向转换。支持JSON导入导出、版本兼容性迁移和位置信息提取。系统采用接口模式，便于扩展新的Trigger类型。

## 核心接口

### ITriggerParameterConverter<T>

```csharp
public interface ITriggerParameterConverter<T> where T : class
{
    int CurrentVersion { get; }
    
    // 数组序列化/反序列化
    void Serialize(T parameters, out int[] intParams, out float[] floatParams, out string[] stringParams);
    T Deserialize(int[] intParams, float[] floatParams, string[] stringParams);
    
    // JSON导入导出
    string ExportToJson(T parameters, bool includeMetadata = false);
    JsonImportResult<T> ImportFromJson(string json);
    
    // 默认值
    T CreateDefault();
}
```

**职责**:
- 参数对象 ↔ 数组格式转换
- 参数对象 ↔ JSON格式转换
- 版本管理和兼容性处理
- 默认值提供

### IPositionInfoProvider

```csharp
public interface IPositionInfoProvider
{
    PositionInfo GetPositionInfo();
}

public class PositionInfo
{
    public PositionType Type { get; set; }
    public Vector3? Point { get; set; }
    public Bounds? Range { get; set; }
}

public enum PositionType { None, Point, Range }
```

**职责**: 从参数对象中提取位置信息，用于场景可视化。

## 转换器实现模式

### 基本实现结构

```csharp
public class SpawnEntityParameterConverter : ITriggerParameterConverter<SpawnEntityParameters>
{
    public int CurrentVersion => 1;
    
    // 序列化：参数对象 → 数组
    public void Serialize(SpawnEntityParameters param, out int[] intParams, out float[] floatParams, out string[] stringParams)
    {
        intParams = new[] { param.EntityId, param.Count };
        
        if (param.Range.HasValue)
        {
            // 范围格式：6个float (min.x, min.y, min.z, max.x, max.y, max.z)
            var bounds = param.Range.Value;
            floatParams = new[]
            {
                bounds.min.x, bounds.min.y, bounds.min.z,
                bounds.max.x, bounds.max.y, bounds.max.z
            };
        }
        else
        {
            // 点格式：3个float (x, y, z)
            floatParams = new[] { param.Position.x, param.Position.y, param.Position.z };
        }
        
        stringParams = new string[0];
    }
    
    // 反序列化：数组 → 参数对象
    public SpawnEntityParameters Deserialize(int[] intParams, float[] floatParams, string[] stringParams)
    {
        var result = new SpawnEntityParameters
        {
            EntityId = intParams.Length > 0 ? intParams[0] : 0,
            Count = intParams.Length > 1 ? intParams[1] : 1
        };
        
        // 根据数组长度判断格式
        if (floatParams.Length >= 6)
        {
            // 范围格式
            result.Range = new Bounds(
                new Vector3(floatParams[0], floatParams[1], floatParams[2]),
                new Vector3(
                    floatParams[3] - floatParams[0],
                    floatParams[4] - floatParams[1],
                    floatParams[5] - floatParams[2]
                )
            );
        }
        else if (floatParams.Length >= 3)
        {
            // 点格式
            result.Position = new Vector3(floatParams[0], floatParams[1], floatParams[2]);
        }
        
        return result;
    }
    
    // JSON导出
    public string ExportToJson(SpawnEntityParameters parameters, bool includeMetadata = false)
    {
        var jsonObj = new JObject
        {
            ["schema"] = "SpawnEntity",
            ["version"] = CurrentVersion,
            ["data"] = new JObject
            {
                ["entityId"] = parameters.EntityId,
                ["count"] = parameters.Count
            }
        };
        
        var dataObj = jsonObj["data"] as JObject;
        
        // 位置信息
        if (parameters.Range.HasValue)
        {
            var bounds = parameters.Range.Value;
            dataObj["range"] = new JObject
            {
                ["center"] = new JObject { ["x"] = bounds.center.x, ["y"] = bounds.center.y, ["z"] = bounds.center.z },
                ["size"] = new JObject { ["x"] = bounds.size.x, ["y"] = bounds.size.y, ["z"] = bounds.size.z }
            };
        }
        else
        {
            dataObj["position"] = new JObject
            {
                ["x"] = parameters.Position.x,
                ["y"] = parameters.Position.y,
                ["z"] = parameters.Position.z
            };
        }
        
        // 可选元数据
        if (includeMetadata)
        {
            Serialize(parameters, out var intParams, out var floatParams, out var stringParams);
            jsonObj["_metadata"] = new JObject
            {
                ["exportedAt"] = DateTime.Now.ToString("O"),
                ["rawData"] = new JObject
                {
                    ["intParams"] = new JArray(intParams),
                    ["floatParams"] = new JArray(floatParams),
                    ["stringParams"] = new JArray(stringParams)
                }
            };
        }
        
        return jsonObj.ToString(Formatting.Indented);
    }
    
    // JSON导入（带版本兼容）
    public JsonImportResult<SpawnEntityParameters> ImportFromJson(string json)
    {
        var result = new JsonImportResult<SpawnEntityParameters>();
        
        try
        {
            var jsonObj = JObject.Parse(json);
            int jsonVersion = jsonObj["version"]?.Value<int>() ?? 1;
            var dataObj = jsonObj["data"] as JObject;
            
            if (dataObj == null)
            {
                result.Errors.Add(new ImportError { Message = "缺少 'data' 字段" });
                return result;
            }
            
            var parameters = CreateDefault();
            
            // 字段映射（容错处理）
            if (dataObj["entityId"] != null)
                parameters.EntityId = dataObj["entityId"].Value<int>();
            else
                result.Warnings.Add(new ImportWarning
                {
                    Field = "entityId",
                    Message = "字段缺失",
                    Resolution = "使用默认值 0"
                });
            
            if (dataObj["count"] != null)
                parameters.Count = dataObj["count"].Value<int>();
            
            // 位置信息处理
            if (dataObj["range"] != null)
            {
                var rangeObj = dataObj["range"] as JObject;
                var center = rangeObj["center"] as JObject;
                var size = rangeObj["size"] as JObject;
                
                parameters.Range = new Bounds(
                    new Vector3(
                        center["x"]?.Value<float>() ?? 0,
                        center["y"]?.Value<float>() ?? 0,
                        center["z"]?.Value<float>() ?? 0
                    ),
                    new Vector3(
                        size["x"]?.Value<float>() ?? 1,
                        size["y"]?.Value<float>() ?? 1,
                        size["z"]?.Value<float>() ?? 1
                    )
                );
            }
            else if (dataObj["position"] != null)
            {
                var posObj = dataObj["position"] as JObject;
                parameters.Position = new Vector3(
                    posObj["x"]?.Value<float>() ?? 0,
                    posObj["y"]?.Value<float>() ?? 0,
                    posObj["z"]?.Value<float>() ?? 0
                );
            }
            
            // 版本兼容性报告
            if (jsonVersion < CurrentVersion)
            {
                result.CompatibilityReport = new CompatibilityReport
                {
                    DetectedVersion = jsonVersion,
                    TargetVersion = CurrentVersion,
                    WasMigrated = true
                };
                result.Warnings.Add(new ImportWarning
                {
                    Field = "version",
                    Message = $"检测到旧版本数据 (v{jsonVersion})",
                    Resolution = $"已自动迁移到当前版本 (v{CurrentVersion})"
                });
            }
            
            result.Parameters = parameters;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportError { Message = $"JSON解析失败: {ex.Message}" });
        }
        
        return result;
    }
    
    // 默认值
    public SpawnEntityParameters CreateDefault()
    {
        return new SpawnEntityParameters
        {
            EntityId = 0,
            Count = 1,
            Position = Vector3.zero
        };
    }
}
```

## 注册机制

### TriggerParameterConverterRegistry

```csharp
public static class TriggerParameterConverterRegistry
{
    private static readonly Dictionary<TriggerConditionType, object> ConditionConverters = new();
    private static readonly Dictionary<TriggerActionType, object> ActionConverters = new();
    
    static TriggerParameterConverterRegistry()
    {
        // 注册条件转换器
        RegisterCondition(TriggerConditionType.SceneStart, new SceneStartParameterConverter());
        RegisterCondition(TriggerConditionType.Delay, new DelayParameterConverter());
        RegisterCondition(TriggerConditionType.TriggerEvent, new TriggerEventParameterConverter());
        
        // 注册动作转换器
        RegisterAction(TriggerActionType.SpawnEntity, new SpawnEntityParameterConverter());
        RegisterAction(TriggerActionType.PlayEffect, new PlayEffectParameterConverter());
    }
    
    public static void RegisterCondition<T>(TriggerConditionType type, ITriggerParameterConverter<T> converter) where T : class
    {
        ConditionConverters[type] = converter;
    }
    
    public static void RegisterAction<T>(TriggerActionType type, ITriggerParameterConverter<T> converter) where T : class
    {
        ActionConverters[type] = converter;
    }
    
    public static ITriggerParameterConverter<T> GetConditionConverter<T>(TriggerConditionType type) where T : class
    {
        return ConditionConverters.TryGetValue(type, out var converter) 
            ? converter as ITriggerParameterConverter<T> 
            : null;
    }
    
    public static ITriggerParameterConverter<T> GetActionConverter<T>(TriggerActionType type) where T : class
    {
        return ActionConverters.TryGetValue(type, out var converter) 
            ? converter as ITriggerParameterConverter<T> 
            : null;
    }
}
```

**特点**:
- 静态构造函数自动注册
- 类型安全的泛型查找
- 支持条件转换器和动作转换器分离管理

## JSON格式规范

### 标准格式

```json
{
  "schema": "SpawnEntity",
  "version": 1,
  "data": {
    "entityId": 1001,
    "count": 3,
    "position": {
      "x": 10.0,
      "y": 0.0,
      "z": 5.0
    }
  }
}
```

### 完整格式（包含元数据）

```json
{
  "schema": "SpawnEntity",
  "version": 1,
  "data": { ... },
  "_metadata": {
    "exportedAt": "2025-01-15T10:30:00Z",
    "rawData": {
      "intParams": [1001, 3],
      "floatParams": [10.0, 0.0, 5.0],
      "stringParams": []
    }
  }
}
```

**字段说明**:
- `schema`: 参数类型标识（如 "SpawnEntity"）
- `version`: 版本号（用于兼容性检测）
- `data`: 参数对象数据（键值对格式）
- `_metadata`: 可选元数据（调试和迁移用）

## 版本兼容性策略

### 版本检测

1. **显式版本号**: JSON中包含 `version` 字段
2. **特征推断**: 通过参数数量、字段存在性等推断版本
3. **混合策略**: 优先使用显式版本号，缺失时使用特征推断

### 兼容性处理

**字段名保持不变**: 已存在的字段名不再变更，确保向后兼容。

**容错导入**:
- 字段缺失 → 使用默认值 + 警告
- 类型不匹配 → 尝试转换 + 警告
- 结构变化 → 智能迁移 + 警告

**版本迁移**:
```csharp
if (jsonVersion < CurrentVersion)
{
    // 调用版本特定的导入方法
    return ImportV1Format(dataObj); // 或 ImportV2Format 等
}
```

### 迁移示例

**场景**: V1只有 `position`（点位置），V2支持 `position` 或 `range`（范围位置）

```csharp
private void ImportV1Format(JObject dataObj, SpawnEntityParameters result)
{
    // V1字段映射
    if (dataObj["entityId"] != null)
        result.EntityId = dataObj["entityId"].Value<int>();
    
    if (dataObj["position"] != null)
    {
        var pos = dataObj["position"] as JObject;
        result.Position = new Vector3(
            pos["x"]?.Value<float>() ?? 0,
            pos["y"]?.Value<float>() ?? 0,
            pos["z"]?.Value<float>() ?? 0
        );
    }
    
    // V2新增字段使用默认值
    // result.Range 已经在CreateDefault中设置了默认值
}
```

## 位置信息提取

### 实现模式

参数对象实现 `IPositionInfoProvider` 接口：

```csharp
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

### 使用场景

场景可视化管理器通过接口提取位置信息：

```csharp
if (action.Parameters is IPositionInfoProvider posInfo)
{
    var positionInfo = posInfo.GetPositionInfo();
    
    if (positionInfo.Type == PositionType.Point)
    {
        // 创建点位置可视化
        CreatePointVisualizer(positionInfo.Point.Value);
    }
    else if (positionInfo.Type == PositionType.Range)
    {
        // 创建范围可视化
        CreateRangeVisualizer(positionInfo.Range.Value);
    }
}
```

## 错误处理

### JsonImportResult

```csharp
public class JsonImportResult<T>
{
    public bool Success { get; set; }
    public T Parameters { get; set; }
    public List<ImportWarning> Warnings { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();
    public CompatibilityReport CompatibilityReport { get; set; }
}
```

**使用场景**:
- 导入成功但有小问题 → `Success = true`, `Warnings` 有内容
- 导入失败 → `Success = false`, `Errors` 有内容
- 版本迁移 → `CompatibilityReport` 包含迁移信息

### 用户反馈

编辑器显示导入结果：

```
✅ JSON导入成功

⚠️ 兼容性警告：
- 检测到旧版本数据 (v1)，已自动迁移到当前版本 (v2)
- 字段 'spawnDelay' 缺失，已使用默认值 0
- 字段 'position' 已转换为 'range' 格式

[查看详情] [接受] [取消]
```

## 扩展新类型

### 步骤

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
    
    // 实现所有接口方法
    public void Serialize(...) { ... }
    public NewActionParameters Deserialize(...) { ... }
    public string ExportToJson(...) { ... }
    public JsonImportResult<NewActionParameters> ImportFromJson(...) { ... }
    public NewActionParameters CreateDefault() { ... }
}
```

3. **注册转换器**:
```csharp
// 在 TriggerParameterConverterRegistry 的静态构造函数中
RegisterAction(TriggerActionType.NewAction, new NewActionParameterConverter());
```

### 最佳实践

- **字段命名**: 使用camelCase，与JSON字段名一致
- **版本管理**: 结构变化时递增版本号
- **默认值**: 提供合理的默认值，确保导入容错
- **位置信息**: 需要位置信息的类型实现 `IPositionInfoProvider`
- **错误处理**: 提供详细的警告和错误信息

## 相关文档

- [场景编辑器设计](Scene-Editor-Design%20场景编辑器设计.md)
- [CSV框架](../CSV-Framework%20CSV框架.md)

