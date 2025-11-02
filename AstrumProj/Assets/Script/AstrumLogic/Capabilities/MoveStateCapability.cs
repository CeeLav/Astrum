namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Move 状态能力（最小实现）。
    /// </summary>
    using System;
    public class MoveStateCapability : Capability
    {
        private const float BattleEnterDistance = 1.5f;
        private const float ArriveThreshold = 0.3f;
        private const float DetectionRadius = 12f;

        public override void Tick()
        {
            if (!CanExecute()) return;
            var fsm = GetOwnerComponent<Astrum.LogicCore.Components.AIStateMachineComponent>();
            if (fsm == null) return;
            if (!string.Equals(fsm.CurrentState, "Move", System.StringComparison.Ordinal)) return;

            var world = OwnerEntity?.World;
            var pos = GetOwnerComponent<Astrum.LogicCore.Components.TransComponent>()?.Position ?? TrueSync.TSVector.zero;
            if (world == null) return;

            // 1) 优先追逐最近目标
            var target = FindNearestEnemy(world, pos, DetectionRadius);
            if (target != null)
            {
                fsm.CurrentTargetId = target.UniqueId;
                var tpos = target.GetComponent<Astrum.LogicCore.Components.TransComponent>()?.Position ?? TrueSync.TSVector.zero;
                var dir = (tpos - pos);
                var dist = dir.magnitude;
                if ((float)dist <= BattleEnterDistance)
                {
                    fsm.NextState = "Battle";
                    return;
                }
                var (nx, nz) = Normalize2D(dir);
                PushMoveInput(nx, nz);
                return;
            }

            // 2) 没有目标：朝随机漫游点移动
            var delta = fsm.WanderTarget - pos;
            var d = delta.magnitude;
            if ((float)d <= ArriveThreshold)
            {
                fsm.NextState = "Idle";
                return;
            }
            var norm = Normalize2D(delta);
            PushMoveInput(norm.x, norm.z);
        }

        private Astrum.LogicCore.Core.Entity? FindNearestEnemy(Astrum.LogicCore.Core.World world, TrueSync.TSVector selfPos, float maxRadius)
        {
            Astrum.LogicCore.Core.Entity? nearest = null;
            var maxR2 = (TrueSync.FP)(maxRadius * maxRadius);
            TrueSync.FP best = maxR2 + TrueSync.FP.One;
            foreach (var kv in world.Entities)
            {
                var e = kv.Value;
                if (e == null || e.UniqueId == OwnerEntity?.UniqueId) continue;
                if (e.GetComponent<Astrum.LogicCore.Components.RoleInfoComponent>() == null) continue;
                var p = e.GetComponent<Astrum.LogicCore.Components.TransComponent>()?.Position ?? TrueSync.TSVector.zero;
                var d2 = (p - selfPos).sqrMagnitude;
                if (d2 < best)
                {
                    best = d2;
                    nearest = e;
                }
            }
            return nearest;
        }

        private (float x, float z) Normalize2D(TrueSync.TSVector v)
        {
            var x = (float)v.x;
            var z = (float)v.z;
            var mag = (float)Math.Sqrt(x * x + z * z);
            if (mag <= 1e-6f) return (0, 0);
            return (x / mag, z / mag);
        }

        private void PushMoveInput(float nx, float nz)
        {
            var input = Astrum.Generated.LSInput.Create();
            input.PlayerId = OwnerEntity?.UniqueId ?? 0;
            input.Frame = OwnerEntity?.World?.CurFrame ?? 0;
            input.MoveX = (long)(nx * (1L << 32));
            input.MoveY = (long)(nz * (1L << 32));
            input.Timestamp = 0;
            var ic = GetOwnerComponent<Astrum.LogicCore.FrameSync.LSInputComponent>();
            ic?.SetInput(input);
        }
    }
}


