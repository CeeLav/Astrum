using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using cfg.Entity;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 怪物信息组件 - 存储怪物配置信息
    /// </summary>
    [MemoryPackable]
    public partial class MonsterInfoComponent : BaseComponent
    {
        /// <summary>
        /// 怪物配置ID（直接使用EntityId）
        /// </summary>
        [MemoryPackIgnore]
        public int MonsterId { get; set; }
        
        /// <summary>
        /// 怪物AI类型
        /// </summary>
        public int AIType { get; set; }
        
        /// <summary>
        /// 怪物等级
        /// </summary>
        public int Level { get; set; }
        
        /// <summary>
        /// 获取怪物配置表数据（健壮性增强：拿不到时记录错误）
        /// </summary>
        [MemoryPackIgnore]
        public EntityBaseTable? MonsterConfig
        {
            get
            {
                if (MonsterId <= 0)
                {
                    ASLogger.Instance.Error($"MonsterInfoComponent.MonsterConfig: Invalid MonsterId for entity {EntityId}");
                    return null;
                }

                var cfg = TableConfig.Instance.Tables.TbEntityBaseTable.Get(MonsterId);
                if (cfg == null)
                {
                    ASLogger.Instance.Error($"MonsterInfoComponent.MonsterConfig: EntityBaseTable not found for MonsterId={MonsterId}");
                }
                return cfg;
            }
        }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        [MemoryPackConstructor]
        public MonsterInfoComponent() : base() 
        { 
            AIType = 0;
            Level = 1;
        }

        public override void OnAttachToEntity(Entity entity)
        {
            base.OnAttachToEntity(entity);
            MonsterId = (int)entity.EntityConfigId;
            
            // 从配置表读取怪物等级（如果配置表有这个字段的话）
            // Level = MonsterConfig?.Level ?? 1;
        }
    }
}


