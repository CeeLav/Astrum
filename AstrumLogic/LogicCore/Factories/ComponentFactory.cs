using System;
using Astrum.LogicCore.Components;

namespace Astrum.LogicCore.Factories
{
    /// <summary>
    /// 组件工厂，负责创建各种组件
    /// </summary>
    public class ComponentFactory
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
        /// 创建位置组件
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="z">Z坐标</param>
        /// <returns>位置组件</returns>
        public PositionComponent CreatePositionComponent(float x = 0f, float y = 0f, float z = 0f)
        {
            return new PositionComponent(x, y, z);
        }

        /// <summary>
        /// 创建速度组件
        /// </summary>
        /// <param name="vx">X方向速度</param>
        /// <param name="vy">Y方向速度</param>
        /// <param name="vz">Z方向速度</param>
        /// <returns>速度组件</returns>
        public VelocityComponent CreateVelocityComponent(float vx = 0f, float vy = 0f, float vz = 0f)
        {
            return new VelocityComponent(vx, vy, vz);
        }

        /// <summary>
        /// 创建移动组件
        /// </summary>
        /// <param name="maxSpeed">最大速度</param>
        /// <param name="acceleration">加速度</param>
        /// <returns>移动组件</returns>
        public MovementComponent CreateMovementComponent(float maxSpeed = 5f, float acceleration = 10f)
        {
            return new MovementComponent(maxSpeed, acceleration);
        }

        /// <summary>
        /// 创建生命值组件
        /// </summary>
        /// <param name="maxHealth">最大生命值</param>
        /// <returns>生命值组件</returns>
        public HealthComponent CreateHealthComponent(int maxHealth = 100)
        {
            return new HealthComponent(maxHealth);
        }

        /// <summary>
        /// 创建输入组件
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>输入组件</returns>
        public LogicCore.FrameSync.LSInputComponent CreateInputComponent(int playerId)
        {
            return new LogicCore.FrameSync.LSInputComponent(playerId);
        }

        /// <summary>
        /// 批量创建组件
        /// </summary>
        /// <param name="componentTypes">组件类型数组</param>
        /// <returns>创建的组件列表</returns>
        public List<BaseComponent> CreateComponents(params Type[] componentTypes)
        {
            var components = new List<BaseComponent>();

            foreach (var type in componentTypes)
            {
                var component = CreateComponentFromType(type);
                if (component != null)
                {
                    components.Add(component);
                }
            }

            return components;
        }

        /// <summary>
        /// 创建默认玩家组件集合
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>玩家组件列表</returns>
        public List<BaseComponent> CreatePlayerComponents(int playerId)
        {
            return new List<BaseComponent>
            {
                CreatePositionComponent(),
                CreateVelocityComponent(),
                CreateMovementComponent(),
                CreateHealthComponent(),
                CreateInputComponent(playerId)
            };
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
