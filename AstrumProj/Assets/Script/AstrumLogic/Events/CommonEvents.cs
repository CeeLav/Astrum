using TrueSync;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 常用事件定义
    /// 
    /// 这些事件用于 Client/View 线程向 Logic 线程发送修改请求
    /// 所有事件数据必须是值类型（struct），避免 GC 和线程安全问题
    /// </summary>

    #region Transform 相关事件

    /// <summary>
    /// 设置实体位置事件
    /// </summary>
    public struct SetPositionEvent : IEvent
    {
        public TSVector Position;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }

    /// <summary>
    /// 设置实体旋转事件
    /// </summary>
    public struct SetRotationEvent : IEvent
    {
        public TSQuaternion Rotation;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }

    /// <summary>
    /// 设置实体缩放事件
    /// </summary>
    public struct SetScaleEvent : IEvent
    {
        public TSVector Scale;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }

    #endregion

    #region Stats 相关事件

    /// <summary>
    /// 设置生命值事件
    /// </summary>
    public struct SetHealthEvent : IEvent
    {
        public FP Health;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }

    /// <summary>
    /// 设置等级事件
    /// </summary>
    public struct SetLevelEvent : IEvent
    {
        public int Level;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }

    /// <summary>
    /// 设置怪物信息事件
    /// </summary>
    public struct SetMonsterInfoEvent : IEvent
    {
        public int MonsterId;
        public int Level;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }

    #endregion

    #region Component 通用事件

    /// <summary>
    /// 加载组件数据事件（通用序列化）
    /// 
    /// 用于存档加载等场景，通过序列化数据恢复组件状态
    /// </summary>
    public struct LoadComponentDataEvent : IEvent
    {
        /// <summary>
        /// 组件类型 ID
        /// </summary>
        public int ComponentTypeId;

        /// <summary>
        /// 序列化后的组件数据
        /// 注意：byte[] 是引用类型，但在事件中传递数据副本是安全的
        /// 发送方应确保不再修改该数组
        /// </summary>
        public byte[] SerializedData;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }

    /// <summary>
    /// 重置组件事件
    /// 
    /// 将组件重置为默认状态
    /// </summary>
    public struct ResetComponentEvent : IEvent
    {
        /// <summary>
        /// 组件类型 ID
        /// </summary>
        public int ComponentTypeId;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }

    #endregion
}

