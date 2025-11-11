using System.Collections.Generic;
using Astrum.LogicCore.SkillSystem;
using MemoryPack;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// Projectile 运行时组件：存储弹道生命周期、轨迹和命中状态
    /// </summary>
    [MemoryPackable]
    public partial class ProjectileComponent : BaseComponent
    {
        /// <summary>
        /// 本弹道携带的技能效果列表（支持多个效果叠加）
        /// </summary>
        public List<int> SkillEffectIds { get; set; } = new List<int>();

        /// <summary>
        /// 发射者实体ID
        /// </summary>
        public long CasterId { get; set; } = 0;

        /// <summary>
        /// 弹道生命周期（帧）
        /// </summary>
        public int LifeTime { get; set; } = 300;

        /// <summary>
        /// 已经存活的帧数
        /// </summary>
        public int ElapsedFrames { get; set; } = 0;

        /// <summary>
        /// 当前轨迹类型
        /// </summary>
        public TrajectoryType TrajectoryType { get; set; } = TrajectoryType.Linear;

        /// <summary>
        /// 轨迹原始配置 JSON（包含方向/速度等参数）
        /// </summary>
        public string TrajectoryData { get; set; } = string.Empty;

        /// <summary>
        /// 初始发射方向（归一化）
        /// </summary>
        public TSVector LaunchDirection { get; set; } = TSVector.forward;

        /// <summary>
        /// 当前速度向量（逻辑层维护）
        /// </summary>
        public TSVector CurrentVelocity { get; set; } = TSVector.zero;

        /// <summary>
        /// 上一帧的位置，用于射线碰撞检测
        /// </summary>
        public TSVector LastPosition { get; set; } = TSVector.zero;

        /// <summary>
        /// 允许穿透的实体数量（0 表示不穿透）
        /// </summary>
        public int PierceCount { get; set; } = 0;

        /// <summary>
        /// 当前已穿透的实体数量
        /// </summary>
        public int PiercedCount { get; set; } = 0;

        /// <summary>
        /// 已命中的实体集合，防止同一帧重复结算
        /// </summary>
        [MemoryPackAllowSerialize]
        public HashSet<long> HitEntities { get; set; } = new HashSet<long>();

        /// <summary>
        /// 出射使用的 Socket 名称（用于表现层绑定）
        /// </summary>
        public string SocketName { get; set; } = string.Empty;

        /// <summary>
        /// 逻辑挂点偏移量（相对于 SocketName 挂点的局部坐标系）
        /// </summary>
        public UnityEngine.Vector3 SocketOffset { get; set; } = UnityEngine.Vector3.zero;

        /// <summary>
        /// 是否已标记为待销毁，由 DeadCapability 统一处理
        /// </summary>
        public bool IsMarkedForDestroy { get; set; } = false;

        /// <summary>
        /// Projectile 配置 ID，供表现层查询特效资源路径
        /// </summary>
        public int ProjectileId { get; set; } = 0;
    }
}
