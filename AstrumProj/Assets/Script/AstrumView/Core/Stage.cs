using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.View.Managers;
using Astrum.View.Archetypes;
using Astrum.View.Components;
using System.Linq;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Astrum.View.Core
{
    /// <summary>
    /// Stage类 - 游戏场景/阶段的表现层映射，不执行游戏逻辑
    /// </summary>
    public class Stage
    {
        // Stage基本信息
        protected string _stageId;
        protected string _stageName;
        protected long _roomId;
        protected Room _room;
        
        // Stage状态
        protected bool _isActive;
        protected bool _isInited;
        
        // 视图组件
        protected Dictionary<long, EntityView> _entityViews;

        // Unity组件
        protected GameObject _stageRoot;
        // Stage根节点不应在切场景时销毁，需持久化
        public GameObject StageRoot => _stageRoot;
        
        // 公共属性
        public string StageId => _stageId;
        public string StageName => _stageName;
        public long RoomId => _roomId;
        public Room Room => _room;
        public bool IsActive => _isActive;
        public bool IsInited => _isInited;
        public Dictionary<long, EntityView> EntityViews => _entityViews;
        
        /// <summary>
        /// 获取指定实体的 EntityView
        /// </summary>
        /// <param name="entityId">实体 UniqueId</param>
        /// <returns>EntityView 实例，如果不存在则返回 null</returns>
        public EntityView GetEntityView(long entityId)
        {
            return _entityViews.TryGetValue(entityId, out var view) ? view : null;
        }
        
        // 事件

        public event Action<EntityView> OnEntityViewAdded;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public Stage(string stageId, string stageName = null)
        {
            _stageId = stageId;
            _stageName = stageName ?? stageId;
            _roomId = 0;
            _isActive = false;
            _isInited = false;
            _entityViews = new Dictionary<long, EntityView>();
        }
        
        /// <summary>
        /// 初始化Stage
        /// </summary>
        public void Initialize()
        {
            if (_isInited) return;
            
            // 启用视图层标记（客户端）
            Entity.HasViewLayer = true;
            
            // 创建Stage根对象
            CreateStageRoot();
            
            _isInited = true;
            
            ASLogger.Instance.Info($"Stage: 初始化完成（使用事件队列模式）- {_stageName}");
        }
        
        /// <summary>
        /// 订阅实体事件
        /// </summary>
        private void SubscribeToEntityEvents()
        {
            EventSystem.Instance.Subscribe<EntityCreatedEventData>(OnEntityCreated);
            EventSystem.Instance.Subscribe<EntityDestroyedEventData>(OnEntityDestroyed);
            EventSystem.Instance.Subscribe<EntitySubArchetypeChangedEventData>(OnEntitySubArchetypeChanged);
            EventSystem.Instance.Subscribe<WorldRollbackEventData>(OnWorldRollback);
            
            ASLogger.Instance.Info($"Stage: 订阅实体事件 - {_stageName}");
        }
        
        /// <summary>
        /// 取消订阅实体事件
        /// </summary>
        private void UnsubscribeFromEntityEvents()
        {
            EventSystem.Instance.Unsubscribe<EntityCreatedEventData>(OnEntityCreated);
            EventSystem.Instance.Unsubscribe<EntityDestroyedEventData>(OnEntityDestroyed);
            EventSystem.Instance.Unsubscribe<EntitySubArchetypeChangedEventData>(OnEntitySubArchetypeChanged);
            EventSystem.Instance.Unsubscribe<WorldRollbackEventData>(OnWorldRollback);
            
            ASLogger.Instance.Info($"Stage: 取消订阅实体事件 - {_stageName}");
        }
        
        /// <summary>
        /// 创建 EntityView（公共方法，供事件和同步方法调用）
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="entityName">实体名称（用于日志）</param>
        /// <returns>是否创建成功</returns>
        private bool CreateEntityViewInternal(long entityId, string entityName = null)
        {
            // 检查是否已存在
            if (_entityViews.ContainsKey(entityId))
            {
                // ASLogger.Instance.Debug($"Stage: EntityView 已存在，跳过创建 - {entityName ?? entityId.ToString()} (ID: {entityId})", "Stage.CreateEntityView");
                return false;
            }
            
            // 创建 EntityView
            var entityView = EntityViewFactory.Instance.CreateEntityView(entityId, this);
            
            if (entityView != null)
            {
                _entityViews[entityId] = entityView;
                entityView.Transform.SetParent(StageRoot.transform);
                OnEntityViewAdded?.Invoke(entityView);
                
                // 触发新实体的全量同步
                SyncEntityViewComponents(entityView);
                
                ASLogger.Instance.Info($"Stage: 创建实体视图成功 - {entityName ?? entityId.ToString()} (ID: {entityId})", "Stage.CreateEntityView");
                return true;
            }
            else
            {
                ASLogger.Instance.Warning($"Stage: 无法创建实体视图 - {entityName ?? entityId.ToString()} (ID: {entityId})", "Stage.CreateEntityView");
                return false;
            }
        }
        
        private void OnEntityCreated(EntityCreatedEventData eventData)
        {
            if (eventData.RoomId != _roomId || eventData.EntityId <= 0) return;
            
            ASLogger.Instance.Info($"Stage: 收到实体创建事件 - {eventData.EntityName} (ID: {eventData.EntityId})", "Stage.Event");
            
            // 检查权威实体是否有影子实体标记
            var entity = Room?.MainWorld?.GetEntity(eventData.EntityId);
            if (entity != null && entity.HasShadow)
            {
                // 有影子实体，等待影子实体创建后再创建 View
                ASLogger.Instance.Info($"Stage: 实体有影子实体标记，跳过创建 View，等待影子实体创建 - {eventData.EntityName} (ID: {eventData.EntityId})", "Stage.Event");
                return;
            }
            
            // 调用公共方法创建 EntityView
            CreateEntityViewInternal(eventData.EntityId, eventData.EntityName);
        }
        
        /// <summary>
        /// 实体销毁事件处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnEntityDestroyed(EntityDestroyedEventData eventData)
        {
            if (eventData.RoomId != _roomId) return;
            
            if (_entityViews.TryGetValue(eventData.EntityId, out var entityView))
            {
                entityView.Destroy();
                _entityViews.Remove(eventData.EntityId);
                ASLogger.Instance.Info($"Stage: 销毁实体视图 - {eventData.EntityName}");
            }
        }
        
        /// <summary>
        /// 同步 EntityView 和 Entity：创建少的，销毁多的
        /// </summary>
        public void SyncEntityViews()
        {
            if (_room?.MainWorld == null)
            {
                ASLogger.Instance.Warning($"Stage: 无法同步 EntityView，Room 或 MainWorld 为空", "Stage.Sync");
                return;
            }
            
            var world = _room.MainWorld;
            int createdCount = 0;
            int destroyedCount = 0;
            
            // 1. 创建少的：遍历所有 Entity，如果不存在对应的 EntityView，则创建
            if (world.Entities != null)
            {
                foreach (var entity in world.Entities.Values)
                {
                    if (entity == null || entity.IsDestroyed) continue;
                    
                    // 如果实体有影子实体标记，跳过（等待影子实体创建后再创建 View）
                    if (entity.HasShadow) continue;
                    
                    if (!_entityViews.ContainsKey(entity.UniqueId))
                    {
                        // 调用公共方法创建 EntityView（与事件处理使用相同的逻辑）
                        if (CreateEntityViewInternal(entity.UniqueId, entity.Name))
                        {
                            createdCount++;
                            // ASLogger.Instance.Debug($"Stage: 同步创建 EntityView - {entity.Name} (ID: {entity.UniqueId})", "Stage.Sync");
                        }
                    }
                }
            }
            
            // 1.5. 检查 ShadowWorld 中的影子实体，如果是玩家实体且 View 不存在，则创建 View
            if (_room?.ShadowWorld != null && _room.ShadowWorld.Entities != null)
            {
                foreach (var entity in _room.ShadowWorld.Entities.Values)
                {
                    if (entity == null || entity.IsDestroyed) continue;
                    
                    // 只处理玩家实体的影子
                    if (_room.MainPlayerId > 0 && entity.UniqueId == _room.MainPlayerId)
                    {
                        if (!_entityViews.ContainsKey(entity.UniqueId))
                        {
                            // 调用公共方法创建 EntityView（绑定到影子实体）
                            if (CreateEntityViewInternal(entity.UniqueId, entity.Name))
                            {
                                createdCount++;
                                ASLogger.Instance.Info($"Stage: 同步创建影子实体 View - {entity.Name} (ID: {entity.UniqueId})", "Stage.Sync");
                            }
                        }
                    }
                }
            }
            
            // 2. 销毁多的：遍历所有 EntityView，如果对应的 Entity 不存在或已销毁，则销毁 EntityView
            var entityViewIdsToRemove = new List<long>();
            foreach (var kvp in _entityViews)
            {
                var entityId = kvp.Key;
                var entityView = kvp.Value;
                
                // 检查 Entity 是否存在且未销毁
                var entity = world.GetEntity(entityId);
                if (entity == null || entity.IsDestroyed)
                {
                    entityViewIdsToRemove.Add(entityId);
                }
            }
            
            // 销毁多余的 EntityView
            foreach (var entityId in entityViewIdsToRemove)
            {
                if (_entityViews.TryGetValue(entityId, out var entityView))
                {
                    entityView.Destroy();
                    _entityViews.Remove(entityId);
                    destroyedCount++;
                    // ASLogger.Instance.Debug($"Stage: 同步销毁 EntityView - EntityId: {entityId}", "Stage.Sync");
                }
            }
            
            if (createdCount > 0 || destroyedCount > 0)
            {
                ASLogger.Instance.Info($"Stage: EntityView 同步完成 - 创建: {createdCount}, 销毁: {destroyedCount}", "Stage.Sync");
            }
            
            // 3. 全量同步：将所有组件视为脏，触发全量更新
            SyncAllComponents();
        }
        
        /// <summary>
        /// 同步指定 EntityView 的所有 ViewComponent（触发全量更新）
        /// </summary>
        /// <param name="entityView">要同步的 EntityView</param>
        private void SyncEntityViewComponents(EntityView entityView)
        {
            if (entityView == null) return;
            
            // 遍历所有 ViewComponent
            foreach (var viewComponent in entityView.ViewComponents)
            {
                if (viewComponent == null || !viewComponent.IsEnabled) continue;
                
                // 获取该 ViewComponent 监听的 ComponentTypeId 列表
                var watchedIds = viewComponent.GetWatchedComponentIds();
                if (watchedIds == null || watchedIds.Length == 0) continue;
                
                // 对每个监听的 ComponentTypeId，触发同步
                foreach (var componentTypeId in watchedIds)
                {
                    viewComponent.SyncDataFromComponent(componentTypeId);
                }
            }
        }
        
        /// <summary>
        /// 全量同步所有 Entity 的所有组件（在 SyncEntityViews 时调用）
        /// 直接触发所有 ViewComponent 的 SyncDataFromComponent
        /// </summary>
        private void SyncAllComponents()
        {
            if (_room?.MainWorld == null) return;
            
            // 遍历所有 EntityView
            foreach (var entityView in _entityViews.Values)
            {
                SyncEntityViewComponents(entityView);
            }
        }
        
        /// <summary>
        /// 世界回滚事件处理（回滚后需要同步 EntityView）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnWorldRollback(WorldRollbackEventData eventData)
        {
            if (eventData.RoomId != _roomId) return;
            
            ASLogger.Instance.Info($"Stage: 收到世界回滚事件 - 帧: {eventData.RollbackFrame}, WorldId: {eventData.WorldId}", "Stage.Rollback");
            
            // 回滚后同步 EntityView（创建少的，销毁多的）
            SyncEntityViews();
        }
        
        /// <summary>
        /// 重置回放视图（用于回放跳转时强制同步所有视图）
        /// </summary>
        public void ResetReplayViews()
        {
            ASLogger.Instance.Info("Stage: 重置回放视图", "Stage.Reset");
            
            // 1. 同步 EntityView（创建少的，销毁多的）
            SyncEntityViews();
            
            // 2. 遍历所有 EntityView，调用 Reset
            foreach (var kvp in _entityViews)
            {
                var entityView = kvp.Value;
                if (entityView != null)
                {
                    entityView.Reset();
                }
            }
            
            ASLogger.Instance.Info($"Stage: 回放视图重置完成 - 实体数量: {_entityViews.Count}", "Stage.Reset");
        }
        
        /// <summary>
        /// 实体子原型变化事件处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnEntitySubArchetypeChanged(EntitySubArchetypeChangedEventData eventData)
        {
            // 检查是否属于当前Stage的Room
            if (eventData.RoomId != _roomId)
            {
                return;
            }
            
            // 获取对应的EntityView
            if (_entityViews.TryGetValue(eventData.EntityId, out var entityView))
            {
                // 根据变化类型调用 EntityView 的 Attach/Detach 方法
                switch (eventData.ChangeType)
                {
                    case SubArchetypeChangeType.Attach:
                        entityView.AttachSubArchetype(eventData.SubArchetype);
                        ASLogger.Instance.Info($"Stage: 处理子原型 Attach 事件 - EntityId: {eventData.EntityId}, SubArchetype: {eventData.SubArchetype}");
                        break;
                    case SubArchetypeChangeType.Detach:
                        entityView.DetachSubArchetype(eventData.SubArchetype);
                        ASLogger.Instance.Info($"Stage: 处理子原型 Detach 事件 - EntityId: {eventData.EntityId}, SubArchetype: {eventData.SubArchetype}");
                        break;
                    default:
                        ASLogger.Instance.Warning($"Stage: 未知的子原型变化类型 - {eventData.ChangeType}");
                        break;
                }
            }
            else
            {
                ASLogger.Instance.Warning($"Stage: 找不到对应的 EntityView - EntityId: {eventData.EntityId}");
            }
        }
        
        /// <summary>
        /// 创建Stage根对象
        /// </summary>
        private void CreateStageRoot()
        {
            _stageRoot = new GameObject($"Stage_{_stageId}");
            UnityEngine.Object.DontDestroyOnLoad(_stageRoot);
            ASLogger.Instance.Info($"Stage: 创建Stage根对象 {_stageRoot.name}");
        }
        
        /// <summary>
        /// 激活Stage
        /// </summary>
        public void SetActive(bool active)
        {
            if (_isActive == active) return;
            
            _isActive = active;
            
            if (_stageRoot != null)
            {
                _stageRoot.SetActive(active);
            }
            
            if (_isActive)
            {
                ASLogger.Instance.Info($"Stage: 激活 {_stageName}");
            }
            else
            {
                ASLogger.Instance.Info($"Stage: 停用 {_stageName}");
            }
        }
        
        /// <summary>
        /// 进入Stage
        /// </summary>
        public void OnEnter()
        {
            ASLogger.Instance.Info($"Stage: 进入 {_stageName}");
            
        }
        
        /// <summary>
        /// 退出Stage
        /// </summary>
        public void OnExit()
        {
            ASLogger.Instance.Info($"Stage: 退出 {_stageName}");
        }
        
        /// <summary>
        /// 更新Stage - 只更新表现层内容
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isActive) return;
            
            using (new ProfileScope("Stage.Update"))
            {
                // 处理视图事件队列
                using (new ProfileScope("Stage.ProcessViewEvents"))
                {
                    ProcessViewEvents();
                }
                
                // 处理脏组件同步
                using (new ProfileScope("Stage.SyncDirtyComponents"))
                {
                    SyncDirtyComponents();
                }
                
                // 更新所有EntityView
                using (new ProfileScope("Stage.UpdateEntityViews"))
                {
                    foreach (var entityView in _entityViews.Values)
                    {
                        entityView?.UpdateView(deltaTime);
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理所有 Entity 的视图事件队列
        /// </summary>
        private void ProcessViewEvents()
        {
            if (_room?.MainWorld == null) return;
            
            int totalEventsProcessed = 0;
            
            // 遍历所有 Entity，检查是否有待处理的视图事件
            foreach (var entity in _room.MainWorld.Entities.Values)
            {
                if (entity == null || !entity.HasPendingViewEvents)
                    continue;
                
                var entityId = entity.UniqueId;
                var eventQueue = entity.ViewEventQueue;
                
                if (eventQueue == null || eventQueue.Count == 0)
                    continue;
                
                // 处理所有事件
                int eventCount = eventQueue.Count;
                while (eventQueue.Count > 0)
                {
                    var evt = eventQueue.Dequeue();
                    
                    // Stage 级别事件：由 Stage 直接处理
                    if (evt.EventType == ViewEventType.EntityCreated)
                    {
                        ProcessStageEvent_EntityCreated(entityId, evt);
                        totalEventsProcessed++;
                    }
                    else if (evt.EventType == ViewEventType.EntityDestroyed)
                    {
                        ProcessStageEvent_EntityDestroyed(entityId, evt);
                        totalEventsProcessed++;
                    }
                    else if (evt.EventType == ViewEventType.WorldRollback)
                    {
                        ProcessStageEvent_WorldRollback(entityId, evt);
                        totalEventsProcessed++;
                    }
                    // EntityView/ViewComponent 级别事件：传递给 EntityView 处理
                    else if (evt.EventType == ViewEventType.SubArchetypeChanged || 
                             evt.EventType == ViewEventType.CustomViewEvent)
                    {
                        // 获取 EntityView
                        if (_entityViews.TryGetValue(entityId, out var entityView))
                        {
                            // 传递给 EntityView 处理
                            entityView.ProcessEvent(evt);
                            totalEventsProcessed++;
                        }
                        else
                        {
                            ASLogger.Instance.Warning($"Stage: EntityView 不存在，无法处理事件 {evt.EventType}，Entity {entityId}", "Stage.ProcessViewEvents");
                        }
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"Stage: 未知的视图事件类型 {evt.EventType}，Entity {entityId}", "Stage.ProcessViewEvents");
                    }
                }
                
                if (eventCount > 0)
                {
                    ASLogger.Instance.Debug($"Stage: 处理了 {eventCount} 个视图事件，Entity {entityId}", "Stage.ProcessViewEvents");
                }
            }
            
            if (totalEventsProcessed > 0)
            {
                ASLogger.Instance.Debug($"Stage: 本帧共处理 {totalEventsProcessed} 个视图事件", "Stage.ProcessViewEvents");
            }
        }
        
        /// <summary>
        /// 处理 Stage 级别事件：实体创建
        /// </summary>
        private void ProcessStageEvent_EntityCreated(long entityId, ViewEvent evt)
        {
            // 检查是否已存在
            if (_entityViews.ContainsKey(entityId))
            {
                ASLogger.Instance.Debug($"Stage: EntityView 已存在，跳过创建 - Entity {entityId}", "Stage.ProcessStageEvent_EntityCreated");
                return;
            }
            
            // 创建 EntityView
            bool created = CreateEntityViewInternal(entityId);
            if (!created)
            {
                ASLogger.Instance.Error($"Stage: 创建 EntityView 失败 - Entity {entityId}", "Stage.ProcessStageEvent_EntityCreated");
            }
        }
        
        /// <summary>
        /// 处理 Stage 级别事件：实体销毁
        /// </summary>
        private void ProcessStageEvent_EntityDestroyed(long entityId, ViewEvent evt)
        {
            // 销毁 EntityView
            if (_entityViews.TryGetValue(entityId, out var entityView))
            {
                _entityViews.Remove(entityId);
                entityView.Destroy();
                ASLogger.Instance.Debug($"Stage: 销毁 EntityView - Entity {entityId}", "Stage.ProcessStageEvent_EntityDestroyed");
            }
        }
        
        /// <summary>
        /// 处理 Stage 级别事件：世界回滚
        /// </summary>
        private void ProcessStageEvent_WorldRollback(long entityId, ViewEvent evt)
        {
            // 回滚所有 EntityView 的状态
            foreach (var entityView in _entityViews.Values)
            {
                entityView.Reset();
            }
            ASLogger.Instance.Debug($"Stage: 处理世界回滚事件", "Stage.ProcessStageEvent_WorldRollback");
        }
        
        /// <summary>
        /// 同步所有 Entity 的脏组件
        /// </summary>
        private void SyncDirtyComponents()
        {
            // 同步 MainWorld 中的实体（只处理没有影子的实体）
            if (_room?.MainWorld != null)
            {
                foreach (var entity in _room.MainWorld.Entities.Values)
                {
                    var dirtyComponentIds = entity.GetDirtyComponentIds();
                    if (dirtyComponentIds.Count > 0)
                    {
                        // 只有当实体没有影子时，才通知对应的 EntityView
                        if (!entity.HasShadow)
                        {
                            // 获取对应的 EntityView
                            if (_entityViews.TryGetValue(entity.UniqueId, out var entityView))
                            {
                                // 通知 EntityView 同步脏组件（传入 ComponentId 集合）
                                entityView.SyncDirtyComponents(dirtyComponentIds);
                            }
                        }
                        
                        // 清除脏标记
                        entity.ClearDirtyComponents();
                    }
                }
            }
            
            // 同步 ShadowWorld 中的实体（处理所有实体）
            if (_room?.ShadowWorld != null)
            {
                foreach (var entity in _room.ShadowWorld.Entities.Values)
                {
                    var dirtyComponentIds = entity.GetDirtyComponentIds();
                    if (dirtyComponentIds.Count > 0)
                    {
                        // 获取对应的 EntityView
                        if (_entityViews.TryGetValue(entity.UniqueId, out var entityView))
                        {
                            // 通知 EntityView 同步脏组件（传入 ComponentId 集合）
                            entityView.SyncDirtyComponents(dirtyComponentIds);
                        }
                        
                        // 清除脏标记
                        entity.ClearDirtyComponents();
                    }
                }
            }
        }
        
        /// <summary>
        /// 设置Room
        /// </summary>
        public void SetRoom(Room room)
        {
            _room = room;
            _roomId = room.RoomId;
            if (room != null)
            {
                ASLogger.Instance.Info($"Stage: 设置Room ID = {room.RoomId}");
            }
            else
            {
                ASLogger.Instance.Info($"Stage: 清除Room（设置为null）");
            }
        }
        
        /// <summary>
        /// 销毁Stage
        /// </summary>
        public void Destroy()
        {
            ASLogger.Instance.Info($"Stage: 销毁 {_stageName}");
            
            // 取消订阅实体事件
            UnsubscribeFromEntityEvents();
            

            
            // 清理所有实体视图
            foreach (var entityView in _entityViews.Values)
            {
                if (entityView != null)
                {
                    entityView.Destroy();
                }
            }
            _entityViews.Clear();
            
            // 销毁Stage根对象
            if (_stageRoot != null)
            {
                UnityEngine.Object.Destroy(_stageRoot);
                _stageRoot = null;
            }
            
            _isInited = false;
            _isActive = false;
        }
    }
}
