using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 角色数据读取器 - 读取EntityBaseTable和RoleBaseTable并合并（使用通用框架）
    /// </summary>
    public static class RoleDataReader
    {
        private const string LOG_PREFIX = "[RoleEditor]";
        
        /// <summary>
        /// 读取角色数据（合并EntityBaseTable和RoleBaseTable）
        /// </summary>
        public static List<RoleEditorData> ReadRoleData()
        {
            var result = new List<RoleEditorData>();
            
            try
            {
                // 1. 使用通用读取器读取EntityBaseTable
                var entityDataList = LubanCSVReader.ReadTable<EntityTableData>(EntityTableData.GetTableConfig());
                var entityDataDict = entityDataList.ToDictionary(e => e.EntityId);
                
                // 2. 使用通用读取器读取RoleBaseTable
                var roleDataList = LubanCSVReader.ReadTable<RoleTableData>(RoleTableData.GetTableConfig());
                var roleDataDict = roleDataList.ToDictionary(r => r.RoleId);
                
                // 3. 读取 EntityModelTable
                var modelDataDict = EntityModelDataReader.ReadAsDictionary();
                
                // 4. 读取 EntityStatsTable
                var statsDataList = LubanCSVReader.ReadTable<EntityStatsTableData>(EntityStatsTableData.GetTableConfig());
                var statsDataDict = statsDataList.ToDictionary(s => s.EntityId);
                
                // 5. 合并数据
                result = MergeData(entityDataDict, roleDataDict, modelDataDict, statsDataDict);
                
                Debug.Log($"{LOG_PREFIX} Successfully loaded {result.Count} roles");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read role data: {ex}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 合并EntityBaseTable、RoleBaseTable和EntityStatsTable数据
        /// </summary>
        private static List<RoleEditorData> MergeData(
            Dictionary<int, EntityTableData> entityDataDict,
            Dictionary<int, RoleTableData> roleDataDict,
            Dictionary<int, EntityModelTableData> modelDataDict,
            Dictionary<int, EntityStatsTableData> statsDataDict)
        {
            var result = new List<RoleEditorData>();
            
            // 找到所有角色ID（取Entity和Role的并集）
            var allIds = new HashSet<int>();
            allIds.UnionWith(entityDataDict.Keys);
            allIds.UnionWith(roleDataDict.Keys);
            
            foreach (var id in allIds.OrderBy(x => x))
            {
                var roleData = new RoleEditorData();
                
                // 从EntityBaseTable填充数据
                if (entityDataDict.TryGetValue(id, out var entityData))
                {
                    roleData.EntityId = entityData.EntityId;
                    roleData.ArchetypeName = entityData.ArchetypeName;
                    roleData.ModelName = entityData.EntityName;  // 使用 EntityName 作为 ModelName
                    roleData.IdleAction = entityData.IdleAction;
                    roleData.WalkAction = entityData.WalkAction;
                    roleData.RunAction = entityData.RunAction;
                    roleData.JumpAction = entityData.JumpAction;
                    roleData.BirthAction = entityData.BirthAction;
                    roleData.DeathAction = entityData.DeathAction;
                    roleData.HitAction = entityData.HitAction;
                    
                    // 从 EntityModelTable 获取模型路径和碰撞数据
                    if (entityData.ModelId > 0 && modelDataDict.TryGetValue(entityData.ModelId, out var modelData))
                    {
                        roleData.ModelPath = modelData.ModelPath;
                        roleData.CollisionData = modelData.CollisionData ?? string.Empty;  // 加载碰撞数据
                        
                        // 尝试加载模型Prefab
                        if (!string.IsNullOrEmpty(modelData.ModelPath))
                        {
                            roleData.ModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelData.ModelPath);
                        }
                    }
                }
                else
                {
                    roleData.EntityId = id;
                    roleData.HitAction = 0;
                }
                
                // 从RoleBaseTable填充数据
                if (roleDataDict.TryGetValue(id, out var roleTableData))
                {
                    roleData.RoleId = roleTableData.RoleId;
                    roleData.RoleName = roleTableData.RoleName;
                    roleData.RoleDescription = roleTableData.RoleDescription;
                    roleData.RoleType = (RoleTypeEnum)roleTableData.RoleType;
                    roleData.DefaultSkillIds = roleTableData.DefaultSkillIds != null
                        ? new List<int>(roleTableData.DefaultSkillIds)
                        : new List<int>();
                }
                else
                {
                    roleData.RoleId = id;
                    roleData.DefaultSkillIds = new List<int>();
                }
                
                // 从 EntityStatsTable 填充属性数据
                if (statsDataDict.TryGetValue(id, out var statsData))
                {
                    roleData.BaseAttack = statsData.BaseAttack;
                    roleData.BaseDefense = statsData.BaseDefense;
                    roleData.BaseHealth = statsData.BaseHealth;
                    roleData.BaseSpeed = statsData.BaseSpeed / 1000f;  // 转换为 m/s
                    
                    roleData.BaseCritRate = statsData.BaseCritRate / 10f;  // 转换为 %
                    roleData.BaseCritDamage = statsData.BaseCritDamage / 10f;
                    roleData.BaseAccuracy = statsData.BaseAccuracy / 10f;
                    roleData.BaseEvasion = statsData.BaseEvasion / 10f;
                    roleData.BaseBlockRate = statsData.BaseBlockRate / 10f;
                    roleData.BaseBlockValue = statsData.BaseBlockValue;
                    roleData.PhysicalRes = statsData.PhysicalRes / 10f;
                    roleData.MagicalRes = statsData.MagicalRes / 10f;
                    roleData.BaseMaxMana = statsData.BaseMaxMana;
                    roleData.ManaRegen = statsData.ManaRegen / 1000f;
                    roleData.HealthRegen = statsData.HealthRegen / 1000f;
                }
                else
                {
                    // 使用默认值
                    roleData.BaseAttack = 50;
                    roleData.BaseDefense = 50;
                    roleData.BaseHealth = 1000;
                    roleData.BaseSpeed = 5f;
                    roleData.BaseCritRate = 5f;
                    roleData.BaseCritDamage = 200f;
                    roleData.BaseAccuracy = 95f;
                    roleData.BaseEvasion = 5f;
                    roleData.BaseBlockRate = 15f;
                    roleData.BaseBlockValue = 60;
                    roleData.PhysicalRes = 10f;
                    roleData.MagicalRes = 0f;
                    roleData.BaseMaxMana = 100;
                    roleData.ManaRegen = 5f;
                    roleData.HealthRegen = 2f;
                }
                
                roleData.IsNew = false;
                roleData.IsDirty = false;
                
                result.Add(roleData);
            }
            
            return result;
        }
    }
}

