using Astrum.LogicCore.Stats;
using System.Collections.Generic;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Capabilities;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 状态组件 - 存储实体的各种状态标志
    /// </summary>
    [MemoryPackable]
    public partial class StateComponent : BaseComponent
    {
        /// <summary>
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<StateComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
        /// <summary>状态字典（使用bool标记状态是否激活）</summary>
        public Dictionary<StateType, bool> States { get; set; } = new Dictionary<StateType, bool>();
        
        [MemoryPackConstructor]
        public StateComponent(Dictionary<StateType, bool> states) : base()
        {
            States = states ?? new Dictionary<StateType, bool>();
        }
        
        public StateComponent() : base() { }

        public override void OnAttachToEntity(Entity entity)
        {
            base.OnAttachToEntity(entity);
            //Set(StateType.DEAD, false);
        }

        /// <summary>获取状态</summary>
        public bool Get(StateType type)
        {
            return States.TryGetValue(type, out var value) && value;
        }
        
        /// <summary>设置状态</summary>
        public void Set(StateType type, bool value)
        {
            States[type] = value;
        }
        
        /// <summary>清空所有状态</summary>
        public void Clear()
        {
            States.Clear();
        }
        
        // ===== 辅助方法 =====
        
        /// <summary>是否可以移动</summary>
        public bool CanMove()
        {
            return !Get(StateType.STUNNED) 
                && !Get(StateType.FROZEN) 
                && !Get(StateType.KNOCKED_BACK) 
                && !Get(StateType.DEAD);
        }
        
        /// <summary>是否可以攻击</summary>
        public bool CanAttack()
        {
            return !Get(StateType.STUNNED) 
                && !Get(StateType.FROZEN) 
                && !Get(StateType.DISARMED) 
                && !Get(StateType.DEAD);
        }
        
        /// <summary>是否可以释放技能</summary>
        public bool CanCastSkill()
        {
            return !Get(StateType.STUNNED) 
                && !Get(StateType.FROZEN) 
                && !Get(StateType.SILENCED) 
                && !Get(StateType.DEAD);
        }
        
        /// <summary>是否可以受到伤害</summary>
        public bool CanTakeDamage()
        {
            return !Get(StateType.INVINCIBLE) && !Get(StateType.DEAD);
        }
        
        /// <summary>
        /// 重置 StateComponent 状态（用于对象池回收）
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            States.Clear();
        }
    }
}

