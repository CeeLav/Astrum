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
                
                // 4. 合并数据
                result = MergeData(entityDataDict, roleDataDict, modelDataDict);
                
                Debug.Log($"{LOG_PREFIX} Successfully loaded {result.Count} roles");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read role data: {ex}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 合并EntityBaseTable和RoleBaseTable数据
        /// </summary>
        private static List<RoleEditorData> MergeData(
            Dictionary<int, EntityTableData> entityDataDict,
            Dictionary<int, RoleTableData> roleDataDict,
            Dictionary<int, EntityModelTableData> modelDataDict)
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
                    
                    // 从 EntityModelTable 获取模型路径
                    if (entityData.ModelId > 0 && modelDataDict.TryGetValue(entityData.ModelId, out var modelData))
                    {
                        roleData.ModelPath = modelData.ModelPath;
                        
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
                }
                
                // 从RoleBaseTable填充数据
                if (roleDataDict.TryGetValue(id, out var roleTableData))
                {
                    roleData.RoleId = roleTableData.RoleId;
                    roleData.RoleName = roleTableData.RoleName;
                    roleData.RoleDescription = roleTableData.RoleDescription;
                    roleData.RoleType = (RoleTypeEnum)roleTableData.RoleType;
                    roleData.BaseAttack = roleTableData.BaseAttack;
                    roleData.BaseDefense = roleTableData.BaseDefense;
                    roleData.BaseHealth = roleTableData.BaseHealth;
                    roleData.BaseSpeed = roleTableData.BaseSpeed;
                    roleData.AttackGrowth = roleTableData.AttackGrowth;
                    roleData.DefenseGrowth = roleTableData.DefenseGrowth;
                    roleData.HealthGrowth = roleTableData.HealthGrowth;
                    roleData.SpeedGrowth = roleTableData.SpeedGrowth;
                    roleData.LightAttackSkillId = roleTableData.LightAttackSkillId;
                    roleData.HeavyAttackSkillId = roleTableData.HeavyAttackSkillId;
                    roleData.Skill1Id = roleTableData.Skill1Id;
                    roleData.Skill2Id = roleTableData.Skill2Id;
                }
                else
                {
                    roleData.RoleId = id;
                }
                
                roleData.IsNew = false;
                roleData.IsDirty = false;
                
                result.Add(roleData);
            }
            
            return result;
        }
    }
}

