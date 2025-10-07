using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Core;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 角色数据验证器
    /// </summary>
    public static class RoleDataValidator
    {
        /// <summary>
        /// 验证角色数据
        /// </summary>
        public static bool Validate(RoleEditorData data, out List<string> errors)
        {
            errors = new List<string>();
            
            // 验证ID
            if (data.EntityId <= 0)
                errors.Add("实体ID必须大于0");
            
            if (data.RoleId <= 0)
                errors.Add("角色ID必须大于0");
            
            if (data.EntityId != data.RoleId)
                errors.Add("实体ID和角色ID必须保持一致");
            
            // 验证原型名
            if (string.IsNullOrWhiteSpace(data.ArchetypeName))
                errors.Add("原型名称不能为空");
            
            // 验证名称
            if (string.IsNullOrWhiteSpace(data.RoleName))
                errors.Add("角色名称不能为空");
            else if (data.RoleName.Length > EditorConfig.MAX_NAME_LENGTH)
                errors.Add($"角色名称长度不能超过{EditorConfig.MAX_NAME_LENGTH}个字符");
            
            // 验证描述
            if (!string.IsNullOrEmpty(data.RoleDescription) && 
                data.RoleDescription.Length > EditorConfig.MAX_DESCRIPTION_LENGTH)
                errors.Add($"角色描述长度不能超过{EditorConfig.MAX_DESCRIPTION_LENGTH}个字符");
            
            // 验证模型路径
            if (!string.IsNullOrEmpty(data.ModelPath))
            {
                if (!data.ModelPath.StartsWith("Assets/"))
                    errors.Add("模型路径必须以 Assets/ 开头");
            }
            
            // 验证基础属性
            ValidateAttribute("基础攻击力", data.BaseAttack, errors);
            ValidateAttribute("基础防御力", data.BaseDefense, errors);
            ValidateAttribute("基础生命值", data.BaseHealth, errors);
            ValidateAttribute("基础速度", data.BaseSpeed, errors);
            
            // 验证成长属性
            ValidateAttribute("攻击力成长", data.AttackGrowth, errors);
            ValidateAttribute("防御力成长", data.DefenseGrowth, errors);
            ValidateAttribute("生命值成长", data.HealthGrowth, errors);
            ValidateAttribute("速度成长", data.SpeedGrowth, errors);
            
            // 更新数据的验证错误列表
            data.ValidationErrors = new List<string>(errors);
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// 验证属性值
        /// </summary>
        private static void ValidateAttribute(string attributeName, float value, List<string> errors)
        {
            if (value < EditorConfig.MIN_ATTRIBUTE_VALUE)
                errors.Add($"{attributeName}不能小于{EditorConfig.MIN_ATTRIBUTE_VALUE}");
            else if (value > EditorConfig.MAX_ATTRIBUTE_VALUE)
                errors.Add($"{attributeName}不能大于{EditorConfig.MAX_ATTRIBUTE_VALUE}");
        }
    }
}

