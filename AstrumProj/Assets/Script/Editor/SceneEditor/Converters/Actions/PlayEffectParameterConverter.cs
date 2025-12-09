using System;
using System.Collections.Generic;
using Astrum.Editor.SceneEditor.Parameters.Actions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Astrum.Editor.SceneEditor.Converters.Actions
{
    public class PlayEffectParameterConverter : ITriggerParameterConverter<PlayEffectParameters>
    {
        public int CurrentVersion => 1;
        
        public void Serialize(PlayEffectParameters param, out int[] intParams, out float[] floatParams, out string[] stringParams)
        {
            intParams = new[] { param.EffectId };
            floatParams = new[] { param.Duration, param.Position.x, param.Position.y, param.Position.z };
            stringParams = new string[0];
        }
        
        public PlayEffectParameters Deserialize(int[] intParams, float[] floatParams, string[] stringParams)
        {
            var result = new PlayEffectParameters
            {
                EffectId = intParams != null && intParams.Length > 0 ? intParams[0] : 0,
                Duration = floatParams != null && floatParams.Length > 0 ? floatParams[0] : 0f,
                Position = floatParams != null && floatParams.Length >= 4 
                    ? new Vector3(floatParams[1], floatParams[2], floatParams[3])
                    : Vector3.zero
            };
            
            return result;
        }
        
        public string ExportToJson(PlayEffectParameters parameters, bool includeMetadata = false)
        {
            var jsonObj = new JObject
            {
                ["schema"] = "PlayEffect",
                ["version"] = CurrentVersion,
                ["data"] = new JObject
                {
                    ["effectId"] = parameters.EffectId,
                    ["duration"] = parameters.Duration,
                    ["position"] = new JObject
                    {
                        ["x"] = parameters.Position.x,
                        ["y"] = parameters.Position.y,
                        ["z"] = parameters.Position.z
                    }
                }
            };
            
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
        
        public JsonImportResult<PlayEffectParameters> ImportFromJson(string json)
        {
            var result = new JsonImportResult<PlayEffectParameters>();
            
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
                
                if (dataObj["effectId"] != null)
                    parameters.EffectId = dataObj["effectId"].Value<int>();
                
                if (dataObj["duration"] != null)
                    parameters.Duration = dataObj["duration"].Value<float>();
                
                if (dataObj["position"] != null)
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
                
                if (jsonVersion < CurrentVersion)
                {
                    result.CompatibilityReport = new CompatibilityReport
                    {
                        DetectedVersion = jsonVersion,
                        TargetVersion = CurrentVersion,
                        WasMigrated = true
                    };
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
        
        public PlayEffectParameters CreateDefault()
        {
            return new PlayEffectParameters
            {
                EffectId = 0,
                Duration = 0f,
                Position = Vector3.zero
            };
        }
    }
}

