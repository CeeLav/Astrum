using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Archetypes;
using Astrum.LogicCore.Components;
using cfg;

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
        /// <param name="archetype">原型</param>
        /// <param name="entityConfigId">实体配置ID（用于命名/资源）</param>
        /// <param name="world">所属世界</param>
        /// <returns>创建的实体</returns>
        public Entity CreateByArchetype(EArchetype archetype, int entityConfigId, World world)
        {
            return CreateByArchetype(archetype, new EntityCreationParams { EntityConfigId = entityConfigId }, world);
        }

        public Entity CreateByArchetype(EArchetype archetype, EntityCreationParams creationParams, World world)
        {
            return CreateByArchetype<Entity>(archetype, creationParams, world);
        }

        public T CreateByArchetype<T>(EArchetype archetype, int entityConfigId, World world) where T : Entity, new()
        {
            return CreateByArchetype<T>(archetype, new EntityCreationParams { EntityConfigId = entityConfigId }, world);
        }

        public T CreateByArchetype<T>(EArchetype archetype, EntityCreationParams creationParams, World world) where T : Entity, new()
        {
            if (!ArchetypeRegistry.Instance.TryGet(archetype, out var info))
            {
                throw new Exception($"Archetype '{archetype}' not found");
            }

            creationParams ??= new EntityCreationParams();
            var entityConfigId = creationParams.EntityConfigId ?? 0;

            // 从对象池获取 Entity
            var entity = ObjectPool.Instance.Fetch<T>();
            entity.Reset(); // 重置状态，确保从对象池获取的对象是干净的
            
            // 设置基本属性
            entity.Name = entityConfigId > 0 ? GetEntityNameFromConfig(entityConfigId) : archetype.ToString();
            entity.Archetype = archetype;
            entity.EntityConfigId = entityConfigId;
            entity.CreationTime = DateTime.Now;
            entity.IsDestroyed = false;
            entity.UniqueId = Entity.GetNextId(); // 设置新的唯一ID
            
            // 确保组件 OnAttachToEntity 内可访问 World
            entity.World = world;
            
            // 【生命周期钩子】组件挂载前
            CallArchetypeLifecycleMethod(archetype, "OnBeforeComponentsAttach", entity, world);

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

            // 【生命周期钩子】组件挂载后
            CallArchetypeLifecycleMethod(archetype, "OnAfterComponentsAttach", entity, world);

            // 【生命周期钩子】能力挂载前
            CallArchetypeLifecycleMethod(archetype, "OnBeforeCapabilitiesAttach", entity, world);

            // 按 ArchetypeInfo 装配能力（新方式：使用 CapabilitySystem）
            foreach (var capType in info.Capabilities ?? Array.Empty<Type>())
            {
                if (capType == null) continue;
                
                // 检查是否是新的 ICapability 接口
                if (typeof(LogicCore.Capabilities.ICapability).IsAssignableFrom(capType))
                {
                    // 新方式：使用 CapabilitySystem 注册
                    try
                    {
                        var capability = LogicCore.Systems.CapabilitySystem.GetCapability(capType);
                        if (capability == null)
                        {
                            ASLogger.Instance.Warning($"Capability {capType.Name} not registered in CapabilitySystem");
                            continue;
                        }
                        
                        // 启用此 Capability（使用 TypeId 作为 Key，存在即表示拥有）
                        entity.CapabilityStates[capability.TypeId] = new LogicCore.Capabilities.CapabilityState
                        {
                            IsActive = false, // 初始未激活，等待 ShouldActivate 判定
                            ActiveDuration = 0,
                            DeactiveDuration = 0,
                            CustomData = new Dictionary<string, object>()
                        };
                        
                        // 注册到 CapabilitySystem
                        world.CapabilitySystem?.RegisterEntityCapability(entity.UniqueId, capability.TypeId);
                        
                        // 调用 OnAttached 回调
                        capability.OnAttached(entity);
                    }
                    catch (Exception ex)
                    {
                        ASLogger.Instance.Error($"创建能力 {capType.Name} 失败: {ex.Message}");
                        ASLogger.Instance.LogException(ex);
                    }
                }
            }

            // 【生命周期钩子】能力挂载后
            CallArchetypeLifecycleMethod(archetype, "OnAfterCapabilitiesAttach", entity, world);

            // 初始化引用计数：按主原型声明与实际实例同步
            foreach (var t in info.Components ?? Array.Empty<Type>())
            {
                var key = t.AssemblyQualifiedName ?? t.FullName;
                if (entity.Components.Any(c => c.GetType() == t))
                {
                    entity.ComponentRefCounts[key] = 1;
                }
            }
            foreach (var t in info.Capabilities ?? Array.Empty<Type>())
            {
                var key = t.AssemblyQualifiedName ?? t.FullName;
                // 检查是否是新的 ICapability 接口
                if (typeof(LogicCore.Capabilities.ICapability).IsAssignableFrom(t))
                {
                    var capability = LogicCore.Systems.CapabilitySystem.GetCapability(t);
                    if (capability != null && entity.CapabilityStates.ContainsKey(capability.TypeId))
                    {
                        entity.CapabilityRefCounts[key] = 1;
                    }
                }
            }
            world.Entities[entity.UniqueId] = entity;

            ApplyCreationParams(entity, creationParams);
            
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
            var tb = TableConfig.Instance.Tables.TbBaseUnitTable.Get(entityConfigId);
            if (tb == null) return null;
            return CreateByArchetype(tb.Archetype, new EntityCreationParams { EntityConfigId = entityConfigId }, world);
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
            var tb = TableConfig.Instance.Tables.TbBaseUnitTable.Get(entityConfigId);
            if (tb == null) return null;
            return CreateByArchetype<T>(tb.Archetype, new EntityCreationParams { EntityConfigId = entityConfigId }, world);
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

            // 构建能力已在 CreateByArchetype 中通过 CapabilitySystem 完成
            // 旧代码已移除
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        /// <param name="entity">要销毁的实体</param>
        /// <param name="world">所属世界</param>
        public void DestroyEntity(Entity entity, World world)
        {
            if (entity == null) return;

            // 【生命周期钩子】实体销毁前
            if (entity.Archetype != default(EArchetype))
            {
                if (ArchetypeRegistry.Instance.TryGet(entity.Archetype, out var info))
                {
                    CallArchetypeLifecycleMethod(entity.Archetype, "OnEntityDestroy", entity, world);
                }
            }

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

            // 回收所有 Component 至对象池
            foreach (var component in entity.Components)
            {
                if (component != null && component.IsFromPool)
                {
                    component.Reset();
                    ObjectPool.Instance.Recycle(component);
                }
            }
            
            // 重置 Entity 状态
            entity.Reset();
            
            // 回收 Entity 至对象池
            ObjectPool.Instance.Recycle(entity);
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
                var entityConfig = TableConfig.Instance.Tables.TbBaseUnitTable.Get(entityConfigId);
                return entityConfig?.EntityName ?? $"Entity_{entityConfigId}";
            }
            catch
            {
                return $"Entity_{entityConfigId}";
            }
        }

        /// <summary>
        /// 调用 Archetype 的静态生命周期方法
        /// </summary>
        /// <param name="archetype">原型</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="entity">实体</param>
        /// <param name="world">世界</param>
        private void CallArchetypeLifecycleMethod(EArchetype archetype, string methodName, Entity entity, World world)
        {
            try
            {
                // 根据 archetype 获取对应的类型
                var archetypeType = GetArchetypeType(archetype);
                if (archetypeType != null)
                {
                    // 使用反射调用静态方法
                    var method = archetypeType.GetMethod(methodName, 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method != null)
                    {
                        method.Invoke(null, new object[] { entity, world });
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"调用 Archetype {archetype} 的 {methodName} 方法失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据原型获取类型（使用反射自动获取，无需硬编码）
        /// </summary>
        /// <param name="archetype">原型</param>
        /// <returns>类型</returns>
        private Type GetArchetypeType(EArchetype archetype)
        {
            if (ArchetypeRegistry.Instance.TryGet(archetype, out var info))
            {
                return info.ArchetypeType;
            }
            
            ASLogger.Instance.Error($"GetArchetypeType: Archetype '{archetype}' not found in registry");
            return null;
        }

        /// <summary>
        /// 根据创建参数应用初始位置/旋转等
        /// </summary>
        private void ApplyCreationParams(Entity entity, EntityCreationParams creationParams)
        {
            if (creationParams == null)
            {
                return;
            }

            if (creationParams.SpawnPosition.HasValue)
            {
                var trans = entity.GetComponent<TransComponent>();
                if (trans != null)
                {
                    trans.Position = creationParams.SpawnPosition.Value;
                }
            }

            if (creationParams.SpawnRotation.HasValue)
            {
                var trans = entity.GetComponent<TransComponent>();
                if (trans != null)
                {
                    trans.Rotation = creationParams.SpawnRotation.Value;
                }
            }
        }
    }
}
