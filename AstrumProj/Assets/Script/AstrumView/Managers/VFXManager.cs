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
        // 活跃的特效实例管理
        // Key: InstanceId (来自 VFXTriggerEventData)
        // Value: VFXInstanceInfo (特效实例信息)
        private Dictionary<int, VFXInstanceInfo> _activeVFX = new Dictionary<int, VFXInstanceInfo>();
        
        // 特效实例ID计数器（用于生成唯一ID）
        private int _nextInstanceId = 1;
        
        // 是否已订阅事件
        private bool _isSubscribed = false;
        
        /// <summary>
        /// 特效实例信息
        /// </summary>
        private class VFXInstanceInfo
        {
            public GameObject Instance { get; set; }
            public float StartTime { get; set; }
            public float Duration { get; set; }
            public bool Loop { get; set; }
        }
        
        public Stage CurrentStage
        {
            get;
            set;
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            // 订阅VFX触发事件
            if (!_isSubscribed)
            {
                EventSystem.Instance.Subscribe<VFXTriggerEventData>(OnVFXTriggered);
                _isSubscribed = true;
                ASLogger.Instance.Info("VFXManager initialized");
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (_isSubscribed)
            {
                EventSystem.Instance.Unsubscribe<VFXTriggerEventData>(OnVFXTriggered);
                _isSubscribed = false;
            }
            
            // 清理所有活跃特效
            ClearAll();
            
            ASLogger.Instance.Info("VFXManager cleaned up");
        }
        
        /// <summary>
        /// VFX触发事件处理
        /// </summary>
        private void OnVFXTriggered(VFXTriggerEventData eventData)
        {
            // 通过ViewManager获取当前Stage
            var currentStage = CurrentStage;//GameDirector.Instance.CurrentGameMode.MainStage;
            if (currentStage == null)
            {
                ASLogger.Instance.Warning("VFXManager: No current Stage available, cannot play VFX");
                return;
            }
            
            // 通过EntityId获取EntityView
            EntityView entityView = null;
            if (currentStage.EntityViews != null && currentStage.EntityViews.TryGetValue(eventData.EntityId, out entityView))
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
            GameObject vfxInstance = UnityEngine.Object.Instantiate(vfxPrefab);
            vfxInstance.name = $"VFX_{data.ResourcePath}_{_nextInstanceId}";
            
            // 生成唯一实例ID（如果未指定）
            if (data.InstanceId == 0)
            {
                data.InstanceId = _nextInstanceId++;
            }
            
            // 设置位置和旋转
            Vector3 position = target.Transform.position;
            Quaternion rotation = target.Transform.rotation;

            var positionOffsetTS = data.PositionOffset;
            var rotationOffsetTS = data.Rotation;

            Vector3 positionOffset = Vector3.zero;
            if (positionOffsetTS != TrueSync.TSVector.zero)
            {
                positionOffset = new Vector3((float)positionOffsetTS.x, (float)positionOffsetTS.y, (float)positionOffsetTS.z);
                position += rotation * positionOffset;
            }

            Vector3 rotationOffset = Vector3.zero;
            if (rotationOffsetTS != TrueSync.TSVector.zero)
            {
                rotationOffset = new Vector3((float)rotationOffsetTS.x, (float)rotationOffsetTS.y, (float)rotationOffsetTS.z);
                rotation *= Quaternion.Euler(rotationOffset);
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
                vfxInstance.transform.localPosition = positionOffset;
                vfxInstance.transform.localRotation = Quaternion.Euler(rotationOffset);
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
            
            // 考虑播放速度调整持续时间
            if (maxDuration > 0 && data.PlaybackSpeed > 0)
            {
                maxDuration /= data.PlaybackSpeed;
            }
            
            // 添加到活跃列表
            _activeVFX[data.InstanceId] = new VFXInstanceInfo
            {
                Instance = vfxInstance,
                StartTime = Time.time,
                Duration = maxDuration,
                Loop = data.Loop
            };
            
            ASLogger.Instance.Debug($"VFXManager: Playing VFX {data.ResourcePath} for Entity {target.EntityId}, InstanceId={data.InstanceId}");
        }
        
        /// <summary>
        /// 停止并清理特效
        /// </summary>
        /// <param name="instanceId">特效实例ID</param>
        public void StopVFX(int instanceId)
        {
            if (_activeVFX.TryGetValue(instanceId, out var vfxInfo))
            {
                if (vfxInfo.Instance != null)
                {
                    // 停止所有粒子系统
                    var particleSystems = vfxInfo.Instance.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (var ps in particleSystems)
                    {
                        if (ps != null)
                        {
                            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        }
                    }
                    
                    // 销毁GameObject
                    UnityEngine.Object.Destroy(vfxInfo.Instance);
                }
                
                // 从活跃列表移除
                _activeVFX.Remove(instanceId);
                
                //ASLogger.Instance.Debug($"VFXManager: Stopped VFX InstanceId={instanceId}");
            }
        }
        
        /// <summary>
        /// 清理所有特效
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _activeVFX)
            {
                if (kvp.Value != null && kvp.Value.Instance != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value.Instance);
                }
            }
            
            _activeVFX.Clear();
            ASLogger.Instance.Info("VFXManager: Cleared all VFX");
        }
        
        /// <summary>
        /// 更新特效（检查并清理已结束的特效）
        /// </summary>
        public void Update()
        {
            if (_activeVFX.Count == 0)
                return;
            
            float currentTime = Time.time;
            List<int> toRemove = null;
            
            // 检查每个特效是否已结束
            foreach (var kvp in _activeVFX)
            {
                var vfxInfo = kvp.Value;
                
                // 跳过循环特效
                if (vfxInfo.Loop)
                    continue;
                
                // 检查特效是否已过期
                if (vfxInfo.Duration > 0 && currentTime - vfxInfo.StartTime >= vfxInfo.Duration)
                {
                    // 标记为需要销毁
                    if (toRemove == null)
                        toRemove = new List<int>();
                    toRemove.Add(kvp.Key);
                }
                // 如果特效实例已被销毁，也需要清理
                else if (vfxInfo.Instance == null)
                {
                    if (toRemove == null)
                        toRemove = new List<int>();
                    toRemove.Add(kvp.Key);
                }
            }
            
            // 销毁已结束的特效
            if (toRemove != null)
            {
                foreach (var instanceId in toRemove)
                {
                    StopVFX(instanceId);
                }
            }
        }
    }
}

