using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Stats;
using Astrum.CommonBase;
using Astrum.Generated;
using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using Astrum.LogicCore.Events;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 动作能力（新架构，基于 Capability）
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

        private const string DefaultExternalSourceTagPrefix = "ExternalAction";
        
        // ====== 性能优化：预分配缓冲区 ======
        // 用于 GetAvailableActions 避免每次创建新 List
        private readonly List<ActionInfo> _availableActionsBuffer = new List<ActionInfo>(16);
        
        // 用于 GetAvailableActionIds 避免每次创建新 List
        private readonly List<int> _availableActionIdsBuffer = new List<int>(8);
        
        // 用于 MergeExternalPreorders 避免 Dictionary 枚举器 GC
        private readonly List<string> _externalKeysBuffer = new List<string>(8);
        
        // ====== 生命周期 & 事件 ======

        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<ActionPreorderEvent>(OnActionPreorder);
        }
        
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
            using (new ProfileScope("ActionCapability.Tick"))
            {
                // 1. 更新当前动作
                UpdateCurrentAction(entity);
                
                // 获取 ActionComponent
                var actionComponent = GetComponent<ActionComponent>(entity);

                if (actionComponent?.CurrentAction != null)
                {
                    UpdateMovementAndAnimationSpeed(entity, actionComponent);
                }

                using (new ProfileScope("ActionCap.SyncInputCommands"))
                {
                    SyncInputCommands(entity, actionComponent);
                }
                
                // 2. 检查所有动作的取消条件
                using (new ProfileScope("ActionCap.CheckCancellation"))
                {
                    CheckActionCancellation(entity);
                }
                
                // 3. 从候选列表选择动作
                using (new ProfileScope("ActionCap.SelectAction"))
                {
                    SelectActionFromCandidates(entity);
                }
            }
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
                TryCacheAction(actionComponent, actionId, entity);
            }
            
            // 设置初始动作ID
            if (actionComponent.AvailableActions.Count > 0 && availableActionIds.Count > 0)
            {
                actionComponent.CurrentActionId = availableActionIds[0];
                
                // 标记ActionComponent为脏，以便视图层同步
                entity.MarkComponentDirty(ActionComponent.ComponentTypeId);
                
                ASLogger.Instance.Debug($"ActionCapability.LoadAvailableActions: Set initial action ActionId={actionComponent.CurrentActionId} " +
                    $"on entity {entity.UniqueId}");
            }
            else
            {
                ASLogger.Instance.Warning($"ActionCapability.LoadAvailableActions: No available actions found for entity {entity.UniqueId}");
            }
        }
        
        /// <summary>
        /// 获取可用动作ID列表（优化：使用预分配缓冲区）
        /// </summary>
        private List<int> GetAvailableActionIds(Entity entity)
        {
            _availableActionIdsBuffer.Clear();
            var config = entity.EntityConfig;

            if (config != null)
            {
                AddIfValid(_availableActionIdsBuffer, config.IdleAction);
                AddIfValid(_availableActionIdsBuffer, config.WalkAction);
                AddIfValid(_availableActionIdsBuffer, config.RunAction);
                AddIfValid(_availableActionIdsBuffer, config.HitAction);
            }

            return _availableActionIdsBuffer;

            static void AddIfValid(List<int> target, int actionId)
            {
                if (actionId > 0 && !target.Contains(actionId))
                {
                    target.Add(actionId);
                }
            }
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
            
            // 清空预订单列表（回收到对象池）
            RecyclePreorderActions(actionComponent.PreorderActions);
            actionComponent.PreorderActions.Clear();
            
            // 检查当前动作是否已结束
            var actionDuration = GetActionDuration(actionComponent.CurrentAction);
            var hasValidCommand = HasValidCommand(entity, actionComponent.CurrentAction); //当前动作的指令是否还在输入
            var shouldTerminate = (actionComponent.CurrentAction.AutoTerminate && !hasValidCommand); //当前动作是否自动终止且没有有效指令
            var shouldContinue = !IsActionFinished(actionComponent, actionDuration) || hasValidCommand;//当前动作未结束或有有效指令则继续
            // ASLogger.Instance.Debug($"ActionCapability.CheckActionCancellation: CurrentActionId={actionComponent.CurrentAction.Id}, " +
            //     $"CurrentFrame={actionComponent.CurrentFrame}, ActionDuration={actionDuration}, " +
            //     $"ShouldContinue={shouldContinue}, ShouldTerminate={shouldTerminate} on entity {entity.UniqueId}");
            if (!shouldContinue || shouldTerminate)
            {
                // 添加默认下一个动作到预订单列表
                var nextActionId = actionComponent.CurrentAction.AutoNextActionId;
                if (nextActionId > 0)
                {
                    // 从可用动作字典中查找下一个动作的数据
                    if (actionComponent.AvailableActions.TryGetValue(nextActionId, out var nextAction))
                    {
                        // 使用对象池创建 PreorderActionInfo，减少 GC 分配
                        var preorder = PreorderActionInfo.Create(
                            actionId: nextActionId,
                            priority: nextAction.Priority,
                            transitionFrames: 3,
                            fromFrame: 0,
                            freezingFrames: 0
                        );
                        actionComponent.PreorderActions.Add(preorder);
                    }
                }
            }
            
            // 检查其他动作的取消条件
            var availableActions = GetAvailableActions(actionComponent);
            
            using (new ProfileScope("ActionCap.CheckCancelLoop"))
            {
                using (new ProfileScope("CancelLoop.IterateActions"))
                {
                    for (int i = 0; i < availableActions.Count; i++)
                    {
                        var action = availableActions[i];
                        
                        using (new ProfileScope("CancelLoop.HasValidCommand"))
                        {
                            if (!HasValidCommand(entity, action))
                            {
                                continue;
                            }
                        }

                        using (new ProfileScope("CancelLoop.TryGetContext"))
                        {
                                        if (TryGetMatchingCancelContext(actionComponent, action, out var cancelTag, out var beCancelledTag))
                            {
                                // 使用对象池创建 PreorderActionInfo，减少 GC 分配
                                var preorder = PreorderActionInfo.Create(
                                    actionId: action.Id,
                                    priority: CalculatePriority(action, cancelTag),
                                    transitionFrames: (cancelTag?.BlendInFrames > 0 ? cancelTag.BlendInFrames : 3),
                                    fromFrame: cancelTag?.StartFromFrames ?? 0,
                                    freezingFrames: beCancelledTag?.BlendOutFrames ?? 0
                                );
                                actionComponent.PreorderActions.Add(preorder);
                            }
                            else if (CanCancelToAction(actionComponent, action))
                            {
                                // 使用对象池创建 PreorderActionInfo，减少 GC 分配
                                var preorder = PreorderActionInfo.Create(
                                    actionId: action.Id,
                                    priority: CalculatePriority(action),
                                    transitionFrames: 3,
                                    fromFrame: 0,
                                    freezingFrames: 0
                                );
                                actionComponent.PreorderActions.Add(preorder);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 从预订单列表选择动作
        /// </summary>
        private void SelectActionFromCandidates(Entity entity)
        {
            ActionComponent actionComponent;
            using (new ProfileScope("Select.GetComponent"))
            {
                actionComponent = GetComponent<ActionComponent>(entity);
                if (actionComponent == null)
                {
                    ASLogger.Instance.Error($"ActionCapability.SelectActionFromCandidates: ActionComponent not found on entity {entity.UniqueId}");
                    return;
                }
            }
            
            using (new ProfileScope("Select.MergeExternal"))
            {
                MergeExternalPreorders(actionComponent, entity);
            }

            if (actionComponent.PreorderActions == null || actionComponent.PreorderActions.Count == 0)
            {
                if (actionComponent.CurrentFrame > actionComponent.CurrentAction.Duration)
                {
                    actionComponent.CurrentFrame -= actionComponent.CurrentAction.Duration; // 避免一直累积帧数
                }
                return;
            }
            
            PreorderActionInfo selectedAction;
            using (new ProfileScope("Select.Sort"))
            {
                using (new ProfileScope("Sort.DoSort"))
                {
                    // 按优先级排序（Sort 可能有 GC）
                    actionComponent.PreorderActions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                }
                using (new ProfileScope("Sort.GetFirst"))
                {
                    selectedAction = actionComponent.PreorderActions[0];
                }
            }
            
            ActionInfo actionInfo;
            using (new ProfileScope("Select.Lookup"))
            {
                // 从 AvailableActions 字典中查找
                if (!actionComponent.AvailableActions.TryGetValue(selectedAction.ActionId, out actionInfo))
                {
                    ASLogger.Instance.Warning($"ActionCapability.SelectActionFromCandidates: ActionId={selectedAction.ActionId} " +
                        $"not found in AvailableActions dictionary on entity {entity.UniqueId}");
                    
                    // 清空候选列表（回收到对象池）
                    using (new ProfileScope("ActionCap.RecycleAfterSelect"))
                    {
                        RecyclePreorderActions(actionComponent.PreorderActions);
                        actionComponent.PreorderActions.Clear();
                    }
                    return;
                }
            }
            
            using (new ProfileScope("ActionCap.SwitchAction"))
            {
                // 切换到新动作
                SwitchToAction(actionComponent, actionInfo, selectedAction, entity);
            }
            
            // 清空候选列表（回收到对象池）
            using (new ProfileScope("ActionCap.RecycleAfterSelect"))
            {
                using (new ProfileScope("Recycle.RecycleLoop"))
                {
                    RecyclePreorderActions(actionComponent.PreorderActions);
                }
                using (new ProfileScope("Recycle.Clear"))
                {
                    actionComponent.PreorderActions.Clear();
                }
            }
        }
        
        /// <summary>
        /// 切换到指定动作
        /// </summary>
        private void SwitchToAction(ActionComponent actionComponent, ActionInfo actionInfo, PreorderActionInfo preorderInfo, Entity entity)
        {
            // 记录切换前的动作信息
            var previousActionId = actionComponent.CurrentAction?.Id ?? 0;
            var previousFrame = actionComponent.CurrentFrame;

            var consumedCommand = ConsumeCommandForAction(actionComponent, actionInfo);
            
            actionComponent.CurrentAction = actionInfo;
            actionComponent.CurrentFrame = preorderInfo.FromFrame;

            // 标记ActionComponent为脏，以便视图层同步
            entity.MarkComponentDirty(ActionComponent.ComponentTypeId);

            TryUpdateFacingByCommand(entity, consumedCommand);
            
            // 只有当动作ID真正变化时才记录日志
            if (previousActionId != actionInfo.Id)
            {
                // 记录切换成功的日志，包含世界类型
                string worldType = (entity.World?.Name == "ShadowWorld") ? "影子世界" : "权威世界";
                //ASLogger.Instance.Info($"[ActionSync] ActionCapability: 实体 {entity.UniqueId} 动作切换，从 ActionId={previousActionId}(Frame={previousFrame}) 到 ActionId={actionInfo.Id}(Frame={preorderInfo.FromFrame})，世界类型：{worldType}");
            }

            UpdateMovementAndAnimationSpeed(entity, actionComponent);
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

            // 标记ActionComponent为脏，以便视图层同步
            entity.MarkComponentDirty(ActionComponent.ComponentTypeId);
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
        /// 获取可用动作列表（优化：使用预分配缓冲区，避免每次创建新 List）
        /// </summary>
        private List<ActionInfo> GetAvailableActions(ActionComponent actionComponent)
        {
            _availableActionsBuffer.Clear(); // 清空但保留容量
            
            if (actionComponent?.AvailableActions != null)
            {
                // 手动复制到缓冲区（使用 foreach Values，Values 的枚举器是 struct）
                foreach (var action in actionComponent.AvailableActions.Values)
                {
                    _availableActionsBuffer.Add(action);
                }
            }
            
            return _availableActionsBuffer;
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
            return TryGetMatchingCancelContext(actionComponent, targetAction, out _, out _);
        }

        private bool TryGetMatchingCancelContext(
            ActionComponent actionComponent,
            ActionInfo targetAction,
            out CancelTag matchedCancelTag,
            out BeCancelledTag matchedBeCancelledTag)
        {
            matchedCancelTag = null;
            matchedBeCancelledTag = null;

            if (actionComponent == null || actionComponent.CurrentAction == null || targetAction == null)
            {
                return false;
            }

            var currentAction = actionComponent.CurrentAction;

            if (targetAction.CancelTags == null || targetAction.CancelTags.Count == 0 ||
                currentAction.BeCancelledTags == null || currentAction.BeCancelledTags.Count == 0)
            {
                return false;
            }

            var bestScore = int.MinValue;

            // 使用 for 循环代替 foreach，避免枚举器分配
            int cancelTagCount = targetAction.CancelTags.Count;
            for (int i = 0; i < cancelTagCount; i++)
            {
                var cancelTag = targetAction.CancelTags[i];
                if (cancelTag == null || string.IsNullOrEmpty(cancelTag.Tag))
                {
                    continue;
                }

                int beCancelledCount = currentAction.BeCancelledTags.Count;
                for (int j = 0; j < beCancelledCount; j++)
                {
                    var beCancelledTag = currentAction.BeCancelledTags[j];
                    if (beCancelledTag == null || beCancelledTag.Tags == null)
                    {
                        continue;
                    }

                    if (!beCancelledTag.Tags.Contains(cancelTag.Tag))
                    {
                        continue;
                    }

                    if (beCancelledTag.RangeFrames == null || beCancelledTag.RangeFrames.Count < 2)
                    {
                        continue;
                    }

                    var rangeStart = beCancelledTag.RangeFrames[0];
                    var rangeEnd = beCancelledTag.RangeFrames[1];

                    if (!IsInTimeRange(actionComponent, rangeStart, rangeEnd))
                    {
                        continue;
                    }

                    var cancelPriority = cancelTag.Priority;
                    var beCancelledPriority = beCancelledTag.Priority;
                    var score = cancelPriority + beCancelledPriority;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        matchedCancelTag = cancelTag;
                        matchedBeCancelledTag = beCancelledTag;
                    }
                }
            }

            return matchedCancelTag != null;
        }
        
        /// <summary>
        /// 检查是否有有效命令
        /// 使用AND逻辑：所有命令都必须满足
        /// </summary>
        private bool HasValidCommand(Entity entity, ActionInfo actionInfo)
        {
            if (actionInfo == null)
            {
                return false;
            }

            if (actionInfo.Commands == null || actionInfo.Commands.Count == 0)
            {
                return false;
            }

            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null)
            {
                return false;
            }

            var inputCommands = actionComponent.InputCommands;
            if (inputCommands == null || inputCommands.Count == 0)
            {
                return false;
            }

            // AND逻辑：检查actionInfo的每个命令是否都在inputCommands中
            // 使用 for 循环代替 foreach，避免枚举器分配
            int commandCount = actionInfo.Commands.Count;
            for (int i = 0; i < commandCount; i++)
            {
                var command = actionInfo.Commands[i];
                if (command == null || string.IsNullOrEmpty(command.CommandName))
                {
                    continue;
                }

                bool found = false;
                for (int j = 0; j < inputCommands.Count; j++)
                {
                    var input = inputCommands[j];
                    if (input == null)
                    {
                        continue;
                    }

                    // 优化：使用 Ordinal 而不是 OrdinalIgnoreCase，避免字符串分配
                    if (string.Equals(input.CommandName, command.CommandName, StringComparison.Ordinal) &&
                        input.ValidFrames >= command.ValidFrames)
                    {
                        found = true;
                        break;
                    }
                }

                // 如果有任何一个命令没找到，返回false
                if (!found)
                {
                    return false;
                }
            }

            // 所有命令都找到了，返回true
            return true;
        }
        
        /// <summary>
        /// 计算动作优先级
        /// </summary>
        private int CalculatePriority(ActionInfo actionInfo)
        {
            return actionInfo?.Priority ?? 0;
        }

        private int CalculatePriority(ActionInfo actionInfo, CancelTag cancelTag)
        {
            return CalculatePriority(actionInfo) + (cancelTag?.Priority ?? 0);
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
        /// <summary>
        /// 同步输入命令（配置驱动版本）
        /// 从ActionCommandMappingTable读取映射配置
        /// </summary>
        private void SyncInputCommands(Entity entity, ActionComponent actionComponent)
        {
            if (actionComponent == null)
            {
                return;
            }

            LSInputComponent inputComponent;
            using (new ProfileScope("Sync.GetInput"))
            {
                inputComponent = GetComponent<LSInputComponent>(entity);
                if (inputComponent == null || inputComponent.CurrentInput == null)
                {
                    actionComponent.InputCommands.Clear();
                    return;
                }
            }

            var currentInput = inputComponent.CurrentInput;
            var commands = actionComponent.InputCommands;
            if (commands == null)
            {
                actionComponent.InputCommands = commands = new List<ActionCommand>();
            }

            // 递减现有命令的剩余帧数，并移除已过期的命令
            using (new ProfileScope("Sync.DecayCommands"))
            {
                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    var cmd = commands[i];
                    if (cmd == null)
                    {
                        commands.RemoveAt(i);
                        continue;
                    }

                    if (cmd.ValidFrames > 0)
                    {
                        cmd.ValidFrames -= 1;
                    }

                    if (cmd.ValidFrames <= 0)
                    {
                        // 回收到对象池（使用泛型版本）
                        if (cmd.IsFromPool)
                        {
                            ObjectPool.Instance.Recycle<ActionCommand>(cmd);
                        }
                        commands.RemoveAt(i);
                    }
                }
            }

            // 根据配置表同步命令
            using (new ProfileScope("Sync.ProcessMappings"))
            {
                var configManager = TableConfig.Instance;
                if (configManager?.IsInitialized == true && configManager.Tables?.TbActionCommandMappingTable != null)
                {
                    var dataList = configManager.Tables.TbActionCommandMappingTable.DataList;
                    // 使用 for 循环代替 foreach，避免枚举器分配
                    for (int i = 0; i < dataList.Count; i++)
                    {
                        var mapping = dataList[i];
                        if (mapping != null)
                        {
                            bool shouldAdd;
                            using (new ProfileScope("Mapping.ShouldAdd"))
                            {
                                shouldAdd = ShouldAddCommand(currentInput, mapping);
                            }
                            
                            if (shouldAdd)
                            {
                                using (new ProfileScope("Mapping.AddOrRefresh"))
                                {
                                    AddOrRefreshCommand(commands, mapping.CommandName, mapping.ValidFrames, currentInput.MouseWorldX, currentInput.MouseWorldZ);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // 配置表未加载，使用硬编码作为后备
                    ASLogger.Instance.Warning("ActionCapability: ActionCommandMappingTable未加载，使用硬编码后备", "Action.Capability");
                    AddOrRefreshCommand(commands, "move", (currentInput.MoveX != 0 || currentInput.MoveY != 0) ? 1 : 0, 0, 0);
                    AddOrRefreshCommand(commands, "attack", currentInput.Attack ? 3 : 0, currentInput.MouseWorldX, currentInput.MouseWorldZ);
                    AddOrRefreshCommand(commands, "skill1", currentInput.Skill1 ? 3 : 0, currentInput.MouseWorldX, currentInput.MouseWorldZ);
                    AddOrRefreshCommand(commands, "skill2", currentInput.Skill2 ? 3 : 0, currentInput.MouseWorldX, currentInput.MouseWorldZ);
                }
            }
        }
        
        /// <summary>
        /// 判断是否应该添加命令（根据配置表规则）
        /// 优化：LsInputField 已改为 string[] 数组，无需 Split
        /// </summary>
        private bool ShouldAddCommand(LSInput input, cfg.Input.ActionCommandMappingTable mapping)
        {
            // LsInputField 现在是 string[] 数组，使用 for 循环避免枚举器
            if (mapping.LsInputField == null || mapping.LsInputField.Length == 0)
                return mapping.TriggerCondition == "Always";
            
            // 使用 for 循环代替 foreach，避免数组枚举器分配
            for (int i = 0; i < mapping.LsInputField.Length; i++)
            {
                var fieldName = mapping.LsInputField[i];
                if (string.IsNullOrEmpty(fieldName))
                    continue;
                
                switch (mapping.TriggerCondition)
                {
                    case "TRUE":
                        if (GetBoolFieldValue(input, fieldName))
                            return true;
                        break;
                        
                    case "NonZero":
                        if (IsFieldNonZero(input, fieldName))
                            return true;
                        break;
                        
                    case "Always":
                        return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取布尔字段值
        /// </summary>
        private bool GetBoolFieldValue(LSInput input, string fieldName)
        {
            switch (fieldName)
            {
                case "Attack": return input.Attack;
                case "Skill1": return input.Skill1;
                case "Skill2": return input.Skill2;
                case "Roll": return input.Roll;
                case "Dash": return input.Dash;
                default: return false;
            }
        }
        
        /// <summary>
        /// 判断字段是否非零
        /// </summary>
        private bool IsFieldNonZero(LSInput input, string fieldName)
        {
            switch (fieldName)
            {
                case "MoveX": return input.MoveX != 0;
                case "MoveY": return input.MoveY != 0;
                case "MouseWorldX": return input.MouseWorldX != 0;
                case "MouseWorldZ": return input.MouseWorldZ != 0;
                default: return false;
            }
        }

        private static void AddOrRefreshCommand(List<ActionCommand> commands, string name, int validFrames, long targetPositionX, long targetPositionZ)
        {
            if (validFrames <= 0 || commands == null)
            {
                return;
            }

            // 使用 for 循环代替 foreach，避免枚举器分配
            for (int i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                // 优化：使用 Ordinal 而不是 OrdinalIgnoreCase，避免字符串分配
                if (cmd != null && string.Equals(cmd.CommandName, name, StringComparison.Ordinal))
                {
                    if (cmd.ValidFrames < validFrames)
                    {
                        cmd.ValidFrames = validFrames;
                    }
                    cmd.TargetPositionX = targetPositionX;
                    cmd.TargetPositionZ = targetPositionZ;
                    return;
                }
            }

            // 使用对象池创建 ActionCommand，减少 GC 分配
            commands.Add(ActionCommand.Create(name, validFrames, targetPositionX, targetPositionZ));
        }

        private ActionCommand ConsumeCommandForAction(ActionComponent actionComponent, ActionInfo actionInfo)
        {
            if (actionComponent == null || actionInfo == null)
            {
                return null;
            }

            var commands = actionComponent.InputCommands;
            if (commands == null || commands.Count == 0)
            {
                return null;
            }

            if (actionInfo.Commands == null || actionInfo.Commands.Count == 0)
            {
                return null;
            }

            // 使用 for 循环代替 foreach，避免枚举器分配
            int commandCount = actionInfo.Commands.Count;
            for (int cmdIdx = 0; cmdIdx < commandCount; cmdIdx++)
            {
                var command = actionInfo.Commands[cmdIdx];
                if (command == null || string.IsNullOrEmpty(command.CommandName))
                {
                    continue;
                }

                for (int i = 0; i < commands.Count; i++)
                {
                    var input = commands[i];
                    if (input == null)
                    {
                        continue;
                    }

                    // 优化：使用 Ordinal 而不是 OrdinalIgnoreCase，避免字符串分配
                    if (string.Equals(input.CommandName, command.CommandName, StringComparison.Ordinal))
                    {
                        commands.RemoveAt(i);
                        // 注意：这里返回的 input 会被使用，不回收到对象池
                        return input;
                    }
                }
            }

            return null;
        }

        private void TryUpdateFacingByCommand(Entity entity, ActionCommand consumedCommand)
        {
            if (entity == null || consumedCommand == null)
            {
                return;
            }

            var trans = GetComponent<TransComponent>(entity);
            if (trans == null)
            {
                return;
            }

            var currentPos = trans.Position;
            var targetX = FP.FromRaw(consumedCommand.TargetPositionX);
            var targetZ = FP.FromRaw(consumedCommand.TargetPositionZ);

            var targetPos = new TSVector(targetX, currentPos.y, targetZ);
            var direction = targetPos - currentPos;
            direction.y = FP.Zero;

            if (direction.sqrMagnitude <= FP.EN4)
            {
                return;
            }

            direction = TSVector.Normalize(direction);
            
            // 注释掉频繁的 Debug 日志，避免字符串格式化产生 GC
            // ASLogger.Instance.Debug(
            //     $"ActionCapability: Entity={entity.UniqueId} Command={consumedCommand.CommandName} Target=({targetX.AsFloat():F2}, {targetZ.AsFloat():F2}) FacingDir=({direction.x.AsFloat():F2}, {direction.z.AsFloat():F2})",
            //     "Action.MouseFacing");
            
            var rotation = TSQuaternion.LookRotation(direction, TSVector.up);
            trans.SetRotation(rotation);
            entity.MarkComponentDirty(TransComponent.ComponentTypeId);
        }

        /// <summary>
        /// 外部预约动作（例如受击、硬直等）
        /// </summary>
        public bool EnqueueExternalAction(Entity entity, string sourceTag, PreorderActionInfo preorderInfo)
        {
            if (entity == null)
            {
                ASLogger.Instance.Warning("[ActionCapability] EnqueueExternalAction called with null entity");
                return false;
            }

            if (preorderInfo == null || preorderInfo.ActionId <= 0)
            {
                ASLogger.Instance.Warning($"[ActionCapability] Invalid preorder request for entity {entity.UniqueId}, actionId={preorderInfo?.ActionId ?? 0}");
                return false;
            }

            var tag = string.IsNullOrWhiteSpace(sourceTag)
                ? $"{DefaultExternalSourceTagPrefix}-{preorderInfo.ActionId}"
                : sourceTag;

            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null)
            {
                ASLogger.Instance.Warning($"[ActionCapability] Entity {entity.UniqueId} lacks ActionComponent, cannot enqueue external action.");
                return false;
            }

            if (!TryCacheAction(actionComponent, preorderInfo.ActionId, entity))
            {
                ASLogger.Instance.Warning($"[ActionCapability] Unable to cache action {preorderInfo.ActionId} for entity {entity.UniqueId}, external request from {tag} ignored.");
                return false;
            }

            // 使用对象池创建 PreorderActionInfo，减少 GC 分配
            var cloned = PreorderActionInfo.Create(
                actionId: preorderInfo.ActionId,
                priority: preorderInfo.Priority,
                transitionFrames: preorderInfo.TransitionFrames,
                fromFrame: preorderInfo.FromFrame,
                freezingFrames: preorderInfo.FreezingFrames
            );

            actionComponent.ExternalPreorders[tag] = cloned;

            ASLogger.Instance.Debug($"[ActionCapability] External action queued: entity={entity.UniqueId}, actionId={cloned.ActionId}, source={tag}, priority={cloned.Priority}");
            return true;
        }

        private void MergeExternalPreorders(ActionComponent actionComponent, Entity entity)
        {
            if (actionComponent.ExternalPreorders == null || actionComponent.ExternalPreorders.Count == 0)
            {
                return;
            }

            // 优化：先收集 Keys，避免 Dictionary 枚举器可能的 GC
            _externalKeysBuffer.Clear();
            
            using (new ProfileScope("Merge.CollectKeys"))
            {
                // 手动遍历 Keys（Keys 的枚举器是 struct）
                foreach (var key in actionComponent.ExternalPreorders.Keys)
                {
                    _externalKeysBuffer.Add(key);
                }
            }
            
            using (new ProfileScope("Merge.ProcessPreorders"))
            {
                // 遍历预缓存的 Keys
                for (int i = 0; i < _externalKeysBuffer.Count; i++)
                {
                    var key = _externalKeysBuffer[i];
                    if (!actionComponent.ExternalPreorders.TryGetValue(key, out var preorder))
                        continue;
                    
                    if (preorder == null || preorder.ActionId <= 0)
                    {
                        continue;
                    }

                    if (!actionComponent.AvailableActions.ContainsKey(preorder.ActionId) &&
                        !TryCacheAction(actionComponent, preorder.ActionId, entity))
                    {
                        ASLogger.Instance.Warning($"[ActionCapability] External preorder actionId={preorder.ActionId} unavailable for entity {entity.UniqueId}, source={key}");
                        continue;
                    }

                    // 使用对象池创建 PreorderActionInfo，减少 GC 分配
                    var merged = PreorderActionInfo.Create(
                        actionId: preorder.ActionId,
                        priority: preorder.Priority,
                        transitionFrames: preorder.TransitionFrames,
                        fromFrame: preorder.FromFrame,
                        freezingFrames: preorder.FreezingFrames
                    );
                    actionComponent.PreorderActions.Add(merged);
                }
            }

            using (new ProfileScope("Merge.Clear"))
            {
                actionComponent.ExternalPreorders.Clear();
            }
        }

        private bool TryCacheAction(ActionComponent actionComponent, int actionId, Entity entity)
        {
            if (actionComponent == null || actionId <= 0)
            {
                return false;
            }

            if (actionComponent.AvailableActions.ContainsKey(actionId))
            {
                return true;
            }

            var actionInfo = ActionConfig.Instance?.GetAction(actionId, entity?.UniqueId ?? 0);
            if (actionInfo == null)
            {
                ASLogger.Instance.Warning($"[ActionCapability] ActionConfig missing actionId={actionId} for entity {entity?.UniqueId}");
                return false;
            }

            actionComponent.AvailableActions[actionId] = actionInfo;
            return true;
        }

        /// <summary>
        /// 回收 PreorderActionInfo 列表到对象池（优化：使用泛型 Recycle 避免 GetType）
        /// </summary>
        private void RecyclePreorderActions(List<PreorderActionInfo> preorders)
        {
            if (preorders == null || preorders.Count == 0) return;
            
            // 使用 for 循环而不是 foreach，避免枚举器分配
            for (int i = 0; i < preorders.Count; i++)
            {
                var preorder = preorders[i];
                if (preorder != null)
                {
                    using (new ProfileScope("Recycle.CheckFromPool"))
                    {
                        if (!preorder.IsFromPool) continue;
                    }
                    
                    using (new ProfileScope("Recycle.DoRecycle"))
                    {
                        // 使用泛型版本，避免 GetType() 和装箱
                        ObjectPool.Instance.Recycle<PreorderActionInfo>(preorder);
                    }
                }
            }
        }
        
        private void OnActionPreorder(Entity entity, ActionPreorderEvent evt)
        {
            // 使用对象池创建 PreorderActionInfo，减少 GC 分配
            var preorder = PreorderActionInfo.Create(
                actionId: evt.ActionId,
                priority: evt.Priority,
                transitionFrames: evt.TransitionFrames,
                fromFrame: evt.FromFrame,
                freezingFrames: evt.FreezingFrames
            );

            EnqueueExternalAction(entity, evt.SourceTag, preorder);
        }

        private void UpdateMovementAndAnimationSpeed(Entity entity, ActionComponent actionComponent)
        {
            if (actionComponent?.CurrentAction == null)
            {
                return;
            }

            var currentAction = actionComponent.CurrentAction;
            var movementComponent = GetComponent<MovementComponent>(entity);
            if (movementComponent == null)
            {
                currentAction.AnimationSpeedMultiplier = 1f;
                return;
            }
            
            // 如果是移动动作，计算动画播放倍率
            var baseMoveSpeed = currentAction.GetBaseMoveSpeed();
            if (baseMoveSpeed.HasValue && baseMoveSpeed.Value > FP.Zero)
            {
                var animationReferenceSpeed = baseMoveSpeed.Value;  // 动画设计速度（来自 MoveActionTable）
                var actualSpeed = movementComponent.Speed;  // 角色实际速度（来自 BaseUnitStatsTable + Buff）
                
                // 计算动画播放倍率 = 实际速度 / 动画设计速度
                if (actualSpeed > FP.Zero)
                {
                    var ratioFp = actualSpeed / animationReferenceSpeed;
                    var ratio = FP.ToFloat(ratioFp);
                    // 边界检查
                    if (ratio <= 0f || float.IsNaN(ratio) || float.IsInfinity(ratio))
                    {
                        ratio = 1f;
                    }
                    
                    currentAction.AnimationSpeedMultiplier = ratio;
                }
                else
                {
                    currentAction.AnimationSpeedMultiplier = 1f;
                }
            }
            else
            {
                // 非移动动作，保持默认倍率
                currentAction.AnimationSpeedMultiplier = 1f;
            }
        }


    }
}
