using AstrumTest.Shared.Fixtures;
using Xunit;

namespace AstrumTest.Shared.Integration
{
    /// <summary>
    /// 逻辑测试集合定义
    /// 提供共享的 ConfigFixture 给所有集成测试
    /// </summary>
    [CollectionDefinition("Logic Test Collection")]
    public class LogicTestCollection : ICollectionFixture<ConfigFixture>
    {
        // 这个类不需要代码，仅用于定义 Collection
    }
}


