using Xunit;
using TrueSync;
using Astrum.LogicCore.Stats;
using StatsContainer = Astrum.LogicCore.Stats.Stats;

namespace AstrumTest.Unit.StatsSystem
{
    /// <summary>
    /// Stats 容器测试 - 测试基础的属性存储功能
    /// </summary>
    [Trait("TestLevel", "Pure")]
    [Trait("Category", "Unit")]
    [Trait("Module", "StatsSystem")]
    [Trait("Priority", "High")]
    public class StatsTests
    {
        [Fact(DisplayName = "Get/Set - 正常设置和获取属性值")]
        public void Get_Set_ShouldWorkCorrectly()
        {
            // Arrange
            var stats = new StatsContainer();
            
            // Act
            stats.Set(StatType.ATK, (FP)100);
            stats.Set(StatType.HP, (FP)1000);
            
            // Assert
            Assert.Equal((FP)100, stats.Get(StatType.ATK));
            Assert.Equal((FP)1000, stats.Get(StatType.HP));
        }
        
        [Fact(DisplayName = "Get - 不存在的属性返回0")]
        public void Get_NonExistentStat_ShouldReturnZero()
        {
            // Arrange
            var stats = new StatsContainer();
            
            // Act
            FP value = stats.Get(StatType.DEF);
            
            // Assert
            Assert.Equal(FP.Zero, value);
        }
        
        [Fact(DisplayName = "Add - 累加属性值")]
        public void Add_ShouldAccumulateValue()
        {
            // Arrange
            var stats = new StatsContainer();
            stats.Set(StatType.ATK, (FP)100);
            
            // Act
            stats.Add(StatType.ATK, (FP)50);
            stats.Add(StatType.ATK, (FP)30);
            
            // Assert
            Assert.Equal((FP)180, stats.Get(StatType.ATK));
        }
        
        [Fact(DisplayName = "Add - 初始为0时应正常累加")]
        public void Add_OnZeroValue_ShouldSetValue()
        {
            // Arrange
            var stats = new StatsContainer();
            
            // Act
            stats.Add(StatType.SPD, (FP)10.5);
            
            // Assert
            Assert.Equal((FP)10.5, stats.Get(StatType.SPD));
        }
        
        [Fact(DisplayName = "Clear - 清空所有属性")]
        public void Clear_ShouldRemoveAllStats()
        {
            // Arrange
            var stats = new StatsContainer();
            stats.Set(StatType.ATK, (FP)100);
            stats.Set(StatType.DEF, (FP)50);
            stats.Set(StatType.HP, (FP)1000);
            
            // Act
            stats.Clear();
            
            // Assert
            Assert.Equal(FP.Zero, stats.Get(StatType.ATK));
            Assert.Equal(FP.Zero, stats.Get(StatType.DEF));
            Assert.Equal(FP.Zero, stats.Get(StatType.HP));
            Assert.Empty(stats.Values);
        }
        
        [Fact(DisplayName = "Clone - 深拷贝属性")]
        public void Clone_ShouldCreateDeepCopy()
        {
            // Arrange
            var original = new StatsContainer();
            original.Set(StatType.ATK, (FP)100);
            original.Set(StatType.DEF, (FP)50);
            
            // Act
            var clone = original.Clone();
            clone.Set(StatType.ATK, (FP)200); // 修改克隆对象
            
            // Assert
            Assert.Equal((FP)100, original.Get(StatType.ATK)); // 原对象不受影响
            Assert.Equal((FP)200, clone.Get(StatType.ATK));
            Assert.Equal((FP)50, clone.Get(StatType.DEF));
        }
        
        [Fact(DisplayName = "定点数精度 - 验证小数运算精度")]
        public void FixedPoint_Precision_ShouldBeAccurate()
        {
            // Arrange
            var stats = new StatsContainer();
            
            // Act - 模拟速度属性（小数）
            stats.Set(StatType.SPD, (FP)10.5);
            stats.Add(StatType.SPD, (FP)0.3);
            
            // Assert
            FP expected = (FP)10.8;
            FP actual = stats.Get(StatType.SPD);
            
            // 定点数比较（允许极小误差）
            Assert.True(TSMath.Abs(expected - actual) < (FP)0.001, 
                $"Expected {(float)expected}, but got {(float)actual}");
        }
    }
}

