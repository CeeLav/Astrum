using System;
using TrueSync;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 视图事件类型枚举
    /// 定义了视图层可以处理的所有事件类型
    /// 注意：ViewEvent 是纯数据结构，位于 AstrumLogic 项目中以便逻辑层（Entity）引用
    /// 视图层（EntityView）通过引用 AstrumLogic 来访问这些定义
    /// </summary>
    public enum ViewEventType
    {
        /// <summary>
        /// 实体创建事件 - 当 Entity 在逻辑层创建时触发
        /// Stage 级别事件，由 Stage 直接处理
        /// </summary>
        EntityCreated,
        
        /// <summary>
        /// 实体销毁事件 - 当 Entity 在逻辑层销毁时触发
        /// Stage 级别事件，由 Stage 直接处理
        /// </summary>
        EntityDestroyed,
        
        /// <summary>
        /// 子原型变化事件 - 当 Entity 添加或移除子原型时触发
        /// EntityView 级别事件，由 EntityView 处理
        /// </summary>
        SubArchetypeChanged,
        
        /// <summary>
        /// 世界回滚事件 - 当 World 执行状态回滚时触发
        /// Stage 级别事件，由 Stage 直接处理
        /// </summary>
        WorldRollback,
        
        /// <summary>
        /// 自定义视图事件 - 用于 ViewComponent 的自定义事件
        /// ViewComponent 级别事件，通过事件注册机制分发
        /// 实际事件类型由 EventData 确定
        /// </summary>
        CustomViewEvent
        
        // 注意：不包含 ComponentDirty
        // 脏组件同步是独立机制，通过 Stage.SyncDirtyComponents() 处理
    }
    
    // ========== ViewComponent 自定义事件数据 ==========
    
    /// <summary>
    /// VFX 触发事件数据
    /// 用于在视图层播放视觉特效
    /// </summary>
    public struct VFXTriggerEvent
    {
        /// <summary>
        /// 特效资源路径
        /// </summary>
        public string ResourcePath;
        
        /// <summary>
        /// 位置偏移（相对于实体位置）
        /// </summary>
        public TSVector PositionOffset;
        
        /// <summary>
        /// 旋转
        /// </summary>
        public TSVector Rotation;
        
        /// <summary>
        /// 缩放
        /// </summary>
        public float Scale;
        
        /// <summary>
        /// 播放速度
        /// </summary>
        public float PlaybackSpeed;
        
        /// <summary>
        /// 是否跟随角色
        /// </summary>
        public bool FollowCharacter;
        
        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool Loop;
    }
    
    /// <summary>
    /// 视图事件数据结构
    /// 封装了视图层事件的所有信息，用于在 Entity 和 EntityView 之间传递事件
    /// 注意：ViewEvent 是纯数据结构，位于 AstrumLogic 项目中以便逻辑层（Entity）引用
    /// 视图层（EntityView）通过引用 AstrumLogic 来访问这些定义
    /// </summary>
    public struct ViewEvent
    {
        /// <summary>
        /// 事件类型
        /// </summary>
        public ViewEventType EventType;
        
        /// <summary>
        /// 事件数据（可选）
        /// 根据 EventType 的不同，可能包含不同类型的数据对象
        /// 例如：SubArchetypeChanged 事件可能包含子原型的类型信息
        /// </summary>
        public object EventData;
        
        /// <summary>
        /// 事件发生的帧号
        /// 用于调试和日志记录
        /// </summary>
        public int Frame;
        
        /// <summary>
        /// 创建视图事件
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="eventData">事件数据（可选）</param>
        /// <param name="frame">帧号</param>
        public ViewEvent(ViewEventType eventType, object eventData = null, int frame = 0)
        {
            EventType = eventType;
            EventData = eventData;
            Frame = frame;
        }
        
        /// <summary>
        /// 获取事件的字符串表示，用于日志记录
        /// </summary>
        public override string ToString()
        {
            return $"ViewEvent(Type={EventType}, Frame={Frame}, HasData={EventData != null})";
        }
    }
}

