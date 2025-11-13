using System.Collections.Generic;
using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Systems;
using Astrum.CommonBase;
using Astrum.CommonBase.Events;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 受击反应能力 - 处理实体受到技能效果的反馈
    /// 优先级：200（低于技能执行，高于击退）
    /// </summary>
    public class HitReactionCapability : Capability<HitReactionCapability>
    {
        // ====== 元数据 ======
        
        public override int Priority => 200;
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag>
        {
            CapabilityTag.Combat,
            CapabilityTag.Animation
        };

        private const string HitReactionExternalActionSource = "HitReaction";
        private const int HitActionPriority = 900;
        private const int HitActionTransitionFrames = 2;
        private const int HitActionFreezeFrames = 0;
        
        // ====== 生命周期 ======
        
        public override bool ShouldActivate(Entity entity)
        {
            return base.ShouldActivate(entity) &&
                   HasComponent<ActionComponent>(entity);
        }
        
        // ====== Tick 方法 ======
        
        /// <summary>
        /// 每帧更新（必须实现）
        /// 新的事件队列系统中，事件由 CapabilitySystem 自动调度，无需在 Tick 中主动拉取
        /// </summary>
        public override void Tick(Entity entity)
        {
            // 事件处理由 RegisterEventHandlers 静态声明，CapabilitySystem 自动调度
            // 此方法保留为空，符合 Capability<T> 接口要求
        }
        
        // ====== 事件处理声明 ======
        
        /// <summary>
        /// 静态声明：该 Capability 处理的事件
        /// </summary>
        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<HitReactionEvent>(OnHitReaction);
        }
        
        // ====== 事件处理函数 ======
        
        /// <summary>
        /// 受击反馈事件处理（由效果处理器发送）
        /// </summary>
        private void OnHitReaction(Entity entity, HitReactionEvent evt)
        {
            ASLogger.Instance.Info($"[HitReactionCapability] OnHitReaction called for entity {entity.UniqueId}, " +
                $"effectId {evt.EffectId}, effectType {evt.EffectType}, caster {evt.CasterId}, " +
                $"vfxPath {(string.IsNullOrEmpty(evt.VisualEffectPath) ? "<none>" : evt.VisualEffectPath)}, " +
                $"sfxPath {(string.IsNullOrEmpty(evt.SoundEffectPath) ? "<none>" : evt.SoundEffectPath)}");
            
            // 1. 播放受击动作
            PlayHitAction(entity, evt);
            
            // 2. 播放受击特效
            PlayHitVFX(entity, evt);

            // 3. 处理音效（当前仅占位）
            HandleHitSound(entity, evt);
        }
        
        // ====== 受击表现方法 ======
        
        /// <summary>
        /// 播放受击动作
        /// </summary>
        private void PlayHitAction(Entity entity, HitReactionEvent evt)
        {
            if (!HasComponent<ActionComponent>(entity))
            {
                ASLogger.Instance.Debug("[HitReactionCapability] ActionComponent missing, skip hit action.");
                return;
            }

            var config = entity.EntityConfig;
            if (config == null)
            {
                ASLogger.Instance.Warning($"[HitReactionCapability] Entity {entity.UniqueId} has no config, unable to resolve HitAction.");
                return;
            }

            if (config.HitAction <= 0)
            {
                ASLogger.Instance.Debug($"[HitReactionCapability] Entity {entity.UniqueId} has no HitAction configured, skip action switch.");
                return;
            }

            UpdateFacingDirection(entity, evt);

            var preorder = new ActionPreorderEvent
            {
                ActionId = config.HitAction,
                Priority = HitActionPriority,
                TransitionFrames = HitActionTransitionFrames,
                FromFrame = 0,
                FreezingFrames = HitActionFreezeFrames,
                SourceTag = HitReactionExternalActionSource,
                TriggerWhenInactive = true
            };

            entity.QueueEvent(preorder);
        }
        
        /// <summary>
        /// 播放受击特效
        /// </summary>
        private void PlayHitVFX(Entity entity, HitReactionEvent evt)
        {
            if (string.IsNullOrEmpty(evt.VisualEffectPath))
            {
                ASLogger.Instance.Debug("[HitReactionCapability] 未配置受击特效路径，跳过");
                return;
            }

            if (entity == null)
            {
                ASLogger.Instance.Warning("[HitReactionCapability] 实体为空，无法播放受击特效");
                return;
            }

            var trans = entity.GetComponent<TransComponent>();
            var positionOffset = TSVector.zero;
            if (evt.HitOffset != TSVector.zero)
            {
                positionOffset = evt.HitOffset;
            }

            var triggerData = new VFXTriggerEventData
            {
                EntityId = entity.UniqueId,
                ResourcePath = evt.VisualEffectPath,
                PositionOffset = positionOffset,
                Rotation = TSVector.zero,
                Scale = 1f,
                PlaybackSpeed = 1f,
                FollowCharacter = true,
                Loop = false
            };

            EventSystem.Instance.Publish(triggerData);

            ASLogger.Instance.Info($"[HitReactionCapability] 发布受击特效事件: entity={entity.UniqueId}, path={evt.VisualEffectPath}");
        }

        /// <summary>
        /// 处理受击音效
        /// </summary>
        private void HandleHitSound(Entity entity, HitReactionEvent evt)
        {
            if (string.IsNullOrEmpty(evt.SoundEffectPath))
            {
                return;
            }

            // TODO: 接入运行时音频播放（目前仅记录日志）
            ASLogger.Instance.Info($"[HitReactionCapability] 受击音效占位: {evt.SoundEffectPath}");
        }

        private void UpdateFacingDirection(Entity entity, HitReactionEvent evt)
        {
            var trans = GetComponent<TransComponent>(entity);
            if (trans == null)
            {
                return;
            }

            var direction = evt.HitDirection;

            if (direction.sqrMagnitude > FP.EN4)
            {
                direction = direction * (-FP.One);
            }
            else
            {
                direction = GetDirectionFromCaster(entity, trans, evt.CasterId);
            }

            direction.y = FP.Zero;

            if (direction.sqrMagnitude <= FP.EN4)
            {
                return;
            }

            direction = TSVector.Normalize(direction);
            var rotation = TSQuaternion.LookRotation(direction, TSVector.up);
            trans.SetRotation(rotation);
        }

        private TSVector GetDirectionFromCaster(Entity target, TransComponent targetTrans, long casterId)
        {
            if (target == null || target.World == null)
            {
                return TSVector.zero;
            }

            var caster = target.World.GetEntity(casterId);
            if (caster == null)
            {
                return TSVector.zero;
            }

            var casterTrans = caster.GetComponent<TransComponent>();
            if (casterTrans == null || targetTrans == null)
            {
                return TSVector.zero;
            }

            var direction = casterTrans.Position - targetTrans.Position;
            direction.y = FP.Zero;
            return direction;
        }
    }
}

