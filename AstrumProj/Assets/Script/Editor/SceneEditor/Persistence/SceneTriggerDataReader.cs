using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Astrum.Editor.SceneEditor.Data;
using Astrum.Editor.SceneEditor.Persistence.Mappings;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.SceneEditor.Registry;
using Astrum.Editor.SceneEditor.Converters;
using cfg;

namespace Astrum.Editor.SceneEditor.Persistence
{
    public static class SceneTriggerDataReader
    {
        private const string LOG_PREFIX = "[SceneTriggerDataReader]";
        
        /// <summary>
        /// 读取所有Trigger数据
        /// </summary>
        public static List<SceneTriggerEditorData> ReadAll()
        {
            var result = new List<SceneTriggerEditorData>();
            
            try
            {
                // 读取三张表
                var triggerTableList = LubanCSVReader.ReadTable<SceneTriggerTableData>(SceneTriggerTableData.GetTableConfig());
                var conditionTableList = LubanCSVReader.ReadTable<SceneTriggerConditionTableData>(SceneTriggerConditionTableData.GetTableConfig());
                var actionTableList = LubanCSVReader.ReadTable<SceneTriggerActionTableData>(SceneTriggerActionTableData.GetTableConfig());
                
                // 转换为字典便于查找
                var conditionDict = conditionTableList.ToDictionary(c => c.ID);
                var actionDict = actionTableList.ToDictionary(a => a.ID);
                
                // 组装编辑器数据
                foreach (var triggerTable in triggerTableList)
                {
                    var editorData = new SceneTriggerEditorData
                    {
                        TriggerId = triggerTable.ID
                    };
                    
                    // 读取条件
                    foreach (var conditionId in triggerTable.ConditionList ?? new List<int>())
                    {
                        if (conditionDict.TryGetValue(conditionId, out var conditionTable))
                        {
                            var conditionData = new ConditionEditorData
                            {
                                ConditionId = conditionTable.ID,
                                ConditionType = conditionTable.ConditionType
                            };
                            
                            // 使用转换器反序列化参数
                            var converter = TriggerParameterConverterRegistry.GetConditionConverter(conditionTable.ConditionType);
                            if (converter != null)
                            {
                                conditionData.Parameters = DeserializeParameters(
                                    converter,
                                    conditionTable.IntParams?.ToArray() ?? new int[0],
                                    conditionTable.FloatParams?.ToArray() ?? new float[0],
                                    conditionTable.StringParams?.ToArray() ?? new string[0]
                                );
                            }
                            
                            editorData.Conditions.Add(conditionData);
                        }
                    }
                    
                    // 读取动作
                    foreach (var actionId in triggerTable.ActionList ?? new List<int>())
                    {
                        if (actionDict.TryGetValue(actionId, out var actionTable))
                        {
                            var actionData = new ActionEditorData
                            {
                                ActionId = actionTable.ID,
                                ActionType = actionTable.ActionType
                            };
                            
                            // 使用转换器反序列化参数
                            var converter = TriggerParameterConverterRegistry.GetActionConverter(actionTable.ActionType);
                            if (converter != null)
                            {
                                actionData.Parameters = DeserializeParameters(
                                    converter,
                                    actionTable.IntParams?.ToArray() ?? new int[0],
                                    actionTable.FloatParams?.ToArray() ?? new float[0],
                                    actionTable.StringParams?.ToArray() ?? new string[0]
                                );
                            }
                            
                            editorData.Actions.Add(actionData);
                        }
                    }
                    
                    result.Add(editorData);
                }
                
                Debug.Log($"{LOG_PREFIX} Successfully loaded {result.Count} triggers");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read trigger data: {ex.Message}\n{ex.StackTrace}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 使用转换器反序列化参数（通过反射调用泛型方法）
        /// </summary>
        private static object DeserializeParameters(object converter, int[] intParams, float[] floatParams, string[] stringParams)
        {
            if (converter == null) return null;
            
            var converterType = converter.GetType();
            var deserializeMethod = converterType.GetMethod("Deserialize");
            
            if (deserializeMethod != null)
            {
                try
                {
                    return deserializeMethod.Invoke(converter, new object[] { intParams, floatParams, stringParams });
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to deserialize parameters: {ex.Message}");
                }
            }
            
            return null;
        }
    }
}

