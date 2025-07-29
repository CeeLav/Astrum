using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 能力抽象基类，所有能力都继承自此类
    /// </summary>
    public abstract class Capability
    {
        /// <summary>
        /// 能力的唯一标识符
        /// </summary>
        public int CapabilityId { get; set; }

        /// <summary>
        /// 拥有此能力的实体
        /// </summary>
        public Entity? OwnerEntity { get; set; }

        /// <summary>
        /// 能力是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 执行优先级
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 能力名称
        /// </summary>
        public virtual string Name => GetType().Name;

        protected Capability()
        {
            CapabilityId = GetHashCode();
        }

        /// <summary>
        /// 初始化能力
        /// </summary>
        public virtual void Initialize()
        {
            // 子类可以重写此方法进行初始化
        }

        /// <summary>
        /// 每帧更新（抽象方法，子类必须实现）
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public abstract void Tick(float deltaTime);

        /// <summary>
        /// 检查是否可以执行
        /// </summary>
        /// <returns>是否可以执行</returns>
        public virtual bool CanExecute()
        {
            return IsActive && OwnerEntity != null && OwnerEntity.IsActive && !OwnerEntity.IsDestroyed;
        }

        /// <summary>
        /// 激活时调用
        /// </summary>
        public virtual void OnActivate()
        {
            IsActive = true;
        }

        /// <summary>
        /// 停用时调用
        /// </summary>
        public virtual void OnDeactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// 设置优先级
        /// </summary>
        /// <param name="priority">优先级</param>
        public void SetPriority(int priority)
        {
            Priority = priority;
        }

        /// <summary>
        /// 获取拥有者实体的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，如果不存在返回null</returns>
        protected T? GetOwnerComponent<T>() where T : class
        {
            return OwnerEntity?.GetComponent<T>();
        }

        /// <summary>
        /// 检查拥有者实体是否有指定组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否拥有该组件</returns>
        protected bool OwnerHasComponent<T>() where T : Astrum.LogicCore.Components.BaseComponent
        {
            return OwnerEntity?.HasComponent<T>() ?? false;
        }
    }
}
