using System;
using System.Collections.Generic;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
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
                authorityFrame.CopyTo(predictionFrame);
            }

            predictionFrame.Inputs[MainPlayerId] = ClientInput;
            
            return predictionFrame;
        }

    }
}
