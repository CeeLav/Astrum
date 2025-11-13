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
    /// 通过监测血量判定死亡，集中管理实体死亡后的能力停用/恢复
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
            
            state.CustomData[KEY_DISABLED_TYPE_IDS] = new List<int>();
            SetCapabilityState(entity, state);
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            if (!base.ShouldActivate(entity))
                return false;
            
            // 抛射物：使用标记位判定
            var projectileComp = GetComponent<ProjectileComponent>(entity);
            if (projectileComp != null)
            {
                return projectileComp.IsMarkedForDestroy;
            }

            // 检查血量和状态判定死亡（角色/怪物等）
            var dynamicStats = GetComponent<DynamicStatsComponent>(entity);
            var stateComp = GetComponent<StateComponent>(entity);
            
            // 必须有血量组件且血量 <= 0 才激活
            if (dynamicStats == null)
                return false;
            
            FP currentHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            
            // 血量 <= 0 视为死亡
            return currentHP <= FP.Zero;
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            if (base.ShouldDeactivate(entity))
                return true;
            
            var projectileComp = GetComponent<ProjectileComponent>(entity);
            if (projectileComp != null)
            {
                // 抛射物在被标记移除前不取消激活
                return !projectileComp.IsMarkedForDestroy;
            }

            // 检查是否复活（血量恢复）
            var dynamicStats = GetComponent<DynamicStatsComponent>(entity);
            if (dynamicStats == null)
                return true; // 没有血量组件，停用
            
            FP currentHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            
            // 血量 > 0 视为复活
            return currentHP > FP.Zero;
        }
        
        public override void OnActivate(Entity entity)
        {
            base.OnActivate(entity);
            
            var projectileComp = GetComponent<ProjectileComponent>(entity);
            if (projectileComp != null)
            {
                if (!projectileComp.IsMarkedForDestroy)
                    return;

                //ASLogger.Instance.Info($"[DeadCapability] Projectile {entity.UniqueId} marked dead, queue destroy");
                entity.World?.QueueDestroyEntity(entity.UniqueId);
                return;
            }

            //ASLogger.Instance.Info($"[DeadCapability] Entity {entity.UniqueId} died (HP <= 0), disabling non-whitelisted capabilities");
            
            // 设置死亡状态
            var stateComp = GetComponent<StateComponent>(entity);
            stateComp?.Set(StateType.DEAD, true);
            
            // 停用非白名单能力
            //DisableNonWhitelistedCapabilities(entity);
        }
        
        public override void OnDeactivate(Entity entity)
        {
            ASLogger.Instance.Info($"[DeadCapability] Entity {entity.UniqueId} revived (HP > 0), restoring capabilities");
            
            // 清除死亡状态
            var stateComp = GetComponent<StateComponent>(entity);
            stateComp?.Set(StateType.DEAD, false);
            
            // 恢复能力
            //RestoreDisabledCapabilities(entity);
            
            base.OnDeactivate(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            // 每帧检查血量变化，触发激活/停用逻辑
            // ShouldActivate/ShouldDeactivate 会被 CapabilitySystem 自动调用
        }
        
        // ====== 辅助方法 ======
        /*
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
        }*/
        /*
        /// <summary>
        /// 恢复被停用的能力
        /// 通过移除 Tag 禁用来恢复（新系统）
        /// 同时恢复旧系统的 Capability（旧系统会自动恢复，因为只调用了 OnDeactivate）
        /// </summary>
        /// 
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
        }*/
        
        /// <summary>
        /// 添加白名单 Capability TypeId（静态方法，供外部配置）
        /// </summary>
        public static void AddWhitelistTypeId(int typeId)
        {
            _whitelistTypeIds.Add(typeId);
        }
    }
}
