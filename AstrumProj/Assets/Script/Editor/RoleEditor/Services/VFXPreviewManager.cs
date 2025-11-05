using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 特效预览管理器 - 管理编辑器中的特效实例
    /// </summary>
    public class VFXPreviewManager
    {
        // === 活跃的特效实例 ===
        private Dictionary<string, GameObject> _activeVFX = new Dictionary<string, GameObject>();
        
        // === 父对象（预览模型） ===
        private GameObject _parentObject;
        
        // === 预览渲染工具 ===
        private PreviewRenderUtility _previewRenderUtility;
        
        // === 当前帧和事件列表 ===
        private int _currentFrame = -1;
        private bool _isPlaying = false;
        private List<TimelineEvent> _vfxEvents = new List<TimelineEvent>();
        
        protected string LogPrefix => "[VFXPreviewManager]";
        
        /// <summary>
        /// 设置父对象（预览模型）
        /// </summary>
        public void SetParent(GameObject parent)
        {
            _parentObject = parent;
        }
        
        /// <summary>
        /// 设置预览渲染工具（用于添加特效到预览场景）
        /// </summary>
        public void SetPreviewRenderUtility(PreviewRenderUtility previewRenderUtility)
        {
            _previewRenderUtility = previewRenderUtility;
        }
        
        /// <summary>
        /// 设置 VFX 事件列表
        /// </summary>
        public void SetVFXEvents(List<TimelineEvent> events)
        {
            _vfxEvents = events?.FindAll(evt => evt.TrackType == "VFX") ?? new List<TimelineEvent>();
        }
        
        /// <summary>
        /// 更新当前帧，检测并触发/停止特效
        /// </summary>
        /// <param name="frame">当前帧</param>
        /// <param name="isPlaying">是否正在播放动画</param>
        public void UpdateFrame(int frame, bool isPlaying = true)
        {
            bool frameChanged = _currentFrame != frame;
            bool playStateChanged = _isPlaying != isPlaying;
            
            if (!frameChanged && !playStateChanged && isPlaying)
                return; // 同一帧、同一播放状态且在播放，不需要重复更新
            
            _currentFrame = frame;
            _isPlaying = isPlaying;
            
            // 清理已结束的特效
            CleanupFinishedVFX(frame);
            
            // 检测并触发新的特效
            TriggerVFXAtFrame(frame);
            
            // 如果不在播放状态，需要让特效模拟到对应时间
            if (!isPlaying)
            {
                UpdateVFXToFrame(frame);
            }
        }
        
        /// <summary>
        /// 清理已结束的特效
        /// </summary>
        private void CleanupFinishedVFX(int currentFrame)
        {
            var toRemove = new List<string>();
            
            foreach (var kvp in _activeVFX)
            {
                string eventId = kvp.Key;
                GameObject vfxInstance = kvp.Value;
                
                // 查找对应的事件
                var evt = _vfxEvents.Find(e => e.EventId == eventId);
                if (evt == null)
                {
                    // 事件不存在，清理
                    toRemove.Add(eventId);
                    continue;
                }
                
                // 检查是否已结束
                if (currentFrame > evt.EndFrame)
                {
                    // 特效已结束，清理
                    toRemove.Add(eventId);
                }
            }
            
            // 执行清理
            foreach (var eventId in toRemove)
            {
                if (_activeVFX.TryGetValue(eventId, out var vfxInstance))
                {
                    Object.DestroyImmediate(vfxInstance);
                    _activeVFX.Remove(eventId);
                }
            }
        }
        
        /// <summary>
        /// 检测并触发当前帧的特效
        /// </summary>
        private void TriggerVFXAtFrame(int frame)
        {
            if (_parentObject == null)
                return;
            
            foreach (var evt in _vfxEvents)
            {
                // 检查是否在当前帧范围内
                if (frame >= evt.StartFrame && frame <= evt.EndFrame)
                {
                    // 检查是否已创建
                    if (!_activeVFX.ContainsKey(evt.EventId))
                    {
                        // 创建特效
                        PlayVFX(evt);
                    }
                }
            }
        }
        
        /// <summary>
        /// 播放特效
        /// </summary>
        private void PlayVFX(TimelineEvent evt)
        {
            var vfxData = evt.GetEventData<Timeline.EventData.VFXEventData>();
            if (vfxData == null || string.IsNullOrEmpty(vfxData.ResourcePath))
            {
                Debug.LogWarning($"{LogPrefix} 特效事件数据为空或资源路径为空: {evt.EventId}");
                return;
            }
            
            // 加载特效 Prefab
            GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxData.ResourcePath);
            if (vfxPrefab == null)
            {
                Debug.LogWarning($"{LogPrefix} 无法加载特效资源: {vfxData.ResourcePath}");
                return;
            }
            
            // 实例化特效
            GameObject vfxInstance = Object.Instantiate(vfxPrefab);
            vfxInstance.hideFlags = HideFlags.HideAndDontSave;
            
            // 设置位置和旋转
            Vector3 position = _parentObject.transform.position;
            Quaternion rotation = _parentObject.transform.rotation;
            
            // 应用位置偏移
            if (vfxData.PositionOffset != Vector3.zero)
            {
                position += rotation * vfxData.PositionOffset;
            }
            else
            {
                // 如果没有位置偏移，使用父对象的位置
                position = _parentObject.transform.position;
            }
            
            // 应用旋转
            if (vfxData.Rotation != Vector3.zero)
            {
                rotation *= Quaternion.Euler(vfxData.Rotation);
            }
            
            vfxInstance.transform.position = position;
            vfxInstance.transform.rotation = rotation;
            
            // 应用缩放
            if (vfxData.Scale != 1.0f)
            {
                vfxInstance.transform.localScale = Vector3.one * vfxData.Scale;
            }
            
            // 如果跟随角色，设置为子对象
            if (vfxData.FollowCharacter)
            {
                vfxInstance.transform.SetParent(_parentObject.transform);
            }
            
            // 无论是否跟随角色，都需要添加到 PreviewRenderUtility 才能被渲染
            // 如果已经跟随角色作为子对象，AddSingleGO 会包含整个层级
            // 如果不跟随，需要单独添加
            if (_previewRenderUtility != null)
            {
                if (!vfxData.FollowCharacter)
                {
                    // 不跟随角色时，需要手动添加到 PreviewRenderUtility
                    _previewRenderUtility.AddSingleGO(vfxInstance);
                }
            }
            
            // 设置播放速度（如果有 ParticleSystem）
            var particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
            
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.simulationSpeed = vfxData.PlaybackSpeed;
                
                // 播放粒子系统
                ps.Play(true); // 使用 withChildren=true 确保子粒子系统也被播放
            }
            
            // 设置循环（如果有 ParticleSystem）
            if (!vfxData.Loop)
            {
                foreach (var ps in particleSystems)
                {
                    var main = ps.main;
                    main.loop = false;
                }
            }
            
            // 确保GameObject激活
            vfxInstance.SetActive(true);
            
            // 添加到活跃列表
            _activeVFX[evt.EventId] = vfxInstance;
        }
        
        /// <summary>
        /// 更新粒子系统（PreviewRenderUtility 需要手动更新）
        /// </summary>
        public void UpdateParticleSystems(float deltaTime)
        {
            // 如果不在播放状态，特效已经在 UpdateVFXToFrame 中设置到对应时间，不需要继续累积更新
            if (!_isPlaying)
                return;
            
            foreach (var kvp in _activeVFX)
            {
                GameObject vfxInstance = kvp.Value;
                if (vfxInstance == null || !vfxInstance.activeSelf)
                    continue;
                
                // 更新所有粒子系统
                var particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true); // 包含未激活的
                if (particleSystems.Length == 0)
                    continue;
                
                foreach (var ps in particleSystems)
                {
                    if (ps == null)
                        continue;
                    
                    // 确保粒子系统已激活
                    if (!ps.gameObject.activeSelf)
                    {
                        ps.gameObject.SetActive(true);
                    }
                    
                    // 如果粒子系统未播放，重新播放
                    if (!ps.isPlaying && !ps.isPaused)
                    {
                        ps.Play(true);
                    }
                    
                    // 手动模拟粒子系统（PreviewRenderUtility 不会自动更新）
                    // 使用 withChildren=true 确保子粒子系统也被更新
                    ps.Simulate(deltaTime, true, false);
                }
            }
        }
        
        /// <summary>
        /// 更新特效到指定帧（用于暂停状态下预览）
        /// </summary>
        private void UpdateVFXToFrame(int frame)
        {
            const float FRAME_TIME = 0.05f; // 20fps = 50ms/帧
            
            // 遍历所有活跃的特效，更新它们到对应时间
            foreach (var kvp in _activeVFX)
            {
                string eventId = kvp.Key;
                GameObject vfxInstance = kvp.Value;
                
                if (vfxInstance == null || !vfxInstance.activeSelf)
                    continue;
                
                // 查找对应的事件
                var evt = _vfxEvents.Find(e => e.EventId == eventId);
                if (evt == null)
                    continue;
                
                // 检查当前帧是否在特效范围内
                if (frame < evt.StartFrame || frame > evt.EndFrame)
                    continue;
                
                var vfxData = evt.GetEventData<Timeline.EventData.VFXEventData>();
                if (vfxData == null)
                    continue;
                
                // 计算特效已经播放的时间（从开始帧到当前帧）
                int framesElapsed = frame - evt.StartFrame;
                float timeElapsed = framesElapsed * FRAME_TIME;
                
                // 应用播放速度
                float actualTime = timeElapsed / vfxData.PlaybackSpeed;
                
                // 更新所有粒子系统到对应时间
                var particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    if (ps == null)
                        continue;
                    
                    // 确保粒子系统已激活
                    if (!ps.gameObject.activeSelf)
                    {
                        ps.gameObject.SetActive(true);
                    }
                    
                    // 重置并播放到指定时间
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                    
                    // 模拟到指定时间（使用 withChildren=true 确保子粒子系统也被更新）
                    // 第三个参数 restart=true 表示从开始播放，然后模拟到指定时间
                    ps.Simulate(actualTime, true, true);
                }
            }
        }
        
        /// <summary>
        /// 更新特效位置（用于跟随角色）
        /// </summary>
        public void UpdateVFXPositions()
        {
            if (_parentObject == null)
                return;
            
            foreach (var kvp in _activeVFX)
            {
                GameObject vfxInstance = kvp.Value;
                if (vfxInstance == null)
                    continue;
                
                // 查找对应的事件
                var evt = _vfxEvents.Find(e => e.EventId == kvp.Key);
                if (evt == null)
                    continue;
                
                var vfxData = evt.GetEventData<Timeline.EventData.VFXEventData>();
                if (vfxData == null)
                    continue;
                
                // 如果跟随角色，位置会由父对象自动更新
                // 如果不跟随，需要手动更新位置
                if (!vfxData.FollowCharacter)
                {
                    Vector3 position = _parentObject.transform.position;
                    Quaternion rotation = _parentObject.transform.rotation;
                    
                    // 应用位置偏移
                    if (vfxData.PositionOffset != Vector3.zero)
                    {
                        position += rotation * vfxData.PositionOffset;
                    }
                    
                    vfxInstance.transform.position = position;
                }
            }
        }
        
        /// <summary>
        /// 清理所有特效
        /// </summary>
        public void ClearAll()
        {
            foreach (var vfxInstance in _activeVFX.Values)
            {
                if (vfxInstance != null)
                {
                    Object.DestroyImmediate(vfxInstance);
                }
            }
            
            _activeVFX.Clear();
            _currentFrame = -1;
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            ClearAll();
            _parentObject = null;
            _vfxEvents.Clear();
        }
    }
}

