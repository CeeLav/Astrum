using Astrum.LogicCore.Core;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 组件基类，所有组件都继承自此类
    /// </summary>
    [MemoryPackUnion(0, typeof(HealthComponent))]
    [MemoryPackUnion(1, typeof(PositionComponent))]
    [MemoryPackUnion(2, typeof(MovementComponent))]
    [MemoryPackUnion(3, typeof(Astrum.LogicCore.FrameSync.LSInputComponent))]
    [MemoryPackable]
    public abstract partial class BaseComponent
    {
        /// <summary>
        /// 组件的唯一标识符
        /// </summary>
        public int ComponentId { get; set; }

        /// <summary>
        /// 所属实体的ID
        /// </summary>
        public long EntityId { get; set; }

        protected BaseComponent()
        {
            ComponentId = GetHashCode();
        }
        
        public virtual void OnAttachToEntity(Entity entity)
        {
            
        }
    }
}
