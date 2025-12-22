using Astrum.LogicCore.Core;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    public partial class TransComponent
    {
        public readonly struct ViewRead
        {
            public readonly long EntityId;
            public readonly bool IsValid;
            public readonly TSVector Position;
            public readonly TSQuaternion Rotation;

            public ViewRead(long entityId, bool isValid, TSVector position, TSQuaternion rotation)
            {
                EntityId = entityId;
                IsValid = isValid;
                Position = position;
                Rotation = rotation;
            }

            public static ViewRead Invalid(long entityId)
            {
                return new ViewRead(entityId, false, default, default);
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


