using System;
using Astrum.LogicCore.Core;
using Astrum.View.Core;
using Astrum.CommonBase;
using Astrum.Client.Core;
using UnityEngine;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 游戏模式抽象基类 - 提供通用实现
    /// </summary>
    public abstract class BaseGameMode : IGameMode
    {
        protected GameModeState _currentState = GameModeState.Initializing;
        protected GameModeConfig _config;
        
        /// <summary>
        /// 逻辑专用线程（多线程模式下使用）
        /// </summary>
        protected LogicThread _logicThread;
        
        /// <summary>
        /// 是否启用逻辑线程（从 GameSetting 读取）
        /// </summary>
        protected bool IsLogicThreadEnabled => GameSetting.Instance?.EnableLogicThread ?? false;
        
        /// <summary>
        /// 逻辑线程帧率（从 GameSetting 读取）
        /// </summary>
        protected int LogicThreadTickRate => GameSetting.Instance?.LogicThreadTickRate ?? 20;
        
        // 状态管理
        public GameModeState CurrentState => _currentState;
        
        /// <summary>
        /// 改变状态
        /// </summary>
        protected virtual void ChangeState(GameModeState newState)
        {
            if (!CanTransitionTo(newState)) 
            {
                ASLogger.Instance.Warning($"BaseGameMode: 无法从 {_currentState} 转换到 {newState}");
                return;
            }
            
            var previousState = _currentState;
            OnStateExit(previousState);
            _currentState = newState;
            OnStateEnter(newState);
            
            // 使用现有 EventSystem 发布事件
            OnGameEvent(new GameModeStateChangedEventData(previousState, newState));
            
            ASLogger.Instance.Info($"BaseGameMode: 状态从 {previousState} 变为 {newState}");
        }
        
        public virtual bool CanTransitionTo(GameModeState targetState)
        {
            return _currentState switch
            {
                GameModeState.Initializing => targetState == GameModeState.Loading || targetState == GameModeState.Initializing || targetState == GameModeState.Ready,
                GameModeState.Loading => targetState == GameModeState.Ready,
                GameModeState.Ready => targetState == GameModeState.Playing,
                GameModeState.Playing => targetState == GameModeState.Paused || targetState == GameModeState.Ending,
                GameModeState.Paused => targetState == GameModeState.Playing || targetState == GameModeState.Ending,
                GameModeState.Ending => targetState == GameModeState.Finished,
                GameModeState.Finished => false,
                _ => false
            };
        }
        
        // 事件处理（基于现有 EventSystem）
        public virtual void OnGameEvent(EventData eventData)
        {
            EventSystem.Instance.Publish(eventData);
        }
        
        public void RegisterEventHandler<T>(Action<T> handler) where T : EventData
        {
            EventSystem.Instance.Subscribe(handler);
        }
        
        public void UnregisterEventHandler<T>(Action<T> handler) where T : EventData
        {
            EventSystem.Instance.Unsubscribe(handler);
        }
        
        // 配置管理
        public virtual GameModeConfig GetConfig()
        {
            return _config ??= CreateDefaultConfig();
        }
        
        public virtual void ApplyConfig(GameModeConfig config)
        {
            _config = config;
            OnGameEvent(new GameModeConfigChangedEventData(config));
        }
        
        public virtual void SaveConfig()
        {
            // 子类可以重写此方法实现具体的保存逻辑
            ASLogger.Instance.Info($"BaseGameMode: 保存配置 - {ModeName}");
        }
        
        public virtual void LoadConfig()
        {
            // 子类可以重写此方法实现具体的加载逻辑
            ASLogger.Instance.Info($"BaseGameMode: 加载配置 - {ModeName}");
        }
        
        protected abstract GameModeConfig CreateDefaultConfig();
        
        // 抽象方法，子类必须实现
        public abstract void Initialize();
        public abstract void StartGame(int sceneId);
        public abstract void Update(float deltaTime);
        public abstract void Shutdown();
        public abstract Room MainRoom { get; set; }
        public abstract Stage MainStage { get; set; }
        public abstract long PlayerId { get; set; }
        public abstract string ModeName { get; }
        public abstract bool IsRunning { get; set; }
        
        // 虚方法，子类可以重写
        public virtual void OnStateEnter(GameModeState state) 
        {
            ASLogger.Instance.Info($"BaseGameMode: 进入状态 {state} - {ModeName}");
        }
        
        public virtual void OnStateExit(GameModeState state) 
        {
            ASLogger.Instance.Info($"BaseGameMode: 退出状态 {state} - {ModeName}");
        }
        
        #region 逻辑线程辅助方法
        
        /// <summary>
        /// 启动逻辑线程（如果配置启用）
        /// </summary>
        /// <param name="room">关联的房间</param>
        /// <returns>是否启动了逻辑线程</returns>
        protected bool StartLogicThread(Room room)
        {
            if (!IsLogicThreadEnabled)
            {
                ASLogger.Instance.Info($"{ModeName}: 单线程模式（向后兼容）");
                return false;
            }
            
            if (room == null)
            {
                ASLogger.Instance.Warning($"{ModeName}: Room 为空，无法启动逻辑线程");
                return false;
            }
            
            try
            {
                // 设置多线程模式标志，通知 View 层不要直接访问 Logic 层的脏标记
                room.IsMultiThreadMode = true;
                
                _logicThread = new LogicThread(room, LogicThreadTickRate);
                _logicThread.OnError += OnLogicThreadError;
                _logicThread.Start();
                ASLogger.Instance.Info($"{ModeName}: 逻辑线程已启动 - TickRate: {LogicThreadTickRate} FPS");
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"{ModeName}: 启动逻辑线程失败 - {ex.Message}");
                room.IsMultiThreadMode = false;
                _logicThread = null;
                return false;
            }
        }
        
        /// <summary>
        /// 停止逻辑线程（如果存在）
        /// </summary>
        protected void StopLogicThread()
        {
            if (_logicThread == null) return;
            
            try
            {
                _logicThread.OnError -= OnLogicThreadError;
                _logicThread.Stop();
                _logicThread.Dispose();
                ASLogger.Instance.Info($"{ModeName}: 逻辑线程已停止");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"{ModeName}: 停止逻辑线程失败 - {ex.Message}");
            }
            finally
            {
                _logicThread = null;
            }
        }
        
        /// <summary>
        /// 暂停逻辑线程
        /// </summary>
        protected void PauseLogicThread()
        {
            _logicThread?.Pause();
        }
        
        /// <summary>
        /// 恢复逻辑线程
        /// </summary>
        protected void ResumeLogicThread()
        {
            _logicThread?.Resume();
        }
        
        /// <summary>
        /// 逻辑线程错误处理
        /// </summary>
        protected virtual void OnLogicThreadError(Exception ex)
        {
            ASLogger.Instance.Error($"{ModeName}: 逻辑线程发生错误 - {ex.Message}");
            // 子类可以重写此方法实现特定的错误处理逻辑
        }
        
        #endregion
    }
}
