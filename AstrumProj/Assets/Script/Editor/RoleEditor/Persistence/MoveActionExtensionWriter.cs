using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 移动动作扩展数据写入器
    /// 写入 MoveActionTable
    /// </summary>
    public static class MoveActionExtensionWriter
    {
        private const string LOG_PREFIX = "[MoveActionExtensionWriter]";
        
        /// <summary>
        /// 写入所有移动扩展数据
        /// </summary>
        public static bool WriteAll(List<ActionEditorData> moveActions)
        {
            if (moveActions == null || moveActions.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} No move actions to write");
                return true; // 空数据不算失败
            }
            
            try
            {
                // 转换为表数据
                var tableDataList = new List<MoveActionTableData>();
                foreach (var action in moveActions)
                {
                    if (action.MoveExtension != null)
                    {
                        var tableData = ConvertToTableData(action);
                        tableDataList.Add(tableData);
                        
                        // 调试日志
                        if (tableData.RootMotionData != null && tableData.RootMotionData.Count > 0)
                        {
                            Debug.Log($"{LOG_PREFIX} Writing MoveAction {tableData.ActionId} with {tableData.RootMotionData.Count} root motion data points");
                        }
                        else
                        {
                            Debug.LogWarning($"{LOG_PREFIX} MoveAction {tableData.ActionId} has no root motion data");
                        }
                    }
                }
                
                if (tableDataList.Count == 0)
                {
                    Debug.LogWarning($"{LOG_PREFIX} No valid move extensions to write");
                    return true;
                }
                
                // 读取现有数据并合并
                var config = MoveActionTableData.GetTableConfig();
                var existing = LubanCSVReader.ReadTable<MoveActionTableData>(config);
                var cache = new Dictionary<int, MoveActionTableData>();
                
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
                    Debug.Log($"{LOG_PREFIX} Successfully wrote {tableDataList.Count} move extensions (total {merged.Count} records)");
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write MoveActionTable");
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write move extensions: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 转换编辑器数据为表数据
        /// </summary>
        private static MoveActionTableData ConvertToTableData(ActionEditorData action)
        {
            var extension = action.MoveExtension;
            
            return new MoveActionTableData
            {
                ActionId = action.ActionId,
                MoveSpeed = extension.MoveSpeed,
                RootMotionData = extension.RootMotionData ?? new List<int>()
            };
        }
    }
}

