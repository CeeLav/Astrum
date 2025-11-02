namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Idle 状态能力（最小实现）。
    /// </summary>
    using System;
    public class IdleStateCapability : Capability
    {
        private const float DetectionRadius = 10f;
        private const float BattleEnterDistance = 1.5f;
        private const int WanderDelayMinFrames = 40; // 2s @20fps
        private const int WanderDelayMaxFrames = 120; // 6s
        private const float WanderRadius = 4f;

        public override void Tick()
        {
            if (!CanExecute()) return;
            var fsm = GetOwnerComponent<Astrum.LogicCore.Components.AIStateMachineComponent>();
            if (fsm == null) return;
            if (!string.Equals(fsm.CurrentState, "Idle", System.StringComparison.Ordinal)) return;

            var world = OwnerEntity?.World;
            var pos = GetOwnerComponent<Astrum.LogicCore.Components.TransComponent>()?.Position ?? TrueSync.TSVector.zero;
            if (world == null) return;

            // 1) 搜索最近目标
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
                var dir2 = Normalize2D(dir);
                PushMoveInput(dir2.x, dir2.z);
                fsm.NextState = "Move";
                return;
            }

            // 2) 无目标：按周期随机漫游
            if (world.CurFrame >= fsm.NextWanderFrame)
            {
                // 确定性“随机”：基于实体ID与切换帧的哈希
                int seed = unchecked((int)(OwnerEntity!.UniqueId ^ ((long)(fsm.LastSwitchFrame + 1) * 1103515245L)));
                float r0 = Hash01(seed);
                float r1 = Hash01(seed + 1);
                float angle = r0 * (float)Math.PI * 2f;
                float radius = 1f + r1 * (WanderRadius - 1f);
                var offset = new TrueSync.TSVector((TrueSync.FP)(Math.Cos(angle) * radius), TrueSync.FP.Zero, (TrueSync.FP)(Math.Sin(angle) * radius));
                fsm.WanderTarget = pos + offset;
                int delay = WanderDelayMinFrames + (int)(Hash01(seed + 2) * (WanderDelayMaxFrames - WanderDelayMinFrames));
                fsm.NextWanderFrame = world.CurFrame + delay;

                fsm.NextState = "Move";
            }
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

        private float Hash01(int seed)
        {
            unchecked
            {
                uint x = (uint)seed;
                x ^= x >> 17; x *= 0xED5AD4BBu;
                x ^= x >> 11; x *= 0xAC4C1B51u;
                x ^= x >> 15; x *= 0x31848BABu;
                x ^= x >> 14;
                return (x / (float)uint.MaxValue);
            }
        }
    }
}


