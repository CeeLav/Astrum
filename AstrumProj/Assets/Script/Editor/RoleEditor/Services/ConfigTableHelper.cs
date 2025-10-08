using UnityEngine;
using UnityEditor;
using Astrum.LogicCore.Managers;
using Astrum.Editor.RoleEditor.Data;
using System.Collections.Generic;
using System.Linq;
using cfg;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 配置表查询工具类 - 封装ConfigManager的查询逻辑
    /// </summary>
    public static class ConfigTableHelper
    {
        private const string LOG_PREFIX = "[ConfigTableHelper]";
        
        /// <summary>
        /// 确保ConfigManager已初始化（编辑器模式，允许失败）
        /// </summary>
        public static bool EnsureConfigManagerInitialized()
        {
            if (!ConfigManager.Instance.IsInitialized)
            {
                try
                {
                    ConfigManager.Instance.Initialize("Config");
                    Debug.Log($"{LOG_PREFIX} ConfigManager initialized successfully");
                    return true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"{LOG_PREFIX} ConfigManager initialization failed (this is normal in editor mode): {ex.Message}");
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// 获取动作表数据
        /// </summary>
        public static cfg.Entity.ActionTable GetActionTable(int actionId)
        {
            if (actionId <= 0) return null;
            
            if (!EnsureConfigManagerInitialized())
            {
                Debug.LogWarning($"{LOG_PREFIX} ConfigManager not available, cannot get ActionTable for actionId {actionId}");
                return null;
            }
            
            try
            {
                return ConfigManager.Instance.Tables.TbActionTable.Get(actionId);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to get ActionTable for actionId {actionId}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取动画路径（通过ActionID）
        /// </summary>
        public static string GetAnimationPath(int actionId)
        {
            if (actionId <= 0) return string.Empty;
            
            var actionTable = GetActionTable(actionId);
            return actionTable?.AnimationName ?? string.Empty;
        }
        
        /// <summary>
        /// 获取动作名称
        /// </summary>
        public static string GetActionName(int actionId)
        {
            if (actionId <= 0) return "无";
            
            var actionTable = GetActionTable(actionId);
            return actionTable?.ActionName ?? "未命名动作";
        }
        
        /// <summary>
        /// 加载动画片段（通过ActionID）
        /// </summary>
        public static AnimationClip LoadAnimationClip(int actionId)
        {
            string animPath = GetAnimationPath(actionId);
            
            if (string.IsNullOrEmpty(animPath))
            {
                Debug.LogWarning($"{LOG_PREFIX} Animation path is empty for actionId {actionId}");
                return null;
            }
            
            var animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            
            if (animClip == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to load animation clip: {animPath}");
            }
            
            return animClip;
        }
        
        /// <summary>
        /// 获取角色的所有可用动作列表
        /// </summary>
        public static List<RoleActionInfo> GetAvailableActions(RoleEditorData role)
        {
            var result = new List<RoleActionInfo>();
            
            if (role == null) return result;
            
            // 收集所有有效的动作ID
            AddActionIfValid(result, "待机", role.IdleAction);
            AddActionIfValid(result, "行走", role.WalkAction);
            AddActionIfValid(result, "奔跑", role.RunAction);
            AddActionIfValid(result, "跳跃", role.JumpAction);
            AddActionIfValid(result, "出生", role.BirthAction);
            AddActionIfValid(result, "死亡", role.DeathAction);
            
            return result;
        }
        
        /// <summary>
        /// 添加动作信息（如果有效）
        /// </summary>
        private static void AddActionIfValid(List<RoleActionInfo> list, string label, int actionId)
        {
            if (actionId > 0)
            {
                var actionName = GetActionName(actionId);
                list.Add(new RoleActionInfo
                {
                    Label = label,
                    ActionId = actionId,
                    ActionName = actionName
                });
            }
        }
    }
    
    /// <summary>
    /// 角色动作信息
    /// </summary>
    public class RoleActionInfo
    {
        public string Label { get; set; }        // 显示标签（如"待机"）
        public int ActionId { get; set; }        // 动作ID
        public string ActionName { get; set; }   // 动作名称（从表读取）
        
        public override string ToString()
        {
            return $"{Label} [{ActionId}] - {ActionName}";
        }
    }
}

