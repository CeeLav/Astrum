using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using cfg;

namespace Astrum.LogicCore.Archetypes
{
    /// <summary>
    /// Monster原型 = BaseUnit + Combat + AI差异
    /// 继承BaseUnit（基础移动）和Combat（战斗系统），不继承Controllable（不需要玩家控制）
    /// </summary>
    [Archetype(EArchetype.Monster, "BaseUnit", "Combat","Controllable")]
    public class MonsterArchetype : Archetype
    {
        private static readonly Type[] _comps = 
        {
            typeof(MonsterInfoComponent),
            // 未来可以添加AI相关组件
            // typeof(AIComponent),
        };
        
        private static readonly Type[] _caps =
        {
            // 未来可以添加AI相关能力
            // typeof(AICapability),
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;

        // ==================== 生命周期钩子 ====================
        
        /// <summary>
        /// 组件挂载前的钩子
        /// </summary>
        public new static void OnBeforeComponentsAttach(Entity entity, World world)
        {
            // 调用父类方法
            Archetype.OnBeforeComponentsAttach(entity, world);
            
            ASLogger.Instance.Debug($"Monster Archetype: 实体 {entity.UniqueId} 准备挂载组件");
        }

        /// <summary>
        /// 组件挂载后的钩子
        /// </summary>
        public new static void OnAfterComponentsAttach(Entity entity, World world)
        {
            // 调用父类方法（Combat会初始化数值系统）
            CombatArchetype.OnAfterComponentsAttach(entity, world);
            
            // Monster特定的初始化
            var monsterInfo = entity.GetComponent<MonsterInfoComponent>();
            if (monsterInfo != null)
            {
                ASLogger.Instance.Debug($"Monster Archetype: 怪物 {entity.UniqueId} 配置ID={monsterInfo.MonsterId}, 等级={monsterInfo.Level}");
                
                // 可以在这里根据怪物配置初始化属性
                // 例如：根据等级调整基础属性
                var levelComp = entity.GetComponent<LevelComponent>();
                if (levelComp != null)
                {
                    levelComp.CurrentLevel = monsterInfo.Level;
                }
            }
            
            ASLogger.Instance.Debug($"Monster Archetype: 实体 {entity.UniqueId} 组件挂载完成");
        }

        /// <summary>
        /// 能力挂载前的钩子
        /// </summary>
        public new static void OnBeforeCapabilitiesAttach(Entity entity, World world)
        {
            Archetype.OnBeforeCapabilitiesAttach(entity, world);
            
            ASLogger.Instance.Debug($"Monster Archetype: 实体 {entity.UniqueId} 准备挂载能力");
        }

        /// <summary>
        /// 能力挂载后的钩子
        /// </summary>
        public new static void OnAfterCapabilitiesAttach(Entity entity, World world)
        {
            Archetype.OnAfterCapabilitiesAttach(entity, world);
            
            ASLogger.Instance.Debug($"Monster Archetype: 实体 {entity.UniqueId} 创建完成");
        }

        /// <summary>
        /// 实体销毁时的钩子
        /// </summary>
        public new static void OnEntityDestroy(Entity entity, World world)
        {
            Archetype.OnEntityDestroy(entity, world);
            
            ASLogger.Instance.Debug($"Monster Archetype: 实体 {entity.UniqueId} 准备销毁");
        }
    }
}


