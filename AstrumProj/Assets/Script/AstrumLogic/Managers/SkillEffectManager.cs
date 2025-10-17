using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.SkillSystem.EffectHandlers;
using cfg.Skill;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 技能效果管理器 - 统一处理所有技能效果（单例）
    /// </summary>
    public class SkillEffectManager : Singleton<SkillEffectManager>
    {
        // 效果队列
        private readonly Queue<SkillEffectData> _effectQueue = new Queue<SkillEffectData>();
        
        // 效果类型 -> Handler 映射
        private readonly Dictionary<int, IEffectHandler> _handlers = new Dictionary<int, IEffectHandler>();
        
        // 当前World引用（用于从EntityId获取Entity）
        public World CurrentWorld { get; set; }
        
        /// <summary>
        /// 单例初始化
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            RegisterAllHandlers();
        }
        
        /// <summary>
        /// 注册所有效果处理器
        /// </summary>
        private void RegisterAllHandlers()
        {
            // 注册伤害处理器 (EffectType = 1)
            RegisterHandler(1, new DamageEffectHandler());
            
            // 注册治疗处理器 (EffectType = 2)
            RegisterHandler(2, new HealEffectHandler());
            
            // TODO: 注册其他处理器
            // RegisterHandler(3, new KnockbackEffectHandler());
            // RegisterHandler(4, new BuffEffectHandler());
            // RegisterHandler(5, new DebuffEffectHandler());
            
            ASLogger.Instance.Info("SkillEffectManager: All handlers registered successfully");
        }
        
        /// <summary>
        /// 注册效果处理器
        /// </summary>
        /// <param name="effectType">效果类型（1=伤害, 2=治疗, 3=击退, 4=Buff, 5=Debuff）</param>
        /// <param name="handler">效果处理器实例</param>
        public void RegisterHandler(int effectType, IEffectHandler handler)
        {
            if (handler == null)
            {
                ASLogger.Instance.Error($"SkillEffectManager.RegisterHandler: Handler is null for type {effectType}");
                return;
            }
            
            _handlers[effectType] = handler;
            ASLogger.Instance.Info($"SkillEffectManager: Registered handler for effect type {effectType}");
        }
        
        /// <summary>
        /// 将技能效果加入队列
        /// </summary>
        public void QueueSkillEffect(SkillEffectData effectData)
        {
            if (effectData == null)
            {
                ASLogger.Instance.Error("SkillEffectManager.QueueSkillEffect: effectData is null");
                return;
            }
            
            _effectQueue.Enqueue(effectData);
            
            ASLogger.Instance.Debug($"Queued effect {effectData.EffectId}: " +
                $"{effectData.CasterId} → {effectData.TargetId}");
        }
        
        /// <summary>
        /// 每帧更新：处理效果队列
        /// </summary>
        public void Update()
        {
            // 处理当前帧的所有效果
            while (_effectQueue.Count > 0)
            {
                var effectData = _effectQueue.Dequeue();
                ProcessEffect(effectData);
            }
        }
        
        /// <summary>
        /// 处理单个效果
        /// </summary>
        private void ProcessEffect(SkillEffectData effectData)
        {
            // 0. 检查World引用
            if (CurrentWorld == null)
            {
                ASLogger.Instance.Error("SkillEffectManager.ProcessEffect: CurrentWorld is null");
                return;
            }
            
            // 1. 从World获取实体
            if (!CurrentWorld.Entities.TryGetValue(effectData.CasterId, out var casterEntity))
            {
                ASLogger.Instance.Warning($"Caster entity not found: {effectData.CasterId}");
                return;
            }
            
            if (!CurrentWorld.Entities.TryGetValue(effectData.TargetId, out var targetEntity))
            {
                ASLogger.Instance.Warning($"Target entity not found: {effectData.TargetId}");
                return;
            }
            
            // 2. 从配置表读取效果配置
            var effectConfig = SkillConfig.Instance.GetSkillEffect(effectData.EffectId);
            if (effectConfig == null)
            {
                ASLogger.Instance.Error($"Effect config not found: {effectData.EffectId}");
                return;
            }
            
            // 3. 获取对应的处理器
            if (!_handlers.TryGetValue(effectConfig.EffectType, out var handler))
            {
                ASLogger.Instance.Warning($"No handler registered for effect type: {effectConfig.EffectType}");
                return;
            }
            
            // 4. 执行效果
            try
            {
                handler.Handle(casterEntity, targetEntity, effectConfig);
                
                ASLogger.Instance.Debug($"Processed effect {effectData.EffectId} " +
                    $"(type {effectConfig.EffectType}) successfully");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"Failed to process effect {effectData.EffectId}: {ex.Message}");
                ASLogger.Instance.LogException(ex);
            }
        }
        
        /// <summary>
        /// 获取当前队列中的效果数量
        /// </summary>
        public int QueuedEffectCount => _effectQueue.Count;
        
        /// <summary>
        /// 清空效果队列（用于测试或重置）
        /// </summary>
        public void ClearQueue()
        {
            _effectQueue.Clear();
            ASLogger.Instance.Debug("SkillEffectManager: Cleared effect queue");
        }
    }
}

