using MemoryPack;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 支持的弹道轨迹类型
    /// </summary>
    public enum TrajectoryType
    {
        Linear = 0,
        Parabola = 1,
        Homing = 2
    }

    /// <summary>
    /// 直线弹道配置
    /// </summary>
    [MemoryPackable]
    public partial class LinearTrajectoryData
    {
        public FP BaseSpeed { get; set; } = FP.FromFloat(0.8f);
        public TSVector Direction { get; set; } = TSVector.forward;
    }

    /// <summary>
    /// 抛物线弹道配置
    /// </summary>
    [MemoryPackable]
    public partial class ParabolicTrajectoryData
    {
        public FP LaunchSpeed { get; set; } = FP.FromFloat(0.6f);
        public TSVector Direction { get; set; } = TSVector.forward;
        public TSVector Gravity { get; set; } = new TSVector(FP.Zero, FP.FromFloat(-0.05f), FP.Zero);
    }

    /// <summary>
    /// 追踪弹道配置
    /// </summary>
    [MemoryPackable]
    public partial class HomingTrajectoryData
    {
        public long TargetEntityId { get; set; } = 0;
        public FP BaseSpeed { get; set; } = FP.FromFloat(0.6f);
        public FP TurnRate { get; set; } = FP.FromFloat(0.1f);
    }
}
