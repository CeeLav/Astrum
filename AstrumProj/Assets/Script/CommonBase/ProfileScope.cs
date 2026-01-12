using System;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 性能监控作用域
    /// 使用 IDisposable 模式自动管理 BeginSample/EndSample 配对
    /// </summary>
    /// <example>
    /// 使用示例：
    /// <code>
    /// using (new ProfileScope("MyMethod"))
    /// {
    ///     // 执行代码
    /// }
    /// </code>
    /// </example>
    public readonly struct ProfileScope : IDisposable
    {
        /// <summary>
        /// 创建性能监控作用域并开始采样
        /// </summary>
        /// <param name="name">样本名称</param>
        public ProfileScope(string name)
        {
            ASProfiler.Instance.BeginSample(name);
        }

        /// <summary>
        /// 结束采样（自动调用）
        /// </summary>
        public void Dispose()
        {
            ASProfiler.Instance.EndSample();
        }
    }
}











