using MemoryPack;
using Astrum.CommonBase;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 动作命令 - 从输入映射到动作的中间层
    /// 支持对象池复用，减少 GC 分配
    /// </summary>
    [MemoryPackable]
    public partial class ActionCommand : IPool
    {
        /// <summary>
        /// 对象是否来自对象池（IPool 接口必需成员）
        /// </summary>
        [MemoryPackIgnore]
        public bool IsFromPool { get; set; }
        
        /// <summary>命令名称</summary>
        public string CommandName { get; set; } = string.Empty;
        
        /// <summary>有效帧数</summary>
        public int ValidFrames { get; set; } = 0;
        
        /// <summary>目标位置X（Q31.32 定点数）</summary>
        public long TargetPositionX { get; set; } = 0;
        
        /// <summary>目标位置Z（Q31.32 定点数）</summary>
        public long TargetPositionZ { get; set; } = 0;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ActionCommand()
        {
        }
        
        /// <summary>
        /// 便捷构造函数
        /// </summary>
        public ActionCommand(string commandName, int validFrames)
            : this(commandName, validFrames, 0, 0)
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public ActionCommand(string commandName, int validFrames, long targetPositionX, long targetPositionZ)
        {
            CommandName = commandName ?? string.Empty;
            ValidFrames = validFrames;
            TargetPositionX = targetPositionX;
            TargetPositionZ = targetPositionZ;
        }
        
        /// <summary>
        /// 从对象池创建实例（性能优化）
        /// </summary>
        public static ActionCommand Create(string commandName, int validFrames, long targetPositionX = 0, long targetPositionZ = 0)
        {
            var instance = ObjectPool.Instance.Fetch<ActionCommand>();
            instance.CommandName = commandName ?? string.Empty;
            instance.ValidFrames = validFrames;
            instance.TargetPositionX = targetPositionX;
            instance.TargetPositionZ = targetPositionZ;
            return instance;
        }
        
        /// <summary>
        /// 重置状态（用于对象池复用）
        /// </summary>
        public void Reset()
        {
            CommandName = string.Empty;
            ValidFrames = 0;
            TargetPositionX = 0;
            TargetPositionZ = 0;
        }
    }
}
