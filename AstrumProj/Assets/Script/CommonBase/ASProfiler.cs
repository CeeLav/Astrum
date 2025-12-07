using System.Diagnostics;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 跨平台性能监控系统
    /// 类似 ASLogger，提供统一的性能监控接口，不同环境可注册不同的 Handler
    /// </summary>
    /// <remarks>
    /// 使用条件编译符号 ENABLE_PROFILER 控制是否启用性能监控
    /// Release 构建中，所有调用会被编译器完全移除，零开销
    /// </remarks>
    public class ASProfiler : Singleton<ASProfiler>
    {
        private IProfilerHandler _handler;
        private readonly object _lock = new object();

        /// <summary>
        /// 注册性能监控处理器
        /// </summary>
        /// <param name="handler">处理器实例</param>
        public void RegisterHandler(IProfilerHandler handler)
        {
            lock (_lock)
            {
                _handler = handler;
            }
        }

        /// <summary>
        /// 移除性能监控处理器
        /// </summary>
        public void UnregisterHandler()
        {
            lock (_lock)
            {
                _handler = null;
            }
        }

        /// <summary>
        /// 开始性能监控样本
        /// 使用 Conditional 特性确保 Release 构建时完全移除
        /// </summary>
        /// <param name="name">样本名称（建议使用字符串常量或 nameof() 避免 GC）</param>
        [Conditional("ENABLE_PROFILER")]
        public void BeginSample(string name)
        {
            _handler?.BeginSample(name);
        }

        /// <summary>
        /// 结束性能监控样本
        /// 使用 Conditional 特性确保 Release 构建时完全移除
        /// </summary>
        [Conditional("ENABLE_PROFILER")]
        public void EndSample()
        {
            _handler?.EndSample();
        }

        /// <summary>
        /// 检查是否已注册处理器
        /// </summary>
        /// <returns>如果已注册处理器返回 true</returns>
        public bool HasHandler()
        {
            lock (_lock)
            {
                return _handler != null;
            }
        }
    }
}





