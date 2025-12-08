using System;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using TrueSync;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 帧级哈希计算工具，用于比对影子实体与权威实体的一致性
    /// </summary>
    public static class FrameHashUtility
    {
        /// <summary>
        /// 计算实体在指定帧的哈希值（关键字段）
        /// </summary>
        public static int Compute(Entity entity, int frameId)
        {
            if (entity == null) return 0;
            
            var hash = new HashCode();
            
            // 位置/朝向（TransComponent）
            if (entity.TryGetComponent<TransComponent>(out var trans))
            {
                hash.Add(trans.Position.x.RawValue);
                hash.Add(trans.Position.y.RawValue);
                hash.Add(trans.Position.z.RawValue);
                hash.Add(trans.Rotation.x.RawValue);
                hash.Add(trans.Rotation.y.RawValue);
                hash.Add(trans.Rotation.z.RawValue);
                hash.Add(trans.Rotation.w.RawValue);
            }
            
            // 移动速度（MovementComponent）
            if (entity.TryGetComponent<MovementComponent>(out var movement))
            {
                hash.Add(movement.Speed.RawValue);
                hash.Add(movement.CanMove);
            }
            
            // 动作状态（ActionComponent）
            if (entity.TryGetComponent<ActionComponent>(out var action))
            {
                hash.Add(action.CurrentActionId);
                hash.Add(action.CurrentFrame);
            }
            
            // 状态标志（StateComponent）
            if (entity.TryGetComponent<StateComponent>(out var state))
            {
                foreach (var kv in state.States)
                {
                    hash.Add((int)kv.Key);
                    hash.Add(kv.Value);
                }
            }
            
            // 动态属性（DynamicStatsComponent - HP等）
            if (entity.TryGetComponent<DynamicStatsComponent>(out var dynamicStats))
            {
                // 计算关键资源，如 HP、MP
                if (dynamicStats.Resources != null)
                {
                    foreach (var kv in dynamicStats.Resources)
                    {
                        hash.Add((int)kv.Key);
                        hash.Add(kv.Value.RawValue);
                    }
                }
            }
            
            return hash.ToHashCode();
        }
    }
}

