using Astrum.Generated;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 服务器帧同步接口 - 包含服务器权威帧推进功能
    /// </summary>
    public interface IServerFrameSync : ILSControllerBase
    {
        /// <summary>
        /// 添加玩家输入到缓存
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="input">输入数据</param>
        void AddPlayerInput(int frame, long playerId, LSInput input);
        
        /// <summary>
        /// 收集指定帧的所有玩家输入（从输入缓存中）
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <returns>该帧的所有玩家输入</returns>
        OneFrameInputs CollectFrameInputs(int frame);
    }
}

