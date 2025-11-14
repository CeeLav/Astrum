using Astrum.Generated;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 客户端帧同步接口 - 包含客户端预测和回滚功能
    /// </summary>
    public interface IClientFrameSync : ILSControllerBase
    {
        /// <summary>
        /// 预测帧
        /// </summary>
        int PredictionFrame { get; set; }
        
        /// <summary>
        /// 最大预测帧数
        /// </summary>
        int MaxPredictionFrames { get; set; }
        
        /// <summary>
        /// 设置玩家输入
        /// </summary>
        void SetPlayerInput(long playerId, LSInput input);
        
        /// <summary>
        /// 设置服务器广播的帧输入（包含回滚逻辑）
        /// </summary>
        void SetOneFrameInputs(OneFrameInputs inputs);
        
        /// <summary>
        /// 回滚到指定帧
        /// </summary>
        void Rollback(int frame);
        
        /// <summary>
        /// 获取当前预测帧对应的时间
        /// </summary>
        long GetCurrentPredictionFrameTime();
        
        /// <summary>
        /// 获取指定预测帧对应的时间
        /// </summary>
        long GetPredictionFrameTime(int predictionFrame);
    }
}

