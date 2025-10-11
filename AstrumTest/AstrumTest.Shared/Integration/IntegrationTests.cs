using AstrumTest.Shared.Fixtures;
using AstrumTest.Shared.Integration.Core;
using Xunit;
using Xunit.Abstractions;

namespace AstrumTest.Shared.Integration
{
    /// <summary>
    /// 集成测试 - 基于帧的数据驱动测试
    /// </summary>
    [Collection("Logic Test Collection")]
    [Trait("TestLevel", "Integration")]
    public class IntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly ConfigFixture _configFixture;
        
        public IntegrationTests(ITestOutputHelper output, ConfigFixture configFixture)
        {
            _output = output;
            _configFixture = configFixture;
        }
        
        /// <summary>
        /// 测试：两个骑士对战
        /// </summary>
        [Fact]
        [Trait("Module", "Combat")]
        public void TwoKnights_BasicFight()
        {
            using var executor = new LogicTestExecutor(_output, _configFixture);
            
            // 核心流程自动完成：读取JSON → 初始化 → 逐帧执行 → 验证 → 报告
            var result = executor.RunTestCase("TwoKnightsFight.json");
            
            // 验证结果
            Assert.True(result.Success, result.FailureMessage);
            Assert.Equal(4, result.TotalFrames);
            Assert.Equal(4, result.PassedFrames);
            Assert.Equal(0, result.FailedFrames);
        }
        
        /// <summary>
        /// 数据驱动测试：运行多个战斗场景
        /// </summary>
        [Theory]
        [InlineData("TwoKnightsFight.json")]
        // 可以继续添加更多测试用例
        // [InlineData("MageVsArcher.json")]
        // [InlineData("MultiTargetSkill.json")]
        [Trait("Module", "Combat")]
        public void Combat_Scenarios(string scenarioFile)
        {
            using var executor = new LogicTestExecutor(_output, _configFixture);
            var result = executor.RunTestCase(scenarioFile);
            Assert.True(result.Success, result.FailureMessage);
        }
    }
}

