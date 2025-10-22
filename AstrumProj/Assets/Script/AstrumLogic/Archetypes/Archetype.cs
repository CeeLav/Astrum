using System;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Archetypes
{
    /// <summary>
    /// 逻辑原型基类：声明本体"增量"的组件与能力集合。
    /// 提供实体生命周期钩子方法，子类可以重写以实现自定义逻辑。
    /// </summary>
    public abstract class Archetype
    {
        public virtual Type[] Components => Array.Empty<Type>();
        public virtual Type[] Capabilities => Array.Empty<Type>();

        /// <summary>
        /// 实体创建后，组件挂载前调用
        /// </summary>
        /// <param name="entity">创建的实体</param>
        /// <param name="world">所属世界</param>
        public virtual void OnBeforeComponentsAttach(Entity entity, World world)
        {
            // 默认不执行任何操作
        }

        /// <summary>
        /// 组件挂载后，能力挂载前调用
        /// </summary>
        /// <param name="entity">创建的实体</param>
        /// <param name="world">所属世界</param>
        public virtual void OnAfterComponentsAttach(Entity entity, World world)
        {
            // 默认不执行任何操作
        }

        /// <summary>
        /// 组件挂载后，能力挂载前调用
        /// </summary>
        /// <param name="entity">创建的实体</param>
        /// <param name="world">所属世界</param>
        public virtual void OnBeforeCapabilitiesAttach(Entity entity, World world)
        {
            // 默认不执行任何操作
        }

        /// <summary>
        /// 能力挂载后，实体完全创建完成时调用
        /// </summary>
        /// <param name="entity">创建的实体</param>
        /// <param name="world">所属世界</param>
        public virtual void OnAfterCapabilitiesAttach(Entity entity, World world)
        {
            // 默认不执行任何操作
        }

        /// <summary>
        /// 实体销毁时调用
        /// </summary>
        /// <param name="entity">被销毁的实体</param>
        /// <param name="world">所属世界</param>
        public virtual void OnEntityDestroy(Entity entity, World world)
        {
            // 默认不执行任何操作
        }
    }
}


