using System;
using System.Collections.Generic;
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
        /// 位置数据缓存（Hash -> Position数据），用于不匹配时输出原数据
        /// </summary>
        private static readonly Dictionary<int, PositionData> _positionCache = new Dictionary<int, PositionData>();
        
        /// <summary>
        /// 位置数据结构
        /// </summary>
        private struct PositionData
        {
            public long X;
            public long Y;
            public long Z;
            
            public PositionData(long x, long y, long z)
            {
                X = x;
                Y = y;
                Z = z;
            }
            
            public override string ToString()
            {
                // 直接使用 FP 类型转换 RawValue 为 float
                // TSVector 的 x, y, z 是 FP 类型，FP 有 AsFloat() 方法
                // 但这里存储的是 RawValue (long)，需要先转换为 FP
                FP fpX = FP.FromRaw(X);
                FP fpY = FP.FromRaw(Y);
                FP fpZ = FP.FromRaw(Z);
                return $"({fpX.AsFloat():F2}, {fpY.AsFloat():F2}, {fpZ.AsFloat():F2})";
            }
        }

        /// <summary>
        /// 计算实体在指定帧的哈希值（临时使用位置值相加的简单算法）
        /// </summary>
        public static int Compute(Entity entity, int frameId)
        {
            if (entity == null) return 0;
            
            try
            {
                // 最简单的 Hash 算法：使用位置值相加
                var trans = entity.GetComponent<TransComponent>();
                if (trans != null)
                {
                    // 直接使用位置的 RawValue 相加作为 Hash
                    long x = trans.Position.x.RawValue;
                    long y = trans.Position.y.RawValue;
                    long z = trans.Position.z.RawValue;
                    
                    // 简单的相加，然后转换为 int
                    long sum = x + y + z;
                    int hash = (int)(sum ^ (sum >> 32)); // 简单的混合高位和低位
                    
                    // 缓存位置数据，使用 Hash 作为 key
                    _positionCache[hash] = new PositionData(x, y, z);
                    
                    return hash;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Warning($"计算实体哈希失败: {ex.Message}", "FrameHashUtility.Compute");
                return 0;
            }
        }
        
        /// <summary>
        /// 根据哈希值获取缓存的位置数据（用于不匹配时输出原数据）
        /// </summary>
        public static string GetCachedPositionInfo(int hash)
        {
            if (_positionCache.TryGetValue(hash, out var positionData))
            {
                return positionData.ToString();
            }
            return "未知位置";
        }
        
        /// <summary>
        /// 清理过期的位置缓存（保留最近一定数量的缓存）
        /// </summary>
        public static void CleanupPositionCache(int keepCount = 100)
        {
            if (_positionCache.Count > keepCount)
            {
                // 简单清理：移除一半的缓存（保留最新的）
                var keysToRemove = new List<int>();
                int removeCount = _positionCache.Count - keepCount / 2;
                int index = 0;
                foreach (var key in _positionCache.Keys)
                {
                    if (index < removeCount)
                    {
                        keysToRemove.Add(key);
                    }
                    index++;
                }
                foreach (var key in keysToRemove)
                {
                    _positionCache.Remove(key);
                }
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

