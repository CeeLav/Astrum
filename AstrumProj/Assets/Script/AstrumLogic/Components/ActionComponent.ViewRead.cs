using Astrum.LogicCore.Core;
using Astrum.LogicCore.ViewRead;

namespace Astrum.LogicCore.Components
{
    public partial class ActionComponent
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
                
                ViewReadFrameSync.Register<ActionComponent, ViewRead>(
                    componentTypeId: ComponentTypeId,
                    createViewRead: comp => new ViewRead(
                        entityId: comp.EntityId,
                        isValid: true,
                        currentActionId: comp.CurrentActionId,
                        currentFrame: comp.CurrentFrame),
                    createInvalid: entityId => ViewRead.Invalid(entityId)
                );
                
                _viewReadRegistered = true;
            }
        }

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


