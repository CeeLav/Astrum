using UnityEngine;
using System;
using Astrum.CommonBase;

namespace Astrum.View.Managers
{
    /// <summary>
    /// 摄像机模式枚�?
    /// </summary>
    public enum CameraMode
    {
        FREE_LOOK,      // 自由视角
        FOLLOW,         // 跟随模式
        FIXED,          // 固定视角
        CINEMATIC       // 电影模式
    }
    
    /// <summary>
    /// 摄像机控制器
    /// 负责控制摄像机的移动、旋转和模式切换
    /// </summary>
    public class CameraController
    {
        // 目标摄像�?
        private Camera _targetCamera;
        private Transform _cameraTransform;
        
        // 跟随目标
        private Transform _followTarget;
        
        // 摄像机模�?
        private CameraMode _cameraMode = CameraMode.FREE_LOOK;
        
        // 移动和旋转速度
        private float _movementSpeed = 5f;
        private float _rotationSpeed = 2f;
        
        // 跟随模式参数
        private Vector3 _followOffset = new Vector3(0, 5, -10);
        private float _followSmoothness = 5f;
        
        // 自由视角参数
        private float _mouseSensitivity = 2f;
        private float _pitch = 0f;
        private float _yaw = 0f;
        
        // 状态管�?
        private bool _isInitialized = false;
        
        // 公共属�?
        public Camera TargetCamera => _targetCamera;
        public Transform FollowTarget => _followTarget;
        public CameraMode CameraMode => _cameraMode;
        public float MovementSpeed => _movementSpeed;
        public float RotationSpeed => _rotationSpeed;
        public bool IsInitialized => _isInitialized;
        
        // 事件
        public event Action<CameraController> OnCameraControllerInitialized;
        public event Action<CameraController> OnCameraControllerShutdown;
        public event Action<CameraMode, CameraMode> OnCameraModeChanged;
        
        /// <summary>
        /// 初始化摄像机控制�?
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                ASLogger.Instance.Warning("CameraController: 已经初始化，跳过重复初始化");
                return;
            }
            
            ASLogger.Instance.Info("CameraController: 初始化开�?..");
            
            try
            {
                _isInitialized = true;
                OnCameraControllerInitialized?.Invoke(this);
                
                ASLogger.Instance.Info("CameraController: 初始化完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"CameraController: 初始化失�?- {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新摄像机控制器
        /// </summary>
        public void Update()
        {
            if (!_isInitialized || _targetCamera == null) return;
            
            try
            {
                switch (_cameraMode)
                {
                    case CameraMode.FREE_LOOK:
                        UpdateFreeLook();
                        break;
                    case CameraMode.FOLLOW:
                        UpdateFollow();
                        break;
                    case CameraMode.FIXED:
                        UpdateFixed();
                        break;
                    case CameraMode.CINEMATIC:
                        UpdateCinematic();
                        break;
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"CameraController: 更新失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置目标摄像�?
        /// </summary>
        /// <param name="camera">目标摄像�?/param>
        public void SetTargetCamera(Camera camera)
        {
            if (camera == null)
            {
                ASLogger.Instance.Warning("CameraController: 尝试设置空的摄像机");
                return;
            }
            
            _targetCamera = camera;
            _cameraTransform = camera.transform;
            
            // 初始化角�?
            if (_cameraTransform != null)
            {
                _yaw = _cameraTransform.eulerAngles.y;
                _pitch = _cameraTransform.eulerAngles.x;
            }
            
            ASLogger.Instance.Info($"CameraController: 设置目标摄像�?- {camera.name}");
        }
        
        /// <summary>
        /// 设置跟随目标
        /// </summary>
        /// <param name="target">跟随目标</param>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            
            if (target != null)
            {
                ASLogger.Instance.Info($"CameraController: 设置跟随目标 - {target.name}");
            }
            else
            {
                ASLogger.Instance.Info("CameraController: 清除跟随目标");
            }
        }
        
        /// <summary>
        /// 切换摄像机模�?
        /// </summary>
        /// <param name="mode">摄像机模�?/param>
        public void SwitchCameraMode(CameraMode mode)
        {
            if (_cameraMode == mode) return;
            
            CameraMode oldMode = _cameraMode;
            _cameraMode = mode;
            
            OnCameraModeChanged?.Invoke(oldMode, _cameraMode);
            ASLogger.Instance.Info($"CameraController: 切换摄像机模�?- {oldMode} -> {_cameraMode}");
        }
        
        /// <summary>
        /// 更新摄像机位�?
        /// </summary>
        public void UpdateCameraPosition()
        {
            if (_cameraTransform == null) return;
            
            // 根据当前模式更新位置
            switch (_cameraMode)
            {
                case CameraMode.FOLLOW:
                    UpdateFollowPosition();
                    break;
                default:
                    // 其他模式的位置更新在各自的Update方法中处�?
                    break;
            }
        }
        
        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="speed">移动速度</param>
        public void SetMovementSpeed(float speed)
        {
            _movementSpeed = Mathf.Max(0, speed);
        }
        
        /// <summary>
        /// 设置旋转速度
        /// </summary>
        /// <param name="speed">旋转速度</param>
        public void SetRotationSpeed(float speed)
        {
            _rotationSpeed = Mathf.Max(0, speed);
        }
        
        /// <summary>
        /// 设置跟随偏移
        /// </summary>
        /// <param name="offset">跟随偏移</param>
        public void SetFollowOffset(Vector3 offset)
        {
            _followOffset = offset;
        }
        
        /// <summary>
        /// 设置跟随平滑�?
        /// </summary>
        /// <param name="smoothness">平滑�?/param>
        public void SetFollowSmoothness(float smoothness)
        {
            _followSmoothness = Mathf.Max(0.1f, smoothness);
        }
        
        /// <summary>
        /// 设置鼠标灵敏�?
        /// </summary>
        /// <param name="sensitivity">鼠标灵敏�?/param>
        public void SetMouseSensitivity(float sensitivity)
        {
            _mouseSensitivity = Mathf.Max(0.1f, sensitivity);
        }
        
        /// <summary>
        /// 关闭摄像机控制器
        /// </summary>
        public void Shutdown()
        {
            ASLogger.Instance.Info("CameraController: 开始关�?..");
            
            _targetCamera = null;
            _cameraTransform = null;
            _followTarget = null;
            _isInitialized = false;
            
            OnCameraControllerShutdown?.Invoke(this);
            
            ASLogger.Instance.Info("CameraController: 关闭完成");
        }
        
        /// <summary>
        /// 更新自由视角模式
        /// </summary>
        private void UpdateFreeLook()
        {
            if (_cameraTransform == null) return;
            
            // 处理鼠标输入
            if (Input.GetMouseButton(1)) // 右键按下时才能旋转
            {
                float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;
                
                _yaw += mouseX;
                _pitch -= mouseY;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
                
                _cameraTransform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
            }
            
            // 处理键盘移动
            Vector3 movement = Vector3.zero;
            
            if (Input.GetKey(KeyCode.W))
                movement += _cameraTransform.forward;
            if (Input.GetKey(KeyCode.S))
                movement -= _cameraTransform.forward;
            if (Input.GetKey(KeyCode.A))
                movement -= _cameraTransform.right;
            if (Input.GetKey(KeyCode.D))
                movement += _cameraTransform.right;
            if (Input.GetKey(KeyCode.Q))
                movement += Vector3.up;
            if (Input.GetKey(KeyCode.E))
                movement += Vector3.down;
            
            if (movement.magnitude > 0)
            {
                movement.Normalize();
                _cameraTransform.position += movement * _movementSpeed * Time.deltaTime;
            }
        }
        
        /// <summary>
        /// 更新跟随模式
        /// </summary>
        private void UpdateFollow()
        {
            if (_followTarget == null || _cameraTransform == null) return;
            
            UpdateFollowPosition();
        }
        
        /// <summary>
        /// 更新跟随位置
        /// </summary>
        private void UpdateFollowPosition()
        {
            if (_followTarget == null || _cameraTransform == null) return;
            
            // 计算目标位置
            Vector3 targetPosition = _followTarget.position + _followOffset;
            
            // 平滑移动
            _cameraTransform.position = Vector3.Lerp(
                _cameraTransform.position, 
                targetPosition, 
                _followSmoothness * Time.deltaTime
            );
            
            // 看向目标
            Vector3 direction = _followTarget.position - _cameraTransform.position;
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _cameraTransform.rotation = Quaternion.Slerp(
                    _cameraTransform.rotation, 
                    targetRotation, 
                    _followSmoothness * Time.deltaTime
                );
            }
        }
        
        /// <summary>
        /// 更新固定模式
        /// </summary>
        private void UpdateFixed()
        {
            // 固定模式下摄像机不移�?
            // 可以在这里添加一些固定的动画效果
        }
        
        /// <summary>
        /// 更新电影模式
        /// </summary>
        private void UpdateCinematic()
        {
            // 电影模式下可以播放预设的摄像机动�?
            // 这里可以添加电影摄像机路径或动画系统
        }
        
        /// <summary>
        /// 获取摄像机状态信�?
        /// </summary>
        /// <returns>状态信�?/returns>
        public string GetCameraStatus()
        {
            if (_targetCamera == null)
                return "摄像机: 未设置";
            
            return $"摄像�? {_targetCamera.name}, 模式: {_cameraMode}, 位置: {_cameraTransform?.position}";
        }
    }
} 
