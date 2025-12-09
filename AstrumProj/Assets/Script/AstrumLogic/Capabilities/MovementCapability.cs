using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Physics;
using TrueSync;
using Astrum.CommonBase;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 移动能力（新架构，基于 Capability&lt;T&gt;）
    /// 处理实体的移动逻辑
    /// </summary>
    public class MovementCapability : Capability<MovementCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 100;
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.Movement, 
            CapabilityTag.Control,
            CapabilityTag.UserInputMovement
        };
        
        // ====== 常量配置（可移至配置文件） ======
        private const string KEY_MOVEMENT_THRESHOLD = "MovementThreshold";
        private static readonly FP DEFAULT_MOVEMENT_THRESHOLD = (FP)0.1f;
        
        // ====== 日志相关 Key ======
        private const string KEY_LOGGED_NO_DERIVED_STATS = "Movement_Log_NoDerivedStats";
        private const string KEY_LOGGED_BLOCKED_MOVE = "Movement_Log_BlockedMove";
        private const string KEY_LOGGED_ZERO_SPEED = "Movement_Log_ZeroSpeed";
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            
            // 初始化自定义数据
            var state = GetCapabilityState(entity);
            if (state.CustomData == null)
                state.CustomData = new Dictionary<string, object>();
            
            state.CustomData[KEY_MOVEMENT_THRESHOLD] = DEFAULT_MOVEMENT_THRESHOLD;
            state.CustomData[KEY_LOGGED_NO_DERIVED_STATS] = false;
            state.CustomData[KEY_LOGGED_BLOCKED_MOVE] = false;
            state.CustomData[KEY_LOGGED_ZERO_SPEED] = false;
            SetCapabilityState(entity, state);
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 检查必需组件是否存在
            return base.ShouldActivate(entity) &&
                   HasComponent<LSInputComponent>(entity) &&
                   HasComponent<MovementComponent>(entity) &&
                   HasComponent<TransComponent>(entity);
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 缺少任何必需组件则停用
            return base.ShouldDeactivate(entity) ||
                   !HasComponent<LSInputComponent>(entity) ||
                   !HasComponent<MovementComponent>(entity) ||
                   !HasComponent<TransComponent>(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            // 获取组件
            var inputComponent = GetComponent<LSInputComponent>(entity);
            var movementComponent = GetComponent<MovementComponent>(entity);
            var transComponent = GetComponent<TransComponent>(entity);
            
            if (inputComponent?.CurrentInput == null || movementComponent == null || transComponent == null)
                return;
            
            var input = inputComponent.CurrentInput;
            
            // 记录 Tick 调用（排除输入为空的情况）
            if (input != null && (input.MoveX != 0 || input.MoveY != 0))
            {
                // 使用 World.CurFrame 作为实际执行的帧号，而不是 input.Frame（预测帧号）
                int actualFrame = entity.World?.CurFrame ?? input.Frame;
                string posInfo = transComponent != null ? $"pos=({transComponent.Position.x.AsFloat():F2}, {transComponent.Position.y.AsFloat():F2}, {transComponent.Position.z.AsFloat():F2})" : "pos=N/A";
                //ASLogger.Instance.Info($"[MovementCapability.Tick] 实体 {entity.UniqueId} | 帧={actualFrame} | HasShadow={entity.HasShadow} | {posInfo} | MoveX={input.MoveX} | MoveY={input.MoveY}", "MovementCapability.Tick");
            }
            
            // 获取移动阈值
            var state = GetCapabilityState(entity);
            FP threshold = DEFAULT_MOVEMENT_THRESHOLD;
            if (state.CustomData != null &&
                state.CustomData.TryGetValue(KEY_MOVEMENT_THRESHOLD, out var thresholdObj) &&
                thresholdObj is FP thresholdFp)
            {
                threshold = thresholdFp;
            }
            
            // 原有移动逻辑（不变）
            FP moveX = (FP)(input.MoveX / (double)(1L << 32));
            FP moveY = (FP)(input.MoveY / (double)(1L << 32));
            
            FP inputMagnitude = FP.Sqrt(moveX * moveX + moveY * moveY);
            if (entity.HasShadow && (input.MoveX != 0|| input.MoveY != 0))
            {
                // 使用 World.CurFrame 作为实际执行的帧号，而不是 input.Frame（预测帧号）
                int actualFrame = entity.World?.CurFrame ?? input.Frame;
                var trans = GetComponent<TransComponent>(entity);
                string posInfo = trans != null ? $"pos=({trans.Position.x.AsFloat():F2}, {trans.Position.y.AsFloat():F2}, {trans.Position.z.AsFloat():F2})" : "pos=N/A";
                ASLogger.Instance.Info($"MovementCapability: 实体 {entity.UniqueId} 有影子且输入非零 | 帧={actualFrame} | HasShadow={entity.HasShadow} | {posInfo} | MoveX={input.MoveX} | MoveY={input.MoveY}", "MovementCapability.Debug");
            }
            if (inputMagnitude > FP.One)
            {
                moveX /= inputMagnitude;
                moveY /= inputMagnitude;
                inputMagnitude = FP.One;
            }
            
            var deltaTime = LSConstValue.UpdateInterval / 1000f;
            
            // 更新朝向
            if (inputMagnitude > threshold)
            {
                TSVector inputDirection = new TSVector(moveX, FP.Zero, moveY);
                if (inputDirection.sqrMagnitude > FP.EN4)
                {
                    transComponent.Rotation = TSQuaternion.LookRotation(inputDirection, TSVector.up);
                }
            }
            var derivedStats = GetComponent<DerivedStatsComponent>(entity);
            if (derivedStats == null)
            {
                // 有明显移动输入但没有派生属性组件，记录一次日志帮助排查
                if (inputMagnitude > threshold &&
                    state.CustomData != null &&
                    state.CustomData.TryGetValue(KEY_LOGGED_NO_DERIVED_STATS, out var loggedObj1) &&
                    loggedObj1 is bool loggedNoDerived == false)
                {
                    ASLogger.Instance.Warning(
                        $"MovementCapability: 实体 {entity.UniqueId} 有移动输入但缺少 DerivedStatsComponent，无法获取 SPD。Frame={inputComponent.CurrentInput.Frame}, MoveX={inputComponent.CurrentInput.MoveX}, MoveY={inputComponent.CurrentInput.MoveY}");
                    state.CustomData[KEY_LOGGED_NO_DERIVED_STATS] = true;
                    SetCapabilityState(entity, state);
                }
                return;
            }

            // 从派生属性获取最终速度（已包含基础速度 + Buff 修饰）
            var finalSpeed = derivedStats.Get(Stats.StatType.SPD);
            
            // 根据Dash状态更新MovementComponent的速度
            UpdateSpeedWithDashMultiplier(movementComponent, input, finalSpeed);
            
            // 处理移动（如果用户输入位移未被禁用）
            if (inputMagnitude > threshold && movementComponent.CanMove)
            {
                FP speed = movementComponent.Speed;
                
                 // 有移动输入但速度为0或很小，记录一次日志
                if (state.CustomData != null &&
                    state.CustomData.TryGetValue(KEY_LOGGED_ZERO_SPEED, out var loggedZeroObj) &&
                    loggedZeroObj is bool loggedZero == false &&
                    speed <= FP.EN4)
                {
                    ASLogger.Instance.Warning(
                        $"MovementCapability: 实体 {entity.UniqueId} 有移动输入但 MovementComponent.Speed≈0，SPD={finalSpeed}。Frame={input.Frame}, MoveX={input.MoveX}, MoveY={input.MoveY}, CanMove={movementComponent.CanMove}, Dash={input.Dash}");
                    state.CustomData[KEY_LOGGED_ZERO_SPEED] = true;
                    SetCapabilityState(entity, state);
                }

                FP dt = (FP)deltaTime;
                FP deltaX = moveX * speed * dt;
                FP deltaY = moveY * speed * dt;
                
                var pos = transComponent.Position;
                transComponent.Position = new TSVector(pos.x + deltaX, pos.y, pos.z + deltaY);
                var trans = transComponent;
                // 使用 World.CurFrame 作为实际执行的帧号，而不是 input.Frame（预测帧号）
                int actualFrame = entity.World?.CurFrame ?? input.Frame;
                ASLogger.Instance.Info($"MovementCapability: 实体 {entity.UniqueId} 移动，frame:{actualFrame}位置：{trans.Position.x.AsFloat():F2}, {trans.Position.y.AsFloat():F2}, {trans.Position.z.AsFloat():F2}");
                
                // 记录位置历史（用于影子回滚调试）
                movementComponent.RecordPosition(actualFrame, transComponent.Position);
                
                entity.World?.HitSystem?.UpdateEntityPosition(entity);
            }
            else if (inputMagnitude > threshold && !movementComponent.CanMove)
            {
                // 有明显移动输入但标记为不能移动，仅记录一次
                if (state.CustomData != null &&
                    state.CustomData.TryGetValue(KEY_LOGGED_BLOCKED_MOVE, out var loggedObj2) &&
                    loggedObj2 is bool loggedBlocked == false)
                {
                    ASLogger.Instance.Debug(
                        $"MovementCapability: 实体 {entity.UniqueId} 有移动输入但 CanMove=false。Frame={input.Frame}, MoveX={input.MoveX}, MoveY={input.MoveY}, Dash={input.Dash}");
                    state.CustomData[KEY_LOGGED_BLOCKED_MOVE] = true;
                    SetCapabilityState(entity, state);
                }
            }
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 根据Dash状态更新MovementComponent的速度
        /// Dash状态下速度为基础速度的2.5倍
        /// </summary>
        private void UpdateSpeedWithDashMultiplier(MovementComponent movementComponent, Astrum.Generated.LSInput input, FP speed)
        {
            if (movementComponent == null || input == null)
                return;
            
            if (input.Dash)
            {
                // 冲刺状态：速度为基础速度的2.5倍
                movementComponent.SetSpeed(speed * (FP)2.5f);
            }
            else
            {
                // 正常状态：恢复为基础速度
                movementComponent.SetSpeed(speed);
            }
        }

    }
}
