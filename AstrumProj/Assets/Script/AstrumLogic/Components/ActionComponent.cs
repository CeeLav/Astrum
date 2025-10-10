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
        /// <summary>当前动作ID（序列化字段）</summary>
        public int CurrentActionId { get; set; } = 0;
        
        /// <summary>当前动作信息（访问器，不序列化）</summary>
        [MemoryPackIgnore]
        public ActionInfo? CurrentAction
        {
            get => CurrentActionId > 0 && AvailableActions.ContainsKey(CurrentActionId) 
                ? AvailableActions[CurrentActionId] 
                : null;
            set => CurrentActionId = value?.Id ?? 0;
        }
        
        /// <summary>当前动作帧</summary>
        public int CurrentFrame { get; set; } = 0;
        
        /// <summary>输入命令缓存</summary>
        [MemoryPackAllowSerialize]
        public List<ActionCommand> InputCommands { get; set; } = new();
        
        /// <summary>预订单动作列表</summary>
        [MemoryPackAllowSerialize]
        public List<PreorderActionInfo> PreorderActions { get; set; } = new();
        
        /// <summary>可用动作字典（Key: ActionId, Value: ActionInfo）</summary>
        [MemoryPackAllowSerialize]
        public Dictionary<int, ActionInfo> AvailableActions { get; set; } = new();
        
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
        public ActionComponent(int componentId, long entityId, int currentActionId, 
            int currentFrame, List<ActionCommand> inputCommands, List<PreorderActionInfo> preorderActions, 
            Dictionary<int, ActionInfo> availableActions)
        {
            ComponentId = componentId;
            EntityId = entityId;
            CurrentActionId = currentActionId;
            CurrentFrame = currentFrame;
            InputCommands = inputCommands ?? new List<ActionCommand>();
            PreorderActions = preorderActions ?? new List<PreorderActionInfo>();
            AvailableActions = availableActions ?? new Dictionary<int, ActionInfo>();
        }
        
        /// <summary>是否正在执行动作</summary>
        public bool IsExecutingAction => CurrentAction != null;
        
        /// <summary>动作是否已结束（需要外部提供动作持续时间）</summary>
        public bool IsActionFinished(int actionDuration) => CurrentAction != null && CurrentFrame >= actionDuration;
    }
}
