using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.CommonBase;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 实体属性能力
    /// 
    /// 处理实体基础属性的设置事件（从 Client 线程发送）
    /// 包括：Monster 信息、等级/经验、生命值等
    /// </summary>
    public class EntityAttributeCapability : Capability<EntityAttributeCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 10; // 较高优先级，早于大部分逻辑
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag>();
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            // 注册事件处理器
            RegisterEventHandlers();
        }
        
        protected override void RegisterEventHandlers()
        {
            // Monster 信息事件
            RegisterEventHandler<SetMonsterInfoEvent>(HandleSetMonsterInfo);
            
            // Level/Exp 事件
            RegisterEventHandler<SetLevelEvent>(HandleSetLevel);
            RegisterEventHandler<GainExpEvent>(HandleGainExp);
            
            // Stats 事件
            RegisterEventHandler<SetHealthEvent>(HandleSetHealth);
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 始终激活，允许处理事件
            return true;
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 永不停用
            return false;
        }
        
        public override void Tick(Entity entity)
        {
            // 此 Capability 不需要每帧逻辑，只处理事件
        }
        
        // ====== 事件处理器 ======
        
        private void HandleSetMonsterInfo(Entity entity, SetMonsterInfoEvent evt)
        {
            var monsterInfo = GetComponent<MonsterInfoComponent>(entity);
            if (monsterInfo != null)
            {
                if (evt.MonsterId > 0)
                    monsterInfo.MonsterId = evt.MonsterId;
                if (evt.Level > 0)
                    monsterInfo.Level = evt.Level;
                if (evt.AIType > 0)
                    monsterInfo.AIType = evt.AIType;
                
                entity.MarkComponentDirty(MonsterInfoComponent.ComponentTypeId);
                ASLogger.Instance.Debug($"EntityAttribute: Set monster info (ID:{evt.MonsterId}, Lv:{evt.Level}, AI:{evt.AIType}) for entity {entity.UniqueId}");
            }
        }
        
        private void HandleSetLevel(Entity entity, SetLevelEvent evt)
        {
            var levelComp = GetComponent<LevelComponent>(entity);
            if (levelComp != null)
            {
                levelComp.CurrentLevel = evt.Level;
                entity.MarkComponentDirty(LevelComponent.ComponentTypeId);
                ASLogger.Instance.Debug($"EntityAttribute: Set level to {evt.Level} for entity {entity.UniqueId}");
            }
        }
        
        private void HandleGainExp(Entity entity, GainExpEvent evt)
        {
            var levelComp = GetComponent<LevelComponent>(entity);
            if (levelComp != null)
            {
                bool leveledUp = levelComp.GainExp(evt.Amount, entity);
                entity.MarkComponentDirty(LevelComponent.ComponentTypeId);
                
                if (leveledUp)
                {
                    ASLogger.Instance.Info($"EntityAttribute: Entity {entity.UniqueId} gained {evt.Amount} exp and leveled up to {levelComp.CurrentLevel}");
                }
                else
                {
                    ASLogger.Instance.Debug($"EntityAttribute: Entity {entity.UniqueId} gained {evt.Amount} exp (Current: {levelComp.CurrentExp}/{levelComp.ExpToNextLevel})");
                }
            }
        }
        
        private void HandleSetHealth(Entity entity, SetHealthEvent evt)
        {
            var dynamicStats = GetComponent<DynamicStatsComponent>(entity);
            if (dynamicStats != null)
            {
                dynamicStats.Set(Astrum.LogicCore.Stats.DynamicResourceType.CURRENT_HP, evt.Health);
                entity.MarkComponentDirty(DynamicStatsComponent.ComponentTypeId);
                ASLogger.Instance.Debug($"EntityAttribute: Set health to {evt.Health} for entity {entity.UniqueId}");
            }
        }
    }
}

