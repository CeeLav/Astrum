using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Physics
{
    /// <summary>
    /// 查询模式
    /// </summary>
    public enum HitQueryMode
    {
        /// <summary>重叠查询（Overlap）</summary>
        Overlap = 0,
        /// <summary>扫掠查询（Sweep）</summary>
        Sweep = 1,
        /// <summary>射线投射（Raycast）</summary>
        Raycast = 2
    }

    /// <summary>
    /// 碰撞形状数据（统一用于配置和查询）
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
        public FP Radius { get; set; }
        
        /// <summary>Capsule 高度（端到端）</summary>
        public FP Height { get; set; }
        
        /// <summary>查询模式（默认为 Overlap）</summary>
        public HitQueryMode QueryMode { get; set; }

        /// <summary>
        /// 创建 Box 类型碰撞形状
        /// </summary>
        public static CollisionShape CreateBox(TSVector halfSize, TSVector localOffset = default, TSQuaternion localRotation = default)
        {
            // 检查 localRotation 是否未初始化（接近零四元数），使用 identity
            bool isDefaultRotation = localRotation.x == FP.Zero && localRotation.y == FP.Zero && 
                                     localRotation.z == FP.Zero && localRotation.w == FP.Zero;
            
            return new CollisionShape
            {
                ShapeType = HitBoxShape.Box,
                LocalOffset = localOffset,
                LocalRotation = isDefaultRotation ? TSQuaternion.identity : localRotation,
                HalfSize = halfSize,
                Radius = FP.Zero,
                Height = FP.Zero,
                QueryMode = HitQueryMode.Overlap
            };
        }

        /// <summary>
        /// 创建 Sphere 类型碰撞形状
        /// </summary>
        public static CollisionShape CreateSphere(FP radius, TSVector localOffset = default)
        {
            return new CollisionShape
            {
                ShapeType = HitBoxShape.Sphere,
                LocalOffset = localOffset,
                LocalRotation = TSQuaternion.identity,
                HalfSize = TSVector.zero,
                Radius = radius,
                Height = FP.Zero,
                QueryMode = HitQueryMode.Overlap
            };
        }

        /// <summary>
        /// 创建 Capsule 类型碰撞形状
        /// </summary>
        public static CollisionShape CreateCapsule(FP radius, FP height, TSVector localOffset = default, TSQuaternion localRotation = default)
        {
            // 检查 localRotation 是否未初始化（接近零四元数），使用 identity
            bool isDefaultRotation = localRotation.x == FP.Zero && localRotation.y == FP.Zero && 
                                     localRotation.z == FP.Zero && localRotation.w == FP.Zero;
            
            return new CollisionShape
            {
                ShapeType = HitBoxShape.Capsule,
                LocalOffset = localOffset,
                LocalRotation = isDefaultRotation ? TSQuaternion.identity : localRotation,
                HalfSize = TSVector.zero,
                Radius = radius,
                Height = height,
                QueryMode = HitQueryMode.Overlap
            };
        }
    }
}

