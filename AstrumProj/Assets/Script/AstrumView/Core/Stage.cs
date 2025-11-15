using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
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
        protected bool _isLoaded;
        protected DateTime _lastSyncTime;
        
        // 视图组件
        protected Dictionary<long, EntityView> _entityViews;
        protected Dictionary<long, GameObject> _unitViews;
        
        // Unity组件
        protected Camera _camera;
        protected Environment _environment;
        protected GameObject _stageRoot;
        // Stage根节点不应在切场景时销毁，需持久化
        public GameObject StageRoot => _stageRoot;
        
        // 公共属性
        public string StageId => _stageId;
        public string StageName => _stageName;
        public long RoomId => _roomId;
        public Room Room => _room;
        public bool IsActive => _isActive;
        public bool IsLoaded => _isLoaded;
        public DateTime LastSyncTime => _lastSyncTime;
        public Dictionary<long, EntityView> EntityViews => _entityViews;
        public Camera Camera => _camera;
        public Environment Environment => _environment;
        
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
        public event Action OnStageActivated;
        public event Action OnStageDeactivated;
        public event Action OnStageLoaded;
        public event Action OnStageUnloaded;
        public event Action<EntityView> OnEntityViewAdded;
        public event Action<long> OnEntityViewRemoved;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public Stage(string stageId, string stageName = null)
        {
            _stageId = stageId;
            _stageName = stageName ?? stageId;
            _roomId = 0;
            _isActive = false;
            _isLoaded = false;
            _lastSyncTime = DateTime.MinValue;
            _entityViews = new Dictionary<long, EntityView>();
            _unitViews = new Dictionary<long, GameObject>();
        }
        
        /// <summary>
        /// 初始化Stage
        /// </summary>
        public void Initialize()
        {
            if (_isLoaded) return;
            
            
            // 创建Stage根对象
            CreateStageRoot();
            
            // 设置摄像机
            SetupCamera();
            
            // 设置环境
            SetupEnvironment();
            
            // 订阅实体事件
            SubscribeToEntityEvents();
            
            // 调用子类的初始化方法
            OnInitialize();
            
            _isLoaded = true;
            OnStageLoaded?.Invoke();
        }
        
        /// <summary>
        /// 子类可重写的初始化方法
        /// </summary>
        protected virtual void OnInitialize()
        {
        }
        
        /// <summary>
        /// 子类可重写的加载方法
        /// </summary>
        protected virtual void OnLoad()
        {
        }
        
        /// <summary>
        /// 子类可重写的卸载方法
        /// </summary>
        protected virtual void OnUnload()
        {
        }
        
        /// <summary>
        /// 子类可重写的更新方法
        /// </summary>
        protected virtual void OnUpdate(float deltaTime)
        {
        }
        
        /// <summary>
        /// 子类可重写的渲染方法
        /// </summary>
        protected virtual void OnRender()
        {
        }
        
        /// <summary>
        /// 子类可重写的Stage进入方法
        /// </summary>
        protected virtual void OnStageEnter()
        {
        }
        
        /// <summary>
        /// 子类可重写的Stage退出方法
        /// </summary>
        protected virtual void OnStageExit()
        {
        }
        
        /// <summary>
        /// 子类可重写的激活方法
        /// </summary>
        protected virtual void OnActivate()
        {
        }
        
        /// <summary>
        /// 子类可重写的停用方法
        /// </summary>
        protected virtual void OnDeactivate()
        {
        }
        
        /// <summary>
        /// 子类可重写的房间同步方法
        /// </summary>
        /// <param name="roomData">房间数据</param>
        protected virtual void OnSyncWithRoom(RoomData roomData)
        {
        }
        
        /// <summary>
        /// 子类可重写的实体视图创建方法
        /// </summary>
        /// <param name="entityId">实体UniqueId</param>
        /// <returns>创建的实体视图</returns>
        protected virtual EntityView CreateEntityView(long entityId)
        {
            return null;
        }
        
        /// <summary>
        /// 订阅实体事件
        /// </summary>
        private void SubscribeToEntityEvents()
        {
            EventSystem.Instance.Subscribe<EntityCreatedEventData>(OnEntityCreated);
            EventSystem.Instance.Subscribe<EntityDestroyedEventData>(OnEntityDestroyed);
            EventSystem.Instance.Subscribe<EntityUpdatedEventData>(OnEntityUpdated);
            EventSystem.Instance.Subscribe<EntityActiveStateChangedEventData>(OnEntityActiveStateChanged);
            EventSystem.Instance.Subscribe<EntityComponentChangedEventData>(OnEntityComponentChanged);
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
            EventSystem.Instance.Unsubscribe<EntityUpdatedEventData>(OnEntityUpdated);
            EventSystem.Instance.Unsubscribe<EntityActiveStateChangedEventData>(OnEntityActiveStateChanged);
            EventSystem.Instance.Unsubscribe<EntityComponentChangedEventData>(OnEntityComponentChanged);
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
                OnEntityViewRemoved?.Invoke(eventData.EntityId);
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
                    OnEntityViewRemoved?.Invoke(entityId);
                    destroyedCount++;
                    // ASLogger.Instance.Debug($"Stage: 同步销毁 EntityView - EntityId: {entityId}", "Stage.Sync");
                }
            }
            
            if (createdCount > 0 || destroyedCount > 0)
            {
                ASLogger.Instance.Info($"Stage: EntityView 同步完成 - 创建: {createdCount}, 销毁: {destroyedCount}", "Stage.Sync");
            }
        }
        
        /// <summary>
        /// 实体更新事件处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnEntityUpdated(EntityUpdatedEventData eventData)
        {
            if (eventData.RoomId != _roomId) return;
            
            if (_entityViews.TryGetValue(eventData.EntityId, out var entityView))
            {
                entityView.SyncWithEntity(eventData.EntityId);
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
        /// 实体激活状态变化事件处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnEntityActiveStateChanged(EntityActiveStateChangedEventData eventData)
        {
            if (eventData.RoomId != _roomId) return;
            
            // 获取对应的EntityView并设置激活状态
            if (_entityViews.TryGetValue(eventData.EntityId, out var entityView))
            {
                entityView.SetActive(eventData.IsActive);
            }
        }
        
        /// <summary>
        /// 实体组件变化事件处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        private void OnEntityComponentChanged(EntityComponentChangedEventData eventData)
        {
            // 检查是否属于当前Stage的Room
            if (eventData.RoomId != _roomId)
            {
                return;
            }
            
            // 获取对应的EntityView并处理组件变化
            if (_entityViews.TryGetValue(eventData.EntityId, out var entityView))
            {
                // 可以根据组件类型和变化类型进行特殊处理
                switch (eventData.ChangeType.ToLower())
                {
                    case "add":
                        // 处理组件添加
                        break;
                    case "remove":
                        // 处理组件移除
                        break;
                    case "update":
                        // 处理组件更新
                        break;
                }
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
        /// 设置摄像机
        /// </summary>
        private void SetupCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                GameObject cameraObj = new GameObject("Stage Camera");
                cameraObj.transform.SetParent(_stageRoot.transform);
                _camera = cameraObj.AddComponent<Camera>();
                _camera.tag = "MainCamera";
                _camera.transform.position = new Vector3(0, 10, -10);
                _camera.transform.rotation = Quaternion.Euler(30, 0, 0);
            }
            
            ASLogger.Instance.Info("Stage: 摄像机设置完成");
        }
        
        /// <summary>
        /// 设置环境
        /// </summary>
        private void SetupEnvironment()
        {
            _environment = UnityEngine.Object.FindObjectOfType<Environment>();
            if (_environment == null)
            {
                GameObject envObj = new GameObject("Environment");
                envObj.transform.SetParent(_stageRoot.transform);
                _environment = envObj.AddComponent<Environment>();
            }
            
            ASLogger.Instance.Info("Stage: 环境设置完成");
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
                OnActivate();
                OnStageActivated?.Invoke();
                ASLogger.Instance.Info($"Stage: 激活 {_stageName}");
            }
            else
            {
                OnDeactivate();
                OnStageDeactivated?.Invoke();
                ASLogger.Instance.Info($"Stage: 停用 {_stageName}");
            }
        }
        
        /// <summary>
        /// 进入Stage
        /// </summary>
        public void OnEnter()
        {
            ASLogger.Instance.Info($"Stage: 进入 {_stageName}");
            
            // 调用子类的进入方法
            OnStageEnter();
            
            // 激活所有实体视图
            foreach (var entityView in _entityViews.Values)
            {
                if (entityView != null)
                {
                    entityView.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// 退出Stage
        /// </summary>
        public void OnExit()
        {
            ASLogger.Instance.Info($"Stage: 退出 {_stageName}");
            
            // 调用子类的退出方法
            OnStageExit();
            

        }
        
        /// <summary>
        /// 更新Stage - 只更新表现层内容
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isActive) return;
            
            // 调用子类的更新方法
            OnUpdate(deltaTime);
            
            // 更新所有EntityView
            foreach (var entityView in _entityViews.Values)
            {
                entityView?.UpdateView(deltaTime);
            }
            
            // 更新环境
            _environment?.Update();
        }
        
        /// <summary>
        /// 设置Room
        /// </summary>
        public void SetRoom(Room room)
        {
            _room = room;
            _lastSyncTime = DateTime.Now;
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
            
            // 清理所有单位视图
            foreach (var unitView in _unitViews.Values)
            {
                if (unitView != null)
                {
                    UnityEngine.Object.Destroy(unitView);
                }
            }
            _unitViews.Clear();
            
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
            
            _isLoaded = false;
            _isActive = false;
            OnStageUnloaded?.Invoke();
        }
    }
}
