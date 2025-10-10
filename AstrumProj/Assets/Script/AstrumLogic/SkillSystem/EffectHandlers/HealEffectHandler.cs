using System;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;
using cfg.Skill;

namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    /// <summary>
    /// 治疗效果处理器
    /// </summary>
    public class HealEffectHandler : IEffectHandler
    {
        public void Handle(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            var healthComponent = target.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                ASLogger.Instance.Warning($"Target {target.UniqueId} has no HealthComponent, cannot apply heal");
                return;
            }
            
            // 计算治疗量
            int healAmount = (int)effectConfig.EffectValue;
            
            // 应用治疗，限制最大HP
            int oldHP = healthComponent.CurrentHealth;
            healthComponent.CurrentHealth = Math.Min(
                healthComponent.MaxHealth, 
                healthComponent.CurrentHealth + healAmount
            );
            
            int actualHeal = healthComponent.CurrentHealth - oldHP;
            
            ASLogger.Instance.Info($"Heal applied: {actualHeal} to {target.UniqueId}, " +
                $"Current HP: {healthComponent.CurrentHealth}/{healthComponent.MaxHealth}");
            
            // TODO: 触发治疗事件
            // EventSystem.Trigger(new HealEvent { ... });
        }
    }
}

