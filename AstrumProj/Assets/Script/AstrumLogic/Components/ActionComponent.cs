using Astrum.LogicCore.ActionSystem;
using MemoryPack;
using System.Collections.Generic;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 动作组件 - 存储实体的动作状态
    /// </summary>
    [MemoryPackable]
    public partial class ActionComponent : BaseComponent
    {
        /// <summary>当前动作信息</summary>
        public ActionInfo? CurrentAction { get; set; }
        
        /// <summary>当前动作进度（0.0-1.0）</summary>
        public float CurrentActionProgress { get; set; } = 0.0f;
        
        /// <summary>当前动作帧</summary>
        public int CurrentFrame { get; set; } = 0;
        
        /// <summary>输入命令缓存</summary>
        [MemoryPackAllowSerialize]
        public List<ActionCommand> InputCommands { get; set; } = new();
        
        /// <summary>预订单动作列表</summary>
        [MemoryPackAllowSerialize]
        public List<PreorderActionInfo> PreorderActions { get; set; } = new();
        
        /// <summary>可用动作列表</summary>
        [MemoryPackAllowSerialize]
        public List<ActionInfo> AvailableActions { get; set; } = new();
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ActionComponent()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public ActionComponent(int componentId, long entityId, ActionInfo? currentAction, float currentActionProgress, 
            int currentFrame, List<ActionCommand> inputCommands, List<PreorderActionInfo> preorderActions, 
            List<ActionInfo> availableActions)
        {
            ComponentId = componentId;
            EntityId = entityId;
            CurrentAction = currentAction;
            CurrentActionProgress = currentActionProgress;
            CurrentFrame = currentFrame;
            InputCommands = inputCommands ?? new List<ActionCommand>();
            PreorderActions = preorderActions ?? new List<PreorderActionInfo>();
            AvailableActions = availableActions ?? new List<ActionInfo>();
        }
        
        /// <summary>是否正在执行动作</summary>
        public bool IsExecutingAction => CurrentAction != null;
        
        /// <summary>动作是否已结束（需要外部提供动作持续时间）</summary>
        public bool IsActionFinished(int actionDuration) => CurrentAction != null && CurrentFrame >= actionDuration;
    }
}
