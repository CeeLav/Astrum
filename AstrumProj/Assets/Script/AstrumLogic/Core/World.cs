using System;
using System.Collections.Generic;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 游戏世界类，管理所有实体和世界级别的逻辑
    /// </summary>
    public class World
    {
        /// <summary>
        /// 世界唯一标识符
        /// </summary>
        public int WorldId { get; set; }

        /// <summary>
        /// 世界名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 世界创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 所有实体的字典（EntityId -> Entity）
        /// </summary>
        public Dictionary<long, Entity> Entities { get; private set; } = new Dictionary<long, Entity>();

        /// <summary>
        /// 当前帧时间差
        /// </summary>
        public float DeltaTime { get; set; }

        /// <summary>
        /// 总运行时间
        /// </summary>
        public float TotalTime { get; set; }

        /// <summary>
        /// 世界更新器
        /// </summary>
        public LSUpdater? Updater { get; set; }

        /// <summary>
        /// 创建新实体
        /// </summary>
        /// <param name="name">实体名称</param>
        /// <returns>创建的实体</returns>
        public T CreateEntity<T>(string name) where T : Entity, new()
        {
            var entity = EntityFactory.Instance.CreateEntity<T>(name);
            Entities.Add(entity.UniqueId, entity);
            return entity;
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        public void DestroyEntity(long entityId)
        {
            if (Entities.TryGetValue(entityId, out var entity))
            {
                entity.Destroy();
                Entities.Remove(entityId);
            }
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>实体对象，如果不存在返回null</returns>
        public Entity? GetEntity(long entityId)
        {
            return Entities.TryGetValue(entityId, out var entity) ? entity : null;
        }

        /// <summary>
        /// 应用输入到实体
        /// </summary>
        /// <param name="frameInputs">单帧输入数据</param>
        public void ApplyInputsToEntities(OneFrameInputs frameInputs)
        {
            foreach (var entity in Entities.Values)
            {
                if (entity.IsActive && !entity.IsDestroyed)
                {
                    // 查找实体的输入组件并应用输入
                    var inputComponent = entity.GetComponent<LSInputComponent>();
                    if (inputComponent != null)
                    {
                        var input = frameInputs.GetInput(inputComponent.PlayerId);
                        if (input != null)
                        {
                            entity.ApplyInput(input);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新世界状态
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public void Update(float deltaTime)
        {
            DeltaTime = deltaTime;
            TotalTime += deltaTime;

            Updater?.UpdateWorld(this, deltaTime);
        }

        /// <summary>
        /// 初始化世界
        /// </summary>
        public virtual void Initialize()
        {
            CreationTime = DateTime.Now;
            TotalTime = 0f;
        }

        /// <summary>
        /// 清理世界资源
        /// </summary>
        public virtual void Cleanup()
        {
            foreach (var entity in Entities.Values)
            {
                entity.Destroy();
            }
            Entities.Clear();
        }
    }
}
