using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
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

            // TODO: 发布受击特效事件到视图层，由视图层负责通过 ResourceManager 按路径加载并实例化 Prefab
            ASLogger.Instance.Info($"[HitReactionCapability] 请求播放受击特效: {evt.VisualEffectPath}");
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

            // TODO: 将音效路径推送到音频系统，由客户端层负责实际播放
            ASLogger.Instance.Info($"[HitReactionCapability] 受击音效占位: {evt.SoundEffectPath}");
        }
    }
}

