namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 位置组件，存储实体的三维坐标
    /// </summary>
    public class PositionComponent : BaseComponent
    {
        /// <summary>
        /// X坐标
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y坐标
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Z坐标
        /// </summary>
        public float Z { get; set; }

        public PositionComponent() : base() { }

        public PositionComponent(float x, float y, float z) : base()
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// 设置位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="z">Z坐标</param>
        public void SetPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// 获取与另一个位置的距离
        /// </summary>
        /// <param name="other">另一个位置组件</param>
        /// <returns>距离</returns>
        public float DistanceTo(PositionComponent other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            float dz = Z - other.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
