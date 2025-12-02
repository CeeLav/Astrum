using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using cfg;
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
        public EArchetype ProjectileArchetype { get; set; }
        public int LifeTime { get; set; } = 300;
        public TrajectoryType TrajectoryType { get; set; } = TrajectoryType.Linear;
        public string TrajectoryData { get; set; } = string.Empty;
        public int PierceCount { get; set; } = 0;
        public List<int> DefaultEffectIds { get; set; } = new List<int>();
        public string SpawnEffectPath { get; set; } = string.Empty;
        public string LoopEffectPath { get; set; } = string.Empty;
        public string HitEffectPath { get; set; } = string.Empty;
        public FP BaseSpeed { get; set; } = FP.Zero;
        public ProjectileEffectOffsetData SpawnEffectOffset { get; set; } = ProjectileEffectOffsetData.Identity();
        public ProjectileEffectOffsetData LoopEffectOffset { get; set; } = ProjectileEffectOffsetData.Identity();
        public ProjectileEffectOffsetData HitEffectOffset { get; set; } = ProjectileEffectOffsetData.Identity();

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
                DefaultEffectIds = DefaultEffectIds != null ? new List<int>(DefaultEffectIds) : new List<int>(),
                SpawnEffectPath = SpawnEffectPath,
                LoopEffectPath = LoopEffectPath,
                HitEffectPath = HitEffectPath,
                BaseSpeed = BaseSpeed,
                SpawnEffectOffset = SpawnEffectOffset.Clone(),
                LoopEffectOffset = LoopEffectOffset.Clone(),
                HitEffectOffset = HitEffectOffset.Clone()
            };
        }
    }

    public sealed class ProjectileEffectOffsetData
    {
        public TSVector Position { get; set; } = TSVector.zero;
        public TSVector Rotation { get; set; } = TSVector.zero;
        public TSVector Scale { get; set; } = TSVector.one;

        public ProjectileEffectOffsetData Clone()
        {
            return new ProjectileEffectOffsetData
            {
                Position = Position,
                Rotation = Rotation,
                Scale = Scale
            };
        }

        public static ProjectileEffectOffsetData Identity()
        {
            return new ProjectileEffectOffsetData
            {
                Position = TSVector.zero,
                Rotation = TSVector.zero,
                Scale = TSVector.one
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
                    ProjectileArchetype = row.ProjectileArchetype,
                    LifeTime = row.LifeTime,
                    TrajectoryType = trajectoryType,
                    TrajectoryData = row.TrajectoryData ?? string.Empty,
                    PierceCount = row.PierceCount,
                    DefaultEffectIds = row.DefaultEffectIds != null ? new List<int>(row.DefaultEffectIds) : new List<int>(),
                    SpawnEffectPath = row.SpawnEffectPath ?? string.Empty,
                    LoopEffectPath = row.LoopEffectPath ?? string.Empty,
                    HitEffectPath = row.HitEffectPath ?? string.Empty,
                    BaseSpeed = ConvertSpeed(row.BaseSpeed),
                    SpawnEffectOffset = ConvertOffset(row.SpawnEffectPositionOffset, row.SpawnEffectRotationOffset, row.SpawnEffectScaleOffset),
                    LoopEffectOffset = ConvertOffset(row.LoopEffectPositionOffset, row.LoopEffectRotationOffset, row.LoopEffectScaleOffset),
                    HitEffectOffset = ConvertOffset(row.HitEffectPositionOffset, row.HitEffectRotationOffset, row.HitEffectScaleOffset)
                };

                _definitions[definition.ProjectileId] = definition;
            }

            ASLogger.Instance.Debug($"ProjectileConfigManager: Loaded {_definitions.Count} projectile definitions");
        }

        private static readonly FP PositionUnit = FP.FromFloat(0.01f);
        private static readonly FP RotationUnit = FP.FromFloat(1.0f);
        private static readonly FP ScaleUnit = FP.FromFloat(0.01f);

        private static FP ConvertSpeed(int rawSpeed)
        {
            if (rawSpeed <= 0)
            {
                return FP.Zero;
            }

            return FP.FromFloat(rawSpeed * 0.01f);
        }

        private static ProjectileEffectOffsetData ConvertOffset(int[] position, int[] rotation, int[] scale)
        {
            return new ProjectileEffectOffsetData
            {
                Position = ConvertVector(position, PositionUnit, TSVector.zero),
                Rotation = ConvertVector(rotation, RotationUnit, TSVector.zero),
                Scale = ConvertVector(scale, ScaleUnit, TSVector.one)
            };
        }

        private static TSVector ConvertVector(int[] values, FP unit, TSVector defaultValue)
        {
            if (values == null || values.Length < 3)
            {
                return defaultValue;
            }

            return new TSVector(
                (values.Length > 0 ? values[0] : 0) * unit,
                (values.Length > 1 ? values[1] : 0) * unit,
                (values.Length > 2 ? values[2] : 0) * unit);
        }
    }
}
