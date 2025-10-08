using Astrum.Editor.RoleEditor.Persistence.Core;
using System.Collections.Generic;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// SkillEffectTable数据映射
    /// </summary>
    public class SkillEffectTableData
    {
        [TableField(0, "skillEffectId")]
        public int SkillEffectId { get; set; }
        
        [TableField(1, "effectType")]
        public int EffectType { get; set; }
        
        [TableField(2, "effectValue")]
        public float EffectValue { get; set; }
        
        [TableField(3, "targetType")]
        public int TargetType { get; set; }
        
        [TableField(4, "effectDuration")]
        public float EffectDuration { get; set; }
        
        [TableField(5, "effectRange")]
        public float EffectRange { get; set; }
        
        [TableField(6, "castTime")]
        public float CastTime { get; set; }
        
        [TableField(7, "effectParams")]
        public string EffectParams { get; set; }
        
        [TableField(8, "visualEffectId")]
        public int VisualEffectId { get; set; }
        
        [TableField(9, "soundEffectId")]
        public int SoundEffectId { get; set; }
        
        /// <summary>
        /// 解析效果参数
        /// 格式: "DamageType:Physical,Knockback:5.0"
        /// </summary>
        public Dictionary<string, string> ParseEffectParams()
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(EffectParams))
                return result;
            
            var pairs = EffectParams.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    result[parts[0].Trim()] = parts[1].Trim();
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 设置效果参数
        /// </summary>
        public void SetEffectParams(Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                EffectParams = string.Empty;
                return;
            }
            
            var parts = new List<string>();
            foreach (var kvp in parameters)
            {
                parts.Add($"{kvp.Key}:{kvp.Value}");
            }
            
            EffectParams = string.Join(",", parts);
        }
        
        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Skill/#SkillEffectTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string>
                    {
                        "skillEffectId", "effectType", "effectValue", "targetType",
                        "effectDuration", "effectRange", "castTime", "effectParams",
                        "visualEffectId", "soundEffectId"
                    },
                    Types = new List<string>
                    {
                        "int", "int", "float", "int",
                        "float", "float", "float", "string",
                        "int", "int"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", "", "", "", "", "", "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "技能效果ID", "效果类型", "效果数值", "目标类型",
                        "效果持续时间", "效果范围", "施法时间", "效果参数",
                        "视觉效果ID", "音效ID"
                    }
                }
            };
        }
    }
}

