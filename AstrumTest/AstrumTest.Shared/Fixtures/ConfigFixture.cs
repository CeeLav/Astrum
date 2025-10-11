using System;
using System.IO;
using Xunit;
using Astrum.LogicCore.Managers;

namespace AstrumTest.Shared.Fixtures
{
    /// <summary>
    /// 配置 Fixture - 在测试集合间共享，提供隔离的配置环境
    /// 同一集合的测试会串行执行，避免状态污染
    /// </summary>
    public class ConfigFixture : IDisposable
    {
        public string ConfigPath { get; private set; }
        
        public ConfigFixture()
        {
            // 一次性初始化配置
            var root = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")
            );
            ConfigPath = Path.Combine(root, "AstrumConfig", "Tables", "output", "Client");
            
            // 只初始化一次
            if (!Directory.Exists(ConfigPath))
            {
                throw new DirectoryNotFoundException(
                    $"配置路径不存在: {ConfigPath}\n" +
                    $"请确保已经生成配置表"
                );
            }
            
            ConfigManager.Instance.Initialize(ConfigPath);
        }
        
        public void Dispose()
        {
            // 清理共享资源（如果需要）
            // 注意：ConfigManager 是单例，通常不需要清理
        }
    }
    
    /// <summary>
    /// 测试集合定义 - 使用此集合的测试类会：
    /// 1. 共享同一个 ConfigFixture 实例
    /// 2. 串行执行（避免并发问题）
    /// 3. 隔离于其他集合的测试
    /// </summary>
    [CollectionDefinition("Config Collection")]
    public class ConfigCollection : ICollectionFixture<ConfigFixture>
    {
        // 这个类仅用于声明集合，无需实现任何内容
        // Xunit 会自动识别并管理 Fixture 的生命周期
    }
}

