using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Events;
using Astrum.CommonBase;
using cfg.Skill;
using TrueSync;
using Astrum.LogicCore.SkillSystem;

namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    /// <summary>
    /// 伤害效果处理器
    /// </summary>
    public class DamageEffectHandler : IEffectHandler
    {
        public void Handle(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            ASLogger.Instance.Debug($"[DamageEffectHandler] Processing damage: Caster={caster.UniqueId}, Target={target.UniqueId}, EffectId={effectConfig.SkillEffectId}");

            if (effectConfig.IntParams == null || effectConfig.IntParams.Length < 3)
            {
                ASLogger.Instance.Error($"[DamageEffectHandler] Effect {effectConfig.SkillEffectId} missing intParams");
                return;
            }
            
            int damageTypeCode = effectConfig.GetDamageTypeCode();
            string visualEffectPath = effectConfig.GetVisualEffectPath();
            string soundEffectPath = effectConfig.GetSoundEffectPath();
            
            // 1. 读取组件（只读，用于计算）
            var casterStats = caster.GetComponent<DerivedStatsComponent>();
            var targetStats = target.GetComponent<DynamicStatsComponent>();
            var targetDerived = target.GetComponent<DerivedStatsComponent>();
            
            if (targetStats == null || targetDerived == null)
            {
                ASLogger.Instance.Warning($"[DamageEffectHandler] Target {target.UniqueId} missing stats components");
                return;
            }
            
            // 2. 计算伤害（纯计算，不修改状态）
            int currentFrame = GetCurrentFrame(caster);
            var damageResult = DamageCalculator.Calculate(caster, target, effectConfig, currentFrame);
            ASLogger.Instance.Debug($"[DamageEffectHandler] Calculated damage: {(float)damageResult.FinalDamage:F2} (Critical: {damageResult.IsCritical})");
            
            // 3. 发送伤害事件给目标（由 DamageCapability 接收并扣血）
            var damageEvent = new DamageEvent
            {
                CasterId = caster.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                Damage = damageResult.FinalDamage,
                IsCritical = damageResult.IsCritical,
                DamageType = damageTypeCode,
                TriggerWhenInactive = true // 即使 DamageCapability 未激活也触发（主动激活）
            };
            
            target.QueueEvent(damageEvent);
            ASLogger.Instance.Debug($"[DamageEffectHandler] Sent DamageEvent to target {target.UniqueId}");
            
            // 4. 发送受击反馈事件（用于播放受击动作和特效）
            var hitDirection = CalculateHitDirection(caster, target);
            var hitReactionEvent = new HitReactionEvent
            {
                CasterId = caster.UniqueId,
                TargetId = target.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                EffectType = effectConfig.EffectType,
                HitDirection = hitDirection,
                CausesStun = damageResult.IsCritical, // 暴击产生硬直
                VisualEffectPath = visualEffectPath,
                SoundEffectPath = soundEffectPath,
                HitOffset = TrueSync.TSVector.zero,
                TriggerWhenInactive = true // 即使 HitReactionCapability 未激活也触发（主动激活）
            };
            
            target.QueueEvent(hitReactionEvent);
            ASLogger.Instance.Debug($"[DamageEffectHandler] Sent HitReactionEvent to target {target.UniqueId}");
        }
        
        /// <summary>
        /// 计算受击方向
        /// </summary>
        private TSVector CalculateHitDirection(Entity caster, Entity target)
        {
            var casterTrans = caster.GetComponent<TransComponent>();
            var targetTrans = target.GetComponent<TransComponent>();
            
            if (casterTrans == null || targetTrans == null)
                return TSVector.forward;
            
            // 从施法者指向目标
            TSVector direction = targetTrans.Position - casterTrans.Position;
            direction.y = FP.Zero; // 只在水平面
            
            if (direction.sqrMagnitude < FP.EN4) // 避免零向量
                return TSVector.forward;
            
            return TSVector.Normalize(direction);
        }
        
        /// <summary>
        /// 获取当前逻辑帧号
        /// </summary>
        private static int GetCurrentFrame(Entity entity)
        {
            // 尝试从实体的World获取当前帧号
            if (entity?.World != null)
            {
                return entity.World.CurFrame;
            }
            
            // 如果无法获取，返回0作为默认值
            ASLogger.Instance.Warning($"[DamageEffect] 无法获取当前帧号，使用默认值0");
            return 0;
        }
    }
}

