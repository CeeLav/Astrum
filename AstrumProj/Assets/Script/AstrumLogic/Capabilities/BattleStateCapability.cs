using System;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Battle 状态能力（新架构，基于 Capability&lt;T&gt;）
    /// </summary>
    public class BattleStateCapability : Capability<BattleStateCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 450; // 状态能力优先级
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.AI,
            CapabilityTag.Combat 
        };
        
        // ====== 常量 ======
        private const float BattleEnterDistance = 1.5f; // 进入战斗的距离阈值
        private const float LoseTargetDistance = 6.0f;  // 目标过远则切回追击
        
        // ====== 生命周期 ======
        
        public override bool ShouldActivate(Entity entity)
        {
            // 检查必需组件是否存在
            return base.ShouldActivate(entity) &&
                   HasComponent<AIStateMachineComponent>(entity) &&
                   HasComponent<TransComponent>(entity);
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 缺少任何必需组件则停用
            return base.ShouldDeactivate(entity) ||
                   !HasComponent<AIStateMachineComponent>(entity) ||
                   !HasComponent<TransComponent>(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            var fsm = GetComponent<AIStateMachineComponent>(entity);
            if (fsm == null) return;
            if (!string.Equals(fsm.CurrentState, "Battle", StringComparison.Ordinal)) return;

            var world = entity.World;
            var selfPos = GetComponent<TransComponent>(entity)?.Position ?? TSVector.zero;
            if (world == null)
            {
                return;
            }

            // 寻找最近目标
            var target = FindNearestEnemy(world, selfPos, entity);
            if (target == null)
            {
                fsm.CurrentTargetId = -1;
                fsm.NextState = "Idle";
                return;
            }

            fsm.CurrentTargetId = target.UniqueId;
            var targetPos = target.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
            FP dist = (targetPos - selfPos).magnitude;

            // 超出战斗距离则切回追击
            if ((float)dist > LoseTargetDistance)
            {
                fsm.NextState = "Move";
                return;
            }

			// 在战斗距离内：基于冷却脉冲式攻击
			var inputComp = GetComponent<LSInputComponent>(entity);
			if (inputComp == null) return;
			var curFrame = entity.World?.CurFrame ?? 0;
			if (curFrame >= fsm.NextAttackFrame)
			{
				// 仅这一帧置 Attack=true，并推进冷却帧
				// 攻击方向使用目标位置（MouseWorldX/Z 表示目标世界坐标）
				var atk = CreateInput(entity, 0, 0, attack: true, attackTargetPos: targetPos);
				inputComp.SetInput(atk);
				fsm.NextAttackFrame = curFrame + TSMath.Max(1, (FP)fsm.AttackIntervalFrames).AsInt();
			}
			else
			{
				// 冷却中：显式写入 Attack=false，避免持续为真
				var idle = CreateInput(entity, 0, 0, attack: false);
				inputComp.SetInput(idle);
			}
        }
        
        // ====== 辅助方法 ======
        
        private Entity? FindNearestEnemy(World world, TSVector selfPos, Entity selfEntity)
        {
            Entity? nearest = null;
            FP best = FP.MaxValue;
            foreach (var kv in world.Entities)
            {
                var e = kv.Value;
                if (e == null || e.UniqueId == selfEntity.UniqueId) continue;
                if (e.GetComponent<RoleInfoComponent>() == null) continue; // 只考虑角色
                var pos = e.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
                var d = (pos - selfPos).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = e;
                }
            }
            return nearest;
        }

        private Astrum.Generated.LSInput CreateInput(Entity entity, float moveX, float moveY, bool attack = false, bool skill1 = false, bool skill2 = false, TSVector? attackTargetPos = null)
        {
            var input = Astrum.Generated.LSInput.Create();
            input.PlayerId = entity.UniqueId;
            input.Frame = entity.World?.CurFrame ?? 0;
            input.MoveX = (long)(moveX * (1L << 32));
            input.MoveY = (long)(moveY * (1L << 32));
            input.Attack = attack;
            input.Skill1 = skill1;
            input.Skill2 = skill2;
            input.Timestamp = 0;
            
            // 设置攻击方向（使用目标位置作为鼠标世界坐标）
            if (attackTargetPos.HasValue)
            {
                input.MouseWorldX = (long)(attackTargetPos.Value.x.AsFloat() * (1L << 32));
                input.MouseWorldZ = (long)(attackTargetPos.Value.z.AsFloat() * (1L << 32));
            }
            else
            {
                // 如果没有指定目标位置，使用默认值（0, 0）
                input.MouseWorldX = 0;
                input.MouseWorldZ = 0;
            }
            
            return input;
        }
    }
}
