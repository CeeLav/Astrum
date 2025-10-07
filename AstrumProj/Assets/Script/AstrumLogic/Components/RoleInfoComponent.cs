using Astrum.LogicCore.Managers;
using cfg.Role;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 角色信息组件 - 存储角色配置信息
    /// </summary>
    [MemoryPackable]
    public partial class RoleInfoComponent : BaseComponent
    {
        /// <summary>
        /// 角色配置ID（直接使用EntityId）
        /// </summary>
        [MemoryPackIgnore]
        public int RoleId => (int)EntityId;
        
        /// <summary>
        /// 获取角色配置表数据
        /// </summary>
        [MemoryPackIgnore]
        public RoleBaseTable? RoleConfig => 
            RoleId > 0 ? ConfigManager.Instance.Tables.TbRoleBaseTable.Get(RoleId) : null;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        [MemoryPackConstructor]
        public RoleInfoComponent() : base() { }
    }
}

