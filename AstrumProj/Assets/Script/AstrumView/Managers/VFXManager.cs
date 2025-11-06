using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.CommonBase.Events;
using Astrum.View.Core;

namespace Astrum.View.Managers
{
    /// <summary>
    /// VFX管理器 - 管理运行时的特效播放（View层）
    /// </summary>
    public class VFXManager : Singleton<VFXManager>
    {
        // 当前Stage引用
        private Stage _currentStage;
        
        // 活跃的特效实例管理
        // Key: InstanceId (来自 VFXTriggerEventData)
        // Value: GameObject (特效实例)
        private Dictionary<int, GameObject> _activeVFX = new Dictionary<int, GameObject>();
        
        // 特效实例ID计数器（用于生成唯一ID）
        private int _nextInstanceId = 1;
        
        /// <summary>
        /// 初始化VFX管理器
        /// </summary>
        /// <param name="stage">当前Stage实例</param>
        public void Initialize(Stage stage)
        {
            _currentStage = stage;
            
            // 订阅VFX触发事件
            EventSystem.Instance.Subscribe<VFXTriggerEventData>(OnVFXTriggered);
            
            ASLogger.Instance.Info("VFXManager initialized");
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            // 取消订阅事件
            EventSystem.Instance.Unsubscribe<VFXTriggerEventData>(OnVFXTriggered);
            
            // 清理所有活跃特效
            ClearAll();
            
            _currentStage = null;
            
            ASLogger.Instance.Info("VFXManager cleaned up");
        }
        
        /// <summary>
        /// VFX触发事件处理
        /// </summary>
        private void OnVFXTriggered(VFXTriggerEventData eventData)
        {
            if (_currentStage == null)
            {
                ASLogger.Instance.Warning("VFXManager: Stage not initialized, cannot play VFX");
                return;
            }
            
            // 通过EntityId获取EntityView
            EntityView entityView = null;
            if (_currentStage.EntityViews != null && _currentStage.EntityViews.TryGetValue(eventData.EntityId, out entityView))
            {
                PlayVFX(entityView, eventData);
            }
            else
            {
                ASLogger.Instance.Warning($"VFXManager: EntityView not found for EntityId {eventData.EntityId}");
            }
        }
        
        /// <summary>
        /// 播放特效
        /// </summary>
        /// <param name="target">目标EntityView</param>
        /// <param name="data">特效数据</param>
        public void PlayVFX(EntityView target, VFXTriggerEventData data)
        {
            if (target == null || target.GameObject == null)
            {
                ASLogger.Instance.Warning("VFXManager: Target EntityView or GameObject is null");
                return;
            }
            
            if (string.IsNullOrEmpty(data.ResourcePath))
            {
                ASLogger.Instance.Warning("VFXManager: ResourcePath is empty");
                return;
            }
            
            // 加载特效资源
            GameObject vfxPrefab = ResourceManager.Instance.LoadResource<GameObject>(data.ResourcePath);
            if (vfxPrefab == null)
            {
                ASLogger.Instance.Warning($"VFXManager: Failed to load VFX resource: {data.ResourcePath}");
                return;
            }
            
            // 实例化特效
            GameObject vfxInstance = Object.Instantiate(vfxPrefab);
            vfxInstance.name = $"VFX_{data.ResourcePath}_{_nextInstanceId}";
            
            // 生成唯一实例ID（如果未指定）
            if (data.InstanceId == 0)
            {
                data.InstanceId = _nextInstanceId++;
            }
            
            // 设置位置和旋转
            Vector3 position = target.Transform.position;
            Quaternion rotation = target.Transform.rotation;
            
            // 应用位置偏移
            if (data.PositionOffset != Vector3.zero)
            {
                position += rotation * data.PositionOffset;
            }
            
            // 应用旋转
            if (data.Rotation != Vector3.zero)
            {
                rotation *= Quaternion.Euler(data.Rotation);
            }
            
            vfxInstance.transform.position = position;
            vfxInstance.transform.rotation = rotation;
            
            // 应用缩放
            if (data.Scale != 1.0f)
            {
                vfxInstance.transform.localScale = Vector3.one * data.Scale;
            }
            
            // 如果跟随角色，设置为子对象
            if (data.FollowCharacter)
            {
                vfxInstance.transform.SetParent(target.Transform);
                // 重新设置本地位置和旋转（相对于父对象）
                vfxInstance.transform.localPosition = data.PositionOffset;
                vfxInstance.transform.localRotation = Quaternion.Euler(data.Rotation);
            }
            
            // 设置粒子系统参数
            var particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                if (ps == null)
                    continue;
                
                var main = ps.main;
                main.simulationSpeed = data.PlaybackSpeed;
                main.loop = data.Loop;
            }
            
            // 播放特效
            vfxInstance.SetActive(true);
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Play(true); // 包含子粒子系统
                }
            }
            
            // 添加到活跃列表
            _activeVFX[data.InstanceId] = vfxInstance;
            
            // 如果不循环，设置自动清理
            if (!data.Loop)
            {
                // 计算特效持续时间（使用最长的粒子系统）
                float maxDuration = 0f;
                foreach (var ps in particleSystems)
                {
                    if (ps != null)
                    {
                        float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                        if (duration > maxDuration)
                            maxDuration = duration;
                    }
                }
                
                // 延迟销毁（如果持续时间大于0）
                if (maxDuration > 0)
                {
                    // 使用协程延迟销毁
                    MonoBehaviour coroutineRunner = _currentStage?.StageRoot?.GetComponent<MonoBehaviour>();
                    if (coroutineRunner == null && _currentStage?.StageRoot != null)
                    {
                        // 如果没有MonoBehaviour，创建一个临时组件用于协程
                        coroutineRunner = _currentStage.StageRoot.AddComponent<CoroutineRunner>();
                    }
                    
                    if (coroutineRunner != null)
                    {
                        coroutineRunner.StartCoroutine(DestroyVFXAfterDelay(data.InstanceId, maxDuration / data.PlaybackSpeed));
                    }
                }
            }
            
            ASLogger.Instance.Debug($"VFXManager: Playing VFX {data.ResourcePath} for Entity {target.EntityId}, InstanceId={data.InstanceId}");
        }
        
        /// <summary>
        /// 延迟销毁特效（协程）
        /// </summary>
        private System.Collections.IEnumerator DestroyVFXAfterDelay(int instanceId, float delay)
        {
            yield return new WaitForSeconds(delay);
            StopVFX(instanceId);
        }
        
        /// <summary>
        /// 停止并清理特效
        /// </summary>
        /// <param name="instanceId">特效实例ID</param>
        public void StopVFX(int instanceId)
        {
            if (_activeVFX.TryGetValue(instanceId, out var vfxInstance))
            {
                // 停止所有粒子系统
                var particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
                
                // 销毁GameObject
                Object.Destroy(vfxInstance);
                
                // 从活跃列表移除
                _activeVFX.Remove(instanceId);
                
                ASLogger.Instance.Debug($"VFXManager: Stopped VFX InstanceId={instanceId}");
            }
        }
        
        /// <summary>
        /// 清理所有特效
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _activeVFX)
            {
                if (kvp.Value != null)
                {
                    Object.Destroy(kvp.Value);
                }
            }
            
            _activeVFX.Clear();
            ASLogger.Instance.Info("VFXManager: Cleared all VFX");
        }
        
        /// <summary>
        /// 更新特效位置（用于跟随角色的特效）
        /// </summary>
        public void Update()
        {
            // 对于跟随角色的特效，位置会自动更新（因为它们是子对象）
            // 这里可以添加其他更新逻辑，如检查特效是否已结束等
        }
        
        /// <summary>
        /// 临时协程运行器（用于在StageRoot上运行协程）
        /// </summary>
        private class CoroutineRunner : MonoBehaviour
        {
            // 空类，仅用于运行协程
        }
    }
}

