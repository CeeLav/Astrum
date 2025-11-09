using System.Collections.Generic;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 移动动作扩展数据读取器
    /// 从 MoveActionTable 读取移动专属数据
    /// </summary>
    public static class MoveActionExtensionReader
    {
        private const string LOG_PREFIX = "[MoveActionExtensionReader]";
        
        /// <summary>
        /// 读取所有移动扩展数据，返回字典（ActionId -> Extension）
        /// </summary>
        public static Dictionary<int, MoveActionExtension> ReadAll()
        {
            var result = new Dictionary<int, MoveActionExtension>();
            
            try
            {
                var config = MoveActionTableData.GetTableConfig();
                var tableData = LubanCSVReader.ReadTable<MoveActionTableData>(config);
                
                foreach (var data in tableData)
                {
                    var extension = ConvertToExtension(data);
                    if (extension != null && !result.ContainsKey(data.ActionId))
                    {
                        result[data.ActionId] = extension;
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Loaded {result.Count} move extensions");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to read MoveActionTable: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 转换表数据为扩展数据
        /// </summary>
        private static MoveActionExtension ConvertToExtension(MoveActionTableData tableData)
        {
            if (tableData == null) return null;
            
            return new MoveActionExtension
            {
                MoveSpeed = tableData.MoveSpeed,
                RootMotionData = tableData.RootMotionData ?? new List<int>()
            };
        }
    }
}

