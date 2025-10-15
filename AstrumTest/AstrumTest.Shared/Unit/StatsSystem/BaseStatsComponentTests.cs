using Xunit;
using TrueSync;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using AstrumTest.Shared.Fixtures;

namespace AstrumTest.Unit.StatsSystem
{
    /// <summary>
    /// BaseStatsComponent 测试 - 测试配置表初始化和等级成长
    /// </summary>
    [Trait("TestLevel", "Component")]
    [Trait("Category", "Unit")]
    [Trait("Module", "StatsSystem")]
    [Trait("Priority", "High")]
    public class BaseStatsComponentTests : IClassFixture<ConfigFixture>
    {
        private readonly ConfigFixture _configFixture;

        public BaseStatsComponentTests(ConfigFixture configFixture)
        {
            _configFixture = configFixture;
        }

        [Fact(DisplayName = "InitializeFromConfig - 正确读取角色基础属性")]
        public void InitializeFromConfig_ShouldLoadCorrectBaseStats()
        {
            // Arrange
            var component = new BaseStatsComponent();
            int knightRoleId = 1001; // 骑士
            int level = 1;
            
            // Act
            component.InitializeFromConfig(knightRoleId, level);
            
            // Assert
            Assert.Equal((FP)80, component.BaseStats.Get(StatType.ATK));
            Assert.Equal((FP)80, component.BaseStats.Get(StatType.DEF));
            Assert.Equal((FP)1000, component.BaseStats.Get(StatType.HP));
            Assert.Equal((FP)10, component.BaseStats.Get(StatType.SPD)); // 配置表10000，除以1000=10
        }
        
        [Fact(DisplayName = "InitializeFromConfig - 正确读取高级属性")]
        public void InitializeFromConfig_ShouldLoadAdvancedStats()
        {
            // Arrange
            var component = new BaseStatsComponent();
            int knightRoleId = 1001;
            
            // Act
            component.InitializeFromConfig(knightRoleId, 1);
            
            // Assert - 暴击率5%（配置表50，除以1000）
            FP critRate = component.BaseStats.Get(StatType.CRIT_RATE);
            Assert.True(TSMath.Abs(critRate - (FP)0.05) < (FP)0.001, 
                $"Expected 0.05, but got {(float)critRate}");
            
            // Assert - 暴击伤害200%（配置表2000，除以1000）
            FP critDamage = component.BaseStats.Get(StatType.CRIT_DMG);
            Assert.True(TSMath.Abs(critDamage - (FP)2.0) < (FP)0.001,
                $"Expected 2.0, but got {(float)critDamage}");
        }
        
        [Theory(DisplayName = "InitializeFromConfig - 不同角色加载不同属性")]
        [InlineData(1001, 80, 80, 1000)]  // 骑士：平衡型
        [InlineData(1002, 60, 60, 800)]   // 弓手：机动型
        [InlineData(1003, 50, 40, 700)]   // 法师：法术型
        [InlineData(1004, 100, 120, 1200)] // 重锤者：坦克型
        [InlineData(1005, 70, 50, 600)]   // 刺客：爆发型
        public void InitializeFromConfig_DifferentRoles_ShouldHaveDifferentStats(
            int roleId, int expectedAtk, int expectedDef, int expectedHp)
        {
            // Arrange
            var component = new BaseStatsComponent();
            
            // Act
            component.InitializeFromConfig(roleId, 1);
            
            // Assert
            Assert.Equal((FP)expectedAtk, component.BaseStats.Get(StatType.ATK));
            Assert.Equal((FP)expectedDef, component.BaseStats.Get(StatType.DEF));
            Assert.Equal((FP)expectedHp, component.BaseStats.Get(StatType.HP));
        }
        
        [Fact(DisplayName = "InitializeFromConfig - 等级2时应用成长")]
        public void InitializeFromConfig_Level2_ShouldApplyGrowth()
        {
            // Arrange
            var component = new BaseStatsComponent();
            int knightRoleId = 1001;
            
            // Act
            component.InitializeFromConfig(knightRoleId, 2);
            
            // Assert - 基础80 + 成长8*1 = 88
            Assert.Equal((FP)88, component.BaseStats.Get(StatType.ATK));
            Assert.Equal((FP)88, component.BaseStats.Get(StatType.DEF));
            Assert.Equal((FP)1100, component.BaseStats.Get(StatType.HP)); // 基础1000 + 成长100*1
        }
        
        [Fact(DisplayName = "InitializeFromConfig - 等级10时应用成长")]
        public void InitializeFromConfig_Level10_ShouldApplyMultipleLevelGrowth()
        {
            // Arrange
            var component = new BaseStatsComponent();
            int knightRoleId = 1001;
            
            // Act
            component.InitializeFromConfig(knightRoleId, 10);
            
            // Assert - 基础80 + 成长8*9 = 152
            FP expectedAtk = (FP)(80 + 8 * 9);
            Assert.Equal(expectedAtk, component.BaseStats.Get(StatType.ATK));
        }
        
        [Fact(DisplayName = "ApplyAllocatedPoints - 加点应正确增加属性")]
        public void ApplyAllocatedPoints_ShouldIncreaseStats()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.InitializeFromConfig(1001, 1);
            
            var growthComp = new GrowthComponent
            {
                AllocatedAttackPoints = 5,
                AllocatedDefensePoints = 3,
                AllocatedHealthPoints = 10,
                AllocatedSpeedPoints = 2
            };
            
            FP atkBefore = baseStats.BaseStats.Get(StatType.ATK);
            
            // Act
            baseStats.ApplyAllocatedPoints(growthComp);
            
            // Assert - 每点攻击+2
            Assert.Equal(atkBefore + (FP)10, baseStats.BaseStats.Get(StatType.ATK));
            // 每点防御+2
            Assert.Equal((FP)(80 + 6), baseStats.BaseStats.Get(StatType.DEF));
            // 每点生命+20
            Assert.Equal((FP)(1000 + 200), baseStats.BaseStats.Get(StatType.HP));
            // 每点速度+0.1
            Assert.True(TSMath.Abs(baseStats.BaseStats.Get(StatType.SPD) - (FP)10.2) < (FP)0.001);
        }
    }
}

