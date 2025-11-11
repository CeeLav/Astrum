using System;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.SkillSystem;
using Newtonsoft.Json;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Projectile 逻辑处理（轨迹与生命周期）
    /// </summary>
    public class ProjectileCapability : Capability<ProjectileCapability>
    {
        public override int Priority => 270;

        public override void Tick(Entity entity)
        {
            var projectileComponent = GetComponent<ProjectileComponent>(entity);
            var transComponent = GetComponent<TransComponent>(entity);

            if (projectileComponent == null || transComponent == null)
            {
                return;
            }

            if (projectileComponent.IsMarkedForDestroy)
            {
                return;
            }

            if (!UpdateLifetime(entity, projectileComponent))
            {
                return;
            }

            projectileComponent.LastPosition = transComponent.Position;
            UpdateTrajectory(entity, projectileComponent, transComponent);
            CheckRaycastCollision(entity, projectileComponent, transComponent);
        }

        private bool UpdateLifetime(Entity entity, ProjectileComponent component)
        {
            component.ElapsedFrames++;
            if (component.ElapsedFrames >= component.LifeTime && component.LifeTime > 0)
            {
                MarkProjectileForDestroy(entity, component);
                return false;
            }
            return true;
        }

        private void UpdateTrajectory(Entity entity, ProjectileComponent component, TransComponent trans)
        {
            switch (component.TrajectoryType)
            {
                case TrajectoryType.Linear:
                    UpdateLinearTrajectory(component, trans);
                    break;
                case TrajectoryType.Parabola:
                    UpdateParabolaTrajectory(component, trans);
                    break;
                case TrajectoryType.Homing:
                    UpdateHomingTrajectory(entity, component, trans);
                    break;
                default:
                    UpdateLinearTrajectory(component, trans);
                    break;
            }
        }

        private void UpdateLinearTrajectory(ProjectileComponent component, TransComponent trans)
        {
            var data = ParseTrajectoryData<LinearTrajectoryData>(component.TrajectoryData) ?? new LinearTrajectoryData();
            var speed = data.BaseSpeed > FP.Zero ? data.BaseSpeed : FP.FromFloat(0.5f);
            var dir = component.CurrentVelocity == TSVector.zero ? component.LaunchDirection : component.CurrentVelocity.normalized;

            if (dir == TSVector.zero)
            {
                dir = data.Direction.normalized;
            }

            var velocity = dir * speed;
            component.CurrentVelocity = velocity;
            trans.Position += velocity;
        }

        private void UpdateParabolaTrajectory(ProjectileComponent component, TransComponent trans)
        {
            var data = ParseTrajectoryData<ParabolicTrajectoryData>(component.TrajectoryData) ?? new ParabolicTrajectoryData();
            if (component.CurrentVelocity == TSVector.zero)
            {
                component.CurrentVelocity = (component.LaunchDirection == TSVector.zero ? data.Direction.normalized : component.LaunchDirection) * data.LaunchSpeed;
            }

            component.CurrentVelocity += data.Gravity;
            trans.Position += component.CurrentVelocity;
        }

        private void UpdateHomingTrajectory(Entity entity, ProjectileComponent component, TransComponent trans)
        {
            var data = ParseTrajectoryData<HomingTrajectoryData>(component.TrajectoryData) ?? new HomingTrajectoryData();
            var world = entity.World;
            if (world != null && data.TargetEntityId > 0)
            {
                var target = world.GetEntity(data.TargetEntityId);
                if (target != null)
                {
                    var targetTrans = target.GetComponent<TransComponent>();
                    if (targetTrans != null)
                    {
                        var desiredDir = (targetTrans.Position - trans.Position).normalized;
                        var currentDir = component.CurrentVelocity == TSVector.zero ? component.LaunchDirection : component.CurrentVelocity.normalized;
                        var blended = TSVector.Lerp(currentDir, desiredDir, data.TurnRate);
                        component.CurrentVelocity = blended.normalized * data.BaseSpeed;
                    }
                }
            }

            if (component.CurrentVelocity == TSVector.zero)
            {
                component.CurrentVelocity = (component.LaunchDirection == TSVector.zero ? TSVector.forward : component.LaunchDirection) * data.BaseSpeed;
            }

            trans.Position += component.CurrentVelocity;
        }

        private void CheckRaycastCollision(Entity projectile, ProjectileComponent component, TransComponent trans)
        {
            var world = projectile.World;
            if (world?.HitSystem == null)
                return;

            var delta = trans.Position - component.LastPosition;
            if (delta.sqrMagnitude <= FP.EN4)
                return;

            var distance = delta.magnitude;
            if (distance <= FP.Epsilon)
                return;

            var filter = new CollisionFilter
            {
                ExcludeAllies = false,
                OnlyEnemies = false
            };

            filter.ExcludedEntityIds.Add(projectile.UniqueId);
            if (component.CasterId > 0)
                filter.ExcludedEntityIds.Add(component.CasterId);

            filter.CustomFilter = target => !component.HitEntities.Contains(target.UniqueId);

            var hits = world.HitSystem.QueryRaycast(projectile, component.LastPosition, delta, distance, filter);

            foreach (var hit in hits)
            {
                if (hit.Entity == null)
                    continue;

                if (!OnProjectileHit(projectile, component, hit))
                    break;
            }
        }

        private bool OnProjectileHit(Entity projectile, ProjectileComponent component, RaycastHitInfo hit)
        {
            var target = hit.Entity;
            if (target == null)
                return true;

            // 尝试添加到 HitEntities，如果已存在则跳过（防止同一帧重复命中）
            if (!component.HitEntities.Add(target.UniqueId))
                return true;

            component.PiercedCount++;

            TriggerSkillEffects(projectile, component, target);

            if (component.PierceCount <= 0 || component.PiercedCount >= component.PierceCount)
            {
                MarkProjectileForDestroy(projectile, component);
                return false;
            }

            return true;
        }

        private void TriggerSkillEffects(Entity projectile, ProjectileComponent component, Entity target)
        {
            var world = projectile.World;
            if (world == null)
                return;

            var skillEffectSystem = world.SkillEffectSystem;
            if (skillEffectSystem == null)
                return;

            foreach (var effectId in component.SkillEffectIds)
            {
                var effectData = new SkillEffectData
                {
                    CasterId = component.CasterId,
                    TargetId = target.UniqueId,
                    EffectId = effectId
                };

                skillEffectSystem.QueueSkillEffect(effectData);
            }
        }

        private void MarkProjectileForDestroy(Entity entity, ProjectileComponent component)
        {
            if (component.IsMarkedForDestroy)
                return;

            component.IsMarkedForDestroy = true;
        }

        private T? ParseTrajectoryData<T>(string json) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new T();
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json) ?? new T();
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Warning($"ProjectileCapability: failed to parse trajectory data - {ex.Message}");
                return new T();
            }
        }
    }
}
