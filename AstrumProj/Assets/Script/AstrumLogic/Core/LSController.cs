using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.CommonBase;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 帧同步控制器，负责管理帧同步逻辑
    /// </summary>
    public class LSController
    {
        /// <summary>
        /// 所属房间
        /// </summary>
        public Room? Room { get; set; }

        /// <summary>
        /// 当前帧
        /// </summary>
        public int AuthorityFrame { get; set; } = -1;
        
        public int PredictionFrame { get; set; } = -1;

        /// <summary>
        /// 帧率（如60FPS）
        /// </summary>
        public int TickRate { get; set; } = 60;
        
        public long CreationTime { get; set; }

        /// <summary>
        /// 上次更新时间
        /// </summary>
        public long LastUpdateTime { get; private set; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; set; } = false;

        /// <summary>
        /// 输入系统
        /// </summary>
        private LSInputSystem _inputSystem;

        /// <summary>
        /// 状态历史（用于回滚）
        /// </summary>
        private Dictionary<int, byte[]> _stateHistory = new();

        /// <summary>
        /// 最大状态历史数量
        /// </summary>
        public int MaxStateHistory { get; set; } = 30;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        public LSController()
        {
            _inputSystem = new LSInputSystem();
            _inputSystem.LSController = this;
            LastUpdateTime = TimeInfo.Instance.ServerNow();
        }

        /// <summary>
        /// 执行一帧
        /// </summary>
        public void Tick()
        {
            if (!IsRunning || IsPaused || Room == null) return;

            long currentTime = TimeInfo.Instance.ServerNow();

            while (true)
            {
                if (currentTime < CreationTime + (PredictionFrame + 1) * LSConstValue.UpdateInterval)
                {
                    return;
                }

                if (PredictionFrame - AuthorityFrame > 5)
                {
                    return;
                }
                
                ++PredictionFrame;

                OneFrameInputs oneFrameInputs = _inputSystem.GetOneFrameMessages(PredictionFrame);
                
                Room.FrameTick(oneFrameInputs);
                if (TimeInfo.Instance.ServerNow() - currentTime > 5)
                {
                    return;
                }
            }
            
        }

        /// <summary>
        /// 保存状态
        /// </summary>
        /// <param name="frame">帧号</param>
        public void SaveState(int frame)
        {
            // 这里应该序列化游戏状态
            // 为简化实现，这里只是创建一个占位符
            _stateHistory[frame] = SerializeGameState();
        }

        /// <summary>
        /// 加载状态
        /// </summary>
        /// <param name="frame">帧号</param>
        public void LoadState(int frame)
        {
            if (_stateHistory.TryGetValue(frame, out var stateData))
            {
                // 这里应该反序列化游戏状态
                DeserializeGameState(stateData);
                AuthorityFrame = frame;
            }
        }

        /// <summary>
        /// 开始控制器
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            IsPaused = false;
            AuthorityFrame = 0;
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
        }

        /// <summary>
        /// 停止控制器
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
            
            _stateHistory.Clear();
        }

        /// <summary>
        /// 暂停控制器
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }
        
        /// <summary>
        /// 添加玩家输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="input">输入数据</param>
        public void AddPlayerInput(long playerId, LSInput input)
        {
            //_inputSystem.CollectInput(playerId, input);
        }

        /// <summary>
        /// 序列化游戏状态
        /// </summary>
        /// <returns>序列化的状态数据</returns>
        private byte[] SerializeGameState()
        {
            // 这里应该实现实际的序列化逻辑
            // 为简化实现，返回空数组
            return new byte[0];
        }

        /// <summary>
        /// 反序列化游戏状态
        /// </summary>
        /// <param name="stateData">状态数据</param>
        private void DeserializeGameState(byte[] stateData)
        {
            // 这里应该实现实际的反序列化逻辑
        }

    }
}
