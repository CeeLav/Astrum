using System;
using System.Threading.Tasks;
using Astrum.CommonBase;
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
            ASLogger.Instance.Info($"CreateEntityCommand: [Execute] 开始执行 - EntityConfigId: {EntityConfigId}, 当前线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            try
            {
                Entity entity = null;
                
                // 方式1：通过 EntityConfigId 创建
                if (EntityConfigId.HasValue)
                {
                    ASLogger.Instance.Info($"CreateEntityCommand: [Execute] 通过EntityConfigId创建实体 - EntityConfigId: {EntityConfigId.Value}");
                    entity = world.CreateEntity(EntityConfigId.Value);
                    ASLogger.Instance.Info($"CreateEntityCommand: [Execute] CreateEntity调用完成 - Entity: {(entity != null ? $"存在, ID={entity.UniqueId}" : "null")}");
                    
                    if (entity != null && Tcs != null)
                    {
                        ASLogger.Instance.Info($"CreateEntityCommand: [Execute] 设置Task结果 - EntityId: {entity.UniqueId}");
                        Tcs.SetResult(entity.UniqueId);
                        ASLogger.Instance.Info($"CreateEntityCommand: [Execute] Task结果已设置");
                    }
                    else if (Tcs != null)
                    {
                        ASLogger.Instance.Warning($"CreateEntityCommand: [Execute] 实体创建失败，设置结果为-1");
                        Tcs.SetResult(-1);
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"CreateEntityCommand: [Execute] Tcs为null，无法设置结果");
                    }
                }
                else
                {
                    ASLogger.Instance.Warning($"CreateEntityCommand: [Execute] EntityConfigId未设置");
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


