using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Physics
{
    /// <summary>
    /// 单个碰撞形状数据
    /// </summary>
    [MemoryPackable]
    public partial struct CollisionShape
    {
        /// <summary>形状类型</summary>
        public HitBoxShape ShapeType { get; set; }
        
        /// <summary>本地偏移（相对实体中心）</summary>
        [MemoryPackAllowSerialize]
        public TSVector LocalOffset { get; set; }
        
        /// <summary>本地旋转</summary>
        [MemoryPackAllowSerialize]
        public TSQuaternion LocalRotation { get; set; }
        
        /// <summary>Box 半尺寸（宽、高、深的一半）</summary>
        [MemoryPackAllowSerialize]
        public TSVector HalfSize { get; set; }
        
        /// <summary>Sphere/Capsule 半径</summary>
        public float Radius { get; set; }
        
        /// <summary>Capsule 高度（端到端）</summary>
        public float Height { get; set; }
    }
}

