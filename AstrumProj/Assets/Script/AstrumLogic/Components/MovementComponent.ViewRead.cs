using Astrum.LogicCore.Core;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    public partial class MovementComponent
    {
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
        }

        public static bool TryGetViewRead(World world, long entityId, out ViewRead read)
        {
            if (world?.ViewReads == null)
            {
                read = default;
                return false;
            }

            return world.ViewReads.TryGet(entityId, ComponentTypeId, out read);
        }
    }
}


