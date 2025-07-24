namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 移动组件，存储移动相关的数据
    /// </summary>
    public class MovementComponent : BaseComponent
    {
        /// <summary>
        /// 最大移动速度
        /// </summary>
        public float MaxSpeed { get; set; }

        /// <summary>
        /// 加速度
        /// </summary>
        public float Acceleration { get; set; }

        /// <summary>
        /// 当前速度
        /// </summary>
        public float CurrentSpeed { get; set; }

        /// <summary>
        /// 摩擦力
        /// </summary>
        public float Friction { get; set; } = 0.9f;

        /// <summary>
        /// 是否可以移动
        /// </summary>
        public bool CanMove { get; set; } = true;

        public MovementComponent() : base() { }

        public MovementComponent(float maxSpeed, float acceleration) : base()
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            CurrentSpeed = 0f;
        }

        /// <summary>
        /// 应用加速度
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        /// <param name="inputMagnitude">输入强度(0-1)</param>
        public void ApplyAcceleration(float deltaTime, float inputMagnitude)
        {
            if (!CanMove) return;

            if (inputMagnitude > 0)
            {
                // 加速
                CurrentSpeed = Math.Min(CurrentSpeed + Acceleration * deltaTime * inputMagnitude, MaxSpeed);
            }
            else
            {
                // 减速
                CurrentSpeed *= Friction;
                if (CurrentSpeed < 0.01f)
                    CurrentSpeed = 0f;
            }
        }

        /// <summary>
        /// 立即停止移动
        /// </summary>
        public void Stop()
        {
            CurrentSpeed = 0f;
        }

        /// <summary>
        /// 设置移动参数
        /// </summary>
        /// <param name="maxSpeed">最大速度</param>
        /// <param name="acceleration">加速度</param>
        /// <param name="friction">摩擦力</param>
        public void SetMovementParams(float maxSpeed, float acceleration, float friction)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            Friction = Math.Clamp(friction, 0f, 1f);
        }
    }
}
