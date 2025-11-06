namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 事件基接口
    /// 所有事件都应实现此接口，以支持统一的事件处理
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// true: 无论 Capability 是否激活都会处理该事件（可用于主动激活 Capability）
        /// false: 只有在 Capability 已激活时才处理（默认行为）
        /// </summary>
        bool TriggerWhenInactive { get; }
    }
}

