using Astrum.LogicCore.Core;
using TrueSync;

namespace Astrum.LogicCore.Physics
{
    /// <summary>
    /// 射线检测返回数据
    /// </summary>
    public struct RaycastHitInfo
    {
        public Entity Entity;
        public FP Distance;
        public TSVector Point;
        public TSVector Normal;
    }
}
