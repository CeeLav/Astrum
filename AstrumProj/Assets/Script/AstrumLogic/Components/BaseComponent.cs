using Astrum.LogicCore.Core;
using Astrum.CommonBase;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 组件基类，所有组件都继承自此类
    /// </summary>
    public abstract partial class BaseComponent : IPool
    {
        /// <summary>
        /// 所属实体的ID
        /// </summary>
        public long EntityId { get; set; }
        
        /// <summary>
        /// 对象是否来自对象池（IPool 接口实现）
        /// </summary>
        public bool IsFromPool { get; set; }

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
        /// 重置 Component 状态（用于对象池回收）
        /// </summary>
        public virtual void Reset()
        {
            // 重置基础字段
            EntityId = 0;
            // 子类需要重写此方法以重置自己的字段
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
