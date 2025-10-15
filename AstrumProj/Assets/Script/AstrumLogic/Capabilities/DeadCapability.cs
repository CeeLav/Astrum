using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Stats;
using Astrum.CommonBase;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 死亡能力 - 集中管理实体死亡后的能力停用/恢复
    /// </summary>
    [MemoryPackable]
    public partial class DeadCapability : Capability
    {
        /// <summary>白名单：死亡后仍可执行的能力类型</summary>
        private static readonly HashSet<Type> _whitelistCapabilityTypes = new HashSet<Type>
        {
            typeof(DeadCapability), // 自身必须保持活跃
            // 未来可添加：AnimationCapability、ViewSyncCapability 等
        };
        
        /// <summary>死亡前被停用的能力列表（用于复活时恢复）</summary>
        [MemoryPackIgnore]
        private List<Capability> _disabledCapabilities = new List<Capability>();
        
        /// <summary>是否已处于死亡状态</summary>
        private bool _isDead = false;
        
        public DeadCapability()
        {
            Priority = 1000; // 最高优先级，确保最先执行
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            // 订阅死亡事件
            EventSystem.Instance.Subscribe<EntityDiedEventData>(OnEntityDied);
            
            // 订阅复活事件（预留）
            EventSystem.Instance.Subscribe<EntityRevivedEventData>(OnEntityRevived);
        }
        
        public override void Tick()
        {
            // 死亡能力本身不需要每帧逻辑，仅响应事件
        }
        
        /// <summary>
        /// 处理实体死亡事件
        /// </summary>
        private void OnEntityDied(EntityDiedEventData eventData)
        {
            // 只处理自己的死亡事件
            if (OwnerEntity == null || eventData.EntityId != OwnerEntity.UniqueId)
                return;
            
            if (_isDead)
            {
                ASLogger.Instance.Warning($"[DeadCapability] Entity {OwnerEntity.UniqueId} already dead, ignoring duplicate event");
                return;
            }
            
            _isDead = true;
            ASLogger.Instance.Info($"[DeadCapability] Entity {OwnerEntity.UniqueId} died, disabling non-whitelisted capabilities");
            
            // 停用非白名单能力
            DisableNonWhitelistedCapabilities();
        }
        
        /// <summary>
        /// 处理实体复活事件（预留）
        /// </summary>
        private void OnEntityRevived(EntityRevivedEventData eventData)
        {
            if (OwnerEntity == null || eventData.EntityId != OwnerEntity.UniqueId)
                return;
            
            if (!_isDead)
            {
                ASLogger.Instance.Warning($"[DeadCapability] Entity {OwnerEntity.UniqueId} not dead, ignoring revive event");
                return;
            }
            
            _isDead = false;
            ASLogger.Instance.Info($"[DeadCapability] Entity {OwnerEntity.UniqueId} revived, restoring capabilities");
            
            // 清除死亡状态
            var stateComp = GetOwnerComponent<StateComponent>();
            stateComp?.Set(StateType.DEAD, false);
            
            // 恢复生命值
            var dynamicStats = GetOwnerComponent<DynamicStatsComponent>();
            var derivedStats = GetOwnerComponent<DerivedStatsComponent>();
            if (dynamicStats != null && derivedStats != null)
            {
                FP maxHP = derivedStats.Get(StatType.HP);
                FP reviveHP = maxHP * eventData.ReviveHealthPercent;
                dynamicStats.Set(DynamicResourceType.CURRENT_HP, reviveHP);
            }
            
            // 恢复能力
            RestoreDisabledCapabilities();
        }
        
        /// <summary>
        /// 停用非白名单能力
        /// </summary>
        private void DisableNonWhitelistedCapabilities()
        {
            if (OwnerEntity == null) return;
            
            _disabledCapabilities.Clear();
            
            foreach (var capability in OwnerEntity.Capabilities)
            {
                if (capability == this) continue; // 跳过自己
                
                // 检查是否在白名单
                if (_whitelistCapabilityTypes.Contains(capability.GetType()))
                    continue;
                
                // 停用能力
                if (capability.IsActive)
                {
                    capability.OnDeactivate();
                    _disabledCapabilities.Add(capability);
                    ASLogger.Instance.Debug($"[DeadCapability] Disabled capability: {capability.Name}");
                }
            }
        }
        
        /// <summary>
        /// 恢复被停用的能力
        /// </summary>
        private void RestoreDisabledCapabilities()
        {
            foreach (var capability in _disabledCapabilities)
            {
                capability.OnActivate();
                ASLogger.Instance.Debug($"[DeadCapability] Restored capability: {capability.Name}");
            }
            
            _disabledCapabilities.Clear();
        }
        
        /// <summary>
        /// 清理时取消订阅
        /// </summary>
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            EventSystem.Instance.Unsubscribe<EntityDiedEventData>(OnEntityDied);
            EventSystem.Instance.Unsubscribe<EntityRevivedEventData>(OnEntityRevived);
        }
        
        /// <summary>
        /// 添加白名单能力类型（静态方法，供外部配置）
        /// </summary>
        public static void AddWhitelistCapability(Type capabilityType)
        {
            if (capabilityType.IsSubclassOf(typeof(Capability)))
            {
                _whitelistCapabilityTypes.Add(capabilityType);
            }
        }
    }
}

