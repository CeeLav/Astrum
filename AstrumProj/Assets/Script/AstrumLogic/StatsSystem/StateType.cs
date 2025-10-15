namespace Astrum.LogicCore.Stats
{
    /// <summary>
    /// 状态类型枚举
    /// </summary>
    public enum StateType
    {
        // ===== 控制状态 =====
        STUNNED = 1,        // 晕眩（无法移动、攻击、释放技能）
        FROZEN = 2,         // 冰冻（无法移动、攻击）
        KNOCKED_BACK = 3,   // 击飞（强制移动）
        SILENCED = 4,       // 沉默（无法释放技能）
        DISARMED = 5,       // 缴械（无法普通攻击）
        
        // ===== 特殊状态 =====
        INVINCIBLE = 10,    // 无敌（不受伤害）
        INVISIBLE = 11,     // 隐身（不被AI检测）
        BLOCKING = 12,      // 格挡中（提高格挡率）
        DASHING = 13,       // 冲刺中（通常带无敌）
        CASTING = 14,       // 施法中（可能被打断）
        
        // ===== 死亡状态 =====
        DEAD = 20,          // 已死亡
    }
}

