using System;
using System.Collections.Generic;
using Astrum.Editor.SceneEditor.Parameters.Conditions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Astrum.Editor.SceneEditor.Converters.Conditions
{
    public class SceneStartParameterConverter : ITriggerParameterConverter<SceneStartParameters>
    {
        public int CurrentVersion => 1;
        
        public void Serialize(SceneStartParameters param, out int[] intParams, out float[] floatParams, out string[] stringParams)
        {
            intParams = new int[0];
            floatParams = new float[0];
            stringParams = new string[0];
        }
        
        public SceneStartParameters Deserialize(int[] intParams, float[] floatParams, string[] stringParams)
        {
            return new SceneStartParameters();
        }
        
        public string ExportToJson(SceneStartParameters parameters, bool includeMetadata = false)
        {
            var jsonObj = new JObject
            {
                ["schema"] = "SceneStart",
                ["version"] = CurrentVersion,
                ["data"] = new JObject()
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
        
        public JsonImportResult<SceneStartParameters> ImportFromJson(string json)
        {
            var result = new JsonImportResult<SceneStartParameters>();
            
            try
            {
                var jsonObj = JObject.Parse(json);
                result.Parameters = CreateDefault();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError { Message = $"JSON解析失败: {ex.Message}" });
            }
            
            return result;
        }
        
        public SceneStartParameters CreateDefault()
        {
            return new SceneStartParameters();
        }
    }
}

