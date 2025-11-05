using System.Linq;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;

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
            
            // 过滤出当前帧的触发事件
            var triggersAtFrame = skillAction.TriggerEffects
                .Where(t => t.Frame == currentFrame)
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
            // 根据触发类型分发
            switch (trigger.TriggerType.ToLower())
            {
                case "collision":
                    HandleCollisionTrigger(caster, skillAction, trigger);
                    break;
                    
                case "direct":
                    HandleDirectTrigger(caster, trigger);
                    break;
                    
                case "condition":
                    HandleConditionTrigger(caster, skillAction, trigger);
                    break;
                    
                default:
                    ASLogger.Instance.Warning($"Unknown trigger type: {trigger.TriggerType}");
                    break;
            }
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

            // 4. 对每个命中目标触发效果
            foreach (var target in hits)
            {
                TriggerSkillEffect(caster, target, trigger.EffectId);
            }
        }
        
        /// <summary>
        /// 处理直接触发
        /// </summary>
        private void HandleDirectTrigger(Entity caster, TriggerFrameInfo trigger)
        {
            TriggerSkillEffect(caster, caster, trigger.EffectId);
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
            
            // 触发效果
            TriggerSkillEffect(caster, caster, trigger.EffectId);
        }
        
        /// <summary>
        /// 触发技能效果（统一入口）
        /// </summary>
        private void TriggerSkillEffect(Entity caster, Entity target, int effectId)
        {
            // 从 World 获取 SkillEffectSystem
            var effectSystem = caster.World?.SkillEffectSystem;
            if (effectSystem == null)
            {
                ASLogger.Instance.Error("SkillEffectSystem not available from World");
                return;
            }
            
            effectSystem.QueueSkillEffect(new SkillEffectData
            {
                CasterId = caster.UniqueId,
                TargetId = target.UniqueId,
                EffectId = effectId
            });
            
            ASLogger.Instance.Debug($"Queued effect {effectId}: {caster.UniqueId} → {target.UniqueId}");
        }
    }
}
