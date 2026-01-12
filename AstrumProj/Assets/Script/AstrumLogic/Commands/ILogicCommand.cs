using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Commands
{
    /// <summary>
    /// 逻辑线程命令接口
    /// </summary>
    public interface ILogicCommand
    {
        void Execute(World world);
    }
}




