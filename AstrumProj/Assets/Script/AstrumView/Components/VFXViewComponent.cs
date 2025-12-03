using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.CommonBase.Events;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.View.Core;
using Astrum.View.Managers;
using TrueSync;

namespace Astrum.View.Components
{
    /// <summary>
    /// VFX 视图组件
    /// 管理实体上的视觉特效播放
    /// 通过 ViewEvent 机制接收逻辑层的特效触发请求
    /// </summary>
    public class VFXViewComponent : ViewComponent
    {
        // 当前播放的特效实例ID列表（VFXManager 使用 instanceId 管理）
        private List<int> _activeVFXInstanceIds = new List<int>();
        
        // 实例ID计数器
        private int _nextInstanceId = 1;
        
        // ====== 静态注册（类型级，只执行一次）======
        static VFXViewComponent()
        {
            // 注册此组件监听 VFXTriggerEvent
            ViewComponentEventRegistry.Instance.RegisterEventHandler(
                typeof(VFXTriggerEvent), 
                typeof(VFXViewComponent));
            
            ASLogger.Instance.Debug("VFXViewComponent: 静态注册 VFXTriggerEvent 事件处理器");
        }
        
        // ====== 实例注册（实例级，第一次初始化时执行）======
        protected override void RegisterViewEventHandlers()
        {
            // 对象池优化：只在第一次初始化时调用
            RegisterViewEventHandler<VFXTriggerEvent>(OnVFXTrigger);
            
            ASLogger.Instance.Debug($"VFXViewComponent: 实例注册 VFXTriggerEvent 事件处理器，EntityId: {OwnerEntity?.UniqueId}");
        }
        
        // ====== 生命周期方法 ======
        
        protected override void OnInitialize()
        {
            ASLogger.Instance.Debug($"VFXViewComponent: 初始化，EntityId: {OwnerEntity?.UniqueId}");
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            // 清理已完成的特效实例
            CleanupFinishedVFX();
        }
        
        protected override void OnDestroy()
        {
            // 清理所有特效实例
            CleanupAllVFX();
            
            ASLogger.Instance.Debug($"VFXViewComponent: 销毁，EntityId: {OwnerEntity?.UniqueId}");
        }
        
        protected override void OnSyncData(object data)
        {
            // VFX 组件不需要同步数据，完全由事件驱动
        }
        
        // ====== 事件处理器 ======
        
        /// <summary>
        /// 处理 VFX 触发事件
        /// </summary>
        private void OnVFXTrigger(VFXTriggerEvent evt)
        {
            if (string.IsNullOrEmpty(evt.ResourcePath))
            {
                ASLogger.Instance.Warning($"VFXViewComponent: 特效资源路径为空，EntityId: {OwnerEntity?.UniqueId}");
                return;
            }
            
            ASLogger.Instance.Debug($"VFXViewComponent: 收到 VFX 触发事件，Path: {evt.ResourcePath}, EntityId: {OwnerEntity?.UniqueId}");
            
            // 播放特效
            PlayVFX(evt);
        }
        
        // ====== VFX 播放逻辑 ======
        
        /// <summary>
        /// 播放特效（通过 VFXManager）
        /// </summary>
        private void PlayVFX(VFXTriggerEvent evt)
        {
            try
            {
                // 检查 VFXManager 是否可用
                if (VFXManager.Instance == null)
                {
                    ASLogger.Instance.Warning($"VFXViewComponent: VFXManager 未初始化，无法播放特效，EntityId: {OwnerEntity?.UniqueId}");
                    return;
                }
                
                // 构造 VFXTriggerEventData（VFXManager 使用的数据格式）
                var instanceId = _nextInstanceId++;
                var eventData = new VFXTriggerEventData
                {
                    InstanceId = instanceId,
                    EntityId = OwnerEntity.UniqueId,
                    ResourcePath = evt.ResourcePath,
                    PositionOffset = evt.PositionOffset,
                    Rotation = evt.Rotation,
                    Scale = evt.Scale,
                    PlaybackSpeed = evt.PlaybackSpeed,
                    FollowCharacter = evt.FollowCharacter,
                    Loop = evt.Loop
                };
                
                // 通过 VFXManager 播放特效
                VFXManager.Instance.PlayVFX(OwnerEntityView, eventData);
                
                // 添加到活跃列表（用于清理）
                _activeVFXInstanceIds.Add(instanceId);
                
                ASLogger.Instance.Info($"VFXViewComponent: 成功播放特效（通过 VFXManager），Path: {evt.ResourcePath}, InstanceId: {instanceId}, Loop: {evt.Loop}, EntityId: {OwnerEntity?.UniqueId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"VFXViewComponent: 播放特效时发生异常，Path: {evt.ResourcePath}, EntityId: {OwnerEntity?.UniqueId}, Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理已完成的特效实例
        /// VFXManager 会自动管理特效生命周期，这里不需要做什么
        /// </summary>
        private void CleanupFinishedVFX()
        {
            // VFXManager 自动清理非循环特效，这里不需要额外处理
        }
        
        /// <summary>
        /// 清理所有特效实例（通过 VFXManager）
        /// </summary>
        private void CleanupAllVFX()
        {
            if (VFXManager.Instance != null)
            {
                // 通过 VFXManager 停止所有特效
                foreach (var instanceId in _activeVFXInstanceIds)
                {
                    VFXManager.Instance.StopVFX(instanceId);
                }
            }
            
            _activeVFXInstanceIds.Clear();
        }
        
        /// <summary>
        /// 停止所有循环特效（通过 VFXManager）
        /// </summary>
        public void StopAllLoopingVFX()
        {
            if (VFXManager.Instance != null)
            {
                // 通过 VFXManager 停止所有特效
                foreach (var instanceId in _activeVFXInstanceIds)
                {
                    VFXManager.Instance.StopVFX(instanceId);
                }
            }
        }
    }
}

