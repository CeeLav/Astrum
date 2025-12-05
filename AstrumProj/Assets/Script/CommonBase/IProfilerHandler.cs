namespace Astrum.CommonBase
{
    /// <summary>
    /// 性能监控处理器接口
    /// 不同环境可实现此接口来提供不同的性能监控实现
    /// </summary>
    public interface IProfilerHandler
    {
        /// <summary>
        /// 开始性能监控样本
        /// </summary>
        /// <param name="name">样本名称</param>
        void BeginSample(string name);

        /// <summary>
        /// 结束性能监控样本
        /// </summary>
        void EndSample();
    }
}




