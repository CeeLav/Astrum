using UnityEngine;
using System.Collections.Generic;

namespace Astrum.Client.Input
{
    /// <summary>
    /// 原始输入提供者接口
    /// 负责将物理输入（键盘、鼠标）转换为逻辑动作
    /// </summary>
    public interface IRawInputProvider
    {
        /// <summary>
        /// 获取按钮状态
        /// </summary>
        /// <param name="actionId">逻辑动作ID（如"Attack", "MoveForward"）</param>
        /// <returns>是否按下</returns>
        bool GetButton(string actionId);
        
        /// <summary>
        /// 获取轴值
        /// </summary>
        /// <param name="axisId">轴ID（如"MoveHorizontal", "MoveVertical"）</param>
        /// <returns>轴值 (-1.0 ~ 1.0)</returns>
        float GetAxis(string axisId);
        
        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        /// <returns>鼠标屏幕坐标</returns>
        Vector2 GetMousePosition();
        
        /// <summary>
        /// 设置启用的动作列表（用于输入上下文管理）
        /// </summary>
        /// <param name="enabledActions">启用的动作ID集合，null表示全部启用</param>
        void SetEnabledActions(HashSet<string> enabledActions);
    }
}




