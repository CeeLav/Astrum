using MemoryPack;
using Astrum.LogicCore.SkillSystem;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 移动动作扩展数据（运行时版本，支持 MemoryPack 序列化）
    /// 对应 MoveActionTable 的专属字段
    /// </summary>
    [MemoryPackable]
    public partial class MoveActionExtension
    {
        /// <summary>
        /// 基础移动速度（×1000，单位：逻辑米/秒）
        /// 从 MoveActionTable.MoveSpeed 加载
        /// </summary>
        public int MoveSpeed { get; set; } = 0;
        
        /// <summary>
        /// 根节点位移数据（运行时数据，从 MoveActionTable 加载）
        /// </summary>
        [MemoryPackAllowSerialize]
        public AnimationRootMotionData RootMotionData { get; set; }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public MoveActionExtension()
        {
            RootMotionData = new AnimationRootMotionData();
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public MoveActionExtension(int moveSpeed, AnimationRootMotionData rootMotionData)
        {
            MoveSpeed = moveSpeed;
            RootMotionData = rootMotionData ?? new AnimationRootMotionData();
        }
    }
}

