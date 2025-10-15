using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;
using cfg.Skill;

namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    /// <summary>
    /// 伤害效果处理器
    /// </summary>
    public class DamageEffectHandler : IEffectHandler
    {
        public void Handle(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            ASLogger.Instance.Info($"[DamageEffect] START - Caster: {caster.UniqueId}, Target: {target.UniqueId}, EffectId: {effectConfig.SkillEffectId}");
            
            // 1. 计算伤害
            // TODO: 传入真实的currentFrame（需要从World或Room获取）
            int currentFrame = 0; // 临时使用0，待后续接入帧同步系统
            var damageResult = DamageCalculator.Calculate(caster, target, effectConfig, currentFrame);
            ASLogger.Instance.Info($"[DamageEffect] Calculated damage: {(float)damageResult.FinalDamage:F2} (Critical: {damageResult.IsCritical})");
            
            // 2. 应用伤害到目标
            var healthComponent = target.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                ASLogger.Instance.Warning($"[DamageEffect] Target {target.UniqueId} has no HealthComponent, cannot apply damage");
                return;
            }
            
            int beforeHP = healthComponent.CurrentHealth;
            int damage = (int)(float)damageResult.FinalDamage;
            healthComponent.CurrentHealth -= damage;
            int afterHP = healthComponent.CurrentHealth;
            
            ASLogger.Instance.Info($"[DamageEffect] HP Change - Target {target.UniqueId}: {beforeHP} → {afterHP} (-{damage})");
            
            // 3. TODO: 触发伤害事件
            // EventSystem.Trigger(new DamageEvent { ... });
            
            // 4. TODO: 播放视觉效果
            // if (effectConfig.VisualEffectId > 0) { ... }
            
            // 5. TODO: 播放音效
            // if (effectConfig.SoundEffectId > 0) { ... }
        }
    }
}

