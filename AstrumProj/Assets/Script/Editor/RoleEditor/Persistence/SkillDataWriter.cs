using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 技能数据写入器 - 将技能数据写入CSV文件
    /// </summary>
    public static class SkillDataWriter
    {
        private const string LOG_PREFIX = "[SkillDataWriter]";
        
        /// <summary>
        /// 写入所有技能数据
        /// </summary>
        public static bool WriteSkillData(List<SkillEditorData> skills)
        {
            if (skills == null || skills.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} 技能数据列表为空，无需写入");
                return false;
            }
            
            try
            {
                // 1. 转换为表格数据
                var skillTableList = new List<SkillTableData>();
                var skillActionTableList = new List<SkillActionTableData>();
                
                foreach (var skill in skills)
                {
                    // 转换 SkillTable 数据
                    var skillTable = ConvertToSkillTableData(skill);
                    skillTableList.Add(skillTable);
                    
                    // 转换 SkillActionTable 数据
                    foreach (var action in skill.SkillActions)
                    {
                        var actionTable = ConvertToSkillActionTableData(action);
                        skillActionTableList.Add(actionTable);
                    }
                }
                
                // 2. 写入 SkillTable
                var skillTableConfig = SkillTableData.GetTableConfig();
                bool skillTableSuccess = LubanCSVWriter.WriteTable(skillTableConfig, skillTableList);
                
                if (!skillTableSuccess)
                {
                    Debug.LogError($"{LOG_PREFIX} 写入 SkillTable 失败");
                    return false;
                }
                
                Debug.Log($"{LOG_PREFIX} 成功写入 {skillTableList.Count} 条 SkillTable 记录");
                
                // 3. 写入 SkillActionTable
                var skillActionTableConfig = SkillActionTableData.GetTableConfig();
                bool skillActionTableSuccess = LubanCSVWriter.WriteTable(skillActionTableConfig, skillActionTableList);
                
                if (!skillActionTableSuccess)
                {
                    Debug.LogError($"{LOG_PREFIX} 写入 SkillActionTable 失败");
                    return false;
                }
                
                Debug.Log($"{LOG_PREFIX} 成功写入 {skillActionTableList.Count} 条 SkillActionTable 记录");
                
                Debug.Log($"{LOG_PREFIX} 成功写入 {skills.Count} 个技能数据");
                
                // 自动打表
                Core.LubanTableGenerator.GenerateClientTables(showDialog: false);
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} 写入技能数据失败: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 写入单个技能数据
        /// </summary>
        public static bool WriteSkill(SkillEditorData skill)
        {
            // 读取现有数据
            var allSkills = SkillDataReader.ReadSkillData();
            
            // 查找并替换
            var existing = allSkills.FirstOrDefault(s => s.SkillId == skill.SkillId);
            if (existing != null)
            {
                allSkills.Remove(existing);
            }
            
            allSkills.Add(skill);
            
            // 写入所有数据
            return WriteSkillData(allSkills);
        }
        
        /// <summary>
        /// 删除技能数据
        /// </summary>
        public static bool DeleteSkill(int skillId)
        {
            // 读取现有数据
            var allSkills = SkillDataReader.ReadSkillData();
            
            // 移除指定技能
            var removed = allSkills.RemoveAll(s => s.SkillId == skillId);
            
            if (removed == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} 未找到技能ID {skillId}，无需删除");
                return false;
            }
            
            Debug.Log($"{LOG_PREFIX} 删除技能 {skillId}");
            
            // 写入所有数据
            return WriteSkillData(allSkills);
        }
        
        /// <summary>
        /// 转换为 SkillTableData
        /// </summary>
        private static SkillTableData ConvertToSkillTableData(SkillEditorData skill)
        {
            var data = new SkillTableData
            {
                Id = skill.SkillId,
                Name = skill.SkillName,
                Description = skill.SkillDescription,
                SkillType = (int)skill.SkillType,
                DisplayCooldown = skill.DisplayCooldown,
                DisplayCost = skill.DisplayCost,
                RequiredLevel = skill.RequiredLevel,
                MaxLevel = skill.MaxLevel,
                IconId = skill.IconId
            };
            
            // 设置技能动作ID列表
            var actionIds = skill.SkillActions.Select(a => a.ActionId).ToList();
            data.SetSkillActionIds(actionIds);
            
            return data;
        }
        
        /// <summary>
        /// 转换为 SkillActionTableData
        /// </summary>
        private static SkillActionTableData ConvertToSkillActionTableData(SkillActionData action)
        {
            var data = new SkillActionTableData
            {
                ActionId = action.ActionId,
                ActualCost = action.ActualCost,
                ActualCooldown = action.ActualCooldown
            };
            
            // 设置触发帧信息
            data.SetTriggerFrames(action.TriggerFrames);
            
            return data;
        }
        
        /// <summary>
        /// 验证写入前的数据
        /// </summary>
        public static bool ValidateBeforeWrite(List<SkillEditorData> skills, out List<string> errors)
        {
            errors = new List<string>();
            
            if (skills == null || skills.Count == 0)
            {
                errors.Add("技能数据列表为空");
                return false;
            }
            
            // 检查重复的技能ID
            var duplicateIds = skills.GroupBy(s => s.SkillId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (duplicateIds.Count > 0)
            {
                errors.Add($"存在重复的技能ID: {string.Join(", ", duplicateIds)}");
            }
            
            // 验证每个技能
            foreach (var skill in skills)
            {
                if (!SkillDataValidator.Validate(skill, out var skillErrors))
                {
                    errors.AddRange(skillErrors.Select(e => $"技能 [{skill.SkillId}] {skill.SkillName}: {e}"));
                }
            }
            
            // 检查 SkillActionTable 中的 ActionId 是否重复
            var allActionIds = skills.SelectMany(s => s.SkillActions.Select(a => a.ActionId)).ToList();
            var duplicateActionIds = allActionIds.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (duplicateActionIds.Count > 0)
            {
                errors.Add($"SkillActionTable 中存在重复的 ActionId（违反一一对应关系）: {string.Join(", ", duplicateActionIds)}");
            }
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// 批量更新技能数据
        /// </summary>
        public static bool BatchUpdate(List<SkillEditorData> skillsToUpdate)
        {
            if (skillsToUpdate == null || skillsToUpdate.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} 没有需要更新的技能");
                return false;
            }
            
            try
            {
                // 读取现有数据
                var allSkills = SkillDataReader.ReadSkillData();
                
                // 更新或添加
                foreach (var updateSkill in skillsToUpdate)
                {
                    var existing = allSkills.FirstOrDefault(s => s.SkillId == updateSkill.SkillId);
                    if (existing != null)
                    {
                        allSkills.Remove(existing);
                    }
                    allSkills.Add(updateSkill);
                }
                
                // 写入所有数据
                return WriteSkillData(allSkills);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} 批量更新失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 导出技能数据到备份文件
        /// </summary>
        public static bool ExportToBackup(List<SkillEditorData> skills, string backupSuffix = null)
        {
            if (string.IsNullOrEmpty(backupSuffix))
            {
                backupSuffix = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            
            try
            {
                // 修改配置路径为备份路径
                var skillTableConfig = SkillTableData.GetTableConfig();
                skillTableConfig.FilePath = skillTableConfig.FilePath.Replace(".csv", $"_backup_{backupSuffix}.csv");
                
                var skillActionTableConfig = SkillActionTableData.GetTableConfig();
                skillActionTableConfig.FilePath = skillActionTableConfig.FilePath.Replace(".csv", $"_backup_{backupSuffix}.csv");
                
                // 转换数据
                var skillTableList = skills.Select(ConvertToSkillTableData).ToList();
                var skillActionTableList = skills.SelectMany(s => s.SkillActions.Select(ConvertToSkillActionTableData)).ToList();
                
                // 写入备份文件
                bool success = LubanCSVWriter.WriteTable(skillTableConfig, skillTableList) &&
                               LubanCSVWriter.WriteTable(skillActionTableConfig, skillActionTableList);
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} 成功导出备份到: {backupSuffix}");
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} 导出备份失败: {ex.Message}");
                return false;
            }
        }
    }
}

