using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Stats;
using Astrum.CommonBase;
using cfg.Skill;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    /// <summary>
    /// 治疗效果处理器
    /// </summary>
    public class HealEffectHandler : IEffectHandler
    {
        public void Handle(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            ASLogger.Instance.Info($"[HealEffect] START - Caster: {caster.UniqueId}, Target: {target.UniqueId}, EffectId: {effectConfig.SkillEffectId}");
            
            // 1. 获取目标组件
            var dynamicStats = target.GetComponent<DynamicStatsComponent>();
            var derivedStats = target.GetComponent<DerivedStatsComponent>();
            var stateComp = target.GetComponent<StateComponent>();
            
            if (dynamicStats == null || derivedStats == null)
            {
                ASLogger.Instance.Warning($"[HealEffect] Target {target.UniqueId} missing stats components");
                return;
            }
            
            // 2. 检查是否已死亡（死亡不可治疗）
            if (stateComp != null && stateComp.Get(StateType.DEAD))
            {
                ASLogger.Instance.Info($"[HealEffect] Target {target.UniqueId} is dead, cannot heal");
                return;
            }
            
            // 3. 计算治疗量（TODO: 根据effectConfig配置计算）
            FP healAmount = (FP)effectConfig.EffectValue; // 临时使用配置值
            
            // 4. 应用治疗
            FP beforeHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            FP actualHeal = dynamicStats.Heal(healAmount, derivedStats);
            FP afterHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            
            ASLogger.Instance.Info($"[HealEffect] HP Change - Target {target.UniqueId}: {(float)beforeHP:F2} → {(float)afterHP:F2} (+{(float)actualHeal:F2})");
            
            // 5. TODO: 播放视觉效果
            // if (effectConfig.VisualEffectId > 0) { ... }
        }
    }
}

