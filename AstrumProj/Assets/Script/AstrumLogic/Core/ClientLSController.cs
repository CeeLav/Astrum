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
    /// 客户端帧同步控制器，负责管理客户端预测帧同步逻辑
    /// </summary>
    public class ClientLSController : ILSControllerBase, IClientFrameSync
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

        public ClientLSController()
        {
            _inputSystem = new LSInputSystem();
            _inputSystem.LSController = this;
            LastUpdateTime = TimeInfo.Instance.ServerNow();
        }

        /// <summary>
        /// 执行一帧（客户端预测更新）
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
            var loadedWorld = LoadState(frame);
            if (loadedWorld == null)
            {
                ASLogger.Instance.Warning($"无法回滚到帧 {frame}，快照数据不存在或为空，跳过回滚", "FrameSync.Rollback");
                return;
            }
            
            Room.MainWorld.Cleanup();
            Room.MainWorld = loadedWorld;
            var aInput = FrameBuffer.FrameInputs(frame);
            Room.FrameTick(aInput);
            for(int i = AuthorityFrame +1; i <= PredictionFrame; ++i)
            {
                var pInput = FrameBuffer.FrameInputs(i);
                CopyOtherInputsTo(aInput, pInput);
                Room.FrameTick(pInput);
            }
        }
        
        private void CopyOtherInputsTo(OneFrameInputs from, OneFrameInputs to)
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
        private string CompareInputs(OneFrameInputs inputs1, OneFrameInputs inputs2)
        {
            var differences = new List<string>();
            
            if (inputs1.Inputs.Count != inputs2.Inputs.Count)
            {
                differences.Add($"PlayerCount: {inputs1.Inputs.Count} vs {inputs2.Inputs.Count}");
            }
            
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
        public void SaveState()
        {
            // 客户端使用 PredictionFrame 保存状态（因为客户端是基于预测帧执行的）
            // 但如果 AuthorityFrame 有效，优先使用 AuthorityFrame（用于回滚）
            int frame = AuthorityFrame >= 0 ? AuthorityFrame : PredictionFrame;
            
            // 检查帧号是否有效
            if (frame < 0)
            {
                ASLogger.Instance.Warning($"ClientLSController.SaveState: 帧号无效 (AuthorityFrame: {AuthorityFrame}, PredictionFrame: {PredictionFrame})，跳过保存状态", "FrameSync.SaveState");
                return;
            }
            
            if (frame > FrameBuffer.MaxFrame)
            {
                while (FrameBuffer.MaxFrame < frame)
                {
                    FrameBuffer.MoveForward(FrameBuffer.MaxFrame);
                }
            }
            
            var memoryBuffer = FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            memoryBuffer.SetLength(0);
            
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
        public World LoadState(int frame)
        {
            var memoryBuffer = FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            
            ASLogger.Instance.Debug($"加载帧状态 - 帧: {frame}, 数据大小: {memoryBuffer.Length} bytes", "FrameSync.LoadState");
            
            if (memoryBuffer.Length == 0)
            {
                ASLogger.Instance.Warning($"帧状态快照为空 - 帧: {frame}, 无法加载状态，返回当前World", "FrameSync.LoadState");
                return null;
            }
            
            try
            {
                World world = MemoryPackHelper.Deserialize( typeof(World),memoryBuffer) as World;
                memoryBuffer.Seek(0, SeekOrigin.Begin);
                
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
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"帧状态反序列化失败 - 帧: {frame}, 错误: {ex.Message}", "FrameSync.LoadState");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                memoryBuffer.Seek(0, SeekOrigin.Begin);
                return null;
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
        }
        
        /// <summary>
        /// 添加玩家输入
        /// </summary>
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
                    var differences = CompareInputs(inputs, pFrame);
                    ASLogger.Instance.Log(LogLevel.Warning, $"Input Mismatch at Frame {AuthorityFrame}. Rolling back from PredictionFrame {PredictionFrame} to AuthorityFrame {AuthorityFrame}.");
                    ASLogger.Instance.Log(LogLevel.Warning, $"Input Differences: {differences}");
                    
                    inputs.CopyTo(pFrame);
                    
                    var snapshotBuffer = FrameBuffer.Snapshot(AuthorityFrame);
                    if (snapshotBuffer.Length > 0)
                    {
                        ASLogger.Instance.Log(LogLevel.Warning, $"roll back start {AuthorityFrame}");
                        Rollback(AuthorityFrame);
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"无法回滚到帧 {AuthorityFrame}，快照数据不存在（可能是首次同步），跳过回滚", "FrameSync.SetOneFrameInputs");
                    }
                }
            }
            var af = _inputSystem.FrameBuffer.FrameInputs(AuthorityFrame);
            inputs.CopyTo(af);
        }
    }
}

