using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.SceneEditor.Data;
using Astrum.Editor.SceneEditor.Converters;

namespace Astrum.Editor.SceneEditor.Visualizers
{
    public class SceneTriggerVisualizer
    {
        private readonly Dictionary<int, GameObject> _visualizerObjects = new Dictionary<int, GameObject>();
        private readonly string _rootName = "SceneTriggerVisualizers";
        private GameObject _rootObject;
        
        public void Initialize()
        {
            Cleanup();
            _rootObject = new GameObject(_rootName);
            _rootObject.hideFlags = HideFlags.DontSave;
        }
        
        public void CreateOrUpdateVisualizer(ActionEditorData action, IPositionInfoProvider positionInfo)
        {
            if (positionInfo == null) return;
            
            var posInfo = positionInfo.GetPositionInfo();
            if (posInfo.Type == PositionType.None) return;
            
            if (!_visualizerObjects.TryGetValue(action.ActionId, out var obj))
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"Trigger_{action.ActionId}";
                obj.transform.SetParent(_rootObject.transform);
                obj.hideFlags = HideFlags.DontSave;
                
                // 设置材质
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = new Color(1f, 0.5f, 0f, 0.3f); // 橙色半透明
                    renderer.material = material;
                }
                
                _visualizerObjects[action.ActionId] = obj;
            }
            
            // 更新位置和大小
            if (posInfo.Type == PositionType.Point && posInfo.Point.HasValue)
            {
                obj.transform.position = posInfo.Point.Value;
                obj.transform.localScale = Vector3.one * 0.5f; // 默认小立方体
            }
            else if (posInfo.Type == PositionType.Range && posInfo.Range.HasValue)
            {
                var bounds = posInfo.Range.Value;
                obj.transform.position = bounds.center;
                obj.transform.localScale = bounds.size;
            }
            
            action.VisualizerObject = obj;
        }
        
        public void RemoveVisualizer(int actionId)
        {
            if (_visualizerObjects.TryGetValue(actionId, out var obj))
            {
                Object.DestroyImmediate(obj);
                _visualizerObjects.Remove(actionId);
            }
        }
        
        public void Cleanup()
        {
            foreach (var obj in _visualizerObjects.Values)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            _visualizerObjects.Clear();
            
            if (_rootObject != null)
            {
                Object.DestroyImmediate(_rootObject);
                _rootObject = null;
            }
        }
    }
}

