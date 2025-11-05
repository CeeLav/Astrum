using System;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Idle 状态能力（新架构，基于 Capability&lt;T&gt;）
    /// </summary>
    public class IdleStateCapability : Capability<IdleStateCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 450; // 状态能力优先级
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.AI 
        };
        
        // ====== 常量 ======
        private const float DetectionRadius = 10f;
        private const float BattleEnterDistance = 1.5f;
        private const int WanderDelayMinFrames = 40; // 2s @20fps
        private const int WanderDelayMaxFrames = 120; // 6s
        private const float WanderRadius = 4f;
        
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
            if (!string.Equals(fsm.CurrentState, "Idle", StringComparison.Ordinal)) return;

            var world = entity.World;
            var pos = GetComponent<TransComponent>(entity)?.Position ?? TSVector.zero;
            if (world == null) return;

            // 1) 搜索最近目标
            var target = FindNearestEnemy(world, pos, DetectionRadius, entity);
            if (target != null)
            {
                fsm.CurrentTargetId = target.UniqueId;
                var tpos = target.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
                var dir = (tpos - pos);
                var dist = dir.magnitude;
                if ((float)dist <= BattleEnterDistance)
                {
                    fsm.NextState = "Battle";
                    return;
                }
                var dir2 = Normalize2D(dir);
                PushMoveInput(entity, dir2.x, dir2.z);
                fsm.NextState = "Move";
                return;
            }

            // 2) 无目标：按周期随机漫游
            if (world.CurFrame >= fsm.NextWanderFrame)
            {
                // 确定性"随机"：基于实体ID与切换帧的哈希
                int seed = unchecked((int)(entity.UniqueId ^ ((long)(fsm.LastSwitchFrame + 1) * 1103515245L)));
                float r0 = Hash01(seed);
                float r1 = Hash01(seed + 1);
                float angle = r0 * (float)Math.PI * 2f;
                float radius = 1f + r1 * (WanderRadius - 1f);
                var offset = new TSVector((FP)(Math.Cos(angle) * radius), FP.Zero, (FP)(Math.Sin(angle) * radius));
                fsm.WanderTarget = pos + offset;
                int delay = WanderDelayMinFrames + (int)(Hash01(seed + 2) * (WanderDelayMaxFrames - WanderDelayMinFrames));
                fsm.NextWanderFrame = world.CurFrame + delay;

                fsm.NextState = "Move";
            }
        }
        
        // ====== 辅助方法 ======
        
        private Entity? FindNearestEnemy(World world, TSVector selfPos, float maxRadius, Entity selfEntity)
        {
            Entity? nearest = null;
            var maxR2 = (FP)(maxRadius * maxRadius);
            FP best = maxR2 + FP.One;
            foreach (var kv in world.Entities)
            {
                var e = kv.Value;
                if (e == null || e.UniqueId == selfEntity.UniqueId) continue;
                if (e.GetComponent<RoleInfoComponent>() == null) continue;
                var p = e.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
                var d2 = (p - selfPos).sqrMagnitude;
                if (d2 < best)
                {
                    best = d2;
                    nearest = e;
                }
            }
            return nearest;
        }

        private (float x, float z) Normalize2D(TSVector v)
        {
            var x = (float)v.x;
            var z = (float)v.z;
            var mag = (float)Math.Sqrt(x * x + z * z);
            if (mag <= 1e-6f) return (0, 0);
            return (x / mag, z / mag);
        }

        private void PushMoveInput(Entity entity, float nx, float nz)
        {
            var input = Astrum.Generated.LSInput.Create();
            input.PlayerId = entity.UniqueId;
            input.Frame = entity.World?.CurFrame ?? 0;
            input.MoveX = (long)(nx * (1L << 32));
            input.MoveY = (long)(nz * (1L << 32));
            input.Timestamp = 0;
            var ic = GetComponent<LSInputComponent>(entity);
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
