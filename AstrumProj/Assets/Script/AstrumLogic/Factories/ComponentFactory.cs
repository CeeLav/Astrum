using System;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;

namespace Astrum.LogicCore.Factories
{
    /// <summary>
    /// 组件工厂，负责创建各种组件
    /// </summary>
    public class ComponentFactory : Singleton<ComponentFactory>
    {
        /// <summary>
        /// 创建指定类型的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>创建的组件实例</returns>
        public T CreateComponent<T>() where T : BaseComponent, new()
        {
            return new T();
        }
        

        /// <summary>
        /// 根据类型创建组件
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <returns>创建的组件实例</returns>
        public BaseComponent? CreateComponentFromType(Type type)
        {
            if (type == null || !type.IsSubclassOf(typeof(BaseComponent)))
                return null;

            try
            {
                return Activator.CreateInstance(type) as BaseComponent;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                Console.WriteLine($"Failed to create component of type {type.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 克隆组件
        /// </summary>
        /// <param name="original">原始组件</param>
        /// <returns>克隆的组件</returns>
        public T? CloneComponent<T>(T original) where T : BaseComponent
        {
            if (original == null) return null;

            var clone = CreateComponentFromType(original.GetType()) as T;
            if (clone != null)
            {
                // 复制基础属性
                clone.EntityId = original.EntityId;
                
                // 这里可以添加更多属性的复制逻辑
                // 由于组件可能有不同的属性，可以考虑使用反射或者让组件实现ICloneable接口
            }

            return clone;
        }
    }
}
