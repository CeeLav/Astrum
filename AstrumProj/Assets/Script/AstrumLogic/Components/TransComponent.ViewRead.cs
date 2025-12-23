using Astrum.LogicCore.Core;
using Astrum.LogicCore.ViewRead;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    public partial class TransComponent
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
                
                ViewReadFrameSync.Register<TransComponent, ViewRead>(
                    componentTypeId: ComponentTypeId,
                    createViewRead: comp => new ViewRead(
                        entityId: comp.EntityId,
                        isValid: true,
                        position: comp.Position,
                        rotation: comp.Rotation),
                    createInvalid: entityId => ViewRead.Invalid(entityId)
                );
                
                _viewReadRegistered = true;
            }
        }

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


