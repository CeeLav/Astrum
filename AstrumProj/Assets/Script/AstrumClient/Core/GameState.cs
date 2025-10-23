using System;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        ApplicationStarting,    // 应用启动中
        ApplicationReady,      // 应用就绪
        GameMenu,              // 游戏菜单
        GameLoading,           // 游戏加载中
        GamePlaying,           // 游戏进行中
        GamePaused,            // 游戏暂停
        GameEnding,            // 游戏结束中
        SystemShutdown         // 系统关闭
    }
    
    /// <summary>
    /// 游戏模式状态枚举
    /// </summary>
    public enum GameModeState
    {
        Initializing,    // 初始化中
        Loading,         // 加载中
        Ready,          // 准备就绪
        Playing,        // 游戏中
        Paused,         // 暂停
        Ending,         // 结束中
        Finished        // 已结束
    }
}
