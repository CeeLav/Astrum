using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Components
{
    public partial class ActionComponent
    {
        public readonly struct ViewRead
        {
            public readonly long EntityId;
            public readonly bool IsValid;
            public readonly int CurrentActionId;
            public readonly int CurrentFrame;

            public ViewRead(long entityId, bool isValid, int currentActionId, int currentFrame)
            {
                EntityId = entityId;
                IsValid = isValid;
                CurrentActionId = currentActionId;
                CurrentFrame = currentFrame;
            }

            public static ViewRead Invalid(long entityId)
            {
                return new ViewRead(entityId, false, 0, 0);
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


