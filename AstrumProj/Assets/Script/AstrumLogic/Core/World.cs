using System;
using System.Collections.Generic;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.FrameSync;
using Astrum.CommonBase;
using MemoryPack;
using Astrum.LogicCore.Archetypes;
using Astrum.LogicCore.Managers;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 游戏世界类，管理所有实体和世界级别的逻辑
    /// </summary>
    [MemoryPackable]
    public partial class World
    {
        /// <summary>
        /// 世界唯一标识符
        /// </summary>
        public int WorldId { get; set; }

        /// <summary>
        /// 世界名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 世界创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 所有实体的字典（EntityId -> Entity）
        /// </summary>
        public Dictionary<long, Entity> Entities { get; private set; }

        /// <summary>
        /// 当前帧时间差
        /// </summary>
        public float DeltaTime { get; set; }

        /// <summary>
        /// 总运行时间
        /// </summary>
        public float TotalTime { get; set; }

        /// <summary>
        /// 世界更新器
        /// </summary>
        public LSUpdater Updater { get; set; }
        
        /// <summary>
        /// 所属房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        public int CurFrame { get; set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public World()
        {
            Entities = new Dictionary<long, Entity>();
        }

        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public World(int worldId, string name, DateTime creationTime, Dictionary<long, Entity> entities,
            float deltaTime, float totalTime, LSUpdater updater, long roomId, int curFrame)
        {
            WorldId = worldId;
            Name = name;
            CreationTime = creationTime;
            Entities = entities ?? new Dictionary<long, Entity>();
            DeltaTime = deltaTime;
            TotalTime = totalTime;
            Updater = updater;
            RoomId = roomId;
            CurFrame = curFrame;
            // 重建关系
            foreach (var entity in Entities.Values)
            {
                // 重建组件的 EntityId 关系
                foreach (var component in entity.Components)
                {
                    component.EntityId = entity.UniqueId;
                }
                
                // 重建 Capability 的 OwnerEntity 关系
                foreach (var capability in entity.Capabilities)
                {
                    capability.OwnerEntity = entity;
                }
            }
        }

        /// <summary>
        /// 创建新实体
        /// </summary>
        /// <param name="entityConfigId">实体配置ID</param>
        /// <returns>创建的实体</returns>
        public T CreateEntity<T>(int entityConfigId) where T : Entity, new()
        {
            var entity = EntityFactory.Instance.CreateEntity<T>(entityConfigId, this);
            
            // 发布实体创建事件
            PublishEntityCreatedEvent(entity);
            
            return entity;
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        public void DestroyEntity(long entityId)
        {
            if (Entities.TryGetValue(entityId, out var entity))
            {
                // 发布实体销毁事件
                PublishEntityDestroyedEvent(entity);
                
                entity.Destroy();
                Entities.Remove(entityId);
            }
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>实体对象，如果不存在返回null</returns>
        public Entity? GetEntity(long entityId)
        {
            return Entities.TryGetValue(entityId, out var entity) ? entity : null;
        }


        /// <summary>
        /// 更新世界状态
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public void Update()
        {
            Updater?.UpdateWorld(this);
            CurFrame++;
        }

        /// <summary>
        /// 初始化世界
        /// </summary>
        public virtual void Initialize(int frame)
        {
            CreationTime = DateTime.Now;
            TotalTime = 0f;
            Updater = new LSUpdater();
            CurFrame = frame;

            // 工厂与原型管理器初始化应由 GameApplication 负责
        }

        /// <summary>
        /// 清理世界资源
        /// </summary>
        public virtual void Cleanup()
        {
            foreach (var entity in Entities.Values)
            {
                entity.Destroy();
            }
            Entities.Clear();
        }
        
        /// <summary>
        /// 发布实体创建事件
        /// </summary>
        /// <param name="entity">创建的实体</param>
        private void PublishEntityCreatedEvent(Entity entity)
        {
            var eventData = new EntityCreatedEventData(entity, WorldId, RoomId);
            EventSystem.Instance.Publish(eventData);
        }
        
        /// <summary>
        /// 发布实体销毁事件
        /// </summary>
        /// <param name="entity">销毁的实体</param>
        private void PublishEntityDestroyedEvent(Entity entity)
        {
            var eventData = new EntityDestroyedEventData(entity, WorldId, RoomId);
            EventSystem.Instance.Publish(eventData);
        }
        
        /// <summary>
        /// 发布实体更新事件
        /// </summary>
        /// <param name="entity">更新的实体</param>
        /// <param name="updateType">更新类型</param>
        /// <param name="updateData">更新数据</param>
        public void PublishEntityUpdatedEvent(Entity entity, string updateType, object updateData)
        {
            var eventData = new EntityUpdatedEventData(entity, WorldId, RoomId, updateType, updateData);
            EventSystem.Instance.Publish(eventData);
        }
        
        /// <summary>
        /// 发布实体激活状态变化事件
        /// </summary>
        /// <param name="entity">状态变化的实体</param>
        /// <param name="isActive">是否激活</param>
        public void PublishEntityActiveStateChangedEvent(Entity entity, bool isActive)
        {
            var eventData = new EntityActiveStateChangedEventData(entity, WorldId, RoomId, isActive);
            EventSystem.Instance.Publish(eventData);
        }

        /// <summary>
        /// 按配置创建实体（支持 ArchetypeName 列）
        /// </summary>
        public Entity CreateEntity(int entityConfigId)
        {
            var entity = EntityFactory.Instance.CreateEntity(entityConfigId, this);
            PublishEntityCreatedEvent(entity);
            return entity;
        }
    }
}
