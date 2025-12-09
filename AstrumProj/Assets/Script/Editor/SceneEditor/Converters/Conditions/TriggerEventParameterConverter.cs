using System;
using System.Collections.Generic;
using Astrum.Editor.SceneEditor.Parameters.Conditions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Astrum.Editor.SceneEditor.Converters.Conditions
{
    public class TriggerEventParameterConverter : ITriggerParameterConverter<TriggerEventParameters>
    {
        public int CurrentVersion => 1;
        
        public void Serialize(TriggerEventParameters param, out int[] intParams, out float[] floatParams, out string[] stringParams)
        {
            intParams = new[] { param.TriggerId };
            floatParams = new float[0];
            stringParams = new string[0];
        }
        
        public TriggerEventParameters Deserialize(int[] intParams, float[] floatParams, string[] stringParams)
        {
            return new TriggerEventParameters
            {
                TriggerId = intParams != null && intParams.Length > 0 ? intParams[0] : 0
            };
        }
        
        public string ExportToJson(TriggerEventParameters parameters, bool includeMetadata = false)
        {
            var jsonObj = new JObject
            {
                ["schema"] = "TriggerEvent",
                ["version"] = CurrentVersion,
                ["data"] = new JObject
                {
                    ["triggerId"] = parameters.TriggerId
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
        
        public JsonImportResult<TriggerEventParameters> ImportFromJson(string json)
        {
            var result = new JsonImportResult<TriggerEventParameters>();
            
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
                
                if (dataObj["triggerId"] != null)
                    parameters.TriggerId = dataObj["triggerId"].Value<int>();
                
                result.Parameters = parameters;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError { Message = $"JSON解析失败: {ex.Message}" });
            }
            
            return result;
        }
        
        public TriggerEventParameters CreateDefault()
        {
            return new TriggerEventParameters { TriggerId = 0 };
        }
    }
}

