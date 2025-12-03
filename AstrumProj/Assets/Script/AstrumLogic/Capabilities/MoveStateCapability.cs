using System;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Move 状态能力（新架构，基于 Capability&lt;T&gt;）
    /// </summary>
    public class MoveStateCapability : Capability<MoveStateCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 450; // 状态能力优先级
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.AI 
        };
        
        // ====== 常量 ======
        private const float BattleEnterDistance = 1.5f;
        private const float ArriveThreshold = 0.3f;
        private const float DetectionRadius = 12f;
        
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
            if (!string.Equals(fsm.CurrentState, "Move", StringComparison.Ordinal)) return;

            var world = entity.World;
            var pos = GetComponent<TransComponent>(entity)?.Position ?? TSVector.zero;
            if (world == null) return;

            // 1) 优先追逐最近目标
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
                var (nx, nz) = Normalize2D(dir);
                PushMoveInput(entity, nx, nz);
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
            PushMoveInput(entity, norm.x, norm.z);
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
            // 使用对象池复用 LSInput，减少 GC 分配
            var input = Astrum.Generated.LSInput.Create(isFromPool: true);
            input.PlayerId = entity.UniqueId;
            input.Frame = entity.World?.CurFrame ?? 0;
            input.MoveX = (long)(nx * (1L << 32));
            input.MoveY = (long)(nz * (1L << 32));
            input.Timestamp = 0;
            var ic = GetComponent<LSInputComponent>(entity);
            ic?.SetInput(input);
        }
    }
}
