using System.Collections.Generic;
using Astrum.LogicCore.Physics;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 碰撞组件 - 负责管理实体在 BEPU 查询世界中的碰撞体
    /// </summary>
    [MemoryPackable]
    public partial class CollisionComponent : BaseComponent
    {
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
    }
}

