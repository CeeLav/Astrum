using System;
using System.Collections.Generic;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Archetypes;

namespace Astrum.LogicCore.Factories
{
    /// <summary>
    /// 实体工厂，负责创建和销毁实体
    /// </summary>
    public class EntityFactory : Singleton<EntityFactory>
    {

        /// <summary>
        /// 实体ID计数器
        /// </summary>
        private static long _nextEntityId = 1;

        public EntityFactory()
        {
            // 默认构造函数
        }

        /// <summary>
        /// 通过 Archetype 创建实体（最小外观，后续接入组件/能力并集装配）。
        /// </summary>
        /// <param name="archetypeName">原型名，例如 "Role"</param>
        /// <param name="entityConfigId">实体配置ID（用于命名/资源）</param>
        /// <param name="world">所属世界</param>
        /// <returns>创建的实体</returns>
        public Entity CreateByArchetype(string archetypeName, int entityConfigId, World world)
        {
            if (!ArchetypeManager.Instance.TryGet(archetypeName, out var info))
            {
                throw new Exception($"Archetype '{archetypeName}' not found");
            }

            var entity = new Entity
            {
                Name = GetEntityNameFromConfig(entityConfigId),
                EntityConfigId = entityConfigId,
                CreationTime = DateTime.Now,
                IsActive = true,
                IsDestroyed = false
            };

            // 按 ArchetypeInfo 装配组件
            var componentFactory = ComponentFactory.Instance;
            foreach (var compType in info.Components ?? Array.Empty<Type>())
            {
                if (compType == null) continue;
                var component = componentFactory.CreateComponentFromType(compType);
                if (component != null)
                {
                    entity.AddComponent(component);
                    component.OnAttachToEntity(entity);
                }
            }

            // 按 ArchetypeInfo 装配能力
            foreach (var capType in info.Capabilities ?? Array.Empty<Type>())
            {
                if (capType == null) continue;
                try
                {
                    var capability = Activator.CreateInstance(capType) as LogicCore.Capabilities.Capability;
                    if (capability != null)
                    {
                        entity.AddCapability(capability);
                    }
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"创建能力 {capType.Name} 失败: {ex.Message}");
                    ASLogger.Instance.LogException(ex);
                }
            }

            world.Entities[entity.UniqueId] = entity;
            return entity;
        }
        
        public T CreateByArchetype<T>(string archetypeName, int entityConfigId, World world)  where T : Entity, new()
        {
            if (!ArchetypeManager.Instance.TryGet(archetypeName, out var info))
            {
                throw new Exception($"Archetype '{archetypeName}' not found");
            }

            var entity = new T
            {
                Name = GetEntityNameFromConfig(entityConfigId),
                EntityConfigId = entityConfigId,
                CreationTime = DateTime.Now,
                IsActive = true,
                IsDestroyed = false
            };

            // 按 ArchetypeInfo 装配组件
            var componentFactory = ComponentFactory.Instance;
            foreach (var compType in info.Components ?? Array.Empty<Type>())
            {
                if (compType == null) continue;
                var component = componentFactory.CreateComponentFromType(compType);
                if (component != null)
                {
                    entity.AddComponent(component);
                    component.OnAttachToEntity(entity);
                }
            }

            // 按 ArchetypeInfo 装配能力
            foreach (var capType in info.Capabilities ?? Array.Empty<Type>())
            {
                if (capType == null) continue;
                try
                {
                    var capability = Activator.CreateInstance(capType) as LogicCore.Capabilities.Capability;
                    if (capability != null)
                    {
                        entity.AddCapability(capability);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建能力 {capType.Name} 失败: {ex.Message}");
                }
            }

            world.Entities[entity.UniqueId] = entity;
            return entity;
        }

        /// <summary>
        /// 从配置表行创建（表需提供 ArchetypeName 列）- 推荐入口
        /// </summary>
        /// <param name="entityConfigId">实体配置ID</param>
        /// <param name="world">所属世界</param>
        /// <returns>创建的实体</returns>
        public Entity CreateEntity(int entityConfigId, World world)
        {
            var tb = ConfigManager.Instance.Tables.TbEntityBaseTable.Get(entityConfigId);
            var archetypeName = tb != null ? tb.ArchetypeName : string.Empty;
            return !string.IsNullOrEmpty(archetypeName)
                ? CreateByArchetype(archetypeName, entityConfigId, world)
                : null;
        }

        /// <summary>
        /// 创建指定类型的实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entityConfigId">实体配置ID</param>
        /// <param name="world">所属世界</param>
        /// <returns>创建的实体</returns>
        public T CreateEntity<T>(int entityConfigId, World world) where T : Entity, new()
        {
            var tb = ConfigManager.Instance.Tables.TbEntityBaseTable.Get(entityConfigId);
            var archetypeName = tb != null ? tb.ArchetypeName : string.Empty;
            return !string.IsNullOrEmpty(archetypeName)
                ? CreateByArchetype<T>(archetypeName, entityConfigId, world)
                : null;
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
                        component.OnAttachToEntity(entity);
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
        /// 销毁实体
        /// </summary>
        /// <param name="entity">要销毁的实体</param>
        /// <param name="world">所属世界</param>
        public void DestroyEntity(Entity entity, World world)
        {
            if (entity == null) return;

            // 从世界中移除
            world.Entities.Remove(entity.UniqueId);

            // 处理父子关系
            if (entity.ParentId != -1)
            {
                var parent = world.GetEntity(entity.ParentId);
                parent?.RemoveChild(entity.UniqueId);
            }

            // 销毁所有子实体
            var childrenToDestroy = new List<long>(entity.ChildrenIds);
            foreach (var childId in childrenToDestroy)
            {
                var child = world.GetEntity(childId);
                if (child != null)
                {
                    DestroyEntity(child, world);
                }
            }

            // 销毁实体
            entity.Destroy();
        }

        /// <summary>
        /// 根据ID销毁实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="world">所属世界</param>
        public void DestroyEntity(long entityId, World world)
        {
            var entity = world.GetEntity(entityId);
            if (entity != null)
            {
                DestroyEntity(entity, world);
            }
        }

        /// <summary>
        /// 批量销毁实体
        /// </summary>
        /// <param name="entityIds">实体ID列表</param>
        /// <param name="world">所属世界</param>
        public void DestroyEntities(IEnumerable<long> entityIds, World world)
        {
            foreach (var entityId in entityIds)
            {
                DestroyEntity(entityId, world);
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

        /// <summary>
        /// 从配置中获取实体名称
        /// </summary>
        /// <param name="entityConfigId">实体配置ID</param>
        /// <returns>实体名称</returns>
        private string GetEntityNameFromConfig(int entityConfigId)
        {
            if (entityConfigId == 0)
            {
                return "UnknownEntity";
            }

            try
            {
                var entityConfig = ConfigManager.Instance.Tables.TbEntityBaseTable.Get(entityConfigId);
                return entityConfig?.EntityName ?? $"Entity_{entityConfigId}";
            }
            catch
            {
                return $"Entity_{entityConfigId}";
            }
        }
    }
}
