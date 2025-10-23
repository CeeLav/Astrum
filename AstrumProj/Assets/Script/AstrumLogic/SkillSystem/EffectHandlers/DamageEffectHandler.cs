using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Stats;
using Astrum.CommonBase;
using cfg.Skill;
using TrueSync;

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
            
            // 1. 获取目标组件
            var dynamicStats = target.GetComponent<DynamicStatsComponent>();
            var derivedStats = target.GetComponent<DerivedStatsComponent>();
            var stateComp = target.GetComponent<StateComponent>();
            
            if (dynamicStats == null || derivedStats == null)
            {
                ASLogger.Instance.Warning($"[DamageEffect] Target {target.UniqueId} missing stats components");
                return;
            }
            
            // 2. 检查是否可以受到伤害
            if (stateComp != null && !stateComp.CanTakeDamage())
            {
                ASLogger.Instance.Info($"[DamageEffect] Target {target.UniqueId} cannot take damage (invincible or dead)");
                return;
            }
            
            // 3. 计算伤害
            int currentFrame = GetCurrentFrame(caster);
            var damageResult = DamageCalculator.Calculate(caster, target, effectConfig, currentFrame);
            ASLogger.Instance.Info($"[DamageEffect] Calculated damage: {(float)damageResult.FinalDamage:F2} (Critical: {damageResult.IsCritical})");
            
            // 4. 应用伤害
            FP beforeHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            FP actualDamage = dynamicStats.TakeDamage(damageResult.FinalDamage, derivedStats);
            FP afterHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            
            ASLogger.Instance.Info($"[DamageEffect] HP Change - Target {target.UniqueId}: {(float)beforeHP:F2} → {(float)afterHP:F2} (-{(float)actualDamage:F2})");
            
            // 5. 检查死亡
            if (afterHP <= FP.Zero && stateComp != null && !stateComp.Get(StateType.DEAD))
            {
                // 设置死亡状态
                stateComp.Set(StateType.DEAD, true);
                
                // 发布死亡事件
                var diedEvent = new EntityDiedEventData(
                    entity: target,
                    worldId: 0,  // TODO: 从Room或World获取真实ID
                    roomId: 0,   // TODO: 从Room获取真实ID
                    killerId: caster.UniqueId,
                    skillId: effectConfig.SkillEffectId
                );
                EventSystem.Instance.Publish(diedEvent);
                
                ASLogger.Instance.Info($"[DamageEffect] Target {target.UniqueId} DIED - Killer: {caster.UniqueId}, Skill: {effectConfig.SkillEffectId}");
            }
            
            // 6. TODO: 播放视觉效果
            // if (effectConfig.VisualEffectId > 0) { ... }
            
            // 7. TODO: 播放音效
            // if (effectConfig.SoundEffectId > 0) { ... }
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

