using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Astrum.CommonBase;

namespace Astrum.Test
{
    /// <summary>
    /// 测试环境的性能监控处理器
    /// 收集所有样本的性能数据，支持查询和统计
    /// </summary>
    public class TestProfilerHandler : IProfilerHandler
    {
        private readonly Dictionary<string, List<double>> _sampleTimes = new Dictionary<string, List<double>>();
        private readonly Stack<(string name, long startTicks)> _stack = new Stack<(string, long)>();
        private readonly object _lock = new object();

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
                    return;
                }

                var (name, startTicks) = _stack.Pop();
                var elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;

                if (!_sampleTimes.ContainsKey(name))
                {
                    _sampleTimes[name] = new List<double>();
                }

                _sampleTimes[name].Add(elapsedMs);
            }
        }

        /// <summary>
        /// 获取指定样本的所有耗时记录
        /// </summary>
        /// <param name="name">样本名称</param>
        /// <returns>耗时记录列表（毫秒）</returns>
        public List<double> GetSampleTimes(string name)
        {
            lock (_lock)
            {
                return _sampleTimes.TryGetValue(name, out var times) 
                    ? new List<double>(times) 
                    : new List<double>();
            }
        }

        /// <summary>
        /// 获取指定样本的平均耗时
        /// </summary>
        /// <param name="name">样本名称</param>
        /// <returns>平均耗时（毫秒），如果没有记录返回 0</returns>
        public double GetAverageSampleTime(string name)
        {
            lock (_lock)
            {
                if (_sampleTimes.TryGetValue(name, out var times) && times.Count > 0)
                {
                    return times.Average();
                }
                return 0;
            }
        }

        /// <summary>
        /// 获取指定样本的最大耗时
        /// </summary>
        /// <param name="name">样本名称</param>
        /// <returns>最大耗时（毫秒），如果没有记录返回 0</returns>
        public double GetMaxSampleTime(string name)
        {
            lock (_lock)
            {
                if (_sampleTimes.TryGetValue(name, out var times) && times.Count > 0)
                {
                    return times.Max();
                }
                return 0;
            }
        }

        /// <summary>
        /// 获取指定样本的调用次数
        /// </summary>
        /// <param name="name">样本名称</param>
        /// <returns>调用次数</returns>
        public int GetSampleCount(string name)
        {
            lock (_lock)
            {
                return _sampleTimes.TryGetValue(name, out var times) ? times.Count : 0;
            }
        }

        /// <summary>
        /// 清空所有性能数据
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _sampleTimes.Clear();
                _stack.Clear();
            }
        }

        /// <summary>
        /// 获取所有样本名称
        /// </summary>
        /// <returns>样本名称列表</returns>
        public List<string> GetAllSampleNames()
        {
            lock (_lock)
            {
                return new List<string>(_sampleTimes.Keys);
            }
        }
    }
}







