using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
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
        public Room Room { get; set; }

        /// <summary>
        /// 当前帧
        /// </summary>
        public int AuthorityFrame { get; set; } = -1;
        
        public int PredictionFrame { get; set; } = -1;
        
        public int MaxPredictionFrames { get; set; } = 5;

        public FrameBuffer FrameBuffer
        {
            get { return _inputSystem.FrameBuffer; }
        }

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

            long currentTime = TimeInfo.Instance.ServerNow() + TimeInfo.Instance.RTT / 2;

            while (true)
            {
                if (currentTime < CreationTime + (PredictionFrame + 1) * LSConstValue.UpdateInterval)
                {
                    return;
                }

                if (PredictionFrame - AuthorityFrame > MaxPredictionFrames)
                {
                    return;
                }
                
                ++PredictionFrame;

                OneFrameInputs oneOneFrameInputs = _inputSystem.GetOneFrameMessages(PredictionFrame);
                Room.FrameTick(oneOneFrameInputs);
                if (Room.MainPlayerId > 0)
                {
                    var eventData = new FrameDataUploadEventData(PredictionFrame, _inputSystem.ClientInput);
                    ASLogger.Instance.Log(LogLevel.Debug, $"FrameDataUploadEventData: {PredictionFrame}, Input  mox: {_inputSystem.ClientInput.MoveX} moy:{_inputSystem.ClientInput.MoveY}" );
                    
                    EventSystem.Instance.Publish(eventData);
                }
                if (TimeInfo.Instance.ServerNow() - currentTime > 5)
                {
                    return;
                }
            }
            
        }

        /// <summary>
        /// 获取当前预测帧对应的时间
        /// </summary>
        /// <returns>当前预测帧对应的时间（毫秒）</returns>
        public long GetCurrentPredictionFrameTime()
        {
            return CreationTime + (PredictionFrame * LSConstValue.UpdateInterval);
        }

        /// <summary>
        /// 获取指定预测帧对应的时间
        /// </summary>
        /// <param name="predictionFrame">预测帧数</param>
        /// <returns>指定预测帧对应的时间（毫秒）</returns>
        public long GetPredictionFrameTime(int predictionFrame)
        {
            return CreationTime + (predictionFrame * LSConstValue.UpdateInterval);
        }

        /// <summary>
        /// 获取下一预测帧对应的时间
        /// </summary>
        /// <returns>下一预测帧对应的时间（毫秒）</returns>
        public long GetNextPredictionFrameTime()
        {
            return CreationTime + ((PredictionFrame + 1) * LSConstValue.UpdateInterval);
        }

        public void Rollback(int frame)
        {
            Room.MainWorld.Cleanup();
            Room.MainWorld = LoadState(frame);
            var aInput = FrameBuffer.FrameInputs(frame);
            //ASLogger.Instance.Warning($"aInput {aInput.Inputs[Room.MainPlayerId].MoveX}");
            Room.FrameTick(aInput);
            for(int i = AuthorityFrame +1; i <= PredictionFrame; ++i)
            {
                var pInput = FrameBuffer.FrameInputs(i);
                CopyOtherInputsTo(aInput, pInput);
                Room.FrameTick(pInput);
            }
            
        }
        public void CopyOtherInputsTo(OneFrameInputs from, OneFrameInputs to)
        {
            long myId = Room.MainPlayerId;
            foreach (var kv in from.Inputs)
            {
                if (kv.Key == myId)
                {
                    continue;
                }
                to.Inputs[kv.Key] = kv.Value;
            }
        }
        
        /// <summary>
        /// 详细比较两个 OneFrameInputs 的差异
        /// </summary>
        /// <param name="inputs1">第一个输入</param>
        /// <param name="inputs2">第二个输入</param>
        /// <returns>差异描述</returns>
        private string CompareInputs(OneFrameInputs inputs1, OneFrameInputs inputs2)
        {
            var differences = new List<string>();
            
            // 比较玩家数量
            if (inputs1.Inputs.Count != inputs2.Inputs.Count)
            {
                differences.Add($"PlayerCount: {inputs1.Inputs.Count} vs {inputs2.Inputs.Count}");
            }
            
            // 获取所有玩家ID
            var allPlayerIds = inputs1.Inputs.Keys.Union(inputs2.Inputs.Keys).OrderBy(x => x);
            
            foreach (var playerId in allPlayerIds)
            {
                bool hasInput1 = inputs1.Inputs.TryGetValue(playerId, out var input1);
                bool hasInput2 = inputs2.Inputs.TryGetValue(playerId, out var input2);
                
                if (!hasInput1)
                {
                    differences.Add($"Player {playerId}: Missing in inputs1");
                    continue;
                }
                
                if (!hasInput2)
                {
                    differences.Add($"Player {playerId}: Missing in inputs2");
                    continue;
                }
                
                // 比较具体输入字段
                var playerDifferences = CompareSingleInput(input1, input2, playerId);
                if (!string.IsNullOrEmpty(playerDifferences))
                {
                    differences.Add($"Player {playerId}: {playerDifferences}");
                }
            }
            
            return differences.Count > 0 ? string.Join("; ", differences) : "No differences found";
        }
        
        /// <summary>
        /// 比较单个 LSInput 的差异
        /// </summary>
        /// <param name="input1">第一个输入</param>
        /// <param name="input2">第二个输入</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>差异描述</returns>
        private string CompareSingleInput(LSInput input1, LSInput input2, long playerId)
        {
            var differences = new List<string>();
            
            if (input1.PlayerId != input2.PlayerId)
            {
                differences.Add($"PlayerId: {input1.PlayerId} vs {input2.PlayerId}");
            }
            
            if (input1.MoveX != input2.MoveX)
            {
                differences.Add($"MoveX: {input1.MoveX} vs {input2.MoveX}");
            }
            
            if (input1.MoveY != input2.MoveY)
            {
                differences.Add($"MoveY: {input1.MoveY} vs {input2.MoveY}");
            }
            
            if (input1.Attack != input2.Attack)
            {
                differences.Add($"Attack: {input1.Attack} vs {input2.Attack}");
            }
            
            if (input1.Skill1 != input2.Skill1)
            {
                differences.Add($"Skill1: {input1.Skill1} vs {input2.Skill1}");
            }
            
            if (input1.Skill2 != input2.Skill2)
            {
                differences.Add($"Skill2: {input1.Skill2} vs {input2.Skill2}");
            }
            
            if (input1.BornInfo != input2.BornInfo)
            {
                differences.Add($"BornInfo: {input1.BornInfo} vs {input2.BornInfo}");
            }
            
            return differences.Count > 0 ? string.Join(", ", differences) : "";
        }
        
        /// <summary>
        /// 保存状态
        /// </summary>
        /// <param name="frame">帧号</param>
        public void SaveState()
        {
            int frame = Room.MainWorld.CurFrame;
            var memoryBuffer = FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            memoryBuffer.SetLength(0);
            
            // 记录保存状态前的 World 信息
            if (Room?.MainWorld != null)
            {
                var world = Room.MainWorld;
                ASLogger.Instance.Debug($"保存帧状态 - 帧: {frame}, World ID: {world.WorldId},World Frame:{world.CurFrame} 实体数量: {world.Entities?.Count ?? 0}", "FrameSync.SaveState");
                
                if (world.Entities != null)
                {
                    foreach (var entity in world.Entities.Values)
                    {
                        ASLogger.Instance.Debug($"  - 保存实体: {entity.Name} (ID: {entity.UniqueId}), 激活: {entity.IsActive}, 组件: {entity.Components?.Count ?? 0}, 能力: {entity.CapabilityStates?.Count ?? 0}", "FrameSync.SaveState");
                    }
                }
            }
            
            MemoryPackHelper.Serialize(Room.MainWorld, memoryBuffer);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            long hash = memoryBuffer.GetBuffer().Hash(0, (int)memoryBuffer.Length);
            FrameBuffer.SetHash(frame, hash);
            
            ASLogger.Instance.Debug($"帧状态保存完成 - 帧: {frame}, 数据大小: {memoryBuffer.Length} bytes, 哈希: {hash}", "FrameSync.SaveState");
        }

        /// <summary>
        /// 加载状态
        /// </summary>
        /// <param name="frame">帧号</param>
        public World LoadState(int frame)
        {
            var memoryBuffer = FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            
            ASLogger.Instance.Debug($"加载帧状态 - 帧: {frame}, 数据大小: {memoryBuffer.Length} bytes", "FrameSync.LoadState");
            
            World world = MemoryPackHelper.Deserialize( typeof(World),memoryBuffer) as World;
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            
            // 记录加载状态后的 World 信息
            if (world != null)
            {
                ASLogger.Instance.Debug($"帧状态加载完成 - 帧: {frame}, World ID: {world.WorldId}, 实体数量: {world.Entities?.Count ?? 0}", "FrameSync.LoadState");
                
                if (world.Entities != null)
                {
                    foreach (var entity in world.Entities.Values)
                    {
                        ASLogger.Instance.Debug($"  - 加载实体: {entity.Name} (ID: {entity.UniqueId}), 激活: {entity.IsActive}, 组件: {entity.Components?.Count ?? 0}, 能力: {entity.CapabilityStates?.Count ?? 0}", "FrameSync.LoadState");
                    }
                }
            }
            else
            {
                ASLogger.Instance.Warning($"帧状态加载失败 - 帧: {frame}, 反序列化结果为 null", "FrameSync.LoadState");
            }
            
            return world;
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
            
            
        }
        
        /// <summary>
        /// 添加玩家输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="input">输入数据</param>
        public void SetPlayerInput(long playerId, LSInput input)
        {
            _inputSystem.ClientInput = input;

            if (input != null)
            {
                ASLogger.Instance.Log(LogLevel.Debug, $"Input Details - Frame: {input.Frame}, Move: ({input.MoveX:F2}, {input.MoveY:F2}), " +
                    $"Attack: {input.Attack}, Skill1: {input.Skill1}, Skill2: {input.Skill2}, Timestamp: {input.Timestamp}");
            }
        }
        
        public void SetOneFrameInputs(OneFrameInputs inputs)
        {
            _inputSystem.FrameBuffer.MoveForward(AuthorityFrame);
            // 服务端返回的消息比预测的还早,此时使用权威帧的输入覆盖预测帧的输入
            if (AuthorityFrame > PredictionFrame)
            {
                var aFrame = FrameBuffer.FrameInputs(AuthorityFrame);
                inputs.CopyTo(aFrame);
            }
            else
            {
                var pFrame = FrameBuffer.FrameInputs(AuthorityFrame);
                if (!inputs.Equal(pFrame))
                {
                    // 详细比较并输出差异
                    var differences = CompareInputs(inputs, pFrame);
                    ASLogger.Instance.Log(LogLevel.Warning, $"Input Mismatch at Frame {AuthorityFrame}. Rolling back from PredictionFrame {PredictionFrame} to AuthorityFrame {AuthorityFrame}.");
                    ASLogger.Instance.Log(LogLevel.Warning, $"Input Differences: {differences}");
                    
                    inputs.CopyTo(pFrame);
                    ASLogger.Instance.Log(LogLevel.Warning,$"roll back start {AuthorityFrame}");
                    Rollback(AuthorityFrame);
                }
                else
                {
                       
                }
            }
            var af = _inputSystem.FrameBuffer.FrameInputs(AuthorityFrame);
            inputs.CopyTo(af);
        }
        
    }
}
