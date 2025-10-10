using TrueSync;

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
    /// 技能命中盒数据
    /// 用于运行时技能碰撞检测查询
    /// </summary>
    public struct HitBoxData
    {
        /// <summary>形状类型（Box/Sphere/Capsule）</summary>
        public HitBoxShape ShapeType { get; set; }

        /// <summary>本地偏移（相对施法者位置）</summary>
        public TSVector LocalOffset { get; set; }

        /// <summary>本地旋转（相对施法者朝向）</summary>
        public TSQuaternion LocalRotation { get; set; }

        /// <summary>Box 半尺寸（宽、高、深的一半）</summary>
        public TSVector HalfSize { get; set; }

        /// <summary>Sphere/Capsule 半径</summary>
        public FP Radius { get; set; }

        /// <summary>Capsule 高度（端到端）</summary>
        public FP Height { get; set; }

        /// <summary>查询模式</summary>
        public HitQueryMode QueryMode { get; set; }

        /// <summary>
        /// 从 CollisionShape 创建 HitBoxData
        /// </summary>
        public static HitBoxData FromCollisionShape(CollisionShape shape)
        {
            return new HitBoxData
            {
                ShapeType = shape.ShapeType,
                LocalOffset = shape.LocalOffset,
                LocalRotation = shape.LocalRotation,
                HalfSize = shape.HalfSize,
                Radius = shape.Radius,
                Height = shape.Height,
                QueryMode = HitQueryMode.Overlap // 默认重叠查询
            };
        }

        /// <summary>
        /// 创建 Box 类型 HitBox
        /// </summary>
        public static HitBoxData CreateBox(TSVector halfSize, TSVector localOffset = default, TSQuaternion localRotation = default)
        {
            // 检查 localRotation 是否未初始化（接近零四元数），使用 identity
            bool isDefaultRotation = localRotation.x == FP.Zero && localRotation.y == FP.Zero && 
                                     localRotation.z == FP.Zero && localRotation.w == FP.Zero;
            
            return new HitBoxData
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
        /// 创建 Sphere 类型 HitBox
        /// </summary>
        public static HitBoxData CreateSphere(FP radius, TSVector localOffset = default)
        {
            return new HitBoxData
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
        /// 创建 Capsule 类型 HitBox
        /// </summary>
        public static HitBoxData CreateCapsule(FP radius, FP height, TSVector localOffset = default, TSQuaternion localRotation = default)
        {
            // 检查 localRotation 是否未初始化（接近零四元数），使用 identity
            bool isDefaultRotation = localRotation.x == FP.Zero && localRotation.y == FP.Zero && 
                                     localRotation.z == FP.Zero && localRotation.w == FP.Zero;
            
            return new HitBoxData
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

