using System.Collections.Generic;
using System.Linq;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 弹道数据写入器
    /// 将编辑器数据写入到 ProjectileTable.csv
    /// </summary>
    public static class ProjectileDataWriter
    {
        private const string LOG_PREFIX = "[ProjectileDataWriter]";

        /// <summary>
        /// 保存单个弹道配置
        /// </summary>
        public static bool SaveProjectile(ProjectileTableData updated)
        {
            if (updated == null)
            {
                Debug.LogError($"{LOG_PREFIX} Updated projectile data is null");
                return false;
            }

            var config = ProjectileTableData.GetTableConfig();
            var allProjectiles = ReadAllProjectiles();
            var index = allProjectiles.FindIndex(p => p.ProjectileId == updated.ProjectileId);

            var workingList = new List<ProjectileTableData>();
            workingList.AddRange(allProjectiles.Select(p => p.ProjectileId == updated.ProjectileId ? updated.Clone() : p.Clone()));

            if (index < 0)
            {
                workingList.Add(updated.Clone());
            }

            bool success = LubanCSVWriter.WriteTable(config, workingList);
            if (success)
            {
                Debug.Log($"{LOG_PREFIX} Saved projectile {updated.ProjectileId} successfully");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} Failed to save projectile {updated.ProjectileId}");
            }

            return success;
        }

        /// <summary>
        /// 读取所有弹道数据
        /// </summary>
        public static List<ProjectileTableData> ReadAllProjectiles()
        {
            var config = ProjectileTableData.GetTableConfig();
            return LubanCSVReader.ReadTable<ProjectileTableData>(config);
        }

        /// <summary>
        /// 根据ID读取弹道数据
        /// </summary>
        public static ProjectileTableData GetProjectile(int projectileId)
        {
            var allProjectiles = ReadAllProjectiles();
            return allProjectiles.FirstOrDefault(p => p.ProjectileId == projectileId);
        }
    }
}
