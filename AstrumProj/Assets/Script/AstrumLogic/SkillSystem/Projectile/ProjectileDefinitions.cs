using System;
using System.Collections.Generic;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 弹道生成请求上下文（由技能逻辑构造，实体工厂透传）
    /// </summary>
    public sealed class ProjectileSpawnContext
    {
        public int ProjectileId { get; init; }
        public List<int> SkillEffectIds { get; init; } = new List<int>();
        public long CasterId { get; init; }
        public TSVector SpawnPosition { get; init; }
        public TSVector SpawnDirection { get; init; }
        public string? OverrideTrajectoryData { get; init; }
    }

    /// <summary>
    /// Projectile 静态配置
    /// </summary>
    public sealed record ProjectileDefinition
    {
        public int ProjectileId { get; init; }
        public string ProjectileName { get; init; } = string.Empty;
        public string ProjectileArchetype { get; init; } = string.Empty;
        public int LifeTime { get; init; } = 300;
        public TrajectoryType TrajectoryType { get; init; } = TrajectoryType.Linear;
        public string TrajectoryData { get; init; } = string.Empty;
        public int PierceCount { get; init; } = 0;
        public IReadOnlyList<int> DefaultEffectIds { get; init; } = Array.Empty<int>();
    }

    /// <summary>
    /// Projectile 配置管理器（临时内存注册，后续可接入表格）
    /// </summary>
    public sealed class ProjectileConfigManager
    {
        public static ProjectileConfigManager Instance { get; } = new ProjectileConfigManager();

        private readonly Dictionary<int, ProjectileDefinition> _definitions = new Dictionary<int, ProjectileDefinition>();

        private ProjectileConfigManager()
        {
        }

        public ProjectileDefinition? GetDefinition(int projectileId)
        {
            return _definitions.TryGetValue(projectileId, out var def) ? def : null;
        }

        public void RegisterOrReplace(ProjectileDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.ProjectileId <= 0)
            {
                throw new ArgumentException("ProjectileId must be greater than zero", nameof(definition));
            }

            _definitions[definition.ProjectileId] = definition;
        }

        public void Clear()
        {
            _definitions.Clear();
        }
    }
}
