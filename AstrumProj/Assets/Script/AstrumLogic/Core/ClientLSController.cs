using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Components;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 客户端帧同步控制器，负责管理客户端预测帧同步逻辑
    /// </summary>
    public class ClientLSController : ILSControllerBase
    {
        /// <summary>
        /// 所属房间
        /// </summary>
        public Room Room { get; set; }

        /// <summary>
        /// 当前玩家对应的 PlayerId（由 GameMode 注入）
        /// </summary>
        public long PlayerId { get; set; } = -1;

        /// <summary>
        /// 服务器下发的权威帧号（由服务器输入更新）
        /// </summary>
        public int AuthorityFrame { get; set; } = -1;
        
        /// <summary>
        /// 已处理完的权威帧号（实际更新完毕的位置）
        /// </summary>
        public int ProcessedAuthorityFrame { get; set; } = -1;
        
        public int PredictionFrame { get; set; } = -1;
        
        public int MaxPredictionFrames { get; set; } = 5;

        public FrameBuffer FrameBuffer
        {
            get { return _inputSystem.FrameBuffer; }
        }

        /// <summary>
        /// 帧率（如60FPS）
        /// </summary>
        public int TickRate { get; set; } = 20;
        
        /// <summary>
        /// 更新服务器时间戳，计算时间差（平滑过渡）
        /// 采用上升容易、下降缓慢的平滑策略
        /// </summary>
        /// <param name="sendTime">服务器时间戳（毫秒）</param>
        public void UpdateRTT(long sendTime)
        {
            TimeInfo.Instance.UpdateRTT(sendTime);
        }
        
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
        /// 是否启用状态保存（单机模式可以禁用以提升性能）
        /// </summary>
        public bool EnableStateSaving { get; set; } = true;

        /// <summary>
        /// 输入系统
        /// </summary>
        private LSInputSystem _inputSystem;

        // 影子输入备份：frameId -> OneFrameInputs（用于影子回滚重放）
        // 注意：frameId 是请求帧号（客户端上报时使用的帧号）
        private readonly Dictionary<int, OneFrameInputs> _shadowInputBackups = new Dictionary<int, OneFrameInputs>();
        
        // 影子实体每帧哈希记录：frameId -> hash（用于哈希比对）
        // 注意：frameId 是下发帧号（服务器实际下发的帧号）
        private readonly Dictionary<int, int> _shadowFrameHashes = new Dictionary<int, int>();
        

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

            long currentTime = TimeInfo.Instance.ServerNow();

            // 第一步：处理权威帧插值（从 ProcessedAuthorityFrame 推进到 AuthorityFrame）
            while (ProcessedAuthorityFrame < AuthorityFrame)
            {
                ++ProcessedAuthorityFrame;
                
                // 获取该权威帧的输入
                var authorityInputs = FrameBuffer.FrameInputs(ProcessedAuthorityFrame);
                Room.MainWorld.CurFrame = ProcessedAuthorityFrame;
                
                // 执行权威帧（仅权威实体，不涉及预测）
                Room.FrameTick(authorityInputs);
                
                // 权威帧处理完成后，比对影子实体与权威实体的哈希
                if (PlayerId > 0 && PredictionFrame >= ProcessedAuthorityFrame)
                {
                    CheckAndRollbackShadow(ProcessedAuthorityFrame);
                }
            }

            // 第二步：处理预测帧（基于已处理完的权威帧进行预测）
            while (true)
            {
                if (currentTime < CreationTime + (PredictionFrame + 1) * LSConstValue.UpdateInterval)
                {
                    return;
                }
                
                // 在预测新帧之前，确保MaxFrame足够大
                if (PredictionFrame + 1 > FrameBuffer.MaxFrame)
                {
                    FrameBuffer.MoveForward(FrameBuffer.MaxFrame);
                }

                // 预测帧不能超过已处理权威帧太多
                /*if (PredictionFrame - ProcessedAuthorityFrame > 10)
                {
                    ASLogger.Instance.Info($"超了：{PredictionFrame - ProcessedAuthorityFrame}");
                    return;
                }*/
                
                ++PredictionFrame;

                OneFrameInputs oneOneFrameInputs;
                using (new ProfileScope("ClientLS.GetOneFrameMessages"))
                {
                    oneOneFrameInputs = _inputSystem.GetOneFrameMessages(PredictionFrame);
                }
                
                // 记录影子输入备份（用于影子回滚快速重放）
                // 注意：备份的 key 是请求帧号（PredictionFrame）
                BackupShadowInput(PredictionFrame, oneOneFrameInputs);

                // 驱动 Room 内的影子实体进行本地预测模拟
                SimulateShadow(PredictionFrame, oneOneFrameInputs);
                
                // 记录影子实体当前帧的哈希（用于后续比对）
                RecordShadowFrameHash(PredictionFrame);

                
                using (new ProfileScope("ClientLS.PublishFrameData"))
                {
                    if (Room.MainPlayerId > 0)
                    {
                        // 确保 Input 的 Frame 字段被正确设置
                        if (_inputSystem.ClientInput != null)
                        {
                            _inputSystem.ClientInput.Frame = PredictionFrame;
                            _inputSystem.ClientInput.PlayerId = Room.MainPlayerId;
                        }
                        
                        var eventData = new FrameDataUploadEventData(PredictionFrame, _inputSystem.ClientInput);
                        //ASLogger.Instance.Log(LogLevel.Info, $"FrameDataUploadEventData: {PredictionFrame}, Input  mox: {_inputSystem.ClientInput.MoveX} moy:{_inputSystem.ClientInput.MoveY}" );
                        
                        EventSystem.Instance.Publish(eventData);
                    }
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

        /// <summary>
        /// 记录影子实体当前帧的哈希
        /// </summary>
        private void RecordShadowFrameHash(int frame)
        {
            if (Room?.ShadowWorld == null || PlayerId <= 0) return;

            // 影子实体使用与源实体相同的 ID（在 ShadowWorld 中）
            long shadowId = PlayerId;
            var shadow = Room.ShadowWorld.GetEntity(shadowId);
            if (shadow == null) return;
            
            int hash = Astrum.LogicCore.FrameSync.FrameHashUtility.Compute(shadow, frame);
            _shadowFrameHashes[frame] = hash;

            // 滑动窗口裁剪：保留近 MaxPredictionFrames 范围
            int minFrameToKeep = frame - (int)(TimeInfo.Instance.ServerTimeDiff / 50 * 5) - 10;
            
            var removeKeys = _shadowFrameHashes.Keys.Where(f => f < minFrameToKeep).ToList();
            foreach (var key in removeKeys)
            {
                _shadowFrameHashes.Remove(key);
            }
        }


        /// <summary>
        /// 检查并回滚影子实体（如果哈希不一致）
        /// </summary>
        private void CheckAndRollbackShadow(int authorityFrame)
        {
            if (Room?.MainWorld == null || Room?.ShadowWorld == null || PlayerId <= 0) return;

            var original = Room.MainWorld.GetEntity(PlayerId);
            // 影子实体使用与源实体相同的 ID（在 ShadowWorld 中）
            long shadowId = PlayerId;
            var shadow = Room.ShadowWorld.GetEntity(shadowId);

            if (original == null || shadow == null) return;

            // 比对哈希：使用记录的影子帧哈希
            int originalHash = Astrum.LogicCore.FrameSync.FrameHashUtility.Compute(original, authorityFrame);
            
            // 获取影子实体在该权威帧记录的哈希
            if (!_shadowFrameHashes.TryGetValue(authorityFrame, out int shadowHash))
            {
                // 如果没有记录，输出详细信息
                var shadowHashesInfo = _shadowFrameHashes.Count > 0
                    ? string.Join(", ", _shadowFrameHashes.OrderBy(kv => kv.Key).Select(kv => $"帧{kv.Key}:哈希{kv.Value}"))
                    : "无记录";
                var minFrame = _shadowFrameHashes.Count > 0 ? _shadowFrameHashes.Keys.Min() : -1;
                var maxFrame = _shadowFrameHashes.Count > 0 ? _shadowFrameHashes.Keys.Max() : -1;
                ASLogger.Instance.Warning(
                    $"影子实体哈希不存在 | 请求帧={authorityFrame} | 预测帧={PredictionFrame} | " +
                    $"哈希记录范围=[{minFrame}~{maxFrame}] | 记录数={_shadowFrameHashes.Count} | " +
                    $"哈希记录详情=[{shadowHashesInfo}]", 
                    "FrameSync.ShadowRollback");
                //return;
            }

            // 哈希不一致，执行回滚
            if (originalHash != shadowHash)
            {
                // 获取 MovementComponent 的位置历史
                var originalMovement = original.GetComponent<Components.MovementComponent>();
                var shadowMovement = shadow.GetComponent<Components.MovementComponent>();
                
                // 优先从位置历史中获取对应帧的位置
                string originalPos = "未知位置";
                string shadowPos = "未知位置";
                
                if (originalMovement != null)
                {
                    var originalHistory = originalMovement.GetPositionHistory();
                    var origHist = originalHistory.FirstOrDefault(h => h.Frame == authorityFrame);
                    if (origHist.Frame == authorityFrame)
                    {
                        originalPos = $"({origHist.Position.x.AsFloat():F2}, {origHist.Position.y.AsFloat():F2}, {origHist.Position.z.AsFloat():F2})";
                    }
                }
                
                if (shadowMovement != null)
                {
                    var shadowHistory = shadowMovement.GetPositionHistory();
                    var shadowHist = shadowHistory.FirstOrDefault(h => h.Frame == authorityFrame);
                    if (shadowHist.Frame == authorityFrame)
                    {
                        shadowPos = $"({shadowHist.Position.x.AsFloat():F2}, {shadowHist.Position.y.AsFloat():F2}, {shadowHist.Position.z.AsFloat():F2})";
                    }
                }
                
                // 如果位置历史中没有，尝试从缓存中获取
                if (originalPos == "未知位置")
                {
                    originalPos = Astrum.LogicCore.FrameSync.FrameHashUtility.GetCachedPositionInfo(originalHash);
                    if (originalPos == "未知位置")
                    {
                        originalPos = Astrum.LogicCore.FrameSync.FrameHashUtility.GetPositionInfo(original);
                    }
                }
                
                if (shadowPos == "未知位置")
                {
                    shadowPos = Astrum.LogicCore.FrameSync.FrameHashUtility.GetCachedPositionInfo(shadowHash);
                    if (shadowPos == "未知位置")
                    {
                        shadowPos = Astrum.LogicCore.FrameSync.FrameHashUtility.GetPositionInfo(shadow);
                    }
                }
                
                // 构建合并的日志信息
                var logParts = new List<string>
                {
                    $"帧={authorityFrame}",
                    $"权威哈希={originalHash}",
                    $"影子哈希={shadowHash}",
                    $"权威位置={originalPos}",
                    $"影子位置={shadowPos}"
                };
                
                // 如果双方都有 MovementComponent，对比位置历史
                if (originalMovement != null && shadowMovement != null)
                {
                    var originalHistory = originalMovement.GetPositionHistory();
                    var shadowHistory = shadowMovement.GetPositionHistory();
                    
                    // 找到共同的帧号（交集）
                    var commonFrames = originalHistory.Select(h => h.Frame)
                        .Intersect(shadowHistory.Select(h => h.Frame))
                        .OrderBy(f => f)
                        .ToList();
                    
                    if (commonFrames.Count > 0)
                    {
                        // 对比共同帧的位置
                        var comparisonLines = new List<string>();
                        foreach (var frame in commonFrames)
                        {
                            var origHist = originalHistory.FirstOrDefault(h => h.Frame == frame);
                            var shadowHist = shadowHistory.FirstOrDefault(h => h.Frame == frame);
                            
                            if (origHist.Frame != 0 && shadowHist.Frame != 0)
                            {
                                var origPosStr = $"({origHist.Position.x.AsFloat():F2}, {origHist.Position.y.AsFloat():F2}, {origHist.Position.z.AsFloat():F2})";
                                var shadowPosStr = $"({shadowHist.Position.x.AsFloat():F2}, {shadowHist.Position.y.AsFloat():F2}, {shadowHist.Position.z.AsFloat():F2})";
                                var match = origHist.Position == shadowHist.Position ? "✓" : "✗";
                                comparisonLines.Add($"帧{frame}: 权威={origPosStr}, 影子={shadowPosStr} {match}");
                            }
                        }
                        
                        if (comparisonLines.Count > 0)
                        {
                            logParts.Add($"位置对比: {string.Join(" | ", comparisonLines)}");
                        }
                    }
                }
                
                // 合并输出一条日志
                ASLogger.Instance.Info($"[影子回滚][PlayerId={PlayerId}] {string.Join(" | ", logParts)}", "FrameSync.ShadowRollback");

                // 回滚影子：从权威实体复制状态
                Room.CopyEntityStateToShadow(PlayerId, shadowId);
                RecordShadowFrameHash(authorityFrame);

                // 重放影子输入从权威帧+1到当前预测帧
                if (PredictionFrame > authorityFrame)
                {
                    ReplayShadowToFrame(authorityFrame, PredictionFrame);
                }
            }
        }
        
        /// <summary>
        /// 保存状态（使用已处理权威帧）
        /// </summary>
        public void SaveState()
        {
            // 如果禁用状态保存（单机模式），直接返回
            if (!EnableStateSaving)
            {
                return;
            }
            
            using (new ProfileScope("SaveState"))
            {
                // 使用 ProcessedAuthorityFrame 保存状态（只有权威帧需要保存，用于后续影子回滚）
                int frame = ProcessedAuthorityFrame;
                
                // 检查帧号是否有效
                if (frame < 0)
                {
                    ASLogger.Instance.Warning($"ClientLSController.SaveState: 帧号无效 (AuthorityFrame: {AuthorityFrame}, ProcessedAuthorityFrame: {ProcessedAuthorityFrame})，跳过保存状态", "FrameSync.SaveState");
                    return;
                }
                
                if (frame > FrameBuffer.MaxFrame)
                {
                    while (FrameBuffer.MaxFrame < frame)
                    {
                        FrameBuffer.MoveForward(FrameBuffer.MaxFrame);
                    }
                }
                
                MemoryBuffer memoryBuffer;
                using (new ProfileScope("SaveState.GetSnapshot"))
                {
                    memoryBuffer = FrameBuffer.Snapshot(frame);
                    memoryBuffer.Seek(0, SeekOrigin.Begin);
                    memoryBuffer.SetLength(0);
                }
                
                // 序列化 World
                using (new ProfileScope("SaveState.SerializeWorld"))
                {
                    MemoryPackHelper.Serialize(Room.MainWorld, memoryBuffer);
                }
                
                // 计算哈希
                using (new ProfileScope("SaveState.CalculateHash"))
                {
                    memoryBuffer.Seek(0, SeekOrigin.Begin);
                    long hash = memoryBuffer.GetBuffer().Hash(0, (int)memoryBuffer.Length);
                    FrameBuffer.SetHash(frame, hash);
                }
                
                // ASLogger.Instance.Debug($"帧状态保存完成 - 帧: {frame}, 数据大小: {memoryBuffer.Length} bytes, 哈希: {hash}", "FrameSync.SaveState");
            }
        }

        /// <summary>
        /// 加载状态
        /// </summary>
        public World LoadState(int frame)
        {
            var memoryBuffer = FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            
            // ASLogger.Instance.Debug($"加载帧状态 - 帧: {frame}, 数据大小: {memoryBuffer.Length} bytes", "FrameSync.LoadState");
            
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
                    // ASLogger.Instance.Debug($"帧状态加载完成 - 帧: {frame}, World ID: {world.WorldId}, 实体数量: {world.Entities?.Count ?? 0}", "FrameSync.LoadState");
                    
                    if (world.Entities != null)
                    {
                        foreach (var entity in world.Entities.Values)
                        {
                            // ASLogger.Instance.Debug($"  - 加载实体: {entity.Name} (ID: {entity.UniqueId}), 组件: {entity.Components?.Count ?? 0}, 能力: {entity.CapabilityStates?.Count ?? 0}", "FrameSync.LoadState");
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
            ProcessedAuthorityFrame = -1; // 初始化为 -1，第一次 Tick 时会变成 0
            PredictionFrame = -1; // 初始化为 -1，第一次 Tick 时会变成 0
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
            if (input != null)
            {
                // 确保 PlayerId 和 Frame 字段被正确设置
                input.PlayerId = playerId;
                PlayerId = playerId;
                // Frame 字段会在发送时由 PredictionFrame 设置，这里不需要设置
                
                _inputSystem.ClientInput = input;
                
                // ASLogger.Instance.Log(LogLevel.Debug, $"Input Details - Frame: {input.Frame}, Move: ({input.MoveX:F2}, {input.MoveY:F2}), " +
                //     $"Attack: {input.Attack}, Skill1: {input.Skill1}, Skill2: {input.Skill2}, Timestamp: {input.Timestamp}");
            }
        }

        /// <summary>
        /// 备份影子输入（滑动窗口）- 只备份本地玩家输入
        /// </summary>
        private void BackupShadowInput(int frame, OneFrameInputs inputs)
        {
            if (inputs == null || PlayerId <= 0) return;
            
            // 只备份本地玩家的输入
            if (!inputs.Inputs.TryGetValue(PlayerId, out var playerInput))
                return;
            
            // 创建只包含本地玩家输入的 OneFrameInputs
            var backup = new OneFrameInputs();
            backup.Inputs[PlayerId] = playerInput;
            _shadowInputBackups[frame] = backup;

            // 窗口裁剪：保留近 MaxPredictionFrames 范围
            int minFrameToKeep = frame - MaxPredictionFrames - 1;
            var removeKeys = _shadowInputBackups.Keys.Where(f => f < minFrameToKeep).ToList();
            foreach (var key in removeKeys)
            {
                _shadowInputBackups.Remove(key);
            }
        }

        /// <summary>
        /// 影子实体模拟入口：直接通知 Room 进行影子实体更新
        /// </summary>
        private void SimulateShadow(int frame, OneFrameInputs inputs)
        {
            if (Room == null || inputs == null) return;
            Room.FrameTickShadow(PlayerId, frame, inputs);
        }

        /// <summary>
        /// 回滚后快速重放影子输入到目标预测帧
        /// </summary>
        public void ReplayShadowToFrame(int fromFrame, int toFrame)
        {
            if (toFrame <= fromFrame) return;
            for (int frame = fromFrame + 1; frame <= toFrame; ++frame)
            {
                if (_shadowInputBackups.TryGetValue(frame, out var inputs))
                {
                    SimulateShadow(frame, inputs);
                    RecordShadowFrameHash(frame);
                }
            }
        }

        private int GetMaxBackupFrame()
        {
            if (_shadowInputBackups.Count == 0) return -1;
            return _shadowInputBackups.Keys.Max();
        }
        
        public void SetOneFrameInputs(OneFrameInputs inputs)
        {
            _inputSystem.FrameBuffer.MoveForward(AuthorityFrame);
            
            // 更新权威帧输入
            var aFrame = FrameBuffer.FrameInputs(AuthorityFrame);
            inputs.CopyTo(aFrame);
        }
    }
}

