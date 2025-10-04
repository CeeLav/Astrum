using MemoryPack;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 动作命令 - 触发动作的输入信息
    /// </summary>
    [MemoryPackable]
    public partial class ActionCommand
    {
        /// <summary>命令名称</summary>
        public string CommandName { get; set; } = string.Empty;
        
        /// <summary>有效帧数</summary>
        public int ValidFrames { get; set; } = 0;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ActionCommand()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public ActionCommand(string commandName, int validFrames)
        {
            CommandName = commandName ?? string.Empty;
            ValidFrames = validFrames;
        }
    }
}
