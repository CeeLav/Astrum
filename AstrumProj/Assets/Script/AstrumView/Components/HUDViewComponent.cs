using UnityEngine;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Stats;
using Astrum.View.Managers;
using TrueSync;

namespace Astrum.View.Components
{
    /// <summary>
    /// HUD视图组件 - 使用HUDManager管理HUD显示
    /// </summary>
    public class HUDViewComponent : ViewComponent
    {
        // HUD显示配置
        private Vector3 healthBarOffset = new Vector3(0, 2.5f, 0);
        private bool _isRegistered = false;
        
        // 当前数据
        private FP _currentHealth = FP.Zero;
        private FP _maxHealth = FP.Zero;
        private FP _currentShield = FP.Zero;
        
        // 监听的组件类型 ID
        private int _dynamicStatsComponentId;
        private int _derivedStatsComponentId;
        
        // 是否已初始化数据
        private bool _hasInitializedData = false;
        
        protected override void OnInitialize()
        {
            // 获取需要监听的组件 ComponentTypeId（通过 ViewRead 检查组件是否存在）
            if (OwnerEntity != null && OwnerEntity.World != null)
            {
                if (DynamicStatsComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var _))
                {
                    _dynamicStatsComponentId = DynamicStatsComponent.ComponentTypeId;
                }
                
                if (DerivedStatsComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var _))
                {
                    _derivedStatsComponentId = DerivedStatsComponent.ComponentTypeId;
                }
                
                // 初始化时主动读取一次数据
                SyncDataFromComponent(0);
            }
            
            // ASLogger.Instance.Debug($"HUDViewComponent 初始化，实体ID: {OwnerEntity?.UniqueId}");
            RegisterOrUpdateHUD();
        }
        
        public override int[] GetWatchedComponentIds()
        {
            var ids = new List<int>();
            if (_dynamicStatsComponentId != 0)
            {
                ids.Add(_dynamicStatsComponentId);
            }
            if (_derivedStatsComponentId != 0)
            {
                ids.Add(_derivedStatsComponentId);
            }
            return ids.Count > 0 ? ids.ToArray() : null;
        }
        
        public override void SyncDataFromComponent(int componentTypeId)
        {
            if (OwnerEntity == null || OwnerEntity.World == null)
            {
                ASLogger.Instance.Warning($"HUDViewComponent.SyncDataFromComponent: OwnerEntity or World is null");
                return;
            }
            
            // 通过 ViewRead 获取血量数据
            if (!DynamicStatsComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var dynamicRead))
            {
                ASLogger.Instance.Warning($"HUDViewComponent.SyncDataFromComponent: Failed to get DynamicStatsComponent ViewRead for entity {OwnerEntity.UniqueId}");
                return;
            }
            
            if (!dynamicRead.IsValid)
            {
                ASLogger.Instance.Warning($"HUDViewComponent.SyncDataFromComponent: DynamicStatsComponent ViewRead is invalid for entity {OwnerEntity.UniqueId}");
                return;
            }
            
            if (!DerivedStatsComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var derivedRead))
            {
                ASLogger.Instance.Warning($"HUDViewComponent.SyncDataFromComponent: Failed to get DerivedStatsComponent ViewRead for entity {OwnerEntity.UniqueId}");
                return;
            }
            
            if (!derivedRead.IsValid)
            {
                ASLogger.Instance.Warning($"HUDViewComponent.SyncDataFromComponent: DerivedStatsComponent ViewRead is invalid for entity {OwnerEntity.UniqueId}");
                return;
            }
            
            FP newCurrentHealth = dynamicRead.CurrentHP;
            FP newMaxHealth = derivedRead.MaxHP;
            FP newCurrentShield = dynamicRead.Shield;
            
            ASLogger.Instance.Debug($"HUDViewComponent.SyncDataFromComponent: Entity {OwnerEntity.UniqueId} - CurrentHP: {newCurrentHealth}, MaxHP: {newMaxHealth}, Shield: {newCurrentShield}");
            
            // 检查数据是否有变化
            if (newCurrentHealth != _currentHealth || newMaxHealth != _maxHealth || newCurrentShield != _currentShield)
            {
                _currentHealth = newCurrentHealth;
                _maxHealth = newMaxHealth;
                _currentShield = newCurrentShield;
                _hasInitializedData = true;
                
                ASLogger.Instance.Info($"HUDViewComponent: Updated health for entity {OwnerEntity.UniqueId} - {_currentHealth}/{_maxHealth}, Shield: {_currentShield}");
                
                // 注册或更新HUD
                RegisterOrUpdateHUD();
            }
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            // 如果还没有初始化数据，尝试读取一次（确保在 Logic 帧末导出后能获取到数据）
            if (!_hasInitializedData)
            {
                SyncDataFromComponent(0);
            }
            
            if (!_isRegistered || OwnerEntity == null)
                return;
            
            // 持续更新HUD位置，确保镜头移动时HUD跟随
            Vector3 worldPosition = GetEntityWorldPosition();
            HUDManager.Instance?.UpdateHUDPosition(OwnerEntity.UniqueId, worldPosition);
        }
        
        protected override void OnSyncData(object data)
        {
            // 此方法现在由 SyncDataFromComponent 调用，不再需要实现
        }
        
        protected override void OnDestroy()
        {
            if (_isRegistered && OwnerEntity != null && HUDManager.Instance != null)
            {
                HUDManager.Instance.UnregisterHUD(OwnerEntity.UniqueId);
                _isRegistered = false;
            }
        }
        
        private void RegisterOrUpdateHUD()
        {
            if (OwnerEntity == null)
            {
                ASLogger.Instance.Warning("HUDViewComponent: OwnerEntity 为空，无法注册HUD");
                return;
            }
            
            if (HUDManager.Instance == null)
            {
                ASLogger.Instance.Warning("HUDViewComponent: HUDManager.Instance 为空，无法注册HUD");
                return;
            }
            
            Vector3 worldPosition = GetEntityWorldPosition();
            // ASLogger.Instance.Debug($"HUDViewComponent: 世界位置 - {worldPosition}");
            
            if (!_isRegistered)
            {
                // 注册新的HUD
                // ASLogger.Instance.Debug($"HUDViewComponent: 注册HUD，实体ID: {OwnerEntity.UniqueId}");
                HUDManager.Instance.RegisterHUD(OwnerEntity.UniqueId, OwnerEntity, worldPosition);
                _isRegistered = true;
            }
            else
            {
                // ASLogger.Instance.Debug($"HUDViewComponent: 更新HUD数据，实体ID: {OwnerEntity.UniqueId}");
            }
            
            // 更新HUD数据
            HUDManager.Instance.UpdateHUDData(OwnerEntity.UniqueId, _currentHealth, _maxHealth, _currentShield);
        }
        
        private Vector3 GetEntityWorldPosition()
        {
            if (OwnerEntityView == null)
            {
                ASLogger.Instance.Warning("HUDViewComponent: OwnerEntity 为空，无法获取世界位置");
                return Vector3.zero;
            }

            return OwnerEntityView.GetWorldPosition();
        }
    }
}