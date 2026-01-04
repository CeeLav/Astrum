using System;
using System.Threading.Tasks;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Factories;
using cfg;

namespace Astrum.LogicCore.Commands
{
    /// <summary>
    /// 创建实体命令（支持两种方式：EntityConfigId 或 Archetype + EntityCreationParams）
    /// </summary>
    public class CreateEntityCommand : ILogicCommand
    {
        // 方式1：通过 EntityConfigId 创建（主线程使用）
        public int? EntityConfigId { get; set; }
        public TaskCompletionSource<long>? Tcs { get; set; }
        
        // 方式2：通过 Archetype + EntityCreationParams 创建（逻辑线程内部使用，替代 QueueCreateEntity）
        public EArchetype? Archetype { get; set; }
        public EntityCreationParams? CreationParams { get; set; }
        public Action<Entity>? PostCreateAction { get; set; }

        public void Execute(World world)
        {
            try
            {
                Entity entity = null;
                
                // 方式1：通过 EntityConfigId 创建
                if (EntityConfigId.HasValue)
                {
                    entity = world.CreateEntity(EntityConfigId.Value);
                    if (entity != null && Tcs != null)
                    {
                        Tcs.SetResult(entity.UniqueId);
                    }
                    else if (Tcs != null)
                    {
                        Tcs.SetResult(-1);
                    }
                }
                /*
                // 方式2：通过 Archetype + EntityCreationParams 创建
                else if (Archetype.HasValue && CreationParams != null)
                {
                    entity = EntityFactory.Instance.CreateByArchetype(
                        Archetype.Value,
                        CreationParams,
                        world);
                    
                    if (entity != null)
                    {
                        PostCreateAction?.Invoke(entity);
                        world.PublishEntityCreatedEvent(entity);
                    }
                }*/
            }
            catch (Exception ex)
            {
                if (Tcs != null)
                {
                    Tcs.SetException(ex);
                }
                else
                {
                    throw; // 逻辑线程内部使用，重新抛出异常
                }
            }
        }
    }
}


