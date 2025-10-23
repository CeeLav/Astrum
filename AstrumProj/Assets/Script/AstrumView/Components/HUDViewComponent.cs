using UnityEngine;
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
        
        protected override void OnInitialize()
        {
            ASLogger.Instance.Debug($"HUDViewComponent 初始化，实体ID: {OwnerEntity?.UniqueId}");
            RegisterOrUpdateHUD();
            
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isRegistered || OwnerEntity == null)
                return;
            OnSyncData(null);
            // 持续更新HUD位置，确保镜头移动时HUD跟随
            Vector3 worldPosition = GetEntityWorldPosition();
            if (worldPosition != Vector3.zero)
            {
                // 更新HUD位置
                HUDManager.Instance?.UpdateHUDPosition(OwnerEntity.UniqueId, worldPosition);
            }
        }
        
        protected override void OnSyncData(object data)
        {
            if (OwnerEntity == null)
            {
                ASLogger.Instance.Warning("HUDViewComponent: OwnerEntity 为空");
                return;
            }
            
            // 获取血量数据
            var dynamicStats = OwnerEntity.GetComponent<DynamicStatsComponent>();
            var derivedStats = OwnerEntity.GetComponent<DerivedStatsComponent>();
            
            ASLogger.Instance.Debug($"HUDViewComponent: 获取组件 - DynamicStats: {dynamicStats != null}, DerivedStats: {derivedStats != null}");
            
            if (dynamicStats != null && derivedStats != null)
            {
                FP newCurrentHealth = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
                FP newMaxHealth = derivedStats.Get(StatType.HP);
                FP newCurrentShield = dynamicStats.Get(DynamicResourceType.SHIELD);
                
                ASLogger.Instance.Debug($"HUDViewComponent: 血量数据 - 当前: {newCurrentHealth}, 最大: {newMaxHealth}, 护盾: {newCurrentShield}");
                
                // 检查数据是否有变化
                if (newCurrentHealth != _currentHealth || newMaxHealth != _maxHealth || newCurrentShield != _currentShield)
                {
                    _currentHealth = newCurrentHealth;
                    _maxHealth = newMaxHealth;
                    _currentShield = newCurrentShield;
                    
                    ASLogger.Instance.Debug($"HUDViewComponent: 血量数据变化，注册/更新HUD");
                    
                    // 注册或更新HUD
                    RegisterOrUpdateHUD();
                }
            }
            else
            {
                ASLogger.Instance.Warning($"HUDViewComponent: 缺少必要组件 - DynamicStats: {dynamicStats != null}, DerivedStats: {derivedStats != null}");
            }
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
            ASLogger.Instance.Debug($"HUDViewComponent: 世界位置 - {worldPosition}");
            
            if (!_isRegistered)
            {
                // 注册新的HUD
                ASLogger.Instance.Debug($"HUDViewComponent: 注册HUD，实体ID: {OwnerEntity.UniqueId}");
                HUDManager.Instance.RegisterHUD(OwnerEntity.UniqueId, OwnerEntity, worldPosition);
                _isRegistered = true;
            }
            else
            {
                ASLogger.Instance.Debug($"HUDViewComponent: 更新HUD数据，实体ID: {OwnerEntity.UniqueId}");
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