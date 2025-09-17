using System;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 位置组件，存储实体的三维坐标
    /// </summary>
    public class PositionComponent : BaseComponent
    {
        /// <summary>
        /// 位置向量（TrueSync 定点）
        /// </summary>
        public TSVector Position { get; set; }

        public PositionComponent() : base() { Position = TSVector.zero; }

        public PositionComponent(FP x, FP y, FP z) : base()
        {
            Position = new TSVector(x, y, z);
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
        public FP DistanceTo(PositionComponent other)
        {
            TSVector diff = Position - other.Position;
            return diff.magnitude;
        }
    }
}
