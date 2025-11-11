using MemoryPack;
using Astrum.LogicCore.Physics;
using System;
using System.Collections.Generic;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 触发帧信息（运行时数据，已包含解析好的碰撞形状）
    /// 从 SkillActionInfo.TriggerFrames JSON 解析后构造
    /// 支持多种类型：SkillEffect (Collision/Direct/Condition)、VFX、SFX
    /// </summary>
    [MemoryPackable]
    public partial class TriggerFrameInfo
    {
        /// <summary>触发帧号（单帧触发）</summary>
        public int Frame { get; set; }
        
        /// <summary>起始帧（多帧范围触发）</summary>
        public int? StartFrame { get; set; }
        
        /// <summary>结束帧（多帧范围触发）</summary>
        public int? EndFrame { get; set; }
        
        /// <summary>
        /// 顶层类型：SkillEffect、VFX、SFX
        /// 对于 SkillEffect 类型，TriggerType 表示内部类型（Collision/Direct/Condition）
        /// </summary>
        public string Type { get; set; } = "SkillEffect";
        
        /// <summary>
        /// 触发类型
        /// - 对于 SkillEffect 类型：Collision/Direct/Condition
        /// - 对于 VFX/SFX 类型：不使用
        /// </summary>
        public string TriggerType { get; set; } = string.Empty;
        
        /// <summary>效果ID列表（仅用于 SkillEffect 类型，支持多个效果）</summary>
        public int[] EffectIds { get; set; } = new int[0];
        
        /// <summary>
        /// 碰撞形状（仅用于 SkillEffect.Collision 类型，已在配置解析时构造）
        /// 运行时可直接使用，无需重新解析
        /// </summary>
        [MemoryPackAllowSerialize]
        public CollisionShape? CollisionShape { get; set; }
        
        /// <summary>触发条件（仅用于 SkillEffect.Condition 类型）</summary>
        [MemoryPackAllowSerialize]
        public TriggerCondition? Condition { get; set; }

        /// <summary>模型 Socket 名称（用于抛射物、特效的定位）</summary>
        public string SocketName { get; set; } = string.Empty;
        
        // === VFX 字段 ===
        
        /// <summary>特效资源路径（仅用于 VFX 类型）</summary>
        public string VFXResourcePath { get; set; } = string.Empty;
        
        /// <summary>位置偏移（仅用于 VFX 类型，存储为 float[3]）</summary>
        public float[] VFXPositionOffset { get; set; } = null; // [x, y, z]
        
        /// <summary>旋转（仅用于 VFX 类型，存储为 float[3]，欧拉角）</summary>
        public float[] VFXRotation { get; set; } = null; // [x, y, z]
        
        /// <summary>缩放（仅用于 VFX 类型）</summary>
        public float VFXScale { get; set; } = 1.0f;
        
        /// <summary>播放速度（仅用于 VFX 类型）</summary>
        public float VFXPlaybackSpeed { get; set; } = 1.0f;
        
        /// <summary>是否跟随角色（仅用于 VFX 类型）</summary>
        public bool VFXFollowCharacter { get; set; } = true;
        
        /// <summary>是否循环播放（仅用于 VFX 类型）</summary>
        public bool VFXLoop { get; set; } = false;
        
        [MemoryPackConstructor]
        public TriggerFrameInfo(int frame, int? startFrame, int? endFrame, string type, string triggerType, 
            int[] effectIds, CollisionShape? collisionShape, TriggerCondition? condition,
            string vfxResourcePath, float[] vfxPositionOffset, float[] vfxRotation,
            float vfxScale, float vfxPlaybackSpeed, bool vfxFollowCharacter, bool vfxLoop)
        {
            Frame = frame;
            StartFrame = startFrame;
            EndFrame = endFrame;
            Type = type ?? "SkillEffect";
            TriggerType = triggerType ?? string.Empty;
            EffectIds = effectIds ?? new int[0];
            CollisionShape = collisionShape;
            Condition = condition;
            VFXResourcePath = vfxResourcePath ?? string.Empty;
            VFXPositionOffset = vfxPositionOffset;
            VFXRotation = vfxRotation;
            VFXScale = vfxScale;
            VFXPlaybackSpeed = vfxPlaybackSpeed;
            VFXFollowCharacter = vfxFollowCharacter;
            VFXLoop = vfxLoop;
            SocketName = string.Empty;
        }
        
        public TriggerFrameInfo() { }
        
        /// <summary>
        /// 获取当前帧是否在触发范围内
        /// </summary>
        public bool IsFrameInRange(int currentFrame)
        {
            if (Frame > 0)
            {
                return currentFrame == Frame;
            }
            else if (StartFrame.HasValue && EndFrame.HasValue)
            {
                return currentFrame >= StartFrame.Value && currentFrame <= EndFrame.Value;
            }
            return false;
        }
        
        /// <summary>
        /// 获取触发帧号（兼容旧代码）
        /// </summary>
        public int GetTriggerFrame()
        {
            if (Frame > 0)
                return Frame;
            if (StartFrame.HasValue)
                return StartFrame.Value;
            return 0;
        }
    }
    
    /// <summary>
    /// 触发条件（用于 Condition 类型触发）
    /// </summary>
    [MemoryPackable]
    public partial class TriggerCondition
    {
        /// <summary>最小能量要求</summary>
        public float EnergyMin { get; set; }
        
        /// <summary>必需的状态标记</summary>
        public string RequiredTag { get; set; } = string.Empty;
        
        [MemoryPackConstructor]
        public TriggerCondition(float energyMin, string requiredTag)
        {
            EnergyMin = energyMin;
            RequiredTag = requiredTag ?? string.Empty;
        }
        
        public TriggerCondition() { }
    }
}

