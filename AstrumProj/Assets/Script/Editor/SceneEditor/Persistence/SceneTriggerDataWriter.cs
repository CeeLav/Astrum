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
    public static class SceneTriggerDataWriter
    {
        private const string LOG_PREFIX = "[SceneTriggerDataWriter]";
        
        /// <summary>
        /// 写入所有Trigger数据（全量保存）
        /// </summary>
        public static bool WriteAll(List<SceneTriggerEditorData> triggers)
        {
            if (triggers == null || triggers.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} No trigger data to write");
                return false;
            }
            
            try
            {
                // 读取现有数据
                var existingTriggers = LubanCSVReader.ReadTable<SceneTriggerTableData>(SceneTriggerTableData.GetTableConfig());
                var existingConditions = LubanCSVReader.ReadTable<SceneTriggerConditionTableData>(SceneTriggerConditionTableData.GetTableConfig());
                var existingActions = LubanCSVReader.ReadTable<SceneTriggerActionTableData>(SceneTriggerActionTableData.GetTableConfig());
                
                // 转换为字典
                var triggerDict = existingTriggers.ToDictionary(t => t.ID);
                var conditionDict = existingConditions.ToDictionary(c => c.ID);
                var actionDict = existingActions.ToDictionary(a => a.ID);
                
                // 收集所有条件和动作
                var allConditions = new Dictionary<int, SceneTriggerConditionTableData>(conditionDict);
                var allActions = new Dictionary<int, SceneTriggerActionTableData>(actionDict);
                var allTriggers = new Dictionary<int, SceneTriggerTableData>(triggerDict);
                
                // 处理编辑器数据
                foreach (var trigger in triggers)
                {
                    // 更新或创建Trigger
                    if (!allTriggers.TryGetValue(trigger.TriggerId, out var triggerTable))
                    {
                        triggerTable = new SceneTriggerTableData { ID = trigger.TriggerId };
                        allTriggers[trigger.TriggerId] = triggerTable;
                    }
                    
                    triggerTable.ConditionList = trigger.Conditions.Select(c => c.ConditionId).ToList();
                    triggerTable.ActionList = trigger.Actions.Select(a => a.ActionId).ToList();
                    
                    // 处理条件
                    foreach (var condition in trigger.Conditions)
                    {
                        if (!allConditions.TryGetValue(condition.ConditionId, out var conditionTable))
                        {
                            conditionTable = new SceneTriggerConditionTableData { ID = condition.ConditionId };
                            allConditions[condition.ConditionId] = conditionTable;
                        }
                        
                        conditionTable.ConditionType = condition.ConditionType;
                        
                        // 使用转换器序列化参数
                        if (condition.Parameters != null)
                        {
                            var converter = TriggerParameterConverterRegistry.GetConditionConverter(condition.ConditionType);
                            if (converter != null)
                            {
                                SerializeParameters(
                                    converter,
                                    condition.Parameters,
                                    out var intParams,
                                    out var floatParams,
                                    out var stringParams
                                );
                                
                                conditionTable.IntParams = intParams?.ToList() ?? new List<int>();
                                conditionTable.FloatParams = floatParams?.ToList() ?? new List<float>();
                                conditionTable.StringParams = stringParams?.ToList() ?? new List<string>();
                            }
                        }
                    }
                    
                    // 处理动作
                    foreach (var action in trigger.Actions)
                    {
                        if (!allActions.TryGetValue(action.ActionId, out var actionTable))
                        {
                            actionTable = new SceneTriggerActionTableData { ID = action.ActionId };
                            allActions[action.ActionId] = actionTable;
                        }
                        
                        actionTable.ActionType = action.ActionType;
                        
                        // 使用转换器序列化参数
                        if (action.Parameters != null)
                        {
                            var converter = TriggerParameterConverterRegistry.GetActionConverter(action.ActionType);
                            if (converter != null)
                            {
                                SerializeParameters(
                                    converter,
                                    action.Parameters,
                                    out var intParams,
                                    out var floatParams,
                                    out var stringParams
                                );
                                
                                actionTable.IntParams = intParams?.ToList() ?? new List<int>();
                                actionTable.FloatParams = floatParams?.ToList() ?? new List<float>();
                                actionTable.StringParams = stringParams?.ToList() ?? new List<string>();
                            }
                        }
                    }
                }
                
                // 写入三张表
                bool triggerSuccess = LubanCSVWriter.WriteTable(
                    SceneTriggerTableData.GetTableConfig(),
                    allTriggers.Values.OrderBy(t => t.ID).ToList(),
                    enableBackup: true
                );
                
                bool conditionSuccess = LubanCSVWriter.WriteTable(
                    SceneTriggerConditionTableData.GetTableConfig(),
                    allConditions.Values.OrderBy(c => c.ID).ToList(),
                    enableBackup: true
                );
                
                bool actionSuccess = LubanCSVWriter.WriteTable(
                    SceneTriggerActionTableData.GetTableConfig(),
                    allActions.Values.OrderBy(a => a.ID).ToList(),
                    enableBackup: true
                );
                
                if (triggerSuccess && conditionSuccess && actionSuccess)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully saved {triggers.Count} triggers");
                    return true;
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to save trigger data");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write trigger data: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 使用转换器序列化参数（通过反射调用泛型方法）
        /// </summary>
        private static void SerializeParameters(object converter, object parameters, out int[] intParams, out float[] floatParams, out string[] stringParams)
        {
            intParams = null;
            floatParams = null;
            stringParams = null;
            
            if (converter == null || parameters == null) return;
            
            try
            {
                // 使用dynamic简化调用（运行时绑定）
                dynamic dynamicConverter = converter;
                dynamic dynamicParameters = parameters;
                
                dynamicConverter.Serialize(dynamicParameters, out int[] ints, out float[] floats, out string[] strings);
                
                intParams = ints;
                floatParams = floats;
                stringParams = strings;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to serialize parameters: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}

