using Astrum.LogicCore.Core;
using Astrum.View.Core;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 游戏模式接口 - 定义所有游戏模式的通用行为
    /// </summary>
    public interface IGameMode
    {
        /// <summary>
        /// 初始化游戏模式
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 启动游戏
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        void StartGame(string sceneName);
        
        /// <summary>
        /// 更新游戏逻辑
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        void Update(float deltaTime);
        
        /// <summary>
        /// 关闭游戏模式
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// 主游戏房间
        /// </summary>
        Room MainRoom { get; }
        
        /// <summary>
        /// 主游戏舞台
        /// </summary>
        Stage MainStage { get; }
        
        /// <summary>
        /// 主玩家ID
        /// </summary>
        long PlayerId { get; }
        
        /// <summary>
        /// 模式名称
        /// </summary>
        string ModeName { get; }
        
        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }
    }
}

