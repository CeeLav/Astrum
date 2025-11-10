using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Inventory;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 背包组件 - 维护角色拥有的物品列表
    /// </summary>
    [MemoryPackable]
    public partial class InventoryComponent : BaseComponent
    {
        /// <summary>物品堆叠列表</summary>
        public List<ItemStack> Items { get; set; } = new();

        [MemoryPackConstructor]
        public InventoryComponent(List<ItemStack> items)
        {
            Items = items ?? new List<ItemStack>();
        }

        public InventoryComponent()
        {
        }

        /// <summary>获取指定物品总数量</summary>
        public int GetCount(int itemId)
        {
            return Items.Where(i => i.ItemId == itemId).Sum(i => i.Count);
        }

        /// <summary>添加物品（自动合并相同配置的堆叠）</summary>
        public void AddItem(int itemId, int count, Dictionary<string, string>? metadata = null)
        {
            if (count <= 0) return;

            // 仅当存在完全相同metadata时才合并
            var existing = Items.FirstOrDefault(i => i.ItemId == itemId && MetadataEquals(i.Metadata, metadata));
            if (existing != null)
            {
                existing.Count += count;
                return;
            }

            Items.Add(new ItemStack(itemId, count, metadata != null ? new Dictionary<string, string>(metadata) : null));
        }

        /// <summary>尝试消耗物品</summary>
        public bool TryConsumeItem(int itemId, int count)
        {
            if (count <= 0) return true;

            var total = GetCount(itemId);
            if (total < count)
            {
                return false;
            }

            var remaining = count;
            foreach (var stack in Items.Where(i => i.ItemId == itemId).ToList())
            {
                if (stack.Count > remaining)
                {
                    stack.Count -= remaining;
                    break;
                }

                remaining -= stack.Count;
                Items.Remove(stack);
                if (remaining <= 0)
                {
                    break;
                }
            }

            return true;
        }

        private static bool MetadataEquals(Dictionary<string, string>? lhs, Dictionary<string, string>? rhs)
        {
            if (ReferenceEquals(lhs, rhs)) return true;
            if (lhs == null || rhs == null) return false;
            if (lhs.Count != rhs.Count) return false;
            foreach (var pair in lhs)
            {
                if (!rhs.TryGetValue(pair.Key, out var value) || value != pair.Value)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
