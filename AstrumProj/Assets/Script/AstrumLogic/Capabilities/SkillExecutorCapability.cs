using System;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.ActionSystem;
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
        
        // ====== 性能优化：预分配缓冲区（避免 GC） ======
        
        /// <summary>
        /// 预分配的触发事件缓冲区，避免每帧创建新 List
        /// 容量 16 足以覆盖大多数技能的触发事件数量
        /// </summary>
        private List<TriggerFrameInfo> _triggerBuffer = new List<TriggerFrameInfo>(16);
        
        /// <summary>
        /// 复用的碰撞过滤器，避免每次碰撞检测时创建新对象
        /// </summary>
        private CollisionFilter _collisionFilter = new CollisionFilter
        {
            ExcludedEntityIds = new HashSet<long>(),
            OnlyEnemies = false
        };
        
        /// <summary>
        /// 预分配的碰撞命中结果缓冲区，避免 HitSystem.QueryHits() 每次创建新 List
        /// 容量 32 足以覆盖大多数碰撞检测的命中数量
        /// </summary>
        private List<Entity> _hitsBuffer = new List<Entity>(32);
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            ASLogger.Instance.Debug($"SkillExecutorCapability attached to entity {entity.UniqueId}");
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
            using (new ProfileScope("SkillExecutorCapability.Tick"))
            {
                // 1. 获取 ActionComponent
                var actionComponent = GetComponent<ActionComponent>(entity);
                if (actionComponent == null) return;
                
                // 2. 检查当前动作是否是技能动作
                var actionInfo = actionComponent.CurrentAction;
                if (actionInfo?.SkillExtension == null)
                    return;
                
                // 3. 获取当前帧
                int currentFrame = actionComponent.CurrentFrame;
                
                // 4. 处理当前帧的触发事件
                using (new ProfileScope("SkillExec.ProcessFrame"))
                {
                    ProcessFrame(entity, actionInfo, currentFrame);
                }
            }
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 处理技能动作的当前帧
        /// </summary>
        private void ProcessFrame(Entity caster, ActionInfo actionInfo, int currentFrame)
        {
            var triggerEffects = actionInfo.GetTriggerEffects();
            if (triggerEffects == null || triggerEffects.Count == 0)
                return;
            
            // 清空缓冲区（不释放容量，避免 GC）
            using (new ProfileScope("ProcessFrame.ClearBuffer"))
            {
                _triggerBuffer.Clear();
            }
            
            // 手动过滤当前帧的触发事件（避免 LINQ ToList 产生 GC）
            using (new ProfileScope("ProcessFrame.FilterTriggers"))
            {
                int count = triggerEffects.Count;
                for (int i = 0; i < count; i++)
                {
                    var trigger = triggerEffects[i];
                    if (trigger.IsFrameInRange(currentFrame))
                    {
                        _triggerBuffer.Add(trigger);
                    }
                }
            }
            
            if (_triggerBuffer.Count == 0)
                return;
            
            //ASLogger.Instance.Debug($"Processing {_triggerBuffer.Count} triggers at frame {currentFrame}");
            
            // 使用 for 循环遍历（避免 foreach 枚举器 GC）
            using (new ProfileScope("ProcessFrame.ProcessTriggers"))
            {
                int triggerCount = _triggerBuffer.Count;
                for (int i = 0; i < triggerCount; i++)
                {
                    ProcessTrigger(caster, actionInfo, _triggerBuffer[i]);
                }
            }
        }
        
        /// <summary>
        /// 处理单个触发事件
        /// </summary>
        private void ProcessTrigger(Entity caster, ActionInfo actionInfo, TriggerFrameInfo trigger)
        {
            using (new ProfileScope("SkillExec.ProcessTrigger"))
        {
            // 根据顶层类型分发
            string type = trigger.Type ?? "SkillEffect";
            
            switch (type)
            {
                case TriggerFrameJsonKeys.TypeSkillEffect:
                        using (new ProfileScope("Trigger.SkillEffect"))
                        {
                    ProcessSkillEffectTrigger(caster, actionInfo, trigger);
                        }
                    break;
                    
                case TriggerFrameJsonKeys.TypeVFX:
                        using (new ProfileScope("Trigger.VFX"))
                        {
                    ProcessVFXTrigger(caster, trigger);
                        }
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
        }
        
        /// <summary>
        /// 处理 SkillEffect 类型触发（内部根据 triggerType 进一步分发）
        /// </summary>
        private void ProcessSkillEffectTrigger(Entity caster, ActionInfo actionInfo, TriggerFrameInfo trigger)
        {
            string triggerType = trigger.TriggerType ?? "";
            
            switch (triggerType)
            {
                case TriggerFrameJsonKeys.TriggerTypeCollision:
                    using (new ProfileScope("SkillEffect.Collision"))
                    {
                    HandleCollisionTrigger(caster, actionInfo, trigger);
                    }
                    break;
                    
                case TriggerFrameJsonKeys.TriggerTypeDirect:
                case TriggerFrameJsonKeys.TriggerTypeProjectile:
                    using (new ProfileScope("SkillEffect.Direct"))
                    {
                    HandleDirectTrigger(caster, trigger);
                    }
                    break;

                case TriggerFrameJsonKeys.TriggerTypeCondition:
                    using (new ProfileScope("SkillEffect.Condition"))
                    {
                    HandleConditionTrigger(caster, actionInfo, trigger);
                    }
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
            using (new ProfileScope("VFX.BuildEventData"))
            {
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
            
            // 转换位置偏移（从 float[] 转换为 TSVector）
            if (trigger.VFXPositionOffset != null && trigger.VFXPositionOffset.Length >= 3)
            {
                eventData.PositionOffset = new TSVector(
                    (FP)trigger.VFXPositionOffset[0],
                    (FP)trigger.VFXPositionOffset[1],
                    (FP)trigger.VFXPositionOffset[2]
                );
            }
            
            // 转换旋转（从 float[] 转换为 TSVector）
            if (trigger.VFXRotation != null && trigger.VFXRotation.Length >= 3)
            {
                eventData.Rotation = new TSVector(
                    (FP)trigger.VFXRotation[0],
                    (FP)trigger.VFXRotation[1],
                    (FP)trigger.VFXRotation[2]
                );
            }
            
            // 转换为 VFXTriggerEvent 并通过 ViewEvent 队列发送
                using (new ProfileScope("VFX.CreateEvent"))
                {
            var vfxEvent = new VFXTriggerEvent
            {
                ResourcePath = eventData.ResourcePath,
                PositionOffset = eventData.PositionOffset,
                Rotation = eventData.Rotation,
                Scale = eventData.Scale,
                PlaybackSpeed = eventData.PlaybackSpeed,
                FollowCharacter = eventData.FollowCharacter,
                Loop = eventData.Loop
            };
            
            // 通过 ViewEvent 队列传递到视图层（异步，不阻塞逻辑层）
                    using (new ProfileScope("VFX.QueueEvent"))
                    {
            caster.QueueViewEvent(new ViewEvent(
                ViewEventType.CustomViewEvent,
                vfxEvent,
                caster.World.CurFrame
            ));
                    }
                }
            
            ASLogger.Instance.Debug($"Queued VFX trigger event: EntityId={caster.UniqueId}, ResourcePath={eventData.ResourcePath}");
            }
        }
        
        /// <summary>
        /// 处理碰撞触发（使用预解析的 CollisionShape）
        /// </summary>
        private void HandleCollisionTrigger(Entity caster, ActionInfo actionInfo, TriggerFrameInfo trigger)
        {
            // 1. 直接使用预解析的 CollisionShape
            if (trigger.CollisionShape == null)
            {
                ASLogger.Instance.Warning($"Collision trigger at frame {trigger.Frame} has no collision shape");
                return;
            }
            
            var shape = trigger.CollisionShape.Value;
            
            // 2. 复用碰撞过滤器（避免每次创建新对象产生 GC）
            using (new ProfileScope("Collision.SetupFilter"))
            {
                _collisionFilter.ExcludedEntityIds.Clear();
                _collisionFilter.ExcludedEntityIds.Add(caster.UniqueId);
                _collisionFilter.OnlyEnemies = false;  // 暂时移除敌对过滤（team系统未实现）
            }
            
            // 3. 从 World 获取 HitSystem 进行碰撞检测
            var hitSystem = caster.World?.HitSystem;
            if (hitSystem == null)
            {
                ASLogger.Instance.Warning("HitSystem not available from World, skipping collision trigger");
                return;
            }
            
            // 使用预分配的 _hitsBuffer，避免每次查询创建新 List
            using (new ProfileScope("Collision.QueryHits"))
            {
                hitSystem.QueryHits(
                caster, 
                shape, 
                    _collisionFilter,
                    _hitsBuffer  // 输出参数：复用的缓冲区
                //skillInstanceId: skillAction.Id
            );
            }

            // 移除冗余日志，只在没有命中时记录
            if (_hitsBuffer.Count == 0)
            {
                ASLogger.Instance.Debug($"[SkillExecutorCapability] Collision trigger hit 0 targets");
                return;
            }

            // 4. 对每个命中目标触发所有效果（使用 for 循环避免枚举器 GC）
            using (new ProfileScope("Collision.TriggerEffects"))
            {
                int hitCount = _hitsBuffer.Count;
                for (int i = 0; i < hitCount; i++)
                {
                    var target = _hitsBuffer[i];
                if (trigger.EffectIds != null)
                {
                        int effectCount = trigger.EffectIds.Length;
                        for (int j = 0; j < effectCount; j++)
                    {
                            TriggerSkillEffect(caster, target, trigger.EffectIds[j], trigger);
                        }
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
                // 使用 for 循环避免枚举器 GC
                int effectCount = trigger.EffectIds.Length;
                for (int i = 0; i < effectCount; i++)
                {
                    TriggerSkillEffect(caster, caster, trigger.EffectIds[i], trigger);
                }
            }
        }
        
        /// <summary>
        /// 处理条件触发
        /// </summary>
        private void HandleConditionTrigger(Entity caster, ActionInfo actionInfo, TriggerFrameInfo trigger)
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
            
            // 触发所有效果（使用 for 循环避免枚举器 GC）
            if (trigger.EffectIds != null)
            {
                int effectCount = trigger.EffectIds.Length;
                for (int i = 0; i < effectCount; i++)
                {
                    TriggerSkillEffect(caster, caster, trigger.EffectIds[i], trigger);
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
                
                var position = trans.Position;
                if (trigger != null)
                {
                    var offset = trigger.SocketOffset;
                    if (offset != TSVector.zero)
                    {
                        var right = trans.Right;
                        var up = trans.Up;
                        var worldOffset = right * offset.x + up * offset.y + forward * offset.z;
                        position += worldOffset;
                    }
                }
                return (position, forward);
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
            using (new ProfileScope("SkillExec.TriggerEffect"))
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
                    using (new ProfileScope("Effect.Projectile"))
            {
                HandleProjectileEffect(caster, effectConfig, trigger);
                    }
                return;
            }

            if (target == null)
            {
                ASLogger.Instance.Warning($"[SkillExecutorCapability] Target is null for effect {effectId}");
                return;
            }

                using (new ProfileScope("Effect.CreateData"))
                {
                    // ✅ 从对象池获取 SkillEffectData（避免每次创建新对象）
                    var effectData = SkillSystem.SkillEffectData.Create(
                        caster.UniqueId,
                        target.UniqueId,
                        effectId
                    );
                    
                    // 加入 SkillEffectSystem 队列
                    using (new ProfileScope("Effect.QueueEffect"))
                    {
                        var skillEffectSystem = caster.World?.SkillEffectSystem;
                        if (skillEffectSystem != null)
                        {
                            skillEffectSystem.QueueSkillEffect(effectData);
                            //ASLogger.Instance.Debug($"[SkillExecutorCapability] Queued SkillEffect: effectId={effectId}, caster={caster.UniqueId}, target={target.UniqueId}");
                        }
                        else
                        {
                            ASLogger.Instance.Error($"[SkillExecutorCapability] SkillEffectSystem not found in World");
                            
                            // 如果入队失败，需要回收对象池
                            if (effectData.IsFromPool)
                            {
                                ObjectPool.Instance.Recycle(effectData);
                            }
                        }
                    }
                }
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
