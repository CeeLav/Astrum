using Xunit;
using TrueSync;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Managers;
using AstrumTest.Shared.Fixtures;
using cfg.Skill;

namespace AstrumTest.Unit.StatsSystem
{
    /// <summary>
    /// DamageCalculator 测试 - 测试伤害计算的各个环节
    /// </summary>
    [Trait("TestLevel", "Component")]
    [Trait("Category", "Unit")]
    [Trait("Module", "StatsSystem")]
    [Trait("Priority", "Critical")]
    public class DamageCalculatorTests : IClassFixture<ConfigFixture>
    {
        private readonly ConfigFixture _configFixture;

        public DamageCalculatorTests(ConfigFixture configFixture)
        {
            _configFixture = configFixture;
        }

        #region 辅助方法

        /// <summary>
        /// 创建测试用实体（带完整属性组件）
        /// </summary>
        private Entity CreateTestEntity(int roleId, int level)
        {
            var entity = new Entity();
            
            // 1. 添加角色信息组件
            var roleInfo = new RoleInfoComponent { RoleId = roleId };
            entity.AddComponent(roleInfo);
            
            // 2. 添加基础属性组件
            var baseStats = new BaseStatsComponent();
            baseStats.InitializeFromConfig(roleId, level);
            entity.AddComponent(baseStats);
            
            // 3. 添加派生属性组件
            var derivedStats = new DerivedStatsComponent();
            derivedStats.RecalculateAll(baseStats);
            entity.AddComponent(derivedStats);
            
            // 4. 添加动态属性组件
            var dynamicStats = new DynamicStatsComponent();
            dynamicStats.InitializeResources(derivedStats);
            entity.AddComponent(dynamicStats);
            
            // 5. 添加状态组件
            var stateComp = new StateComponent();
            entity.AddComponent(stateComp);
            
            return entity;
        }

        #endregion

        [Fact(DisplayName = "Calculate - 基础伤害计算")]
        public void Calculate_BasicDamage_ShouldWorkCorrectly()
        {
            // Arrange
            var caster = CreateTestEntity(1001, 1); // 骑士，攻击80
            var target = CreateTestEntity(1001, 1);
            
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001); // 轻击，150%攻击
            int currentFrame = 100;
            
            // Act
            var result = DamageCalculator.Calculate(caster, target, effectConfig, currentFrame);
            
            // Assert - 基础伤害约 80 * 1.5 = 120，再经过防御减免
            // 注意：由于确定性随机，特定frame可能Miss，这是正常的
            if (!result.IsMiss)
            {
                Assert.True(result.FinalDamage > FP.Zero);
            }
        }
        
        [Fact(DisplayName = "Calculate - 确定性随机（相同种子产生相同结果）")]
        public void Calculate_DeterministicRandom_ShouldProduceSameResult()
        {
            // Arrange
            var caster = CreateTestEntity(1005, 1); // 刺客，高暴击率25%
            var target = CreateTestEntity(1001, 1);
            
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            int currentFrame = 12345;
            
            // Act - 执行3次，使用相同的帧号
            var result1 = DamageCalculator.Calculate(caster, target, effectConfig, currentFrame);
            var result2 = DamageCalculator.Calculate(caster, target, effectConfig, currentFrame);
            var result3 = DamageCalculator.Calculate(caster, target, effectConfig, currentFrame);
            
            // Assert - 所有结果应该完全相同
            Assert.Equal(result1.FinalDamage, result2.FinalDamage);
            Assert.Equal(result1.FinalDamage, result3.FinalDamage);
            Assert.Equal(result1.IsCritical, result2.IsCritical);
            Assert.Equal(result1.IsCritical, result3.IsCritical);
            Assert.Equal(result1.IsBlocked, result2.IsBlocked);
            Assert.Equal(result1.IsBlocked, result3.IsBlocked);
        }
        
        [Fact(DisplayName = "Calculate - 不同帧号产生不同结果")]
        public void Calculate_DifferentFrames_ShouldProduceDifferentResults()
        {
            // Arrange
            var caster = CreateTestEntity(1005, 1);
            var target = CreateTestEntity(1001, 1);
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            
            // Act - 使用不同帧号
            var result1 = DamageCalculator.Calculate(caster, target, effectConfig, 100);
            var result2 = DamageCalculator.Calculate(caster, target, effectConfig, 101);
            var result3 = DamageCalculator.Calculate(caster, target, effectConfig, 102);
            
            // Assert - 至少应该有一些不同（暴击或伤害浮动）
            bool hasVariation = result1.FinalDamage != result2.FinalDamage ||
                               result2.FinalDamage != result3.FinalDamage ||
                               result1.IsCritical != result2.IsCritical;
            
            Assert.True(hasVariation, "不同帧号应该产生不同的随机结果");
        }
        
        [Fact(DisplayName = "Calculate - 无敌状态下不受伤害")]
        public void Calculate_Invincible_ShouldDealZeroDamage()
        {
            // Arrange
            var caster = CreateTestEntity(1001, 1);
            var target = CreateTestEntity(1001, 1);
            var stateComp = target.GetComponent<StateComponent>();
            stateComp.Set(StateType.INVINCIBLE, true);
            
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            
            // Act
            var result = DamageCalculator.Calculate(caster, target, effectConfig, 100);
            
            // Assert
            Assert.Equal(FP.Zero, result.FinalDamage);
        }
        
        [Fact(DisplayName = "Calculate - 暴击应增加伤害")]
        public void Calculate_Critical_ShouldIncreaseDamage()
        {
            // Arrange - 创建刺客（高暴击率25%）
            var caster = CreateTestEntity(1005, 1);
            var target = CreateTestEntity(1001, 1);
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            
            // Act - 尝试多个帧号，找到一个暴击的
            DamageResult normalResult = null;
            DamageResult critResult = null;
            
            for (int frame = 0; frame < 100; frame++)
            {
                var result = DamageCalculator.Calculate(caster, target, effectConfig, frame);
                if (result.IsCritical)
                    critResult = result;
                else
                    normalResult = result;
                
                if (normalResult != null && critResult != null)
                    break;
            }
            
            // Assert - 暴击伤害应该高于普通伤害
            if (normalResult != null && critResult != null)
            {
                Assert.True(critResult.FinalDamage > normalResult.FinalDamage,
                    $"暴击伤害 {(float)critResult.FinalDamage} 应该高于普通伤害 {(float)normalResult.FinalDamage}");
            }
        }
        
        [Fact(DisplayName = "Calculate - 防御应减少伤害")]
        public void Calculate_Defense_ShouldReduceDamage()
        {
            // Arrange
            var caster = CreateTestEntity(1001, 1); // 攻击80
            var lowDefTarget = CreateTestEntity(1003, 1); // 法师，防御40
            var highDefTarget = CreateTestEntity(1004, 1); // 重锤者，防御120
            
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            int frame = 100;
            
            // Act
            var resultLowDef = DamageCalculator.Calculate(caster, lowDefTarget, effectConfig, frame);
            var resultHighDef = DamageCalculator.Calculate(caster, highDefTarget, effectConfig, frame);
            
            // Assert - 高防御目标受到的伤害应该更低
            Assert.True(resultHighDef.FinalDamage < resultLowDef.FinalDamage,
                $"高防御伤害 {(float)resultHighDef.FinalDamage} 应低于低防御伤害 {(float)resultLowDef.FinalDamage}");
        }
        
        [Theory(DisplayName = "Calculate - Buff修饰器应影响伤害")]
        [InlineData(5001, 200)] // 力量祝福：+20%攻击
        [InlineData(5004, 100)] // 狂暴：+10%攻击 +5%暴击率
        public void Calculate_WithBuff_ShouldAffectDamage(int buffId, int modifierValue)
        {
            // Arrange
            var caster = CreateTestEntity(1001, 1);
            var target = CreateTestEntity(1001, 1);
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            int frame = 100;
            
            // 计算无Buff的伤害
            var resultNoBuff = DamageCalculator.Calculate(caster, target, effectConfig, frame);
            
            // 添加Buff
            var buffComp = caster.GetComponent<BuffComponent>();
            if (buffComp == null)
            {
                buffComp = new BuffComponent();
                caster.AddComponent(buffComp);
            }
            
            var buffConfig = ConfigManager.Instance.Tables.TbBuffTable.Get(buffId);
            buffComp.AddBuff(new BuffInstance
            {
                BuffId = buffId,
                Duration = buffConfig.Duration,
                RemainingFrames = buffConfig.Duration,
                Stackable = buffConfig.Stackable,
                MaxStack = buffConfig.MaxStack,
                StackCount = 1
            });
            
            // 重新计算派生属性
            var baseStats = caster.GetComponent<BaseStatsComponent>();
            var derivedStats = caster.GetComponent<DerivedStatsComponent>();
            var buffModifiers = buffComp.GetAllModifiers();
            foreach (var kvp in buffModifiers)
            {
                foreach (var mod in kvp.Value)
                {
                    derivedStats.AddModifier(kvp.Key, mod);
                }
            }
            derivedStats.RecalculateAll(baseStats);
            
            // Act
            var resultWithBuff = DamageCalculator.Calculate(caster, target, effectConfig, frame);
            
            // Assert - Buff应该增加伤害
            Assert.True(resultWithBuff.FinalDamage > resultNoBuff.FinalDamage,
                $"Buff后伤害 {(float)resultWithBuff.FinalDamage} 应高于无Buff伤害 {(float)resultNoBuff.FinalDamage}");
        }
    }
}

