using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.LogicCore.Managers;
using cfg.Input;


namespace Astrum.Client.Input
{
    /// <summary>
    /// 基于配置表的输入提供者
    /// 从 InputBindingTable.csv 读取按键配置，将物理输入转换为逻辑动作
    /// </summary>
    public class ConfigurableInputProvider : IRawInputProvider
    {
        private Dictionary<string, InputBindingTable> _bindings;
        private HashSet<string> _enabledActions;
        
        /// <summary>
        /// 构造函数，自动加载配置表
        /// </summary>
        public ConfigurableInputProvider()
        {
            LoadBindings();
            _enabledActions = null; // null表示全部启用
        }
        
        /// <summary>
        /// 从配置表加载按键绑定
        /// </summary>
        private void LoadBindings()
        {
            _bindings = new Dictionary<string, InputBindingTable>();
            
            try
            {
                // 从配置表加载
                var configManager = TableConfig.Instance;
                if (!configManager.IsInitialized || configManager.Tables?.TbInputBindingTable == null)
                {
                    ASLogger.Instance.Error("ConfigurableInputProvider: TbInputBindingTable is null", "Input.Provider");
                    return;
                }
                
                foreach (var binding in configManager.Tables.TbInputBindingTable.DataList)
                {
                    if (binding != null && !string.IsNullOrEmpty(binding.ActionId))
                    {
                        _bindings[binding.ActionId] = binding;
                    }
                }
                
                ASLogger.Instance.Info($"ConfigurableInputProvider: 加载了 {_bindings.Count} 个输入绑定", "Input.Provider");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ConfigurableInputProvider: 加载输入绑定失败 - {ex.Message}", "Input.Provider");
            }
        }
        
        /// <summary>
        /// 获取按钮状态
        /// </summary>
        public bool GetButton(string actionId)
        {
            // 检查是否启用
            if (_enabledActions != null && !_enabledActions.Contains(actionId))
                return false;
                
            if (!_bindings.TryGetValue(actionId, out var binding))
                return false;
            
            // 检查主按键
            if (!string.IsNullOrEmpty(binding.PrimaryKey) && binding.PrimaryKey != "None")
            {
                if (Enum.TryParse<KeyCode>(binding.PrimaryKey, out var key))
                {
                    if (UnityEngine.Input.GetKey(key))
                        return true;
                }
            }
            
            // 检查备用按键
            if (!string.IsNullOrEmpty(binding.AlternativeKey) && binding.AlternativeKey != "None")
            {
                if (Enum.TryParse<KeyCode>(binding.AlternativeKey, out var key))
                {
                    if (UnityEngine.Input.GetKey(key))
                        return true;
                }
            }
            
            // 检查鼠标按键
            if (binding.MouseButton >= 0 && binding.MouseButton <= 2)
            {
                if (UnityEngine.Input.GetMouseButton(binding.MouseButton))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取轴值（通过组合按钮计算）
        /// </summary>
        public float GetAxis(string axisId)
        {
            float value = 0f;
            
            // 根据轴ID查找对应的正负按键
            switch (axisId)
            {
                case "MoveHorizontal":
                    if (GetButton("MoveLeft")) value -= 1f;
                    if (GetButton("MoveRight")) value += 1f;
                    break;
                    
                case "MoveVertical":
                    if (GetButton("MoveBackward")) value -= 1f;
                    if (GetButton("MoveForward")) value += 1f;
                    break;
                    
                default:
                    ASLogger.Instance.Warning($"ConfigurableInputProvider: 未知的轴ID '{axisId}'", "Input.Provider");
                    break;
            }
            
            return Mathf.Clamp(value, -1f, 1f);
        }
        
        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        public Vector2 GetMousePosition()
        {
            return UnityEngine.Input.mousePosition;
        }
        
        /// <summary>
        /// 设置启用的动作列表（用于输入上下文）
        /// </summary>
        public void SetEnabledActions(HashSet<string> enabledActions)
        {
            _enabledActions = enabledActions;
            
            if (enabledActions == null)
            {
                ASLogger.Instance.Debug("ConfigurableInputProvider: 启用所有输入动作", "Input.Provider");
            }
            else
            {
                ASLogger.Instance.Debug($"ConfigurableInputProvider: 启用 {enabledActions.Count} 个输入动作", "Input.Provider");
            }
        }
    }
}

