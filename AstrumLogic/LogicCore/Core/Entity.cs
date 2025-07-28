using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 实体类，游戏中所有对象的基础类
    /// </summary>
    public class Entity
    {
        private static long _nextId = 1;

        /// <summary>
        /// 获取此实体类型需要的组件类型列表
        /// 子类可以重写此方法来定义自己需要的组件
        /// </summary>
        /// <returns>组件类型列表</returns>
        public virtual Type[] GetRequiredComponentTypes()
        {
            return Array.Empty<Type>();
        }

        /// <summary>
        /// 获取此实体类型需要的默认能力类型列表
        /// 子类可以重写此方法来定义自己需要的默认能力
        /// </summary>
        /// <returns>能力类型列表</returns>
        public virtual Type[] GetRequiredCapabilityTypes()
        {
            return Array.Empty<Type>();
        }

        /// <summary>
        /// 实体的全局唯一标识符
        /// </summary>
        public long UniqueId { get; private set; }

        /// <summary>
        /// 实体名称，便于调试和识别
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 实体是否激活，控制逻辑执行
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 实体是否已销毁，用于生命周期管理
        /// </summary>
        public bool IsDestroyed { get; set; } = false;

        /// <summary>
        /// 实体创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 组件掩码，用于快速查询和筛选
        /// </summary>
        public long ComponentMask { get; private set; } = 0;

        /// <summary>
        /// 挂载的组件列表
        /// </summary>
        public List<BaseComponent> Components { get; private set; } = new List<BaseComponent>();

        /// <summary>
        /// 父实体ID，-1表示无父实体
        /// </summary>
        public long ParentId { get; set; } = -1;

        /// <summary>
        /// 子实体ID列表
        /// </summary>
        public List<long> ChildrenIds { get; private set; } = new List<long>();

        /// <summary>
        /// 实体具备的能力列表
        /// </summary>
        public List<Capability> Capabilities { get; private set; } = new List<Capability>();

        public Entity()
        {
            UniqueId = _nextId++;
            CreationTime = DateTime.Now;
        }

        /// <summary>
        /// 添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件实例</param>
        public void AddComponent<T>(T component) where T : BaseComponent
        {
            if (component == null) return;

            component.EntityId = UniqueId;
            Components.Add(component);
            
            // 更新组件掩码
            UpdateComponentMask();
        }

        /// <summary>
        /// 移除组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void RemoveComponent<T>() where T : BaseComponent
        {
            var component = Components.FirstOrDefault(c => c is T);
            if (component != null)
            {
                Components.Remove(component);
                UpdateComponentMask();
            }
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，如果不存在返回null</returns>
        public T? GetComponent<T>() where T : class
        {
            return Components.FirstOrDefault(c => c is T) as T;
        }

        /// <summary>
        /// 检查是否有指定组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否拥有该组件</returns>
        public bool HasComponent<T>() where T : BaseComponent
        {
            return Components.Any(c => c is T);
        }

        /// <summary>
        /// 应用输入到实体
        /// </summary>
        /// <param name="input">输入数据</param>
        public void ApplyInput(LSInput input)
        {
            var inputComponent = GetComponent<LSInputComponent>();
            inputComponent?.SetInput(input);
        }

        /// <summary>
        /// 设置激活状态
        /// </summary>
        /// <param name="active">是否激活</param>
        public void SetActive(bool active)
        {
            IsActive = active;
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public void Destroy()
        {
            IsDestroyed = true;
            IsActive = false;
            
            // 清理组件
            Components.Clear();
            
            // 清理能力
            foreach (var capability in Capabilities)
            {
                capability.OnDeactivate();
            }
            Capabilities.Clear();
        }

        /// <summary>
        /// 获取所有子实体
        /// </summary>
        /// <returns>子实体ID列表</returns>
        public List<long> GetChildren()
        {
            return new List<long>(ChildrenIds);
        }

        /// <summary>
        /// 设置父实体
        /// </summary>
        /// <param name="parentId">父实体ID</param>
        public void SetParent(long parentId)
        {
            ParentId = parentId;
        }

        /// <summary>
        /// 添加子实体
        /// </summary>
        /// <param name="childId">子实体ID</param>
        public void AddChild(long childId)
        {
            if (!ChildrenIds.Contains(childId))
            {
                ChildrenIds.Add(childId);
            }
        }

        /// <summary>
        /// 移除子实体
        /// </summary>
        /// <param name="childId">子实体ID</param>
        public void RemoveChild(long childId)
        {
            ChildrenIds.Remove(childId);
        }

        /// <summary>
        /// 添加能力
        /// </summary>
        /// <param name="capability">能力实例</param>
        public void AddCapability(Capability capability)
        {
            if (capability != null)
            {
                capability.OwnerEntity = this;
                Capabilities.Add(capability);
                capability.Initialize();
            }
        }

        /// <summary>
        /// 移除能力
        /// </summary>
        /// <param name="capability">能力实例</param>
        public void RemoveCapability(Capability capability)
        {
            if (Capabilities.Remove(capability))
            {
                capability.OnDeactivate();
            }
        }

        /// <summary>
        /// 更新组件掩码
        /// </summary>
        private void UpdateComponentMask()
        {
            ComponentMask = 0;
            foreach (var component in Components)
            {
                // 这里可以根据组件类型设置相应的位标志
                ComponentMask |= (1L << component.ComponentId);
            }
        }
    }
}
