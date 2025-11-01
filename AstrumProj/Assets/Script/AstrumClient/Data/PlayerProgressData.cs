using System.Collections.Generic;
using MemoryPack;

namespace Astrum.Client.Data
{
    /// <summary>
    /// 玩家进度数据（持久化）
    /// </summary>
    [MemoryPackable]
    public partial class PlayerProgressData
    {
        /// <summary>当前等级</summary>
        public int Level { get; set; } = 1;
        
        /// <summary>当前经验值</summary>
        public int Exp { get; set; } = 0;
        
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
        
        // 注意：ExpToNextLevel 不需要保存，由配置表计算得出
    }
}

