using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 帧同步控制器基础接口 - 包含客户端和服务器共同需要的功能
    /// </summary>
    public interface ILSControllerBase
    {
        /// <summary>
        /// 所属房间
        /// </summary>
        Room Room { get; set; }
        
        /// <summary>
        /// 权威帧（客户端读取，服务器写入）
        /// </summary>
        int AuthorityFrame { get; set; }
        
        /// <summary>
        /// 帧缓冲区
        /// </summary>
        FrameBuffer FrameBuffer { get; }
        
        /// <summary>
        /// 创建时间（毫秒）
        /// </summary>
        long CreationTime { get; set; }
        
        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }
        
        /// <summary>
        /// 是否暂停
        /// </summary>
        bool IsPaused { get; set; }
        
        /// <summary>
        /// 帧率（如60FPS）
        /// </summary>
        int TickRate { get; set; }
        
        /// <summary>
        /// 更新帧同步（客户端预测更新或服务器权威更新）
        /// </summary>
        void Tick();
        
        /// <summary>
        /// 启动控制器
        /// </summary>
        void Start();
        
        /// <summary>
        /// 停止控制器
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 保存当前帧状态
        /// </summary>
        void SaveState();
        
        /// <summary>
        /// 加载指定帧的状态
        /// </summary>
        World LoadState(int frame);
    }
}

