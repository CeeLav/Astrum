using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 弹道表读取服务
    /// </summary>
    public static class ProjectileDataReader
    {
        private const string LOG_PREFIX = "[ProjectileDataReader]";

        private static List<ProjectileTableData> _cache;
        private static Dictionary<int, ProjectileTableData> _cacheById;

        public static List<ProjectileTableData> ReadAll()
        {
            if (_cache != null)
            {
                return _cache;
            }

            try
            {
                var config = ProjectileTableData.GetTableConfig();
                _cache = LubanCSVReader.ReadTable<ProjectileTableData>(config);
                Debug.Log($"{LOG_PREFIX} Loaded {_cache.Count} projectile rows");
                return _cache;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read projectile table: {ex.Message}");
                _cache = new List<ProjectileTableData>();
                return _cache;
            }
        }

        public static ProjectileTableData GetProjectile(int projectileId)
        {
            if (projectileId <= 0)
            {
                return null;
            }

            if (_cacheById == null)
            {
                _cacheById = ReadAll().ToDictionary(p => p.ProjectileId);
            }

            return _cacheById.TryGetValue(projectileId, out var projectile) ? projectile : null;
        }

        public static void ClearCache()
        {
            _cache = null;
            _cacheById = null;
            Debug.Log($"{LOG_PREFIX} cache cleared");
        }
    }
}
