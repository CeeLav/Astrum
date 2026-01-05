using System;
using System.Threading;
using Astrum.CommonBase;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 逻辑专用线程，用于在独立线程中运行游戏逻辑
    /// </summary>
    /// <remarks>
    /// 设计决策：
    /// - 使用 System.Threading.Thread 直接创建，生命周期可控
    /// - 采用持续轮询 + 短休眠策略，平衡响应速度和 CPU 占用
    /// - 支持暂停/恢复功能
    /// - 提供配置开关，默认禁用以保持向后兼容
    /// </remarks>
    public class LogicThread
    {
        /// <summary>
        /// 线程名称
        /// </summary>
        private const string ThreadName = "AstrumLogicThread";
        
        /// <summary>
        /// 默认休眠时间（毫秒）
        /// </summary>
        private const int DefaultSleepMs = 1;
        
        /// <summary>
        /// 暂停时的休眠时间（毫秒）
        /// </summary>
        private const int PausedSleepMs = 10;
        
        /// <summary>
        /// 停止超时时间（毫秒）
        /// </summary>
        private const int StopTimeoutMs = 5000;
        
        /// <summary>
        /// 目标帧率
        /// </summary>
        public int TickRate { get; }
        
        /// <summary>
        /// 目标帧间隔（毫秒）
        /// </summary>
        public float TargetDeltaTime { get; }
        
        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused => _isPaused;
        
        /// <summary>
        /// 是否发生错误
        /// </summary>
        public bool HasError => _hasError;
        
        /// <summary>
        /// 最后一次错误信息
        /// </summary>
        public string LastError { get; private set; }
        
        /// <summary>
        /// 错误发生时的回调（在逻辑线程中调用）
        /// </summary>
        public event Action<Exception> OnError;
        
        // 线程控制
        private Thread _logicThread;
        private volatile bool _isRunning;
        private volatile bool _isPaused;
        private volatile bool _hasError;
        
        // 同步事件
        private readonly ManualResetEventSlim _stopEvent = new ManualResetEventSlim(false);
        
        // 关联的 Room
        private readonly Room _room;
        
        /// <summary>
        /// 创建逻辑线程
        /// </summary>
        /// <param name="room">关联的房间</param>
        /// <param name="tickRate">目标帧率（默认 20 FPS）</param>
        public LogicThread(Room room, int tickRate = 20)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
            TickRate = tickRate;
            TargetDeltaTime = 1000f / tickRate; // 毫秒
        }
        
        /// <summary>
        /// 启动逻辑线程
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("LogicThread: 线程已在运行，不能重复启动");
            }
            
            _isRunning = true;
            _isPaused = false;
            _hasError = false;
            _stopEvent.Reset();
            
            _logicThread = new Thread(LogicThreadLoop)
            {
                Name = ThreadName,
                IsBackground = true // 应用退出时自动终止线程
            };
            
            _logicThread.Start();
            ASLogger.Instance.Info($"[LogicThread] 逻辑线程已启动 - TickRate: {TickRate} FPS, TargetDeltaTime: {TargetDeltaTime:F2}ms");
        }
        
        /// <summary>
        /// 停止逻辑线程
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                ASLogger.Instance.Warning("[LogicThread] 线程未运行，无需停止");
                return;
            }
            
            ASLogger.Instance.Info("[LogicThread] 正在停止逻辑线程...");
            _isRunning = false;
            
            // 等待线程退出
            if (_stopEvent.Wait(StopTimeoutMs))
            {
                ASLogger.Instance.Info("[LogicThread] 逻辑线程已正常退出");
            }
            else
            {
                ASLogger.Instance.Warning($"[LogicThread] 逻辑线程在 {StopTimeoutMs}ms 内未能正常退出");
            }
            
            _logicThread = null;
        }
        
        /// <summary>
        /// 暂停逻辑线程
        /// </summary>
        public void Pause()
        {
            if (!_isRunning)
            {
                ASLogger.Instance.Warning("[LogicThread] 线程未运行，无法暂停");
                return;
            }
            
            _isPaused = true;
            ASLogger.Instance.Info("[LogicThread] 逻辑线程已暂停");
        }
        
        /// <summary>
        /// 恢复逻辑线程
        /// </summary>
        public void Resume()
        {
            if (!_isRunning)
            {
                ASLogger.Instance.Warning("[LogicThread] 线程未运行，无法恢复");
                return;
            }
            
            _isPaused = false;
            ASLogger.Instance.Info("[LogicThread] 逻辑线程已恢复");
        }
        
        /// <summary>
        /// 逻辑线程主循环
        /// </summary>
        private void LogicThreadLoop()
        {
            try
            {
                while (_isRunning)
                {
                    // 暂停时休眠更长时间
                    if (_isPaused)
                    {
                        Thread.Sleep(PausedSleepMs);
                        continue;
                    }
                    
                    try
                    {
                        // 调用 Room.Update()
                        // Room.Update() 内部会调用 LSController.Tick()
                        // Tick() 内部逻辑保持不变：
                        //   1. ProcessPendingInputs() - 消费输入队列
                        //   2. 处理权威帧（有多少处理多少）
                        //   3. 处理预测帧（基于时间戳判断）
                        //   4. CheckAndRollbackShadow() - 比对和回滚
                        _room?.Update(0);
                    }
                    catch (Exception ex)
                    {
                        HandleError(ex);
                    }
                    
                    // 短暂休眠，避免 CPU 空转
                    // 权威帧最多延迟 1ms（相比网络延迟可忽略）
                    // 预测帧时间控制在 Tick 内部（不受影响）
                    Thread.Sleep(DefaultSleepMs);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            finally
            {
                // 通知主线程，线程已退出
                _stopEvent.Set();
            }
        }
        
        /// <summary>
        /// 处理错误
        /// </summary>
        private void HandleError(Exception ex)
        {
            _hasError = true;
            LastError = ex.Message;
            
            ASLogger.Instance.Error($"[LogicThread] 逻辑线程发生异常: {ex.Message}");
            ASLogger.Instance.LogException(ex, LogLevel.Error);
            
            // 触发错误事件
            try
            {
                OnError?.Invoke(ex);
            }
            catch (Exception callbackEx)
            {
                ASLogger.Instance.Error($"[LogicThread] 错误回调执行失败: {callbackEx.Message}");
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isRunning)
            {
                Stop();
            }
            
            _stopEvent.Dispose();
        }
    }
}



