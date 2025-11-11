using System;
using System.Linq;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using Astrum.CommonBase.Events;
using Astrum.LogicCore.Events;
using cfg.Skill;
using TrueSync;
using static Astrum.CommonBase.TriggerFrameJsonKeys;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 技能动作执行能力（新架构，基于 Capability&lt;T&gt;）
    /// 负责处理技能动作的触发帧逻辑
    /// 独立轮询，不依赖其他 Capability
    /// </summary>
    public class SkillExecutorCapability : Capability<SkillExecutorCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 250; // 优先级高于 SkillCapability (300)，确保执行逻辑优先
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.Skill, 
            CapabilityTag.Combat 
        };
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            ASLogger.Instance.Info($"SkillExecutorCapability attached to entity {entity.UniqueId}");
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 检查必需组件是否存在
            return base.ShouldActivate(entity) &&
                   HasComponent<ActionComponent>(entity);
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 缺少任何必需组件则停用
            return base.ShouldDeactivate(entity) ||
                   !HasComponent<ActionComponent>(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            // 1. 获取 ActionComponent
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null) return;
            
            // 2. 检查当前动作是否是技能动作
            if (!(actionComponent.CurrentAction is SkillActionInfo skillAction))
                return;
            
            // 3. 获取当前帧
            int currentFrame = actionComponent.CurrentFrame;
            
            // 4. 处理当前帧的触发事件
            ProcessFrame(entity, skillAction, currentFrame);
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 处理技能动作的当前帧
        /// </summary>
        private void ProcessFrame(Entity caster, SkillActionInfo skillAction, int currentFrame)
        {
            if (skillAction.TriggerEffects == null || skillAction.TriggerEffects.Count == 0)
                return;
            
            // 过滤出当前帧的触发事件（支持单帧和多帧范围）
            var triggersAtFrame = skillAction.TriggerEffects
                .Where(t => t.IsFrameInRange(currentFrame))
                .ToList();
            
            if (triggersAtFrame.Count == 0)
                return;
            
            ASLogger.Instance.Debug($"Processing {triggersAtFrame.Count} triggers at frame {currentFrame}");
            
            // 处理每个触发事件
            foreach (var trigger in triggersAtFrame)
            {
                ProcessTrigger(caster, skillAction, trigger);
            }
        }
        
        /// <summary>
        /// 处理单个触发事件
        /// </summary>
        private void ProcessTrigger(Entity caster, SkillActionInfo skillAction, TriggerFrameInfo trigger)
        {
            // 根据顶层类型分发
            string type = trigger.Type ?? "SkillEffect";
            
            switch (type)
            {
                case TriggerFrameJsonKeys.TypeSkillEffect:
                    ProcessSkillEffectTrigger(caster, skillAction, trigger);
                    break;
                    
                case TriggerFrameJsonKeys.TypeVFX:
                    ProcessVFXTrigger(caster, trigger);
                    break;
                    
                case TriggerFrameJsonKeys.TypeSFX:
                    // TODO: 实现 SFX 处理
                    ASLogger.Instance.Warning("SFX trigger not yet implemented");
                    break;
                    
                default:
                    ASLogger.Instance.Warning($"Unknown trigger type: {type}");
                    break;
            }
        }
        
        /// <summary>
        /// 处理 SkillEffect 类型触发（内部根据 triggerType 进一步分发）
        /// </summary>
        private void ProcessSkillEffectTrigger(Entity caster, SkillActionInfo skillAction, TriggerFrameInfo trigger)
        {
            string triggerType = trigger.TriggerType ?? "";
            
            switch (triggerType)
            {
                case TriggerFrameJsonKeys.TriggerTypeCollision:
                    HandleCollisionTrigger(caster, skillAction, trigger);
                    break;
                    
                case TriggerFrameJsonKeys.TriggerTypeDirect:
                case TriggerFrameJsonKeys.TriggerTypeProjectile:
                    HandleDirectTrigger(caster, trigger);
                    break;

                case TriggerFrameJsonKeys.TriggerTypeCondition:
                    HandleConditionTrigger(caster, skillAction, trigger);
                    break;
                    
                default:
                    ASLogger.Instance.Warning($"Unknown SkillEffect triggerType: {triggerType}");
                    break;
            }
        }
        
        /// <summary>
        /// 处理 VFX 类型触发（发布事件到 View 层）
        /// </summary>
        private void ProcessVFXTrigger(Entity caster, TriggerFrameInfo trigger)
        {
            // 获取当前帧（从 ActionComponent）
            var actionComponent = GetComponent<ActionComponent>(caster);
            int currentFrame = actionComponent?.CurrentFrame ?? 0;
            
            // 检查是否为开始帧（对于多帧范围的 VFX，只在开始帧触发一次）
            bool shouldTrigger = false;
            
            if (trigger.StartFrame.HasValue && trigger.EndFrame.HasValue)
            {
                // 多帧范围：只在开始帧触发
                shouldTrigger = (currentFrame == trigger.StartFrame.Value);
            }
            else if (trigger.Frame > 0)
            {
                // 单帧：直接触发
                shouldTrigger = (currentFrame == trigger.Frame);
            }
            
            if (!shouldTrigger)
                return;
            
            // 构建 VFX 事件数据
            var eventData = new VFXTriggerEventData
            {
                EntityId = caster.UniqueId,
                ResourcePath = trigger.VFXResourcePath ?? string.Empty,
                Scale = trigger.VFXScale,
                PlaybackSpeed = trigger.VFXPlaybackSpeed,
                FollowCharacter = trigger.VFXFollowCharacter,
                Loop = trigger.VFXLoop,
                InstanceId = trigger.GetHashCode() // 使用触发帧的哈希作为实例ID
            };
            
            // 转换位置偏移（从 float[] 转换为 Unity Vector3）
            if (trigger.VFXPositionOffset != null && trigger.VFXPositionOffset.Length >= 3)
            {
                eventData.PositionOffset = new UnityEngine.Vector3(
                    trigger.VFXPositionOffset[0],
                    trigger.VFXPositionOffset[1],
                    trigger.VFXPositionOffset[2]
                );
            }
            
            // 转换旋转（从 float[] 转换为 Unity Vector3）
            if (trigger.VFXRotation != null && trigger.VFXRotation.Length >= 3)
            {
                eventData.Rotation = new UnityEngine.Vector3(
                    trigger.VFXRotation[0],
                    trigger.VFXRotation[1],
                    trigger.VFXRotation[2]
                );
            }
            
            // 发布事件
            EventSystem.Instance.Publish(eventData);
            
            ASLogger.Instance.Debug($"Published VFX trigger event: EntityId={caster.UniqueId}, ResourcePath={eventData.ResourcePath}");
        }
        
        /// <summary>
        /// 处理碰撞触发（使用预解析的 CollisionShape）
        /// </summary>
        private void HandleCollisionTrigger(Entity caster, SkillActionInfo skillAction, TriggerFrameInfo trigger)
        {
            // 1. 直接使用预解析的 CollisionShape
            if (trigger.CollisionShape == null)
            {
                ASLogger.Instance.Warning($"Collision trigger at frame {trigger.Frame} has no collision shape");
                return;
            }
            
            var shape = trigger.CollisionShape.Value;
            
            // 2. 构造碰撞过滤器
            var filter = new CollisionFilter
            {
                ExcludedEntityIds = new HashSet<long> { caster.UniqueId },
                OnlyEnemies = false  // 暂时移除敌对过滤（team系统未实现）
            };
            
            // 3. 从 World 获取 HitSystem 进行碰撞检测
            var hitSystem = caster.World?.HitSystem;
            if (hitSystem == null)
            {
                ASLogger.Instance.Warning("HitSystem not available from World, skipping collision trigger");
                return;
            }
            
            var hits = hitSystem.QueryHits(
                caster, 
                shape, 
                filter
                //skillInstanceId: skillAction.Id
            );

            ASLogger.Instance.Info($"[SkillExecutorCapability] Collision trigger hit {hits.Count} targets, EffectIds: [{string.Join(",", trigger.EffectIds ?? new int[0])}]");

            // 4. 对每个命中目标触发所有效果
            foreach (var target in hits)
            {
                if (trigger.EffectIds != null)
                {
                    foreach (var effectId in trigger.EffectIds)
                    {
                        TriggerSkillEffect(caster, target, effectId, trigger);
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理直接触发
        /// </summary>
        private void HandleDirectTrigger(Entity caster, TriggerFrameInfo trigger)
        {
            if (trigger.EffectIds != null)
            {
                foreach (var effectId in trigger.EffectIds)
                {
                    TriggerSkillEffect(caster, caster, effectId, trigger);
                }
            }
        }
        
        /// <summary>
        /// 处理条件触发
        /// </summary>
        private void HandleConditionTrigger(Entity caster, SkillActionInfo skillAction, TriggerFrameInfo trigger)
        {
            // 检查条件（简化实现）
            if (trigger.Condition != null)
            {
                // TODO: 实际条件检查逻辑
                // 示例：检查能量
                // if (trigger.Condition.EnergyMin > 0)
                // {
                //     var resourceComp = caster.GetComponent<ResourceComponent>();
                //     if (resourceComp?.GetResource("Energy") < trigger.Condition.EnergyMin)
                //         return;
                // }
                
                // 示例：检查状态标记
                // if (!string.IsNullOrEmpty(trigger.Condition.RequiredTag))
                // {
                //     var statusComp = caster.GetComponent<StatusComponent>();
                //     if (!statusComp?.HasTag(trigger.Condition.RequiredTag) ?? true)
                //         return;
                // }
            }
            
            // 触发所有效果
            if (trigger.EffectIds != null)
            {
                foreach (var effectId in trigger.EffectIds)
                {
                    TriggerSkillEffect(caster, caster, effectId, trigger);
                }
            }
        }

        private (TSVector Position, TSVector Direction) CalculateProjectileSpawnTransform(Entity caster, TriggerFrameInfo trigger)
        {
            var trans = GetComponent<TransComponent>(caster);
            if (trans != null)
            {
                var forward = trans.Forward;
                if (forward == TSVector.zero)
                {
                    forward = TSVector.forward;
                }
                return (trans.Position, forward);
            }

            return (TSVector.zero, TSVector.forward);
        }

        private string ExtractEffectParams(SkillEffectTable effectConfig)
        {
            if (effectConfig.StringParams != null && effectConfig.StringParams.Length > 0)
            {
                return effectConfig.StringParams[0] ?? string.Empty;
            }

            return string.Empty;
        }
        
        /// <summary>
        /// 触发技能效果（统一入口）
        /// 直接调用 SkillEffectSystem 处理效果
        /// </summary>
        private void TriggerSkillEffect(Entity caster, Entity target, int effectId, TriggerFrameInfo trigger)
        {
            // 构造效果数据
            var effectConfig = SkillConfig.Instance.GetSkillEffect(effectId);
            if (effectConfig == null)
            {
                ASLogger.Instance.Warning($"[SkillExecutorCapability] Effect config not found: {effectId}");
                return;
            }

            var effectType = effectConfig.EffectType ?? string.Empty;

            if (string.Equals(effectType, "Projectile", StringComparison.OrdinalIgnoreCase))
            {
                HandleProjectileEffect(caster, effectConfig, trigger);
                return;
            }

            if (target == null)
            {
                ASLogger.Instance.Warning($"[SkillExecutorCapability] Target is null for effect {effectId}");
                return;
            }

            var effectData = new SkillSystem.SkillEffectData
            {
                CasterId = caster.UniqueId,
                TargetId = target.UniqueId,
                EffectId = effectId
            };
            
            // 加入 SkillEffectSystem 队列
            var skillEffectSystem = caster.World?.SkillEffectSystem;
            if (skillEffectSystem != null)
            {
                skillEffectSystem.QueueSkillEffect(effectData);
                ASLogger.Instance.Info($"[SkillExecutorCapability] Queued SkillEffect: effectId={effectId}, caster={caster.UniqueId}, target={target.UniqueId}");
            }
            else
            {
                ASLogger.Instance.Error($"[SkillExecutorCapability] SkillEffectSystem not found in World");
            }
        }

        private void HandleProjectileEffect(Entity caster, SkillEffectTable effectConfig, TriggerFrameInfo trigger)
        {
            var (spawnPosition, spawnDirection) = CalculateProjectileSpawnTransform(caster, trigger);

            var request = new ProjectileSpawnRequestEvent
            {
                CasterEntityId = caster.UniqueId,
                SkillEffectId = effectConfig.SkillEffectId,
                EffectParamsJson = ExtractEffectParams(effectConfig),
                TriggerInfo = trigger,
                SpawnPosition = spawnPosition,
                SpawnDirection = spawnDirection,
                SocketName = trigger.SocketName ?? string.Empty,
                TriggerWhenInactive = true
            };

            caster.QueueEvent(request);
        }
    }
}
