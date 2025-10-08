using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline.EventData
{
    /// <summary>
    /// 特效事件数据
    /// </summary>
    [Serializable]
    public class VFXEventData
    {
        /// <summary>特效资源路径</summary>
        public string ResourcePath = "";
        
        /// <summary>位置偏移</summary>
        public Vector3 PositionOffset = Vector3.zero;
        
        /// <summary>旋转</summary>
        public Vector3 Rotation = Vector3.zero;
        
        /// <summary>缩放</summary>
        public float Scale = 1.0f;
        
        /// <summary>播放速度</summary>
        public float PlaybackSpeed = 1.0f;
        
        /// <summary>是否跟随角色</summary>
        public bool FollowCharacter = true;
        
        /// <summary>是否循环播放</summary>
        public bool Loop = false;
        
        /// <summary>备注</summary>
        public string Note = "";
        
        // === 工厂方法 ===
        
        /// <summary>
        /// 创建默认数据
        /// </summary>
        public static VFXEventData CreateDefault()
        {
            return new VFXEventData
            {
                ResourcePath = "",
                PositionOffset = Vector3.zero,
                Rotation = Vector3.zero,
                Scale = 1.0f,
                PlaybackSpeed = 1.0f,
                FollowCharacter = true,
                Loop = false,
                Note = ""
            };
        }
        
        /// <summary>
        /// 克隆数据
        /// </summary>
        public VFXEventData Clone()
        {
            return new VFXEventData
            {
                ResourcePath = this.ResourcePath,
                PositionOffset = this.PositionOffset,
                Rotation = this.Rotation,
                Scale = this.Scale,
                PlaybackSpeed = this.PlaybackSpeed,
                FollowCharacter = this.FollowCharacter,
                Loop = this.Loop,
                Note = this.Note
            };
        }
        
        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(ResourcePath))
                return "[未选择特效]";
            
            string fileName = System.IO.Path.GetFileNameWithoutExtension(ResourcePath);
            return fileName;
        }
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            
            if (string.IsNullOrEmpty(ResourcePath))
            {
                errors.Add("特效资源路径不能为空");
            }
            
            if (Scale <= 0)
            {
                errors.Add("缩放必须大于0");
            }
            
            if (PlaybackSpeed <= 0)
            {
                errors.Add("播放速度必须大于0");
            }
            
            return errors.Count == 0;
        }
    }
}
