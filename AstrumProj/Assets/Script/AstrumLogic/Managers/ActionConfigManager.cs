using Astrum.LogicCore.ActionSystem;
using Astrum.CommonBase;
using cfg;
using System.Collections.Generic;
using System.Linq;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 动作配置管理器 - 工厂类，用于组装ActionInfo（单例）
    /// </summary>
    public class ActionConfigManager : Singleton<ActionConfigManager>
    {
        /// <summary>单例实例</summary>
        public new static ActionConfigManager Instance { get; private set; }
        
        /// <summary>动作表格数据缓存</summary>
        private Dictionary<int, ActionTableData> _actionTableData = new();
        
        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        public static void Initialize()
        {
            if (Instance == null)
            {
                Instance = new ActionConfigManager();
            }
            Instance.LoadActionTableData();
        }
        
        /// <summary>
        /// 加载动作表格数据
        /// </summary>
        private void LoadActionTableData()
        {
            // 从Luban表格加载动作配置数据
            // TODO: 暂时不引用Luban表格，后续实现
            // 例如：_actionTableData = Tables.Action.DataList.ToDictionary(x => x.Id);
        }
        
        /// <summary>
        /// 获取动作信息（工厂方法）
        /// </summary>
        /// <param name="actionId">动作ID</param>
        /// <param name="entityId">实体ID</param>
        /// <returns>组装后的ActionInfo</returns>
        public ActionInfo? GetAction(int actionId, long entityId)
        {
            // 从表格获取基础数据
            if (!_actionTableData.TryGetValue(actionId, out var tableData))
            {
                return null;
            }
            
            // 组装ActionInfo
            var actionInfo = new ActionInfo
            {
                Id = actionId,
                Catalog = tableData.Catalog,
                Priority = tableData.Priority,
                AutoNextActionId = tableData.AutoNextActionId,
                KeepPlayingAnim = tableData.KeepPlayingAnim,
                AutoTerminate = tableData.AutoTerminate
            };
            
            // 从表格数据组装CancelTags
            actionInfo.CancelTags = tableData.CancelTags?.Select(ct => new CancelTag
            {
                Tag = ct.Tag,
                StartFrom = ct.StartFrom,
                BlendInFrames = ct.BlendInFrames,
                Priority = ct.Priority
            }).ToList() ?? new List<CancelTag>();
            
            // 从表格数据组装BeCancelledTags
            actionInfo.BeCancelledTags = tableData.BeCancelledTags?.Select(bt => new BeCancelledTag
            {
                Tags = bt.Tags,
                Range = new vector2(),
                BlendOutFrames = bt.BlendOutFrames,
                Priority = bt.Priority
            }).ToList() ?? new List<BeCancelledTag>();
            
            // 从表格数据组装TempBeCancelledTags
            actionInfo.TempBeCancelledTags = tableData.TempBeCancelledTags?.Select(tt => new TempBeCancelledTag
            {
                Id = tt.Id,
                Tags = tt.Tags,
                DurationFrames = tt.DurationFrames,
                BlendOutFrames = tt.BlendOutFrames,
                Priority = tt.Priority
            }).ToList() ?? new List<TempBeCancelledTag>();
            
            // 从表格数据组装Commands
            actionInfo.Commands = tableData.Commands?.Select(cmd => new ActionCommand
            {
                CommandName = cmd.CommandName,
                ValidFrames = cmd.ValidFrames
            }).ToList() ?? new List<ActionCommand>();
            
            // TODO: 根据entityId获取运行时数据，与表格数据合并
            // 例如：从实体的状态、buff等获取额外的动作修改
            
            return actionInfo;
        }
    }
    
    /// <summary>
    /// 动作表格数据结构（临时定义，后续由Luban生成）
    /// </summary>
    public class ActionTableData
    {
        public int Id { get; set; }
        public string Catalog { get; set; } = string.Empty;
        public int Priority { get; set; }
        public int AutoNextActionId { get; set; }
        public bool KeepPlayingAnim { get; set; }
        public bool AutoTerminate { get; set; }
        public List<CancelTagTableData>? CancelTags { get; set; }
        public List<BeCancelledTagTableData>? BeCancelledTags { get; set; }
        public List<TempBeCancelledTagTableData>? TempBeCancelledTags { get; set; }
        public List<ActionCommandTableData>? Commands { get; set; }
    }
    
    /// <summary>
    /// 取消标签表格数据
    /// </summary>
    public class CancelTagTableData
    {
        public string Tag { get; set; } = string.Empty;
        public float StartFrom { get; set; }
        public int BlendInFrames { get; set; }
        public int Priority { get; set; }
    }
    
    /// <summary>
    /// 被取消标签表格数据
    /// </summary>
    public class BeCancelledTagTableData
    {
        public List<string> Tags { get; set; } = new();
        public Vector2TableData Range { get; set; } = new();
        public int BlendOutFrames { get; set; }
        public int Priority { get; set; }
    }
    
    /// <summary>
    /// 临时被取消标签表格数据
    /// </summary>
    public class TempBeCancelledTagTableData
    {
        public string Id { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public int DurationFrames { get; set; }
        public int BlendOutFrames { get; set; }
        public int Priority { get; set; }
    }
    
    /// <summary>
    /// 动作命令表格数据
    /// </summary>
    public class ActionCommandTableData
    {
        public string CommandName { get; set; } = string.Empty;
        public int ValidFrames { get; set; }
    }
    
    /// <summary>
    /// 二维向量表格数据
    /// </summary>
    public class Vector2TableData
    {
        public float X { get; set; }
        public float Y { get; set; }
    }
}
