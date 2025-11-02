using System;
using System.Linq;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    [MemoryPackable]
    public partial class BattleStateCapability : Capability
    {
        private const float BattleEnterDistance = 1.5f; // 进入战斗的距离阈值
        private const float LoseTargetDistance = 6.0f;  // 目标过远则切回追击

        public override void Tick()
        {
            if (!CanExecute()) return;
            var fsm = GetOwnerComponent<AIStateMachineComponent>();
            if (fsm == null) return;
            if (!string.Equals(fsm.CurrentState, "Battle", StringComparison.Ordinal)) return;

            var world = OwnerEntity?.World;
            var selfPos = GetOwnerComponent<TransComponent>()?.Position ?? TSVector.zero;
            if (world == null)
            {
                return;
            }

            // 寻找最近目标
            var target = FindNearestEnemy(world, selfPos);
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
			var inputComp = GetOwnerComponent<LSInputComponent>();
			if (inputComp == null) return;
			var curFrame = OwnerEntity?.World?.CurFrame ?? 0;
			if (curFrame >= fsm.NextAttackFrame)
			{
				// 仅这一帧置 Attack=true，并推进冷却帧
				var atk = CreateInput(0, 0, attack: true);
				inputComp.SetInput(atk);
				fsm.NextAttackFrame = curFrame + TSMath.Max(1, (TrueSync.FP)fsm.AttackIntervalFrames).AsInt();
			}
			else
			{
				// 冷却中：显式写入 Attack=false，避免持续为真
				var idle = CreateInput(0, 0, attack: false);
				inputComp.SetInput(idle);
			}
        }

        private Entity? FindNearestEnemy(World world, TSVector selfPos)
        {
            Entity? nearest = null;
            FP best = FP.MaxValue;
            foreach (var kv in world.Entities)
            {
                var e = kv.Value;
                if (e == null || e.UniqueId == OwnerEntity?.UniqueId) continue;
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

        private Astrum.Generated.LSInput CreateInput(float moveX, float moveY, bool attack = false, bool skill1 = false, bool skill2 = false)
        {
            var input = Astrum.Generated.LSInput.Create();
            input.PlayerId = OwnerEntity?.UniqueId ?? 0;
            input.Frame = OwnerEntity?.World?.CurFrame ?? 0;
            input.MoveX = (long)(moveX * (1L << 32));
            input.MoveY = (long)(moveY * (1L << 32));
            input.Attack = attack;
            input.Skill1 = skill1;
            input.Skill2 = skill2;
            input.Timestamp = 0;
            return input;
        }
    }
}


