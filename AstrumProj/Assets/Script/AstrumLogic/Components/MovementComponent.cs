using System;
using Astrum.LogicCore.Capabilities;
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
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<MovementComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
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
        
        /// <summary>
        /// 重置 MovementComponent 状态（用于对象池回收）
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Speed = FP.One;
            BaseSpeed = FP.One;
            CanMove = true;
        }
    }
}
