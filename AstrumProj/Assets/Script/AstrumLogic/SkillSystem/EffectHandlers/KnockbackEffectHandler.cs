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
            
            // 1. 获取或添加击退组件
            var knockback = target.GetComponent<KnockbackComponent>();
            
            // 2. 计算击退方向（从施法者指向目标）
            var direction = CalculateKnockbackDirection(caster, target);
            
            // 3. 设置击退参数
            knockback.IsKnockingBack = true;
            knockback.Direction = direction;
            knockback.TotalDistance = FP.FromFloat(effectConfig.EffectValue / 1000f); // 配置值单位是毫米，转为米
            knockback.RemainingTime = FP.FromFloat(effectConfig.EffectDuration); // 秒
            knockback.Speed = knockback.TotalDistance / knockback.RemainingTime;
            knockback.MovedDistance = FP.Zero;
            knockback.Type = KnockbackType.Linear; // 默认线性
            knockback.CasterId = caster.UniqueId;
            
            ASLogger.Instance.Info($"[KnockbackEffectHandler] Knockback applied: distance={knockback.TotalDistance}m, " +
                $"speed={knockback.Speed}m/s, direction={direction}");
            
            // 4. 发送受击反馈事件
            var hitReactionEvent = new HitReactionEvent
            {
                CasterId = caster.UniqueId,
                TargetId = target.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                EffectType = effectConfig.EffectType,
                HitDirection = direction,
                CausesStun = true // 击退通常产生硬直
            };
            
            target.QueueEvent(hitReactionEvent);
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

