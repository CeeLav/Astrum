using Astrum.LogicCore.Core;
using Astrum.CommonBase;

namespace Astrum.LogicCore.Factories
{
    /// <summary>
    /// 实体工厂，负责创建和销毁实体
    /// </summary>
    public class EntityFactory : Singleton<EntityFactory>
    {
        /// <summary>
        /// 所属世界
        /// </summary>
        public World World { get; set; }

        /// <summary>
        /// 实体ID计数器
        /// </summary>
        private static long _nextEntityId = 1;

        public EntityFactory()
        {
            // 默认构造函数，World 需要在初始化后设置
        }

        /// <summary>
        /// 初始化工厂
        /// </summary>
        /// <param name="world">所属世界</param>
        public void Initialize(World world)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>
        /// 创建基础实体
        /// </summary>
        /// <param name="name">实体名称</param>
        /// <returns>创建的实体</returns>
        public Entity CreateEntity(string name)
        {
            var entity = new Entity
            {
                Name = name,
                CreationTime = DateTime.Now,
                IsActive = true,
                IsDestroyed = false
            };

            // 根据实体类型自动构建和挂载组件
            BuildComponentsForEntity(entity);

            // 将实体添加到世界中
            World.Entities[entity.UniqueId] = entity;

            return entity;
        }

        /// <summary>
        /// 创建指定类型的实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="name">实体名称</param>
        /// <returns>创建的实体</returns>
        public T CreateEntity<T>(string name) where T : Entity, new()
        {
            var entity = new T
            {
                Name = name,
                CreationTime = DateTime.Now,
                IsActive = true,
                IsDestroyed = false
            };

            // 根据实体类型自动构建和挂载组件
            BuildComponentsForEntity(entity);

            // 将实体添加到世界中
            World.Entities[entity.UniqueId] = entity;

            return entity;
        }

        /// <summary>
        /// 根据实体类型构建和挂载组件与能力
        /// </summary>
        /// <param name="entity">实体实例</param>
        private void BuildComponentsForEntity(Entity entity)
        {
            // 构建组件
            var componentTypes = entity.GetRequiredComponentTypes();
            var componentFactory = ComponentFactory.Instance;

            foreach (var componentType in componentTypes)
            {
                if (componentType.IsSubclassOf(typeof(LogicCore.Components.BaseComponent)))
                {
                    var component = componentFactory.CreateComponentFromType(componentType);
                    if (component != null)
                    {
                        entity.AddComponent(component);
                    }
                }
            }

            // 构建能力
            var capabilityTypes = entity.GetRequiredCapabilityTypes();

            foreach (var capabilityType in capabilityTypes)
            {
                if (capabilityType.IsSubclassOf(typeof(LogicCore.Capabilities.Capability)))
                {
                    try
                    {
                        var capability = Activator.CreateInstance(capabilityType) as LogicCore.Capabilities.Capability;
                        if (capability != null)
                        {
                            entity.AddCapability(capability);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"创建能力 {capabilityType.Name} 失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 创建带有指定组件的实体
        /// </summary>
        /// <param name="name">实体名称</param>
        /// <param name="componentTypes">组件类型列表</param>
        /// <returns>创建的实体</returns>
        public Entity CreateEntityWithComponents(string name, params Type[] componentTypes)
        {
            var entity = CreateEntity(name);

            var componentFactory = ComponentFactory.Instance;

            foreach (var componentType in componentTypes)
            {
                if (componentType.IsSubclassOf(typeof(LogicCore.Components.BaseComponent)))
                {
                    var component = componentFactory.CreateComponentFromType(componentType);
                    if (component != null)
                    {
                        component.EntityId = entity.UniqueId;
                        entity.Components.Add(component);
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        /// <param name="entity">要销毁的实体</param>
        public void DestroyEntity(Entity entity)
        {
            if (entity == null) return;

            // 从世界中移除
            World.Entities.Remove(entity.UniqueId);

            // 处理父子关系
            if (entity.ParentId != -1)
            {
                var parent = World.GetEntity(entity.ParentId);
                parent?.RemoveChild(entity.UniqueId);
            }

            // 销毁所有子实体
            var childrenToDestroy = new List<long>(entity.ChildrenIds);
            foreach (var childId in childrenToDestroy)
            {
                var child = World.GetEntity(childId);
                if (child != null)
                {
                    DestroyEntity(child);
                }
            }

            // 销毁实体
            entity.Destroy();
        }

        /// <summary>
        /// 根据ID销毁实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        public void DestroyEntity(long entityId)
        {
            var entity = World.GetEntity(entityId);
            if (entity != null)
            {
                DestroyEntity(entity);
            }
        }

        /// <summary>
        /// 批量销毁实体
        /// </summary>
        /// <param name="entityIds">实体ID列表</param>
        public void DestroyEntities(IEnumerable<long> entityIds)
        {
            foreach (var entityId in entityIds)
            {
                DestroyEntity(entityId);
            }
        }

        /// <summary>
        /// 获取下一个实体ID
        /// </summary>
        /// <returns>实体ID</returns>
        public static long GetNextEntityId()
        {
            return _nextEntityId++;
        }

        /// <summary>
        /// 重置实体ID计数器
        /// </summary>
        public static void ResetEntityIdCounter()
        {
            _nextEntityId = 1;
        }
    }
}
