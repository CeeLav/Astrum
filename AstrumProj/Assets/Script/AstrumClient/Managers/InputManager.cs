using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Generated;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 输入管理�?- 负责处理玩家输入
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        [Header("输入设置")]
        private float inputThreshold = 0.1f;
        private int maxInputHistory = 10;
        
        // 输入状�?
        private InputState currentInput;
        private InputState previousInput;
        private List<InputState> inputHistory = new List<InputState>();
        
        // 公共属性
        public InputState CurrentInput => currentInput;
        public InputState PreviousInput => previousInput;
        public List<InputState> InputHistory => inputHistory;
        
        // 事件
        public event Action<InputState> OnInputChanged;
        public event Action<KeyCode> OnKeyPressed;
        public event Action<KeyCode> OnKeyReleased;
        public event Action<Vector2> OnMouseMoved;
        
        /// <summary>
        /// 初始化输入管理器
        /// </summary>
        public void Initialize()
        {
            // 初始化输入状态
            currentInput = new InputState();
            previousInput = new InputState();
            ASLogger.Instance.Info("InputManager: 初始化输入管理器", "Input.Manager");
            
            ASLogger.Instance.Info("InputManager: 输入管理器初始化完成", "Input.Manager");
        }
        
        /// <summary>
        /// 更新输入管理器
        /// </summary>
        public void Update()
        {
            if (GamePlayManager.Instance.MainRoom == null)
            {
                return;
            }
            var input = CollectLSInput(GamePlayManager.Instance.PlayerId);
            GamePlayManager.Instance.MainRoom?.LSController.SetPlayerInput(GamePlayManager.Instance.PlayerId, input);
            /*
            // 保存上一帧的输入状态
            if(currentInput != null)
                previousInput = currentInput.Clone();
            
            // 更新当前输入状态
            UpdateInputState();
            ASLogger.Instance.Debug($"Current Input: {currentInput.MoveInput}, Mouse Position: {currentInput.MousePosition}", "Input.State");
            
            // 检查输入变化
            if (HasInputChanged())
            {
                // 添加到历史记录
                AddToHistory(currentInput.Clone());
                
                // 触发输入变化事件
                OnInputChanged?.Invoke(currentInput);
            }
            
            // 处理传统输入
            ProcessLegacyInput();
            */
        }
        
        /// <summary>
        /// 更新输入状态
        /// </summary>
        private void UpdateInputState()
        {
            // 更新鼠标位置 - 使用传统 Input 系统
            currentInput.MousePosition = Input.mousePosition;
            currentInput.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // 更新鼠标按钮状�?
            currentInput.MouseButtons[MouseButton.Left] = Input.GetMouseButton(0);
            currentInput.MouseButtons[MouseButton.Right] = Input.GetMouseButton(1);
            currentInput.MouseButtons[MouseButton.Middle] = Input.GetMouseButton(2);
            
            // 更新键盘状态
            UpdateKeyboardState();
            
            // 更新移动输入
            UpdateMoveInput();
        }
        
        /// <summary>
        /// 更新键盘状态 - 使用传统 Input 系统
        /// </summary>
        private void UpdateKeyboardState()
        {
            // 处理常用按键
            var keysToCheck = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space, KeyCode.Escape };
            
            foreach (var keyCode in keysToCheck)
            {
                bool currentState = Input.GetKey(keyCode);
                bool previousState = previousInput.KeyStates.ContainsKey(keyCode) ? previousInput.KeyStates[keyCode] : false;
                
                if (currentState != previousState)
                {
                    currentInput.KeyStates[keyCode] = currentState;
                    
                    if (currentState)
                    {
                        OnKeyPressed?.Invoke(keyCode);
                    }
                    else
                    {
                        OnKeyReleased?.Invoke(keyCode);
                    }
                }
            }
        }
        
        /// <summary>
        /// 更新移动输入
        /// </summary>
        private void UpdateMoveInput()
        {
            Vector2 moveInput = Vector2.zero;
            
            // 使用 WASD 键进行移动
            if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;
            if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
            
            // 标准化向量
            if (moveInput.magnitude > 1f)
            {
                moveInput.Normalize();
            }
            
            currentInput.MoveInput = moveInput;
            
            // 更新动作输入
            currentInput.IsActionPressed = Input.GetKey(KeyCode.Space);
        }
        
        /// <summary>
        /// 处理传统输入（备用方案）
        /// </summary>
        private void ProcessLegacyInput()
        {
            // 处理键盘输入
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                bool currentState = Input.GetKey(keyCode);
                bool previousState = previousInput.KeyStates.ContainsKey(keyCode) ? previousInput.KeyStates[keyCode] : false;
                
                if (currentState != previousState)
                {
                    currentInput.KeyStates[keyCode] = currentState;
                    
                    if (currentState)
                    {
                        OnKeyPressed?.Invoke(keyCode);
                    }
                    else
                    {
                        OnKeyReleased?.Invoke(keyCode);
                    }
                }
            }
            
            // 处理鼠标移动
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            if (mouseDelta.magnitude > inputThreshold)
            {
                OnMouseMoved?.Invoke(mouseDelta);
            }
        }
        
        /// <summary>
        /// 检查输入是否发生变�?
        /// </summary>
        /// <returns>是否发生变化</returns>
        private bool HasInputChanged()
        {
            // 检查移动输�?
            if (Vector2.Distance(currentInput.MoveInput, previousInput.MoveInput) > inputThreshold)
                return true;
            
            // 检查动作输�?
            if (currentInput.IsActionPressed != previousInput.IsActionPressed)
                return true;
            
            // 检查鼠标位�?
            if (Vector2.Distance(currentInput.MousePosition, previousInput.MousePosition) > 1f)
                return true;
            
            // 检查鼠标按�?
            foreach (var kvp in currentInput.MouseButtons)
            {
                if (previousInput.MouseButtons.TryGetValue(kvp.Key, out bool previousState))
                {
                    if (kvp.Value != previousState)
                        return true;
                }
                else if (kvp.Value)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 添加到输入历�?
        /// </summary>
        /// <param name="inputState">输入状�?/param>
        private void AddToHistory(InputState inputState)
        {
            inputHistory.Add(inputState);
            
            // 限制历史记录数量
            while (inputHistory.Count > maxInputHistory)
            {
                inputHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 关闭输入管理�?
        /// </summary>
        public void Shutdown()
        {
            ASLogger.Instance.Info("InputManager: 关闭输入管理器", "Input.Manager");
            
            // InputSystem相关代码已注释
            // if (inputActions != null)
            // {
            //     inputActions.Disable();
            // }
            
            // 清理输入历史
            inputHistory.Clear();
            
            // 清理事件
            OnInputChanged = null;
            OnKeyPressed = null;
            OnKeyReleased = null;
            OnMouseMoved = null;
        }
        
        /// <summary>
        /// 收集并转化为帧同步输入（LSInput）
        /// </summary>
        public LSInput CollectLSInput(long playerId)
        {
            var input = new LSInput();
            input.PlayerId = playerId;
            //input.Frame = frame;
            input.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // 移动输入
            input.MoveX = 0f;
            input.MoveY = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.W)) input.MoveY += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) input.MoveY -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) input.MoveX -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) input.MoveX += 1f;
            // 攻击输入
            input.Attack = UnityEngine.Input.GetKey(KeyCode.Space) || UnityEngine.Input.GetMouseButton(0);
            // 技能1输入
            input.Skill1 = UnityEngine.Input.GetKey(KeyCode.Q);
            // 技能2输入
            input.Skill2 = UnityEngine.Input.GetKey(KeyCode.E);
            ASLogger.Instance.Debug($"LSInput: MainPlayerID:{playerId} MoveX={input.MoveX}, MoveY={input.MoveY}, Attack={input.Attack}, Skill1={input.Skill1}, Skill2={input.Skill2}", "Input.LSInput");
            return input;
        }
    }
    
    /// <summary>
    /// 输入状�?
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
            
            // 复制按键状�?
            foreach (var kvp in KeyStates)
            {
                clone.KeyStates[kvp.Key] = kvp.Value;
            }
            
            // 复制鼠标状�?
            clone.MousePosition = MousePosition;
            foreach (var kvp in MouseButtons)
            {
                clone.MouseButtons[kvp.Key] = kvp.Value;
            }
            
            // 复制其他状�?
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
