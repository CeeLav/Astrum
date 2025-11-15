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

        public static bool Equal (this OneFrameInputs a, OneFrameInputs b)
        {
            if (a is null || b is null)
            {
                if (a is null && b is null)
                {
                    return true;
                }
                return false;
            }
            
            if (a.Inputs.Count != b.Inputs.Count)
            {
                return false;
            }

            foreach (var kv in a.Inputs)
            {
                if (!b.Inputs.TryGetValue(kv.Key, out LSInput inputInfo))
                {
                    return false;
                }

                if (!kv.Value.Equal(inputInfo))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 比较两个 LSInput 是否相等
        /// </summary>
        /// <param name="a">第一个输入</param>
        /// <param name="b">第二个输入</param>
        /// <returns>是否相等</returns>
        public static bool Equal(this LSInput a, LSInput b)
        {
            if (a is null || b is null)
            {
                if (a is null && b is null)
                {
                    return true;
                }
                return false;
            }

            return a.PlayerId == b.PlayerId &&
                   a.MoveX == b.MoveX &&
                   a.MoveY == b.MoveY &&
                   a.Attack == b.Attack &&
                   a.Skill1 == b.Skill1 &&
                   a.Skill2 == b.Skill2 &&
                   //a.Timestamp == b.Timestamp &&
                   a.BornInfo == b.BornInfo;
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
        
        public ILSControllerBase? LSController { get; set; }
        
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
            
            // 只在有实际输入时输出日志
            if (ClientInput != null && HasActualInput(ClientInput))
            {
                ASLogger.Instance.Debug($"Predict Frame: {frame}, MoveX={ClientInput.MoveX}, MoveY={ClientInput.MoveY}", "FrameSync.Prediction");
            }
            
            return predictionFrame;
        }
        
        /// <summary>
        /// 检查输入是否有实际动作（非空输入）
        /// </summary>
        private static bool HasActualInput(LSInput input)
        {
            if (input == null) return false;
            
            // 检查移动输入
            if (input.MoveX != 0 || input.MoveY != 0)
                return true;
            
            // 检查按钮输入
            if (input.Attack || input.Skill1 || input.Skill2 || input.Roll || input.Dash)
                return true;
            
            // 检查鼠标位置输入
            if (input.MouseWorldX != 0 || input.MouseWorldZ != 0)
                return true;
            
            return false;
        }

    }
}
