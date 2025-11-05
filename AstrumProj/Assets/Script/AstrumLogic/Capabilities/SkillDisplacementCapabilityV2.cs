using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 技能位移能力（新架构，基于 Capability&lt;T&gt;）
    /// 处理技能释放时的位移逻辑
    /// 基于动画根节点位移数据（Root Motion Data），在技能动作执行过程中逐帧应用位移和旋转
    /// </summary>
    public class SkillDisplacementCapabilityV2 : Capability<SkillDisplacementCapabilityV2>
    {
        // ====== 元数据 ======
        public override int Priority => 150; // 优先级高于 MovementCapability (100)，确保技能位移优先
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.Movement, 
            CapabilityTag.Skill 
        };
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            ASLogger.Instance.Debug($"SkillDisplacementCapabilityV2 attached to entity {entity.UniqueId}");
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 检查必需组件是否存在
            return base.ShouldActivate(entity) &&
                   HasComponent<ActionComponent>(entity) &&
                   HasComponent<TransComponent>(entity);
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 缺少任何必需组件则停用
            return base.ShouldDeactivate(entity) ||
                   !HasComponent<ActionComponent>(entity) ||
                   !HasComponent<TransComponent>(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            // 1. 获取当前动作信息
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null || actionComponent.CurrentAction == null)
            {
                return;
            }
            
            // 2. 检查当前动作是否是技能动作
            if (!(actionComponent.CurrentAction is SkillActionInfo skillAction))
            {
                return;
            }
            
            // 3. 检查是否有根节点位移数据（检查 HasMotion 属性）
            if (skillAction.RootMotionData == null || 
                !skillAction.RootMotionData.HasMotion)
            {
                return;
            }
            
            // 4. 应用当前帧的位移
            int currentFrame = actionComponent.CurrentFrame;
            ApplyRootMotion(entity, skillAction, currentFrame);
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 应用动画根节点位移
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="skillAction">技能动作信息</param>
        /// <param name="currentFrame">当前帧索引</param>
        private void ApplyRootMotion(Entity entity, SkillActionInfo skillAction, int currentFrame)
        {
            // 1. 检查帧索引有效性
            if (currentFrame < 0 || 
                currentFrame >= skillAction.RootMotionData.Frames.Count)
            {
                return;
            }
            
            // 2. 获取当前帧的位移数据
            var frameData = skillAction.RootMotionData.Frames[currentFrame];
            
            // 3. 获取实体的变换组件
            var transComponent = GetComponent<TransComponent>(entity);
            if (transComponent == null)
            {
                return;
            }
            
            // 4. 应用增量位移（局部空间转世界空间）
            TSVector worldDeltaPosition = TransformDeltaToWorld(
                frameData.DeltaPosition, 
                transComponent.Rotation
            );
            
            // 强制Y轴分量为0（技能位移只影响水平方向，不影响垂直方向）
            worldDeltaPosition = new TSVector(worldDeltaPosition.x, FP.Zero, worldDeltaPosition.z);
            
            transComponent.Position = transComponent.Position + worldDeltaPosition;
            
            // 5. 应用增量旋转（暂时禁用，不进行旋转逻辑）
            // TODO: 未来需要旋转时再启用
            // TSQuaternion 没有 != 运算符，通过比较分量判断是否为 identity
            // if (frameData.DeltaRotation.x != FP.Zero || 
            //     frameData.DeltaRotation.y != FP.Zero || 
            //     frameData.DeltaRotation.z != FP.Zero || 
            //     frameData.DeltaRotation.w != FP.One)
            // {
            //     transComponent.Rotation = transComponent.Rotation * frameData.DeltaRotation;
            // }
            
            // 6. 【物理世界同步】更新实体在物理世界中的位置
            if (entity.World != null)
            {
                entity.World.HitSystem?.UpdateEntityPosition(entity);
            }
        }
        
        /// <summary>
        /// 将局部空间的增量位移转换为世界空间
        /// </summary>
        /// <param name="localDelta">局部空间位移（定点数）</param>
        /// <param name="rotation">角色当前朝向（四元数）</param>
        /// <returns>世界空间位移</returns>
        private TSVector TransformDeltaToWorld(TSVector localDelta, TSQuaternion rotation)
        {
            // 将局部位移旋转到世界空间（使用 TrueSync 的运算符重载）
            return rotation * localDelta;
        }
        
        /// <summary>
        /// 检查技能位移是否激活（运行时获取）
        /// </summary>
        /// <param name="entity">实体</param>
        /// <returns>是否正在应用技能位移</returns>
        public bool IsDisplacementActive(Entity entity)
        {
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent?.CurrentAction is SkillActionInfo skillAction)
            {
                return skillAction.RootMotionData != null && 
                       skillAction.RootMotionData.HasMotion;
            }
            return false;
        }
        
        /// <summary>
        /// 获取当前技能动作ID（运行时获取）
        /// </summary>
        /// <param name="entity">实体</param>
        /// <returns>当前技能动作ID，如果没有则返回0</returns>
        public int GetCurrentSkillActionId(Entity entity)
        {
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent?.CurrentAction is SkillActionInfo skillAction)
            {
                return skillAction.Id;
            }
            return 0;
        }
    }
}

