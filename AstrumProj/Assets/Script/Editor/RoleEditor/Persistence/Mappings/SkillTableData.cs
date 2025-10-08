using Astrum.Editor.RoleEditor.Persistence.Core;
using System.Collections.Generic;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// SkillTable数据映射
    /// </summary>
    public class SkillTableData
    {
        [TableField(0, "id")]
        public int Id { get; set; }
        
        [TableField(1, "name")]
        public string Name { get; set; }
        
        [TableField(2, "description")]
        public string Description { get; set; }
        
        [TableField(3, "skillType")]
        public int SkillType { get; set; }
        
        [TableField(4, "skillActionIds")]
        public string SkillActionIdsRaw { get; set; }
        
        [TableField(5, "displayCooldown")]
        public float DisplayCooldown { get; set; }
        
        [TableField(6, "displayCost")]
        public int DisplayCost { get; set; }
        
        [TableField(7, "requiredLevel")]
        public int RequiredLevel { get; set; }
        
        [TableField(8, "maxLevel")]
        public int MaxLevel { get; set; }
        
        [TableField(9, "iconId")]
        public int IconId { get; set; }
        
        /// <summary>
        /// 解析技能动作ID数组
        /// </summary>
        public List<int> GetSkillActionIds()
        {
            var result = new List<int>();
            
            if (string.IsNullOrEmpty(SkillActionIdsRaw))
                return result;
            
            // 格式: "3001,3002" 或 "3001"
            var parts = SkillActionIdsRaw.Split(',');
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int actionId))
                {
                    result.Add(actionId);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 设置技能动作ID数组
        /// </summary>
        public void SetSkillActionIds(List<int> actionIds)
        {
            if (actionIds == null || actionIds.Count == 0)
            {
                SkillActionIdsRaw = string.Empty;
                return;
            }
            
            SkillActionIdsRaw = string.Join(",", actionIds);
        }
        
        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Skill/#SkillTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string>
                    {
                        "id", "name", "description", "skillType", "skillActionIds",
                        "displayCooldown", "displayCost", "requiredLevel", "maxLevel", "iconId"
                    },
                    Types = new List<string>
                    {
                        "int", "string", "string", "int", "(array#sep=,),int",
                        "float", "int", "int", "int", "int"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", "", "", "", "", "", "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "技能ID", "技能名称", "技能描述", "技能类型", "技能动作ID列表",
                        "展示冷却时间", "展示法力消耗", "学习所需等级", "技能最大等级", "图标ID"
                    }
                }
            };
        }
    }
}

