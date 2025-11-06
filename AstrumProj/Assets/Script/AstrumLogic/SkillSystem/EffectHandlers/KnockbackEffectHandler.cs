using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using cfg.Skill;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    /// <summary>
    /// 击退效果处理器 (EffectType = 3)
    /// </summary>
    public class KnockbackEffectHandler : IEffectHandler
    {
        public void Handle(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            ASLogger.Instance.Info($"[KnockbackEffectHandler] Processing knockback: effectId={effectConfig.SkillEffectId}, " +
                $"distance={effectConfig.EffectValue}, duration={effectConfig.EffectDuration}");
            
            // 1. 计算击退方向（只读组件数据）
            var direction = CalculateKnockbackDirection(caster, target);
            
            // 2. 读取配置参数
            FP distance = FP.FromFloat(effectConfig.EffectValue / 1000f); // 配置值单位是毫米，转为米
            FP duration = FP.FromFloat(effectConfig.EffectDuration); // 秒
            
            ASLogger.Instance.Info($"[KnockbackEffectHandler] Calculated: distance={distance}m, duration={duration}s, direction={direction}");
            
            // 3. 发送击退事件给目标（由 KnockbackCapability 接收并写入组件）
            var knockbackEvent = new KnockbackEvent
            {
                CasterId = caster.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                Direction = direction,
                Distance = distance,
                Duration = duration,
                Type = KnockbackType.Linear, // 默认线性，后续可从配置读取
                TriggerWhenInactive = true // 即使 KnockbackCapability 未激活也触发（主动激活）
            };
            
            target.QueueEvent(knockbackEvent);
            ASLogger.Instance.Info($"[KnockbackEffectHandler] Sent KnockbackEvent to target {target.UniqueId}");
            
            // 4. 发送受击反馈事件（用于播放受击动作和特效）
            var hitReactionEvent = new HitReactionEvent
            {
                CasterId = caster.UniqueId,
                TargetId = target.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                EffectType = effectConfig.EffectType,
                HitDirection = direction,
                CausesStun = true, // 击退通常产生硬直
                TriggerWhenInactive = true // 即使 HitReactionCapability 未激活也触发（主动激活）
            };
            
            target.QueueEvent(hitReactionEvent);
            ASLogger.Instance.Info($"[KnockbackEffectHandler] Sent HitReactionEvent to target {target.UniqueId}");
        }
        
        /// <summary>
        /// 计算击退方向
        /// </summary>
        private TSVector CalculateKnockbackDirection(Entity caster, Entity target)
        {
            var casterTrans = caster.GetComponent<TransComponent>();
            var targetTrans = target.GetComponent<TransComponent>();
            
            if (casterTrans == null || targetTrans == null)
                return TSVector.forward;
            
            // 从施法者指向目标
            TSVector direction = targetTrans.Position - casterTrans.Position;
            direction.y = FP.Zero; // 只在水平面击退
            
            if (direction.sqrMagnitude < FP.EN4) // 避免零向量
                return TSVector.forward;
            
            return TSVector.Normalize(direction);
        }
    }
}

