using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Astrum.Editor.RoleEditor.SkillEffectEditors;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;

namespace Astrum.Editor.RoleEditor.Services
{
    internal class ProjectilePreviewManager
    {
        private const float FRAME_TIME = 0.05f; // 20fps
        private const float DEFAULT_BASE_SPEED = 6f; // 米/秒（预设值，当配置缺失时使用）

        private readonly Dictionary<string, ProjectileEventState> _states = new Dictionary<string, ProjectileEventState>();
        private readonly List<ProjectileEventInfo> _events = new List<ProjectileEventInfo>();
        private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

        private GameObject _caster;
        private PreviewRenderUtility _previewRenderUtility;

        private Component _socketRefsComponent;
        private System.Reflection.MethodInfo _getSocketTransformMethod;
        private bool _socketComponentSearched;

        public void SetContext(GameObject caster, PreviewRenderUtility previewRenderUtility)
        {
            _caster = caster;
            _previewRenderUtility = previewRenderUtility;
            _socketRefsComponent = null;
            _getSocketTransformMethod = null;
            _socketComponentSearched = false;
        }

        public void SetTimelineEvents(IReadOnlyList<TimelineEvent> timelineEvents)
        {
            _events.Clear();
            _states.Clear();

            if (timelineEvents == null || timelineEvents.Count == 0)
            {
                return;
            }

            foreach (var evt in timelineEvents)
            {
                if (evt == null || !string.Equals(evt.TrackType, "SkillEffect", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var effectData = evt.GetEventData<SkillEffectEventData>();
                if (effectData?.EffectIds == null || effectData.EffectIds.Count == 0)
                {
                    continue;
                }

                foreach (var effectId in effectData.EffectIds)
                {
                    if (effectId <= 0)
                    {
                        continue;
                    }

                    var effectConfig = SkillEffectDataReader.GetSkillEffect(effectId);
                    if (effectConfig == null)
                    {
                        continue;
                    }

                    if (!string.Equals(effectConfig.EffectType, "Projectile", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var projectileInfo = BuildEventInfo(evt, effectData, effectId, effectConfig);
                    if (projectileInfo != null)
                    {
                        _events.Add(projectileInfo);
                    }
                }
            }
        }

        public void UpdateFrame(int frame, bool isPlaying)
        {
            if (_events.Count == 0)
            {
                return;
            }

            foreach (var info in _events)
            {
                var state = GetOrCreateState(info);
                EvaluateState(state, frame, isPlaying);
            }
        }

        public void Cleanup()
        {
            foreach (var state in _states.Values)
            {
                DestroyStateObjects(state);
            }
            _states.Clear();
        }

        public void ClearAll()
        {
            Cleanup();
            _events.Clear();
        }

        private ProjectileEventInfo BuildEventInfo(TimelineEvent evt, SkillEffectEventData eventData, int effectId, SkillEffectTableData effectConfig)
        {
            if (effectConfig?.IntParams == null || effectConfig.IntParams.Count == 0)
            {
                Debug.LogWarning($"[ProjectilePreview] Effect {effectId} 缺少 ProjectileId");
                return null;
            }

            int projectileId = effectConfig.IntParams[0];
            if (projectileId <= 0)
            {
                Debug.LogWarning($"[ProjectilePreview] Effect {effectId} 的 ProjectileId 无效");
                return null;
            }

            var projectileData = ProjectileDataReader.GetProjectile(projectileId);
            if (projectileData == null)
            {
                Debug.LogWarning($"[ProjectilePreview] 未找到 ProjectileId {projectileId} 对应的数据");
                return null;
            }

            var stringParams = effectConfig.StringParams ?? new List<string>();

            string spawnOffsetJson = stringParams.Count > 1 ? stringParams[1] : string.Empty;
            string loopOffsetJson = stringParams.Count > 2 ? stringParams[2] : string.Empty;
            string hitOffsetJson = stringParams.Count > 3 ? stringParams[3] : string.Empty;

            var info = new ProjectileEventInfo
            {
                TimelineEvent = evt,
                EventData = eventData,
                EffectId = effectId,
                Projectile = projectileData,
                SpawnOffset = ProjectileEffectOffsetUtility.Parse(spawnOffsetJson),
                LoopOffset = ProjectileEffectOffsetUtility.Parse(loopOffsetJson),
                HitOffset = ProjectileEffectOffsetUtility.Parse(hitOffsetJson)
            };

            return info;
        }

        private ProjectileEventState GetOrCreateState(ProjectileEventInfo info)
        {
            string key = GetStateKey(info);
            if (!_states.TryGetValue(key, out var state))
            {
                state = new ProjectileEventState(info);
                _states[key] = state;
            }

            return state;
        }

        private void EvaluateState(ProjectileEventState state, int frame, bool isPlaying)
        {
            var info = state.Info;
            int startFrame = info.TimelineEvent.StartFrame;

            if (frame < startFrame)
            {
                ResetState(state);
                return;
            }

            float elapsed = (frame - startFrame) * FRAME_TIME;

            if (!state.SpawnTriggered)
            {
                TriggerSpawn(state, elapsed);
            }

            if (state.ProjectileFx == null)
            {
                // 没有飞行特效，仍然检查是否需要触发命中特效
                TryComplete(state, frame);
                return;
            }

            UpdateProjectileMotion(state, elapsed, isPlaying);
            TryComplete(state, frame);
        }

        private void TriggerSpawn(ProjectileEventState state, float elapsed)
        {
            var info = state.Info;

            // 计算基础姿态
            GetBasePose(info, out Vector3 basePosition, out Quaternion baseRotation);

            // spawn effect
            if (!string.IsNullOrEmpty(info.Projectile.SpawnEffectPath) && !state.SpawnTriggered)
            {
                var spawnPose = ApplyOffset(basePosition, baseRotation, info.SpawnOffset);
                state.SpawnFx = InstantiateEffect(info.Projectile.SpawnEffectPath, spawnPose.Position, spawnPose.Rotation, spawnPose.Scale, parentToCaster: true);
            }

            // projectile loop (主体)
            if (!string.IsNullOrEmpty(info.Projectile.LoopEffectPath) && state.ProjectileFx == null)
            {
                var loopPose = ApplyOffset(basePosition, baseRotation, info.LoopOffset);
                state.ProjectileFx = InstantiateEffect(info.Projectile.LoopEffectPath, loopPose.Position, loopPose.Rotation, loopPose.Scale, parentToCaster: false);
                state.StartPosition = loopPose.Position;
                state.BaseRotation = loopPose.Rotation;
                state.Direction = loopPose.Rotation * Vector3.forward;
                state.Speed = ResolveBaseSpeed(info.Projectile.TrajectoryData);
                state.ActiveDuration = ResolveActiveDuration(info);
                state.EndFrame = ResolveEndFrame(info);
            }

            state.SpawnTriggered = true;
            state.StartElapsed = elapsed;
        }

        private void UpdateProjectileMotion(ProjectileEventState state, float elapsed, bool isPlaying)
        {
            if (state.ProjectileFx == null)
            {
                return;
            }

            float travelTime = Mathf.Clamp(elapsed, 0f, state.ActiveDuration);
            float distance = state.Speed * travelTime;
            Vector3 position = state.StartPosition + state.Direction * distance;

            state.ProjectileFx.transform.position = position;
            state.ProjectileFx.transform.rotation = state.BaseRotation;
            state.LastKnownPosition = position;
        }

        private void TryComplete(ProjectileEventState state, int frame)
        {
            if (state.HitTriggered)
            {
                return;
            }

            bool shouldEnd = frame >= state.EndFrame || state.ActiveDuration <= 0f;
            if (!shouldEnd)
            {
                return;
            }

            TriggerHit(state);
        }

        private void TriggerHit(ProjectileEventState state)
        {
            var info = state.Info;
            GetBasePose(info, out Vector3 basePosition, out Quaternion baseRotation);

            Vector3 impactPosition = state.LastKnownPosition != Vector3.zero ? state.LastKnownPosition : basePosition;
            Quaternion impactRotation = baseRotation;

            var hitPose = ApplyOffset(impactPosition, impactRotation, info.HitOffset, useWorldOffset: true);

            if (!string.IsNullOrEmpty(info.Projectile.HitEffectPath))
            {
                state.HitFx = InstantiateEffect(info.Projectile.HitEffectPath, hitPose.Position, hitPose.Rotation, hitPose.Scale, parentToCaster: false);
            }

            if (state.ProjectileFx != null)
            {
                UnityEngine.Object.DestroyImmediate(state.ProjectileFx);
                state.ProjectileFx = null;
            }

            state.HitTriggered = true;
        }

        private void ResetState(ProjectileEventState state)
        {
            DestroyStateObjects(state);
            state.Reset();
        }

        private void DestroyStateObjects(ProjectileEventState state)
        {
            if (state.SpawnFx != null)
            {
                UnityEngine.Object.DestroyImmediate(state.SpawnFx);
                state.SpawnFx = null;
            }

            if (state.ProjectileFx != null)
            {
                UnityEngine.Object.DestroyImmediate(state.ProjectileFx);
                state.ProjectileFx = null;
            }

            if (state.HitFx != null)
            {
                UnityEngine.Object.DestroyImmediate(state.HitFx);
                state.HitFx = null;
            }
        }

        private void GetBasePose(ProjectileEventInfo info, out Vector3 position, out Quaternion rotation)
        {
            Transform anchor = ResolveSocketTransform(info.EventData.SocketName);

            if (anchor != null)
            {
                position = anchor.position;
                rotation = anchor.rotation;
            }
            else if (_caster != null)
            {
                position = _caster.transform.position;
                rotation = _caster.transform.rotation;
            }
            else
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
            }

            if (_caster != null && info.EventData.SocketOffset != Vector3.zero)
            {
                position += _caster.transform.TransformVector(info.EventData.SocketOffset);
            }
        }

        private OffsetPose ApplyOffset(Vector3 basePosition, Quaternion baseRotation, ProjectileEffectOffset offset, bool useWorldOffset = false)
        {
            if (offset == null)
            {
                offset = ProjectileEffectOffset.Default();
            }

            Vector3 position = basePosition + (useWorldOffset ? offset.Position : baseRotation * offset.Position);
            Quaternion rotation = baseRotation * Quaternion.Euler(offset.Rotation);
            Vector3 scale = ProjectileEffectOffsetUtility.EnsureValidScale(offset.Scale);

            return new OffsetPose(position, rotation, scale);
        }

        private float ResolveBaseSpeed(string trajectoryData)
        {
            if (string.IsNullOrEmpty(trajectoryData))
            {
                return DEFAULT_BASE_SPEED;
            }

            try
            {
                var data = JsonUtility.FromJson<TrajectoryData>(trajectoryData);
                if (data == null)
                {
                    return DEFAULT_BASE_SPEED;
                }

                if (data.BaseSpeed > 0f)
                {
                    return data.BaseSpeed;
                }

                if (data.LaunchSpeed > 0f)
                {
                    return data.LaunchSpeed;
                }
            }
            catch
            {
                // ignore
            }

            return DEFAULT_BASE_SPEED;
        }

        private float ResolveActiveDuration(ProjectileEventInfo info)
        {
            float lifetimeSeconds = info.Projectile.LifeTime > 0 ? info.Projectile.LifeTime * FRAME_TIME : 0f;
            if (lifetimeSeconds <= 0f)
            {
                lifetimeSeconds = (info.TimelineEvent.GetDuration()) * FRAME_TIME;
            }

            if (lifetimeSeconds <= 0f)
            {
                lifetimeSeconds = 0.5f;
            }

            return lifetimeSeconds;
        }

        private int ResolveEndFrame(ProjectileEventInfo info)
        {
            int endFrameByEvent = info.TimelineEvent.EndFrame;
            int endFrameByLifetime = info.TimelineEvent.StartFrame + (info.Projectile.LifeTime > 0 ? info.Projectile.LifeTime : 0);

            if (info.Projectile.LifeTime <= 0)
            {
                return endFrameByEvent;
            }

            return Math.Max(info.TimelineEvent.StartFrame, Math.Min(endFrameByEvent, endFrameByLifetime));
        }

        private Transform ResolveSocketTransform(string socketName)
        {
            if (string.IsNullOrEmpty(socketName) || _caster == null)
            {
                return null;
            }

            EnsureSocketRefsComponent();

            Transform socket = null;

            if (_socketRefsComponent != null && _getSocketTransformMethod != null)
            {
                try
                {
                    var result = _getSocketTransformMethod.Invoke(_socketRefsComponent, new object[] { socketName });
                    socket = result as Transform;
                }
                catch
                {
                    socket = null;
                }
            }

            if (socket == null)
            {
                socket = _caster.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => string.Equals(t.name, socketName, StringComparison.OrdinalIgnoreCase));
            }

            return socket;
        }

        private void EnsureSocketRefsComponent()
        {
            if (_socketComponentSearched)
            {
                return;
            }

            _socketComponentSearched = true;

            if (_caster == null)
            {
                return;
            }

            _socketRefsComponent = _caster.GetComponentsInChildren<Component>(true)
                .FirstOrDefault(c => c != null && c.GetType().Name == "SocketRefs");

            if (_socketRefsComponent != null)
            {
                _getSocketTransformMethod = _socketRefsComponent.GetType().GetMethod("GetSocketTransform", new[] { typeof(string) });
            }
        }

        private GameObject InstantiateEffect(string path, Vector3 position, Quaternion rotation, Vector3 scale, bool parentToCaster)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var prefab = LoadPrefab(path);
            if (prefab == null)
            {
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;

            if (parentToCaster && _caster != null)
            {
                instance.transform.SetParent(_caster.transform, true);
            }

            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.AddSingleGO(instance);
            }

            ActivateParticles(instance);

            return instance;
        }

        private GameObject LoadPrefab(string path)
        {
            if (_prefabCache.TryGetValue(path, out var cached) && cached != null)
            {
                return cached;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[ProjectilePreview] 无法加载特效资源: {path}");
                return null;
            }

            _prefabCache[path] = prefab;
            return prefab;
        }

        private void ActivateParticles(GameObject instance)
        {
            var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                ps.Simulate(0f, true, true, true);
                ps.Play(true);
            }
        }

        private string GetStateKey(ProjectileEventInfo info)
        {
            return $"{info.TimelineEvent.EventId}_{info.EffectId}";
        }

        private class ProjectileEventInfo
        {
            public TimelineEvent TimelineEvent;
            public SkillEffectEventData EventData;
            public int EffectId;
            public ProjectileTableData Projectile;
            public ProjectileEffectOffset SpawnOffset;
            public ProjectileEffectOffset LoopOffset;
            public ProjectileEffectOffset HitOffset;
        }

        private class ProjectileEventState
        {
            public ProjectileEventInfo Info { get; }
            public bool SpawnTriggered;
            public bool HitTriggered;
            public GameObject SpawnFx;
            public GameObject ProjectileFx;
            public GameObject HitFx;
            public Vector3 StartPosition;
            public Quaternion BaseRotation = Quaternion.identity;
            public Vector3 Direction = Vector3.forward;
            public float Speed = DEFAULT_BASE_SPEED;
            public float ActiveDuration = 0.5f;
            public Vector3 LastKnownPosition = Vector3.zero;
            public float StartElapsed;
            public int EndFrame;

            public ProjectileEventState(ProjectileEventInfo info)
            {
                Info = info;
            }

            public void Reset()
            {
                SpawnTriggered = false;
                HitTriggered = false;
                StartPosition = Vector3.zero;
                Direction = Vector3.forward;
                Speed = DEFAULT_BASE_SPEED;
                ActiveDuration = 0.5f;
                LastKnownPosition = Vector3.zero;
            }
        }

        [Serializable]
        private class TrajectoryData
        {
            public float BaseSpeed = DEFAULT_BASE_SPEED;
            public float LaunchSpeed = DEFAULT_BASE_SPEED;
        }

        private readonly struct OffsetPose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;

            public OffsetPose(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }
        }
    }
}
