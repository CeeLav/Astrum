using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.View.Core;

namespace Astrum.View.Managers
{
    /// <summary>
    /// 同步数据类
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
    /// 房间数据类 - 用于同步
    /// </summary>
    [Serializable]
    public class RoomData
    {
        public long RoomId { get; set; }
        public List<long> EntityIds { get; set; } = new List<long>(); // 只存储Entity的UniqueId
        public Dictionary<string, object> RoomSettings { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// 视图同步管理器
    /// 负责管理表现层与逻辑层的同步
    /// </summary>
    public class ViewSyncManager
    {
        // 单例实例
        public static ViewSyncManager Instance { get; private set; }
        
        // 同步设置
        private float _syncInterval = 0.016f; // 60fps
        private Queue<long> _pendingSyncEntityIds = new Queue<long>(); // 只存储需要同步的EntityId
        private int _lastSyncFrame = 0;
        
        // 状态管理
        private bool _isInitialized = false;
        private float _lastSyncTime = 0f;
        
        // 公共属性
        public float SyncInterval => _syncInterval;
        public int PendingSyncCount => _pendingSyncEntityIds.Count;
        public int LastSyncFrame => _lastSyncFrame;
        public bool IsInitialized => _isInitialized;
        
        // 事件
        public event Action<ViewSyncManager> OnViewSyncManagerInitialized;
        public event Action<ViewSyncManager> OnViewSyncManagerShutdown;
        public event Action<long> OnEntitySyncProcessed; // 只传递EntityId
        
        // 静态构造函数确保单例
        static ViewSyncManager()
        {
            Instance = new ViewSyncManager();
        }
        
        // 私有构造函数
        private ViewSyncManager()
        {
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
            
            ASLogger.Instance.Info("ViewSyncManager: 初始化开始..");
            
            try
            {
                _isInitialized = true;
                OnViewSyncManagerInitialized?.Invoke(this);
                
                ASLogger.Instance.Info("ViewSyncManager: 初始化完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ViewSyncManager: 初始化失败 - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新同步管理器
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;
            
            _lastSyncTime += deltaTime;
            
            // 检查是否需要同步
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
                // 设置Room ID
                stage.SetRoomId(roomData.RoomId);
                
                // 同步实体ID列表
                SyncEntitiesWithStage(stage, roomData.EntityIds);
                
                ASLogger.Instance.Debug($"ViewSyncManager: 同步Stage与Room - {stage.StageName}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ViewSyncManager: 同步Stage与Room失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 同步实体ID列表到Stage
        /// </summary>
        private void SyncEntitiesWithStage(Stage stage, List<long> entityIds)
        {
            foreach (var entityId in entityIds)
            {
                // 将EntityId加入同步队列，实际的数据获取在EntityView中处理
                ScheduleEntitySync(entityId);
            }
        }
        
        /// <summary>
        /// 处理同步队列
        /// </summary>
        public void ProcessSyncQueue()
        {
            if (!_isInitialized) return;
            
            int processedCount = 0;
            
            while (_pendingSyncEntityIds.Count > 0)
            {
                long entityId = _pendingSyncEntityIds.Dequeue();
                
                try
                {
                    ProcessEntitySync(entityId);
                    processedCount++;
                    
                    OnEntitySyncProcessed?.Invoke(entityId);
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"ViewSyncManager: 处理实体同步失败 - EntityId: {entityId}, {ex.Message}");
                }
            }
            
            if (processedCount > 0)
            {
                _lastSyncFrame++;
                ASLogger.Instance.Debug($"ViewSyncManager: 处理同步队列 - 处理数量: {processedCount}");
            }
        }
        
        /// <summary>
        /// 调度实体同步
        /// </summary>
        /// <param name="entityId">实体ID</param>
        public void ScheduleEntitySync(long entityId)
        {
            if (!_isInitialized)
            {
                ASLogger.Instance.Warning("ViewSyncManager: 调度同步失败 - 管理器未初始化");
                return;
            }
            
            _pendingSyncEntityIds.Enqueue(entityId);
            ASLogger.Instance.Debug($"ViewSyncManager: 调度实体同步 - Entity ID: {entityId}");
        }
        
        /// <summary>
        /// 处理单个实体同步
        /// </summary>
        private void ProcessEntitySync(long entityId)
        {
            // 这里可以根据实际需求处理实体同步
            // 实际的数据获取和同步将在EntityView中处理
            ASLogger.Instance.Debug($"ViewSyncManager: 处理实体同步 - Entity ID: {entityId}");
        }
        
        /// <summary>
        /// 设置同步间隔
        /// </summary>
        public void SetSyncInterval(float interval)
        {
            if (interval > 0)
            {
                _syncInterval = interval;
                ASLogger.Instance.Info($"ViewSyncManager: 设置同步间隔 = {interval}");
            }
        }
        
        /// <summary>
        /// 关闭同步管理器
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized)
            {
                ASLogger.Instance.Warning("ViewSyncManager: 未初始化，跳过关闭操作");
                return;
            }
            
            ASLogger.Instance.Info("ViewSyncManager: 关闭同步管理器");
            
            // 清理资源
            _pendingSyncEntityIds.Clear();
            _isInitialized = false;
            
            OnViewSyncManagerShutdown?.Invoke(this);
        }
    }
}
