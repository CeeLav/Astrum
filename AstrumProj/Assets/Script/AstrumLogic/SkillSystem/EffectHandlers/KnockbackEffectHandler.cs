using System;
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
            string directionParam = ExtractDirectionParam(effectConfig);
            var direction = CalculateKnockbackDirection(caster, target, directionParam);
            
            // 2. 读取配置参数
            FP distance = FP.FromFloat(effectConfig.EffectValue / 1000f); // 配置值单位是毫米，转为米
            FP duration = FP.FromFloat(effectConfig.EffectDuration); // 秒
            
            ASLogger.Instance.Info($"[KnockbackEffectHandler] Calculated: distance={distance}m, duration={duration}s, directionMode={directionParam}, direction={direction}");
            
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
        private string ExtractDirectionParam(SkillEffectTable effectConfig)
        {
            if (string.IsNullOrWhiteSpace(effectConfig.EffectParams))
                return string.Empty;

            var segments = effectConfig.EffectParams.Split(',');
            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                    continue;

                var parts = segment.Split(':');
                if (parts.Length != 2)
                    continue;

                if (string.Equals(parts[0].Trim(), "Direction", System.StringComparison.OrdinalIgnoreCase))
                {
                    return parts[1].Trim();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 计算击退方向
        /// </summary>
        private TSVector CalculateKnockbackDirection(Entity caster, Entity target, string directionParam)
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
            switch (directionParam?.ToLowerInvariant())
            {
                case "forward":
                    resolved = ResolveForward();
                    break;
                case "backward":
                    {
                        var forward = ResolveForward();
                        resolved = new TSVector(-forward.x, -forward.y, -forward.z);
                    }
                    break;
                case "outward":
                    resolved = ResolveOutward();
                    break;
                case "inward":
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

