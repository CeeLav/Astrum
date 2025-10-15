namespace Astrum.LogicCore.Stats
{
    /// <summary>
    /// 属性类型枚举 - 定义游戏中所有数值属性
    /// </summary>
    public enum StatType
    {
        // ===== 基础战斗属性 =====
        HP = 1,              // 生命值上限
        ATK = 2,             // 攻击力
        DEF = 3,             // 防御力
        SPD = 4,             // 移动速度
        
        // ===== 高级战斗属性 =====
        CRIT_RATE = 10,      // 暴击率
        CRIT_DMG = 11,       // 暴击伤害倍率
        ACCURACY = 12,       // 命中率
        EVASION = 13,        // 闪避率
        BLOCK_RATE = 14,     // 格挡率
        BLOCK_VALUE = 15,    // 格挡值
        
        // ===== 抗性属性 =====
        PHYSICAL_RES = 20,   // 物理抗性
        MAGICAL_RES = 21,    // 魔法抗性
        
        // ===== 元素属性（可扩展）=====
        ELEMENT_FIRE = 30,   // 火元素强度
        ELEMENT_ICE = 31,    // 冰元素强度
        ELEMENT_LIGHTNING = 32, // 雷元素强度
        ELEMENT_DARK = 33,   // 暗元素强度
        
        // ===== 资源属性 =====
        MAX_MANA = 40,       // 法力值上限
        MANA_REGEN = 41,     // 法力恢复速度
        HEALTH_REGEN = 42,   // 生命恢复速度
        
        // ===== 其他可扩展属性 =====
        ATTACK_SPEED = 50,   // 攻击速度
        CAST_SPEED = 51,     // 施法速度
        COOLDOWN_REDUCTION = 52, // 冷却缩减
        LIFESTEAL = 53,      // 生命偷取
        EXP_GAIN = 54,       // 经验获取加成
    }
}

