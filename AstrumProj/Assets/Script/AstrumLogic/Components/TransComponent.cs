using System;
using TrueSync;
using MemoryPack;
using Astrum.LogicCore.Capabilities;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 变换组件（Transform），存储实体的位置和旋转
    /// 注：为了向后兼容，类名保留 PositionComponent，但功能已扩展为完整的 Transform
    /// </summary>
    [MemoryPackable]
    public partial class TransComponent : BaseComponent
    {
        /// <summary>
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<TransComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
        /// <summary>
        /// 位置向量（TrueSync 定点）
        /// </summary>
        public TSVector Position { get; set; }

        /// <summary>
        /// 旋转四元数（TrueSync 定点）
        /// </summary>
        public TSQuaternion Rotation { get; set; }

        public TransComponent() : base() 
        { 
            Position = TSVector.zero;
            Rotation = TSQuaternion.identity;
        }

        [MemoryPackConstructor]
        public TransComponent(long entityId, TSVector position, TSQuaternion rotation) : base()
        {
            EntityId = entityId;
            Position = position;
            Rotation = rotation;
        }

        public TransComponent(FP x, FP y, FP z) : base()
        {
            Position = new TSVector(x, y, z);
            Rotation = TSQuaternion.identity;
        }

        public TransComponent(TSVector position, TSQuaternion rotation) : base()
        {
            Position = position;
            Rotation = rotation;
        }

        /// <summary>
        /// 设置位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="z">Z坐标</param>
        public void SetPosition(FP x, FP y, FP z)
        {
            Position = new TSVector(x, y, z);
        }

        public TSVector GetPosition()
        {
            return Position;
        }

        /// <summary>
        /// 获取与另一个位置的距离
        /// </summary>
        /// <param name="other">另一个位置组件</param>
        /// <returns>距离（FP）</returns>
        public FP DistanceTo(TransComponent other)
        {
            TSVector diff = Position - other.Position;
            return diff.magnitude;
        }

        // ========== 旋转相关方法 ==========

        /// <summary>
        /// 设置旋转
        /// </summary>
        public void SetRotation(TSQuaternion rotation)
        {
            Rotation = rotation;
        }

        /// <summary>
        /// 从欧拉角设置旋转
        /// </summary>
        public void SetRotationFromEuler(FP x, FP y, FP z)
        {
            Rotation = TSQuaternion.Euler(x, y, z);
        }

        /// <summary>
        /// 从Y轴角度设置旋转（2D游戏常用）
        /// </summary>
        public void SetRotationFromYaw(FP yawDegrees)
        {
            Rotation = TSQuaternion.Euler(FP.Zero, yawDegrees, FP.Zero);
        }

        /// <summary>
        /// 获取前方向量
        /// </summary>
        public TSVector Forward => Rotation * TSVector.forward;

        /// <summary>
        /// 获取右方向量
        /// </summary>
        public TSVector Right => Rotation * TSVector.right;

        /// <summary>
        /// 获取上方向量
        /// </summary>
        public TSVector Up => Rotation * TSVector.up;

        /// <summary>
        /// 朝向目标点
        /// </summary>
        public void LookAt(TSVector target)
        {
            var direction = target - Position;
            if (direction.sqrMagnitude > FP.EN4)  // 避免零向量
            {
                Rotation = TSQuaternion.LookRotation(direction, TSVector.up);
            }
        }

        /// <summary>
        /// 绕Y轴旋转（2D游戏常用）
        /// </summary>
        public void RotateY(FP degrees)
        {
            Rotation = TSQuaternion.Euler(FP.Zero, degrees, FP.Zero) * Rotation;
        }
    }
}
