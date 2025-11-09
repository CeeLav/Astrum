using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 技能动作扩展数据
    /// 对应 SkillActionTable 的专属字段
    /// </summary>
    [Serializable]
    public class SkillActionExtension
    {
        [TitleGroup("技能成本")]
        [LabelText("实际法力消耗"), Range(0, 1000)]
        [InfoBox("释放该技能动作实际消耗的法力值", InfoMessageType.Info)]
        public int ActualCost = 0;
        
        [TitleGroup("技能成本")]
        [LabelText("实际冷却(帧)"), Range(0, 3600)]
        [InfoBox("实际冷却帧数 (60帧 = 1秒)", InfoMessageType.Info)]
        public int ActualCooldown = 60;
        
        [TitleGroup("技能成本")]
        [LabelText("实际冷却(秒)"), ReadOnly]
        [ShowInInspector]
        public float ActualCooldownSeconds => ActualCooldown / 60f;
        
        /// <summary>
        /// 触发帧信息（JSON格式）
        /// </summary>
        [HideInInspector]
        public string TriggerFrames = "";
        
        /// <summary>
        /// 触发效果列表（从 TriggerFrames 解析）
        /// </summary>
        [HideInInspector]
        public List<TriggerFrameData> TriggerEffects = new List<TriggerFrameData>();
        
        /// <summary>
        /// 根节点位移数据（整型数组）
        /// </summary>
        [HideInInspector]
        public List<int> RootMotionData = new List<int>();
        
        /// <summary>
        /// 创建默认技能扩展数据
        /// </summary>
        public static SkillActionExtension CreateDefault()
        {
            return new SkillActionExtension
            {
                ActualCost = 0,
                ActualCooldown = 60,
                TriggerFrames = "",
                TriggerEffects = new List<TriggerFrameData>(),
                RootMotionData = new List<int>()
            };
        }
        
        /// <summary>
        /// 克隆技能扩展数据
        /// </summary>
        public SkillActionExtension Clone()
        {
            return new SkillActionExtension
            {
                ActualCost = this.ActualCost,
                ActualCooldown = this.ActualCooldown,
                TriggerFrames = this.TriggerFrames,
                TriggerEffects = new List<TriggerFrameData>(this.TriggerEffects),
                RootMotionData = new List<int>(this.RootMotionData)
            };
        }
    }
}

