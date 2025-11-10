using System.Collections.Generic;
using MemoryPack;

namespace Astrum.LogicCore.Inventory
{
    /// <summary>
    /// 背包物品堆叠信息
    /// </summary>
    [MemoryPackable]
    public partial class ItemStack
    {
        /// <summary>物品配置ID</summary>
        public int ItemId { get; set; }

        /// <summary>数量（>=1）</summary>
        public int Count { get; set; }

        /// <summary>附加元数据，如随机属性、耐久等（可选）</summary>
        public Dictionary<string, string>? Metadata { get; set; }

        public ItemStack()
        {
        }

        [MemoryPackConstructor]
        public ItemStack(int itemId, int count, Dictionary<string, string>? metadata)
        {
            ItemId = itemId;
            Count = count;
            Metadata = metadata;
        }
    }
}
