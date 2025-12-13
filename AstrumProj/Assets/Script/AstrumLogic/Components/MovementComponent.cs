using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Capabilities;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 移动类型枚举
    /// </summary>
    public enum MovementType
    {
        /// <summary>
        /// 无移动
        /// </summary>
        None,
        /// <summary>
        /// 玩家输入的主动移动
        /// </summary>
        PlayerInput,
        /// <summary>
        /// 技能位移
        /// </summary>
        SkillDisplacement,
        /// <summary>
        /// 被动位移（如Knockback）
        /// </summary>
        PassiveDisplacement
    }

    /// <summary>
    /// 位置历史记录（帧号 + 位置）
    /// </summary>
    public struct PositionHistory
    {
        public int Frame;
        public TSVector Position;
        
        public PositionHistory(int frame, TSVector position)
        {
            Frame = frame;
            Position = position;
        }
    }

    /// <summary>
    /// 移动组件，存储移动相关的数据
    /// </summary>
    [MemoryPackable]
    public partial class MovementComponent : BaseComponent
    {
        /// <summary>
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<MovementComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
        /// <summary>
        /// 移动速度
        /// </summary>
        public FP Speed { get; set; } = FP.One;

        /// <summary>
        /// 基准移动速度（通常来自配置）
        /// </summary>
        [MemoryPackInclude]
        public FP BaseSpeed { get; private set; } = FP.One;

        /// <summary>
        /// 是否可以移动
        /// </summary>
        public bool CanMove { get; set; } = true;

        /// <summary>
        /// 当前移动类型
        /// </summary>
        public MovementType CurrentMovementType { get; set; } = MovementType.None;

        /// <summary>
        /// 记录最近一次发生位移的逻辑帧号（World.CurFrame）。
        /// 用于在不新增“每帧重置能力”的前提下，由 MovementCapability 在本帧末尾结算 CurrentMovementType。
        /// </summary>
        public int LastMoveFrame { get; set; } = -1;

        /// <summary>
        /// 位置历史缓存（过去10帧的位置信息）
        /// 运行时数据，不序列化
        /// </summary>
        [MemoryPackIgnore]
        private Queue<PositionHistory> _positionHistory = new Queue<PositionHistory>();

        /// <summary>
        /// 位置历史缓存的最大数量
        /// </summary>
        private const int MAX_POSITION_HISTORY = 10;

        [MemoryPackConstructor]
        public MovementComponent() : base() { }

        public MovementComponent(FP speed) : base()
        {
            Speed = speed;
            BaseSpeed = speed;
        }

        /// <summary>
        /// 立即停止移动
        /// </summary>
        public void Stop()
        {
            Speed = FP.Zero;
        }

        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="speed">速度值</param>
        public void SetSpeed(FP speed)
        {
            Speed = TSMath.Max(FP.Zero, speed);
        }

        /// <summary>
        /// 设置基准移动速度（通常来自配置）
        /// </summary>
        public void SetBaseSpeed(FP speed)
        {
            BaseSpeed = TSMath.Max(FP.Zero, speed);
        }

        /// <summary>
        /// 获取基准移动速度
        /// </summary>
        public FP GetBaseSpeed()
        {
            return BaseSpeed;
        }

        /// <summary>
        /// 获取当前速度与基准速度的倍率（基准为0时返回1）
        /// </summary>
        public FP GetSpeedMultiplier()
        {
            return BaseSpeed > FP.Zero ? Speed / BaseSpeed : FP.One;
        }

        /// <summary>
        /// 获取当前速度
        /// </summary>
        /// <returns>当前速度值</returns>
        public FP GetSpeed()
        {
            return Speed;
        }
        
        /// <summary>
        /// 记录当前位置到历史缓存
        /// </summary>
        /// <param name="frame">当前帧号</param>
        /// <param name="position">当前位置</param>
        public void RecordPosition(int frame, TSVector position)
        {
            _positionHistory.Enqueue(new PositionHistory(frame, position));
            
            // 保持最多 MAX_POSITION_HISTORY 条记录
            while (_positionHistory.Count > MAX_POSITION_HISTORY)
            {
                _positionHistory.Dequeue();
            }
        }

        /// <summary>
        /// 获取位置历史（按帧号排序）
        /// </summary>
        /// <returns>位置历史列表</returns>
        public List<PositionHistory> GetPositionHistory()
        {
            return _positionHistory.OrderBy(h => h.Frame).ToList();
        }

        /// <summary>
        /// 获取位置历史的字符串表示（用于日志输出）
        /// </summary>
        /// <returns>格式化的位置历史字符串</returns>
        public string GetPositionHistoryString()
        {
            var history = GetPositionHistory();
            if (history.Count == 0)
            {
                return "无历史记录";
            }

            var lines = history.Select(h => 
                $"frame={h.Frame}, pos=({h.Position.x.AsFloat():F2}, {h.Position.y.AsFloat():F2}, {h.Position.z.AsFloat():F2})");
            return string.Join(" | ", lines);
        }

        /// <summary>
        /// 重置 MovementComponent 状态（用于对象池回收）
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Speed = FP.One;
            BaseSpeed = FP.One;
            CanMove = true;
            CurrentMovementType = MovementType.None;
            LastMoveFrame = -1;
            _positionHistory?.Clear();
        }
    }
}
