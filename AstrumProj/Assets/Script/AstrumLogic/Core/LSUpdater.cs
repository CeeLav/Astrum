using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
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

            using (new Astrum.CommonBase.ProfileScope("LSUpdater.UpdateWorld"))
            {
                // 使用 CapabilitySystem 统一调度（按 Capability 遍历，每个 Capability 更新所有拥有它的实体）
                if (world.CapabilitySystem != null)
                {
                    // 记录 CapabilitySystem.Update 调用（仅当有实体有非空输入时）
                    bool hasNonEmptyInput = false;
                    if (world.Entities != null)
                    {
                        foreach (var entity in world.Entities.Values)
                        {
                            if (entity != null && !entity.IsDestroyed)
                            {
                                var inputComp = entity.GetComponent<LSInputComponent>();
                                if (inputComp?.CurrentInput != null && (inputComp.CurrentInput.MoveX != 0 || inputComp.CurrentInput.MoveY != 0))
                                {
                                    hasNonEmptyInput = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (hasNonEmptyInput)
                    {
                        //ASLogger.Instance.Info($"[LSUpdater.UpdateWorld] 调用 CapabilitySystem.Update | World.CurFrame={world.CurFrame}", "LSUpdater.UpdateWorld");
                    }
                    
                    world.CapabilitySystem.Update(world);
                }

                // 更新世界状态（不调用world.Update避免递归）
                //world.DeltaTime = deltaTime;
                //world.TotalTime += deltaTime;
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
