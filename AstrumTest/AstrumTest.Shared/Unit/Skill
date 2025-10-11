using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using TrueSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.SkillSystem.EffectHandlers;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Factories;
using AstrumTest.Shared.Fixtures;

namespace AstrumTest
{
    /// <summary>
    /// 技能效果系统集成测试
    /// 测试从配置创建角色并进行真实战斗场景的技能效果流程
    /// </summary>
    [Collection("Config Collection")]
    [Trait("Category", "Integration")]
    [Trait("Module", "Skill")]
    [Trait("Priority", "High")]
    public class SkillEffectIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ConfigFixture _configFixture;
        private readonly World _testWorld;

        public SkillEffectIntegrationTests(ITestOutputHelper output, ConfigFixture configFixture)
        {
            _output = output;
            _configFixture = configFixture;
            
            _output.WriteLine($"[SkillEffectIntegrationTests] Config path: {_configFixture.ConfigPath}");
            
            // 初始化 ArchetypeManager（必须在创建实体之前）
            Astrum.LogicCore.Archetypes.ArchetypeManager.Instance.Initialize();
            _output.WriteLine("✅ ArchetypeManager initialized");
            
            // 初始化 SkillEffectManager 并注册 Handler
            InitializeSkillEffectManager();
            
            // 创建测试世界
            _testWorld = new World();
            _testWorld.Initialize(0);
            
            _output.WriteLine("✅ Test environment initialized");
        }

        private void InitializeSkillEffectManager()
        {
            var effectManager = SkillEffectManager.Instance;
            effectManager.ClearQueue();
            
            // 注册伤害处理器
            effectManager.RegisterHandler(1, new DamageEffectHandler());
            
            // 注册治疗处理器
            effectManager.RegisterHandler(2, new HealEffectHandler());
            
            _output.WriteLine("✅ SkillEffectManager initialized with handlers");
        }

        /// <summary>
        /// 创建角色实体（使用 EntityFactory）
        /// </summary>
        private Entity CreateRoleEntity(int roleId, TSVector position)
        {
            // 使用 EntityFactory 从配置创建实体
            var entity = EntityFactory.Instance.CreateEntity(roleId, _testWorld);
            if (entity == null)
            {
                throw new Exception($"Failed to create entity with config ID {roleId}");
            }
            
            // 设置位置
            var posComp = entity.GetComponent<TransComponent>();
            if (posComp != null)
            {
                posComp.Position = position;
            }
            
            // 添加血量组件（如果没有的话）
            var healthComp = entity.GetComponent<HealthComponent>();
            if (healthComp == null)
            {
                healthComp = new HealthComponent();
                entity.AddComponent(healthComp);
            }
            
            // 从角色配置表读取基础属性
            var roleConfig = ConfigManager.Instance.Tables.TbRoleBaseTable.Get(roleId);
            if (roleConfig != null)
            {
                healthComp.MaxHealth = (int)roleConfig.BaseHealth;
                healthComp.CurrentHealth = (int)roleConfig.BaseHealth;
                
                _output.WriteLine($"Role {roleId}: HP={roleConfig.BaseHealth}, ATK={roleConfig.BaseAttack}, DEF={roleConfig.BaseDefense}");
            }
            
            // 注册到物理世界（使用单例）
            HitManager.Instance.RegisterEntity(entity);
            
            return entity;
        }

        /// <summary>
        /// 测试 1: 基础系统验证 - 配置加载
        /// </summary>
        [Fact]
        public void Test_ConfigurationLoading()
        {
            _output.WriteLine("=== Test: Configuration Loading ===");
            
            // 验证配置管理器
            var configManager = ConfigManager.Instance;
            Assert.NotNull(configManager);
            Assert.NotNull(configManager.Tables);
            
            // 验证角色配置
            var roleTable = configManager.Tables.TbRoleBaseTable;
            Assert.NotNull(roleTable);
            var knight = roleTable.Get(1001);
            Assert.NotNull(knight);
            _output.WriteLine($"Knight config: ID={knight.Id}, HP={knight.BaseHealth}, ATK={knight.BaseAttack}");
            
            // 验证技能配置
            var skillTable = configManager.Tables.TbSkillTable;
            Assert.NotNull(skillTable);
            var normalAttack = skillTable.Get(2001);
            Assert.NotNull(normalAttack);
            _output.WriteLine($"Normal Attack: ID={normalAttack.Id}");
            
            // 验证技能效果配置
            var effectTable = configManager.Tables.TbSkillEffectTable;
            Assert.NotNull(effectTable);
            _output.WriteLine($"Loaded {effectTable.DataList.Count} skill effects");
            
            _output.WriteLine("✅ All configurations loaded successfully");
        }

        /// <summary>
        /// 测试 2: 真实战斗场景 - 两个骑士互相普通攻击
        /// 使用 EntityFactory 从配置创建角色，模拟真实的战斗流程
        /// </summary>
        [Fact]
        public void Test_RealCombatScenario_TwoKnightsBasicAttack()
        {
            _output.WriteLine("=== Test: Real Combat - Two Knights Basic Attack ===");
            
            // 创建骑士A（攻击者）位于原点
            var knightA = CreateRoleEntity(1001, TSVector.zero);
            _output.WriteLine($"Knight A created: ID={knightA.UniqueId}, Position={knightA.GetComponent<TransComponent>().Position}");
            
            // 创建骑士B（目标）位于前方1.2单位（在攻击范围内，攻击盒偏移1+半径1=2单位范围）
            var knightB = CreateRoleEntity(1001, new TSVector((FP)1.2, FP.Zero, FP.Zero));
            _output.WriteLine($"Knight B created: ID={knightB.UniqueId}, Position={knightB.GetComponent<TransComponent>().Position}");
            
            // 获取初始状态
            var knightA_HP = knightA.GetComponent<HealthComponent>().CurrentHealth;
            var knightB_HP = knightB.GetComponent<HealthComponent>().CurrentHealth;
            _output.WriteLine($"Initial HP: Knight A={knightA_HP}, Knight B={knightB_HP}");
            
            // === 第一次攻击：骑士A攻击骑士B ===
            _output.WriteLine("\n--- Round 1: Knight A attacks Knight B ---");
            
            // 骑士A准备释放普通攻击（ActionId 3001）
            var skillActionA = SkillConfigManager.Instance.CreateSkillActionInstance(3001, 1);
            Assert.NotNull(skillActionA);
            Assert.NotEmpty(skillActionA.TriggerEffects);
            _output.WriteLine($"Knight A skill action loaded: {skillActionA.Id}, TriggerFrames={skillActionA.TriggerEffects.Count}");
            
            // 设置动作
            var actionCompA = knightA.GetComponent<ActionComponent>();
            actionCompA.CurrentAction = skillActionA;
            actionCompA.CurrentFrame = 0;
            
            // 检查 SkillExecutorCapability 是否存在
            var executorA = knightA.Capabilities.Find(c => c is SkillExecutorCapability) as SkillExecutorCapability;
            if (executorA == null)
            {
                _output.WriteLine("⚠️  Knight A 没有 SkillExecutorCapability!");
                foreach (var cap in knightA.Capabilities)
                {
                    _output.WriteLine($"  - {cap.GetType().Name}");
                }
            }
            else
            {
                _output.WriteLine($"✅ Knight A has SkillExecutorCapability");
            }
            
            // 测试碰撞检测
            _output.WriteLine("\n--- Testing Collision Detection ---");
            var trigger = skillActionA.TriggerEffects[0];
            _output.WriteLine($"Trigger: Frame={trigger.Frame}, Type={trigger.TriggerType}, EffectId={trigger.EffectId}");
            _output.WriteLine($"CollisionShape: {(trigger.CollisionShape.HasValue ? trigger.CollisionShape.Value.ShapeType.ToString() : "null")}");
            
            if (trigger.CollisionShape.HasValue)
            {
                var testShape = trigger.CollisionShape.Value;
                var testFilter = new CollisionFilter
                {
                    ExcludedEntityIds = new System.Collections.Generic.HashSet<long> { knightA.UniqueId },
                    OnlyEnemies = false  // 禁用阵营过滤来测试
                };
                
                var testHits = HitManager.Instance.QueryHits(knightA, testShape, testFilter, 0);
                _output.WriteLine($"Manual collision test found {testHits.Count} targets");
                foreach (var hit in testHits)
                {
                    _output.WriteLine($"  - Hit entity {hit.UniqueId} at {hit.GetComponent<TransComponent>()?.Position}");
                }
            }
            
            // 模拟技能执行：逐帧更新直到触发帧（Frame 10）
            for (int frame = 0; frame <= 15; frame++)
            {
                actionCompA.CurrentFrame = frame;
                
                // SkillExecutorCapability 处理触发帧
                if (executorA != null)
                {
                    executorA.Tick();
                    if (frame == 10)
                    {
                        _output.WriteLine($"Frame {frame}: Executed SkillExecutorCapability.Tick()");
                        _output.WriteLine($"Effect queue count: {SkillEffectManager.Instance.QueuedEffectCount}");
                    }
                }
                
                // SkillEffectManager 处理效果队列
                int beforeCount = SkillEffectManager.Instance.QueuedEffectCount;
                SkillEffectManager.Instance.Update();
                int afterCount = SkillEffectManager.Instance.QueuedEffectCount;
                if (beforeCount > 0)
                {
                    _output.WriteLine($"Frame {frame}: Processed {beforeCount - afterCount} effects");
                }
            }
            
            // 验证骑士B受到伤害
            var knightB_HP_After = knightB.GetComponent<HealthComponent>().CurrentHealth;
            int damageDealt = knightB_HP - knightB_HP_After;
            _output.WriteLine($"Knight B HP after attack: {knightB_HP_After} (damage: {damageDealt})");
            
            // 验证攻击成功
            Assert.True(damageDealt > 0, $"Knight B should take damage, but HP remained {knightB_HP}");
            Assert.InRange(damageDealt, 1, 200); // 伤害应该在合理范围内
            
            // 最终总结
            _output.WriteLine($"\n=== Combat Test Summary ===");
            _output.WriteLine($"✅ Knight A successfully attacked Knight B");
            _output.WriteLine($"✅ Damage: {damageDealt} HP (HP: {knightB_HP} → {knightB_HP_After})");
            _output.WriteLine($"\n✅ Complete skill flow verified:");
            _output.WriteLine($"  1. EntityFactory created roles from config ✓");
            _output.WriteLine($"  2. SkillExecutorCapability triggered at frame 10 ✓");
            _output.WriteLine($"  3. HitManager detected collision ✓");
            _output.WriteLine($"  4. SkillEffectManager queued & processed effect ✓");
            _output.WriteLine($"  5. DamageCalculator calculated damage ✓");
            _output.WriteLine($"  6. DamageEffectHandler applied damage to target ✓");
        }

        /// <summary>
        /// 测试 3: DamageCalculator 基础计算
        /// </summary>
        [Fact]
        public void Test_DamageCalculator_BasicCalculation()
        {
            _output.WriteLine("=== Test: DamageCalculator Basic Calculation ===");
            
            // 创建简单的测试实体
            var caster = new Entity();
            var casterHealth = new HealthComponent { MaxHealth = 100, CurrentHealth = 100 };
            caster.AddComponent(casterHealth);
            
            var target = new Entity();
            var targetHealth = new HealthComponent { MaxHealth = 100, CurrentHealth = 100 };
            target.AddComponent(targetHealth);
            
            // 从配置表获取效果数据
            var effectConfig = SkillConfigManager.Instance.GetSkillEffect(4022);
            if (effectConfig == null)
            {
                _output.WriteLine("⚠️  SkillEffect 4022 not found in config, skipping test");
                return;
            }
            
            _output.WriteLine($"Effect: ID={effectConfig.SkillEffectId}, Type={effectConfig.EffectType}, Value={effectConfig.EffectValue}");

            // Act
            var result = DamageCalculator.Calculate(caster, target, effectConfig);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FinalDamage >= 0, "Damage should be non-negative");
            _output.WriteLine($"✅ Damage calculated: {result.FinalDamage:F2}, Critical: {result.IsCritical}");
        }

        /// <summary>
        /// 测试 4: SkillEffectManager 队列处理
        /// </summary>
        [Fact]
        public void Test_SkillEffectManager_QueueProcessing()
        {
            _output.WriteLine("=== Test: SkillEffectManager Queue Processing ===");
            
            // 创建简单的测试实体
            var caster = new Entity();
            var casterHealth = new HealthComponent { MaxHealth = 100, CurrentHealth = 100 };
            caster.AddComponent(casterHealth);
            _testWorld.Entities[caster.UniqueId] = caster;
            
            var target = new Entity();
            var targetHealth = new HealthComponent { MaxHealth = 100, CurrentHealth = 100 };
            target.AddComponent(targetHealth);
            _testWorld.Entities[target.UniqueId] = target;
            
            int initialHP = targetHealth.CurrentHealth;
            _output.WriteLine($"Initial target HP: {initialHP}");

            // 清空队列
            var effectManager = SkillEffectManager.Instance;
            effectManager.ClearQueue();
            
            // 队列3次伤害效果
            for (int i = 0; i < 3; i++)
            {
                effectManager.QueueSkillEffect(new SkillEffectData
                {
                    CasterEntity = caster,
                    TargetEntity = target,
                    EffectId = 4022
                });
            }
            
            _output.WriteLine($"Queued 3 effects, queue count: {effectManager.QueuedEffectCount}");
            
            // 处理队列
            effectManager.Update();

            // Assert
            int finalHP = targetHealth.CurrentHealth;
            int totalDamage = initialHP - finalHP;
            _output.WriteLine($"Final HP: {finalHP}, Total damage: {totalDamage}");
            
            Assert.Equal(0, effectManager.QueuedEffectCount);
            Assert.True(totalDamage > 0, "Total damage should be greater than 0");
            _output.WriteLine($"✅ Effect queue processed: -{totalDamage} HP");
        }

        /// <summary>
        /// 测试 5: 从配置表加载技能效果数据
        /// </summary>
        [Fact]
        public void Test_LoadSkillEffectsFromConfig()
        {
            _output.WriteLine("=== Test: Load Skill Effects From Config ===");
            
            var configManager = ConfigManager.Instance;
            var skillEffectTable = configManager.Tables.TbSkillEffectTable;
            
            _output.WriteLine($"SkillEffectTable loaded with {skillEffectTable.DataList.Count} entries");
            
            // 统计各类型效果
            int damageEffects = 0;
            int healEffects = 0;
            int knockbackEffects = 0;
            
            foreach (var effect in skillEffectTable.DataList)
            {
                if (effect.EffectType == 1) damageEffects++;
                else if (effect.EffectType == 2) healEffects++;
                else if (effect.EffectType == 3) knockbackEffects++;
            }
            
            _output.WriteLine($"Damage effects: {damageEffects}");
            _output.WriteLine($"Heal effects: {healEffects}");
            _output.WriteLine($"Knockback effects: {knockbackEffects}");
            
            Assert.True(skillEffectTable.DataList.Count > 0, "Should have loaded skill effects");
            Assert.True(damageEffects > 0, "Should have at least one damage effect");
            _output.WriteLine("✅ Skill effects loaded successfully");
        }

        public void Dispose()
        {
            // 清理资源
            SkillEffectManager.Instance?.ClearQueue();
            _output.WriteLine("✅ Test cleanup completed");
        }
    }
}
