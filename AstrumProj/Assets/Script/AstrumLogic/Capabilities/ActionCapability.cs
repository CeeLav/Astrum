using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.FrameSync;
using Astrum.CommonBase;
using System.Collections.Generic;
using System.Reflection;
using cfg;
using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 动作能力 - 管理单个实体的动作系统
    /// </summary>
    [MemoryPackable]
    public partial class ActionCapability : Capability
    {
        
        /// <summary>
        /// 初始化动作能力
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            
            // 检查动作组件是否存在
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.Initialize: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return;
            }
            
            // 预加载所有可用的ActionInfo
            LoadAvailableActions();
        }
        
        /// <summary>
        /// 每帧更新
        /// </summary>
        public override void Tick()
        {
            if (!CanExecute()) return;
            // 1. 更新当前动作
            UpdateCurrentAction();
            
            // 2. 检查所有动作的取消条件
            CheckActionCancellation();
            
            // 3. 从候选列表选择动作
            SelectActionFromCandidates();
            
        }
        
        /// <summary>
        /// 加载所有可用动作
        /// </summary>
        private void LoadAvailableActions()
        {
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.LoadAvailableActions: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return;
            }
            
            actionComponent.AvailableActions.Clear();
            
            // 获取所有可用的动作ID（这里需要根据实际需求实现）
            var availableActionIds = GetAvailableActionIds();
            
            foreach (var actionId in availableActionIds)
            {
                var actionInfo = ActionConfigManager.Instance?.GetAction(actionId, OwnerEntity?.UniqueId ?? 0);
                if (actionInfo != null)
                {
                    actionComponent.AvailableActions.Add(actionInfo);
                }
            }
            
            var initialAction = GetActionInfo(availableActionIds[0]);
            actionComponent.CurrentAction = initialAction;
            
            if (initialAction != null)
            {
                ASLogger.Instance.Info($"ActionCapability.LoadAvailableActions: Set initial action ActionId={initialAction.Id} " +
                    $"on entity {OwnerEntity?.UniqueId}");
            }
        }
        
        /// <summary>
        /// 获取可用动作ID列表
        /// </summary>
        private List<int> GetAvailableActionIds()
        {
            var config = OwnerEntity.EntityConfig;
            var list = new List<int>();
            list.Add(config.IdleAction);
            list.Add(config.WalkAction);
            // TODO: 根据实际需求实现
            // 例如：从配置、技能系统、装备等获取
            return list;
        }
        
        /// <summary>
        /// 检查动作取消条件
        /// </summary>
        private void CheckActionCancellation()
        {
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.CheckActionCancellation: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return;
            }
            if (actionComponent.CurrentAction == null) return;
            
            // 清空预订单列表
            actionComponent.PreorderActions.Clear();
            
            // 检查当前动作是否已结束
            var actionDuration = GetActionDuration(actionComponent.CurrentAction);
            if (IsActionFinished(actionDuration))
            {
                // 添加默认下一个动作到预订单列表
                var nextActionId = actionComponent.CurrentAction.AutoNextActionId;
                if (nextActionId > 0)
                {
                    // 从可用动作列表中查找下一个动作的数据
                    var nextAction = actionComponent.AvailableActions.Find(a => a.Id == nextActionId);
                    if (nextAction != null)
                    {
                        actionComponent.PreorderActions.Add(new PreorderActionInfo
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
            // 检查其他动作的取消条件
            var availableActions = GetAvailableActions();
            
            foreach (var action in availableActions)
            {
                if (CanCancelToAction(action))
                {
                    if (HasValidCommand(action))
                    {
                        actionComponent.PreorderActions.Add(new PreorderActionInfo
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
        
        /// <summary>
        /// 从预订单列表选择动作
        /// </summary>
        private void SelectActionFromCandidates()
        {
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.SelectActionFromCandidates: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return;
            }
            if (actionComponent.PreorderActions == null || actionComponent.PreorderActions.Count == 0) return;
            
            // 按优先级排序
            actionComponent.PreorderActions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            // 选择优先级最高的动作
            var selectedAction = actionComponent.PreorderActions[0];
            var actionInfo = ActionConfigManager.Instance?.GetAction(selectedAction.ActionId, OwnerEntity?.UniqueId ?? 0);
            
            if (actionInfo != null)
            {
                ASLogger.Instance.Info($"ActionCapability.SelectActionFromCandidates: Selected action ActionId={selectedAction.ActionId} " +
                    $"with Priority={selectedAction.Priority} from {actionComponent.PreorderActions.Count} candidates on entity {OwnerEntity?.UniqueId}");
                
                // 切换到新动作
                SwitchToAction(actionInfo, selectedAction);
            }
            else
            {
                ASLogger.Instance.Warning($"ActionCapability.SelectActionFromCandidates: Failed to get ActionInfo for ActionId={selectedAction.ActionId} " +
                    $"on entity {OwnerEntity?.UniqueId}");
            }
            
            // 清空候选列表
            actionComponent.PreorderActions.Clear();
        }
        
        /// <summary>
        /// 切换到指定动作
        /// </summary>
        private void SwitchToAction(ActionInfo actionInfo, PreorderActionInfo preorderInfo)
        {
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.SwitchToAction: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return;
            }
            
            // 记录切换前的动作信息
            var previousActionId = actionComponent.CurrentAction?.Id ?? 0;
            var previousFrame = actionComponent.CurrentFrame;
            
            actionComponent.CurrentAction = actionInfo;
            actionComponent.CurrentFrame = preorderInfo.FromFrame;
            
            // 记录切换成功的日志
            ASLogger.Instance.Info($"ActionCapability.SwitchToAction: Successfully switched action on entity {OwnerEntity?.UniqueId} " +
                $"from ActionId={previousActionId}(Frame={previousFrame}) to ActionId={actionInfo.Id}(Frame={preorderInfo.FromFrame}) " +
                $"[Priority={preorderInfo.Priority}, TransitionFrames={preorderInfo.TransitionFrames}]");
        }
        
        /// <summary>
        /// 更新当前动作
        /// </summary>
        private void UpdateCurrentAction()
        {
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.UpdateCurrentAction: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return;
            }
            if (actionComponent.CurrentAction == null) return;
            
            // 按帧更新动作进度
            actionComponent.CurrentFrame += 1;
        }
        
        /// <summary>
        /// 检查动作是否已结束
        /// </summary>
        private bool IsActionFinished(int actionDuration)
        {
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.IsActionFinished: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return false;
            }
            if (actionComponent.CurrentAction == null) return false;
            
            return actionComponent.CurrentFrame >= actionDuration;
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
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.GetAvailableActions: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return new List<ActionInfo>();
            }
            return actionComponent.AvailableActions ?? new List<ActionInfo>();
        }
        
        /// <summary>
        /// 获取动作持续时间
        /// </summary>
        private int GetActionDuration(ActionInfo actionInfo)
        {
            if (actionInfo == null)
            {
                ASLogger.Instance.Error($"ActionCapability.GetActionDuration: ActionInfo is null on entity {OwnerEntity?.UniqueId}");
                return 0;
            }
            return actionInfo.Duration;
        }
        
        /// <summary>
        /// 检查是否可以取消到指定动作
        /// </summary>
        private bool CanCancelToAction(ActionInfo targetAction)
        {
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.CanCancelToAction: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return false;
            }
            if (actionComponent.CurrentAction == null || targetAction == null) return false;
            
            // 检查CancelTag是否匹配BeCancelledTag
            foreach (var cancelTag in targetAction.CancelTags)
            {
                foreach (var beCancelledTag in actionComponent.CurrentAction.BeCancelledTags)
                {
                    if (beCancelledTag.Tags.Contains(cancelTag.Tag))
                    {
                        // 检查时间范围
                        if (beCancelledTag.RangeFrames.Count >= 2 && 
                            IsInTimeRange(beCancelledTag.RangeFrames[0], beCancelledTag.RangeFrames[1]))
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
        private bool IsInTimeRange(int startFrame, int endFrame)
        {
            var actionComponent = GetOwnerComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.IsInTimeRange: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
                return false;
            }
            if (actionComponent.CurrentAction == null) return false;
            
            // 直接按帧数判断
            return actionComponent.CurrentFrame >= startFrame && actionComponent.CurrentFrame <= endFrame;
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
            // 获取输入组件
            var inputComponent = GetOwnerComponent<LSInputComponent>();
            if (inputComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.CheckCommandMatch: LSInputComponent not found on entity {OwnerEntity?.UniqueId}");
                return false;
            }
            if (inputComponent.CurrentInput == null) return false;
            
            var currentInput = inputComponent.CurrentInput;
            //ASLogger.Instance.Warning($"CheckCommandMatch: Command={commandName}, Input=[MoveX={currentInput.MoveX}, MoveY={currentInput.MoveY}, Attack={currentInput.Attack}, Skill1={currentInput.Skill1}, Skill2={currentInput.Skill2}]");
            // 根据命令名称匹配输入
            return commandName.ToLower() switch
            {
                "move" => currentInput.MoveX != 0 || currentInput.MoveY != 0,
                "attack" or "normalattack" => currentInput.Attack,
                "skill1" => currentInput.Skill1,
                "skill2" => currentInput.Skill2,
                _ => false
            };
        }
        
    }
}
