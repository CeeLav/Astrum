namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Capability 标签枚举
    /// 普通枚举，每个 Capability 可以拥有多个 Tag（使用 HashSet 存储）
    /// </summary>
    public enum CapabilityTag
    {
        /// <summary>
        /// 移动相关（位移、旋转等）
        /// </summary>
        Movement,
        
        /// <summary>
        /// 玩家控制相关（输入响应、指令处理等）
        /// </summary>
        Control,
        
        /// <summary>
        /// 攻击相关（普通攻击、连击等）
        /// </summary>
        Attack,
        
        /// <summary>
        /// 技能相关（技能释放、技能效果等）
        /// </summary>
        Skill,
        
        /// <summary>
        /// AI 相关（AI 决策、状态机等）
        /// </summary>
        AI,
        
        /// <summary>
        /// 动画相关（动画播放、状态同步等，通常不禁用）
        /// </summary>
        Animation,
        
        /// <summary>
        /// 物理相关（碰撞、物理模拟等）
        /// </summary>
        Physics,
        
        /// <summary>
        /// 战斗相关（伤害计算、状态效果等）
        /// </summary>
        Combat,
        
        /// <summary>
        /// 交互相关（拾取、对话、使用物品等）
        /// </summary>
        Interaction,
        
        /// <summary>
        /// 用户输入位移（玩家通过输入控制的位移，技能释放时会被禁用）
        /// </summary>
        UserInputMovement
    }
}

