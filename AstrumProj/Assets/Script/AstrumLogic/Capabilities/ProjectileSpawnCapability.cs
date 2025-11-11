using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.SkillSystem;
using cfg.Skill;
using Astrum.LogicCore.Factories;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

            var caster = world.GetEntity(evt.CasterEntityId);
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

            if (world == null)
            {
                ASLogger.Instance.Warning("ProjectileSpawnCapability: World is null, skip spawn");
                return;
            }

            QueueProjectileEntity(world, definition, evt, effectIds, spawnDirection, caster);
        }

        private ProjectileDefinition? ResolveProjectileDefinition(SkillEffectTable effectConfig, string overrideJson)
        {
            int projectileId = 0;
            if (effectConfig.IntParams != null && effectConfig.IntParams.Length > 0)
            {
                projectileId = effectConfig.IntParams[0];
            }

            string raw = !string.IsNullOrWhiteSpace(overrideJson)
                ? overrideJson
                : (effectConfig.StringParams != null && effectConfig.StringParams.Length > 0
                    ? effectConfig.StringParams[0]
                    : string.Empty);

            try
            {
                if (projectileId <= 0 && !string.IsNullOrWhiteSpace(raw))
                {
                    var rootForId = JObject.Parse(raw);
                    if (rootForId.TryGetValue(TriggerFrameJsonKeys.ProjectileId, out var idToken) && idToken.Type == JTokenType.Integer)
                    {
                        projectileId = idToken.Value<int>();
                    }
                }

                if (projectileId <= 0)
                {
                    ASLogger.Instance.Warning("ProjectileSpawnCapability: projectileId missing in effect IntParams/StringParams");
                    return null;
                }

                var definition = ProjectileConfigManager.Instance.GetDefinition(projectileId);
                if (definition == null)
                {
                    ASLogger.Instance.Warning($"ProjectileSpawnCapability: projectile definition {projectileId} not registered");
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var root = JObject.Parse(raw);
                    if (root.TryGetValue(TriggerFrameJsonKeys.TrajectoryOverride, out var overrideToken) && overrideToken is JObject overrideObject)
                    {
                        var merged = MergeTrajectoryOverride(definition.TrajectoryData, overrideObject);
                        definition.TrajectoryData = merged;
                    }
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

            if (!string.Equals(effectConfig.EffectType, "Projectile", StringComparison.OrdinalIgnoreCase)
                && !results.Contains(effectConfig.SkillEffectId))
            {
                results.Add(effectConfig.SkillEffectId);
            }

            if (effectConfig.IntParams != null && effectConfig.IntParams.Length > 1)
            {
                for (int i = 1; i < effectConfig.IntParams.Length; i++)
                {
                    var extraId = effectConfig.IntParams[i];
                    if (extraId > 0 && !results.Contains(extraId))
                    {
                        results.Add(extraId);
                    }
                }
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
                    var root = JObject.Parse(overrideJson);
                    if (root.TryGetValue(TriggerFrameJsonKeys.AdditionalEffectIds, out var arrayToken) && arrayToken is JArray array)
                    {
                        foreach (var item in array)
                        {
                            var extraId = item.Type == JTokenType.Integer ? item.Value<int>() : (int?)null;
                            if (extraId.HasValue && extraId.Value > 0 && !results.Contains(extraId.Value))
                            {
                                results.Add(extraId.Value);
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

        private void QueueProjectileEntity(World world, ProjectileDefinition definition, ProjectileSpawnRequestEvent evt, IReadOnlyList<int> effectIds, TSVector normalizedDirection, Entity caster)
        {
            var context = new ProjectileSpawnContext
            {
                ProjectileId = definition.ProjectileId,
                SkillEffectIds = new List<int>(effectIds),
                CasterId = evt.CasterEntityId,
                SpawnPosition = evt.SpawnPosition,
                SpawnDirection = normalizedDirection,
                OverrideTrajectoryData = null
            };

            var creationParams = new EntityCreationParams
            {
                SpawnPosition = evt.SpawnPosition,
                ExtraData = context
            };

            world.QueueCreateEntity(
                definition.ProjectileArchetype,
                creationParams,
                projectile =>
                {
                    if (projectile == null)
                    {
                        ASLogger.Instance.Warning("ProjectileSpawnCapability: queued projectile creation returned null");
                        return;
                    }

                    InitializeProjectile(projectile, caster, definition, effectIds, evt.SpawnPosition, normalizedDirection, evt.SocketName, evt.SocketOffset);
                });
        }

        private void InitializeProjectile(Entity projectile, Entity caster, ProjectileDefinition definition, IReadOnlyList<int> effectIds, TSVector spawnPosition, TSVector direction, string socketName, UnityEngine.Vector3 socketOffset)
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
            projectileComponent.InitialPosition = spawnPosition;
            projectileComponent.SocketName = socketName ?? string.Empty;
            projectileComponent.SocketOffset = socketOffset;
            projectileComponent.IsMarkedForDestroy = false;
            projectileComponent.ProjectileId = definition.ProjectileId; // 供表现层查询配置

            var trans = projectile.GetComponent<TransComponent>();
            if (trans != null)
            {
                trans.Position = spawnPosition;
            }
        }

        private string MergeTrajectoryOverride(string baseJson, JObject overrideObject)
        {
            JObject baseObject;
            try
            {
                baseObject = string.IsNullOrWhiteSpace(baseJson) ? new JObject() : JObject.Parse(baseJson);
            }
            catch
            {
                baseObject = new JObject();
            }

            baseObject.Merge(overrideObject, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace
            });

            return baseObject.ToString(Formatting.None);
        }
    }
}
