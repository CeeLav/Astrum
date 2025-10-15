using Xunit;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Components;

namespace AstrumTest.Unit.StatsSystem
{
    /// <summary>
    /// StateComponent 测试 - 测试状态管理和辅助方法
    /// </summary>
    [Trait("TestLevel", "Pure")]
    [Trait("Category", "Unit")]
    [Trait("Module", "StatsSystem")]
    [Trait("Priority", "High")]
    public class StateComponentTests
    {
        [Fact(DisplayName = "Get/Set - 正常设置和获取状态")]
        public void Get_Set_ShouldWorkCorrectly()
        {
            // Arrange
            var component = new StateComponent();
            
            // Act
            component.Set(StateType.STUNNED, true);
            component.Set(StateType.INVINCIBLE, true);
            
            // Assert
            Assert.True(component.Get(StateType.STUNNED));
            Assert.True(component.Get(StateType.INVINCIBLE));
            Assert.False(component.Get(StateType.DEAD));
        }
        
        [Fact(DisplayName = "CanMove - 晕眩时不能移动")]
        public void CanMove_WhenStunned_ShouldReturnFalse()
        {
            // Arrange
            var component = new StateComponent();
            component.Set(StateType.STUNNED, true);
            
            // Act
            bool canMove = component.CanMove();
            
            // Assert
            Assert.False(canMove);
        }
        
        [Fact(DisplayName = "CanMove - 冰冻时不能移动")]
        public void CanMove_WhenFrozen_ShouldReturnFalse()
        {
            // Arrange
            var component = new StateComponent();
            component.Set(StateType.FROZEN, true);
            
            // Act & Assert
            Assert.False(component.CanMove());
        }
        
        [Fact(DisplayName = "CanMove - 正常状态可以移动")]
        public void CanMove_NormalState_ShouldReturnTrue()
        {
            // Arrange
            var component = new StateComponent();
            
            // Act & Assert
            Assert.True(component.CanMove());
        }
        
        [Fact(DisplayName = "CanAttack - 晕眩时不能攻击")]
        public void CanAttack_WhenStunned_ShouldReturnFalse()
        {
            // Arrange
            var component = new StateComponent();
            component.Set(StateType.STUNNED, true);
            
            // Act & Assert
            Assert.False(component.CanAttack());
        }
        
        [Fact(DisplayName = "CanAttack - 缴械时不能攻击")]
        public void CanAttack_WhenDisarmed_ShouldReturnFalse()
        {
            // Arrange
            var component = new StateComponent();
            component.Set(StateType.DISARMED, true);
            
            // Act & Assert
            Assert.False(component.CanAttack());
        }
        
        [Fact(DisplayName = "CanCastSkill - 沉默时不能释放技能")]
        public void CanCastSkill_WhenSilenced_ShouldReturnFalse()
        {
            // Arrange
            var component = new StateComponent();
            component.Set(StateType.SILENCED, true);
            
            // Act & Assert
            Assert.False(component.CanCastSkill());
        }
        
        [Fact(DisplayName = "CanTakeDamage - 无敌时不能受到伤害")]
        public void CanTakeDamage_WhenInvincible_ShouldReturnFalse()
        {
            // Arrange
            var component = new StateComponent();
            component.Set(StateType.INVINCIBLE, true);
            
            // Act & Assert
            Assert.False(component.CanTakeDamage());
        }
        
        [Fact(DisplayName = "CanTakeDamage - 死亡时不能受到伤害")]
        public void CanTakeDamage_WhenDead_ShouldReturnFalse()
        {
            // Arrange
            var component = new StateComponent();
            component.Set(StateType.DEAD, true);
            
            // Act & Assert
            Assert.False(component.CanTakeDamage());
        }
        
        [Fact(DisplayName = "Clear - 清空所有状态")]
        public void Clear_ShouldRemoveAllStates()
        {
            // Arrange
            var component = new StateComponent();
            component.Set(StateType.STUNNED, true);
            component.Set(StateType.FROZEN, true);
            component.Set(StateType.INVINCIBLE, true);
            
            // Act
            component.Clear();
            
            // Assert
            Assert.False(component.Get(StateType.STUNNED));
            Assert.False(component.Get(StateType.FROZEN));
            Assert.False(component.Get(StateType.INVINCIBLE));
            Assert.Empty(component.States);
        }
    }
}

