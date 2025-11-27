using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 组件基类，所有组件都继承自此类
    /// </summary>
    public abstract partial class BaseComponent
    {
        /// <summary>
        /// 组件的唯一标识符
        /// </summary>
        public static int ComponentId { get; set; }

        /// <summary>
        /// 所属实体的ID
        /// </summary>
        public long EntityId { get; set; }

        protected BaseComponent()
        {
            ComponentId = GetHashCode();
        }

        public int GetComponentId()
        {
            return ComponentId;
        }
        
        public virtual void OnAttachToEntity(Entity entity)
        {
            
        }

        /// <summary>
        /// 重写 ToString 方法，避免循环引用
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"{GetType().Name}[Id={ComponentId}, EntityId={EntityId}]";
        }
    }
}
