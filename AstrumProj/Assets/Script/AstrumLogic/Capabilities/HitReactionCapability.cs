using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.Managers;
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
            // TODO: 根据攻击方向（evt.HitDirection）播放不同的受击动作
            // 临时：播放通用受击动作
            var action = GetComponent<ActionComponent>(entity);
            if (action != null)
            {
                // 根据效果类型播放不同动作
                // action.PlayAction("Hit", priority: ActionPriority.High);
            }
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
            var positionOffset = UnityEngine.Vector3.zero;
            if (evt.HitOffset != TSVector.zero)
            {
                positionOffset = new UnityEngine.Vector3((float)evt.HitOffset.x, (float)evt.HitOffset.y, (float)evt.HitOffset.z);
            }

            var triggerData = new VFXTriggerEventData
            {
                EntityId = entity.UniqueId,
                ResourcePath = evt.VisualEffectPath,
                PositionOffset = positionOffset,
                Rotation = UnityEngine.Vector3.zero,
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
    }
}

