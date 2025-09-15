using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{

    public static class FrameUtils
    {
        public static void CopyTo(this OneFrameInputs from, OneFrameInputs to)
        {
            foreach (var input in from.Inputs)
            {
                to.Inputs[input.Key] = input.Value;
            }
        }
    }
    /// <summary>
    /// 帧同步输入系统
    /// </summary>
    public class LSInputSystem
    {
        /// <summary>
        /// 帧缓冲区
        /// </summary>
        public FrameBuffer FrameBuffer { get; private set; }
        
        public LSController? LSController { get; set; }
        
        public long MainPlayerId => LSController?.Room?.MainPlayerId ?? -1;
        
        public int AuthorityFrame => LSController?.AuthorityFrame ?? -1;
        
        /// <summary>
        /// 当前处理的帧号
        /// </summary>
        public int CurrentProcessingFrame { get; private set; } = 0;
        
        public LSInput ClientInput { get; set; } = new LSInput();
        
        /// <summary>
        /// 私有构造函数
        /// </summary>
        public LSInputSystem()
        {
            FrameBuffer = new FrameBuffer();
        }


        
        public OneFrameInputs GetOneFrameMessages( int frame)
        {
            
            if (frame <= AuthorityFrame)
            {
                return FrameBuffer.FrameInputs(frame);
            }
            
            // predict
            OneFrameInputs predictionFrame = FrameBuffer.FrameInputs(frame);
            
            FrameBuffer.MoveForward(frame);
            if (FrameBuffer.CheckFrame(AuthorityFrame))
            {
                OneFrameInputs authorityFrame = FrameBuffer.FrameInputs(AuthorityFrame);
                foreach (var authorityFrameInput in authorityFrame.Inputs)
                {
                    predictionFrame.Inputs[authorityFrameInput.Key] = authorityFrameInput.Value;
                }
            }

            if (MainPlayerId > 0)//客户端主玩家生成了才进行输入
            {
                predictionFrame.Inputs[MainPlayerId] = ClientInput;
            }
            ASLogger.Instance.Debug($"Predict Frame: {frame}, MoveX={ClientInput.MoveX}, MoveY={ClientInput.MoveY}", "FrameSync.Prediction");
            return predictionFrame;
        }

    }
}
