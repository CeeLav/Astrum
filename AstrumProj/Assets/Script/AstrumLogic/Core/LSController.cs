using System;
using System.Collections.Generic;
using System.Linq;
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
        public int CurrentFrame { get; private set; } = 0;

        /// <summary>
        /// 帧率（如60FPS）
        /// </summary>
        public int TickRate { get; set; } = 60;

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
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 执行一帧
        /// </summary>
        public void Tick()
        {
            if (!IsRunning || IsPaused || Room == null) return;

            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long deltaTime = currentTime - LastUpdateTime;
            
            // 控制帧率
            long frameTime = 1000 / TickRate;
            if (deltaTime < frameTime) return;

            // 保存当前状态
            SaveState(CurrentFrame);

            // 分发输入到各个世界
            DistributeInputsToWorlds();

            // 更新输入系统
            _inputSystem.ProcessFrame(CurrentFrame);
            _inputSystem.Update();

            // 执行帧逻辑
            ExecuteFrame(CurrentFrame);

            // 更新帧计数
            CurrentFrame++;
            LastUpdateTime = currentTime;

            // 清理过旧的状态
            CleanupOldStates();
        }

        /// <summary>
        /// 将输入分发到各个World
        /// </summary>
        public void DistributeInputsToWorlds()
        {
            if (Room == null) return;

            // 获取当前帧的输入
            var frameInputs = _inputSystem.GetFrameInputs(CurrentFrame);
            
            // 如果没有输入，生成预测输入
            if (frameInputs == null)
            {
                frameInputs = GeneratePredictedFrameInputs(CurrentFrame);
            }

            // 分发输入到所有世界
            foreach (var world in Room.Worlds)
            {
                DistributeInputsToEntities(world, frameInputs);
            }
        }

        /// <summary>
        /// 将输入分发到实体
        /// </summary>
        /// <param name="world">世界对象</param>
        public void DistributeInputsToEntities(World world)
        {
            var frameInputs = _inputSystem.GetFrameInputs(CurrentFrame);
            DistributeInputsToEntities(world, frameInputs);
        }

        /// <summary>
        /// 将输入分发到实体
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="frameInputs">帧输入数据</param>
        private void DistributeInputsToEntities(World world, OneFrameInputs? frameInputs)
        {
            if (world == null || frameInputs == null) return;

            world.ApplyInputsToEntities(frameInputs);
        }

        /// <summary>
        /// 回滚到指定帧
        /// </summary>
        /// <param name="frame">目标帧号</param>
        public void RollbackToFrame(int frame)
        {
            if (frame < 0 || frame >= CurrentFrame) return;

            // 加载指定帧的状态
            LoadState(frame);

            // 重新执行从目标帧到当前帧的所有逻辑
            for (int f = frame; f < CurrentFrame; f++)
            {
                ExecuteFrame(f);
            }
        }

        /// <summary>
        /// 预测帧执行
        /// </summary>
        /// <param name="frame">帧号</param>
        public void PredictFrame(int frame)
        {
            // 生成预测输入并执行
            var predictedInputs = GeneratePredictedFrameInputs(frame);
            
            if (Room != null)
            {
                foreach (var world in Room.Worlds)
                {
                    DistributeInputsToEntities(world, predictedInputs);
                }
            }

            ExecuteFrame(frame);
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
                CurrentFrame = frame;
            }
        }

        /// <summary>
        /// 开始控制器
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            IsPaused = false;
            CurrentFrame = 0;
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _inputSystem.Reset();
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
        /// 恢复控制器
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 添加玩家输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="input">输入数据</param>
        public void AddPlayerInput(int playerId, LSInput input)
        {
            _inputSystem.CollectInput(playerId, input);
        }

        /// <summary>
        /// 执行单帧逻辑
        /// </summary>
        /// <param name="frame">帧号</param>
        private void ExecuteFrame(int frame)
        {
            if (Room == null) return;

            float deltaTime = 1f / TickRate;

            // 更新所有世界
            foreach (var world in Room.Worlds)
            {
                world.Update(deltaTime);
            }
        }

        /// <summary>
        /// 生成预测帧输入
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <returns>预测的帧输入</returns>
        private OneFrameInputs GeneratePredictedFrameInputs(int frame)
        {
            var frameInputs = new OneFrameInputs(frame);
            
            if (Room != null)
            {
                foreach (var playerId in Room.Players)
                {
                    var predictedInput = _inputSystem.GetPredictedInput(playerId, frame);
                    frameInputs.AddInput(playerId, predictedInput);
                }
            }

            return frameInputs;
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

        /// <summary>
        /// 清理过旧的状态
        /// </summary>
        private void CleanupOldStates()
        {
            if (_stateHistory.Count <= MaxStateHistory) return;

            int keepFrame = CurrentFrame - MaxStateHistory;
            var framesToRemove = _stateHistory.Keys.Where(f => f < keepFrame).ToList();
            
            foreach (var frame in framesToRemove)
            {
                _stateHistory.Remove(frame);
            }
        }
    }
}
