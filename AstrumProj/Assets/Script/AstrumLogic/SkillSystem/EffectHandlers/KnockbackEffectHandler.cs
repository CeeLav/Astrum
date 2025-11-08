using System;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using cfg.Skill;
using TrueSync;
using Astrum.LogicCore.SkillSystem;

namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    /// <summary>
    /// 击退效果处理器 (EffectType = "Knockback")
    /// </summary>
    public class KnockbackEffectHandler : IEffectHandler
    {
        public void Handle(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            if (effectConfig.IntParams == null || effectConfig.IntParams.Length < 3)
            {
                ASLogger.Instance.Error($"[KnockbackEffectHandler] Effect {effectConfig.SkillEffectId} missing intParams");
                return;
            }

            int distanceMm = effectConfig.GetIntParam(1, 0);
            int durationMs = effectConfig.GetIntParam(2, 0);
            int directionMode = effectConfig.GetIntParam(3, 2);

            ASLogger.Instance.Info($"[KnockbackEffectHandler] Processing knockback: effectId={effectConfig.SkillEffectId}, " +
                $"distanceMm={distanceMm}, durationMs={durationMs}, directionMode={directionMode}");

            // 1. 计算击退方向（只读组件数据）
            var direction = CalculateKnockbackDirection(caster, target, directionMode);
            
            // 2. 读取配置参数
            FP distance = FP.FromFloat(distanceMm / 1000f);
            FP duration = FP.FromFloat(durationMs / 1000f);
            
            ASLogger.Instance.Info($"[KnockbackEffectHandler] Calculated: distance={distance}m, duration={duration}s, directionMode={directionMode}, direction={direction}");
            
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
                VisualEffectPath = effectConfig.GetVisualEffectPath(),
                SoundEffectPath = effectConfig.GetSoundEffectPath(),
                HitOffset = TrueSync.TSVector.zero,
                TriggerWhenInactive = true // 即使 HitReactionCapability 未激活也触发（主动激活）
            };
            
            target.QueueEvent(hitReactionEvent);
            ASLogger.Instance.Info($"[KnockbackEffectHandler] Sent HitReactionEvent to target {target.UniqueId}");
        }
        
        /// <summary>
        /// 计算击退方向
        /// </summary>
        private TSVector CalculateKnockbackDirection(Entity caster, Entity target, int directionMode)
        {
            var casterTrans = caster.GetComponent<TransComponent>();
            var targetTrans = target.GetComponent<TransComponent>();
            
            if (casterTrans == null || targetTrans == null)
                return TSVector.forward;
            
            TSVector ResolveForward()
            {
                var forward = casterTrans.Rotation * TSVector.forward;
                forward.y = FP.Zero;
                return forward;
            }

            TSVector ResolveOutward()
            {
                TSVector dir = targetTrans.Position - casterTrans.Position;
                dir.y = FP.Zero;
                return dir;
            }

            TSVector resolved;
            switch (directionMode)
            {
                case 0: // Forward
                    resolved = ResolveForward();
                    break;
                case 1: // Backward
                    {
                        var forward = ResolveForward();
                        resolved = new TSVector(-forward.x, -forward.y, -forward.z);
                    }
                    break;
                case 2: // Outward (away from caster)
                    resolved = ResolveOutward();
                    break;
                case 3: // Inward (towards caster)
                    {
                        var outward = ResolveOutward();
                        resolved = new TSVector(-outward.x, -outward.y, -outward.z);
                    }
                    break;
                default:
                    resolved = ResolveOutward();
                    break;
            }

            if (resolved.sqrMagnitude < FP.EN4)
            {
                resolved = ResolveOutward();
            }

            if (resolved.sqrMagnitude < FP.EN4)
            {
                resolved = ResolveForward();
            }

            if (resolved.sqrMagnitude < FP.EN4)
            {
                resolved = TSVector.forward;
            }

            return TSVector.Normalize(resolved);
        }
    }
}

