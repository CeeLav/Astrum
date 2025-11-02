namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Idle 状态能力（最小实现）。
    /// </summary>
    public class IdleStateCapability : Capability
    {
        public override void Tick()
        {
            // 待机不做操作。真实实现可添加感知/巡逻切换条件。
        }
    }
}


