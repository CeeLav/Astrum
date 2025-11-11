using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.SkillSystem;
using cfg.Skill;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 监听技能触发帧事件并创建弹道实体
    /// </summary>
    public class ProjectileSpawnCapability : Capability<ProjectileSpawnCapability>
    {
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag>
        {
            CapabilityTag.Skill,
            CapabilityTag.Combat
        };

        public override int Priority => 260;
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;

        public override void Tick(Entity entity)
        {
            // 事件驱动，无需逐帧逻辑
        }

        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<ProjectileSpawnRequestEvent>(OnProjectileSpawnRequested);
        }

        private void OnProjectileSpawnRequested(Entity entity, ProjectileSpawnRequestEvent evt)
        {
            var world = entity.World;
            if (world == null)
            {
                ASLogger.Instance.Warning("ProjectileSpawnCapability: World is null, skip spawn");
                return;
            }

            var caster = world.GetEntityById(evt.CasterEntityId);
            if (caster == null)
            {
                ASLogger.Instance.Warning($"ProjectileSpawnCapability: caster {evt.CasterEntityId} not found");
                return;
            }

            var effectConfig = SkillConfig.Instance.GetSkillEffect(evt.SkillEffectId);
            if (effectConfig == null)
            {
                ASLogger.Instance.Warning($"ProjectileSpawnCapability: effect {evt.SkillEffectId} not found");
                return;
            }

            var definition = ResolveProjectileDefinition(effectConfig, evt.EffectParamsJson);
            if (definition == null)
            {
                ASLogger.Instance.Warning($"ProjectileSpawnCapability: no projectile definition for effect {evt.SkillEffectId}");
                return;
            }

            var effectIds = ResolveProjectileEffectIds(effectConfig, definition, evt.TriggerInfo, evt.EffectParamsJson);
            if (effectIds.Count == 0)
            {
                ASLogger.Instance.Warning("ProjectileSpawnCapability: effect id list is empty, skip spawn");
                return;
            }

            var spawnDirection = evt.SpawnDirection;
            if (spawnDirection.magnitude <= FP.Epsilon)
            {
                spawnDirection = evt.TriggerInfo != null && caster.GetComponent<TransComponent>() != null
                    ? caster.GetComponent<TransComponent>().Forward
                    : TSVector.forward;
            }

            var projectile = CreateProjectileEntity(world, definition, evt, effectIds, spawnDirection);
            if (projectile == null)
            {
                ASLogger.Instance.Warning("ProjectileSpawnCapability: failed to create projectile entity");
                return;
            }

            InitializeProjectile(projectile, caster, definition, effectIds, evt.SpawnPosition, spawnDirection);
        }

        private ProjectileDefinition? ResolveProjectileDefinition(SkillEffectTable effectConfig, string overrideJson)
        {
            string raw = !string.IsNullOrWhiteSpace(overrideJson) ? overrideJson : (effectConfig.StringParams != null && effectConfig.StringParams.Length > 0 ? effectConfig.StringParams[0] : string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (!doc.RootElement.TryGetProperty(TriggerFrameJsonKeys.ProjectileId, out var idElement) || !idElement.TryGetInt32(out var projectileId) || projectileId <= 0)
                {
                    return null;
                }

                var definition = ProjectileConfigManager.Instance.GetDefinition(projectileId);
                if (definition == null)
                {
                    ASLogger.Instance.Warning($"ProjectileSpawnCapability: projectile definition {projectileId} not registered");
                    return null;
                }

                if (doc.RootElement.TryGetProperty(TriggerFrameJsonKeys.TrajectoryOverride, out var overrideElement) && overrideElement.ValueKind == JsonValueKind.Object)
                {
                    var merged = MergeTrajectoryOverride(definition.TrajectoryData, overrideElement);
                    definition = definition with { TrajectoryData = merged };
                }

                return definition;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ProjectileSpawnCapability: failed to parse projectile definition json - {ex.Message}");
                return null;
            }
        }

        private IReadOnlyList<int> ResolveProjectileEffectIds(SkillEffectTable effectConfig, ProjectileDefinition definition, TriggerFrameInfo trigger, string overrideJson)
        {
            var results = new List<int>();

            if (definition.DefaultEffectIds != null)
            {
                results.AddRange(definition.DefaultEffectIds);
            }

            if (!results.Contains(effectConfig.SkillEffectId))
            {
                results.Add(effectConfig.SkillEffectId);
            }

            if (trigger?.EffectIds != null)
            {
                foreach (var id in trigger.EffectIds)
                {
                    if (id > 0 && !results.Contains(id))
                    {
                        results.Add(id);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(overrideJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(overrideJson);
                    if (doc.RootElement.TryGetProperty(TriggerFrameJsonKeys.AdditionalEffectIds, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in arrayElement.EnumerateArray())
                        {
                            if (item.TryGetInt32(out var extraId) && extraId > 0 && !results.Contains(extraId))
                            {
                                results.Add(extraId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Warning($"ProjectileSpawnCapability: parse additional effect ids failed - {ex.Message}", "Projectile.Spawn");
                }
            }

            return results;
        }

        private Entity? CreateProjectileEntity(World world, ProjectileDefinition definition, ProjectileSpawnRequestEvent evt, IReadOnlyList<int> effectIds, TSVector normalizedDirection)
        {
            if (world.EntityFactory == null)
            {
                return null;
            }

            var context = new ProjectileSpawnContext
            {
                ProjectileId = definition.ProjectileId,
                SkillEffectIds = new List<int>(effectIds),
                CasterId = evt.CasterEntityId,
                SpawnPosition = evt.SpawnPosition,
                SpawnDirection = normalizedDirection,
                OverrideTrajectoryData = null
            };

            return world.EntityFactory.CreateByArchetype(
                definition.ProjectileArchetype,
                new EntityCreationParams
                {
                    SpawnPosition = evt.SpawnPosition,
                    ExtraData = context
                },
                world);
        }

        private void InitializeProjectile(Entity projectile, Entity caster, ProjectileDefinition definition, IReadOnlyList<int> effectIds, TSVector spawnPosition, TSVector direction)
        {
            var projectileComponent = projectile.GetComponent<ProjectileComponent>();
            if (projectileComponent == null)
            {
                ASLogger.Instance.Warning("ProjectileSpawnCapability: ProjectileComponent missing on spawned entity");
                return;
            }

            projectileComponent.SkillEffectIds.Clear();
            foreach (var id in effectIds)
            {
                projectileComponent.SkillEffectIds.Add(id);
            }

            projectileComponent.CasterId = caster.UniqueId;
            projectileComponent.LifeTime = definition.LifeTime;
            projectileComponent.ElapsedFrames = 0;
            projectileComponent.PiercedCount = 0;
            projectileComponent.HitEntities.Clear();
            projectileComponent.TrajectoryType = definition.TrajectoryType;
            projectileComponent.TrajectoryData = definition.TrajectoryData;
            projectileComponent.PierceCount = definition.PierceCount;
            projectileComponent.LaunchDirection = direction.normalized;
            projectileComponent.CurrentVelocity = TSVector.zero;
            projectileComponent.LastPosition = spawnPosition;

            var trans = projectile.GetComponent<TransComponent>();
            if (trans != null)
            {
                trans.Position = spawnPosition;
            }
        }

        private string MergeTrajectoryOverride(string baseJson, JsonElement overrideElement)
        {
            JsonNode node;
            try
            {
                node = string.IsNullOrWhiteSpace(baseJson) ? new JsonObject() : (JsonNode.Parse(baseJson) ?? new JsonObject());
            }
            catch
            {
                node = new JsonObject();
            }

            if (node is not JsonObject obj)
            {
                obj = new JsonObject();
            }

            foreach (var property in overrideElement.EnumerateObject())
            {
                obj[property.Name] = JsonNode.Parse(property.Value.GetRawText());
            }

            return obj.ToJsonString();
        }
    }
}
