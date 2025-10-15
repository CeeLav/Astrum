namespace Astrum.LogicCore.Stats
{
    /// <summary>
    /// 动态资源类型
    /// </summary>
    public enum DynamicResourceType
    {
        // ===== 核心资源 =====
        CURRENT_HP = 1,      // 当前生命值
        CURRENT_MANA = 2,    // 当前法力值
        
        // ===== 战斗资源 =====
        ENERGY = 10,         // 能量（0-100）
        RAGE = 11,           // 怒气（0-100）
        COMBO = 12,          // 连击数
        
        // ===== 临时防护 =====
        SHIELD = 20,         // 护盾值

    }
}

