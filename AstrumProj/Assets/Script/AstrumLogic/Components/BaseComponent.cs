using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 组件基类，所有组件都继承自此类
    /// </summary>
    public abstract partial class BaseComponent
    {
        /// <summary>
        /// 所属实体的ID
        /// </summary>
        public long EntityId { get; set; }

        protected BaseComponent()
        {
        }

        /// <summary>
        /// 获取组件的类型 ID（子类需要重写此方法）
        /// </summary>
        /// <returns>组件的类型 ID</returns>
        public abstract int GetComponentTypeId();
        
        /// <summary>
        /// 获取组件的实例 ID（兼容旧代码，返回类型 ID）
        /// </summary>
        /// <returns>组件的 ID</returns>
        public int GetComponentId()
        {
            return GetComponentTypeId();
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
            return $"{GetType().Name}[TypeId={GetComponentTypeId()}, EntityId={EntityId}]";
        }
    }
}
