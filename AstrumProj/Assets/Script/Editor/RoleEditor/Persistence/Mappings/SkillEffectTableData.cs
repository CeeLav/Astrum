using Astrum.Editor.RoleEditor.Persistence.Core;
using System.Collections.Generic;
using System.Linq;

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
        public string EffectType { get; set; }
        
        [TableField(2, "intParams")]
        public List<int> IntParams { get; set; } = new List<int>();
        
        [TableField(3, "stringParams")]
        public List<string> StringParams { get; set; } = new List<string>();
        
        /// <summary>
        /// 解析效果参数
        /// 格式: "DamageType:Physical,Knockback:5.0"
        /// </summary>
        public Dictionary<string, string> ParseEffectParams()
        {
            var result = new Dictionary<string, string>();
            
            if (StringParams == null || StringParams.Count == 0)
                return result;
            
            foreach (var entry in StringParams)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                var parts = entry.Split(':');
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
                StringParams = new List<string>();
                return;
            }
            
            StringParams = parameters
                .Select(kvp => $"{kvp.Key}:{kvp.Value}")
                .ToList();
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
                        "skillEffectId", "effectType", "intParams", "stringParams"
                    },
                    Types = new List<string>
                    {
                        "int", "string", "(array#sep=|,int)", "(array#sep=|,string)"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "技能效果ID", "效果类型键", "整数参数数组", "字符串参数数组"
                    }
                }
            };
        }
    }
}

