using System.Collections.Generic;
using MemoryPack;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 动画根节点位移数据（运行时，使用定点数）
    /// </summary>
    [MemoryPackable]
    public partial class AnimationRootMotionData
    {
        /// <summary>
        /// 每帧的位移数据
        /// </summary>
        public List<RootMotionFrameData> Frames { get; set; } = new List<RootMotionFrameData>();
        
        /// <summary>
        /// 动画总帧数（逻辑帧）
        /// </summary>
        public int TotalFrames { get; set; }
        
        /// <summary>
        /// 是否有有效的位移数据
        /// </summary>
        public bool HasMotion => Frames != null && Frames.Count > 0;
    }

    /// <summary>
    /// 单帧位移数据（运行时，使用定点数）
    /// </summary>
    [MemoryPackable]
    public partial class RootMotionFrameData
    {
        /// <summary>
        /// 帧索引（从0开始）
        /// </summary>
        public int FrameIndex { get; set; }
        
        /// <summary>
        /// 相对位置（TrueSync 定点数）
        /// </summary>
        public TSVector RelativePosition { get; set; }
        
        /// <summary>
        /// 相对旋转（TrueSync 定点数）
        /// </summary>
        public TSQuaternion RelativeRotation { get; set; }
        
        /// <summary>
        /// 相对于上一帧的增量位置（TrueSync 定点数）
        /// </summary>
        public TSVector DeltaPosition { get; set; }
        
        /// <summary>
        /// 相对于上一帧的增量旋转（TrueSync 定点数）
        /// </summary>
        public TSQuaternion DeltaRotation { get; set; }
    }
}

