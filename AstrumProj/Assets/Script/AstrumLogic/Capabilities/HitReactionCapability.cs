using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 受击反应能力 - 处理实体受到技能效果的反馈
    /// 优先级：200（低于技能执行，高于击退）
    /// </summary>
    public class HitReactionCapability : Capability<HitReactionCapability>
    {
        // ====== 元数据 ======
        
        public override int Priority => 200;
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag>
        {
            CapabilityTag.Combat,
            CapabilityTag.Animation
        };
        
        // ====== 生命周期 ======
        
        public override bool ShouldActivate(Entity entity)
        {
            return base.ShouldActivate(entity) &&
                   HasComponent<ActionComponent>(entity);
        }
        
        // ====== Tick 方法 ======
        
        /// <summary>
        /// 每帧更新（必须实现）
        /// 新的事件队列系统中，事件由 CapabilitySystem 自动调度，无需在 Tick 中主动拉取
        /// </summary>
        public override void Tick(Entity entity)
        {
            // 事件处理由 RegisterEventHandlers 静态声明，CapabilitySystem 自动调度
            // 此方法保留为空，符合 Capability<T> 接口要求
        }
        
        // ====== 事件处理声明 ======
        
        /// <summary>
        /// 静态声明：该 Capability 处理的事件
        /// </summary>
        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<SkillEffectEvent>(OnSkillEffect);
        }
        
        // ====== 事件处理函数 ======
        
        /// <summary>
        /// 事件处理函数（由 CapabilitySystem 自动调度，第一个参数必须是 Entity）
        /// </summary>
        private void OnSkillEffect(Entity entity, SkillEffectEvent evt)
        {
            ASLogger.Instance.Info($"[HitReactionCapability] OnSkillEffect called for entity {entity.UniqueId}, effectId {evt.EffectId}, caster {evt.CasterId}");
            ProcessSkillEffect(entity, evt);
        }
        
        /// <summary>
        /// 处理技能效果事件
        /// </summary>
        private void ProcessSkillEffect(Entity entity, SkillEffectEvent evt)
        {
            // 获取效果配置
            var effectConfig = GetEffectConfig(evt.EffectId);
            if (effectConfig == null)
            {
                ASLogger.Instance.Warning($"[HitReactionCapability] Effect config not found: {evt.EffectId}");
                return;
            }
            
            ASLogger.Instance.Info($"[HitReactionCapability] Processing effect {evt.EffectId}, type: {effectConfig.EffectType}");
            
            // 根据效果类型处理
            switch (effectConfig.EffectType)
            {
                case 1: // 伤害
                    ProcessDamage(entity, evt, effectConfig);
                    break;
                
                case 2: // 治疗
                    ProcessHeal(entity, evt, effectConfig);
                    break;
                
                case 3: // 击退
                    ProcessKnockback(entity, evt, effectConfig);
                    break;
                
                case 4: // Buff
                    ProcessBuff(entity, evt, effectConfig);
                    break;
                
                case 5: // Debuff
                    ProcessDebuff(entity, evt, effectConfig);
                    break;
                
                default:
                    ASLogger.Instance.Warning($"[HitReactionCapability] Unknown effect type: {effectConfig.EffectType}");
                    break;
            }
        }
        
        // ====== 效果处理方法 ======
        
        /// <summary>
        /// 处理击退效果
        /// </summary>
        private void ProcessKnockback(Entity entity, SkillEffectEvent evt, cfg.Skill.SkillEffectTable effectConfig)
        {
            ASLogger.Instance.Info($"[HitReactionCapability] ProcessKnockback called for entity {entity.UniqueId}");
            
            // 1. 播放受击动作
            PlayHitAction(entity, evt.CasterId);
            
            // 2. 播放受击特效
            PlayHitVFX(entity, evt.CasterId);
            
            // 3. 写入击退数据
            var knockback = GetOrAddComponent<KnockbackComponent>(entity);
            var caster = entity.World?.GetEntity(evt.CasterId);
            
            if (caster != null)
            {
                // 计算击退方向（施法者朝向目标）
                var direction = CalculateKnockbackDirection(caster, entity);
                
                // 设置击退参数
                knockback.IsKnockingBack = true;
                knockback.Direction = direction;
                knockback.TotalDistance = FP.FromFloat(effectConfig.EffectValue); // 米
                knockback.RemainingTime = FP.FromFloat(effectConfig.EffectDuration); // 秒
                knockback.Speed = knockback.TotalDistance / knockback.RemainingTime;
                knockback.MovedDistance = FP.Zero;
                knockback.Type = KnockbackType.Linear; // 默认线性
                knockback.CasterId = evt.CasterId;
                
                ASLogger.Instance.Info($"[HitReactionCapability] Applied knockback: distance={effectConfig.EffectValue}m, " +
                    $"duration={effectConfig.EffectDuration}s, speed={knockback.Speed}m/s, direction={direction}");
            }
            else
            {
                ASLogger.Instance.Warning($"[HitReactionCapability] Caster not found: {evt.CasterId}");
            }
        }
        
        /// <summary>
        /// 计算击退方向
        /// </summary>
        private TSVector CalculateKnockbackDirection(Entity caster, Entity target)
        {
            var casterTrans = GetComponent<TransComponent>(caster);
            var targetTrans = GetComponent<TransComponent>(target);
            
            if (casterTrans == null || targetTrans == null)
                return TSVector.forward;
            
            // 从施法者指向目标
            TSVector direction = targetTrans.Position - casterTrans.Position;
            direction.y = FP.Zero; // 只在水平面击退
            
            if (direction.sqrMagnitude < FP.EN4) // 避免零向量
                return TSVector.forward;
            
            return TSVector.Normalize(direction);
        }
        
        /// <summary>
        /// 播放受击动作
        /// </summary>
        private void PlayHitAction(Entity entity, long casterId)
        {
            // TODO: 根据攻击方向播放不同的受击动作
            // 临时：播放通用受击动作
            var action = GetComponent<ActionComponent>(entity);
            if (action != null)
            {
                // action.PlayAction("Hit", priority: ActionPriority.High);
            }
        }
        
        /// <summary>
        /// 播放受击特效
        /// </summary>
        private void PlayHitVFX(Entity entity, long casterId)
        {
            // TODO: 发布受击特效事件到 View 层
            // EventSystem.Instance.Publish(new HitVFXEvent { ... });
        }
        
        /// <summary>
        /// 处理伤害效果
        /// </summary>
        private void ProcessDamage(Entity entity, SkillEffectEvent evt, cfg.Skill.SkillEffectTable effectConfig)
        {
            ASLogger.Instance.Info($"[HitReactionCapability] ProcessDamage called for entity {entity.UniqueId}, damage: {effectConfig.EffectValue}");
            
            // TODO: 伤害处理
            PlayHitAction(entity, evt.CasterId);
            PlayHitVFX(entity, evt.CasterId);
        }
        
        /// <summary>
        /// 处理治疗效果
        /// </summary>
        private void ProcessHeal(Entity entity, SkillEffectEvent evt, cfg.Skill.SkillEffectTable effectConfig)
        {
            // TODO: 治疗处理
        }
        
        /// <summary>
        /// 处理Buff效果
        /// </summary>
        private void ProcessBuff(Entity entity, SkillEffectEvent evt, cfg.Skill.SkillEffectTable effectConfig)
        {
            // TODO: Buff处理
        }
        
        /// <summary>
        /// 处理Debuff效果
        /// </summary>
        private void ProcessDebuff(Entity entity, SkillEffectEvent evt, cfg.Skill.SkillEffectTable effectConfig)
        {
            // TODO: Debuff处理
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 获取效果配置
        /// </summary>
        private cfg.Skill.SkillEffectTable GetEffectConfig(int effectId)
        {
            return SkillConfig.Instance.GetSkillEffect(effectId);
        }
        
        /// <summary>
        /// 获取或添加组件
        /// </summary>
        private TComponent GetOrAddComponent<TComponent>(Entity entity) where TComponent : BaseComponent, new()
        {
            var component = GetComponent<TComponent>(entity);
            if (component == null)
            {
                component = new TComponent();
                entity.AddComponent(component);
            }
            return component;
        }
    }
}

