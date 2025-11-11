using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using cfg.Projectile;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 弹道生成请求上下文（由技能逻辑构造，实体工厂透传）
    /// </summary>
    public sealed class ProjectileSpawnContext
    {
        public int ProjectileId { get; set; }
        public List<int> SkillEffectIds { get; set; } = new List<int>();
        public long CasterId { get; set; }
        public TSVector SpawnPosition { get; set; }
        public TSVector SpawnDirection { get; set; }
        public string OverrideTrajectoryData { get; set; }
    }

    /// <summary>
    /// Projectile 静态配置
    /// </summary>
    public sealed class ProjectileDefinition
    {
        public int ProjectileId { get; set; }
        public string ProjectileName { get; set; } = string.Empty;
        public string ProjectileArchetype { get; set; } = string.Empty;
        public int LifeTime { get; set; } = 300;
        public TrajectoryType TrajectoryType { get; set; } = TrajectoryType.Linear;
        public string TrajectoryData { get; set; } = string.Empty;
        public int PierceCount { get; set; } = 0;
        public List<int> DefaultEffectIds { get; set; } = new List<int>();

        public ProjectileDefinition Clone()
        {
            return new ProjectileDefinition
            {
                ProjectileId = ProjectileId,
                ProjectileName = ProjectileName,
                ProjectileArchetype = ProjectileArchetype,
                LifeTime = LifeTime,
                TrajectoryType = TrajectoryType,
                TrajectoryData = TrajectoryData,
                PierceCount = PierceCount,
                DefaultEffectIds = DefaultEffectIds != null ? new List<int>(DefaultEffectIds) : new List<int>()
            };
        }
    }

    /// <summary>
    /// Projectile 配置管理器
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
            return _definitions.TryGetValue(projectileId, out var def) ? def.Clone() : null;
        }

        public void RegisterOrReplace(ProjectileDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.ProjectileId <= 0)
            {
                throw new ArgumentException("ProjectileId must be greater than zero", nameof(definition));
            }

            _definitions[definition.ProjectileId] = definition.Clone();
        }

        public void Clear()
        {
            _definitions.Clear();
        }

        public void LoadFromTable(TbProjectileTable table)
        {
            _definitions.Clear();

            if (table == null)
            {
                ASLogger.Instance.Warning("ProjectileConfigManager.LoadFromTable: table is null");
                return;
            }

            foreach (var row in table.DataList)
            {
                if (row == null)
                    continue;

                if (!Enum.TryParse(row.TrajectoryType, true, out TrajectoryType trajectoryType))
                {
                    ASLogger.Instance.Warning($"ProjectileConfigManager: Unknown trajectory type '{row.TrajectoryType}' for projectile {row.ProjectileId}, fallback Linear");
                    trajectoryType = TrajectoryType.Linear;
                }

                var definition = new ProjectileDefinition
                {
                    ProjectileId = row.ProjectileId,
                    ProjectileName = row.ProjectileName ?? string.Empty,
                    ProjectileArchetype = row.ProjectileArchetype ?? string.Empty,
                    LifeTime = row.LifeTime,
                    TrajectoryType = trajectoryType,
                    TrajectoryData = row.TrajectoryData ?? string.Empty,
                    PierceCount = row.PierceCount,
                    DefaultEffectIds = row.DefaultEffectIds != null ? new List<int>(row.DefaultEffectIds) : new List<int>()
                };

                _definitions[definition.ProjectileId] = definition;
            }

            ASLogger.Instance.Info($"ProjectileConfigManager: Loaded {_definitions.Count} projectile definitions");
        }
    }
}
