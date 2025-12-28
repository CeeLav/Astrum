using Astrum.CommonBase;
using Astrum.Client.Managers.GameModes;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 游戏模式变化事件数据
    /// </summary>
    public class GameModeChangedEventData : EventData
    {
        public IGameMode PreviousMode { get; set; }
        public IGameMode NewMode { get; set; }
        
        public GameModeChangedEventData(IGameMode previousMode, IGameMode newMode)
        {
            PreviousMode = previousMode;
            NewMode = newMode;
        }
    }
    
    /// <summary>
    /// 游戏模式状态变化事件数据
    /// </summary>
    public class GameModeStateChangedEventData : EventData
    {
        public GameModeState PreviousState { get; set; }
        public GameModeState NewState { get; set; }
        
        public GameModeStateChangedEventData(GameModeState previousState, GameModeState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
    
    /// <summary>
    /// 游戏模式配置变化事件数据
    /// </summary>
    public class GameModeConfigChangedEventData : EventData
    {
        public GameModeConfig Config { get; set; }
        
        public GameModeConfigChangedEventData(GameModeConfig config)
        {
            Config = config;
        }
    }
}
