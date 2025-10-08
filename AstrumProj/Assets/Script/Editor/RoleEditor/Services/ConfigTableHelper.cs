using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using System.Collections.Generic;
using System.Linq;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 配置表查询工具类 - 直接读取CSV文件，不依赖ConfigManager
    /// </summary>
    public static class ConfigTableHelper
    {
        private const string LOG_PREFIX = "[ConfigTableHelper]";
        
        // 缓存数据
        private static List<EntityTableData> _entityTableCache;
        private static List<RoleTableData> _roleTableCache;
        private static List<ActionTableData> _actionTableCache;
        
        /// <summary>
        /// 获取动作表数据（从CSV读取）
        /// </summary>
        public static ActionTableData GetActionTable(int actionId)
        {
            if (actionId <= 0) return null;
            
            LoadActionTableIfNeeded();
            
            return _actionTableCache?.FirstOrDefault(x => x.Id == actionId);
        }
        
        /// <summary>
        /// 获取动画路径（通过ActionID）
        /// </summary>
        public static string GetAnimationPath(int actionId)
        {
            var actionTable = GetActionTable(actionId);
            if (actionTable == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} Animation path is empty for actionId {actionId}");
                return string.Empty;
            }
            return actionTable.AnimationPath ?? string.Empty;
        }
        
        /// <summary>
        /// 加载动画片段
        /// </summary>
        public static AnimationClip LoadAnimationClip(int actionId)
        {
            string animationPath = GetAnimationPath(actionId);
            if (string.IsNullOrEmpty(animationPath))
            {
                Debug.LogWarning($"{LOG_PREFIX} Animation path is empty for actionId {actionId}");
                return null;
            }
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath);
            if (clip == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to load animation clip at path: {animationPath}");
            }
            
            return clip;
        }
        
        /// <summary>
        /// 获取动作名称
        /// </summary>
        public static string GetActionName(int actionId)
        {
            var actionTable = GetActionTable(actionId);
            return actionTable?.Name ?? $"Action_{actionId}";
        }
        
        /// <summary>
        /// 获取角色可用的动作列表
        /// </summary>
        public static List<RoleActionInfo> GetAvailableActions(RoleEditorData roleData)
        {
            var actions = new List<RoleActionInfo>();
            
            if (roleData == null) return actions;
            
            // 添加默认动作
            AddActionIfValid(actions, "静止动作", roleData.IdleAction);
            AddActionIfValid(actions, "跳跃动作", roleData.JumpAction);
            AddActionIfValid(actions, "行走动作", roleData.WalkAction);
            
            return actions;
        }
        
        /// <summary>
        /// 添加动作到列表（如果有效）
        /// </summary>
        private static void AddActionIfValid(List<RoleActionInfo> actions, string actionName, int actionId)
        {
            if (actionId <= 0) return;
            
            var actionTable = GetActionTable(actionId);
            if (actionTable != null)
            {
                actions.Add(new RoleActionInfo
                {
                    ActionId = actionId,
                    ActionName = actionTable.Name ?? actionName,
                    AnimationPath = actionTable.AnimationPath
                });
            }
            else
            {
                // 即使找不到ActionTable，也添加基本信息
                actions.Add(new RoleActionInfo
                {
                    ActionId = actionId,
                    ActionName = actionName,
                    AnimationPath = string.Empty
                });
            }
        }
        
        /// <summary>
        /// 加载ActionTable数据（如果需要）
        /// </summary>
        private static void LoadActionTableIfNeeded()
        {
            if (_actionTableCache != null) return;
            
            try
            {
                var config = new LubanTableConfig
                {
                    FilePath = "AstrumConfig/Tables/Datas/Entity/#ActionTable.csv",
                    HeaderLines = 4,
                    HasEmptyFirstColumn = true,
                    Header = new TableHeader 
                    { 
                        VarNames = new List<string> { "actionId", "actionName", "actionType", "duration", "AnimationName" },
                        Types = new List<string> { "int", "string", "string", "int", "string" },
                        Groups = new List<string> { "", "", "", "", "" },
                        Descriptions = new List<string> { "动作ID", "动作名称", "动作类型", "持续时间", "动画名称" }
                    }
                };
                
                _actionTableCache = LubanCSVReader.ReadTable<ActionTableData>(config);
                Debug.Log($"{LOG_PREFIX} Loaded {_actionTableCache.Count} action records from CSV");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to load ActionTable: {ex.Message}");
                _actionTableCache = new List<ActionTableData>();
            }
        }
        
        /// <summary>
        /// 清除缓存（用于重新加载数据）
        /// </summary>
        public static void ClearCache()
        {
            _entityTableCache = null;
            _roleTableCache = null;
            _actionTableCache = null;
            Debug.Log($"{LOG_PREFIX} Cache cleared");
        }
    }
    
    /// <summary>
    /// 角色动作信息
    /// </summary>
    public class RoleActionInfo
    {
        public int ActionId { get; set; }
        public string ActionName { get; set; }
        public string AnimationPath { get; set; }
    }
    
    /// <summary>
    /// ActionTable数据映射
    /// </summary>
    public class ActionTableData
    {
        [TableField(1, "actionId")]
        public int Id { get; set; }
        
        [TableField(2, "actionName")]
        public string Name { get; set; }
        
        [TableField(5, "AnimationName")]
        public string AnimationPath { get; set; }
        
        [TableField(3, "actionType")]
        public string ActionType { get; set; }
    }
}