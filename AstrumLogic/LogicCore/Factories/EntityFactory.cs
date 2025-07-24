using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Factories
{
    /// <summary>
    /// 实体工厂，负责创建和销毁实体
    /// </summary>
    public class EntityFactory
    {
        /// <summary>
        /// 所属世界
        /// </summary>
        public World World { get; set; }

        /// <summary>
        /// 实体ID计数器
        /// </summary>
        private static long _nextEntityId = 1;

        public EntityFactory(World world)
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

            // 将实体添加到世界中
            World.Entities[entity.UniqueId] = entity;

            return entity;
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

            foreach (var componentType in componentTypes)
            {
                if (componentType.IsSubclassOf(typeof(LogicCore.Components.BaseComponent)))
                {
                    var component = Activator.CreateInstance(componentType) as LogicCore.Components.BaseComponent;
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
        /// 创建玩家实体
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="playerName">玩家名称</param>
        /// <returns>玩家实体</returns>
        public Entity CreatePlayerEntity(int playerId, string playerName = "")
        {
            var entity = CreateEntity(string.IsNullOrEmpty(playerName) ? $"Player_{playerId}" : playerName);

            // 添加玩家基础组件
            entity.AddComponent(new LogicCore.Components.PositionComponent(0, 0, 0));
            entity.AddComponent(new LogicCore.Components.VelocityComponent(0, 0, 0));
            entity.AddComponent(new LogicCore.Components.MovementComponent(5f, 10f)); // 默认速度和加速度
            entity.AddComponent(new LogicCore.Components.HealthComponent(100)); // 默认100血量
            entity.AddComponent(new LogicCore.FrameSync.LSInputComponent(playerId));

            // 添加移动能力
            entity.AddCapability(new LogicCore.Capabilities.MovementCapability());

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
