using System;
using Astrum.LogicCore.Capabilities;
using MemoryPack;
using TrueSync;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Components
{
    [MemoryPackable]
    public partial class AIStateMachineComponent : BaseComponent
    {
        /// <summary>
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<AIStateMachineComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
        public string CurrentState { get; set; } = string.Empty;
        public string NextState { get; set; } = string.Empty;
        public string LastState { get; set; } = string.Empty;
        public int LastSwitchFrame { get; set; } = 0;

        // 当前追逐目标（实体ID，-1 表示无目标）
        public long CurrentTargetId { get; set; } = -1;

        // 随机漫游目标点（世界坐标）
        public TSVector WanderTarget { get; set; } = TSVector.zero;

        // 下一次允许触发随机漫游的帧号（用于去抖）
        public int NextWanderFrame { get; set; } = 0;

		// 攻击冷却：下一次允许攻击的帧、攻击间隔帧数（默认 20 帧约1秒@20fps）
		public int NextAttackFrame { get; set; } = 0;
		public int AttackIntervalFrames { get; set; } = 20;

        public override void OnAttachToEntity(Entity entity)
        {
            // 默认状态置空，由调度能力决定首次状态
        }
        
        /// <summary>
        /// 重置 AIStateMachineComponent 状态（用于对象池回收）
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            CurrentState = string.Empty;
            NextState = string.Empty;
            LastState = string.Empty;
            LastSwitchFrame = 0;
            CurrentTargetId = -1;
            WanderTarget = TSVector.zero;
            NextWanderFrame = 0;
            NextAttackFrame = 0;
            AttackIntervalFrames = 20;
        }
    }
}


