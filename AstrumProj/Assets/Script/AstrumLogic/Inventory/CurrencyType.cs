namespace Astrum.LogicCore.Inventory
{
    /// <summary>
    /// 货币类型枚举，可根据策划案扩展
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>主要游戏币</summary>
        Gold = 1,
        /// <summary>付费货币</summary>
        Diamond = 2,
        /// <summary>活动兑换券</summary>
        Voucher = 3,
    }
}
