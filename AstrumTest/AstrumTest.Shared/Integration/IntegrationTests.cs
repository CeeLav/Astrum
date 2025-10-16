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
        [Trait("Module", "Combat")]
        public void Combat_Scenarios(string scenarioFile)
        {
            using var executor = new LogicTestExecutor(_output, _configFixture);
            var result = executor.RunTestCase(scenarioFile);
            Assert.True(result.Success, result.FailureMessage);
        }
        
        /// <summary>
        /// 数值系统 - 死亡流程测试
        /// </summary>
        [Fact]
        [Trait("Module", "StatsSystem")]
        [Trait("Category", "Death")]
        public void StatsSystem_DeathFlow()
        {
            using var executor = new LogicTestExecutor(_output, _configFixture);
            var result = executor.RunTestCase("StatsSystem_DeathFlow.json");
            Assert.True(result.Success, result.FailureMessage);
        }
        
        /// <summary>
        /// 数值系统 - 距离检测测试
        /// </summary>
        [Fact]
        [Trait("Module", "StatsSystem")]
        [Trait("Category", "Distance")]
        public void StatsSystem_DistanceCheck()
        {
            using var executor = new LogicTestExecutor(_output, _configFixture);
            var result = executor.RunTestCase("StatsSystem_DistanceCheck.json");
            Assert.True(result.Success, result.FailureMessage);
        }
        
        /// <summary>
        /// 数值系统 - 连续攻击测试
        /// </summary>
        [Fact]
        [Trait("Module", "StatsSystem")]
        [Trait("Category", "Combat")]
        public void StatsSystem_MultipleAttacks()
        {
            using var executor = new LogicTestExecutor(_output, _configFixture);
            var result = executor.RunTestCase("StatsSystem_MultipleAttacks.json");
            Assert.True(result.Success, result.FailureMessage);
        }
        
        /// <summary>
        /// 数值系统 - 不同职业对战测试
        /// </summary>
        [Fact]
        [Trait("Module", "StatsSystem")]
        [Trait("Category", "Combat")]
        public void StatsSystem_DifferentRoles()
        {
            using var executor = new LogicTestExecutor(_output, _configFixture);
            var result = executor.RunTestCase("StatsSystem_DifferentRoles.json");
            Assert.True(result.Success, result.FailureMessage);
        }
        
        /// <summary>
        /// 数值系统 - 批量场景测试
        /// </summary>
        [Theory]
        [InlineData("StatsSystem_DeathFlow.json")]
        [InlineData("StatsSystem_DistanceCheck.json")]
        [InlineData("StatsSystem_MultipleAttacks.json")]
        [InlineData("StatsSystem_DifferentRoles.json")]
        [Trait("Module", "StatsSystem")]
        [Trait("Category", "Integration")]
        public void StatsSystem_AllScenarios(string scenarioFile)
        {
            using var executor = new LogicTestExecutor(_output, _configFixture);
            var result = executor.RunTestCase(scenarioFile);
            Assert.True(result.Success, result.FailureMessage);
        }
    }
}

