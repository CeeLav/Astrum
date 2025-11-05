using UnityEngine;
using UnityEditor;
using Animancer;
using Astrum.Editor.RoleEditor.Services;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 预览模块基类 - 提供通用的3D模型预览和动画播放功能
    /// </summary>
    public abstract class BasePreviewModule
    {
        // === 渲染组件 ===
        protected PreviewRenderUtility _previewRenderUtility;
        protected GameObject _previewInstance;
        protected AnimancerComponent _animancer;
        protected AnimancerState _currentAnimState;
        
        // === 播放状态 ===
        protected bool _isPlaying = false;
        protected float _animationSpeed = 1.0f;
        protected double _lastUpdateTime = 0;
        
        // === 相机控制 ===
        protected Vector2 _dragRotation = new Vector2(0f, 30f);  // 默认30度俯视角（斜上方）
        protected float _zoomLevel = 2f;  // 默认距离更近
        protected Vector3 _modelCenter = Vector3.zero;
        protected float _orbitRadius = 3f;
        
        protected const float MIN_ZOOM = 1f;
        protected const float MAX_ZOOM = 10f;
        protected const float ZOOM_SPEED = 0.1f;
        
        protected virtual string LogPrefix => "[BasePreviewModule]";
        
        // === 初始化和清理 ===
        
        /// <summary>
        /// 初始化预览系统
        /// </summary>
        public virtual void Initialize()
        {
            if (_previewRenderUtility == null)
            {
                _previewRenderUtility = new PreviewRenderUtility();
                
                // 配置相机
                _previewRenderUtility.camera.transform.position = new Vector3(0, 1, -3);
                _previewRenderUtility.camera.transform.LookAt(Vector3.zero);
                _previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                _previewRenderUtility.camera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
                _previewRenderUtility.camera.nearClipPlane = 0.1f;
                _previewRenderUtility.camera.farClipPlane = 100f;
                _previewRenderUtility.camera.fieldOfView = 60f;
                
                // 确保相机能渲染粒子系统
                _previewRenderUtility.camera.renderingPath = RenderingPath.Forward;
                _previewRenderUtility.camera.allowHDR = false;
                _previewRenderUtility.camera.allowMSAA = false;
                
                // 灯光
                _previewRenderUtility.lights[0].intensity = 1.0f;
                _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0f);
                
                Debug.Log($"{LogPrefix} Preview system initialized");
            }
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public virtual void Cleanup()
        {
            CleanupPreviewInstance();
            
            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }
            
            Debug.Log($"{LogPrefix} Cleaned up preview resources");
        }
        
        // === 模型管理 ===
        
        /// <summary>
        /// 加载角色模型
        /// </summary>
        protected void LoadModel(string modelPath)
        {
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogWarning($"{LogPrefix} Model path is empty");
                CleanupPreviewInstance();
                return;
            }
            
            // 清理旧实例
            CleanupPreviewInstance();
            
            // 加载模型Prefab
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null)
            {
                Debug.LogError($"{LogPrefix} Failed to load model: {modelPath}");
                return;
            }
            
            // 实例化模型
            _previewInstance = Object.Instantiate(prefab);
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            _previewRenderUtility.AddSingleGO(_previewInstance);
            
            // 计算边界和中心点
            CalculateModelBounds();
            
            // 获取或添加Animancer
            _animancer = AnimationHelper.GetOrAddAnimancer(_previewInstance);
            
            // 关闭Root Motion
            if (_animancer != null && _animancer.Animator != null)
            {
                _animancer.Animator.applyRootMotion = false;
            }
            
            Debug.Log($"{LogPrefix} Loaded model: {modelPath}");
        }
        
        /// <summary>
        /// 计算模型边界框和中心点
        /// </summary>
        protected void CalculateModelBounds()
        {
            if (_previewInstance == null) return;
            
            // 获取所有渲染器
            var renderers = _previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                _modelCenter = Vector3.zero;
                _orbitRadius = 2f;
                return;
            }
            
            // 计算边界框
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            // 设置模型中心点（保持模型在原点，相机环绕模型中心）
            _modelCenter = bounds.center;
            _orbitRadius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 1.5f;
            
            // 确保模型位置在原点（不移动模型，让相机适应）
            _previewInstance.transform.position = Vector3.zero;
            _previewInstance.transform.rotation = Quaternion.identity;
            
            Debug.Log($"{LogPrefix} Model bounds calculated: center={_modelCenter}, size={bounds.size}, orbitRadius={_orbitRadius}");
        }
        
        /// <summary>
        /// 清理预览实例
        /// </summary>
        protected void CleanupPreviewInstance()
        {
            if (_previewInstance != null)
            {
                Object.DestroyImmediate(_previewInstance);
                _previewInstance = null;
                _animancer = null;
                _currentAnimState = null;
            }
        }
        
        // === 播放控制 ===
        
        /// <summary>
        /// 播放动画
        /// </summary>
        public virtual void Play()
        {
            if (_currentAnimState != null)
            {
                _isPlaying = true;
                _currentAnimState.Speed = _animationSpeed;
                _lastUpdateTime = EditorApplication.timeSinceStartup;
                Debug.Log($"{LogPrefix} Playing");
            }
        }
        
        /// <summary>
        /// 暂停动画
        /// </summary>
        public virtual void Pause()
        {
            if (_currentAnimState != null)
            {
                _isPlaying = false;
                _currentAnimState.Speed = 0;
                Debug.Log($"{LogPrefix} Paused");
            }
        }
        
        /// <summary>
        /// 停止动画
        /// </summary>
        public virtual void Stop()
        {
            if (_currentAnimState != null)
            {
                _isPlaying = false;
                _currentAnimState.Time = 0;
                _currentAnimState.Speed = 0;
                Debug.Log($"{LogPrefix} Stopped");
            }
        }
        
        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying()
        {
            return _isPlaying;
        }
        
        /// <summary>
        /// 设置动画速度
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            _animationSpeed = speed;
            if (_currentAnimState != null)
            {
                _currentAnimState.Speed = _isPlaying ? speed : 0;
            }
        }
        
        /// <summary>
        /// 重置相机视角
        /// </summary>
        public void ResetCamera()
        {
            _dragRotation = new Vector2(0f, 30f);  // 30度俯视角
            _zoomLevel = 2f;  // 距离更近
        }
        
        // === 渲染 ===
        
        /// <summary>
        /// 绘制预览（抽象方法，由子类实现）
        /// </summary>
        public abstract void DrawPreview(Rect rect);
        
        /// <summary>
        /// 渲染预览场景
        /// </summary>
        protected void RenderPreview(Rect rect)
        {
            if (_previewRenderUtility == null || _previewInstance == null)
            {
                DrawEmptyPreview(rect);
                return;
            }
            
            // 更新相机位置（球面坐标）
            UpdateCamera();
            
            // 更新动画
            UpdateAnimation();
            
            // 渲染
            _previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            _previewRenderUtility.camera.Render();
            Texture texture = _previewRenderUtility.EndPreview();
            GUI.DrawTexture(rect, texture);
        }
        
        /// <summary>
        /// 绘制空预览（无模型时）
        /// </summary>
        protected void DrawEmptyPreview(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.gray;
            GUI.Label(rect, "No Model Loaded", style);
        }
        
        /// <summary>
        /// 更新相机位置
        /// </summary>
        protected void UpdateCamera()
        {
            if (_previewRenderUtility == null) return;
            
            // 球面坐标计算
            float theta = _dragRotation.x * Mathf.Deg2Rad;
            float phi = _dragRotation.y * Mathf.Deg2Rad;
            float radius = _orbitRadius * _zoomLevel;
            
            Vector3 cameraPos = _modelCenter + new Vector3(
                radius * Mathf.Sin(phi) * Mathf.Cos(theta),
                radius * Mathf.Cos(phi),
                radius * Mathf.Sin(phi) * Mathf.Sin(theta)
            );
            
            _previewRenderUtility.camera.transform.position = cameraPos;
            _previewRenderUtility.camera.transform.LookAt(_modelCenter);
            
            // 调整裁剪平面
            float distance = Vector3.Distance(cameraPos, _modelCenter);
            _previewRenderUtility.camera.nearClipPlane = Mathf.Max(0.01f, distance * 0.01f);
            _previewRenderUtility.camera.farClipPlane = distance * 10f;
        }
        
        /// <summary>
        /// 更新动画
        /// </summary>
        protected virtual void UpdateAnimation()
        {
            if (_isPlaying && _currentAnimState != null && _animancer != null)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentTime - _lastUpdateTime);
                _lastUpdateTime = currentTime;
                
                // 手动驱动Animancer
                AnimationHelper.EvaluateAnimancer(_animancer, deltaTime * _animationSpeed);
            }
        }
        
        /// <summary>
        /// 处理输入（鼠标拖拽和缩放）
        /// </summary>
        protected void HandleInput(Rect rect)
        {
            Event evt = Event.current;
            
            if (!rect.Contains(evt.mousePosition))
                return;
            
            // 左键拖拽旋转
            if (evt.type == EventType.MouseDrag && evt.button == 0)
            {
                _dragRotation.x += evt.delta.x * 0.5f;
                _dragRotation.y -= evt.delta.y * 0.5f;
                _dragRotation.y = Mathf.Clamp(_dragRotation.y, 10f, 170f);
                evt.Use();
            }
            
            // 滚轮缩放
            if (evt.type == EventType.ScrollWheel)
            {
                _zoomLevel += evt.delta.y * ZOOM_SPEED;
                _zoomLevel = Mathf.Clamp(_zoomLevel, MIN_ZOOM, MAX_ZOOM);
                evt.Use();
            }
        }
    }
}
