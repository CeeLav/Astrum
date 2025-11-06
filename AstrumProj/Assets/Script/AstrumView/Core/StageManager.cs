using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.View.Core
{
    /// <summary>
    /// Stage管理器 - 负责管理所有游戏Stage
    /// </summary>
    public class StageManager : Singleton<StageManager>
    {
        private Dictionary<string, Stage> _stages;
        private Stage _currentStage;
        private Stage _previousStage;
        
        // 公共属性
        public Stage CurrentStage => _currentStage;
        public Stage PreviousStage => _previousStage;
        public Dictionary<string, Stage> AllStages => _stages;
        
        protected override void Awake()
        {
            base.Awake();
            _stages = new Dictionary<string, Stage>();
            
        }
        
        /// <summary>
        /// 注册Stage
        /// </summary>
        public void RegisterStage(Stage stage)
        {
            if (stage == null)
            {
                ASLogger.Instance.Warning("StageManager: 尝试注册空Stage");
                return;
            }
            
            if (_stages.ContainsKey(stage.StageId))
            {
                ASLogger.Instance.Warning($"StageManager: Stage已存在，ID: {stage.StageId}");
                return;
            }
            
            _stages[stage.StageId] = stage;
            ASLogger.Instance.Info($"StageManager: 注册Stage - {stage.StageName}");
        }
        
        /// <summary>
        /// 注销Stage
        /// </summary>
        public void UnregisterStage(string stageId)
        {
            if (!_stages.ContainsKey(stageId))
            {
                ASLogger.Instance.Warning($"StageManager: Stage不存在，ID: {stageId}");
                return;
            }
            
            Stage stage = _stages[stageId];
            if (stage == _currentStage)
            {
                ASLogger.Instance.Warning($"StageManager: 不能注销当前活跃Stage，ID: {stageId}");
                return;
            }
            
            _stages.Remove(stageId);
            ASLogger.Instance.Info($"StageManager: 注销Stage - {stage.StageName}");
        }
        
        /// <summary>
        /// 设置当前Stage
        /// </summary>
        public void SetCurrentStage(Stage newStage)
        {
            if (newStage == _currentStage) return;
            
            ASLogger.Instance.Info($"StageManager: 切换Stage {_currentStage?.StageName ?? "None"} -> {newStage?.StageName ?? "None"}");
            
            // 退出当前Stage
            if (_currentStage != null)
            {
                _currentStage.OnExit();
                _currentStage.SetActive(false);
                _previousStage = _currentStage;
            }
            
            // 设置新Stage
            _currentStage = newStage;
            
            // 进入新Stage
            if (_currentStage != null)
            {
                // 自动注册新Stage
                if (!_stages.ContainsKey(_currentStage.StageId))
                {
                    RegisterStage(_currentStage);
                }
                
                _currentStage.SetActive(true);
                _currentStage.OnEnter();
            }
        }
        
        /// <summary>
        /// 根据ID获取Stage
        /// </summary>
        public Stage GetStage(string stageId)
        {
            _stages.TryGetValue(stageId, out Stage stage);
            return stage;
        }
        
        /// <summary>
        /// 根据类型获取Stage
        /// </summary>
        public T GetStage<T>() where T : Stage
        {
            foreach (var stage in _stages.Values)
            {
                if (stage is T targetStage)
                {
                    return targetStage;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 切换到指定ID的Stage
        /// </summary>
        public bool SwitchToStage(string stageId)
        {
            Stage targetStage = GetStage(stageId);
            if (targetStage == null)
            {
                ASLogger.Instance.Warning($"StageManager: 找不到Stage，ID: {stageId}");
                return false;
            }
            
            SetCurrentStage(targetStage);
            return true;
        }
        
        /// <summary>
        /// 返回上一个Stage
        /// </summary>
        public bool SwitchToPreviousStage()
        {
            if (_previousStage == null)
            {
                ASLogger.Instance.Warning("StageManager: 没有上一个Stage");
                return false;
            }
            
            SetCurrentStage(_previousStage);
            return true;
        }
        
        /// <summary>
        /// 更新所有Stage
        /// </summary>
        public void Update(float deltaTime)
        {
            // 更新当前Stage
            if (_currentStage != null && _currentStage.IsActive)
            {
                _currentStage.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// 清空所有Stage
        /// </summary>
        public void ClearAllStages()
        {
            ASLogger.Instance.Info("StageManager: 清空所有Stage");
            
            // 退出当前Stage
            if (_currentStage != null)
            {
                _currentStage.OnExit();
                _currentStage.SetActive(false);
            }
            
            // 清空所有Stage
            _stages.Clear();
            _currentStage = null;
            _previousStage = null;
        }
    }
}
