using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.ViewRead
{
    public static class ViewReadFrameSync
    {
        public static void EndOfLogicFrame(World world)
        {
            if (world == null || world.Entities == null)
            {
                return;
            }

            var store = world.ViewReads;
            if (store == null)
            {
                return;
            }

            foreach (var entity in world.Entities.Values)
            {
                if (entity == null)
                {
                    continue;
                }

                var dirtyIds = entity.GetDirtyComponentIds();
                if (dirtyIds == null || dirtyIds.Count == 0)
                {
                    continue;
                }

                foreach (var componentTypeId in dirtyIds)
                {
                    // 只处理当前试点组件：Trans/Movement/Action
                    if (componentTypeId == TransComponent.ComponentTypeId)
                    {
                        if (entity.GetComponentById(componentTypeId) is TransComponent trans)
                        {
                            var vr = new TransComponent.ViewRead(entity.UniqueId, true, trans.Position, trans.Rotation);
                            store.WriteBack(entity.UniqueId, componentTypeId, in vr);
                        }
                        else
                        {
                            var vr = TransComponent.ViewRead.Invalid(entity.UniqueId);
                            store.WriteBack(entity.UniqueId, componentTypeId, in vr);
                        }
                    }
                    else if (componentTypeId == MovementComponent.ComponentTypeId)
                    {
                        if (entity.GetComponentById(componentTypeId) is MovementComponent move)
                        {
                            var vr = new MovementComponent.ViewRead(
                                entityId: entity.UniqueId,
                                isValid: true,
                                speed: move.Speed,
                                canMove: move.CanMove,
                                currentMovementType: move.CurrentMovementType,
                                moveDirection: move.MoveDirection);
                            store.WriteBack(entity.UniqueId, componentTypeId, in vr);
                        }
                        else
                        {
                            var vr = MovementComponent.ViewRead.Invalid(entity.UniqueId);
                            store.WriteBack(entity.UniqueId, componentTypeId, in vr);
                        }
                    }
                    else if (componentTypeId == ActionComponent.ComponentTypeId)
                    {
                        if (entity.GetComponentById(componentTypeId) is ActionComponent action)
                        {
                            var vr = new ActionComponent.ViewRead(
                                entityId: entity.UniqueId,
                                isValid: true,
                                currentActionId: action.CurrentActionId,
                                currentFrame: action.CurrentFrame);
                            store.WriteBack(entity.UniqueId, componentTypeId, in vr);
                        }
                        else
                        {
                            var vr = ActionComponent.ViewRead.Invalid(entity.UniqueId);
                            store.WriteBack(entity.UniqueId, componentTypeId, in vr);
                        }
                    }
                }

                entity.ClearDirtyComponents();
            }

            store.SwapAllIfWritten();
        }
    }
}


