using Astrum.LogicCore.Core;
using cfg.Skill;

namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    /// <summary>
    /// 技能效果处理器接口
    /// 所有具体的效果处理器都需要实现此接口
    /// </summary>
    public interface IEffectHandler
    {
        /// <summary>
        /// 处理技能效果
        /// </summary>
        /// <param name="caster">施法者实体</param>
        /// <param name="target">目标实体</param>
        /// <param name="effectConfig">效果配置数据（来自配置表）</param>
        void Handle(Entity caster, Entity target, SkillEffectTable effectConfig);
    }
}

