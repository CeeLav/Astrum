using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline.EventData
{
    /// <summary>
    /// 音效事件数据
    /// </summary>
    [Serializable]
    public class SFXEventData
    {
        /// <summary>音效资源路径</summary>
        public string ResourcePath = "";
        
        /// <summary>音量 (0-1)</summary>
        public float Volume = 0.8f;
        
        /// <summary>音调 (0.5-2)</summary>
        public float Pitch = 1.0f;
        
        /// <summary>空间混音 (0=2D, 1=3D)</summary>
        public float SpatialBlend = 1.0f;
        
        /// <summary>是否循环播放</summary>
        public bool Loop = false;
        
        /// <summary>是否跟随角色</summary>
        public bool FollowCharacter = true;
        
        /// <summary>最大听距</summary>
        public float MaxDistance = 50f;
        
        /// <summary>备注</summary>
        public string Note = "";
        
        // === 工厂方法 ===
        
        /// <summary>
        /// 创建默认数据
        /// </summary>
        public static SFXEventData CreateDefault()
        {
            return new SFXEventData
            {
                ResourcePath = "",
                Volume = 0.8f,
                Pitch = 1.0f,
                SpatialBlend = 1.0f,
                Loop = false,
                FollowCharacter = true,
                MaxDistance = 50f,
                Note = ""
            };
        }
        
        /// <summary>
        /// 克隆数据
        /// </summary>
        public SFXEventData Clone()
        {
            return new SFXEventData
            {
                ResourcePath = this.ResourcePath,
                Volume = this.Volume,
                Pitch = this.Pitch,
                SpatialBlend = this.SpatialBlend,
                Loop = this.Loop,
                FollowCharacter = this.FollowCharacter,
                MaxDistance = this.MaxDistance,
                Note = this.Note
            };
        }
        
        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(ResourcePath))
                return "[未选择音效]";
            
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
                errors.Add("音效资源路径不能为空");
            }
            
            if (Volume < 0 || Volume > 1)
            {
                errors.Add("音量必须在0-1之间");
            }
            
            if (Pitch < 0.5f || Pitch > 2f)
            {
                errors.Add("音调必须在0.5-2之间");
            }
            
            if (SpatialBlend < 0 || SpatialBlend > 1)
            {
                errors.Add("空间混音必须在0-1之间");
            }
            
            if (MaxDistance <= 0)
            {
                errors.Add("最大距离必须大于0");
            }
            
            return errors.Count == 0;
        }
    }
}
