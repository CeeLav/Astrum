using Xunit;
using TrueSync;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using AstrumTest.Shared.Fixtures;

namespace AstrumTest.Unit.StatsSystem
{
    /// <summary>
    /// BuffComponent 测试 - 测试 Buff 叠加和修饰器解析
    /// </summary>
    [Trait("TestLevel", "Component")]
    [Trait("Category", "Unit")]
    [Trait("Module", "StatsSystem")]
    [Trait("Priority", "High")]
    public class BuffComponentTests : IClassFixture<ConfigFixture>
    {
        private readonly ConfigFixture _configFixture;

        public BuffComponentTests(ConfigFixture configFixture)
        {
            _configFixture = configFixture;
        }

        [Fact(DisplayName = "AddBuff - 添加可叠加Buff应增加层数")]
        public void AddBuff_Stackable_ShouldIncreaseStackCount()
        {
            // Arrange
            var component = new BuffComponent();
            var buff1 = new BuffInstance
            {
                BuffId = 5001, // 力量祝福（可叠加，最多3层）
                Duration = 600,
                RemainingFrames = 600,
                Stackable = true,
                MaxStack = 3,
                StackCount = 1
            };
            
            // Act
            component.AddBuff(buff1);
            component.AddBuff(buff1);
            component.AddBuff(buff1);
            
            // Assert
            Assert.Single(component.Buffs); // 只有一个Buff实例
            Assert.Equal(3, component.Buffs[0].StackCount); // 叠加到3层
        }
        
        [Fact(DisplayName = "AddBuff - 超过最大层数时不再叠加")]
        public void AddBuff_ExceedMaxStack_ShouldNotIncrease()
        {
            // Arrange
            var component = new BuffComponent();
            var buff = new BuffInstance
            {
                BuffId = 5001,
                Duration = 600,
                RemainingFrames = 600,
                Stackable = true,
                MaxStack = 3,
                StackCount = 1
            };
            
            // Act - 添加4次
            component.AddBuff(buff);
            component.AddBuff(buff);
            component.AddBuff(buff);
            component.AddBuff(buff);
            
            // Assert - 最多3层
            Assert.Equal(3, component.Buffs[0].StackCount);
        }
        
        [Fact(DisplayName = "AddBuff - 不可叠加Buff应刷新持续时间")]
        public void AddBuff_NotStackable_ShouldRefreshDuration()
        {
            // Arrange
            var component = new BuffComponent();
            var buff1 = new BuffInstance
            {
                BuffId = 5002, // 极速（不可叠加）
                Duration = 300,
                RemainingFrames = 100, // 快到期了
                Stackable = false,
                MaxStack = 1
            };
            
            var buff2 = new BuffInstance
            {
                BuffId = 5002,
                Duration = 300,
                RemainingFrames = 300,
                Stackable = false,
                MaxStack = 1
            };
            
            // Act
            component.AddBuff(buff1);
            component.AddBuff(buff2); // 刷新
            
            // Assert
            Assert.Single(component.Buffs);
            Assert.Equal(300, component.Buffs[0].RemainingFrames); // 持续时间被刷新
        }
        
        [Fact(DisplayName = "UpdateBuffs - 每帧递减持续时间")]
        public void UpdateBuffs_ShouldDecreaseRemainingFrames()
        {
            // Arrange
            var component = new BuffComponent();
            var buff = new BuffInstance
            {
                BuffId = 5001,
                Duration = 600,
                RemainingFrames = 10,
                Stackable = false,
                MaxStack = 1
            };
            component.AddBuff(buff);
            
            // Act - 更新5帧
            for (int i = 0; i < 5; i++)
            {
                component.UpdateBuffs();
            }
            
            // Assert
            Assert.Equal(5, component.Buffs[0].RemainingFrames);
        }
        
        [Fact(DisplayName = "UpdateBuffs - 持续时间结束时移除Buff")]
        public void UpdateBuffs_ExpiredBuff_ShouldBeRemoved()
        {
            // Arrange
            var component = new BuffComponent();
            var buff = new BuffInstance
            {
                BuffId = 5001,
                Duration = 600,
                RemainingFrames = 2,
                Stackable = false,
                MaxStack = 1
            };
            component.AddBuff(buff);
            
            // Act - 更新3帧
            component.UpdateBuffs();
            component.UpdateBuffs();
            component.UpdateBuffs();
            
            // Assert - Buff应该被移除
            Assert.Empty(component.Buffs);
        }
        
        [Fact(DisplayName = "GetAllModifiers - 解析Buff修饰器字符串")]
        public void GetAllModifiers_ShouldParseBuffModifiers()
        {
            // Arrange
            var component = new BuffComponent();
            var buff = new BuffInstance
            {
                BuffId = 5001, // 力量祝福："ATK:Percent:200"（+20%攻击）
                Duration = 600,
                RemainingFrames = 600,
                Stackable = true,
                MaxStack = 3,
                StackCount = 2 // 2层
            };
            component.AddBuff(buff);
            
            // Act
            var modifiers = component.GetAllModifiers();
            
            // Assert
            Assert.True(modifiers.ContainsKey(StatType.ATK));
            var atkModifiers = modifiers[StatType.ATK];
            Assert.Single(atkModifiers);
            
            var mod = atkModifiers[0];
            Assert.Equal(ModifierType.Percent, mod.Type);
            // 200/1000 = 0.2，2层 = 0.4
            Assert.True(TSMath.Abs(mod.Value - (FP)0.4) < (FP)0.001,
                $"Expected 0.4, but got {(float)mod.Value}");
        }
        
        [Fact(DisplayName = "RemoveBuff - 移除指定Buff")]
        public void RemoveBuff_ShouldRemoveSpecificBuff()
        {
            // Arrange
            var component = new BuffComponent();
            component.AddBuff(new BuffInstance { BuffId = 5001, Duration = 600, RemainingFrames = 600 });
            component.AddBuff(new BuffInstance { BuffId = 5002, Duration = 300, RemainingFrames = 300 });
            
            // Act
            component.RemoveBuff(5001);
            
            // Assert
            Assert.Single(component.Buffs);
            Assert.Equal(5002, component.Buffs[0].BuffId);
        }
    }
}

