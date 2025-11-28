using MemoryPack;
using System.Collections.Generic;
using TrueSync;
using Astrum.LogicCore.SkillSystem;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 动作信息 - 动作系统的核心数据结构（运行时数据）
    /// 由表格数据和运行时数据共同组装而成
    /// 使用 Extension 模式存储不同类型动作的扩展数据
    /// </summary>
    [MemoryPackable]
    public partial class ActionInfo
    {
        /// <summary>动作唯一标识符</summary>
        public int Id { get; set; } = 0;
        
        /// <summary>动作类型标签（如"受伤动作"、"攻击动作"）</summary>
        public string Catalog { get; set; } = string.Empty;
        
        /// <summary>取消信息 - 此动作可以取消其他动作的依据</summary>
        [MemoryPackAllowSerialize]
        public List<CancelTag> CancelTags { get; set; } = new();
        
        /// <summary>被取消信息 - 此动作可以被其他动作取消的依据</summary>
        [MemoryPackAllowSerialize]
        public List<BeCancelledTag> BeCancelledTags { get; set; } = new();
        
        /// <summary>临时被取消信息 - 动作过程中临时开启的取消点</summary>
        [MemoryPackAllowSerialize]
        public List<TempBeCancelledTag> TempBeCancelledTags { get; set; } = new();
        
        /// <summary>动作命令 - 触发此动作的输入信息</summary>
        [MemoryPackAllowSerialize]
        public List<ActionCommand> Commands { get; set; } = new();
        
        /// <summary>自然下一个动作ID</summary>
        public int AutoNextActionId { get; set; } = 0;
        
        /// <summary>切换到此动作时是否保持播放动画</summary>
        public bool KeepPlayingAnim { get; set; } = false;
        
        /// <summary>是否自动终止动作</summary>
        public bool AutoTerminate { get; set; } = false;
        
        /// <summary>基础优先级</summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>动作持续时间（帧数）</summary>
        public int Duration { get; set; } = 0;
        
        /// <summary>
        /// 移动动作扩展数据（仅当 ActionType 为 "Move" 时非空）
        /// </summary>
        [MemoryPackAllowSerialize]
        public MoveActionExtension? MoveExtension { get; set; }
        
        /// <summary>
        /// 技能动作扩展数据（仅当 ActionType 为 "Skill" 时非空）
        /// </summary>
        [MemoryPackAllowSerialize]
        public SkillActionExtension? SkillExtension { get; set; }
        
        /// <summary>
        /// 当前动作对应的动画播放速度倍率（默认1，受逻辑速度影响）
        /// </summary>
        public float AnimationSpeedMultiplier { get; set; } = 1f;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ActionInfo()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public ActionInfo(int id, string catalog, List<CancelTag> cancelTags, List<BeCancelledTag> beCancelledTags, 
            List<TempBeCancelledTag> tempBeCancelledTags, List<ActionCommand> commands, int autoNextActionId, 
            bool keepPlayingAnim, bool autoTerminate, int priority, int duration,
            MoveActionExtension? moveExtension, SkillActionExtension? skillExtension)
        {
            Id = id;
            Catalog = catalog ?? string.Empty;
            CancelTags = cancelTags ?? new List<CancelTag>();
            BeCancelledTags = beCancelledTags ?? new List<BeCancelledTag>();
            TempBeCancelledTags = tempBeCancelledTags ?? new List<TempBeCancelledTag>();
            Commands = commands ?? new List<ActionCommand>();
            AutoNextActionId = autoNextActionId;
            KeepPlayingAnim = keepPlayingAnim;
            AutoTerminate = autoTerminate;
            Priority = priority;
            Duration = duration;
            MoveExtension = moveExtension;
            SkillExtension = skillExtension;
            AnimationSpeedMultiplier = 1f;
        }
        
        // ========== 便利访问方法 ==========
        
        /// <summary>
        /// 获取基准移动速度（从 MoveExtension 转换）
        /// </summary>
        /// <returns>移动速度（FP），如果不存在则返回 null</returns>
        public FP? GetBaseMoveSpeed()
        {
            if (MoveExtension != null && MoveExtension.MoveSpeed > 0)
            {
                return FP.FromFloat(MoveExtension.MoveSpeed / 1000f);
            }
            return null;
        }
        
        /// <summary>
        /// 获取根节点位移数据（从 MoveExtension 或 SkillExtension）
        /// </summary>
        /// <returns>根节点位移数据，如果不存在则返回 null</returns>
        public AnimationRootMotionData? GetRootMotionData()
        {
            return MoveExtension?.RootMotionData ?? SkillExtension?.RootMotionData;
        }
        
        /// <summary>
        /// 获取实际法力消耗（从 SkillExtension）
        /// </summary>
        /// <returns>法力消耗，如果不存在则返回 0</returns>
        public int GetActualCost()
        {
            return SkillExtension?.ActualCost ?? 0;
        }
        
        /// <summary>
        /// 获取实际冷却帧数（从 SkillExtension）
        /// </summary>
        /// <returns>冷却帧数，如果不存在则返回 0</returns>
        public int GetActualCooldown()
        {
            return SkillExtension?.ActualCooldown ?? 0;
        }
        
        /// <summary>
        /// 获取触发效果列表（从 SkillExtension）
        /// </summary>
        /// <returns>触发效果列表，如果不存在则返回空列表</returns>
        public List<TriggerFrameInfo> GetTriggerEffects()
        {
            return SkillExtension?.TriggerEffects ?? new List<TriggerFrameInfo>();
        }
    }
}
