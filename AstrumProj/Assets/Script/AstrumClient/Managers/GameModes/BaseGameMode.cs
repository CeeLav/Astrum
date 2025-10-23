using System;
using Astrum.LogicCore.Core;
using Astrum.View.Core;
using Astrum.CommonBase;
using Astrum.Client.Core;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 游戏模式抽象基类 - 提供通用实现
    /// </summary>
    public abstract class BaseGameMode : IGameMode
    {
        protected GameModeState _currentState = GameModeState.Initializing;
        protected GameModeConfig _config;
        
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
        public abstract void StartGame(string sceneName);
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
    }
}
