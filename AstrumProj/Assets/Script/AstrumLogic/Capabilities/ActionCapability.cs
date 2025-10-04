using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
using System.Collections.Generic;
using cfg;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 动作能力 - 管理单个实体的动作系统
    /// </summary>
    public class ActionCapability : Capability
    {
        /// <summary>动作组件</summary>
        private ActionComponent? _actionComponent;
        
        /// <summary>
        /// 初始化动作能力
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            
            // 获取或创建动作组件
            _actionComponent = OwnerEntity?.GetComponent<ActionComponent>();
            if (_actionComponent == null)
            {
                _actionComponent = new ActionComponent();
                OwnerEntity?.AddComponent(_actionComponent);
            }
            
            // 初始化配置管理器
            ActionConfigManager.Initialize();
            
            // 预加载所有可用的ActionInfo
            LoadAvailableActions();
        }
        
        /// <summary>
        /// 每帧更新
        /// </summary>
        public override void Tick()
        {
            if (!CanExecute()) return;
            
            // 1. 检查所有动作的取消条件
            CheckActionCancellation();
            
            // 2. 从候选列表选择动作
            SelectActionFromCandidates();
            
            // 3. 更新当前动作
            UpdateCurrentAction();
        }
        
        /// <summary>
        /// 加载所有可用动作
        /// </summary>
        private void LoadAvailableActions()
        {
            if (_actionComponent == null) return;
            
            _actionComponent.AvailableActions.Clear();
            
            // 获取所有可用的动作ID（这里需要根据实际需求实现）
            var availableActionIds = GetAvailableActionIds();
            
            foreach (var actionId in availableActionIds)
            {
                var actionInfo = ActionConfigManager.Instance?.GetAction(actionId, OwnerEntity?.UniqueId ?? 0);
                if (actionInfo != null)
                {
                    _actionComponent.AvailableActions.Add(actionInfo);
                }
            }
        }
        
        /// <summary>
        /// 获取可用动作ID列表
        /// </summary>
        private List<int> GetAvailableActionIds()
        {
            // TODO: 根据实际需求实现
            // 例如：从配置、技能系统、装备等获取
            return new List<int>();
        }
        
        /// <summary>
        /// 检查动作取消条件
        /// </summary>
        private void CheckActionCancellation()
        {
            if (_actionComponent?.CurrentAction == null) return;
            
            // 清空预订单列表
            _actionComponent.PreorderActions.Clear();
            
            // 检查当前动作是否已结束
            var actionDuration = GetActionDuration();
            if (IsActionFinished(actionDuration))
            {
                // 添加默认下一个动作到预订单列表
                var nextActionId = _actionComponent.CurrentAction.AutoNextActionId;
                if (nextActionId > 0)
                {
                    var nextAction = GetActionInfo(nextActionId);
                    if (nextAction != null)
                    {
                        _actionComponent.PreorderActions.Add(new PreorderActionInfo
                        {
                            ActionId = nextActionId,
                            Priority = nextAction.Priority,
                            TransitionFrames = 0,
                            FromFrame = 0,
                            FreezingFrames = 0
                        });
                    }
                }
            }
            else
            {
                // 检查其他动作的取消条件
                var availableActions = GetAvailableActions();
                
                foreach (var action in availableActions)
                {
                    if (CanCancelToAction(action))
                    {
                        if (HasValidCommand(action))
                        {
                            _actionComponent.PreorderActions.Add(new PreorderActionInfo
                            {
                                ActionId = action.Id,
                                Priority = CalculatePriority(action),
                                TransitionFrames = 3,
                                FromFrame = 0,
                                FreezingFrames = 0
                            });
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 从预订单列表选择动作
        /// </summary>
        private void SelectActionFromCandidates()
        {
            if (_actionComponent?.PreorderActions == null || _actionComponent.PreorderActions.Count == 0) return;
            
            // 按优先级排序
            _actionComponent.PreorderActions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            // 选择优先级最高的动作
            var selectedAction = _actionComponent.PreorderActions[0];
            var actionInfo = ActionConfigManager.Instance?.GetAction(selectedAction.ActionId, OwnerEntity?.UniqueId ?? 0);
            
            if (actionInfo != null)
            {
                // 切换到新动作
                SwitchToAction(actionInfo, selectedAction);
            }
            
            // 清空候选列表
            _actionComponent.PreorderActions.Clear();
        }
        
        /// <summary>
        /// 切换到指定动作
        /// </summary>
        private void SwitchToAction(ActionInfo actionInfo, PreorderActionInfo preorderInfo)
        {
            if (_actionComponent == null) return;
            
            _actionComponent.CurrentAction = actionInfo;
            // TODO: 根据实际帧率计算动作进度
            _actionComponent.CurrentActionProgress = 0.0f;
            _actionComponent.CurrentFrame = preorderInfo.FromFrame;
        }
        
        /// <summary>
        /// 更新当前动作
        /// </summary>
        private void UpdateCurrentAction()
        {
            if (_actionComponent?.CurrentAction == null) return;
            
            // 按帧更新动作进度
            _actionComponent.CurrentFrame += 1;
            var actionDuration = GetActionDuration();
            if (actionDuration > 0)
            {
                _actionComponent.CurrentActionProgress = (float)_actionComponent.CurrentFrame / actionDuration;
            }
        }
        
        /// <summary>
        /// 检查动作是否已结束
        /// </summary>
        private bool IsActionFinished(int actionDuration)
        {
            if (_actionComponent?.CurrentAction == null) return false;
            
            return _actionComponent.CurrentFrame >= actionDuration;
        }
        
        /// <summary>
        /// 获取动作信息
        /// </summary>
        private ActionInfo? GetActionInfo(int actionId)
        {
            return ActionConfigManager.Instance?.GetAction(actionId, OwnerEntity?.UniqueId ?? 0);
        }
        
        /// <summary>
        /// 获取可用动作列表
        /// </summary>
        private List<ActionInfo> GetAvailableActions()
        {
            // 从ActionComponent获取所有可用的ActionInfo
            return _actionComponent?.AvailableActions ?? new List<ActionInfo>();
        }
        
        /// <summary>
        /// 获取动作持续时间
        /// </summary>
        private int GetActionDuration()
        {
            // TODO: 根据实际的动作系统实现
            // 应该从ActionInfo或配置中获取动作的帧数
            return 0;
        }
        
        /// <summary>
        /// 检查是否可以取消到指定动作
        /// </summary>
        private bool CanCancelToAction(ActionInfo targetAction)
        {
            if (_actionComponent?.CurrentAction == null || targetAction == null) return false;
            
            // 检查CancelTag是否匹配BeCancelledTag
            foreach (var cancelTag in targetAction.CancelTags)
            {
                foreach (var beCancelledTag in _actionComponent.CurrentAction.BeCancelledTags)
                {
                    if (beCancelledTag.Tags.Contains(cancelTag.Tag))
                    {
                        // 检查时间范围
                        if (IsInTimeRange(beCancelledTag.Range))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查是否有有效命令
        /// </summary>
        private bool HasValidCommand(ActionInfo actionInfo)
        {
            foreach (var command in actionInfo.Commands)
            {
                if (IsCommandValid(command))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 计算动作优先级
        /// </summary>
        private int CalculatePriority(ActionInfo actionInfo)
        {
            return actionInfo.Priority;
        }
        
        /// <summary>
        /// 检查是否在时间范围内
        /// </summary>
        private bool IsInTimeRange(vector2 range)
        {
            if (_actionComponent?.CurrentAction == null) return false;
            
            return _actionComponent.CurrentActionProgress >= range.X && _actionComponent.CurrentActionProgress <= range.Y;
        }
        
        /// <summary>
        /// 检查命令是否有效
        /// </summary>
        private bool IsCommandValid(ActionCommand command)
        {
            // 检查命令名称是否匹配
            return CheckCommandMatch(command.CommandName);
        }
        
        /// <summary>
        /// 检查命令是否匹配
        /// </summary>
        private bool CheckCommandMatch(string commandName)
        {
            // TODO: 实现命令匹配逻辑
            // 应该检查输入命令是否匹配命令名称
            return false;
        }
        
        /// <summary>
        /// 执行动作
        /// </summary>
        public void ExecuteAction(int actionId)
        {
            var actionInfo = ActionConfigManager.Instance?.GetAction(actionId, OwnerEntity?.UniqueId ?? 0);
            if (actionInfo != null)
            {
                var preorderInfo = new PreorderActionInfo
                {
                    ActionId = actionId,
                    Priority = actionInfo.Priority,
                    TransitionFrames = 3,
                    FromFrame = 0,
                    FreezingFrames = 0
                };
                
                SwitchToAction(actionInfo, preorderInfo);
            }
        }
        
        /// <summary>
        /// 检查是否可以执行动作
        /// </summary>
        public bool CanExecuteAction(int actionId)
        {
            var actionInfo = ActionConfigManager.Instance?.GetAction(actionId, OwnerEntity?.UniqueId ?? 0);
            return actionInfo != null && CanCancelToAction(actionInfo);
        }
    }
}
