using Astrum.Editor.RoleEditor.Persistence.Core;
using System.Collections.Generic;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// SkillActionTable数据映射
    /// </summary>
    public class SkillActionTableData
    {
        [TableField(0, "actionId")]
        public int ActionId { get; set; }
        
        [TableField(1, "actualCost")]
        public int ActualCost { get; set; }
        
        [TableField(2, "actualCooldown")]
        public int ActualCooldown { get; set; }
        
        [TableField(3, "triggerFrames")]
        public string TriggerFrames { get; set; }
        
        [TableField(4, "rootMotionData")]
        public List<int> RootMotionData { get; set; } = new List<int>();
        
        /// <summary>
        /// 解析触发帧信息
        /// 格式: "Frame5:Collision:4001,Frame8:Direct:4002"
        /// </summary>
        public List<TriggerFrameInfo> ParseTriggerFrames()
        {
            var result = new List<TriggerFrameInfo>();
            
            if (string.IsNullOrEmpty(TriggerFrames))
                return result;
            
            var parts = TriggerFrames.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                
                var segments = trimmed.Split(':');
                if (segments.Length >= 3)
                {
                    // 解析帧号（去掉"Frame"前缀）
                    var frameStr = segments[0].Replace("Frame", "").Trim();
                    if (int.TryParse(frameStr, out int frame) &&
                        int.TryParse(segments[2].Trim(), out int effectId))
                    {
                        result.Add(new TriggerFrameInfo
                        {
                            Frame = frame,
                            TriggerType = segments[1].Trim(),
                            EffectId = effectId
                        });
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 设置触发帧信息
        /// </summary>
        public void SetTriggerFrames(List<TriggerFrameInfo> triggerFrames)
        {
            if (triggerFrames == null || triggerFrames.Count == 0)
            {
                TriggerFrames = string.Empty;
                return;
            }
            
            var parts = new List<string>();
            foreach (var info in triggerFrames)
            {
                parts.Add($"Frame{info.Frame}:{info.TriggerType}:{info.EffectId}");
            }
            
            TriggerFrames = string.Join(",", parts);
        }
        
        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Skill/#SkillActionTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string>
                    {
                        "actionId", "actualCost",
                        "actualCooldown", "triggerFrames", "rootMotionData"
                    },
                    Types = new List<string>
                    {
                        "int", "int", "int", "string", "(array#sep=,),int"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "动作ID", "实际法力消耗",
                        "实际冷却时间(帧)", "触发帧信息(含碰撞盒)", "根节点位移数据"
                    }
                }
            };
        }
    }
    
    /// <summary>
    /// 触发帧信息（可序列化）
    /// </summary>
    [System.Serializable]
    public class TriggerFrameInfo
    {
        /// <summary>帧号</summary>
        public int Frame { get; set; }
        
        /// <summary>触发类型（Collision/Direct/Condition）</summary>
        public string TriggerType { get; set; } = "Direct";
        
        /// <summary>效果ID</summary>
        public int EffectId { get; set; }
        
        /// <summary>用于Odin显示</summary>
        public string DisplayName => ToString();
        
        public override string ToString()
        {
            return $"Frame{Frame}:{TriggerType}:{EffectId}";
        }
    }
}

