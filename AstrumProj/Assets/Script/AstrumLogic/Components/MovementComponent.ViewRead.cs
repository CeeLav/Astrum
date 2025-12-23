using Astrum.LogicCore.Core;
using Astrum.LogicCore.ViewRead;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    public partial class MovementComponent
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
                
                ViewReadFrameSync.Register<MovementComponent, ViewRead>(
                    componentTypeId: ComponentTypeId,
                    createViewRead: comp => new ViewRead(
                        entityId: comp.EntityId,
                        isValid: true,
                        speed: comp.Speed,
                        canMove: comp.CanMove,
                        currentMovementType: comp.CurrentMovementType,
                        moveDirection: comp.MoveDirection),
                    createInvalid: entityId => ViewRead.Invalid(entityId)
                );
                
                _viewReadRegistered = true;
            }
        }

        public readonly struct ViewRead
        {
            public readonly long EntityId;
            public readonly bool IsValid;
            public readonly FP Speed;
            public readonly bool CanMove;
            public readonly MovementType CurrentMovementType;
            public readonly TSVector MoveDirection;

            public ViewRead(long entityId, bool isValid, FP speed, bool canMove, MovementType currentMovementType, TSVector moveDirection)
            {
                EntityId = entityId;
                IsValid = isValid;
                Speed = speed;
                CanMove = canMove;
                CurrentMovementType = currentMovementType;
                MoveDirection = moveDirection;
            }

            public static ViewRead Invalid(long entityId)
            {
                return new ViewRead(entityId, false, default, false, MovementType.None, default);
            }

            public override string ToString()
            {
                return IsValid ? 
                    $"MovementComponent.ViewRead [EntityId={EntityId}, Speed={Speed.AsFloat():F2}, CanMove={CanMove}, CurrentMovementType={CurrentMovementType}, MoveDirection=({MoveDirection.x.AsFloat():F2}, {MoveDirection.y.AsFloat():F2}, {MoveDirection.z.AsFloat():F2})]" :
                    $"MovementComponent.ViewRead [EntityId={EntityId}, Invalid]";
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


