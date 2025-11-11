using TrueSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 实体工厂创建参数
    /// </summary>
    public sealed class EntityCreationParams
    {
        /// <summary>
        /// 可选的实体配置表 ID（未提供时表示无需配置表）
        /// </summary>
        public int? EntityConfigId { get; set; }

        /// <summary>
        /// 初始生成位置（逻辑坐标）
        /// </summary>
        public TSVector? SpawnPosition { get; set; }

        /// <summary>
        /// 初始生成朝向（逻辑旋转）
        /// </summary>
        public TSQuaternion? SpawnRotation { get; set; }

        /// <summary>
        /// 附加数据（用于传递自定义上下文，例如 ProjectileSpawnContext）
        /// </summary>
        public object? ExtraData { get; set; }
    }
}
