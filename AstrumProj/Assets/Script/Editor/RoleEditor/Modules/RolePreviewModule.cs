using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Animancer;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Services;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 角色预览模块 - 3D模型预览和动画播放（使用Animancer）
    /// </summary>
    public class RolePreviewModule
    {
        // === 渲染组件 ===
        private PreviewRenderUtility _previewRenderUtility;
        private GameObject _previewInstance;
        private AnimancerComponent _animancer;
        private AnimancerState _currentAnimState;
        
        // === 当前数据 ===
        private RoleEditorData _currentRole;
        private List<RoleActionInfo> _availableActions = new List<RoleActionInfo>();
        private int _currentActionId = 0;
        private int _selectedActionIndex = 0;
        
        // === 播放状态 ===
        private bool _isPlaying = false;
        private float _animationSpeed = 1.0f;
        private double _lastUpdateTime = 0;
        
        // === 相机控制 ===
        private Vector2 _dragRotation = Vector2.zero;
        private float _zoomLevel = 3f;
        private Vector3 _modelCenter = Vector3.zero;
        private float _orbitRadius = 3f;
        
        // === 碰撞盒预览 ===
        private bool _showCollisionShape = false;
        
        /// <summary>
        /// 是否显示碰撞盒（可通过外部控制）
        /// </summary>
        public bool ShowCollisionShape
        {
            get => _showCollisionShape;
            set => _showCollisionShape = value;
        }
        
        // === 常量 ===
        private const float MIN_ZOOM = 1f;
        private const float MAX_ZOOM = 10f;
        private const float ZOOM_SPEED = 0.1f;
        
        /// <summary>
        /// 初始化预览系统
        /// </summary>
        public void Initialize()
        {
            if (_previewRenderUtility == null)
            {
                _previewRenderUtility = new PreviewRenderUtility();
                
                // 配置相机
                _previewRenderUtility.camera.transform.position = new Vector3(0, 1, -3);
                _previewRenderUtility.camera.transform.LookAt(Vector3.zero);
                _previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                _previewRenderUtility.camera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
                
                // 调整相机参数
                _previewRenderUtility.camera.nearClipPlane = 0.1f;
                _previewRenderUtility.camera.farClipPlane = 100f;
                _previewRenderUtility.camera.fieldOfView = 60f;
                
                // 添加灯光
                _previewRenderUtility.lights[0].intensity = 1.0f;
                _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0f);
                
                Debug.Log("[RolePreviewModule] Preview system initialized");
            }
        }
        
        /// <summary>
        /// 设置要预览的角色
        /// </summary>
        public void SetRole(RoleEditorData role)
        {
            if (_currentRole == role) return;
            
            _currentRole = role;
            
            if (role == null)
            {
                CleanupPreviewInstance();
                return;
            }
            
            // 加载模型和动画
            ReloadModel();
        }
        
        /// <summary>
        /// 重新加载模型
        /// </summary>
        private void ReloadModel()
        {
            // 清理旧实例
            CleanupPreviewInstance();
            
            if (_currentRole == null || string.IsNullOrEmpty(_currentRole.ModelPath))
            {
                Debug.LogWarning("[RolePreviewModule] No model path specified");
                return;
            }
            
            // 加载模型Prefab
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_currentRole.ModelPath);
            if (prefab == null)
            {
                Debug.LogError($"[RolePreviewModule] Failed to load model: {_currentRole.ModelPath}");
                return;
            }
            
            // 实例化模型
            _previewInstance = Object.Instantiate(prefab);
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            _previewRenderUtility.AddSingleGO(_previewInstance);
            
            // 计算模型边界框和中心点
            CalculateModelBounds();
            
            // 初始化Animancer
            _animancer = AnimationHelper.GetOrAddAnimancer(_previewInstance);
            
            // 关闭Root Motion，防止动画产生位移
            if (_animancer != null && _animancer.Animator != null)
            {
                _animancer.Animator.applyRootMotion = false;
            }
            
            // 获取可用动作列表
            _availableActions = ConfigTableHelper.GetAvailableActions(_currentRole);
            
            // 播放默认动画（待机）
            if (_currentRole.IdleAction > 0)
            {
                PlayAction(_currentRole.IdleAction);
                _selectedActionIndex = 0;
            }
            
            // 重置相机
            _dragRotation = Vector2.zero;
            _zoomLevel = 3f;
            
            Debug.Log($"[RolePreviewModule] Loaded model for role [{_currentRole.RoleId}] {_currentRole.RoleName}");
        }
        
        /// <summary>
        /// 计算模型边界框和中心点
        /// </summary>
        private void CalculateModelBounds()
        {
            if (_previewInstance == null) return;
            
            // 获取所有渲染器
            var renderers = _previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            
            // 计算边界框
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            // 设置模型中心点
            _modelCenter = bounds.center;
            _orbitRadius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 1.5f;
            
            // 调整模型位置，使其中心在原点
            Vector3 offset = _modelCenter;
            _previewInstance.transform.position = -offset;
            _modelCenter = Vector3.zero;
            
            Debug.Log($"[RolePreviewModule] Model bounds: {bounds.size}, Center: {_modelCenter}, Orbit radius: {_orbitRadius}");
        }
        
        /// <summary>
        /// 播放指定动作
        /// </summary>
        public void PlayAction(int actionId)
        {
            if (_animancer == null || actionId <= 0) return;
            
            // 重置模型位置，防止累积位移
            if (_previewInstance != null)
            {
                _previewInstance.transform.position = -_modelCenter;
                _previewInstance.transform.rotation = Quaternion.identity;
            }
            
            // 使用Helper播放动画
            _currentAnimState = AnimationHelper.PlayAnimationByActionId(_animancer, actionId, fadeTime: 0.25f);
            
            if (_currentAnimState != null)
            {
                _currentAnimState.Speed = _animationSpeed;
                _currentActionId = actionId;
                _isPlaying = true;
                _lastUpdateTime = EditorApplication.timeSinceStartup;
            }
        }
        
        /// <summary>
        /// 绘制预览窗口
        /// </summary>
        public void DrawPreview(Rect rect)
        {
            if (_previewRenderUtility == null)
            {
                Initialize();
            }
            
            if (_currentRole == null || _previewInstance == null)
            {
                EditorGUI.HelpBox(rect, "请先选择一个角色", MessageType.Info);
                return;
            }
            
            HandleInput(rect);
            RenderPreview(rect);
            // DrawControlPanel(rect); // 移除，控制面板现在在外部处理
        }
        
        /// <summary>
        /// 处理输入（鼠标拖拽和缩放）
        /// </summary>
        private void HandleInput(Rect rect)
        {
            Event e = Event.current;
            
            if (!rect.Contains(e.mousePosition))
                return;
            
            // 左键拖拽旋转
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                _dragRotation.x += e.delta.x;
                _dragRotation.y -= e.delta.y;
                e.Use();
            }
            
            // 滚轮缩放
            if (e.type == EventType.ScrollWheel)
            {
                _zoomLevel += e.delta.y * ZOOM_SPEED;
                _zoomLevel = Mathf.Clamp(_zoomLevel, MIN_ZOOM, MAX_ZOOM);
                e.Use();
            }
        }
        
        /// <summary>
        /// 渲染预览
        /// </summary>
        private void RenderPreview(Rect rect)
        {
            // 预留底部控制面板的空间，确保最小高度
            float controlPanelHeight = Mathf.Min(100f, rect.height * 0.3f);
            float previewHeight = Mathf.Max(50f, rect.height - controlPanelHeight);
            
            Rect previewRect = new Rect(rect.x, rect.y, rect.width, previewHeight);
            
            // 确保渲染区域有效
            if (previewRect.width <= 0 || previewRect.height <= 0)
            {
                EditorGUI.HelpBox(rect, "预览区域太小", MessageType.Warning);
                return;
            }
            
            _previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);
            
            if (_previewInstance != null)
            {
                // 计算球形轨道相机位置
                float theta = _dragRotation.x * Mathf.Deg2Rad; // 水平角度
                float phi = _dragRotation.y * Mathf.Deg2Rad;   // 垂直角度
                
                // 限制垂直角度，避免翻转
                phi = Mathf.Clamp(phi, -80f * Mathf.Deg2Rad, 80f * Mathf.Deg2Rad);
                
                // 计算相机位置（球形坐标）
                float radius = _orbitRadius * _zoomLevel;
                Vector3 cameraPos = _modelCenter + new Vector3(
                    radius * Mathf.Sin(phi) * Mathf.Cos(theta),
                    radius * Mathf.Cos(phi),
                    radius * Mathf.Sin(phi) * Mathf.Sin(theta)
                );
                
                // 设置相机位置和朝向
                _previewRenderUtility.camera.transform.position = cameraPos;
                _previewRenderUtility.camera.transform.LookAt(_modelCenter);
                
                // 调整相机裁剪平面，确保模型不被裁剪
                float distance = Vector3.Distance(cameraPos, _modelCenter);
                _previewRenderUtility.camera.nearClipPlane = Mathf.Max(0.01f, distance * 0.01f);
                _previewRenderUtility.camera.farClipPlane = distance * 10f;
                
                // 模型保持原始旋转（不跟随拖拽）
                // _previewInstance.transform.rotation = Quaternion.identity;
                
                // 更新Animancer（预览模式的关键）
                if (_isPlaying && _animancer != null)
                {
                    double currentTime = EditorApplication.timeSinceStartup;
                    float deltaTime = (float)(currentTime - _lastUpdateTime);
                    
                    AnimationHelper.EvaluateAnimancer(_animancer, deltaTime * _animationSpeed);
                    
                    _lastUpdateTime = currentTime;
                }
                
                // 渲染
                _previewRenderUtility.camera.Render();
                
                // 绘制碰撞盒（在 camera.Render() 后，EndPreview() 前）
                if (_showCollisionShape && _currentRole != null && !string.IsNullOrEmpty(_currentRole.CollisionData))
                {
                    CollisionShapePreview.DrawCollisionData(_currentRole.CollisionData, _previewRenderUtility.camera);
                }
            }
            
            Texture resultTexture = _previewRenderUtility.EndPreview();
            GUI.DrawTexture(previewRect, resultTexture, ScaleMode.StretchToFill, false);
            
            // 绘制提示
            DrawHints(previewRect);
        }
        
        /// <summary>
        /// 绘制控制面板
        /// </summary>
        private void DrawControlPanel(Rect rect)
        {
            // 底部控制区域
            Rect controlRect = new Rect(rect.x, rect.yMax - 100, rect.width, 100);
            GUI.Box(controlRect, "", EditorStyles.toolbar);
            
            GUILayout.BeginArea(controlRect);
            {
                GUILayout.Space(5);
                
                // 播放控制按钮
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button(_isPlaying ? "暂停" : "播放", GUILayout.Width(60), GUILayout.Height(25)))
                    {
                        _isPlaying = !_isPlaying;
                        if (_isPlaying)
                        {
                            _lastUpdateTime = EditorApplication.timeSinceStartup;
                        }
                    }
                    
                    if (GUILayout.Button("停止", GUILayout.Width(60), GUILayout.Height(25)))
                    {
                        _isPlaying = false;
                        if (_currentActionId > 0)
                        {
                            PlayAction(_currentActionId); // 重新播放（重置到开头）
                            _isPlaying = false;
                        }
                    }
                    
                    if (GUILayout.Button("重置视角", GUILayout.Width(80), GUILayout.Height(25)))
                    {
                        _dragRotation = Vector2.zero;
                        _zoomLevel = 3f;
                    }
                    
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                // 动画选择
                if (_availableActions.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("动画:", GUILayout.Width(40));
                        
                        string[] actionLabels = _availableActions.Select(a => a.ToString()).ToArray();
                        int newIndex = EditorGUILayout.Popup(_selectedActionIndex, actionLabels);
                        
                        if (newIndex != _selectedActionIndex)
                        {
                            _selectedActionIndex = newIndex;
                            PlayAction(_availableActions[newIndex].ActionId);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("该角色没有配置动作", MessageType.Info);
                }
                
                GUILayout.Space(5);
                
                // 速度控制
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("速度:", GUILayout.Width(40));
                    float newSpeed = EditorGUILayout.Slider(_animationSpeed, 0.1f, 2.0f);
                    
                    if (Mathf.Abs(newSpeed - _animationSpeed) > 0.01f)
                    {
                        _animationSpeed = newSpeed;
                        if (_currentAnimState != null)
                        {
                            _currentAnimState.Speed = _animationSpeed;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 绘制操作提示
        /// </summary>
        private void DrawHints(Rect rect)
        {
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 10;
            hintStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            hintStyle.alignment = TextAnchor.LowerCenter;
            
            Rect hintRect = new Rect(rect.x, rect.yMax - 20, rect.width, 16);
            GUI.Label(hintRect, "左键拖拽旋转 | 滚轮缩放", hintStyle);
        }
        
        /// <summary>
        /// 清理预览实例
        /// </summary>
        private void CleanupPreviewInstance()
        {
            if (_previewInstance != null)
            {
                Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
                _animancer = null;
                _currentAnimState = null;
            }
        }
        
        
        /// <summary>
        /// 停止动画
        /// </summary>
        public void StopAnimation()
        {
            if (_animancer != null)
            {
                _animancer.Stop();
            }
            _isPlaying = false;
        }
        
        /// <summary>
        /// 设置动画播放速度
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            _animationSpeed = speed;
            if (_animancer != null && _currentAnimState != null)
            {
                _currentAnimState.Speed = speed;
            }
        }
        
        /// <summary>
        /// 重置相机视角
        /// </summary>
        public void ResetCamera()
        {
            _dragRotation = Vector2.zero;
            _zoomLevel = 3f;
        }
        
        /// <summary>
        /// 清理所有资源
        /// </summary>
        public void Cleanup()
        {
            CleanupPreviewInstance();
            
            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }
            
            Debug.Log("[RolePreviewModule] Cleaned up preview resources");
        }
    }
}

