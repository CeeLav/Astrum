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

        private static ProjectilePreviewManager _activeInstance;
        public static ProjectilePreviewManager ActiveInstance => _activeInstance;

        private readonly Dictionary<string, ProjectileEventState> _states = new Dictionary<string, ProjectileEventState>();
        private readonly List<ProjectileEventInfo> _events = new List<ProjectileEventInfo>();
        private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        private readonly List<ManualProjectileState> _manualStates = new List<ManualProjectileState>();
        private bool _editorUpdateHooked = false;
        private double _lastEditorUpdateTime = 0f;

        public bool HasManualProjectiles => _manualStates.Count > 0;

        private GameObject _caster;
        private PreviewRenderUtility _previewRenderUtility;

        private Component _socketRefsComponent;
        private System.Reflection.MethodInfo _getSocketTransformMethod;
        private bool _socketComponentSearched;

        public void SetContext(GameObject caster, PreviewRenderUtility previewRenderUtility)
        {
            _activeInstance = this;

            if (_caster != caster || _previewRenderUtility != previewRenderUtility)
            {
                ClearManualProjectiles();
            }

            _caster = caster;
            _previewRenderUtility = previewRenderUtility;
            _socketRefsComponent = null;
            _getSocketTransformMethod = null;
            _socketComponentSearched = false;

            if (_caster == null || _previewRenderUtility == null)
            {
                ClearManualProjectiles();
                return;
            }

            ConfigurePreviewCamera();
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

            ClearManualProjectiles();

            if (_activeInstance == this)
            {
                _activeInstance = null;
            }

            DetachEditorUpdateHook();
        }

        public void ClearAll()
        {
            Cleanup();
            _events.Clear();
        }

        public void FireManualProjectile(SkillEffectTableData effectData, string socketName, Vector3 socketOffset)
        {
            if (effectData == null)
            {
                Debug.LogWarning("[ProjectilePreview] 手动预览失败：效果数据为空");
                return;
            }

            var effectClone = effectData.Clone();
            if (effectClone.SkillEffectId <= 0)
            {
                Debug.LogWarning("[ProjectilePreview] 手动预览失败：SkillEffectId 无效");
                return;
            }

            var eventData = SkillEffectEventData.CreateDefault();
            eventData.EffectIds = new List<int> { effectClone.SkillEffectId };
            eventData.TriggerType = "Manual";
            eventData.SocketName = string.IsNullOrEmpty(socketName) ? string.Empty : socketName;
            eventData.SocketOffset = socketOffset;

            Debug.Log($"[ProjectilePreview] 手动预览 - SocketName: '{eventData.SocketName}', SocketOffset: {eventData.SocketOffset}");

            var fakeEvent = new TimelineEvent
            {
                TrackType = "SkillEffect",
                StartFrame = 0,
                EndFrame = 120
            };
            fakeEvent.SetEventData(eventData);

            var info = BuildEventInfo(fakeEvent, eventData, effectClone.SkillEffectId, effectClone);
            if (info == null)
            {
                Debug.LogWarning("[ProjectilePreview] 手动预览失败：无法构建 Projectile 信息");
                return;
            }

            fakeEvent.EndFrame = info.Projectile.LifeTime > 0 ? info.Projectile.LifeTime : fakeEvent.EndFrame;

            var state = new ProjectileEventState(info);
            var manualState = new ManualProjectileState
            {
                State = state,
                StartTime = EditorApplication.timeSinceStartup
            };

            _manualStates.Add(manualState);
            EnsureEditorUpdateHook();

            TriggerSpawn(state, 0f);
            state.StartElapsed = 0f;
            UpdateProjectileMotion(state, 0f, true);
        }

        public void ClearManualProjectiles()
        {
            foreach (var manual in _manualStates)
            {
                DestroyStateObjects(manual.State);
            }
            _manualStates.Clear();
            DetachEditorUpdateHook();
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

            Debug.Log($"[ProjectilePreview] Event {evt.EventId} -> projectileId={projectileId}, spawn='{projectileData.SpawnEffectPath}', loop='{projectileData.LoopEffectPath}', hit='{projectileData.HitEffectPath}'");

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

            Debug.Log($"[ProjectilePreview] Offsets -> spawn={DescribeOffset(info.SpawnOffset)}, loop={DescribeOffset(info.LoopOffset)}, hit={DescribeOffset(info.HitOffset)}");

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
                Debug.Log($"[ProjectilePreview] Spawn effect '{info.Projectile.SpawnEffectPath}' at {spawnPose.Position} / {spawnPose.Rotation.eulerAngles}");
                state.SpawnFx = InstantiateEffect(info.Projectile.SpawnEffectPath, spawnPose.Position, spawnPose.Rotation, spawnPose.Scale, parentToCaster: true);
            }

            // projectile loop (主体)
            if (!string.IsNullOrEmpty(info.Projectile.LoopEffectPath) && state.ProjectileFx == null)
            {
                var loopPose = ApplyOffset(basePosition, baseRotation, info.LoopOffset);
                Debug.Log($"[ProjectilePreview] Loop effect '{info.Projectile.LoopEffectPath}' start {loopPose.Position} dir={(loopPose.Rotation * Vector3.forward)} speed={state.Speed:F2}");
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
                Debug.Log($"[ProjectilePreview] Hit effect '{info.Projectile.HitEffectPath}' at {hitPose.Position} / {hitPose.Rotation.eulerAngles}");
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

                if (!string.IsNullOrEmpty(info.EventData.SocketName))
                {
                    Debug.LogWarning($"[ProjectilePreview] 未在模型上找到挂点 \"{info.EventData.SocketName}\"，改用角色根节点。");
                }
            }
            else
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
            }

            if (info.EventData.SocketOffset != Vector3.zero)
            {
                if (anchor != null)
                {
                    position += rotation * info.EventData.SocketOffset;
                }
                else if (_caster != null)
                {
                    position += _caster.transform.TransformVector(info.EventData.SocketOffset);
                }
                else
                {
                    position += info.EventData.SocketOffset;
                }
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
            if (info.Projectile.LifeTime > 0)
            {
                return info.Projectile.LifeTime * FRAME_TIME;
            }

            int durationFrames = info.TimelineEvent.GetDuration();
            if (durationFrames > 0)
            {
                return durationFrames * FRAME_TIME;
            }

            return 0.5f;
        }

        private int ResolveEndFrame(ProjectileEventInfo info)
        {
            if (info.Projectile.LifeTime > 0)
            {
                return info.TimelineEvent.StartFrame + info.Projectile.LifeTime;
            }

            return Math.Max(info.TimelineEvent.StartFrame, info.TimelineEvent.EndFrame);
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

            GameObject instance;
            if (parentToCaster && _caster != null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, position, rotation, _caster.transform);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }

            instance.hideFlags = HideFlags.HideAndDontSave;

            if (parentToCaster && _caster != null)
            {
                instance.transform.localScale = scale;
            }
            else
            {
                instance.transform.localScale = scale;
                if (_previewRenderUtility != null)
                {
                    _previewRenderUtility.AddSingleGO(instance);
                }
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

            Debug.Log($"[ProjectilePreview] Loaded prefab '{path}'");
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

        private void EnsureEditorUpdateHook()
        {
            if (_editorUpdateHooked)
                return;

            EditorApplication.update += OnEditorUpdate;
            _editorUpdateHooked = true;
            _lastEditorUpdateTime = EditorApplication.timeSinceStartup;

            ConfigurePreviewCamera();
        }

        private void DetachEditorUpdateHook()
        {
            if (!_editorUpdateHooked)
                return;

            EditorApplication.update -= OnEditorUpdate;
            _editorUpdateHooked = false;
            _lastEditorUpdateTime = 0f;
        }

        private void OnEditorUpdate()
        {
            if (_manualStates.Count == 0)
            {
                DetachEditorUpdateHook();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float deltaTime = _lastEditorUpdateTime > 0 ? (float)(now - _lastEditorUpdateTime) : 0f;
            _lastEditorUpdateTime = now;

            for (int i = _manualStates.Count - 1; i >= 0; --i)
            {
                var manual = _manualStates[i];
                if (!manual.SpawnInitialized)
                {
                    manual.SpawnInitialized = true;
                    if (!manual.State.SpawnTriggered)
                    {
                        TriggerSpawn(manual.State, 0f);
                    }
                }

                float elapsed = (float)(now - manual.StartTime);
                UpdateProjectileMotion(manual.State, elapsed, true);

                int frame = manual.State.Info.TimelineEvent.StartFrame + Mathf.FloorToInt(elapsed / FRAME_TIME);
                TryComplete(manual.State, frame);

                UpdateManualParticles(manual.State, deltaTime);

                if (manual.State.HitTriggered && manual.State.ProjectileFx == null)
                {
                    if (manual.CompleteTime < 0)
                    {
                        manual.CompleteTime = now;
                    }
                    else if (now - manual.CompleteTime > 1.0f)
                    {
                        DestroyStateObjects(manual.State);
                        _manualStates.RemoveAt(i);
                        continue;
                    }
                }
            }

            if (_manualStates.Count == 0)
            {
                DetachEditorUpdateHook();
            }
        }

        private void UpdateManualParticles(ProjectileEventState state, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            SimulateParticles(state.SpawnFx, deltaTime);
            SimulateParticles(state.ProjectileFx, deltaTime);
            SimulateParticles(state.HitFx, deltaTime);
        }

        private void SimulateParticles(GameObject root, float deltaTime)
        {
            if (root == null || !root.activeSelf)
                return;

            var particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                ps.Simulate(deltaTime, true, false, true);
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

        private class ManualProjectileState
        {
            public ProjectileEventState State;
            public double StartTime;
            public double CompleteTime = -1;
            public bool SpawnInitialized = false;
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

        private static string DescribeOffset(ProjectileEffectOffset offset)
        {
            if (offset == null)
            {
                return "<null>";
            }

            return $"pos={offset.Position}, rot={offset.Rotation}, scale={offset.Scale}";
        }

        private void ConfigurePreviewCamera()
        {
            if (_previewRenderUtility == null || _previewRenderUtility.camera == null)
            {
                return;
            }

            var cam = _previewRenderUtility.camera;
            cam.transform.position = new Vector3(0f, 1.5f, -4f);
            cam.transform.LookAt(Vector3.zero);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.fieldOfView = 45f;
            cam.renderingPath = RenderingPath.Forward;
            cam.allowHDR = false;
            cam.allowMSAA = true;
        }
    }
}
