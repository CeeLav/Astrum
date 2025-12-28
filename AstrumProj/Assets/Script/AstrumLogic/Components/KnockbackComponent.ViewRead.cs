using Astrum.LogicCore.Core;
using Astrum.LogicCore.ViewRead;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    public partial class KnockbackComponent
    {
        private static bool _viewReadRegistered = false;
        private static readonly object _registerLock = new object();

        /// <summary>
        /// 延迟注册 ViewRead 导出器（避免与 MemoryPack 生成的静态构造函数冲突）
        /// </summary>
        private static void EnsureViewReadRegistered()
        {
            if (_viewReadRegistered) return;
            
            lock (_registerLock)
            {
                if (_viewReadRegistered) return;
                
                ViewReadFrameSync.Register<KnockbackComponent, ViewRead>(
                    componentTypeId: ComponentTypeId,
                    createViewRead: comp => new ViewRead(
                        entityId: comp.EntityId,
                        isValid: true,
                        isKnockingBack: comp.IsKnockingBack,
                        direction: comp.Direction,
                        speed: comp.Speed,
                        totalDistance: comp.TotalDistance,
                        movedDistance: comp.MovedDistance,
                        remainingTime: comp.RemainingTime,
                        type: comp.Type,
                        startPosition: comp.StartPosition,
                        targetPosition: comp.TargetPosition),
                    createInvalid: entityId => ViewRead.Invalid(entityId)
                );
                
                _viewReadRegistered = true;
            }
        }

        public readonly struct ViewRead
        {
            public readonly long EntityId;
            public readonly bool IsValid;
            public readonly bool IsKnockingBack;
            public readonly TSVector Direction;
            public readonly FP Speed;
            public readonly FP TotalDistance;
            public readonly FP MovedDistance;
            public readonly FP RemainingTime;
            public readonly KnockbackType Type;
            public readonly TSVector StartPosition;
            public readonly TSVector TargetPosition;

            public ViewRead(
                long entityId, 
                bool isValid, 
                bool isKnockingBack, 
                TSVector direction, 
                FP speed, 
                FP totalDistance, 
                FP movedDistance,
                FP remainingTime,
                KnockbackType type,
                TSVector startPosition,
                TSVector targetPosition)
            {
                EntityId = entityId;
                IsValid = isValid;
                IsKnockingBack = isKnockingBack;
                Direction = direction;
                Speed = speed;
                TotalDistance = totalDistance;
                MovedDistance = movedDistance;
                RemainingTime = remainingTime;
                Type = type;
                StartPosition = startPosition;
                TargetPosition = targetPosition;
            }

            /// <summary>
            /// 获取剩余击退距离
            /// </summary>
            public FP RemainingDistance => TotalDistance - MovedDistance;

            /// <summary>
            /// 获取总击退时间（秒）
            /// </summary>
            public FP TotalTime => TotalDistance > FP.Zero && Speed > FP.Zero ? TotalDistance / Speed : FP.Zero;

            public static ViewRead Invalid(long entityId)
            {
                return new ViewRead(entityId, false, false, default, default, default, default, default, default, default, default);
            }

            public override string ToString()
            {
                return IsValid ? 
                    $"KnockbackComponent.ViewRead [EntityId={EntityId}, IsKnockingBack={IsKnockingBack}, Direction=({Direction.x.AsFloat():F2}, {Direction.y.AsFloat():F2}, {Direction.z.AsFloat():F2}), Speed={Speed.AsFloat():F2}, TotalDistance={TotalDistance.AsFloat():F2}, MovedDistance={MovedDistance.AsFloat():F2}, RemainingTime={RemainingTime.AsFloat():F2}, Type={Type}, StartPosition=({StartPosition.x.AsFloat():F2}, {StartPosition.y.AsFloat():F2}, {StartPosition.z.AsFloat():F2}), TargetPosition=({TargetPosition.x.AsFloat():F2}, {TargetPosition.y.AsFloat():F2}, {TargetPosition.z.AsFloat():F2})]" :
                    $"KnockbackComponent.ViewRead [EntityId={EntityId}, Invalid]";
            }
        }

        public static bool TryGetViewRead(World world, long entityId, out ViewRead read)
        {
            EnsureViewReadRegistered();
            
            if (world?.ViewReads == null)
            {
                read = default;
                return false;
            }

            return world.ViewReads.TryGet(entityId, ComponentTypeId, out read);
        }
    }
}
