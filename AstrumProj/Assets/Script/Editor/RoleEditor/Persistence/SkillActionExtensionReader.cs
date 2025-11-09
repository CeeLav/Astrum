using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 技能动作扩展数据读取器
    /// 从 SkillActionTable 读取技能专属数据
    /// </summary>
    public static class SkillActionExtensionReader
    {
        private const string LOG_PREFIX = "[SkillActionExtensionReader]";
        
        /// <summary>
        /// 读取所有技能扩展数据，返回字典（ActionId -> Extension）
        /// </summary>
        public static Dictionary<int, SkillActionExtension> ReadAll()
        {
            var result = new Dictionary<int, SkillActionExtension>();
            
            try
            {
                var config = SkillActionTableData.GetTableConfig();
                var tableData = LubanCSVReader.ReadTable<SkillActionTableData>(config);
                
                foreach (var data in tableData)
                {
                    var extension = ConvertToExtension(data);
                    if (extension != null && !result.ContainsKey(data.ActionId))
                    {
                        result[data.ActionId] = extension;
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Loaded {result.Count} skill extensions");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to read SkillActionTable: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 转换表数据为扩展数据
        /// </summary>
        private static SkillActionExtension ConvertToExtension(SkillActionTableData tableData)
        {
            if (tableData == null) return null;
            
            var triggerFrames = tableData.TriggerFrames ?? "";
            var triggerEffects = TriggerFrameData.ParseFromJSON(triggerFrames);
            
            Debug.Log($"{LOG_PREFIX} Converting ActionId {tableData.ActionId}: TriggerFrames length={triggerFrames.Length}, TriggerEffects count={triggerEffects?.Count ?? 0}");
            
            if (!string.IsNullOrEmpty(triggerFrames) && (triggerEffects == null || triggerEffects.Count == 0))
            {
                Debug.LogWarning($"{LOG_PREFIX} ActionId {tableData.ActionId} has TriggerFrames data but failed to parse: {triggerFrames.Substring(0, Math.Min(100, triggerFrames.Length))}...");
            }
            
            return new SkillActionExtension
            {
                ActualCost = tableData.ActualCost,
                ActualCooldown = tableData.ActualCooldown,
                TriggerFrames = triggerFrames,
                TriggerEffects = triggerEffects ?? new List<TriggerFrameData>(),
                RootMotionData = tableData.RootMotionData ?? new List<int>()
            };
        }
    }
}

