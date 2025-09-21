using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Core;
using MemoryPack;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 逻辑更新器，负责更新游戏逻辑
    /// </summary>
    [MemoryPackable]
    public partial class LSUpdater
    {
        /// <summary>
        /// 固定时间步长
        /// </summary>
        public float FixedDeltaTime { get; set; } = 1f / 60f; // 默认60FPS
        
        /// <summary>
        /// 更新单个世界
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="deltaTime">时间差</param>
        public void UpdateWorld(World world)
        {
            if (world == null) return;

            // 获取需要更新的实体
            var entities = GetEntitiesForUpdate(world);

            // 更新所有实体的能力
            foreach (var entity in entities)
            {
                UpdateEntityCapabilities(entity);
            }

            // 更新世界状态（不调用world.Update避免递归）
            //world.DeltaTime = deltaTime;
            //world.TotalTime += deltaTime;
        }



        /// <summary>
        /// 获取指定世界中需要更新的实体
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <returns>需要更新的实体列表</returns>
        public List<Entity> GetEntitiesForUpdate(World world)
        {
            var entities = new List<Entity>();

            if (world == null) return entities;

            foreach (var entity in world.Entities.Values)
            {
                if (entity.IsActive && !entity.IsDestroyed)
                {
                    entities.Add(entity);
                }
            }

            // 按优先级排序（如果实体有优先级系统的话）
            // entities.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            return entities;
        }

        /// <summary>
        /// 更新实体的能力
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="deltaTime">时间差</param>
        private void UpdateEntityCapabilities(Entity entity)
        {
            if (entity == null || !entity.IsActive || entity.IsDestroyed) return;

            // 按优先级排序能力
            var sortedCapabilities = entity.Capabilities
                .Where(c => c.IsActive && c.CanExecute())
                .OrderByDescending(c => c.Priority)
                .ToList();

            // 执行所有能力的Tick方法
            foreach (var capability in sortedCapabilities)
            {
                try
                {
                    capability.Tick();
                }
                catch (Exception ex)
                {
                    // 记录错误但不中断其他能力的执行
                    Console.WriteLine($"Error updating capability {capability.Name} for entity {entity.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 设置更新频率
        /// </summary>
        /// <param name="fps">帧率</param>
        public void SetUpdateRate(int fps)
        {
            if (fps > 0)
            {
                FixedDeltaTime = 1f / fps;
            }
        }

        /// <summary>
        /// 获取当前更新频率
        /// </summary>
        /// <returns>当前FPS</returns>
        public int GetUpdateRate()
        {
            return FixedDeltaTime > 0 ? (int)(1f / FixedDeltaTime) : 0;
        }

        /// <summary>
        /// 暂停更新
        /// </summary>
        public void Pause()
        {
            // 可以添加暂停逻辑
        }

        /// <summary>
        /// 恢复更新
        /// </summary>
        public void Resume()
        {
            // 可以添加恢复逻辑
        }
    }
}
