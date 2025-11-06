using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Systems;
using Astrum.CommonBase;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 死亡能力（新架构，基于 Capability&lt;T&gt;）
    /// 集中管理实体死亡后的能力停用/恢复
    /// </summary>
    public class DeadCapability : Capability<DeadCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 1000; // 最高优先级，确保最先执行
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.Combat 
        };
        
        // ====== 常量配置 ======
        private const string KEY_IS_DEAD = "IsDead";
        private const string KEY_DISABLED_TYPE_IDS = "DisabledTypeIds";
        
        /// <summary>
        /// 白名单：死亡后仍可执行的 Capability TypeId 集合
        /// </summary>
        private static readonly HashSet<int> _whitelistTypeIds = new HashSet<int>
        {
            // DeadCapability 自身必须保持活跃
            TypeId
        };
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            
            // 初始化自定义数据
            var state = GetCapabilityState(entity);
            if (state.CustomData == null)
                state.CustomData = new Dictionary<string, object>();
            
            state.CustomData[KEY_IS_DEAD] = false;
            state.CustomData[KEY_DISABLED_TYPE_IDS] = new List<int>();
            SetCapabilityState(entity, state);
            
            // 为这个 Entity 创建事件订阅器（使用闭包绑定 Entity）
            // 注意：由于事件系统是全局的，我们需要为每个 Entity 创建独立的订阅器
            // 这里使用闭包来绑定 Entity 引用
            var diedHandler = new Action<EntityDiedEventData>(evt => OnEntityDied(entity, evt));
            var revivedHandler = new Action<EntityRevivedEventData>(evt => OnEntityRevived(entity, evt));
            
            // 将处理器存储在 CustomData 中，以便在 OnDetached 时取消订阅
            state.CustomData["_diedHandler"] = diedHandler;
            state.CustomData["_revivedHandler"] = revivedHandler;
            SetCapabilityState(entity, state);
            
            // 订阅死亡事件
            EventSystem.Instance.Subscribe(diedHandler);
            
            // 订阅复活事件
            EventSystem.Instance.Subscribe(revivedHandler);
        }
        
        public override void OnDetached(Entity entity)
        {
            // 取消订阅事件
            var state = GetCapabilityState(entity);
            if (state.CustomData != null)
            {
                if (state.CustomData.TryGetValue("_diedHandler", out var diedHandler) && diedHandler is Action<EntityDiedEventData> dh)
                {
                    EventSystem.Instance.Unsubscribe(dh);
                }
                
                if (state.CustomData.TryGetValue("_revivedHandler", out var revivedHandler) && revivedHandler is Action<EntityRevivedEventData> rh)
                {
                    EventSystem.Instance.Unsubscribe(rh);
                }
            }
            
            base.OnDetached(entity);
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 始终激活（死亡能力本身应该一直可用）
            return base.ShouldActivate(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            // 死亡能力本身不需要每帧逻辑，仅响应事件
        }
        
        // ====== 事件处理 ======
        
        /// <summary>
        /// 处理实体死亡事件（带 Entity 参数，通过闭包绑定）
        /// </summary>
        private void OnEntityDied(Entity entity, EntityDiedEventData eventData)
        {
            if (entity == null || entity.UniqueId != eventData.EntityId) return;
            
            var state = GetCapabilityState(entity);
            var isDead = (bool)(state.CustomData?.TryGetValue(KEY_IS_DEAD, out var deadValue) == true ? deadValue : false);
            
            if (isDead)
            {
                ASLogger.Instance.Warning($"[DeadCapability] Entity {entity.UniqueId} already dead, ignoring duplicate event");
                return;
            }
            
            // 标记为死亡
            if (state.CustomData == null)
                state.CustomData = new Dictionary<string, object>();
            
            state.CustomData[KEY_IS_DEAD] = true;
            
            ASLogger.Instance.Info($"[DeadCapability] Entity {entity.UniqueId} died, disabling non-whitelisted capabilities");
            
            // 停用非白名单能力
            DisableNonWhitelistedCapabilities(entity);
            
            SetCapabilityState(entity, state);
        }
        
        /// <summary>
        /// 处理实体复活事件（带 Entity 参数，通过闭包绑定）
        /// </summary>
        private void OnEntityRevived(Entity entity, EntityRevivedEventData eventData)
        {
            if (entity == null || entity.UniqueId != eventData.EntityId) return;
            
            var state = GetCapabilityState(entity);
            var isDead = (bool)(state.CustomData?.TryGetValue(KEY_IS_DEAD, out var deadValue) == true ? deadValue : false);
            
            if (!isDead)
            {
                ASLogger.Instance.Warning($"[DeadCapability] Entity {entity.UniqueId} not dead, ignoring revive event");
                return;
            }
            
            // 标记为未死亡
            if (state.CustomData == null)
                state.CustomData = new Dictionary<string, object>();
            
            state.CustomData[KEY_IS_DEAD] = false;
            
            ASLogger.Instance.Info($"[DeadCapability] Entity {entity.UniqueId} revived, restoring capabilities");
            
            // 清除死亡状态
            var stateComp = GetComponent<StateComponent>(entity);
            stateComp?.Set(StateType.DEAD, false);
            
            // 恢复生命值
            var dynamicStats = GetComponent<DynamicStatsComponent>(entity);
            var derivedStats = GetComponent<DerivedStatsComponent>(entity);
            if (dynamicStats != null && derivedStats != null)
            {
                FP maxHP = derivedStats.Get(StatType.HP);
                FP reviveHP = maxHP * eventData.ReviveHealthPercent;
                dynamicStats.Set(DynamicResourceType.CURRENT_HP, reviveHP);
            }
            
            // 恢复能力
            RestoreDisabledCapabilities(entity);
            
            SetCapabilityState(entity, state);
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 停用非白名单能力
        /// 使用 Tag 系统批量禁用（Movement、Control、Attack、Skill 等）
        /// 同时处理新旧两套系统
        /// </summary>
        private void DisableNonWhitelistedCapabilities(Entity entity)
        {
            if (entity?.World == null) return;
            
            var disabledTypeIds = new List<int>();
            
            // 处理新系统：遍历实体上所有新架构的 Capability
            foreach (var kvp in entity.CapabilityStates)
            {
                var typeId = kvp.Key;
                var capabilityState = kvp.Value;
                
                // 跳过白名单
                if (_whitelistTypeIds.Contains(typeId))
                    continue;
                
                // 跳过已经停用的
                if (!capabilityState.IsActive)
                    continue;
                
                // 获取 Capability 实例
                var capability = CapabilitySystem.GetCapability(typeId);
                if (capability == null)
                    continue;
                
                // 使用 Tag 系统禁用（通过禁用所有相关 Tag）
                foreach (var tag in capability.Tags)
                {
                    entity.World.CapabilitySystem.DisableCapabilitiesByTag(entity, tag, entity.UniqueId);
                }
                
                disabledTypeIds.Add(typeId);
                ASLogger.Instance.Debug($"[DeadCapability] Disabled new capability TypeId: {typeId}");
            }
            
            // 保存被禁用的 TypeId 列表（仅新系统）
            var state = GetCapabilityState(entity);
            if (state.CustomData == null)
                state.CustomData = new Dictionary<string, object>();
            
            state.CustomData[KEY_DISABLED_TYPE_IDS] = disabledTypeIds;
            SetCapabilityState(entity, state);
        }
        
        /// <summary>
        /// 恢复被停用的能力
        /// 通过移除 Tag 禁用来恢复（新系统）
        /// 同时恢复旧系统的 Capability（旧系统会自动恢复，因为只调用了 OnDeactivate）
        /// </summary>
        private void RestoreDisabledCapabilities(Entity entity)
        {
            if (entity?.World == null) return;
            
            var state = GetCapabilityState(entity);
            var disabledTypeIds = state.CustomData?.TryGetValue(KEY_DISABLED_TYPE_IDS, out var idsValue) == true 
                ? idsValue as List<int> 
                : null;
            
            // 恢复新系统的 Capability（通过移除 Tag 禁用）
            if (disabledTypeIds != null && disabledTypeIds.Count > 0)
            {
                foreach (var typeId in disabledTypeIds)
                {
                    var capability = CapabilitySystem.GetCapability(typeId);
                    if (capability == null)
                        continue;
                    
                    // 通过移除 Tag 禁用来恢复
                    foreach (var tag in capability.Tags)
                    {
                        entity.World.CapabilitySystem.EnableCapabilitiesByTag(entity, tag, entity.UniqueId);
                    }
                    
                    ASLogger.Instance.Debug($"[DeadCapability] Restored new capability TypeId: {typeId}");
                }
            }
            
            // 清空被禁用的列表
            if (state.CustomData == null)
                state.CustomData = new Dictionary<string, object>();
            
            state.CustomData[KEY_DISABLED_TYPE_IDS] = new List<int>();
            SetCapabilityState(entity, state);
        }
        
        /// <summary>
        /// 添加白名单 Capability TypeId（静态方法，供外部配置）
        /// </summary>
        public static void AddWhitelistTypeId(int typeId)
        {
            _whitelistTypeIds.Add(typeId);
        }
    }
}
