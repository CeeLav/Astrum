using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.View.Core;

namespace Astrum.View.Managers
{
    /// <summary>
    /// 同步数据�?
    /// </summary>
    [Serializable]
    public class SyncData
    {
        public long EntityId { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public Dictionary<string, object> ComponentData { get; set; } = new Dictionary<string, object>();
        public long Timestamp { get; set; }
    }
    
    /// <summary>
    /// 视图同步管理�?
    /// 负责管理表现层与逻辑层的同步
    /// </summary>
    public class ViewSyncManager
    {
        // 单例实例
        public static ViewSyncManager Instance { get; private set; }
        
        // 同步设置
        private float _syncInterval = 0.016f; // 60fps
        private Queue<SyncData> _pendingSyncs = new Queue<SyncData>();
        private int _lastSyncFrame = 0;
        
        // 状态管�?
        private bool _isInitialized = false;
        private float _lastSyncTime = 0f;
        
        // 公共属�?
        public float SyncInterval => _syncInterval;
        public int PendingSyncCount => _pendingSyncs.Count;
        public int LastSyncFrame => _lastSyncFrame;
        public bool IsInitialized => _isInitialized;
        
        // 事件
        public event Action<ViewSyncManager> OnViewSyncManagerInitialized;
        public event Action<ViewSyncManager> OnViewSyncManagerShutdown;
        public event Action<SyncData> OnSyncDataProcessed;
        
        // 私有构造函�?
        private ViewSyncManager()
        {
        }
        
        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>ViewSyncManager实例</returns>
        public static ViewSyncManager GetInstance()
        {
            if (Instance == null)
            {
                Instance = new ViewSyncManager();
            }
            return Instance;
        }
        
        /// <summary>
        /// 初始化视图同步管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                ASLogger.Instance.Warning("ViewSyncManager: 已经初始化，跳过重复初始化");
                return;
            }
            
            ASLogger.Instance.Info("ViewSyncManager: 初始化开�?..");
            
            try
            {
                _isInitialized = true;
                OnViewSyncManagerInitialized?.Invoke(this);
                
                ASLogger.Instance.Info("ViewSyncManager: 初始化完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ViewSyncManager: 初始化失�?- {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新同步管理�?
        /// </summary>
        /// <param name="deltaTime">帧时�?/param>
        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;
            
            _lastSyncTime += deltaTime;
            
            // 检查是否需要同�?
            if (_lastSyncTime >= _syncInterval)
            {
                ProcessSyncQueue();
                _lastSyncTime = 0f;
            }
        }
        
        /// <summary>
        /// 同步Stage与Room
        /// </summary>
        /// <param name="stage">Stage</param>
        /// <param name="roomData">房间数据</param>
        public void SyncStageWithRoom(Stage stage, RoomData roomData)
        {
            if (!_isInitialized || stage == null || roomData == null)
            {
                ASLogger.Instance.Warning("ViewSyncManager: 同步Stage与Room失败 - 参数无效");
                return;
            }
            
            try
            {
                stage.SyncWithRoom(roomData);
                ASLogger.Instance.Debug($"ViewSyncManager: 同步Stage与Room - {stage.StageName}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ViewSyncManager: 同步Stage与Room失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 同步实体视图与实�?
        /// </summary>
        /// <param name="entityView">实体视图</param>
        /// <param name="entityData">实体数据</param>
        public void SyncEntityViewWithEntity(EntityView entityView, EntityData entityData)
        {
            if (!_isInitialized || entityView == null || entityData == null)
            {
                ASLogger.Instance.Warning("ViewSyncManager: 同步实体视图与实体失�?- 参数无效");
                return;
            }
            
            try
            {
                entityView.SyncWithEntity(entityData);
                ASLogger.Instance.Debug($"ViewSyncManager: 同步实体视图与实�?- ID: {entityData.EntityId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ViewSyncManager: 同步实体视图与实体失�?- {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理同步队列
        /// </summary>
        public void ProcessSyncQueue()
        {
            if (!_isInitialized) return;
            
            int processedCount = 0;
            
            while (_pendingSyncs.Count > 0)
            {
                SyncData syncData = _pendingSyncs.Dequeue();
                
                try
                {
                    ProcessSyncData(syncData);
                    processedCount++;
                    
                    OnSyncDataProcessed?.Invoke(syncData);
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"ViewSyncManager: 处理同步数据失败 - {ex.Message}");
                }
            }
            
            if (processedCount > 0)
            {
                _lastSyncFrame++;
                ASLogger.Instance.Debug($"ViewSyncManager: 处理同步队列 - 处理数量: {processedCount}");
            }
        }
        
        /// <summary>
        /// 调度同步
        /// </summary>
        /// <param name="syncData">同步数据</param>
        public void ScheduleSync(SyncData syncData)
        {
            if (!_isInitialized || syncData == null)
            {
                ASLogger.Instance.Warning("ViewSyncManager: 调度同步失败 - 参数无效");
                return;
            }
            
            // 设置时间�?
            syncData.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _pendingSyncs.Enqueue(syncData);
            
            ASLogger.Instance.Debug($"ViewSyncManager: 调度同步 - 实体ID: {syncData.EntityId}");
        }
        
        /// <summary>
        /// 设置同步间隔
        /// </summary>
        /// <param name="interval">同步间隔（秒�?/param>
        public void SetSyncInterval(float interval)
        {
            _syncInterval = Mathf.Max(0.001f, interval);
            ASLogger.Instance.Info($"ViewSyncManager: 设置同步间隔 - {_syncInterval:F3}秒");
        }
        
        /// <summary>
        /// 清空同步队列
        /// </summary>
        public void ClearSyncQueue()
        {
            _pendingSyncs.Clear();
            ASLogger.Instance.Info("ViewSyncManager: 清空同步队列");
        }
        
        /// <summary>
        /// 获取同步统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public string GetSyncStatistics()
        {
            return $"同步间隔: {_syncInterval:F3}s, 待处理同�? {_pendingSyncs.Count}, 最后同步帧: {_lastSyncFrame}";
        }
        
        /// <summary>
        /// 关闭视图同步管理�?
        /// </summary>
        public void Shutdown()
        {
            ASLogger.Instance.Info("ViewSyncManager: 开始关�?..");
            
            // 清空同步队列
            ClearSyncQueue();
            
            _isInitialized = false;
            
            OnViewSyncManagerShutdown?.Invoke(this);
            
            ASLogger.Instance.Info("ViewSyncManager: 关闭完成");
        }
        
        /// <summary>
        /// 处理同步数据
        /// </summary>
        /// <param name="syncData">同步数据</param>
        private void ProcessSyncData(SyncData syncData)
        {
            if (syncData == null) return;
            
            // 查找对应的实体视�?
            Stage currentStage = ViewManager.GetInstance().CurrentStage;
            if (currentStage == null)
            {
                ASLogger.Instance.Warning($"ViewSyncManager: 无法处理同步数据 - 当前Stage为空，实体ID: {syncData.EntityId}");
                return;
            }
            
            EntityView entityView = currentStage.GetEntityView(syncData.EntityId);
            if (entityView == null)
            {
                ASLogger.Instance.Warning($"ViewSyncManager: 无法处理同步数据 - 实体视图不存在，实体ID: {syncData.EntityId}");
                return;
            }
            
            // 创建实体数据
            EntityData entityData = new EntityData
            {
                EntityId = syncData.EntityId,
                Position = syncData.Position,
                Rotation = syncData.Rotation,
                Scale = syncData.Scale,
                ComponentData = syncData.ComponentData
            };
            
            // 同步实体视图
            SyncEntityViewWithEntity(entityView, entityData);
        }
        
        /// <summary>
        /// 创建同步数据
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <param name="scale">缩放</param>
        /// <param name="componentData">组件数据</param>
        /// <returns>同步数据</returns>
        public static SyncData CreateSyncData(long entityId, Vector3 position, Quaternion rotation, Vector3 scale, Dictionary<string, object> componentData = null)
        {
            return new SyncData
            {
                EntityId = entityId,
                Position = position,
                Rotation = rotation,
                Scale = scale,
                ComponentData = componentData ?? new Dictionary<string, object>(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
        
        /// <summary>
        /// 批量同步实体
        /// </summary>
        /// <param name="entityDataList">实体数据列表</param>
        public void BatchSyncEntities(List<EntityData> entityDataList)
        {
            if (!_isInitialized || entityDataList == null)
            {
                ASLogger.Instance.Warning("ViewSyncManager: 批量同步实体失败 - 参数无效");
                return;
            }
            
            foreach (var entityData in entityDataList)
            {
                SyncData syncData = CreateSyncData(
                    entityData.EntityId,
                    entityData.Position,
                    entityData.Rotation,
                    entityData.Scale,
                    entityData.ComponentData
                );
                
                ScheduleSync(syncData);
            }
            
            ASLogger.Instance.Debug($"ViewSyncManager: 批量同步实体 - 数量: {entityDataList.Count}");
        }
    }
} 
