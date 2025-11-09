using UnityEngine;
using System;
using Astrum.CommonBase;
using Astrum.View.Core;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 相机管理器 - 负责管理游戏主相机的控制
    /// 实现第三人称俯视角MOBA风格的相机控制
    /// </summary>
    public class CameraManager : Singleton<CameraManager>
    {
        [Header("相机设置")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Transform _cameraTransform;
        
        [Header("跟随设置")]
        [SerializeField] private Transform _followTarget; // 跟随目标（玩家角色）
        [SerializeField] private float _followSmoothness = 5f; // 跟随平滑度
        [SerializeField] private float _lookAtSmoothness = 3f; // 看向目标平滑度
        
        [Header("球面控制")]
        [SerializeField] private float _distance = 15f; // 相机距离
        [SerializeField] private float _minDistance = 5f; // 最小距离
        [SerializeField] private float _maxDistance = 30f; // 最大距离
        [SerializeField] private float _mouseSensitivity = 2f; // 鼠标灵敏度
        [SerializeField] private float _scrollSensitivity = 2f; // 滚轮灵敏度
        
        // 内部状态
        private bool _isInitialized = false;
        private Vector3 _lastMousePosition;
        private float _currentDistance;
        private float _currentYaw = 0f; // 水平角度
        private float _currentPitch = 30f; // 垂直角度（俯视角）
        
        // 输入状态
        private bool _isRightMouseDown = false;
        
        // 公共属性
        public Camera MainCamera => _mainCamera;
        public Transform FollowTarget => _followTarget;
        public new bool IsInitialized => _isInitialized;
        public float CurrentDistance => _currentDistance;
        
        // 事件
        public event Action<CameraManager> OnCameraManagerInitialized;
        public event Action<CameraManager> OnCameraManagerShutdown;
        public event Action<Transform> OnFollowTargetChanged;
        
        /// <summary>
        /// 初始化相机管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                ASLogger.Instance.Warning("CameraManager: 已经初始化，跳过重复初始化");
                return;
            }
            
            ASLogger.Instance.Info("CameraManager: 初始化开始...");
            
            try
            {
                // 获取主相机
                SetupMainCamera();
                
                // 初始化相机状态
                InitializeCameraState();
                
                _isInitialized = true;
                OnCameraManagerInitialized?.Invoke(this);
                
                ASLogger.Instance.Info("CameraManager: 初始化完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"CameraManager: 初始化失败 - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新相机管理器
        /// </summary>
        public void Update()
        {
            if (!_isInitialized || _mainCamera == null) return;
            
            try
            {
                // 处理输入
                HandleInput();
                
                // 更新相机位置和旋转
                UpdateCamera();
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"CameraManager: 更新失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置跟随目标
        /// </summary>
        /// <param name="target">跟随目标Transform</param>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            
            if (target != null)
            {
                ASLogger.Instance.Info($"CameraManager: 设置跟随目标 - {target.name}");
                
                // 立即调整相机位置到目标附近
                if (_mainCamera != null)
                {
                    Vector3 targetPosition = CalculateSpherePosition();
                    _mainCamera.transform.position = targetPosition;
                    _mainCamera.transform.LookAt(target.position);
                }
            }
            else
            {
                ASLogger.Instance.Info("CameraManager: 清除跟随目标");
            }
            
            OnFollowTargetChanged?.Invoke(target);
        }
        
        
        /// <summary>
        /// 设置跟随平滑度
        /// </summary>
        /// <param name="smoothness">平滑度</param>
        public void SetFollowSmoothness(float smoothness)
        {
            _followSmoothness = Mathf.Max(0.1f, smoothness);
        }
        
        /// <summary>
        /// 设置鼠标灵敏度
        /// </summary>
        /// <param name="sensitivity">灵敏度</param>
        public void SetMouseSensitivity(float sensitivity)
        {
            _mouseSensitivity = Mathf.Max(0.1f, sensitivity);
        }
        
        /// <summary>
        /// 设置相机距离
        /// </summary>
        /// <param name="distance">距离</param>
        public void SetCameraDistance(float distance)
        {
            _currentDistance = Mathf.Clamp(distance, _minDistance, _maxDistance);
        }
        
        /// <summary>
        /// 重置相机到默认位置
        /// </summary>
        public void ResetCamera()
        {
            if (_followTarget != null)
            {
                _currentDistance = _distance;
                _currentYaw = 0f;
                _currentPitch = 30f;
                
                ASLogger.Instance.Info("CameraManager: 重置相机到默认位置");
            }
        }
        
        /// <summary>
        /// 关闭相机管理器
        /// </summary>
        public void Shutdown()
        {
            ASLogger.Instance.Info("CameraManager: 开始关闭...");
            
            _mainCamera = null;
            _cameraTransform = null;
            _followTarget = null;
            _isInitialized = false;
            
            OnCameraManagerShutdown?.Invoke(this);
            
            ASLogger.Instance.Info("CameraManager: 关闭完成");
        }
        
        /// <summary>
        /// 设置主相机
        /// </summary>
        private void SetupMainCamera()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                // 如果没有主相机，创建一个
                GameObject cameraObj = new GameObject("MainCamera");
                _mainCamera = cameraObj.AddComponent<Camera>();
                _mainCamera.tag = "MainCamera";
                
                ASLogger.Instance.Info("CameraManager: 创建主相机");
            }
            else
            {
                ASLogger.Instance.Info($"CameraManager: 找到主相机 - {_mainCamera.name}");
            }
            
            _cameraTransform = _mainCamera.transform;
        }
        
        /// <summary>
        /// 初始化相机状态
        /// </summary>
        private void InitializeCameraState()
        {
            _currentDistance = _distance;
            _currentYaw = 0f;
            _currentPitch = 30f; // 默认俯视角
        }
        
        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            // 右键拖拽旋转相机
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                _isRightMouseDown = true;
                _lastMousePosition = UnityEngine.Input.mousePosition;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(1))
            {
                _isRightMouseDown = false;
            }
            
            // 滚轮缩放距离
            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentDistance -= scroll * _scrollSensitivity;
                _currentDistance = Mathf.Clamp(_currentDistance, _minDistance, _maxDistance);
            }
            
            // 右键拖拽旋转
            if (_isRightMouseDown)
            {
                Vector3 mouseDelta = UnityEngine.Input.mousePosition - _lastMousePosition;
                
                // 水平旋转（绕Y轴）
                _currentYaw += mouseDelta.x * _mouseSensitivity * Time.deltaTime;
                
                // 垂直旋转（绕X轴）
                _currentPitch -= mouseDelta.y * _mouseSensitivity * Time.deltaTime;
                _currentPitch = Mathf.Clamp(_currentPitch, 10f, 80f); // 限制在10-80度之间
                
                _lastMousePosition = UnityEngine.Input.mousePosition;
            }
        }
        
        /// <summary>
        /// 更新相机
        /// </summary>
        private void UpdateCamera()
        {
            if (_followTarget == null) return;
            
            // 计算球面位置
            Vector3 targetPosition = CalculateSpherePosition();
            
            // 平滑移动相机
            _cameraTransform.position = Vector3.Lerp(
                _cameraTransform.position,
                targetPosition,
                _followSmoothness * Time.deltaTime
            );
            
            // 让相机看向目标
            Vector3 lookDirection = (_followTarget.position - _cameraTransform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            
            // 平滑旋转相机
            _cameraTransform.rotation = Quaternion.Slerp(
                _cameraTransform.rotation,
                targetRotation,
                _lookAtSmoothness * Time.deltaTime
            );
        }
        
        /// <summary>
        /// 计算球面位置
        /// </summary>
        /// <returns>球面位置</returns>
        private Vector3 CalculateSpherePosition()
        {
            Vector3 basePosition = _followTarget.position;
            
            // 将角度转换为弧度
            float yawRad = _currentYaw * Mathf.Deg2Rad;
            float pitchRad = _currentPitch * Mathf.Deg2Rad;
            
            // 计算球面坐标
            float x = _currentDistance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad);
            float y = _currentDistance * Mathf.Sin(pitchRad);
            float z = _currentDistance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad);
            
            Vector3 offset = new Vector3(x, y, z);
            return basePosition + offset;
        }
        
        
        /// <summary>
        /// 获取相机状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public string GetCameraStatus()
        {
            if (_mainCamera == null)
                return "相机: 未设置";
            
            return $"相机: {_mainCamera.name}, 跟随目标: {(_followTarget?.name ?? "无")}, 距离: {_currentDistance:F1}, 角度: ({_currentYaw:F1}°, {_currentPitch:F1}°)";
        }
    }
}
