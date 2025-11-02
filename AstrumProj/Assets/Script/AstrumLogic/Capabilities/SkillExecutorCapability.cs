using System.Linq;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 技能动作执行能力 - 负责处理技能动作的触发帧逻辑
    /// 独立轮询，不依赖其他 Capability
    /// </summary>
    [MemoryPackable]
    public partial class SkillExecutorCapability : Capability
    {
        public override void Initialize()
        {
            base.Initialize();
            ASLogger.Instance.Info($"SkillExecutorCapability initialized for entity {OwnerEntity?.UniqueId}");
        }
        
        public override void Tick()
        {
            if (!CanExecute()) return;
            
            // 1. 获取 ActionComponent
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null) return;
            
            // 2. 检查当前动作是否是技能动作
            if (!(actionComponent.CurrentAction is SkillActionInfo skillAction))
                return;
            
            // 3. 获取当前帧
            int currentFrame = actionComponent.CurrentFrame;
            
            // 4. 处理当前帧的触发事件
            ProcessFrame(OwnerEntity, skillAction, currentFrame);
        }
        
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
                ExcludedEntityIds = new System.Collections.Generic.HashSet<long> { caster.UniqueId },
                OnlyEnemies = false  // 暂时移除敌对过滤（team系统未实现）
            };
            
            // 3. 从 World 获取 HitSystem 进行碰撞检测
            var hitSystem = caster.World?.HitSystem;
            if (hitSystem == null)
            {
                ASLogger.Instance.Warning("HitSystem not available from World, skipping collision trigger");
                return;
            }
            
            // 调试：打印攻击盒世界位置
            var trans = caster.GetComponent<TransComponent>();
            var worldTf = shape.ToWorldTransform(trans?.Position ?? TrueSync.TSVector.zero, trans?.Rotation ?? TrueSync.TSQuaternion.identity);
            ASLogger.Instance.Info($"[AI AttackBox] Caster={caster.UniqueId} Center=({(float)worldTf.WorldCenter.x:F2},{(float)worldTf.WorldCenter.y:F2},{(float)worldTf.WorldCenter.z:F2}) Frame={caster.World?.CurFrame ?? 0}");

            var hits = hitSystem.QueryHits(
                caster, 
                shape, 
                filter
                //skillInstanceId: skillAction.Id
            );
            
            ASLogger.Instance.Debug($"Collision trigger hit {hits.Count} targets");
            
            // 调试：打印命中实体位置
            if (hits != null && hits.Count > 0)
            {
                foreach (var target in hits)
                {
                    var tpos = target.GetComponent<TransComponent>()?.Position ?? TrueSync.TSVector.zero;
                    ASLogger.Instance.Info($"[AI AttackHit] Target={target.UniqueId} Pos=({(float)tpos.x:F2},{(float)tpos.y:F2},{(float)tpos.z:F2})");
                }
            }

            // 调试：打印物理世界所有注册实体的位置
            var allBodies = hitSystem.PhysicsWorld?.GetEntityPositions();
            if (allBodies != null)
            {
                foreach (var (id, pos) in allBodies)
                {
                    ASLogger.Instance.Info($"[PhysicsBodies] Id={id} Pos=({(float)pos.x:F2},{(float)pos.y:F2},{(float)pos.z:F2})");
                }
            }

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
