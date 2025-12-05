using System;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Stats;
using Astrum.CommonBase;
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
        
        // ====== 性能优化：预分配缓冲区（避免 GC） ======
        
        /// <summary>
        /// 预分配的附近实体查询缓冲区，避免 QuerySphereOverlap 每次创建新 List
        /// </summary>
        private List<Entity> _nearbyEntitiesBuffer = new List<Entity>(32);
        
        // ====== 常量 ======
        private const float BattleEnterDistance = 1.5f; // 进入战斗的距离阈值
        private const float LoseTargetDistance = 6.0f;  // 目标过远则切回追击
        private const float RetargetDistance = 8.0f;    // 超过此距离才重新查找目标（避免频繁查询）
        
        // ====== 生命周期 ======
        
        public override void OnDeactivate(Entity entity)
        {
            base.OnDeactivate(entity);
            
            // 状态切换时清理目标缓存（切出战斗状态）
            var fsm = GetComponent<AIStateMachineComponent>(entity);
            if (fsm != null)
            {
                fsm.CurrentTargetId = -1;
                fsm.LastTargetValidationFrame = -1;
            }
        }
        
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
            using (new ProfileScope("BattleStateCapability.Tick"))
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

                // 优化：优先使用缓存的目标，避免每帧查询
                Entity target = null;
                bool needRetarget = false;
                
                using (new ProfileScope("BattleState.ValidateTarget"))
                {
                    // 1. 尝试使用缓存的目标
                    if (fsm.CurrentTargetId > 0)
                    {
                        target = world.GetEntity(fsm.CurrentTargetId);
                        
                        // 验证目标是否仍然有效
                        if (target != null && !IsEntityDead(target))
                        {
                            var cachedTargetPos = target.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
                            FP cachedDist = (cachedTargetPos - selfPos).magnitude;
                            
                            // 目标在合理范围内，继续使用（无需重新查询）
                            if ((float)cachedDist < RetargetDistance)
                            {
                                // 缓存命中，直接使用
                                fsm.LastTargetValidationFrame = world.CurFrame;
                            }
                            else
                            {
                                // 目标超出范围，需要重新查找
                                needRetarget = true;
                                target = null;
                            }
                        }
                        else
                        {
                            // 目标无效（死亡或销毁），需要重新查找
                            needRetarget = true;
                            target = null;
                        }
                    }
                    else
                    {
                        // 无缓存目标，需要查找
                        needRetarget = true;
                    }
                }
                
                // 2. 仅在必要时重新查找目标
                if (needRetarget)
                {
                    using (new ProfileScope("BattleState.FindNearestEnemy"))
                    {
                        target = FindNearestEnemy(world, selfPos, entity);
                        
                        if (target == null)
                        {
                            fsm.CurrentTargetId = -1;
                            fsm.LastTargetValidationFrame = world.CurFrame;
                            fsm.NextState = "Idle";
                            return;
                        }
                        
                        // 更新缓存
                        fsm.CurrentTargetId = target.UniqueId;
                        fsm.LastTargetValidationFrame = world.CurFrame;
                    }
                }

                // 3. 使用目标执行战斗逻辑
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
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 检查实体是否死亡
        /// </summary>
        private bool IsEntityDead(Entity entity)
        {
            return false;
            if (entity == null) return true;
            
            // 检查是否有 DynamicStatsComponent 且 HP <= 0
            var stats = entity.GetComponent<DynamicStatsComponent>();
            if (stats != null)
            {
                return stats.Get(DynamicResourceType.CURRENT_HP) <= FP.Zero;
            }
            
            return false;
        }
        
        private Entity? FindNearestEnemy(World world, TSVector selfPos, Entity selfEntity)
        {
            // 优化：使用 BEPU 物理引擎的空间索引查询附近实体
            var physicsWorld = world.HitSystem?.PhysicsWorld;
            if (physicsWorld != null)
            {
                // 使用物理查询（仅查询附近实体，而非全量遍历）
                // 使用预分配的缓冲区，避免每次创建新 List
                physicsWorld.QuerySphereOverlap(selfPos, (FP)RetargetDistance, _nearbyEntitiesBuffer);
                
                Entity nearestEnemy = null;
                FP bestDist = FP.MaxValue;
                
                // 使用 for 循环避免枚举器 GC
                int nearbyCount = _nearbyEntitiesBuffer.Count;
                for (int i = 0; i < nearbyCount; i++)
                {
                    var e = _nearbyEntitiesBuffer[i];
                    if (e == null || e.UniqueId == selfEntity.UniqueId) continue;
                    if (IsEntityDead(e)) continue; // 跳过死亡实体
                    if (e.GetComponent<RoleInfoComponent>() == null) continue; // 只考虑角色
                    
                    var pos = e.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
                    var d = (pos - selfPos).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        nearestEnemy = e;
                    }
                }
                
                return nearestEnemy;
            }
            
            // 回退：如果物理世界不可用，使用全量遍历（保证功能正确性）
            Entity nearestFallback = null;
            FP best = FP.MaxValue;
            foreach (var kv in world.Entities)
            {
                var e = kv.Value;
                if (e == null || e.UniqueId == selfEntity.UniqueId) continue;
                if (IsEntityDead(e)) continue;
                if (e.GetComponent<RoleInfoComponent>() == null) continue; // 只考虑角色
                var pos = e.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
                var d = (pos - selfPos).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearestFallback = e;
                }
            }
            return nearestFallback;
        }

        private Astrum.Generated.LSInput CreateInput(Entity entity, float moveX, float moveY, bool attack = false, bool skill1 = false, bool skill2 = false, TSVector? attackTargetPos = null)
        {
            // 使用对象池复用 LSInput，减少 GC 分配
            var input = Astrum.Generated.LSInput.Create(isFromPool: true);
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
