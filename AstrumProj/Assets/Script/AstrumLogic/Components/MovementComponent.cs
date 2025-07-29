using System;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 移动组件，存储移动相关的数据
    /// </summary>
    public class MovementComponent : BaseComponent
    {
        /// <summary>
        /// 移动速度
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// 是否可以移动
        /// </summary>
        public bool CanMove { get; set; } = true;

        public MovementComponent() : base() { }

        public MovementComponent(float speed) : base()
        {
            Speed = speed;
        }

        /// <summary>
        /// 立即停止移动
        /// </summary>
        public void Stop()
        {
            Speed = 0f;
        }

        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="speed">速度值</param>
        public void SetSpeed(float speed)
        {
            Speed = Math.Max(0f, speed);
        }

        /// <summary>
        /// 获取当前速度
        /// </summary>
        /// <returns>当前速度值</returns>
        public float GetSpeed()
        {
            return Speed;
        }
    }
}
