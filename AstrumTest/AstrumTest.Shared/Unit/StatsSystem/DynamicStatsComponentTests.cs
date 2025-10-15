using Xunit;
using TrueSync;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Components;

namespace AstrumTest.Unit.StatsSystem
{
    /// <summary>
    /// DynamicStatsComponent 测试 - 测试动态资源和护盾逻辑
    /// </summary>
    [Trait("TestLevel", "Component")]
    [Trait("Category", "Unit")]
    [Trait("Module", "StatsSystem")]
    [Trait("Priority", "High")]
    public class DynamicStatsComponentTests
    {
        [Fact(DisplayName = "Get/Set - 正常设置和获取动态资源")]
        public void Get_Set_ShouldWorkCorrectly()
        {
            // Arrange
            var component = new DynamicStatsComponent();
            
            // Act
            component.Set(DynamicResourceType.CURRENT_HP, (FP)1000);
            component.Set(DynamicResourceType.CURRENT_MANA, (FP)100);
            
            // Assert
            Assert.Equal((FP)1000, component.Get(DynamicResourceType.CURRENT_HP));
            Assert.Equal((FP)100, component.Get(DynamicResourceType.CURRENT_MANA));
        }
        
        [Fact(DisplayName = "TakeDamage - 无护盾时直接扣血")]
        public void TakeDamage_NoShield_ShouldReduceHP()
        {
            // Arrange
            var dynamicStats = new DynamicStatsComponent();
            dynamicStats.Set(DynamicResourceType.CURRENT_HP, (FP)1000);
            
            var derivedStats = new DerivedStatsComponent();
            
            // Act
            FP actualDamage = dynamicStats.TakeDamage((FP)300, derivedStats);
            
            // Assert
            Assert.Equal((FP)300, actualDamage);
            Assert.Equal((FP)700, dynamicStats.Get(DynamicResourceType.CURRENT_HP));
        }
        
        [Fact(DisplayName = "TakeDamage - 有护盾时先扣除护盾")]
        public void TakeDamage_WithShield_ShouldDepleteShieldFirst()
        {
            // Arrange
            var dynamicStats = new DynamicStatsComponent();
            dynamicStats.Set(DynamicResourceType.CURRENT_HP, (FP)1000);
            dynamicStats.Set(DynamicResourceType.SHIELD, (FP)200);
            
            var derivedStats = new DerivedStatsComponent();
            
            // Act - 伤害300，护盾200
            FP actualDamage = dynamicStats.TakeDamage((FP)300, derivedStats);
            
            // Assert - 护盾消耗200，生命损失100
            Assert.Equal((FP)100, actualDamage); // 实际生命伤害
            Assert.Equal(FP.Zero, dynamicStats.Get(DynamicResourceType.SHIELD));
            Assert.Equal((FP)900, dynamicStats.Get(DynamicResourceType.CURRENT_HP));
        }
        
        [Fact(DisplayName = "TakeDamage - 护盾足够时生命不受损")]
        public void TakeDamage_ShieldEnough_ShouldNotReduceHP()
        {
            // Arrange
            var dynamicStats = new DynamicStatsComponent();
            dynamicStats.Set(DynamicResourceType.CURRENT_HP, (FP)1000);
            dynamicStats.Set(DynamicResourceType.SHIELD, (FP)500);
            
            var derivedStats = new DerivedStatsComponent();
            
            // Act - 伤害300，护盾500
            FP actualDamage = dynamicStats.TakeDamage((FP)300, derivedStats);
            
            // Assert - 生命不受损
            Assert.Equal(FP.Zero, actualDamage);
            Assert.Equal((FP)200, dynamicStats.Get(DynamicResourceType.SHIELD)); // 护盾剩余200
            Assert.Equal((FP)1000, dynamicStats.Get(DynamicResourceType.CURRENT_HP));
        }
        
        [Fact(DisplayName = "Heal - 正常恢复生命值")]
        public void Heal_ShouldRestoreHP()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.HP, (FP)1000);
            
            var derivedStats = new DerivedStatsComponent();
            derivedStats.RecalculateAll(baseStats);
            
            var dynamicStats = new DynamicStatsComponent();
            dynamicStats.Set(DynamicResourceType.CURRENT_HP, (FP)500);
            
            // Act
            FP actualHeal = dynamicStats.Heal((FP)300, derivedStats);
            
            // Assert
            Assert.Equal((FP)300, actualHeal);
            Assert.Equal((FP)800, dynamicStats.Get(DynamicResourceType.CURRENT_HP));
        }
        
        [Fact(DisplayName = "Heal - 不超过上限")]
        public void Heal_ShouldNotExceedMaxHP()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.HP, (FP)1000);
            
            var derivedStats = new DerivedStatsComponent();
            derivedStats.RecalculateAll(baseStats);
            
            var dynamicStats = new DynamicStatsComponent();
            dynamicStats.Set(DynamicResourceType.CURRENT_HP, (FP)900);
            
            // Act - 治疗300，但只有100空间
            FP actualHeal = dynamicStats.Heal((FP)300, derivedStats);
            
            // Assert
            Assert.Equal((FP)100, actualHeal);
            Assert.Equal((FP)1000, dynamicStats.Get(DynamicResourceType.CURRENT_HP));
        }
        
        [Fact(DisplayName = "ConsumeMana - 法力足够时消耗成功")]
        public void ConsumeMana_EnoughMana_ShouldSucceed()
        {
            // Arrange
            var component = new DynamicStatsComponent();
            component.Set(DynamicResourceType.CURRENT_MANA, (FP)100);
            
            // Act
            bool success = component.ConsumeMana((FP)30);
            
            // Assert
            Assert.True(success);
            Assert.Equal((FP)70, component.Get(DynamicResourceType.CURRENT_MANA));
        }
        
        [Fact(DisplayName = "ConsumeMana - 法力不足时失败")]
        public void ConsumeMana_NotEnoughMana_ShouldFail()
        {
            // Arrange
            var component = new DynamicStatsComponent();
            component.Set(DynamicResourceType.CURRENT_MANA, (FP)20);
            
            // Act
            bool success = component.ConsumeMana((FP)30);
            
            // Assert
            Assert.False(success);
            Assert.Equal((FP)20, component.Get(DynamicResourceType.CURRENT_MANA)); // 不变
        }
        
        [Fact(DisplayName = "AddEnergy - 自动限制在0-100")]
        public void AddEnergy_ShouldClampTo0_100()
        {
            // Arrange
            var component = new DynamicStatsComponent();
            component.Set(DynamicResourceType.ENERGY, (FP)80);
            
            // Act
            component.AddEnergy((FP)50); // 80 + 50 = 130，但限制到100
            
            // Assert
            Assert.Equal((FP)100, component.Get(DynamicResourceType.ENERGY));
        }
        
        [Fact(DisplayName = "AddRage - 自动限制在0-100")]
        public void AddRage_ShouldClampTo0_100()
        {
            // Arrange
            var component = new DynamicStatsComponent();
            
            // Act
            component.AddRage((FP)120); // 超过100
            
            // Assert
            Assert.Equal((FP)100, component.Get(DynamicResourceType.RAGE));
        }
        
        [Fact(DisplayName = "InitializeResources - 满血满蓝")]
        public void InitializeResources_ShouldSetToMax()
        {
            // Arrange
            var baseStats = new BaseStatsComponent();
            baseStats.BaseStats.Set(StatType.HP, (FP)1400);
            baseStats.BaseStats.Set(StatType.MAX_MANA, (FP)200);
            
            var derivedStats = new DerivedStatsComponent();
            derivedStats.RecalculateAll(baseStats);
            
            var dynamicStats = new DynamicStatsComponent();
            
            // Act
            dynamicStats.InitializeResources(derivedStats);
            
            // Assert
            Assert.Equal((FP)1400, dynamicStats.Get(DynamicResourceType.CURRENT_HP));
            Assert.Equal((FP)200, dynamicStats.Get(DynamicResourceType.CURRENT_MANA));
            Assert.Equal(FP.Zero, dynamicStats.Get(DynamicResourceType.ENERGY));
            Assert.Equal(FP.Zero, dynamicStats.Get(DynamicResourceType.RAGE));
        }
    }
}

