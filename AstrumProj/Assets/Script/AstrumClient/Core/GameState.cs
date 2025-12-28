namespace Astrum.Client.Core
{
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
