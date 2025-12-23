using Astrum.LogicCore.Core;
using Astrum.LogicCore.ViewRead;
using Astrum.LogicCore.SkillSystem;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    public partial class ProjectileComponent
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
                
                ViewReadFrameSync.Register<ProjectileComponent, ViewRead>(
                    componentTypeId: ComponentTypeId,
                    createViewRead: comp => new ViewRead(
                        entityId: comp.EntityId,
                        isValid: true,
                        projectileId: comp.ProjectileId,
                        currentVelocity: comp.CurrentVelocity,
                        launchDirection: comp.LaunchDirection,
                        trajectoryType: comp.TrajectoryType,
                        socketName: comp.SocketName,
                        isMarkedForDestroy: comp.IsMarkedForDestroy),
                    createInvalid: entityId => ViewRead.Invalid(entityId)
                );
                
                _viewReadRegistered = true;
            }
        }

        public readonly struct ViewRead
        {
            public readonly long EntityId;
            public readonly bool IsValid;
            public readonly int ProjectileId;
            public readonly TSVector CurrentVelocity;
            public readonly TSVector LaunchDirection;
            public readonly TrajectoryType TrajectoryType;
            public readonly string SocketName;
            public readonly bool IsMarkedForDestroy;

            public ViewRead(long entityId, bool isValid, int projectileId, TSVector currentVelocity, 
                TSVector launchDirection, TrajectoryType trajectoryType, string socketName, bool isMarkedForDestroy)
            {
                EntityId = entityId;
                IsValid = isValid;
                ProjectileId = projectileId;
                CurrentVelocity = currentVelocity;
                LaunchDirection = launchDirection;
                TrajectoryType = trajectoryType;
                SocketName = socketName ?? string.Empty;
                IsMarkedForDestroy = isMarkedForDestroy;
            }

            public static ViewRead Invalid(long entityId)
            {
                return new ViewRead(entityId, false, 0, default, default, TrajectoryType.Linear, string.Empty, false);
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

