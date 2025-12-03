using Astrum.CommonBase;
using UnityEngine.Profiling;

namespace Astrum.Client.Profiling
{
    /// <summary>
    /// Unity 环境的性能监控处理器
    /// 直接调用 Unity Profiler API，与 Unity Profiler 窗口无缝集成
    /// </summary>
    public class UnityProfilerHandler : IProfilerHandler
    {
        /// <summary>
        /// 开始性能监控样本
        /// </summary>
        /// <param name="name">样本名称</param>
        public void BeginSample(string name)
        {
            Profiler.BeginSample(name);
        }

        /// <summary>
        /// 结束性能监控样本
        /// </summary>
        public void EndSample()
        {
            Profiler.EndSample();
        }
    }
}


