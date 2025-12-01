using System.Collections.Generic;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Inventory;
using Astrum.LogicCore.Capabilities;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 货币组件 - 维护角色的各类货币余额
    /// </summary>
    [MemoryPackable]
    public partial class CurrencyComponent : BaseComponent
    {
        /// <summary>
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<CurrencyComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
        /// <summary>货币余额表</summary>
        public Dictionary<CurrencyType, long> Balances { get; set; } = new();

        [MemoryPackConstructor]
        public CurrencyComponent(Dictionary<CurrencyType, long> balances)
        {
            Balances = balances ?? new Dictionary<CurrencyType, long>();
        }

        public CurrencyComponent()
        {
        }

        /// <summary>获取指定货币余额</summary>
        public long GetBalance(CurrencyType type)
        {
            return Balances.TryGetValue(type, out var value) ? value : 0;
        }

        /// <summary>设置货币余额（可用于读档恢复）</summary>
        public void SetBalance(CurrencyType type, long amount)
        {
            Balances[type] = amount < 0 ? 0 : amount;
        }

        /// <summary>增加货币</summary>
        public void Add(CurrencyType type, long delta)
        {
            if (delta <= 0) return;
            Balances[type] = GetBalance(type) + delta;
        }

        /// <summary>尝试消耗货币</summary>
        public bool TryConsume(CurrencyType type, long cost)
        {
            if (cost <= 0) return true;
            var current = GetBalance(type);
            if (current < cost) return false;
            Balances[type] = current - cost;
            return true;
        }
        
        /// <summary>
        /// 重置 CurrencyComponent 状态（用于对象池回收）
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Balances.Clear();
        }
    }
}
