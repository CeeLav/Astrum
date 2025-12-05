using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.ActionSystem;
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
    public class SkillDisplacementCapability : Capability<SkillDisplacementCapability>
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
        
        /// <summary>
        /// 用于标识是技能位移系统禁用用户输入位移的 instigatorId
        /// 使用负数以避免与实际实体ID冲突
        /// </summary>
        private const long SKILL_DISPLACEMENT_DISABLER_ID = -1;
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            ASLogger.Instance.Debug($"SkillDisplacementCapability attached to entity {entity.UniqueId}");
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 检查必需组件是否存在
            if (!base.ShouldActivate(entity))
                return false;
            
            // 1. 检查必需组件
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null || actionComponent.CurrentAction == null)
                return false;
            
            var transComponent = GetComponent<TransComponent>(entity);
            if (transComponent == null)
                return false;
            
            // 2. 检查当前动作是否是技能动作
            var actionInfo = actionComponent.CurrentAction;
            if (actionInfo?.SkillExtension == null)
                return false;
            
            // 3. 检查是否有根节点位移数据（检查 HasMotion 属性）
            var rootMotionData = actionInfo.GetRootMotionData();
            if (rootMotionData == null || !rootMotionData.HasMotion)
                return false;
            
            return true;
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 如果基础检查失败，则停用
            if (base.ShouldDeactivate(entity))
                return true;
            
            // 检查组件是否存在
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null || actionComponent.CurrentAction == null)
                return true;
            
            var transComponent = GetComponent<TransComponent>(entity);
            if (transComponent == null)
                return true;
            
            // 检查是否仍然是技能动作且有位移数据
            var actionInfo = actionComponent.CurrentAction;
            if (actionInfo?.SkillExtension == null)
                return true;
            
            var rootMotionData = actionInfo.GetRootMotionData();
            if (rootMotionData == null || !rootMotionData.HasMotion)
                return true;
            
            return false;
        }
        
        public override void OnActivate(Entity entity)
        {
            base.OnActivate(entity);
            
            // 禁用用户输入位移
            DisableUserInputMovement(entity);
        }
        
        public override void OnDeactivate(Entity entity)
        {
            // 恢复用户输入位移
            RestoreUserInputMovement(entity);
            
            base.OnDeactivate(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            // 获取当前动作信息（ShouldActivate 已经检查过了，这里直接使用）
            var actionComponent = GetComponent<ActionComponent>(entity);
            if (actionComponent == null || actionComponent.CurrentAction == null)
            {
                return;
            }
            
            var actionInfo = actionComponent.CurrentAction;
            if (actionInfo?.SkillExtension == null)
            {
                return;
            }
            
            // 应用当前帧的位移
            int currentFrame = actionComponent.CurrentFrame;
            ApplyRootMotion(entity, actionInfo, currentFrame);
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 应用动画根节点位移
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="actionInfo">动作信息</param>
        /// <param name="currentFrame">当前帧索引</param>
        private void ApplyRootMotion(Entity entity, ActionInfo actionInfo, int currentFrame)
        {
            // 1. 获取根节点位移数据
            var rootMotionData = actionInfo.GetRootMotionData();
            if (rootMotionData == null || !rootMotionData.HasMotion)
            {
                return;
            }
            
            // 2. 检查帧索引有效性
            if (currentFrame < 0 || 
                currentFrame >= rootMotionData.Frames.Count)
            {
                return;
            }
            
            // 3. 获取当前帧的位移数据
            var frameData = rootMotionData.Frames[currentFrame];
            
            // 4. 获取实体的变换组件
            var transComponent = GetComponent<TransComponent>(entity);
            if (transComponent == null)
            {
                return;
            }
            
            // 5. 应用增量位移（局部空间转世界空间）
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
            var actionInfo = actionComponent?.CurrentAction;
            if (actionInfo?.SkillExtension != null)
            {
                var rootMotionData = actionInfo.GetRootMotionData();
                return rootMotionData != null && rootMotionData.HasMotion;
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
            var actionInfo = actionComponent?.CurrentAction;
            if (actionInfo?.SkillExtension != null)
            {
                return actionInfo.Id;
            }
            return 0;
        }
        
        // ====== 用户输入位移禁用/恢复方法 ======
        
        /// <summary>
        /// 禁用用户输入位移（技能位移激活时）
        /// </summary>
        private void DisableUserInputMovement(Entity entity)
        {
            var capabilitySystem = entity.World?.CapabilitySystem;
            if (capabilitySystem == null)
            {
                ASLogger.Instance.Warning("CapabilitySystem not available from World, cannot disable user input movement");
                return;
            }
            
            // 禁用用户输入位移能力（通过 UserInputMovement Tag）
            capabilitySystem.DisableCapabilitiesByTag(
                entity, 
                CapabilityTag.UserInputMovement, 
                SKILL_DISPLACEMENT_DISABLER_ID, 
                "Skill displacement active"
            );
            
            //ASLogger.Instance.Debug($"Disabled user input movement for entity {entity.UniqueId} during skill displacement");
        }
        
        /// <summary>
        /// 恢复用户输入位移（技能位移停用时）
        /// </summary>
        private void RestoreUserInputMovement(Entity entity)
        {
            var capabilitySystem = entity.World?.CapabilitySystem;
            if (capabilitySystem == null)
            {
                ASLogger.Instance.Warning("CapabilitySystem not available from World, cannot restore user input movement");
                return;
            }
            
            // 恢复用户输入位移能力
            capabilitySystem.EnableCapabilitiesByTag(
                entity, 
                CapabilityTag.UserInputMovement, 
                SKILL_DISPLACEMENT_DISABLER_ID
            );
            
            //ASLogger.Instance.Debug($"Restored user input movement for entity {entity.UniqueId} after skill displacement");
        }
    }
}
