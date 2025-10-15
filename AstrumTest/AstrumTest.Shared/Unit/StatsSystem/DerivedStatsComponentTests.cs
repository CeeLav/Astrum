using Xunit;
using TrueSync;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Components;

namespace AstrumTest.Unit.StatsSystem
{
    /// <summary>
    /// DerivedStatsComponent 测试 - 测试修饰器计算和脏标记机制
    /// </summary>
    [Trait("TestLevel", "Component")]
    [Trait("Category", "Unit")]
    [Trait("Module", "StatsSystem")]
    [Trait("Priority", "High")]
    public class DerivedStatsComponentTests
    {
        [Fact(DisplayName = "RecalculateAll - 无修饰器时等于基础属性")]
        public void RecalculateAll_NoModifiers_ShouldEqualBaseStats()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.ATK, (FP)100);
            baseStats.BaseStats.Set(StatType.DEF, (FP)50);
            
            var derivedStats = new DerivedStatsComponent();
            
            // Act
            derivedStats.RecalculateAll(baseStats);
            
            // Assert
            Assert.Equal((FP)100, derivedStats.Get(StatType.ATK));
            Assert.Equal((FP)50, derivedStats.Get(StatType.DEF));
        }
        
        [Fact(DisplayName = "AddModifier - Flat修饰器应直接增加")]
        public void AddModifier_FlatType_ShouldAddDirectly()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.ATK, (FP)100);
            
            var derivedStats = new DerivedStatsComponent();
            derivedStats.AddModifier(StatType.ATK, new StatModifier
            {
                SourceId = 5001,
                Type = ModifierType.Flat,
                Value = (FP)50,
                Priority = 100
            });
            
            // Act
            derivedStats.RecalculateAll(baseStats);
            
            // Assert - 100 + 50 = 150
            Assert.Equal((FP)150, derivedStats.Get(StatType.ATK));
        }
        
        [Fact(DisplayName = "AddModifier - Percent修饰器应按百分比增加")]
        public void AddModifier_PercentType_ShouldAddPercentage()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.ATK, (FP)100);
            
            var derivedStats = new DerivedStatsComponent();
            derivedStats.AddModifier(StatType.ATK, new StatModifier
            {
                SourceId = 5001,
                Type = ModifierType.Percent,
                Value = (FP)0.2, // 20%
                Priority = 200
            });
            
            // Act
            derivedStats.RecalculateAll(baseStats);
            
            // Assert - 100 * (1 + 0.2) = 120
            FP result = derivedStats.Get(StatType.ATK);
            Assert.True(TSMath.Abs(result - (FP)120) < (FP)0.1,
                $"Expected 120, but got {(float)result}");
        }
        
        [Fact(DisplayName = "AddModifier - 混合修饰器应按公式计算")]
        public void AddModifier_MixedModifiers_ShouldFollowFormula()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.ATK, (FP)100);
            
            var derivedStats = new DerivedStatsComponent();
            
            // 添加固定值 +50
            derivedStats.AddModifier(StatType.ATK, new StatModifier
            {
                SourceId = 5001,
                Type = ModifierType.Flat,
                Value = (FP)50,
                Priority = 100
            });
            
            // 添加百分比 +20%
            derivedStats.AddModifier(StatType.ATK, new StatModifier
            {
                SourceId = 5002,
                Type = ModifierType.Percent,
                Value = (FP)0.2,
                Priority = 200
            });
            
            // 添加最终乘数 +30%
            derivedStats.AddModifier(StatType.ATK, new StatModifier
            {
                SourceId = 5003,
                Type = ModifierType.FinalMultiplier,
                Value = (FP)0.3,
                Priority = 300
            });
            
            // Act
            derivedStats.RecalculateAll(baseStats);
            
            // Assert - (100 + 50) * (1 + 0.2) * (1 + 0.3) = 150 * 1.2 * 1.3 = 234
            FP result = derivedStats.Get(StatType.ATK);
            Assert.True(TSMath.Abs(result - (FP)234) < (FP)0.1, 
                $"Expected 234, but got {(float)result}");
        }
        
        [Fact(DisplayName = "RemoveModifier - 移除修饰器后重新计算")]
        public void RemoveModifier_ShouldRecalculateWithoutRemovedModifier()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.ATK, (FP)100);
            
            var derivedStats = new DerivedStatsComponent();
            derivedStats.AddModifier(StatType.ATK, new StatModifier
            {
                SourceId = 5001,
                Type = ModifierType.Flat,
                Value = (FP)50,
                Priority = 100
            });
            derivedStats.RecalculateAll(baseStats);
            Assert.Equal((FP)150, derivedStats.Get(StatType.ATK));
            
            // Act
            derivedStats.RemoveModifier(5001);
            derivedStats.RecalculateAll(baseStats);
            
            // Assert - 应该恢复到基础值
            Assert.Equal((FP)100, derivedStats.Get(StatType.ATK));
        }
        
        [Fact(DisplayName = "脏标记机制 - RecalculateIfDirty只在需要时重算")]
        public void DirtyFlag_RecalculateIfDirty_ShouldOptimizePerformance()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.ATK, (FP)100);
            
            var derivedStats = new DerivedStatsComponent();
            derivedStats.RecalculateAll(baseStats);
            
            // 修改基础属性，但不标记为脏
            baseStats.BaseStats.Set(StatType.ATK, (FP)200);
            
            // Act - 不会重算，因为没有脏标记
            derivedStats.RecalculateIfDirty(baseStats);
            
            // Assert - 仍然是旧值
            Assert.Equal((FP)100, derivedStats.Get(StatType.ATK));
            
            // 标记为脏后重算
            derivedStats.MarkDirty();
            derivedStats.RecalculateIfDirty(baseStats);
            
            // Assert - 现在是新值
            Assert.Equal((FP)200, derivedStats.Get(StatType.ATK));
        }
        
        [Fact(DisplayName = "ClearModifiers - 清空所有修饰器")]
        public void ClearModifiers_ShouldRemoveAllModifiers()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.ATK, (FP)100);
            
            var derivedStats = new DerivedStatsComponent();
            derivedStats.AddModifier(StatType.ATK, new StatModifier { SourceId = 1, Type = ModifierType.Flat, Value = (FP)50, Priority = 100 });
            derivedStats.AddModifier(StatType.ATK, new StatModifier { SourceId = 2, Type = ModifierType.Percent, Value = (FP)0.2, Priority = 200 });
            derivedStats.RecalculateAll(baseStats);
            
            // Act
            derivedStats.ClearModifiers();
            derivedStats.RecalculateAll(baseStats);
            
            // Assert
            Assert.Equal((FP)100, derivedStats.Get(StatType.ATK));
        }
    }
}

