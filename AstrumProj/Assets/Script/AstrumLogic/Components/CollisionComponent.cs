using System.Collections.Generic;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
using Astrum.LogicCore.Capabilities;
using MemoryPack;
using TrueSync;
// 使用别名避免与 BEPU 的 Entity 类冲突
using AstrumEntity = Astrum.LogicCore.Core.Entity;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 碰撞组件 - 负责管理实体在 BEPU 查询世界中的碰撞体
    /// </summary>
    [MemoryPackable]
    public partial class CollisionComponent : BaseComponent
    {
        /// <summary>
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<CollisionComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
        /// <summary>碰撞形状列表（可多个）</summary>
        public List<CollisionShape> Shapes { get; set; } = new();
        
        /// <summary>碰撞层（位域）</summary>
        public int CollisionLayer { get; set; } = 1;
        
        /// <summary>碰撞掩码（与哪些层交互）</summary>
        public int CollisionMask { get; set; } = 255;
        
        /// <summary>是否触发器（仅检测不阻挡）</summary>
        public bool IsTrigger { get; set; } = false;
        
        /// <summary>
        /// BEPU 中的刚体句柄（运行时，暂不实现）
        /// </summary>
        [MemoryPackIgnore]
        public object BodyHandle { get; set; }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CollisionComponent()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public CollisionComponent(long entityId, List<CollisionShape> shapes, 
            int collisionLayer, int collisionMask, bool isTrigger)
        {
            EntityId = entityId;
            Shapes = shapes ?? new List<CollisionShape>();
            CollisionLayer = collisionLayer;
            CollisionMask = collisionMask;
            IsTrigger = isTrigger;
        }

        /// <summary>
        /// 当组件附加到实体时调用
        /// 自动从配置表加载碰撞形状数据
        /// </summary>
        public override void OnAttachToEntity(Entity entity)
        {
            base.OnAttachToEntity(entity);
            
            // 如果 Shapes 已经有数据（例如反序列化后），则不再加载
            if (Shapes != null && Shapes.Count > 0)
                return;
            
            // 从实体配置获取模型ID
            var entityConfig = entity.EntityConfig;
            if (entityConfig == null)
            {
                ASLogger.Instance.Warning($"CollisionComponent: Entity {entity.UniqueId} has no EntityConfig");
                return;
            }
            
            // 获取模型配置
            var modelConfig = TableConfig.Instance.Tables.TbBaseUnitModelTable.Get(entityConfig.ModelId);
            if (modelConfig == null)
            {
                ASLogger.Instance.Warning($"CollisionComponent: ModelId {entityConfig.ModelId} not found in BaseUnitModelTable");
                return;
            }
            
            // 解析碰撞数据
            if (!string.IsNullOrEmpty(modelConfig.CollisionData))
            {
                Shapes = CollisionDataParser.Parse(modelConfig.CollisionData);
                ASLogger.Instance.Debug($"CollisionComponent: Loaded {Shapes.Count} collision shapes for Entity {entity.UniqueId} (Model {entityConfig.ModelId})");
            }
            else
            {
                ASLogger.Instance.Debug($"CollisionComponent: No collision data for Entity {entity.UniqueId} (Model {entityConfig.ModelId})");
            }
            
            // 【物理世界注册】注册实体到 HitSystem
            if (entity is AstrumEntity astrumEntity && Shapes != null && Shapes.Count > 0 && entity.World != null)
            {
                entity.World.HitSystem?.RegisterEntity(astrumEntity);
                // 【关键修复】注册后立即同步位置，确保物理世界中的位置与逻辑层一致
                entity.World.HitSystem?.UpdateEntityPosition(astrumEntity);
                var pos = entity.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
                ASLogger.Instance.Info($"[CollisionComponent] Registered entity {entity.UniqueId} to HitSystem at Pos=({(float)pos.x:F2},{(float)pos.y:F2},{(float)pos.z:F2})");
            }
        }
        
        /// <summary>
        /// 重置 CollisionComponent 状态（用于对象池回收）
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Shapes.Clear();
            CollisionLayer = 1;
            CollisionMask = 255;
            IsTrigger = false;
            BodyHandle = null;
        }
    }
}

