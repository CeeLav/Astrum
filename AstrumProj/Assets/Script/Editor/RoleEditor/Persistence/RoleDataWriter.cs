using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 角色数据写入器 - 拆分RoleEditorData并写入EntityBaseTable和RoleBaseTable（使用通用框架）
    /// </summary>
    public static class RoleDataWriter
    {
        private const string LOG_PREFIX = "[RoleEditor]";
        
        /// <summary>
        /// 写入角色数据（拆分并写入EntityBaseTable、EntityModelTable和RoleBaseTable）
        /// </summary>
        public static bool WriteRoleData(List<RoleEditorData> roles)
        {
            try
            {
                // 1. 拆分数据为Entity、Model和Role
                var entityDataList = ConvertToEntityData(roles);
                var modelDataList = ConvertToModelData(roles);
                var roleTableDataList = ConvertToRoleTableData(roles);
                
                // 2. 使用通用写入器写入EntityBaseTable
                bool entitySuccess = LubanCSVWriter.WriteTable(
                    EntityTableData.GetTableConfig(),
                    entityDataList,
                    enableBackup: true
                );
                
                // 3. 写入 EntityModelTable（如果有模型数据）
                bool modelSuccess = true;
                if (modelDataList.Count > 0)
                {
                    modelSuccess = EntityModelDataWriter.WriteAll(modelDataList, enableBackup: true);
                }
                
                // 4. 使用通用写入器写入RoleBaseTable
                bool roleSuccess = LubanCSVWriter.WriteTable(
                    RoleTableData.GetTableConfig(),
                    roleTableDataList,
                    enableBackup: true
                );
                
                if (entitySuccess && modelSuccess && roleSuccess)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully saved {roles.Count} roles");
                    
                    // 自动打表
                    Core.LubanTableGenerator.GenerateClientTables(showDialog: false);
                    
                    return true;
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to save role data");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write role data: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 转换为EntityTableData列表
        /// </summary>
        private static List<EntityTableData> ConvertToEntityData(List<RoleEditorData> roles)
        {
            return roles.OrderBy(r => r.EntityId).Select(r => new EntityTableData
            {
                EntityId = r.EntityId,
                EntityName = r.ModelName,  // 使用 ModelName 作为 EntityName
                ArchetypeName = r.ArchetypeName,
                ModelId = r.EntityId + 1000,  // 临时方案：EntityId + 1000 作为 ModelId（后续可优化）
                IdleAction = r.IdleAction,
                WalkAction = r.WalkAction,
                RunAction = r.RunAction,
                JumpAction = r.JumpAction,
                BirthAction = r.BirthAction,
                DeathAction = r.DeathAction
            }).ToList();
        }
        
        /// <summary>
        /// 转换为 EntityModelTableData 列表
        /// </summary>
        private static List<EntityModelTableData> ConvertToModelData(List<RoleEditorData> roles)
        {
            // 合并现有的 EntityModelTable 数据
            var existingModels = EntityModelDataReader.ReadAsDictionary();
            var modelDataList = new List<EntityModelTableData>();
            
            foreach (var role in roles.OrderBy(r => r.EntityId))
            {
                if (string.IsNullOrEmpty(role.ModelPath))
                    continue;
                
                var modelId = role.EntityId + 1000;  // 临时方案：EntityId + 1000 作为 ModelId
                
                // 检查是否已存在
                if (existingModels.TryGetValue(modelId, out var existingModel))
                {
                    // 更新现有数据
                    existingModel.ModelPath = role.ModelPath;
                    existingModel.CollisionData = role.CollisionData ?? string.Empty;  // 更新碰撞数据
                    modelDataList.Add(existingModel);
                }
                else
                {
                    // 创建新数据
                    modelDataList.Add(new EntityModelTableData
                    {
                        ModelId = modelId,
                        ModelPath = role.ModelPath,
                        CollisionData = role.CollisionData ?? string.Empty  // 保存碰撞盒数据
                    });
                }
            }
            
            return modelDataList;
        }
        
        /// <summary>
        /// 转换为RoleTableData列表
        /// </summary>
        private static List<RoleTableData> ConvertToRoleTableData(List<RoleEditorData> roles)
        {
            return roles.OrderBy(r => r.RoleId).Select(r => new RoleTableData
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                RoleDescription = r.RoleDescription,
                RoleType = (int)r.RoleType,
                BaseAttack = r.BaseAttack,
                BaseDefense = r.BaseDefense,
                BaseHealth = r.BaseHealth,
                BaseSpeed = r.BaseSpeed,
                AttackGrowth = r.AttackGrowth,
                DefenseGrowth = r.DefenseGrowth,
                HealthGrowth = r.HealthGrowth,
                SpeedGrowth = r.SpeedGrowth,
                LightAttackSkillId = r.LightAttackSkillId,
                HeavyAttackSkillId = r.HeavyAttackSkillId,
                Skill1Id = r.Skill1Id,
                Skill2Id = r.Skill2Id
            }).ToList();
        }
    }
}

