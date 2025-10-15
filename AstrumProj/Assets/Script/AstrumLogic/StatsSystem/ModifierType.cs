namespace Astrum.LogicCore.Stats
{
    /// <summary>
    /// 修饰器类型
    /// </summary>
    public enum ModifierType
    {
        Flat = 1,           // 固定值加成（+50攻击）
        Percent = 2,        // 百分比加成（+20%攻击）
        FinalMultiplier = 3 // 最终乘数（×1.5伤害）
    }
}

