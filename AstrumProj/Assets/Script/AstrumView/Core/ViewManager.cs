using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;

namespace Astrum.View.Core
{
    /// <summary>
    /// 视图管理器 - 负责管理游戏视图层的核心逻辑
    /// </summary>
    public class ViewManager : Singleton<ViewManager>
    {
        // 视图管理器设置
        private bool _enableLogging = true;
        private bool _isInitialized;
        private bool _isUpdating;
        private float _deltaTime;
        private float _lastFrameTime;
        
        // Stage管理
        private Stage _currentStage;
        private Dictionary<string, Stage> _stageCache = new Dictionary<string, Stage>();
        private List<Stage> _stageHistory = new List<Stage>();
        
        // 公共属性
        public Stage CurrentStage => _currentStage;
        public new bool IsInitialized => _isInitialized;
        public bool IsUpdating => _isUpdating;
        public float DeltaTime => _deltaTime;
        
        // 事件
        public event Action<Stage> OnStageChanged;
        public event Action<Stage> OnStageLoaded;
        public event Action<Stage> OnStageUnloaded;
        
        /// <summary>
        /// 获取ViewManager单例实例
        /// </summary>
        /// <returns>ViewManager实例</returns>
        public static ViewManager GetInstance()
        {
            return Instance;
        }
        
        /// <summary>
        /// 初始化视图管理器
        /// </summary>
        public void Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.LogWarning("ViewManager: 视图管理器已经初始化");
                    return;
                }
                
                if (_enableLogging)
                    Debug.Log("ViewManager: 开始初始化视图管理器");
                
                // 初始化基础组件
                InitializeComponents();
                
                // 预加载常用Stage
                PreloadCommonStages();
                
                _isInitialized = true;
                
                Debug.Log("ViewManager: 初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ViewManager: 初始化失败 - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新视图管理器
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public void Update(float deltaTime)
        {
            if (!_isInitialized || _isUpdating) return;
            
            _isUpdating = true;
            _deltaTime = deltaTime;
            
            try
            {
                // 更新当前Stage
                _currentStage?.Update(deltaTime);
            }
            finally
            {
                _isUpdating = false;
            }
        }
        
        /// <summary>
        /// 切换到指定Stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="addToHistory">是否添加到历史记录</param>
        public void ChangeStage(string stageId, bool addToHistory = true)
        {
            if (string.IsNullOrEmpty(stageId))
            {
                Debug.LogError("ViewManager: Stage ID不能为空");
                return;
            }
            
            if (_enableLogging)
                Debug.Log($"ViewManager: 切换到Stage {stageId}");
            
            // 获取或创建Stage
            Stage targetStage = GetOrCreateStage(stageId);
            if (targetStage == null)
            {
                Debug.LogError($"ViewManager: 无法创建Stage {stageId}");
                return;
            }
            
            // 停用当前Stage
            if (_currentStage != null)
            {
                _currentStage.SetActive(false);
                
                if (addToHistory)
                {
                    _stageHistory.Add(_currentStage);
                }
            }
            
            // 激活新Stage
            _currentStage = targetStage;
            _currentStage.SetActive(true);
            
            // 触发事件
            OnStageChanged?.Invoke(_currentStage);
        }
        
        /// <summary>
        /// 获取或创建Stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Stage实例</returns>
        private Stage GetOrCreateStage(string stageId)
        {
            if (_stageCache.TryGetValue(stageId, out Stage stage))
            {
                return stage;
            }
            
            // Stage是抽象类，不能直接实例化
            // 返回null，由调用者决定如何处理
            ASLogger.Instance.Warning($"ViewManager: Stage '{stageId}' 未找到且无法创建抽象Stage实例");
            return null;
        }
        
        /// <summary>
        /// 预加载常用Stage
        /// </summary>
        private void PreloadCommonStages()
        {
            // TODO: 实现预加载逻辑
        }
        
        /// <summary>
        /// 初始化基础组件
        /// </summary>
        private void InitializeComponents()
        {
            // TODO: 实现基础组件初始化逻辑
        }
    }
}
