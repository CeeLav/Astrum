using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using System.Collections.Generic;
using System.Linq;
using cfg;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 动作能力（新架构，基于 Capability&lt;T&gt;）
    /// 管理单个实体的动作系统
    /// </summary>
    public class ActionCapability : Capability<ActionCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 200; // 动作系统优先级较高
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.Control, 
            CapabilityTag.Animation 
        };
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            
            // 检查动作组件是否存在
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.OnAttached: ActionComponent not found on entity {entity.UniqueId}");
                return;
            }
            
            // 预加载所有可用的ActionInfo
            LoadAvailableActions(entity);
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 检查必需组件是否存在
            return base.ShouldActivate(entity) &&
                   HasComponent<ActionComponent>(entity);
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 缺少任何必需组件则停用
            return base.ShouldDeactivate(entity) ||
                   !HasComponent<ActionComponent>(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            // 1. 更新当前动作
            UpdateCurrentAction(entity);
            
            // 2. 检查所有动作的取消条件
            CheckActionCancellation(entity);
            
            // 3. 从候选列表选择动作
            SelectActionFromCandidates(entity);
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 加载所有可用动作
        /// </summary>
        private void LoadAvailableActions(Entity entity)
        {
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.LoadAvailableActions: ActionComponent not found on entity {entity.UniqueId}");
                return;
            }
            
            // 获取所有可用的动作ID
            var availableActionIds = GetAvailableActionIds(entity);
            
            foreach (var actionId in availableActionIds)
            {
                var actionInfo = ActionConfig.Instance?.GetAction(actionId, entity.UniqueId);
                if (actionInfo != null)
                {
                    actionComponent.AvailableActions[actionId] = actionInfo;
                }
            }
            
            // 设置初始动作ID
            if (actionComponent.AvailableActions.Count > 0 && availableActionIds.Count > 0)
            {
                actionComponent.CurrentActionId = availableActionIds[0];
                ASLogger.Instance.Debug($"ActionCapability.LoadAvailableActions: Set initial action ActionId={actionComponent.CurrentActionId} " +
                    $"on entity {entity.UniqueId}");
            }
            else
            {
                ASLogger.Instance.Warning($"ActionCapability.LoadAvailableActions: No available actions found for entity {entity.UniqueId}");
            }
        }
        
        /// <summary>
        /// 获取可用动作ID列表
        /// </summary>
        private List<int> GetAvailableActionIds(Entity entity)
        {
            var config = entity.EntityConfig;
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
        private void CheckActionCancellation(Entity entity)
        {
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.CheckActionCancellation: ActionComponent not found on entity {entity.UniqueId}");
                return;
            }
            if (actionComponent.CurrentAction == null) return;
            
            // 清空预订单列表
            actionComponent.PreorderActions.Clear();
            
            // 检查当前动作是否已结束
            var actionDuration = GetActionDuration(actionComponent.CurrentAction);
            
            var shouldTerminate = 
                (actionComponent.CurrentAction.AutoTerminate && !HasValidCommand(entity, actionComponent.CurrentAction));
            if (actionComponent.CurrentAction.AutoTerminate)
            {
                ASLogger.Instance.Debug($"shouldTerminate: {shouldTerminate}, ActionId={actionComponent.CurrentAction.Id}, CurrentFrame={actionComponent.CurrentFrame}, Duration={actionDuration}, AutoTerminate={actionComponent.CurrentAction.AutoTerminate}, " +
                                       $"HasValidCommand={HasValidCommand(entity, actionComponent.CurrentAction)} on entity {entity.UniqueId}");
            }

            var shouldContinue = !IsActionFinished(actionComponent, actionDuration) || HasValidCommand(entity, actionComponent.CurrentAction);
            if (!shouldContinue || shouldTerminate)
            {
                // 添加默认下一个动作到预订单列表
                var nextActionId = actionComponent.CurrentAction.AutoNextActionId;
                if (nextActionId > 0)
                {
                    // 从可用动作字典中查找下一个动作的数据
                    if (actionComponent.AvailableActions.TryGetValue(nextActionId, out var nextAction))
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
            var availableActions = GetAvailableActions(actionComponent);
            
            foreach (var action in availableActions)
            {
                if (CanCancelToAction(actionComponent, action))
                {
                    if (HasValidCommand(entity, action))
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
        private void SelectActionFromCandidates(Entity entity)
        {
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.SelectActionFromCandidates: ActionComponent not found on entity {entity.UniqueId}");
                return;
            }
            if (actionComponent.PreorderActions == null || actionComponent.PreorderActions.Count == 0) return;
            
            // 按优先级排序
            actionComponent.PreorderActions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            // 选择优先级最高的动作
            var selectedAction = actionComponent.PreorderActions[0];
            
            // 从 AvailableActions 字典中查找
            if (actionComponent.AvailableActions.TryGetValue(selectedAction.ActionId, out var actionInfo))
            {
                ASLogger.Instance.Debug($"ActionCapability.SelectActionFromCandidates: Selected action ActionId={selectedAction.ActionId} " +
                    $"with Priority={selectedAction.Priority} from {actionComponent.PreorderActions.Count} candidates on entity {entity.UniqueId}");
                
                // 切换到新动作
                SwitchToAction(actionComponent, actionInfo, selectedAction, entity);
            }
            else
            {
                ASLogger.Instance.Warning($"ActionCapability.SelectActionFromCandidates: ActionId={selectedAction.ActionId} " +
                    $"not found in AvailableActions dictionary on entity {entity.UniqueId}");
            }
            
            // 清空候选列表
            actionComponent.PreorderActions.Clear();
        }
        
        /// <summary>
        /// 切换到指定动作
        /// </summary>
        private void SwitchToAction(ActionComponent actionComponent, ActionInfo actionInfo, PreorderActionInfo preorderInfo, Entity entity)
        {
            // 记录切换前的动作信息
            var previousActionId = actionComponent.CurrentAction?.Id ?? 0;
            var previousFrame = actionComponent.CurrentFrame;
            
            actionComponent.CurrentAction = actionInfo;
            actionComponent.CurrentFrame = preorderInfo.FromFrame;
            
            // 记录切换成功的日志
            ASLogger.Instance.Debug($"ActionCapability.SwitchToAction: Successfully switched action on entity {entity.UniqueId} " +
                $"from ActionId={previousActionId}(Frame={previousFrame}) to ActionId={actionInfo.Id}(Frame={preorderInfo.FromFrame}) " +
                $"[Priority={preorderInfo.Priority}, TransitionFrames={preorderInfo.TransitionFrames}]");
        }
        
        /// <summary>
        /// 更新当前动作
        /// </summary>
        private void UpdateCurrentAction(Entity entity)
        {
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null)
            {
                ASLogger.Instance.Error($"ActionCapability.UpdateCurrentAction: ActionComponent not found on entity {entity.UniqueId}");
                return;
            }
            if (actionComponent.CurrentAction == null) return;
            
            // 按帧更新动作进度
            actionComponent.CurrentFrame += 1;
        }
        
        /// <summary>
        /// 检查动作是否已结束
        /// </summary>
        private bool IsActionFinished(ActionComponent actionComponent, int actionDuration)
        {
            if (actionComponent == null || actionComponent.CurrentAction == null) return false;
            return actionComponent.CurrentFrame >= actionDuration;
        }
        
        /// <summary>
        /// 获取动作信息
        /// </summary>
        private ActionInfo? GetActionInfo(int actionId, long entityId)
        {
            return ActionConfig.Instance?.GetAction(actionId, entityId);
        }
        
        /// <summary>
        /// 获取可用动作列表
        /// </summary>
        private IEnumerable<ActionInfo> GetAvailableActions(ActionComponent actionComponent)
        {
            if (actionComponent == null) return new List<ActionInfo>();
            return actionComponent.AvailableActions?.Values ?? (IEnumerable<ActionInfo>)new List<ActionInfo>();
        }
        
        /// <summary>
        /// 获取动作持续时间
        /// </summary>
        private int GetActionDuration(ActionInfo actionInfo)
        {
            if (actionInfo == null) return 0;
            return actionInfo.Duration;
        }
        
        /// <summary>
        /// 检查是否可以取消到指定动作
        /// </summary>
        private bool CanCancelToAction(ActionComponent actionComponent, ActionInfo targetAction)
        {
            if (actionComponent == null || actionComponent.CurrentAction == null || targetAction == null) return false;
            
            // 检查CancelTag是否匹配BeCancelledTag
            foreach (var cancelTag in targetAction.CancelTags)
            {
                foreach (var beCancelledTag in actionComponent.CurrentAction.BeCancelledTags)
                {
                    if (beCancelledTag.Tags.Contains(cancelTag.Tag))
                    {
                        // 检查时间范围
                        if (beCancelledTag.RangeFrames.Count >= 2 && 
                            IsInTimeRange(actionComponent, beCancelledTag.RangeFrames[0], beCancelledTag.RangeFrames[1]))
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
        private bool HasValidCommand(Entity entity, ActionInfo actionInfo)
        {
            foreach (var command in actionInfo.Commands)
            {
                if (IsCommandValid(entity, command))
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
        private bool IsInTimeRange(ActionComponent actionComponent, int startFrame, int endFrame)
        {
            if (actionComponent == null || actionComponent.CurrentAction == null) return false;
            // 直接按帧数判断
            return actionComponent.CurrentFrame >= startFrame && actionComponent.CurrentFrame <= endFrame;
        }
        
        /// <summary>
        /// 检查命令是否有效
        /// </summary>
        private bool IsCommandValid(Entity entity, ActionCommand command)
        {
            // 检查命令名称是否匹配
            return CheckCommandMatch(entity, command.CommandName);
        }
        
        /// <summary>
        /// 检查命令是否匹配
        /// </summary>
        private bool CheckCommandMatch(Entity entity, string commandName)
        {
            // 获取输入组件（Monster等非玩家控制实体可能没有此组件，这是正常的）
            var inputComponent = GetComponent<LSInputComponent>(entity);
            if (inputComponent == null)
            {
                // 没有输入组件的实体（如Monster）无法匹配玩家输入命令
                return false;
            }
            if (inputComponent.CurrentInput == null) return false;
            
            var currentInput = inputComponent.CurrentInput;
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
