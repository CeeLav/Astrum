using System;
using System.Collections.Generic;
using System.Diagnostics;
using Astrum.CommonBase;

namespace Astrum.Server.Profiling
{
    /// <summary>
    /// 服务器环境的性能监控处理器
    /// 使用 Stopwatch 记录耗时，超过阈值时输出警告日志
    /// </summary>
    public class ServerProfilerHandler : IProfilerHandler
    {
        private readonly Stack<(string name, long startTicks)> _stack = new Stack<(string, long)>();
        private readonly object _lock = new object();
        private long _thresholdMs;

        /// <summary>
        /// 获取或设置性能阈值（毫秒）
        /// 超过此阈值的操作会输出警告日志
        /// </summary>
        public long ThresholdMs
        {
            get => _thresholdMs;
            set => _thresholdMs = value;
        }

        /// <summary>
        /// 创建服务器性能监控处理器
        /// </summary>
        /// <param name="thresholdMs">性能阈值（毫秒），默认 5ms</param>
        public ServerProfilerHandler(long thresholdMs = 5)
        {
            _thresholdMs = thresholdMs;
        }

        /// <summary>
        /// 开始性能监控样本
        /// </summary>
        /// <param name="name">样本名称</param>
        public void BeginSample(string name)
        {
            lock (_lock)
            {
                _stack.Push((name, Stopwatch.GetTimestamp()));
            }
        }

        /// <summary>
        /// 结束性能监控样本
        /// </summary>
        public void EndSample()
        {
            lock (_lock)
            {
                if (_stack.Count == 0)
                {
                    ASLogger.Instance.Warning("[Profiler] EndSample called without matching BeginSample");
                    return;
                }

                var (name, startTicks) = _stack.Pop();
                var elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;

                if (elapsedMs > _thresholdMs)
                {
                    ASLogger.Instance.Warning($"[Profiler] {name} took {elapsedMs:F2}ms (threshold: {_thresholdMs}ms)");
                }
            }
        }

        /// <summary>
        /// 清空监控栈（用于异常恢复）
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _stack.Clear();
            }
        }
    }
}

