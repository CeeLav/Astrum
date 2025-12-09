using System;
using System.Collections.Generic;
using Astrum.Editor.SceneEditor.Parameters.Conditions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Astrum.Editor.SceneEditor.Converters.Conditions
{
    public class DelayParameterConverter : ITriggerParameterConverter<DelayParameters>
    {
        public int CurrentVersion => 1;
        
        public void Serialize(DelayParameters param, out int[] intParams, out float[] floatParams, out string[] stringParams)
        {
            intParams = new int[0];
            floatParams = new[] { param.DelaySeconds };
            stringParams = new string[0];
        }
        
        public DelayParameters Deserialize(int[] intParams, float[] floatParams, string[] stringParams)
        {
            return new DelayParameters
            {
                DelaySeconds = floatParams != null && floatParams.Length > 0 ? floatParams[0] : 0f
            };
        }
        
        public string ExportToJson(DelayParameters parameters, bool includeMetadata = false)
        {
            var jsonObj = new JObject
            {
                ["schema"] = "Delay",
                ["version"] = CurrentVersion,
                ["data"] = new JObject
                {
                    ["delaySeconds"] = parameters.DelaySeconds
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
        
        public JsonImportResult<DelayParameters> ImportFromJson(string json)
        {
            var result = new JsonImportResult<DelayParameters>();
            
            try
            {
                var jsonObj = JObject.Parse(json);
                var dataObj = jsonObj["data"] as JObject;
                
                if (dataObj == null)
                {
                    result.Errors.Add(new ImportError { Message = "缺少 'data' 字段" });
                    return result;
                }
                
                var parameters = CreateDefault();
                
                if (dataObj["delaySeconds"] != null)
                    parameters.DelaySeconds = dataObj["delaySeconds"].Value<float>();
                
                result.Parameters = parameters;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError { Message = $"JSON解析失败: {ex.Message}" });
            }
            
            return result;
        }
        
        public DelayParameters CreateDefault()
        {
            return new DelayParameters { DelaySeconds = 0f };
        }
    }
}

