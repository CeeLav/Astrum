using System;
using Xunit;
using Xunit.Abstractions;
using TrueSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.SkillSystem.EffectHandlers;
using Astrum.LogicCore.Factories;
using AstrumTest.Shared.Fixtures;

namespace AstrumTest.Shared.Fixtures
{
    /// <summary>
    /// 共享测试场景 - 提供完整的游戏逻辑环境
    /// 用于集成测试，初始化所有必要的系统
    /// </summary>
    public class SharedTestScenario : IDisposable
    {
        public World World { get; private set; }
        public HitManager HitManager { get; private set; }
        public SkillEffectManager SkillEffectManager { get; private set; }
        public EntityFactory EntityFactory { get; private set; }
        public SkillConfigManager SkillConfigManager { get; private set; }
        public ConfigManager ConfigManager { get; private set; }
        public ITestOutputHelper Output { get; }
        
        public SharedTestScenario(ITestOutputHelper output, ConfigFixture configFixture)
        {
            Output = output;
            ConfigManager = Astrum.LogicCore.Managers.ConfigManager.Instance;
            
            // 初始化所有系统
            InitializeSystems();
            
            Output.WriteLine("✅ Shared test scenario initialized");
        }
        
        private void InitializeSystems()
        {
            // 初始化原型管理器
            Astrum.LogicCore.Archetypes.ArchetypeManager.Instance.Initialize();
            
            // 初始化世界
            World = new World { WorldId = 1, Name = "TestWorld" };
            World.Initialize(0);
            
            // 初始化物理
            HitManager = Astrum.LogicCore.Physics.HitManager.Instance;
            
            // 初始化技能系统
            SkillEffectManager = Astrum.LogicCore.Managers.SkillEffectManager.Instance;
            SkillEffectManager.ClearQueue();
            RegisterSkillHandlers();
            
            SkillConfigManager = Astrum.LogicCore.Managers.SkillConfigManager.Instance;
            
            // 初始化实体工厂
            EntityFactory = Astrum.LogicCore.Factories.EntityFactory.Instance;
            // EntityFactory 不需要 Initialize
        }
        
        private void RegisterSkillHandlers()
        {
            // 注册伤害处理器
            SkillEffectManager.RegisterHandler(1, new DamageEffectHandler());
            
            // 注册治疗处理器
            SkillEffectManager.RegisterHandler(2, new HealEffectHandler());
            
            Output.WriteLine("✅ Skill effect handlers registered");
        }
        
        /// <summary>
        /// 模拟游戏帧更新
        /// </summary>
        public void SimulateFrames(int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                World.Update();
                SkillEffectManager.Update();
            }
        }
        
        public void Dispose()
        {
            SkillEffectManager?.ClearQueue();
            // World 没有 Dispose 方法，不需要调用
            Output.WriteLine("✅ Test scenario cleanup completed");
        }
    }
    
    /// <summary>
    /// 共享测试场景集合定义
    /// </summary>
    [CollectionDefinition("Shared Test Collection")]
    public class SharedTestCollection : ICollectionFixture<ConfigFixture>, ICollectionFixture<SharedTestScenario>
    {
        // 这个类仅用于声明集合，无需实现任何内容
    }
}

