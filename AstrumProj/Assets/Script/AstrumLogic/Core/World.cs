using System;
using System.Collections.Generic;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Systems;
using Astrum.LogicCore.Events;
using Astrum.CommonBase;
using MemoryPack;
using Astrum.LogicCore.Archetypes;
using Astrum.LogicCore.Managers;
using cfg;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 游戏世界类，管理所有实体和世界级别的逻辑
    /// </summary>
    [MemoryPackable]
    public partial class World : IPool
    {
        /// <summary>
        /// 对象是否来自对象池（IPool 接口实现）
        /// </summary>
        [MemoryPackIgnore]
        public bool IsFromPool { get; set; }
        
        private struct PendingSubChange
        {
            public long EntityId;
            public EArchetype Archetype;
            public bool Attach;
        }

        private readonly List<PendingSubChange> _pendingSubArchetypeChanges = new List<PendingSubChange>();

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
        /// 命中系统（碰撞检测）
        /// </summary>
        public HitSystem HitSystem { get; private set; }

        /// <summary>
        /// 技能效果系统
        /// </summary>
        public SkillEffectSystem SkillEffectSystem { get; private set; }

        /// <summary>
        /// Capability 统一调度系统
        /// </summary>
        public CapabilitySystem CapabilitySystem { get; private set; }
        
        /// <summary>
        /// 全局广播事件队列（不序列化，瞬时数据）
        /// </summary>
        [MemoryPackIgnore]
        public Events.GlobalEventQueue GlobalEventQueue { get; private set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public World()
        {
            Entities = new Dictionary<long, Entity>(100); // 预分配容量 100
            HitSystem = new HitSystem();
            SkillEffectSystem = new SkillEffectSystem();
            CapabilitySystem = new CapabilitySystem();
            CapabilitySystem.World = this;
            GlobalEventQueue = new Events.GlobalEventQueue(); // 初始化全局事件队列
        }

        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public World(int worldId, string name, DateTime creationTime, Dictionary<long, Entity> entities,
            float totalTime, LSUpdater updater, long roomId, int curFrame,
            HitSystem hitSystem, SkillEffectSystem skillEffectSystem, CapabilitySystem capabilitySystem)
        {
            WorldId = worldId;
            Name = name;
            CreationTime = creationTime;
            Entities = entities ?? new Dictionary<long, Entity>(100); // 预分配容量 100
            TotalTime = totalTime;
            Updater = updater;
            RoomId = roomId;
            CurFrame = curFrame;
            HitSystem = hitSystem ?? new HitSystem();
            SkillEffectSystem = skillEffectSystem ?? new SkillEffectSystem();
            CapabilitySystem = capabilitySystem ?? new CapabilitySystem();
            
            CapabilitySystem.World = this;
            GlobalEventQueue = new Events.GlobalEventQueue(); // 初始化全局事件队列（不序列化）
            
            // 确保静态数据已初始化

            // 重建关系
            foreach (var entity in Entities.Values)
            {
                // 重建 Entity 的 World 引用
                entity.World = this;
                
                // 重建组件的 EntityId 关系
                foreach (var component in entity.Components)
                {
                    component.EntityId = entity.UniqueId;
                }
                
                // 重建 CapabilitySystem 的注册（从 Entity 的 CapabilityStates 恢复）
                if (entity.CapabilityStates != null && entity.CapabilityStates.Count > 0)
                {
                    foreach (var kvp in entity.CapabilityStates)
                    {
                        CapabilitySystem.RegisterEntityCapability(entity.UniqueId, kvp.Key);
                    }
                }
                else
                {
                    ASLogger.Instance.Warning($"World 反序列化：Entity {entity.UniqueId} 的 CapabilityStates 为空", "World.Deserialize");
                }
            }
            
            // 验证重建结果
            if (CapabilitySystem.TypeIdToEntityIds.Count == 0)
            {
                ASLogger.Instance.Error($"World 反序列化：重建后 TypeIdToEntityIds 仍为空！实体数量: {Entities.Count}", "World.Deserialize");
            }
            else
            {
                ASLogger.Instance.Info($"World 反序列化：成功重建 TypeIdToEntityIds，包含 {CapabilitySystem.TypeIdToEntityIds.Count} 种 Capability 类型", "World.Deserialize");
            }
            
            // 重建 Systems 的引用
            SkillEffectSystem.CurrentWorld = this;
            
            // 从 Entities 同步物理数据
            HitSystem.SyncFromEntities(Entities);
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

        public void QueueDestroyEntity(long entityId)
        {
            if (!_pendingDestroyEntities.Contains(entityId))
            {
                _pendingDestroyEntities.Add(entityId);
            }
        }

        public void QueueCreateEntity(EArchetype archetype, EntityCreationParams creationParams, Action<Entity>? postCreate = null)
        {

            var pending = new PendingEntityCreation
            {
                Archetype = archetype,
                CreationParams = CloneCreationParams(creationParams),
                PostCreateAction = postCreate
            };

            _pendingCreateEntities.Add(pending);
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        public void DestroyEntity(long entityId)
        {
            DestroyEntityImmediate(entityId);
        }

        private void DestroyEntityImmediate(long entityId)
        {
            if (!Entities.TryGetValue(entityId, out var entity))
                return;

            PublishEntityDestroyedEvent(entity);
            CapabilitySystem?.UnregisterEntity(entityId);
            entity.ClearEventQueue();
            EntityFactory.Instance.DestroyEntity(entity, this);
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
            using (new ProfileScope("World.Update"))
            {
                // 1. 更新所有 Capability（可能会产生新事件）
                using (new ProfileScope("World.UpdateWorld"))
                {
                    Updater?.UpdateWorld(this);
                }
                
                // 2. 处理本帧产生的所有事件（个体+全体）
                using (new ProfileScope("World.ProcessEntityEvents"))
                {
                    CapabilitySystem?.ProcessEntityEvents();
                }

                ProcessQueuedEntityCreates();
                ProcessQueuedEntityDestroys();
                
                // 3. 帧计数和后处理
                using (new ProfileScope("World.StepPhysics"))
                {
                    var physicsDeltaTime = Updater != null ? Updater.FixedDeltaTime : (1f / 60f);
                    HitSystem?.StepPhysics(physicsDeltaTime);
                }

                CurFrame++;
                ApplyQueuedSubArchetypeChangesAtFrameEnd();
            }
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

            // 初始化系统
            HitSystem = new HitSystem();
            HitSystem.Initialize();
            
            SkillEffectSystem = new SkillEffectSystem();
            SkillEffectSystem.CurrentWorld = this;
            SkillEffectSystem.Initialize();
            
            CapabilitySystem = new CapabilitySystem();
            CapabilitySystem.World = this;

            // 工厂与原型管理器初始化应由 GameApplication 负责
        }

        public void EnqueueSubArchetypeChange(long entityId, EArchetype subArchetype, bool attach)
        {
            _pendingSubArchetypeChanges.Add(new PendingSubChange
            {
                EntityId = entityId,
                Archetype = subArchetype,
                Attach = attach
            });
        }

        private readonly List<long> _pendingDestroyEntities = new List<long>();
        private readonly List<PendingEntityCreation> _pendingCreateEntities = new List<PendingEntityCreation>();

        private sealed class PendingEntityCreation
        {
            public EArchetype Archetype;
            public EntityCreationParams CreationParams = new EntityCreationParams();
            public Action<Entity>? PostCreateAction;
        }

        private static EntityCreationParams CloneCreationParams(EntityCreationParams? source)
        {
            if (source == null)
            {
                return new EntityCreationParams();
            }

            return new EntityCreationParams
            {
                EntityConfigId = source.EntityConfigId,
                SpawnPosition = source.SpawnPosition,
                SpawnRotation = source.SpawnRotation,
                ExtraData = source.ExtraData
            };
        }

        private void ProcessQueuedEntityCreates()
        {
            if (_pendingCreateEntities.Count == 0)
                return;

            foreach (var pending in _pendingCreateEntities)
            {
                var entity = EntityFactory.Instance.CreateByArchetype(
                    pending.Archetype,
                    pending.CreationParams,
                    this);

                if (entity != null)
                {
                    pending.PostCreateAction?.Invoke(entity);
                    PublishEntityCreatedEvent(entity);
                }
            }

            _pendingCreateEntities.Clear();
        }

        private void ProcessQueuedEntityDestroys()
        {
            if (_pendingDestroyEntities.Count == 0)
                return;

            foreach (var entityId in _pendingDestroyEntities)
            {
                DestroyEntityImmediate(entityId);
            }

            _pendingDestroyEntities.Clear();
        }

        public void ApplyQueuedSubArchetypeChangesAtFrameEnd()
        {
            if (_pendingSubArchetypeChanges.Count == 0) return;
            foreach (var change in _pendingSubArchetypeChanges)
            {
                var ent = GetEntity(change.EntityId);
                if (ent == null) continue;
                if (change.Attach)
                {
                    ent.AttachSubArchetype(change.Archetype, out _);
                }
                else
                {
                    ent.DetachSubArchetype(change.Archetype, out _);
                }
            }
            _pendingSubArchetypeChanges.Clear();
        }

        /// <summary>
        /// 清理世界资源
        /// </summary>
        public virtual void Cleanup()
        {
            // 清理所有全局事件
            GlobalEventQueue?.ClearAll();
            
            // 清理所有实体的个体事件
            foreach (var entity in Entities.Values)
            {
                entity?.ClearEventQueue();
            }
            
            // 回收所有 Entity 至对象池（通过 EntityFactory 统一处理）
            var entitiesToDestroy = new List<Entity>(Entities.Values);
            foreach (var entity in entitiesToDestroy)
            {
                if (entity != null)
                {
                    EntityFactory.Instance.DestroyEntity(entity, this);
                }
            }
            Entities.Clear();
            
            // 清理系统资源
            HitSystem?.Dispose();
            SkillEffectSystem?.ClearQueue();
        }
        
        /// <summary>
        /// 回收 World 至对象池（在 Cleanup 后调用）
        /// </summary>
        public void Recycle()
        {
            Cleanup();
            Reset();
            
            // 回收 World 至对象池
            if (IsFromPool)
            {
                ObjectPool.Instance.Recycle(this);
            }
        }
        
        /// <summary>
        /// 发布实体创建事件
        /// </summary>
        /// <param name="entity">创建的实体</param>
        private void PublishEntityCreatedEvent(Entity entity)
        {
            // 使用视图事件队列（新机制）
            entity.QueueViewEvent(new ViewEvent(ViewEventType.EntityCreated, null, CurFrame));
            
            // 保留 EventSystem 发布用于其他系统（如音效、特效等）
            // 注释掉以避免重复处理，但保留代码便于回退
            // var eventData = new EntityCreatedEventData(entity, WorldId, RoomId);
            // EventSystem.Instance.Publish(eventData);
        }
        
        /// <summary>
        /// 发布实体销毁事件
        /// </summary>
        /// <param name="entity">销毁的实体</param>
        private void PublishEntityDestroyedEvent(Entity entity)
        {
            // 使用视图事件队列（新机制）
            entity.QueueViewEvent(new ViewEvent(ViewEventType.EntityDestroyed, null, CurFrame));
            
            // 保留 EventSystem 发布用于其他系统（如音效、特效等）
            // 注释掉以避免重复处理，但保留代码便于回退
            // var eventData = new EntityDestroyedEventData(entity, WorldId, RoomId);
            // EventSystem.Instance.Publish(eventData);
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
        
        /// <summary>
        /// 重置 World 状态（用于对象池回收）
        /// </summary>
        public virtual void Reset()
        {
            // 重置基础字段
            WorldId = 0;
            Name = string.Empty;
            CreationTime = default(DateTime);
            TotalTime = 0f;
            CurFrame = 0;
            RoomId = 0;
            
            // 清空所有集合
            Entities.Clear();
            _pendingSubArchetypeChanges.Clear();
            
            // 清理系统资源（需要在重置前清理）
            if (GlobalEventQueue != null)
            {
                GlobalEventQueue.ClearAll();
            }
            
            // 重置系统（将在 Initialize 时重新创建）
            HitSystem = null;
            SkillEffectSystem = null;
            CapabilitySystem = null;
            Updater = null;
        }
    }
}
