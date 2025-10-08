using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using System.Collections.Generic;
using System.Linq;

namespace Astrum.Editor.RoleEditor.Test
{
    /// <summary>
    /// 技能数据测试工具
    /// </summary>
    public static class SkillDataTest
    {
        private const string MENU_ROOT = "Tools/Role & Skill Editor/Skill Data Tests/";
        
        // ===== 读取测试 =====
        
        [MenuItem(MENU_ROOT + "1. 测试读取技能数据")]
        public static void TestReadSkillData()
        {
            Debug.Log("========== 开始测试读取技能数据 ==========");
            
            try
            {
                var skills = SkillDataReader.ReadSkillData();
                
                Debug.Log($"✓ 成功读取 {skills.Count} 个技能");
                
                foreach (var skill in skills)
                {
                    Debug.Log($"  - [{skill.SkillId}] {skill.SkillName}");
                    Debug.Log($"    类型: {skill.SkillType}, 等级: {skill.RequiredLevel}-{skill.MaxLevel}");
                    Debug.Log($"    动作数量: {skill.SkillActions.Count}");
                    
                    foreach (var action in skill.SkillActions)
                    {
                        Debug.Log($"      └─ 动作 {action.ActionId}: {action.TriggerFrames.Count} 个触发帧");
                    }
                }
                
                EditorUtility.DisplayDialog("测试成功", 
                    $"成功读取 {skills.Count} 个技能数据", "确定");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ 测试失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("测试失败", 
                    $"读取技能数据失败：{ex.Message}", "确定");
            }
            
            Debug.Log("========== 测试完成 ==========");
        }
        
        [MenuItem(MENU_ROOT + "2. 测试读取单个技能")]
        public static void TestReadSingleSkill()
        {
            Debug.Log("========== 开始测试读取单个技能 ==========");
            
            try
            {
                int testSkillId = 2001; // 测试技能ID
                var skill = SkillDataReader.ReadSkillById(testSkillId);
                
                if (skill != null)
                {
                    Debug.Log($"✓ 成功读取技能 [{skill.SkillId}] {skill.SkillName}");
                    Debug.Log($"  描述: {skill.SkillDescription}");
                    Debug.Log($"  类型: {skill.SkillType}");
                    Debug.Log($"  冷却: {skill.DisplayCooldown}秒, 消耗: {skill.DisplayCost}");
                    Debug.Log($"  动作列表:");
                    
                    foreach (var action in skill.SkillActions)
                    {
                        Debug.Log($"    - ActionId: {action.ActionId}");
                        Debug.Log($"      攻击盒: {action.AttackBoxInfo}");
                        Debug.Log($"      实际消耗: {action.ActualCost}, 实际冷却: {action.ActualCooldown}帧");
                        Debug.Log($"      触发帧: {string.Join(", ", action.TriggerFrames.Select(t => t.ToString()))}");
                    }
                    
                    EditorUtility.DisplayDialog("测试成功", 
                        $"成功读取技能 [{skill.SkillId}] {skill.SkillName}", "确定");
                }
                else
                {
                    Debug.LogWarning($"✗ 未找到技能ID {testSkillId}");
                    EditorUtility.DisplayDialog("测试警告", 
                        $"未找到技能ID {testSkillId}", "确定");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ 测试失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("测试失败", 
                    $"读取单个技能失败：{ex.Message}", "确定");
            }
            
            Debug.Log("========== 测试完成 ==========");
        }
        
        // ===== 验证测试 =====
        
        [MenuItem(MENU_ROOT + "3. 测试数据验证")]
        public static void TestDataValidation()
        {
            Debug.Log("========== 开始测试数据验证 ==========");
            
            try
            {
                var skills = SkillDataReader.ReadSkillData();
                
                int validCount = 0;
                int invalidCount = 0;
                
                foreach (var skill in skills)
                {
                    if (SkillDataValidator.Validate(skill, out var errors))
                    {
                        validCount++;
                        Debug.Log($"✓ 技能 [{skill.SkillId}] {skill.SkillName} 验证通过");
                    }
                    else
                    {
                        invalidCount++;
                        Debug.LogWarning($"✗ 技能 [{skill.SkillId}] {skill.SkillName} 验证失败:");
                        foreach (var error in errors)
                        {
                            Debug.LogWarning($"    - {error}");
                        }
                    }
                }
                
                Debug.Log($"验证完成: {validCount} 个通过, {invalidCount} 个失败");
                
                EditorUtility.DisplayDialog("验证完成", 
                    $"验证结果:\n通过: {validCount} 个\n失败: {invalidCount} 个", "确定");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ 测试失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("测试失败", 
                    $"数据验证失败：{ex.Message}", "确定");
            }
            
            Debug.Log("========== 测试完成 ==========");
        }
        
        [MenuItem(MENU_ROOT + "4. 测试数据完整性")]
        public static void TestDataIntegrity()
        {
            Debug.Log("========== 开始测试数据完整性 ==========");
            
            try
            {
                if (SkillDataReader.ValidateDataIntegrity(out var errors))
                {
                    Debug.Log("✓ 数据完整性验证通过，没有发现问题");
                    EditorUtility.DisplayDialog("验证成功", 
                        "数据完整性验证通过", "确定");
                }
                else
                {
                    Debug.LogWarning($"✗ 数据完整性验证失败，发现 {errors.Count} 个问题:");
                    foreach (var error in errors)
                    {
                        Debug.LogWarning($"  - {error}");
                    }
                    
                    EditorUtility.DisplayDialog("验证失败", 
                        $"发现 {errors.Count} 个问题，请查看Console", "确定");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ 测试失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("测试失败", 
                    $"数据完整性验证失败：{ex.Message}", "确定");
            }
            
            Debug.Log("========== 测试完成 ==========");
        }
        
        // ===== ActionTable 映射测试 =====
        
        [MenuItem(MENU_ROOT + "5. 测试 ActionTable 对应关系")]
        public static void TestActionTableMapping()
        {
            Debug.Log("========== 开始测试 ActionTable 对应关系 ==========");
            
            try
            {
                var skills = SkillDataReader.ReadSkillData();
                
                int totalActions = 0;
                int validActions = 0;
                int invalidActions = 0;
                var invalidActionIds = new List<int>();
                
                foreach (var skill in skills)
                {
                    foreach (var action in skill.SkillActions)
                    {
                        totalActions++;
                        var actionTable = Services.ConfigTableHelper.GetActionTable(action.ActionId);
                        
                        if (actionTable != null)
                        {
                            validActions++;
                            Debug.Log($"✓ 技能 [{skill.SkillId}] 的动作 {action.ActionId} 在 ActionTable 中存在");
                            Debug.Log($"    ActionName: {actionTable.ActionName}, ActionType: {actionTable.ActionType}");
                        }
                        else
                        {
                            invalidActions++;
                            invalidActionIds.Add(action.ActionId);
                            Debug.LogError($"✗ 技能 [{skill.SkillId}] 的动作 {action.ActionId} 在 ActionTable 中不存在！");
                        }
                    }
                }
                
                Debug.Log($"对应关系测试完成:");
                Debug.Log($"  总动作数: {totalActions}");
                Debug.Log($"  有效: {validActions}");
                Debug.Log($"  无效: {invalidActions}");
                
                if (invalidActions > 0)
                {
                    EditorUtility.DisplayDialog("测试失败", 
                        $"发现 {invalidActions} 个无效的ActionId，不满足一一对应关系\n" +
                        $"无效ID: {string.Join(", ", invalidActionIds)}", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("测试成功", 
                        $"所有技能动作都与 ActionTable 正确对应\n总计: {totalActions} 个动作", "确定");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ 测试失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("测试失败", 
                    $"ActionTable 对应关系测试失败：{ex.Message}", "确定");
            }
            
            Debug.Log("========== 测试完成 ==========");
        }
        
        // ===== 写入测试（慎用！）=====
        
        [MenuItem(MENU_ROOT + "写入测试/创建测试技能")]
        public static void TestCreateSkill()
        {
            Debug.Log("========== 开始测试创建技能 ==========");
            
            try
            {
                // 创建测试技能
                var testSkill = SkillEditorData.CreateDefault(9999);
                testSkill.SkillName = "测试技能";
                testSkill.SkillDescription = "这是一个测试技能";
                testSkill.SkillType = SkillTypeEnum.攻击;
                
                // 添加一个测试动作
                testSkill.AddSkillAction(3001); // 使用现有的ActionId
                
                // 验证
                if (!SkillDataValidator.Validate(testSkill, out var errors))
                {
                    Debug.LogError("✗ 测试技能验证失败:");
                    foreach (var error in errors)
                    {
                        Debug.LogError($"  - {error}");
                    }
                    return;
                }
                
                Debug.Log("✓ 测试技能创建成功:");
                Debug.Log($"  ID: {testSkill.SkillId}");
                Debug.Log($"  名称: {testSkill.SkillName}");
                Debug.Log($"  动作数: {testSkill.SkillActions.Count}");
                
                EditorUtility.DisplayDialog("创建成功", 
                    "测试技能创建成功（未写入文件）\n" +
                    "请查看Console查看详细信息", "确定");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ 测试失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("测试失败", 
                    $"创建测试技能失败：{ex.Message}", "确定");
            }
            
            Debug.Log("========== 测试完成 ==========");
        }
        
        // ===== 实用工具 =====
        
        [MenuItem(MENU_ROOT + "工具/列出所有技能ID")]
        public static void ListAllSkillIds()
        {
            Debug.Log("========== 所有技能ID列表 ==========");
            
            try
            {
                var skillIds = SkillDataReader.GetAllSkillIds();
                Debug.Log($"共 {skillIds.Count} 个技能:");
                foreach (var id in skillIds)
                {
                    Debug.Log($"  - {id}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ 获取失败: {ex.Message}");
            }
            
            Debug.Log("====================================");
        }
        
        [MenuItem(MENU_ROOT + "工具/清除 ConfigTableHelper 缓存")]
        public static void ClearConfigCache()
        {
            Services.ConfigTableHelper.ClearCache();
            Debug.Log("✓ ConfigTableHelper 缓存已清除");
            EditorUtility.DisplayDialog("清除成功", 
                "ConfigTableHelper 缓存已清除", "确定");
        }
    }
}

