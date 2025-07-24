namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 速度组件，存储实体的三维速度
    /// </summary>
    public class VelocityComponent : BaseComponent
    {
        /// <summary>
        /// X方向速度
        /// </summary>
        public float VX { get; set; }

        /// <summary>
        /// Y方向速度
        /// </summary>
        public float VY { get; set; }

        /// <summary>
        /// Z方向速度
        /// </summary>
        public float VZ { get; set; }

        public VelocityComponent() : base() { }

        public VelocityComponent(float vx, float vy, float vz) : base()
        {
            VX = vx;
            VY = vy;
            VZ = vz;
        }

        /// <summary>
        /// 设置速度
        /// </summary>
        /// <param name="vx">X方向速度</param>
        /// <param name="vy">Y方向速度</param>
        /// <param name="vz">Z方向速度</param>
        public void SetVelocity(float vx, float vy, float vz)
        {
            VX = vx;
            VY = vy;
            VZ = vz;
        }

        /// <summary>
        /// 获取速度大小
        /// </summary>
        /// <returns>速度大小</returns>
        public float GetMagnitude()
        {
            return (float)Math.Sqrt(VX * VX + VY * VY + VZ * VZ);
        }

        /// <summary>
        /// 归一化速度向量
        /// </summary>
        public void Normalize()
        {
            float magnitude = GetMagnitude();
            if (magnitude > 0)
            {
                VX /= magnitude;
                VY /= magnitude;
                VZ /= magnitude;
            }
        }
    }
}
