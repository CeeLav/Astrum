using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 技能数据读取器 - 从CSV文件读取技能数据
    /// </summary>
    public static class SkillDataReader
    {
        private const string LOG_PREFIX = "[SkillDataReader]";
        
        /// <summary>
        /// 读取所有技能数据
        /// </summary>
        public static List<SkillEditorData> ReadSkillData()
        {
            var result = new List<SkillEditorData>();
            
            try
            {
                // 1. 读取 SkillTable
                var skillTableConfig = SkillTableData.GetTableConfig();
                var skillTableList = LubanCSVReader.ReadTable<SkillTableData>(skillTableConfig);
                
                Debug.Log($"{LOG_PREFIX} 读取到 {skillTableList.Count} 条 SkillTable 记录");
                
                // 2. 读取 SkillActionTable
                var skillActionTableConfig = SkillActionTableData.GetTableConfig();
                var skillActionTableList = LubanCSVReader.ReadTable<SkillActionTableData>(skillActionTableConfig);
                
                Debug.Log($"{LOG_PREFIX} 读取到 {skillActionTableList.Count} 条 SkillActionTable 记录");
                
                // 3. 按SkillId分组SkillActionTable
                var actionsBySkillId = skillActionTableList
                    .GroupBy(a => a.SkillId)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                // 4. 组装 SkillEditorData
                foreach (var skillTable in skillTableList)
                {
                    var editorData = ConvertToEditorData(skillTable, actionsBySkillId);
                    if (editorData != null)
                    {
                        result.Add(editorData);
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} 成功读取 {result.Count} 个技能数据");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} 读取技能数据失败: {ex.Message}\n{ex.StackTrace}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 读取单个技能数据
        /// </summary>
        public static SkillEditorData ReadSkillById(int skillId)
        {
            var allSkills = ReadSkillData();
            return allSkills.FirstOrDefault(s => s.SkillId == skillId);
        }
        
        /// <summary>
        /// 转换为编辑器数据
        /// </summary>
        private static SkillEditorData ConvertToEditorData(
            SkillTableData skillTable, 
            Dictionary<int, List<SkillActionTableData>> actionsBySkillId)
        {
            var data = SkillEditorData.CreateInstance<SkillEditorData>();
            
            // 映射 SkillTable 字段
            data.SkillId = skillTable.Id;
            data.SkillName = skillTable.Name ?? "";
            data.SkillDescription = skillTable.Description ?? "";
            data.SkillType = (SkillTypeEnum)skillTable.SkillType;
            data.DisplayCooldown = skillTable.DisplayCooldown;
            data.DisplayCost = skillTable.DisplayCost;
            data.RequiredLevel = skillTable.RequiredLevel;
            data.MaxLevel = skillTable.MaxLevel;
            data.IconId = skillTable.IconId;
            
            // 映射 SkillActionTable 数据
            data.SkillActions = new List<SkillActionData>();
            
            if (actionsBySkillId.TryGetValue(skillTable.Id, out var actions))
            {
                foreach (var actionTable in actions)
                {
                    var actionData = new SkillActionData
                    {
                        ActionId = actionTable.ActionId,
                        SkillId = actionTable.SkillId,
                        AttackBoxInfo = actionTable.AttackBoxInfo ?? "",
                        ActualCost = actionTable.ActualCost,
                        ActualCooldown = actionTable.ActualCooldown,
                        TriggerFrames = actionTable.ParseTriggerFrames()
                    };
                    
                    data.SkillActions.Add(actionData);
                }
                
                Debug.Log($"{LOG_PREFIX} 技能 [{skillTable.Id}] {skillTable.Name} 包含 {actions.Count} 个动作");
            }
            else
            {
                // 技能没有动作，使用SkillTable中的skillActionIds
                var actionIds = skillTable.GetSkillActionIds();
                if (actionIds.Count > 0)
                {
                    Debug.LogWarning($"{LOG_PREFIX} 技能 [{skillTable.Id}] {skillTable.Name} 在 SkillActionTable 中没有记录，" +
                                   $"但在 SkillTable 中配置了 {actionIds.Count} 个动作ID");
                    
                    // 创建占位符动作数据
                    foreach (var actionId in actionIds)
                    {
                        var actionData = new SkillActionData
                        {
                            ActionId = actionId,
                            SkillId = skillTable.Id,
                            AttackBoxInfo = "",
                            ActualCost = skillTable.DisplayCost,
                            ActualCooldown = (int)(skillTable.DisplayCooldown * 60), // 转换为帧数
                            TriggerFrames = new List<TriggerFrameInfo>()
                        };
                        
                        data.SkillActions.Add(actionData);
                    }
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX} 技能 [{skillTable.Id}] {skillTable.Name} 没有配置任何技能动作");
                }
            }
            
            // 初始化编辑器状态
            data.IsNew = false;
            data.IsDirty = false;
            data.ValidationErrors = new List<string>();
            
            return data;
        }
        
        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public static bool ValidateDataIntegrity(out List<string> errors)
        {
            errors = new List<string>();
            
            try
            {
                // 读取所有技能数据
                var skills = ReadSkillData();
                
                // 检查是否有重复的技能ID
                var duplicateIds = skills.GroupBy(s => s.SkillId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                
                if (duplicateIds.Count > 0)
                {
                    errors.Add($"发现重复的技能ID: {string.Join(", ", duplicateIds)}");
                }
                
                // 检查每个技能的数据有效性
                foreach (var skill in skills)
                {
                    if (!SkillDataValidator.Validate(skill, out var skillErrors))
                    {
                        errors.AddRange(skillErrors.Select(e => $"技能 [{skill.SkillId}] {skill.SkillName}: {e}"));
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} 数据完整性验证完成，发现 {errors.Count} 个问题");
            }
            catch (System.Exception ex)
            {
                errors.Add($"验证数据完整性时发生异常: {ex.Message}");
                Debug.LogError($"{LOG_PREFIX} {errors[errors.Count - 1]}");
            }
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// 获取所有技能ID列表
        /// </summary>
        public static List<int> GetAllSkillIds()
        {
            try
            {
                var skillTableConfig = SkillTableData.GetTableConfig();
                var skillTableList = LubanCSVReader.ReadTable<SkillTableData>(skillTableConfig);
                return skillTableList.Select(s => s.Id).ToList();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} 获取技能ID列表失败: {ex.Message}");
                return new List<int>();
            }
        }
        
        /// <summary>
        /// 检查技能ID是否存在
        /// </summary>
        public static bool SkillIdExists(int skillId)
        {
            var allIds = GetAllSkillIds();
            return allIds.Contains(skillId);
        }
    }
}

