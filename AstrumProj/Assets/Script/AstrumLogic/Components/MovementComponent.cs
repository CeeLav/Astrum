using System;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 移动组件，存储移动相关的数据
    /// </summary>
    [MemoryPackable]
    public partial class MovementComponent : BaseComponent
    {
        /// <summary>
        /// 移动速度
        /// </summary>
        public FP Speed { get; set; } = FP.One;

        /// <summary>
        /// 基准移动速度（通常来自配置）
        /// </summary>
        [MemoryPackInclude]
        public FP BaseSpeed { get; private set; } = FP.One;

        /// <summary>
        /// 是否可以移动
        /// </summary>
        public bool CanMove { get; set; } = true;

        [MemoryPackConstructor]
        public MovementComponent() : base() { }

        public MovementComponent(FP speed) : base()
        {
            Speed = speed;
            BaseSpeed = speed;
        }

        /// <summary>
        /// 立即停止移动
        /// </summary>
        public void Stop()
        {
            Speed = FP.Zero;
        }

        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="speed">速度值</param>
        public void SetSpeed(FP speed)
        {
            Speed = TSMath.Max(FP.Zero, speed);
        }

        /// <summary>
        /// 设置基准移动速度（通常来自配置）
        /// </summary>
        public void SetBaseSpeed(FP speed)
        {
            BaseSpeed = TSMath.Max(FP.Zero, speed);
        }

        /// <summary>
        /// 获取基准移动速度
        /// </summary>
        public FP GetBaseSpeed()
        {
            return BaseSpeed;
        }

        /// <summary>
        /// 获取当前速度与基准速度的倍率（基准为0时返回1）
        /// </summary>
        public FP GetSpeedMultiplier()
        {
            return BaseSpeed > FP.Zero ? Speed / BaseSpeed : FP.One;
        }

        /// <summary>
        /// 获取当前速度
        /// </summary>
        /// <returns>当前速度值</returns>
        public FP GetSpeed()
        {
            return Speed;
        }
    }
}
