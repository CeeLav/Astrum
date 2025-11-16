using System.Collections.Generic;
using Astrum.LogicCore.Inventory;
using MemoryPack;

namespace Astrum.LogicCore.Data
{
    /// <summary>
    /// 玩家进度数据（持久化）
    /// 服务器端和客户端共享的数据结构
    /// </summary>
    [MemoryPackable]
    public partial class PlayerProgressData
    {
        /// <summary>当前等级</summary>
        public int Level { get; set; } = 1;
        
        /// <summary>当前经验值</summary>
        public int Exp { get; set; } = 0;
        
        /// <summary>升级所需经验</summary>
        public int ExpToNextLevel { get; set; } = 1000;
        
        /// <summary>最大等级</summary>
        public int MaxLevel { get; set; } = 100;
        
        /// <summary>角色ID</summary>
        public int RoleId { get; set; } = 1001;
        
        /// <summary>可分配的属性点</summary>
        public int AvailableStatPoints { get; set; } = 0;
        
        /// <summary>已分配的攻击点</summary>
        public int AllocatedAttackPoints { get; set; } = 0;
        
        /// <summary>已分配的防御点</summary>
        public int AllocatedDefensePoints { get; set; } = 0;
        
        /// <summary>已分配的生命点</summary>
        public int AllocatedHealthPoints { get; set; } = 0;
        
        /// <summary>已分配的速度点</summary>
        public int AllocatedSpeedPoints { get; set; } = 0;
        
        /// <summary>星能碎片（资源）</summary>
        public int StarFragments { get; set; } = 0;
        
        /// <summary>其他资源（可扩展）</summary>
        public Dictionary<string, int> Resources { get; set; } = new();
        
        /// <summary>货币余额</summary>
        public Dictionary<CurrencyType, long> Currencies { get; set; } = new();
        
        /// <summary>背包物品列表</summary>
        public List<ItemStack> Inventory { get; set; } = new();
        
        // 注意：战斗中临时数据（生命、护盾等）不需要保存
    }
}

