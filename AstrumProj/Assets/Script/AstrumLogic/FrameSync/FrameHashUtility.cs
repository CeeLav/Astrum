using System;
using System.IO;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;
using TrueSync;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 帧级哈希计算工具，用于比对影子实体与权威实体的一致性
    /// </summary>
    public static class FrameHashUtility
    {
        /// <summary>
        /// 计算实体在指定帧的哈希值（通过 MemoryPack 序列化后计算 bytes 的哈希）
        /// </summary>
        public static int Compute(Entity entity, int frameId)
        {
            if (entity == null) return 0;
            
            try
            {
                // 使用 MemoryPack 序列化实体
                var memoryBuffer = ObjectPool.Instance.Fetch<MemoryBuffer>();
                try
                {
                    memoryBuffer.Seek(0, SeekOrigin.Begin);
                    memoryBuffer.SetLength(0);
                    MemoryPackHelper.Serialize(entity, memoryBuffer);
                    memoryBuffer.Seek(0, SeekOrigin.Begin);
                    
                    // 计算序列化后的 bytes 的哈希
                    byte[] buffer = memoryBuffer.GetBuffer();
                    int length = (int)memoryBuffer.Length;
                    
                    if (buffer == null || length == 0)
                    {
                        return 0;
                    }
                    
                    // 使用与 FrameBuffer 相同的哈希计算方法
                    long hash = buffer.Hash(0, length);
                    return (int)hash;
                }
                finally
                {
                    ObjectPool.Instance.Recycle(memoryBuffer);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Warning($"计算实体哈希失败: {ex.Message}", "FrameHashUtility.Compute");
                return 0;
            }
        }

        /// <summary>
        /// 获取实体的位置信息（用于调试输出）
        /// </summary>
        public static string GetPositionInfo(Entity entity)
        {
            if (entity == null) return "null";
            
            var trans = entity.GetComponent<TransComponent>();
            if (trans != null)
            {
                return $"Position=({trans.Position.x.AsFloat():F2}, {trans.Position.y.AsFloat():F2}, {trans.Position.z.AsFloat():F2})";
            }
            
            return "No TransComponent";
        }
    }
}

