using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;

namespace Astrum.View.Core
{
    /// <summary>
    /// Stage基类 - 游戏场景/阶段的抽象表示
    /// </summary>
    public abstract class Stage
    {
        // Stage基本信息
        protected string _stageId;
        protected string _stageName;
        protected long _roomId;
        
        // Stage状态
        protected bool _isActive;
        protected bool _isLoaded;
        protected DateTime _lastSyncTime;
        
        // 视图组件
        protected Dictionary<long, EntityView> _entityViews;
        
        // Unity组件
        protected Camera _camera;
        protected Environment _environment;
        
        // 公共属性
        public string StageId => _stageId;
        public string StageName => _stageName;
        public long RoomId => _roomId;
        public bool IsActive => _isActive;
        public bool IsLoaded => _isLoaded;
        public DateTime LastSyncTime => _lastSyncTime;
        public Dictionary<long, EntityView> EntityViews => _entityViews;
        public Camera Camera => _camera;
        public Environment Environment => _environment;
        
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
            
            Initialize();
        }
        
        /// <summary>
        /// 初始化Stage
        /// </summary>
        protected virtual void Initialize()
        {
            // 子类可以重写此方法进行特定初始化
        }
        
        /// <summary>
        /// 激活Stage
        /// </summary>
        public virtual void SetActive(bool active)
        {
            if (_isActive == active) return;
            
            _isActive = active;
            
            if (_isActive)
            {
                OnActivate();
                OnStageActivated?.Invoke();
            }
            else
            {
                OnDeactivate();
                OnStageDeactivated?.Invoke();
            }
        }
        
        /// <summary>
        /// 更新Stage
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public virtual void Update(float deltaTime)
        {
            if (!_isActive) return;
            
            // 更新所有EntityView
            foreach (var entityView in _entityViews.Values)
            {
                entityView?.UpdateView(deltaTime);
            }
            
            // 更新环境
            _environment?.Update();
        }
        
        /// <summary>
        /// 渲染Stage
        /// </summary>
        public virtual void Render()
        {
            if (!_isActive || !_isLoaded) return;
            
            // 子类特定的渲染逻辑
            OnRender();
        }
        
        /// <summary>
        /// 进入Stage时调�?
        /// </summary>
        public virtual void OnEnter()
        {
            ASLogger.Instance.Info($"Stage {_stageName}: 进入Stage");
            
            // 激活所有实体视�?
            foreach (var entityView in _entityViews.Values)
            {
                entityView.SetActive(true);
            }
            
            // 子类特定的进入逻辑
            OnStageEnter();
        }
        
        /// <summary>
        /// 退出Stage时调�?
        /// </summary>
        public virtual void OnExit()
        {
            ASLogger.Instance.Info($"Stage {_stageName}: 退出Stage");
            
            // 停用所有实体视�?
            foreach (var entityView in _entityViews.Values)
            {
                entityView.SetActive(false);
            }
            
            // 子类特定的退出逻辑
            OnStageExit();
        }
        
        /// <summary>
        /// 添加实体视图
        /// </summary>
        /// <param name="entityView">实体视图</param>
        public virtual void AddEntityView(EntityView entityView)
        {
            if (entityView == null)
            {
                ASLogger.Instance.Warning($"Stage {_stageName}: 尝试添加空的实体视图");
                return;
            }
            
            if (_entityViews.ContainsKey(entityView.EntityId))
            {
                ASLogger.Instance.Warning($"Stage {_stageName}: 实体视图已存在，ID: {entityView.EntityId}");
                return;
            }
            
            _entityViews[entityView.EntityId] = entityView;
            
            OnEntityViewAdded?.Invoke(entityView);
            ASLogger.Instance.Info($"Stage {_stageName}: 添加实体视图，ID: {entityView.EntityId}");
        }
        
        /// <summary>
        /// 移除实体视图
        /// </summary>
        /// <param name="entityId">实体ID</param>
        public virtual void RemoveEntityView(long entityId)
        {
            if (!_entityViews.ContainsKey(entityId))
            {
                ASLogger.Instance.Warning($"Stage {_stageName}: 实体视图不存在，ID: {entityId}");
                return;
            }
            
            EntityView entityView = _entityViews[entityId];
            _entityViews.Remove(entityId);
            
            if (entityView != null)
            {
                entityView.Destroy();
            }
            
            OnEntityViewRemoved?.Invoke(entityId);
            ASLogger.Instance.Info($"Stage {_stageName}: 移除实体视图，ID: {entityId}");
        }
        
        /// <summary>
        /// 获取实体视图
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>实体视图</returns>
        public virtual EntityView GetEntityView(long entityId)
        {
            _entityViews.TryGetValue(entityId, out EntityView entityView);
            return entityView;
        }
        
        /// <summary>
        /// 清空所有实体视�?
        /// </summary>
        public virtual void ClearEntityViews()
        {
            foreach (var entityView in _entityViews.Values)
            {
                if (entityView != null)
                {
                    entityView.Destroy();
                }
            }
            
            _entityViews.Clear();
            ASLogger.Instance.Info($"Stage {_stageName}: 清空所有实体视图");
        }
        
        /// <summary>
        /// 与逻辑层Room同步
        /// </summary>
        /// <param name="roomData">房间数据</param>
        public virtual void SyncWithRoom(RoomData roomData)
        {
            if (roomData == null)
            {
                ASLogger.Instance.Warning($"Stage {_stageName}: 房间数据为空，跳过同步");
                return;
            }
            
            // 更新Room ID
            _roomId = roomData.RoomId;
            
            // 同步实体数据
            SyncEntities(roomData.Entities);
            
            // 同步环境数据
            SyncEnvironment(roomData.Environment);
            
            _lastSyncTime = DateTime.Now;
            
            // 子类特定的同步逻辑
            OnSyncWithRoom(roomData);
        }
        
        /// <summary>
        /// 设置摄像�?
        /// </summary>
        protected virtual void SetupCamera()
        {
            // 查找场景中的主摄像机
            _camera = Camera.main;
            if (_camera == null)
            {
                // 如果没有主摄像机，创建一�?
                GameObject cameraObj = new GameObject("StageCamera");
                _camera = cameraObj.AddComponent<Camera>();
                _camera.tag = "MainCamera";
            }
        }
        
        /// <summary>
        /// 设置环境
        /// </summary>
        protected virtual void SetupEnvironment()
        {
            // 查找场景中的环境组件
            _environment = UnityEngine.Object.FindObjectOfType<Environment>();
            if (_environment == null)
            {
                // 如果没有环境组件，创建一个
                GameObject envObj = new GameObject("Environment");
                _environment = envObj.AddComponent<Environment>();
            }
        }
        
        /// <summary>
        /// 同步实体数据
        /// </summary>
        /// <param name="entities">实体数据列表</param>
        protected virtual void SyncEntities(List<EntityData> entities)
        {
            if (entities == null) return;
            
            // 移除不存在的实体
            List<long> entitiesToRemove = new List<long>();
            foreach (var kvp in _entityViews)
            {
                if (!entities.Exists(e => e.EntityId == kvp.Key))
                {
                    entitiesToRemove.Add(kvp.Key);
                }
            }
            
            foreach (long entityId in entitiesToRemove)
            {
                RemoveEntityView(entityId);
            }
            
            // 添加或更新实�?
            foreach (var entityData in entities)
            {
                EntityView entityView = GetEntityView(entityData.EntityId);
                if (entityView == null)
                {
                    // 创建新的实体视图
                    entityView = CreateEntityView(entityData);
                    if (entityView != null)
                    {
                        AddEntityView(entityView);
                    }
                }
                else
                {
                    // 同步现有实体视图
                    entityView.SyncWithEntity(entityData);
                }
            }
        }
        
        /// <summary>
        /// 同步环境数据
        /// </summary>
        /// <param name="environmentData">环境数据</param>
        protected virtual void SyncEnvironment(EnvironmentData environmentData)
        {
            if (_environment != null && environmentData != null)
            {
                _environment.SyncWithData(environmentData);
            }
        }
        
        /// <summary>
        /// 创建实体视图
        /// </summary>
        /// <param name="entityData">实体数据</param>
        /// <returns>实体视图</returns>
        protected abstract EntityView CreateEntityView(EntityData entityData);
        
        // 抽象方法 - 子类必须实现
        protected abstract void OnInitialize();
        protected abstract void OnLoad();
        protected abstract void OnUnload();
        protected abstract void OnUpdate(float deltaTime);
        protected abstract void OnRender();
        protected abstract void OnStageEnter();
        protected abstract void OnStageExit();
        protected abstract void OnSyncWithRoom(RoomData roomData);
        protected abstract void OnActivate();
        protected abstract void OnDeactivate();
    }
    
    /// <summary>
    /// 房间数据（用于同步）
    /// </summary>
    [Serializable]
    public class RoomData
    {
        public long RoomId { get; set; }
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        public EnvironmentData Environment { get; set; }
    }
    
    /// <summary>
    /// 实体数据（用于同步）
    /// </summary>
    [Serializable]
    public class EntityData
    {
        public long EntityId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public string EntityType { get; set; }
        public Dictionary<string, object> ComponentData { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// 环境数据（用于同步）
    /// </summary>
    [Serializable]
    public class EnvironmentData
    {
        public string EnvironmentType { get; set; }
        public Vector3 Lighting { get; set; }
        public float Weather { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }
}

