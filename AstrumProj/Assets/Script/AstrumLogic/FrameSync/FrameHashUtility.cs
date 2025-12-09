using System;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using TrueSync;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 帧级哈希计算工具，用于比对影子实体与权威实体的一致性
    /// </summary>
    public static class FrameHashUtility
    {
        /// <summary>
        /// 计算实体在指定帧的哈希值（关键字段）
        /// 临时改为只比较位置
        /// </summary>
        public static int Compute(Entity entity, int frameId)
        {
            if (entity == null) return 0;
            
            var hash = new HashCode();
            
            // 位置/朝向（TransComponent）- 临时只比较位置
            var trans = entity.GetComponent<TransComponent>();
            if (trans != null)
            {
                hash.Add(trans.Position.x.RawValue);
                hash.Add(trans.Position.y.RawValue);
                hash.Add(trans.Position.z.RawValue);
            }
            
            return hash.ToHashCode();
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

