using UnityEngine;
using Astrum.CommonBase;

namespace Astrum.CommonBase.Events
{
    /// <summary>
    /// VFX 触发事件数据
    /// Logic 层向 View 层传递特效触发信息
    /// </summary>
    public class VFXTriggerEventData
    {
        /// <summary>实体ID（特效绑定的实体）</summary>
        public long EntityId { get; set; }
        
        /// <summary>特效资源路径</summary>
        public string ResourcePath { get; set; } = string.Empty;
        
        /// <summary>位置偏移（相对于实体位置）</summary>
        public Vector3 PositionOffset { get; set; }
        
        /// <summary>旋转（欧拉角）</summary>
        public Vector3 Rotation { get; set; }
        
        /// <summary>缩放</summary>
        public float Scale { get; set; } = 1.0f;
        
        /// <summary>播放速度</summary>
        public float PlaybackSpeed { get; set; } = 1.0f;
        
        /// <summary>是否跟随角色</summary>
        public bool FollowCharacter { get; set; } = true;
        
        /// <summary>是否循环播放</summary>
        public bool Loop { get; set; } = false;
        
        /// <summary>特效实例ID（用于后续停止/清理）</summary>
        public int InstanceId { get; set; }
    }
}

