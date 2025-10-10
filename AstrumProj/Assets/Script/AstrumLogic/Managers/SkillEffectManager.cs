using Astrum.CommonBase;
using Astrum.LogicCore.SkillSystem;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 技能效果管理器 - 统一处理所有技能效果（单例）
    /// Phase 2 将完善此类的实现
    /// </summary>
    public class SkillEffectManager : Singleton<SkillEffectManager>
    {
        /// <summary>
        /// 将技能效果加入队列（Phase 2 实现）
        /// </summary>
        public void QueueSkillEffect(SkillEffectData effectData)
        {
            // TODO: Phase 2 实现
            ASLogger.Instance.Debug($"SkillEffectManager.QueueSkillEffect: Effect {effectData.EffectId} " +
                $"from {effectData.CasterEntity?.UniqueId} to {effectData.TargetEntity?.UniqueId}");
        }
        
        /// <summary>
        /// 每帧更新（Phase 2 实现）
        /// </summary>
        public void Update()
        {
            // TODO: Phase 2 实现
        }
    }
}

