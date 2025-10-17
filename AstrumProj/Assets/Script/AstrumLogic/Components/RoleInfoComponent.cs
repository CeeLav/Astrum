using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
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
        public int RoleId { get; set; }
        
        /// <summary>
        /// 获取角色配置表数据（健壮性增强：拿不到时记录错误）
        /// </summary>
        [MemoryPackIgnore]
        public RoleBaseTable? RoleConfig
        {
            get
            {
                if (RoleId <= 0)
                {
                    ASLogger.Instance.Error($"RoleInfoComponent.RoleConfig: Invalid RoleId for entity {EntityId}");
                    return null;
                }

                var cfg = TableConfig.Instance.Tables.TbRoleBaseTable.Get(RoleId);
                if (cfg == null)
                {
                    ASLogger.Instance.Error($"RoleInfoComponent.RoleConfig: RoleBaseTable not found for RoleId={RoleId}");
                }
                return cfg;
            }
        }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        [MemoryPackConstructor]
        public RoleInfoComponent() : base() { }

        public override void OnAttachToEntity(Entity entity)
        {
            base.OnAttachToEntity(entity);
            RoleId = (int)entity.EntityConfigId;
        }
    }
}

