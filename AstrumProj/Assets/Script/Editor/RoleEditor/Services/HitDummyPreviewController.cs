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
        private GameObject _rootContainer;
        private GameObject _anchor;
        private PreviewRenderUtility _previewRenderUtility;
        private SkillHitDummyTemplate _currentTemplate;

        public void SetPreviewContext(GameObject anchor, PreviewRenderUtility previewRenderUtility)
        {
            _anchor = anchor;
            _previewRenderUtility = previewRenderUtility;

            if (_currentTemplate != null)
            {
                RefreshInstances();
            }
        }

        public void ApplyTemplate(SkillHitDummyTemplate template)
        {
            _currentTemplate = template;
            RefreshInstances();
        }

        public void UpdateFrame()
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
        }

        public void Clear()
        {
            DestroyInstances();
            _currentTemplate = null;
        }

        private void RefreshInstances()
        {
            DestroyInstances();

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

                var marker = dummyInstance.GetComponent<HitDummyTargetMarker>();
                if (marker == null)
                {
                    try
                    {
                        marker = dummyInstance.AddComponent<HitDummyTargetMarker>();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[HitDummyPreview] Failed to add HitDummyTargetMarker: {ex.Message}", dummyInstance);
                    }
                }

                if (marker != null)
                {
                    marker.Initialize();
                }
                else
                {
                    Debug.LogWarning("[HitDummyPreview] Hit dummy instance is missing HitDummyTargetMarker and will be skipped.", dummyInstance);
                    continue;
                }

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

            if (_rootContainer != null)
            {
                Object.DestroyImmediate(_rootContainer);
                _rootContainer = null;
            }
        }
    }
}


