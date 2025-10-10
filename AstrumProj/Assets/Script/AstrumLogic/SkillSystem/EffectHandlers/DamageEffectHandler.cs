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
            // 1. 计算伤害
            var damageResult = DamageCalculator.Calculate(caster, target, effectConfig);
            
            // 2. 应用伤害到目标
            var healthComponent = target.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                ASLogger.Instance.Warning($"Target {target.UniqueId} has no HealthComponent, cannot apply damage");
                return;
            }
            
            healthComponent.CurrentHealth -= (int)damageResult.FinalDamage;
            
            ASLogger.Instance.Info($"Damage applied: {damageResult.FinalDamage} to {target.UniqueId}, " +
                $"Critical: {damageResult.IsCritical}, Remaining HP: {healthComponent.CurrentHealth}");
            
            // 3. TODO: 触发伤害事件
            // EventSystem.Trigger(new DamageEvent { ... });
            
            // 4. TODO: 播放视觉效果
            // if (effectConfig.VisualEffectId > 0) { ... }
            
            // 5. TODO: 播放音效
            // if (effectConfig.SoundEffectId > 0) { ... }
        }
    }
}

