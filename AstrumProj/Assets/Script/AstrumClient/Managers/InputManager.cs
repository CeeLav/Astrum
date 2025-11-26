using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Client.Core;
using Astrum.Client.Input;
using Astrum.Client.Managers.GameModes;
using Astrum.View.Core;
using Astrum.LogicCore.Core;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 输入管理器 - 负责调度输入采集和LSInput组装
    /// 重构为配置驱动，使用IRawInputProvider和LSInputAssembler
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        private IRawInputProvider _inputProvider;
        
        // ====== 旧代码保留（未使用，便于回退） ======
        [Header("输入设置")]
        private float inputThreshold = 0.1f;
        private int maxInputHistory = 10;
        
        // 输入状态（未使用）
        private InputState currentInput;
        private InputState previousInput;
        private List<InputState> inputHistory = new List<InputState>();
        
        // 公共属性（未使用）
        public InputState CurrentInput => currentInput;
        public InputState PreviousInput => previousInput;
        public List<InputState> InputHistory => inputHistory;
        
        // 事件（未使用）
        public event Action<InputState> OnInputChanged;
        public event Action<KeyCode> OnKeyPressed;
        public event Action<KeyCode> OnKeyReleased;
        public event Action<Vector2> OnMouseMoved;
        // ====== 旧代码结束 ======
        
        /// <summary>
        /// 初始化输入管理器
        /// </summary>
        public void Initialize()
        {
            // 创建配置驱动的输入提供者
            _inputProvider = new ConfigurableInputProvider();
            
            // 初始化LSInput组装器
            LSInputAssembler.Initialize();
            
            ASLogger.Instance.Info("InputManager: 输入管理器初始化完成（配置驱动模式）", "Input.Manager");
            
            // 旧代码保留
            currentInput = new InputState();
            previousInput = new InputState();
        }
        
        /// <summary>
        /// 更新输入管理器
        /// </summary>
        public void Update()
        {
            var currentGameMode = GameDirector.Instance?.CurrentGameMode;
            if (currentGameMode?.MainRoom == null || currentGameMode is ReplayGameMode)
            {
                return;
            }
            
            Transform playerTransform = null;
            var stage = currentGameMode.MainStage;
            if (stage != null && currentGameMode.PlayerId > 0 && stage.EntityViews != null &&
                stage.EntityViews.TryGetValue(currentGameMode.PlayerId, out var entityView))
            {
                playerTransform = entityView.Transform;
            }
            
            // 新逻辑：使用LSInputAssembler组装输入
            var input = LSInputAssembler.AssembleFromRawInput(
                _inputProvider,
                currentGameMode.PlayerId,
                CameraManager.Instance?.MainCamera,
                playerTransform
            );
            
            if (currentGameMode.MainRoom.LSController is ClientLSController clientSync)
            {
                clientSync.SetPlayerInput(currentGameMode.PlayerId, input);
            }
        }

        
        /// <summary>
        /// 关闭输入管理器
        /// </summary>
        public void Shutdown()
        {
            ASLogger.Instance.Info("InputManager: 关闭输入管理器", "Input.Manager");
            
            _inputProvider = null;
            
            // 清理输入历史
            inputHistory.Clear();
            
            // 清理事件
            OnInputChanged = null;
            OnKeyPressed = null;
            OnKeyReleased = null;
            OnMouseMoved = null;
        }
        
        // ====== 旧代码保留（便于回退） ======
        /*
        /// <summary>
        /// 收集并转化为帧同步输入（LSInput）- 旧实现
        /// </summary>
        public LSInput CollectLSInput(long playerId)
        {
            var input = new LSInput();
            input.PlayerId = playerId;
            
            // 获取相机方向相关的移动输入
            Vector2 cameraRelativeMove = GetCameraRelativeMoveInput();
            
            // 转 Q31.32 定点数
            long ToQ32(float v) => (long)(v * (double)(1L << 32));
            input.MoveX = ToQ32(cameraRelativeMove.x);
            input.MoveY = ToQ32(cameraRelativeMove.y);
            
            // 攻击输入
            input.Attack = UnityEngine.Input.GetKey(KeyCode.Space) || UnityEngine.Input.GetMouseButton(0);
            // 技能1输入
            input.Skill1 = UnityEngine.Input.GetKey(KeyCode.Q);
            // 技能2输入
            input.Skill2 = UnityEngine.Input.GetKey(KeyCode.E);
            
            ASLogger.Instance.Debug($"LSInput: MainPlayerID:{playerId} MoveX={cameraRelativeMove.x:F2}, MoveY={cameraRelativeMove.y:F2}, Attack={input.Attack}, Skill1={input.Skill1}, Skill2={input.Skill2}", "Input.LSInput");
            return input;
        }
        
        /// <summary>
        /// 获取考虑相机方向的移动输入 - 旧实现
        /// </summary>
        private Vector2 GetCameraRelativeMoveInput()
        {
            // 获取原始WASD输入
            Vector2 rawInput = Vector2.zero;
            if (UnityEngine.Input.GetKey(KeyCode.W)) rawInput.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) rawInput.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) rawInput.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) rawInput.x += 1f;
            
            // 如果没有输入，直接返回
            if (rawInput.magnitude < 0.1f)
                return Vector2.zero;
            
            // 归一化输入
            rawInput.Normalize();
            
            // 获取相机方向
            Vector3 cameraForward = GetCameraForward();
            Vector3 cameraRight = GetCameraRight();
            
            // 将输入转换为相机相对坐标
            Vector3 worldMove = cameraForward * rawInput.y + cameraRight * rawInput.x;
            
            // 返回XZ平面的移动向量（忽略Y轴）
            return new Vector2(worldMove.x, worldMove.z);
        }
        
        /// <summary>
        /// 获取相机前方向量（忽略Y轴）- 旧实现
        /// </summary>
        private Vector3 GetCameraForward()
        {
            if (CameraManager.Instance?.MainCamera != null)
            {
                Vector3 forward = CameraManager.Instance.MainCamera.transform.forward;
                // 忽略Y轴，只保留XZ平面的方向
                forward.y = 0f;
                return forward.normalized;
            }
            
            // 如果没有相机，返回默认前方向量
            return Vector3.forward;
        }
        
        /// <summary>
        /// 获取相机右方向量（忽略Y轴）- 旧实现
        /// </summary>
        private Vector3 GetCameraRight()
        {
            if (CameraManager.Instance?.MainCamera != null)
            {
                Vector3 right = CameraManager.Instance.MainCamera.transform.right;
                // 忽略Y轴，只保留XZ平面的方向
                right.y = 0f;
                return right.normalized;
            }
            
            // 如果没有相机，返回默认右方向量
            return Vector3.right;
        }
        */
        // ====== 旧代码结束 ======
    }
    
    /// <summary>
    /// 输入状态（未使用，保留便于回退）
    /// </summary>
    [Serializable]
    public class InputState
    {
        public Dictionary<KeyCode, bool> KeyStates { get; set; } = new Dictionary<KeyCode, bool>();
        public Vector2 MousePosition { get; set; }
        public Dictionary<MouseButton, bool> MouseButtons { get; set; } = new Dictionary<MouseButton, bool>();
        public Vector2 MoveInput { get; set; }
        public bool IsActionPressed { get; set; }
        public long Timestamp { get; set; }
        
        public InputState Clone()
        {
            InputState clone = new InputState();
            
            // 复制按键状态
            foreach (var kvp in KeyStates)
            {
                clone.KeyStates[kvp.Key] = kvp.Value;
            }
            
            // 复制鼠标状态
            clone.MousePosition = MousePosition;
            foreach (var kvp in MouseButtons)
            {
                clone.MouseButtons[kvp.Key] = kvp.Value;
            }
            
            // 复制其他状态
            clone.MoveInput = MoveInput;
            clone.IsActionPressed = IsActionPressed;
            clone.Timestamp = Timestamp;
            
            return clone;
        }
    }
    
    /// <summary>
    /// 鼠标按钮枚举
    /// </summary>
    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }
}
