using System;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 帧同步输入数据
    /// </summary>
    public class LSInput
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public long PlayerId { get; set; }

        /// <summary>
        /// 帧号
        /// </summary>
        public int Frame { get; set; }

        /// <summary>
        /// X轴移动输入
        /// </summary>
        public float MoveX { get; set; }

        /// <summary>
        /// Y轴移动输入
        /// </summary>
        public float MoveY { get; set; }

        /// <summary>
        /// 攻击输入
        /// </summary>
        public bool Attack { get; set; }

        /// <summary>
        /// 技能1输入
        /// </summary>
        public bool Skill1 { get; set; }

        /// <summary>
        /// 技能2输入
        /// </summary>
        public bool Skill2 { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp { get; set; }

        public LSInput()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 克隆输入数据
        /// </summary>
        /// <returns>克隆的输入数据</returns>
        public LSInput Clone()
        {
            return new LSInput
            {
                PlayerId = PlayerId,
                Frame = Frame,
                MoveX = MoveX,
                MoveY = MoveY,
                Attack = Attack,
                Skill1 = Skill1,
                Skill2 = Skill2,
                Timestamp = Timestamp
            };
        }

        /// <summary>
        /// 检查是否为空输入
        /// </summary>
        /// <returns>是否为空输入</returns>
        public bool IsEmpty()
        {
            return MoveX == 0 && MoveY == 0 && !Attack && !Skill1 && !Skill2;
        }

        /// <summary>
        /// 获取移动输入的强度
        /// </summary>
        /// <returns>移动强度(0-1)</returns>
        public float GetMoveInputMagnitude()
        {
            return (float)Math.Sqrt(MoveX * MoveX + MoveY * MoveY);
        }

        /// <summary>
        /// 获取标准化的移动向量
        /// </summary>
        /// <returns>标准化的移动向量</returns>
        public (float x, float y) GetNormalizedMoveVector()
        {
            float magnitude = GetMoveInputMagnitude();
            if (magnitude > 0)
            {
                return (MoveX / magnitude, MoveY / magnitude);
            }
            return (0, 0);
        }
    }
}
