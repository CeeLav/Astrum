using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 技能动作扩展数据写入器
    /// 写入 SkillActionTable
    /// </summary>
    public static class SkillActionExtensionWriter
    {
        private const string LOG_PREFIX = "[SkillActionExtensionWriter]";
        
        /// <summary>
        /// 写入所有技能扩展数据
        /// </summary>
        public static bool WriteAll(List<ActionEditorData> skillActions)
        {
            if (skillActions == null || skillActions.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} No skill actions to write");
                return true; // 空数据不算失败
            }
            
            try
            {
                // 转换为表数据
                var tableDataList = new List<SkillActionTableData>();
                foreach (var action in skillActions)
                {
                    if (action.SkillExtension != null)
                    {
                        var tableData = ConvertToTableData(action);
                        tableDataList.Add(tableData);
                    }
                }
                
                if (tableDataList.Count == 0)
                {
                    Debug.LogWarning($"{LOG_PREFIX} No valid skill extensions to write");
                    return true;
                }
                
                // 读取现有数据并合并
                var config = SkillActionTableData.GetTableConfig();
                var existing = LubanCSVReader.ReadTable<SkillActionTableData>(config);
                var cache = new Dictionary<int, SkillActionTableData>();
                
                foreach (var entry in existing)
                {
                    if (!cache.ContainsKey(entry.ActionId))
                    {
                        cache[entry.ActionId] = entry;
                    }
                }
                
                // 更新新数据
                foreach (var entry in tableDataList)
                {
                    cache[entry.ActionId] = entry;
                }
                
                // 排序并写入
                var merged = cache.Values.OrderBy(data => data.ActionId).ToList();
                bool success = LubanCSVWriter.WriteTable(config, merged, enableBackup: true);
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote {tableDataList.Count} skill extensions (total {merged.Count} records)");
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write SkillActionTable");
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write skill extensions: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 转换编辑器数据为表数据
        /// </summary>
        private static SkillActionTableData ConvertToTableData(ActionEditorData action)
        {
            var extension = action.SkillExtension;
            
            return new SkillActionTableData
            {
                ActionId = action.ActionId,
                ActualCost = extension.ActualCost,
                ActualCooldown = extension.ActualCooldown,
                TriggerFrames = extension.TriggerFrames ?? "",
                RootMotionData = extension.RootMotionData ?? new List<int>()
            };
        }
    }
}

