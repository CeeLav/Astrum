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
        /// 写入角色数据（拆分并写入EntityBaseTable和RoleBaseTable）
        /// </summary>
        public static bool WriteRoleData(List<RoleEditorData> roles)
        {
            try
            {
                // 1. 拆分数据为Entity和Role
                var entityDataList = ConvertToEntityData(roles);
                var roleTableDataList = ConvertToRoleTableData(roles);
                
                // 2. 使用通用写入器写入EntityBaseTable
                bool entitySuccess = LubanCSVWriter.WriteTable(
                    EntityTableData.GetTableConfig(),
                    entityDataList,
                    enableBackup: true
                );
                
                // 3. 使用通用写入器写入RoleBaseTable
                bool roleSuccess = LubanCSVWriter.WriteTable(
                    RoleTableData.GetTableConfig(),
                    roleTableDataList,
                    enableBackup: true
                );
                
                if (entitySuccess && roleSuccess)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully saved {roles.Count} roles");
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
                ArchetypeName = r.ArchetypeName,
                ModelName = r.ModelName,
                ModelPath = r.ModelPath,
                IdleAction = r.IdleAction,
                WalkAction = r.WalkAction,
                RunAction = r.RunAction,
                JumpAction = r.JumpAction,
                BirthAction = r.BirthAction,
                DeathAction = r.DeathAction
            }).ToList();
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

