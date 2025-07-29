using UnityEngine;
// using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 输入管理�?- 负责处理玩家输入
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        [Header("输入设置")]
        private bool enableLogging = false;
        private float inputThreshold = 0.1f;
        private int maxInputHistory = 10;
        
        // 输入状�?
        private InputState currentInput;
        private InputState previousInput;
        private List<InputState> inputHistory = new List<InputState>();
        
        // 输入动作资源 (注释掉InputSystem相关代码)
        // private InputActionAsset inputActions;
        // private InputAction moveAction;
        // private InputAction actionAction;
        // private InputAction pauseAction;
        
        // 公共属性
        public InputState CurrentInput => currentInput;
        public InputState PreviousInput => previousInput;
        public List<InputState> InputHistory => inputHistory;
        
        // 事件
        public event Action<InputState> OnInputChanged;
        public event Action<KeyCode> OnKeyPressed;
        public event Action<KeyCode> OnKeyReleased;
        public event Action<Vector2> OnMouseMoved;
        
        protected void Start()
        {
            // 初始化输入状态
            currentInput = new InputState();
            previousInput = new InputState();
        }
        
        /// <summary>
        /// 初始化输入管理器
        /// </summary>
        public void Initialize()
        {
            if (enableLogging)
                Debug.Log("InputManager: 初始化输入管理器");
            
            // 加载输入动作资源
            LoadInputActions();
            
            // 设置输入动作回调
            SetupInputCallbacks();
            
            if (enableLogging)
                Debug.Log("InputManager: 输入管理器初始化完成");
        }
        
        /// <summary>
        /// 加载输入动作资源
        /// </summary>
        private void LoadInputActions()
        {
            try
            {
                // InputSystem相关代码已注释，使用传统输入方式
                // inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
                
                // if (inputActions != null)
                // {
                //     moveAction = inputActions.FindAction("Player/Move");
                //     actionAction = inputActions.FindAction("Player/Action");
                //     pauseAction = inputActions.FindAction("UI/Pause");
                //     
                //     if (enableLogging)
                //         Debug.Log("InputManager: 输入动作资源加载成功");
                // }
                // else
                // {
                //     Debug.LogWarning("InputManager: 未找到输入动作资源，使用默认输入处理");
                // }
                
                if (enableLogging)
                    Debug.Log("InputManager: 使用传统输入处理方式");
            }
            catch (Exception ex)
            {
                Debug.LogError($"InputManager: 初始化输入处理失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置输入回调
        /// </summary>
        private void SetupInputCallbacks()
        {
            // InputSystem相关代码已注释
            // if (inputActions != null)
            // {
            //     inputActions.Enable();
            //     
            //     if (moveAction != null)
            //     {
            //         moveAction.performed += OnMovePerformed;
            //         moveAction.canceled += OnMoveCanceled;
            //     }
            //     
            //     if (actionAction != null)
            //     {
            //         actionAction.performed += OnActionPerformed;
            //         actionAction.canceled += OnActionCanceled;
            //     }
            //     
            //     if (pauseAction != null)
            //     {
            //         pauseAction.performed += OnPausePerformed;
            //     }
            // }
            
            if (enableLogging)
                Debug.Log("InputManager: 传统输入回调设置完成");
        }
        
        /// <summary>
        /// 更新输入管理�?
        /// </summary>
        public void Update()
        {
            // 保存上一帧的输入状�?
            if(currentInput != null)
                previousInput = currentInput.Clone();
            
            // 更新当前输入状�?
            UpdateInputState();
            
            // 检查输入变�?
            if (HasInputChanged())
            {
                // 添加到历史记�?
                AddToHistory(currentInput.Clone());
                
                // 触发输入变化事件
                OnInputChanged?.Invoke(currentInput);
            }
            
            // 处理传统输入（作为备用）
            ProcessLegacyInput();
        }
        
        /// <summary>
        /// 更新输入状�?
        /// </summary>
        private void UpdateInputState()
        {
            // 更新鼠标位置
            currentInput.MousePosition = Input.mousePosition;
            currentInput.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // 更新鼠标按钮状�?
            currentInput.MouseButtons[MouseButton.Left] = Input.GetMouseButton(0);
            currentInput.MouseButtons[MouseButton.Right] = Input.GetMouseButton(1);
            currentInput.MouseButtons[MouseButton.Middle] = Input.GetMouseButton(2);
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
        /// 获取输入增量
        /// </summary>
        /// <returns>输入状�?/returns>
        public InputState GetInputDelta()
        {
            InputState delta = new InputState();
            
            // 计算移动输入增量
            delta.MoveInput = currentInput.MoveInput - previousInput.MoveInput;
            
            // 计算鼠标位置增量
            delta.MousePosition = currentInput.MousePosition - previousInput.MousePosition;
            
            // 动作输入变化
            delta.IsActionPressed = currentInput.IsActionPressed && !previousInput.IsActionPressed;
            
            return delta;
        }
        
        /// <summary>
        /// 检查按键是否按�?
        /// </summary>
        /// <param name="keyCode">按键代码</param>
        /// <returns>是否按下</returns>
        public bool IsKeyPressed(KeyCode keyCode)
        {
            return currentInput.KeyStates.ContainsKey(keyCode) && currentInput.KeyStates[keyCode];
        }
        
        /// <summary>
        /// 检查按键是否持续按�?
        /// </summary>
        /// <param name="keyCode">按键代码</param>
        /// <returns>是否持续按下</returns>
        public bool IsKeyHeld(KeyCode keyCode)
        {
            return currentInput.KeyStates.ContainsKey(keyCode) && currentInput.KeyStates[keyCode] &&
                   previousInput.KeyStates.ContainsKey(keyCode) && previousInput.KeyStates[keyCode];
        }
        
        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        /// <returns>鼠标位置</returns>
        public Vector2 GetMousePosition()
        {
            return currentInput.MousePosition;
        }
        
        /// <summary>
        /// 获取移动输入
        /// </summary>
        /// <returns>移动输入向量</returns>
        public Vector2 GetMoveInput()
        {
            return currentInput.MoveInput;
        }
        
        /// <summary>
        /// 检查是否按下动作键
        /// </summary>
        /// <returns>是否按下动作�?/returns>
        public bool IsActionPressed()
        {
            return currentInput.IsActionPressed;
        }
        
        /// <summary>
        /// 移动输入回调
        /// </summary>
        private void OnMovePerformed(object context)
        {
            // InputSystem相关代码已注释
            // var movement = context.ReadValue<Vector2>();
            // currentInput.MoveVector = movement;
        }
        
        /// <summary>
        /// 移动输入取消回调
        /// </summary>
        private void OnMoveCanceled(object context)
        {
            currentInput.MoveInput = Vector2.zero;
            
            if (enableLogging)
                Debug.Log("InputManager: 移动输入取消");
        }
        
        /// <summary>
        /// 动作输入回调
        /// </summary>
        private void OnActionPerformed(object context)
        {
            currentInput.IsActionPressed = true;
            
            if (enableLogging)
                Debug.Log("InputManager: 动作输入");
        }
        
        /// <summary>
        /// 动作输入取消回调
        /// </summary>
        private void OnActionCanceled(object context)
        {
            currentInput.IsActionPressed = false;
            
            if (enableLogging)
                Debug.Log("InputManager: 动作输入取消");
        }
        
        /// <summary>
        /// 暂停输入回调
        /// </summary>
        private void OnPausePerformed(object context)
        {
            if (enableLogging)
                Debug.Log("InputManager: 暂停输入");
            
            // 触发暂停事件
            OnKeyPressed?.Invoke(KeyCode.Escape);
        }
        
        /// <summary>
        /// 清理输入历史
        /// </summary>
        public void ClearInputHistory()
        {
            inputHistory.Clear();
            
            if (enableLogging)
                Debug.Log("InputManager: 清理输入历史");
        }
        
        /// <summary>
        /// 关闭输入管理�?
        /// </summary>
        public void Shutdown()
        {
            if (enableLogging)
                Debug.Log("InputManager: 关闭输入管理器");
            
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
