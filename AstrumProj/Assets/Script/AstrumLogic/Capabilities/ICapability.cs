using System.Collections.Generic;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Capability 核心接口
    /// 所有 Capability 必须实现此接口
    /// </summary>
    public interface ICapability
    {
        /// <summary>
        /// 类型 ID（基于 TypeHash 的稳定哈希值）
        /// </summary>
        int TypeId { get; }
        
        /// <summary>
        /// 执行优先级（数值越大越优先）
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// 此 Capability 的 Tag 集合
        /// 用于支持基于 Tag 的批量禁用
        /// </summary>
        IReadOnlyCollection<CapabilityTag> Tags { get; }
        
        /// <summary>
        /// 是否跟踪激活持续时间（ActiveDuration）
        /// 如果为 true，系统会在每帧更新 ActiveDuration
        /// </summary>
        bool TrackActiveDuration { get; }
        
        /// <summary>
        /// 是否跟踪禁用持续时间（DeactiveDuration）
        /// 如果为 true，系统会在每帧更新 DeactiveDuration
        /// </summary>
        bool TrackDeactiveDuration { get; }
        
        /// <summary>
        /// 判定此 Capability 是否应该激活
        /// 每帧调用，用于检查激活条件（如：所需组件是否存在、外部条件是否满足）
        /// </summary>
        bool ShouldActivate(Entity entity);
        
        /// <summary>
        /// 判定此 Capability 是否应该停用
        /// 每帧调用，用于检查停用条件（如：所需组件被移除、外部条件不再满足）
        /// </summary>
        bool ShouldDeactivate(Entity entity);
        
        /// <summary>
        /// 首次挂载到实体时调用（在 Archetype 装配时）
        /// 用于初始化 Entity 上的状态数据
        /// 注意：此方法替代了原来的 Initialize() 方法
        /// </summary>
        void OnAttached(Entity entity);
        
        /// <summary>
        /// 从实体上完全卸载时调用（在 SubArchetype 卸载时）
        /// 用于清理 Entity 上的状态数据
        /// </summary>
        void OnDetached(Entity entity);
        
        /// <summary>
        /// 激活时调用（当 IsActive 从 false 变为 true）
        /// </summary>
        void OnActivate(Entity entity);
        
        /// <summary>
        /// 停用时调用（当 IsActive 从 true 变为 false）
        /// </summary>
        void OnDeactivate(Entity entity);
        
        /// <summary>
        /// 每帧更新（仅在激活状态下调用）
        /// </summary>
        void Tick(Entity entity);
    }
}

