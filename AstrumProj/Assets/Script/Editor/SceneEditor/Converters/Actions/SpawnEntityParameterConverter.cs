using System;
using System.Collections.Generic;
using Astrum.Editor.SceneEditor.Parameters.Actions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Astrum.Editor.SceneEditor.Converters.Actions
{
    public class SpawnEntityParameterConverter : ITriggerParameterConverter<SpawnEntityParameters>
    {
        public int CurrentVersion => 1;
        
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
        
        public SpawnEntityParameters Deserialize(int[] intParams, float[] floatParams, string[] stringParams)
        {
            var result = new SpawnEntityParameters
            {
                EntityId = intParams != null && intParams.Length > 0 ? intParams[0] : 0,
                Count = intParams != null && intParams.Length > 1 ? intParams[1] : 1
            };
            
            // 根据数组长度判断格式
            if (floatParams != null && floatParams.Length >= 6)
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
            else if (floatParams != null && floatParams.Length >= 3)
            {
                // 点格式
                result.Position = new Vector3(floatParams[0], floatParams[1], floatParams[2]);
            }
            
            return result;
        }
        
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
                    ["center"] = new JObject
                    {
                        ["x"] = bounds.center.x,
                        ["y"] = bounds.center.y,
                        ["z"] = bounds.center.z
                    },
                    ["size"] = new JObject
                    {
                        ["x"] = bounds.size.x,
                        ["y"] = bounds.size.y,
                        ["z"] = bounds.size.z
                    }
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
                    
                    if (center != null && size != null)
                    {
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
                }
                else if (dataObj["position"] != null)
                {
                    var posObj = dataObj["position"] as JObject;
                    if (posObj != null)
                    {
                        parameters.Position = new Vector3(
                            posObj["x"]?.Value<float>() ?? 0,
                            posObj["y"]?.Value<float>() ?? 0,
                            posObj["z"]?.Value<float>() ?? 0
                        );
                    }
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
}

