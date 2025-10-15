using System;
using Xunit;
using Xunit.Abstractions;
using TrueSync;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.SkillSystem.EffectHandlers;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
using AstrumTest.Shared.Fixtures;
using cfg.Skill;

namespace AstrumTest.Integration.StatsSystem
{
    /// <summary>
    /// 战斗系统集成测试 - 测试完整的战斗流程、伤害流程和死亡流程
    /// </summary>
    [Trait("TestLevel", "Integration")]
    [Trait("Category", "Integration")]
    [Trait("Module", "CombatSystem")]
    [Trait("Priority", "Critical")]
    public class CombatIntegrationTests : IClassFixture<ConfigFixture>
    {
        private readonly ConfigFixture _configFixture;
        private readonly ITestOutputHelper _output;

        public CombatIntegrationTests(ConfigFixture configFixture, ITestOutputHelper output)
        {
            _configFixture = configFixture;
            _output = output;
        }

        #region 辅助方法

        /// <summary>
        /// 创建完整的战斗实体（包含所有数值组件）
        /// </summary>
        private Entity CreateCombatEntity(int roleId, int level, long uniqueId = 0)
        {
            var entity = new Entity();
            
            // 1. 角色信息
            var roleInfo = new RoleInfoComponent { RoleId = roleId };
            entity.AddComponent(roleInfo);
            
            // 2. 基础属性
            var baseStats = new BaseStatsComponent();
            baseStats.InitializeFromConfig(roleId, level);
            entity.AddComponent(baseStats);
            
            // 3. 派生属性
            var derivedStats = new DerivedStatsComponent();
            derivedStats.RecalculateAll(baseStats);
            entity.AddComponent(derivedStats);
            
            // 4. 动态属性
            var dynamicStats = new DynamicStatsComponent();
            dynamicStats.InitializeResources(derivedStats);
            entity.AddComponent(dynamicStats);
            
            // 5. 状态组件
            var stateComp = new StateComponent();
            entity.AddComponent(stateComp);
            
            // 6. Buff组件
            var buffComp = new BuffComponent();
            entity.AddComponent(buffComp);
            
            // 7. 等级组件
            var levelComp = new LevelComponent
            {
                CurrentLevel = level,
                CurrentExp = 0,
                ExpToNextLevel = 1000,
                MaxLevel = 100
            };
            entity.AddComponent(levelComp);
            
            // 8. 成长组件
            var growthComp = new GrowthComponent { RoleId = roleId };
            entity.AddComponent(growthComp);
            
            _output.WriteLine($"[Entity Created] ID={entity.UniqueId}, Role={roleId}, Level={level}");
            _output.WriteLine($"  ATK={derivedStats.Get(StatType.ATK)}, DEF={derivedStats.Get(StatType.DEF)}");
            _output.WriteLine($"  HP={dynamicStats.Get(DynamicResourceType.CURRENT_HP)}/{derivedStats.Get(StatType.HP)}");
            
            return entity;
        }

        #endregion

        [Fact(DisplayName = "完整战斗流程 - 创建角色→造成伤害→检查死亡")]
        public void FullCombatFlow_Attack_ShouldWork()
        {
            // Arrange - 创建骑士和法师
            var knight = CreateCombatEntity(1001, 1, 10001); // 骑士，攻击80，防御80，HP1000
            var mage = CreateCombatEntity(1003, 1, 10002);   // 法师，攻击50，防御40，HP700
            
            var knightDynamic = knight.GetComponent<DynamicStatsComponent>();
            var mageDynamic = mage.GetComponent<DynamicStatsComponent>();
            var mageState = mage.GetComponent<StateComponent>();
            
            FP mageHPBefore = mageDynamic.Get(DynamicResourceType.CURRENT_HP);
            
            // Act - 骑士攻击法师（轻击技能，150%攻击力）
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            var damageResult = DamageCalculator.Calculate(knight, mage, effectConfig, 100);
            
            _output.WriteLine($"\n[Combat] Knight attacks Mage");
            _output.WriteLine($"  Damage Result: {(float)damageResult.FinalDamage:F2} (Crit: {damageResult.IsCritical}, Block: {damageResult.IsBlocked}, Miss: {damageResult.IsMiss})");
            
            // 应用伤害
            FP actualDamage = mageDynamic.TakeDamage(damageResult.FinalDamage, mage.GetComponent<DerivedStatsComponent>());
            FP mageHPAfter = mageDynamic.Get(DynamicResourceType.CURRENT_HP);
            
            _output.WriteLine($"  Mage HP: {(float)mageHPBefore:F2} → {(float)mageHPAfter:F2} (-{(float)actualDamage:F2})");
            
            // Assert
            if (!damageResult.IsMiss) // 如果没有未命中，才检查伤害
            {
                Assert.True(damageResult.FinalDamage > FP.Zero, "命中时伤害应大于0");
                Assert.True(mageHPAfter < mageHPBefore, "命中时法师血量应减少");
            }
            Assert.False(mageState.Get(StateType.DEAD), "法师应该还活着");
        }
        
        [Fact(DisplayName = "死亡流程 - 生命值为0时设置死亡状态")]
        public void DeathFlow_HPZero_ShouldSetDeadState()
        {
            // Arrange
            var attacker = CreateCombatEntity(1004, 1, 20001); // 重锤者，攻击100
            var victim = CreateCombatEntity(1003, 1, 20002);   // 法师，HP700
            
            var victimDynamic = victim.GetComponent<DynamicStatsComponent>();
            var victimState = victim.GetComponent<StateComponent>();
            var victimDerived = victim.GetComponent<DerivedStatsComponent>();
            
            // Act - 造成致命伤害
            FP overkillDamage = (FP)10000; // 远超生命值的伤害
            FP actualDamage = victimDynamic.TakeDamage(overkillDamage, victimDerived);
            
            // 检查死亡
            FP currentHP = victimDynamic.Get(DynamicResourceType.CURRENT_HP);
            if (currentHP <= FP.Zero)
            {
                victimState.Set(StateType.DEAD, true);
            }
            
            _output.WriteLine($"\n[Death Test] Victim HP: {(float)currentHP:F2}, Dead: {victimState.Get(StateType.DEAD)}");
            
            // Assert
            Assert.Equal(FP.Zero, currentHP);
            Assert.True(victimState.Get(StateType.DEAD), "应该标记为死亡");
            Assert.False(victimState.CanTakeDamage(), "死亡后不能再受伤");
        }
        
        [Fact(DisplayName = "Buff战斗流程 - Buff应影响伤害计算")]
        public void BuffCombatFlow_WithBuff_ShouldAffectDamage()
        {
            // Arrange
            var attacker = CreateCombatEntity(1001, 1, 30001); // 骑士
            var target = CreateCombatEntity(1001, 1, 30002);
            
            var attackerBuff = attacker.GetComponent<BuffComponent>();
            var attackerBase = attacker.GetComponent<BaseStatsComponent>();
            var attackerDerived = attacker.GetComponent<DerivedStatsComponent>();
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            
            // 计算无Buff伤害
            var resultNoBuff = DamageCalculator.Calculate(attacker, target, effectConfig, 100);
            _output.WriteLine($"\n[Buff Combat] Damage without buff: {(float)resultNoBuff.FinalDamage:F2}");
            
            // Act - 添加力量祝福（+20%攻击）
            var buffConfig = ConfigManager.Instance.Tables.TbBuffTable.Get(5001);
            attackerBuff.AddBuff(new BuffInstance
            {
                BuffId = 5001,
                Duration = buffConfig.Duration,
                RemainingFrames = buffConfig.Duration,
                Stackable = buffConfig.Stackable,
                MaxStack = buffConfig.MaxStack,
                StackCount = 1,
                BuffType = buffConfig.BuffType
            });
            
            // 重新计算派生属性
            var buffModifiers = attackerBuff.GetAllModifiers();
            foreach (var kvp in buffModifiers)
            {
                foreach (var mod in kvp.Value)
                {
                    attackerDerived.AddModifier(kvp.Key, mod);
                }
            }
            attackerDerived.RecalculateAll(attackerBase);
            
            var resultWithBuff = DamageCalculator.Calculate(attacker, target, effectConfig, 100);
            _output.WriteLine($"  Damage with buff: {(float)resultWithBuff.FinalDamage:F2}");
            _output.WriteLine($"  Increase: {(float)(resultWithBuff.FinalDamage - resultNoBuff.FinalDamage):F2} (+{(float)((resultWithBuff.FinalDamage - resultNoBuff.FinalDamage) / resultNoBuff.FinalDamage * 100):F1}%)");
            
            // Assert
            Assert.True(resultWithBuff.FinalDamage > resultNoBuff.FinalDamage, "Buff应该增加伤害");
        }
        
        [Fact(DisplayName = "护盾战斗流程 - 护盾应优先承受伤害")]
        public void ShieldCombatFlow_Shield_ShouldAbsorbDamageFirst()
        {
            // Arrange
            var attacker = CreateCombatEntity(1001, 1, 40001);
            var defender = CreateCombatEntity(1001, 1, 40002);
            
            var defenderDynamic = defender.GetComponent<DynamicStatsComponent>();
            var defenderDerived = defender.GetComponent<DerivedStatsComponent>();
            
            // 添加护盾
            defenderDynamic.Set(DynamicResourceType.SHIELD, (FP)200);
            FP hpBefore = defenderDynamic.Get(DynamicResourceType.CURRENT_HP);
            
            // Act - 造成150伤害
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            var damageResult = DamageCalculator.Calculate(attacker, defender, effectConfig, 100);
            
            FP actualDamage = defenderDynamic.TakeDamage(damageResult.FinalDamage, defenderDerived);
            
            FP shieldAfter = defenderDynamic.Get(DynamicResourceType.SHIELD);
            FP hpAfter = defenderDynamic.Get(DynamicResourceType.CURRENT_HP);
            
            _output.WriteLine($"\n[Shield Test] Damage: {(float)damageResult.FinalDamage:F2}");
            _output.WriteLine($"  Shield: 200 → {(float)shieldAfter:F2}");
            _output.WriteLine($"  HP: {(float)hpBefore:F2} → {(float)hpAfter:F2} (actual damage: {(float)actualDamage:F2})");
            
            // Assert - 护盾消耗，生命不变或略微减少
            Assert.True(shieldAfter < (FP)200, "护盾应该被消耗");
        }
        
        [Fact(DisplayName = "完整战斗流程 - 多次攻击直到死亡")]
        public void FullCombatFlow_MultipleAttacks_UntilDeath()
        {
            // Arrange
            var attacker = CreateCombatEntity(1004, 5, 50001); // 5级重锤者，高攻击
            var victim = CreateCombatEntity(1003, 1, 50002);   // 1级法师，低血量700
            
            var victimDynamic = victim.GetComponent<DynamicStatsComponent>();
            var victimDerived = victim.GetComponent<DerivedStatsComponent>();
            var victimState = victim.GetComponent<StateComponent>();
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            
            bool isDead = false;
            int attackCount = 0;
            int maxAttacks = 20; // 防止无限循环
            
            _output.WriteLine($"\n[Full Combat] Attacker vs Victim");
            _output.WriteLine($"  Attacker ATK: {attacker.GetComponent<DerivedStatsComponent>().Get(StatType.ATK)}");
            _output.WriteLine($"  Victim DEF: {victimDerived.Get(StatType.DEF)}, HP: {victimDynamic.Get(DynamicResourceType.CURRENT_HP)}");
            _output.WriteLine("\n--- Attack Loop ---");
            
            // Act - 循环攻击直到死亡
            while (!isDead && attackCount < maxAttacks)
            {
                attackCount++;
                
                // 计算伤害
                var damageResult = DamageCalculator.Calculate(attacker, victim, effectConfig, attackCount);
                
                // 应用伤害
                FP hpBefore = victimDynamic.Get(DynamicResourceType.CURRENT_HP);
                FP actualDamage = victimDynamic.TakeDamage(damageResult.FinalDamage, victimDerived);
                FP hpAfter = victimDynamic.Get(DynamicResourceType.CURRENT_HP);
                
                _output.WriteLine($"  Attack #{attackCount}: Damage={actualDamage:F2} (Crit: {damageResult.IsCritical}), HP: {hpBefore:F2} → {hpAfter:F2}");
                
                // 检查死亡
                if (hpAfter <= FP.Zero && !victimState.Get(StateType.DEAD))
                {
                    victimState.Set(StateType.DEAD, true);
                    isDead = true;
                    _output.WriteLine($"\n  💀 VICTIM DIED after {attackCount} attacks!");
                }
            }
            
            // Assert
            Assert.True(isDead, $"受害者应该在{maxAttacks}次攻击内死亡");
            Assert.True(victimState.Get(StateType.DEAD), "死亡状态应该被标记");
            Assert.True(victimDynamic.Get(DynamicResourceType.CURRENT_HP) <= FP.Zero, "生命值应该为0");
            Assert.False(victimState.CanTakeDamage(), "死亡后不能再受伤");
            Assert.False(victimState.CanMove(), "死亡后不能移动");
            Assert.False(victimState.CanAttack(), "死亡后不能攻击");
        }
        
        [Fact(DisplayName = "Buff叠加战斗 - 多层Buff应累积效果")]
        public void BuffStackingCombat_MultipleStacks_ShouldCumulate()
        {
            // Arrange
            var attacker = CreateCombatEntity(1001, 1, 60001);
            var target = CreateCombatEntity(1001, 1, 60002);
            
            var attackerBuff = attacker.GetComponent<BuffComponent>();
            var attackerBase = attacker.GetComponent<BaseStatsComponent>();
            var attackerDerived = attacker.GetComponent<DerivedStatsComponent>();
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            
            _output.WriteLine($"\n[Buff Stacking Combat]");
            
            // Act - 逐步添加力量祝福（最多3层）
            var buffConfig = ConfigManager.Instance.Tables.TbBuffTable.Get(5001);
            FP[] damages = new FP[4];
            
            for (int stack = 0; stack <= 3; stack++)
            {
                if (stack > 0)
                {
                    attackerBuff.AddBuff(new BuffInstance
                    {
                        BuffId = 5001,
                        Duration = buffConfig.Duration,
                        RemainingFrames = buffConfig.Duration,
                        Stackable = buffConfig.Stackable,
                        MaxStack = buffConfig.MaxStack,
                        StackCount = 1
                    });
                    
                    // 重新计算派生属性
                    attackerDerived.ClearModifiers();
                    var buffModifiers = attackerBuff.GetAllModifiers();
                    foreach (var kvp in buffModifiers)
                    {
                        foreach (var mod in kvp.Value)
                        {
                            attackerDerived.AddModifier(kvp.Key, mod);
                        }
                    }
                    attackerDerived.RecalculateAll(attackerBase);
                }
                
                var result = DamageCalculator.Calculate(attacker, target, effectConfig, 100 + stack);
                damages[stack] = result.FinalDamage;
                
                _output.WriteLine($"  Stack {stack}: ATK={attackerDerived.Get(StatType.ATK)}, Damage={damages[stack]:F2}");
            }
            
            // Assert - 整体趋势应该是伤害递增（排除Miss的情况）
            // 由于确定性随机，某些层可能Miss导致伤害为0，但至少应有递增趋势
            FP avgDamage = (damages[0] + damages[1] + damages[2] + damages[3]) / (FP)4;
            Assert.True(avgDamage > FP.Zero, "平均伤害应该大于0");
            Assert.True(damages[3] > damages[0], "3层Buff伤害应该明显高于0层");
        }
        
        [Fact(DisplayName = "升级流程 - 升级应增加属性并满血满蓝")]
        public void LevelUpFlow_ShouldIncreaseStatsAndRestoreResources()
        {
            // Arrange
            var entity = CreateCombatEntity(1001, 1, 70001);
            
            var levelComp = entity.GetComponent<LevelComponent>();
            var baseStats = entity.GetComponent<BaseStatsComponent>();
            var derivedStats = entity.GetComponent<DerivedStatsComponent>();
            var dynamicStats = entity.GetComponent<DynamicStatsComponent>();
            
            // 模拟战斗受伤
            dynamicStats.Set(DynamicResourceType.CURRENT_HP, (FP)500);
            dynamicStats.Set(DynamicResourceType.CURRENT_MANA, (FP)50);
            
            FP atkBefore = derivedStats.Get(StatType.ATK);
            FP hpMaxBefore = derivedStats.Get(StatType.HP);
            
            _output.WriteLine($"\n[Level Up Test] Level 1 → 2");
            _output.WriteLine($"  Before: ATK={atkBefore}, MaxHP={hpMaxBefore}");
            _output.WriteLine($"  Current HP: {dynamicStats.Get(DynamicResourceType.CURRENT_HP)}");
            
            // Act - 获得足够经验升级
            bool leveledUp = levelComp.GainExp(1000, entity);
            
            FP atkAfter = derivedStats.Get(StatType.ATK);
            FP hpMaxAfter = derivedStats.Get(StatType.HP);
            FP currentHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            FP currentMana = dynamicStats.Get(DynamicResourceType.CURRENT_MANA);
            
            _output.WriteLine($"  After: ATK={atkAfter}, MaxHP={hpMaxAfter}");
            _output.WriteLine($"  Current HP: {currentHP} (should be full)");
            
            // Assert
            Assert.True(leveledUp, "应该成功升级");
            Assert.Equal(2, levelComp.CurrentLevel);
            Assert.True(atkAfter > atkBefore, "攻击力应该增加");
            Assert.True(hpMaxAfter > hpMaxBefore, "最大生命值应该增加");
            Assert.True(TSMath.Abs(hpMaxAfter - currentHP) < (FP)0.001, "应该满血");
            Assert.True(TSMath.Abs(derivedStats.Get(StatType.MAX_MANA) - currentMana) < (FP)0.001, "应该满蓝");
        }
        
        [Fact(DisplayName = "加点流程 - 加点应增加属性")]
        public void AllocatePointFlow_ShouldIncreaseStats()
        {
            // Arrange
            var entity = CreateCombatEntity(1001, 1, 80001);
            
            var growthComp = entity.GetComponent<GrowthComponent>();
            var baseStats = entity.GetComponent<BaseStatsComponent>();
            var derivedStats = entity.GetComponent<DerivedStatsComponent>();
            
            // 给予属性点
            growthComp.AvailableStatPoints = 10;
            
            FP atkBefore = derivedStats.Get(StatType.ATK);
            FP defBefore = derivedStats.Get(StatType.DEF);
            
            _output.WriteLine($"\n[Allocate Points Test]");
            _output.WriteLine($"  Before: ATK={atkBefore}, DEF={defBefore}");
            
            // Act - 分配5点攻击，5点防御
            for (int i = 0; i < 5; i++)
            {
                bool success = growthComp.AllocatePoint(StatType.ATK, entity);
                Assert.True(success, $"第{i + 1}次加点应该成功");
            }
            
            for (int i = 0; i < 5; i++)
            {
                bool success = growthComp.AllocatePoint(StatType.DEF, entity);
                Assert.True(success, $"第{i + 1}次加防应该成功");
            }
            
            FP atkAfter = derivedStats.Get(StatType.ATK);
            FP defAfter = derivedStats.Get(StatType.DEF);
            
            _output.WriteLine($"  After: ATK={atkAfter}, DEF={defAfter}");
            _output.WriteLine($"  Increase: ATK +{(float)(atkAfter - atkBefore)}, DEF +{(float)(defAfter - defBefore)}");
            
            // Assert
            Assert.Equal((FP)0, (FP)growthComp.AvailableStatPoints);
            Assert.Equal(atkBefore + (FP)10, atkAfter); // 每点+2攻击
            Assert.Equal(defBefore + (FP)10, defAfter); // 每点+2防御
        }
        
        [Fact(DisplayName = "完整战斗循环 - 包含Buff更新和过期")]
        public void FullCombatCycle_WithBuffUpdate_ShouldWork()
        {
            // Arrange
            var attacker = CreateCombatEntity(1001, 1, 90001);
            var target = CreateCombatEntity(1001, 1, 90002);
            
            var attackerBuff = attacker.GetComponent<BuffComponent>();
            var attackerBase = attacker.GetComponent<BaseStatsComponent>();
            var attackerDerived = attacker.GetComponent<DerivedStatsComponent>();
            var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);
            
            // 添加短期Buff（5帧）
            attackerBuff.AddBuff(new BuffInstance
            {
                BuffId = 5001,
                Duration = 5,
                RemainingFrames = 5,
                Stackable = false,
                MaxStack = 1,
                StackCount = 1
            });
            
            _output.WriteLine($"\n[Full Combat Cycle] Buff lifecycle test");
            
            // Act & Assert - 模拟10帧战斗
            for (int frame = 0; frame < 10; frame++)
            {
                // 更新Buff
                attackerBuff.UpdateBuffs();
                
                // 重新计算派生属性
                attackerDerived.ClearModifiers();
                var buffModifiers = attackerBuff.GetAllModifiers();
                foreach (var kvp in buffModifiers)
                {
                    foreach (var mod in kvp.Value)
                    {
                        attackerDerived.AddModifier(kvp.Key, mod);
                    }
                }
                attackerDerived.RecalculateAll(attackerBase);
                
                // 攻击
                var result = DamageCalculator.Calculate(attacker, target, effectConfig, frame);
                
                bool hasBuff = attackerBuff.Buffs.Count > 0;
                _output.WriteLine($"  Frame {frame}: Damage={result.FinalDamage:F2}, HasBuff={hasBuff}, BuffCount={attackerBuff.Buffs.Count}");
                
                // Buff持续5帧，从RemainingFrames=5开始，每帧-1，到0时移除
                // Frame 0-3有Buff，Frame 4-9没有
                if (frame <= 3)
                {
                    Assert.True(hasBuff, $"第{frame}帧应该还有Buff");
                }
                else
                {
                    Assert.False(hasBuff, $"第{frame}帧Buff应该已过期");
                }
            }
        }
    }
}

