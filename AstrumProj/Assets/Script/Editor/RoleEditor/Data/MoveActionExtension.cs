using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 移动动作扩展数据
    /// 对应 MoveActionTable 的专属字段
    /// </summary>
    [Serializable]
    public class MoveActionExtension
    {
        [TitleGroup("移动配置")]
        [LabelText("基础移动速度"), ReadOnly]
        [InfoBox("基于 Root Motion 自动计算，单位：m/s × 1000", InfoMessageType.Info)]
        public int MoveSpeed = 0;
        
        [TitleGroup("移动配置")]
        [LabelText("移动速度(m/s)"), ReadOnly]
        [ShowInInspector]
        public float MoveSpeedMeters => MoveSpeed / 1000f;
        
        /// <summary>
        /// 根节点位移数据（整型数组）
        /// </summary>
        [HideInInspector]
        public List<int> RootMotionData = new List<int>();
        
        /// <summary>
        /// 创建默认移动扩展数据
        /// </summary>
        public static MoveActionExtension CreateDefault()
        {
            return new MoveActionExtension
            {
                MoveSpeed = 0,
                RootMotionData = new List<int>()
            };
        }
        
        /// <summary>
        /// 克隆移动扩展数据
        /// </summary>
        public MoveActionExtension Clone()
        {
            return new MoveActionExtension
            {
                MoveSpeed = this.MoveSpeed,
                RootMotionData = new List<int>(this.RootMotionData)
            };
        }
    }
}

