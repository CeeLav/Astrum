using System;
using Astrum.CommonBase;

namespace Astrum.LogicCore
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        None,
        Loading,
        MainMenu,
        InGame,
        Paused,
        GameOver
    }

    /// <summary>
    /// 游戏状态变更事件
    /// </summary>
    public class GameStateChangedEvent : EventData
    {
        public GameState OldState { get; }
        public GameState NewState { get; }

        public GameStateChangedEvent(GameState oldState, GameState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// 游戏状态管理器 - 与Unity解耦的核心逻辑
    /// </summary>
    public class GameStateManager
    {
        private static GameStateManager _instance;

        public static GameStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameStateManager();
                }

                return _instance;
            }
        }

        private GameState _currentState = GameState.None;

        /// <summary>
        /// 当前游戏状态
        /// </summary>
        public GameState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    var previousState = _currentState;
                    _currentState = value;
                    OnStateChanged?.Invoke(previousState, _currentState);

                    // 发布事件到事件系统
                    EventSystem.Instance?.Publish(new GameStateChangedEvent(previousState, _currentState));
                }
            }
        }

        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        /// <summary>
        /// 游戏配置
        /// </summary>
        public GameConfig Config { get; private set; }

        /// <summary>
        /// 玩家管理器
        /// </summary>
        public PlayerManager PlayerManager { get; private set; }

        private GameStateManager()
        {
            Config = new GameConfig();
            PlayerManager = new PlayerManager();
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        public void Initialize()
        {
            CurrentState = GameState.Loading;
            PlayerManager.Initialize();
            CurrentState = GameState.MainMenu;
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (CurrentState == GameState.MainMenu)
            {
                CurrentState = GameState.InGame;
            }
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (CurrentState == GameState.InGame)
            {
                CurrentState = GameState.Paused;
            }
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.InGame;
            }
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame()
        {
            CurrentState = GameState.GameOver;
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            CurrentState = GameState.MainMenu;
        }

        /// <summary>
        /// 更新游戏逻辑
        /// </summary>
        public void Update(float deltaTime)
        {
            PlayerManager.Update(deltaTime);
        }

        /// <summary>
        /// 设置游戏状态
        /// </summary>
        /// <param name="newState">新的游戏状态</param>
        public void SetGameState(GameState newState)
        {
            CurrentState = newState;
        }
    }
}
