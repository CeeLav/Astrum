using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Data;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 负责在预览窗口中实例化、更新和清理木桩目标
    /// </summary>
    public class HitDummyPreviewController
    {
        private readonly List<GameObject> _dummyInstances = new List<GameObject>();
        private readonly List<HitDummyTargetMarker> _markers = new List<HitDummyTargetMarker>();
        private readonly Dictionary<HitDummyTargetMarker, GameObject> _activeHitEffects = new Dictionary<HitDummyTargetMarker, GameObject>();
        private readonly Dictionary<GameObject, ParticleSystem[]> _effectParticleSystems = new Dictionary<GameObject, ParticleSystem[]>();
        private GameObject _hitEffectRoot;
        private GameObject _rootContainer;
        private GameObject _anchor;
        private PreviewRenderUtility _previewRenderUtility;
        private SkillHitDummyTemplate _currentTemplate;

        public void SetPreviewContext(GameObject anchor, PreviewRenderUtility previewRenderUtility)
        {
            _anchor = anchor;
            _previewRenderUtility = previewRenderUtility;
        }

        public void ApplyTemplate(SkillHitDummyTemplate template)
        {
            _currentTemplate = template;
            RefreshInstances();
            ResetTargets();
        }

        private const float DefaultFrameDelta = 1f / 20f;

        public void UpdateFrame(float deltaTime = DefaultFrameDelta)
        {
            if (_currentTemplate == null || _rootContainer == null)
            {
                return;
            }

            if (_currentTemplate.FollowAnchorPosition && _anchor != null)
            {
                _rootContainer.transform.position = _anchor.transform.position;
            }

            if (_currentTemplate.FollowAnchorRotation && _anchor != null)
            {
                _rootContainer.transform.rotation = _anchor.transform.rotation;
            }

            foreach (var marker in _markers)
            {
                marker?.Update(deltaTime);
            }

            UpdateHitEffects(deltaTime);
        }

        public void Clear()
        {
            DestroyInstances();
            _currentTemplate = null;
        }

        private void RefreshInstances()
        {
            DestroyInstances();
            _markers.Clear();
            _activeHitEffects.Clear();

            if (_currentTemplate == null)
            {
                return;
            }

            if (_anchor == null && _previewRenderUtility == null)
            {
                // 预览环境尚未完成初始化，下一次上下文设置时再刷新
                return;
            }

            _rootContainer = new GameObject("HitDummyRoot");
            _rootContainer.hideFlags = HideFlags.HideAndDontSave;

            if (_currentTemplate.FollowAnchorPosition && _anchor != null)
            {
                _rootContainer.transform.SetParent(_anchor.transform, false);
                _rootContainer.transform.localPosition = Vector3.zero;
                _rootContainer.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _rootContainer.transform.position = Vector3.zero;
                _rootContainer.transform.rotation = Quaternion.identity;
                if (_previewRenderUtility != null)
                {
                    _previewRenderUtility.AddSingleGO(_rootContainer);
                }
            }

            ApplyRootTransform();

            if (_currentTemplate.Placements == null || _currentTemplate.Placements.Count == 0)
            {
                return;
            }

            foreach (var placement in _currentTemplate.Placements)
            {
                if (placement == null) continue;

                GameObject dummyInstance = InstantiatePlacement(placement);
                if (dummyInstance == null) continue;

                dummyInstance.transform.SetParent(_rootContainer.transform, false);
                dummyInstance.transform.localPosition = placement.Position;
                dummyInstance.transform.localRotation = Quaternion.Euler(placement.Rotation);
                dummyInstance.transform.localScale = placement.Scale;

                if (_currentTemplate.LockY)
                {
                    var pos = dummyInstance.transform.localPosition;
                    pos.y = Mathf.Max(0f, pos.y);
                    dummyInstance.transform.localPosition = pos;
                }

                var marker = new HitDummyTargetMarker(dummyInstance);
                _markers.Add(marker);

                _dummyInstances.Add(dummyInstance);
            }
        }

        private void ApplyRootTransform()
        {
            if (_rootContainer == null || _currentTemplate == null) return;

            _rootContainer.transform.localPosition += _currentTemplate.RootOffset;
            _rootContainer.transform.localRotation *= Quaternion.Euler(_currentTemplate.RootRotation);
            _rootContainer.transform.localScale = _currentTemplate.RootScale;
        }

        private GameObject InstantiatePlacement(SkillHitDummyPlacement placement)
        {
            GameObject prefab = placement.OverridePrefab != null
                ? placement.OverridePrefab
                : _currentTemplate.BasePrefab;

            GameObject instance = null;

            if (prefab != null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                ApplyDebugMaterial(instance, placement.DebugColor);
            }

            if (instance == null)
            {
                return null;
            }

            instance.name = placement.Name;
            instance.hideFlags = HideFlags.HideAndDontSave;

            if (prefab != null)
            {
                ApplyDebugMaterial(instance, placement.DebugColor);
            }

            return instance;
        }

        private void ApplyDebugMaterial(GameObject instance, Color color)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null) continue;

                var material = new Material(renderer.sharedMaterial);
                if (material.HasProperty("_Color"))
                {
                    material.color = color;
                    material.SetColor("_Color", color);
                }

                renderer.sharedMaterial = material;
            }
        }

        private void DestroyInstances()
        {
            foreach (var dummy in _dummyInstances)
            {
                if (dummy != null)
                {
                    Object.DestroyImmediate(dummy);
                }
            }
            _dummyInstances.Clear();
            _markers.Clear();

            if (_rootContainer != null)
            {
                Object.DestroyImmediate(_rootContainer);
                _rootContainer = null;
            }

            foreach (var vfx in _activeHitEffects.Values)
            {
                if (vfx != null)
                {
                    Object.DestroyImmediate(vfx);
                }
            }
            _activeHitEffects.Clear();
            _effectParticleSystems.Clear();

            if (_hitEffectRoot != null)
            {
                Object.DestroyImmediate(_hitEffectRoot);
                _hitEffectRoot = null;
            }
        }

        public IReadOnlyList<HitDummyTargetMarker> GetMarkers()
        {
            return _markers;
        }

        public void ResetTargets()
        {
            foreach (var marker in _markers)
            {
                marker?.ResetState();
            }

            foreach (var vfx in _activeHitEffects.Values)
            {
                if (vfx != null)
                {
                    Object.DestroyImmediate(vfx);
                }
            }
            _activeHitEffects.Clear();
            _effectParticleSystems.Clear();
        }

        public bool ApplyKnockback(HitDummyTargetMarker marker, Vector3 direction, float distance, int frame, float durationSeconds, HitDummyKnockbackCurve curve)
        {
            if (marker == null)
                return false;

            var applied = marker.ApplyKnockback(direction, distance, frame, durationSeconds, curve);
            return applied;
        }

        public void PlayHitEffects(IEnumerable<HitDummyInteractionEvaluator.HitDummyFrameResult> damageResults)
        {
            if (damageResults == null)
                return;

            EnsureHitEffectRoot();

            foreach (var result in damageResults)
            {
                if (result == null || result.Target == null || string.IsNullOrEmpty(result.VfxResourcePath))
                    continue;

                GameObject existing = null;
                if (_activeHitEffects.TryGetValue(result.Target, out existing) && existing != null)
                {
                    Object.DestroyImmediate(existing);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(result.VfxResourcePath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[HitDummyPreview] Unable to load hit VFX at path {result.VfxResourcePath}");
                    continue;
                }

                var instance = Object.Instantiate(prefab);
                instance.hideFlags = HideFlags.HideAndDontSave;

                Vector3 position = result.HitPosition + result.VfxOffset;
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.identity;

                if (_hitEffectRoot != null)
                {
                    instance.transform.SetParent(_hitEffectRoot.transform, true);
                }

                var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    ps.Clear(true);
                    ps.Play(true);
                }

                _activeHitEffects[result.Target] = instance;
                _effectParticleSystems[instance] = particleSystems;
            }
        }

        private void EnsureHitEffectRoot()
        {
            if (_hitEffectRoot != null)
                return;

            _hitEffectRoot = new GameObject("HitDummyVFXRoot");
            _hitEffectRoot.hideFlags = HideFlags.HideAndDontSave;

            if (_currentTemplate != null && _currentTemplate.FollowAnchorPosition && _anchor != null)
            {
                _hitEffectRoot.transform.SetParent(_anchor.transform, false);
                _hitEffectRoot.transform.localPosition = Vector3.zero;
                _hitEffectRoot.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _hitEffectRoot.transform.position = Vector3.zero;
                _hitEffectRoot.transform.rotation = Quaternion.identity;
                if (_previewRenderUtility != null)
                {
                    _previewRenderUtility.AddSingleGO(_hitEffectRoot);
                }
            }
        }

        private void UpdateHitEffects(float deltaTime)
        {
            if (_effectParticleSystems.Count == 0)
                return;

            var finishedInstances = new List<GameObject>();

            foreach (var kvp in _effectParticleSystems)
            {
                var instance = kvp.Key;
                var systems = kvp.Value;

                if (instance == null || systems == null || systems.Length == 0)
                {
                    finishedInstances.Add(instance);
                    continue;
                }

                bool anyAlive = false;

                foreach (var ps in systems)
                {
                    if (ps == null)
                        continue;

                    ps.Simulate(deltaTime, true, false);
                    if (ps.IsAlive(true))
                    {
                        anyAlive = true;
                    }
                }

                if (!anyAlive)
                {
                    finishedInstances.Add(instance);
                }
            }

            if (finishedInstances.Count == 0)
                return;

            foreach (var instance in finishedInstances)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }

                _effectParticleSystems.Remove(instance);

                HitDummyTargetMarker removeKey = null;
                foreach (var entry in _activeHitEffects)
                {
                    if (entry.Value == instance)
                    {
                        removeKey = entry.Key;
                        break;
                    }
                }

                if (removeKey != null)
                {
                    _activeHitEffects.Remove(removeKey);
                }
            }
        }
    }
}


