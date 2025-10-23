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
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isRegistered || OwnerEntity == null)
                return;
            
            // 获取实体的世界位置
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
                return;
            
            // 获取血量数据
            var dynamicStats = OwnerEntity.GetComponent<DynamicStatsComponent>();
            var derivedStats = OwnerEntity.GetComponent<DerivedStatsComponent>();
            
            if (dynamicStats != null && derivedStats != null)
            {
                FP newCurrentHealth = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
                FP newMaxHealth = derivedStats.Get(StatType.HP);
                FP newCurrentShield = dynamicStats.Get(DynamicResourceType.SHIELD);
                
                // 检查数据是否有变化
                if (newCurrentHealth != _currentHealth || newMaxHealth != _maxHealth || newCurrentShield != _currentShield)
                {
                    _currentHealth = newCurrentHealth;
                    _maxHealth = newMaxHealth;
                    _currentShield = newCurrentShield;
                    
                    // 注册或更新HUD
                    RegisterOrUpdateHUD();
                }
            }
        }
        
        protected override void OnDestroy()
        {
            if (_isRegistered && OwnerEntity != null && HUDManager.Instance != null)
            {
                HUDManager.Instance.UnregisterHUD(OwnerEntity.UniqueId);
                _isRegistered = false;
                ASLogger.Instance.Debug($"HUDViewComponent 注销HUD，实体ID: {OwnerEntity.UniqueId}");
            }
        }
        
        private void RegisterOrUpdateHUD()
        {
            if (OwnerEntity == null || HUDManager.Instance == null)
                return;
            
            Vector3 worldPosition = GetEntityWorldPosition();
            if (worldPosition == Vector3.zero)
                return;
            
            if (!_isRegistered)
            {
                // 注册新的HUD
                HUDManager.Instance.RegisterHUD(OwnerEntity.UniqueId, OwnerEntity, worldPosition);
                _isRegistered = true;
                ASLogger.Instance.Debug($"HUDViewComponent 注册HUD，实体ID: {OwnerEntity.UniqueId}");
            }
            
            // 更新HUD数据
            HUDManager.Instance.UpdateHUDData(OwnerEntity.UniqueId, _currentHealth, _maxHealth, _currentShield);
        }
        
        private Vector3 GetEntityWorldPosition()
        {
            if (OwnerEntity == null)
                return Vector3.zero;
            
            // 暂时返回一个默认位置，实际项目中需要根据具体的组件结构来获取
            // TODO: 实现从TransViewComponent或其他组件获取世界位置
            return Vector3.zero;
        }
    }
}