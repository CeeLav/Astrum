using Astrum.LogicCore.SkillSystem;
using Astrum.CommonBase;
using System.Collections.Generic;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 技能配置管理器 - 从Luban表组装技能数据（单例）
    /// </summary>
    public class SkillConfigManager : Singleton<SkillConfigManager>
    {
        /// <summary>
        /// 技能信息缓存 <"skillId_level", SkillInfo>
        /// 同一个技能同一个等级，所有实体共享同一个实例
        /// </summary>
        private Dictionary<string, SkillInfo> _skillInfoCache = new Dictionary<string, SkillInfo>();
        
        // ========== 主入口：获取技能信息（根据等级构造，带缓存）==========
        
        /// <summary>
        /// 获取技能信息（根据等级构造，带缓存）
        /// </summary>
        /// <param name="skillId">技能ID</param>
        /// <param name="level">技能等级</param>
        /// <returns>完整的技能信息（包含解析后的技能动作）</returns>
        public SkillInfo? GetSkillInfo(int skillId, int level)
        {
            var cacheKey = $"{skillId}_{level}";
            
            // 检查缓存
            if (_skillInfoCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
            
            // 构造新的 SkillInfo
            var skillInfo = BuildSkillInfo(skillId, level);
            
            // 缓存结果
            if (skillInfo != null)
            {
                _skillInfoCache[cacheKey] = skillInfo;
                ASLogger.Instance.Debug($"SkillConfigManager: Cached SkillInfo for {cacheKey}");
            }
            
            return skillInfo;
        }
        
        /// <summary>
        /// 构造技能信息（内部方法）
        /// </summary>
        private SkillInfo? BuildSkillInfo(int skillId, int level)
        {
            var configManager = ConfigManager.Instance;
            if (!configManager.IsInitialized)
            {
                ASLogger.Instance.Error("SkillConfigManager.BuildSkillInfo: ConfigManager not initialized");
                return null;
            }
            
            // 从 SkillTable 获取基础配置
            var skillTable = configManager.Tables.TbSkillTable.Get(skillId);
            if (skillTable == null)
            {
                ASLogger.Instance.Error($"SkillConfigManager.BuildSkillInfo: Skill {skillId} not found");
                return null;
            }
            
            // 组装 SkillInfo
            var skillInfo = new SkillInfo
            {
                SkillId = skillTable.Id,
                CurrentLevel = level,
                Name = skillTable.Name,
                Description = skillTable.Description,
                SkillType = skillTable.SkillType,
                IconId = skillTable.IconId,
                RequiredLevel = skillTable.RequiredLevel,
                MaxLevel = skillTable.MaxLevel,
                DisplayCooldown = skillTable.DisplayCooldown,
                DisplayCost = skillTable.DisplayCost,
                SkillActionIds = new List<int>(skillTable.SkillActionIds)
            };
            
            ASLogger.Instance.Info($"SkillConfigManager: Built SkillInfo for skill {skillId} ({skillInfo.Name}) level {level}");
            return skillInfo;
        }
        
        // ========== 技能动作构造 ==========
        
        /// <summary>
        /// 构造技能动作信息（根据技能等级）
        /// </summary>
        private SkillActionInfo? BuildSkillActionInfo(int actionId, int skillLevel)
        {
            var configManager = ConfigManager.Instance;
            if (!configManager.IsInitialized) return null;
            
            // 1. 从 ActionTable 获取基础动作数据
            var actionTable = configManager.Tables.TbActionTable.GetOrDefault(actionId);
            if (actionTable == null)
            {
                ASLogger.Instance.Error($"SkillConfigManager: ActionTable {actionId} not found");
                return null;
            }
            
            // 2. 从 SkillActionTable 获取技能专属数据
            var skillActionTable = configManager.Tables.TbSkillActionTable.Get(actionId);
            if (skillActionTable == null)
            {
                ASLogger.Instance.Error($"SkillConfigManager: SkillActionTable {actionId} not found");
                return null;
            }
            
            // 3. 创建 SkillActionInfo 实例
            var skillActionInfo = new SkillActionInfo();
            
            // 4. 填充基类字段（复用 ActionConfigManager 的逻辑）
            ActionConfigManager.Instance.PopulateBaseActionFields(skillActionInfo, actionTable);
            
            // 5. 填充技能专属字段
            ActionConfigManager.Instance.PopulateSkillActionFields(skillActionInfo, skillActionTable);
            
            // 6. 解析触发帧（关键：应用技能等级）
            skillActionInfo.TriggerEffects = ParseTriggerFrames(
                skillActionInfo.TriggerFrames, 
                skillLevel
            );
            
            ASLogger.Instance.Debug($"SkillConfigManager: Built SkillActionInfo {actionId} for skill level {skillLevel}");
            
            return skillActionInfo;
        }

        /// <summary>
        /// 对外公开：根据动作ID与技能等级构造新的 SkillActionInfo 实例
        /// </summary>
        public SkillActionInfo? CreateSkillActionInstance(int actionId, int skillLevel)
        {
            return BuildSkillActionInfo(actionId, skillLevel);
        }
        
        // ========== BaseEffectId + Level 映射 ==========
        
        /// <summary>
        /// 获取效果值（BaseEffectId + Level 映射）
        /// 当前阶段：仅精确查找，插值算法后续实现
        /// </summary>
        public EffectValueResult GetEffectValue(int baseEffectId, int level)
        {
            var configManager = ConfigManager.Instance;
            if (!configManager.IsInitialized)
            {
                ASLogger.Instance.Error("SkillConfigManager.GetEffectValue: ConfigManager not initialized");
                return new EffectValueResult { Value = 0f };
            }
            
            // 计算实际效果ID：BaseEffectId + Level
            int actualEffectId = baseEffectId + level;
            
            // 精确查找
            var effect = configManager.Tables.TbSkillEffectTable.Get(actualEffectId);
            if (effect != null)
            {
                return new EffectValueResult
                {
                    EffectId = actualEffectId,
                    Value = effect.EffectValue,
                    IsInterpolated = false,
                    EffectData = effect
                };
            }
            
            // TODO: 后续阶段实现插值逻辑
            ASLogger.Instance.Warning($"SkillConfigManager.GetEffectValue: Effect {actualEffectId} " +
                $"(base={baseEffectId}, level={level}) not found. Interpolation not implemented yet.");
            
            return new EffectValueResult { Value = 0f };
        }
        
        /// <summary>
        /// 获取技能效果数据
        /// </summary>
        public cfg.Skill.SkillEffectTable? GetSkillEffect(int effectId)
        {
            var configManager = ConfigManager.Instance;
            if (!configManager.IsInitialized) return null;
            
            return configManager.Tables.TbSkillEffectTable.Get(effectId);
        }
        
        // ========== 触发帧解析 ==========
        
        /// <summary>
        /// 解析触发帧信息（应用等级映射）
        /// 格式：Frame5:Collision:10000,Frame8:Direct:10100
        /// </summary>
        public List<TriggerFrameEffect> ParseTriggerFrames(string triggerFramesStr, int skillLevel)
        {
            var results = new List<TriggerFrameEffect>();
            
            if (string.IsNullOrEmpty(triggerFramesStr))
                return results;
            
            var parts = triggerFramesStr.Split(',');
            foreach (var part in parts)
            {
                var segments = part.Trim().Split(':');
                if (segments.Length >= 3)
                {
                    // 解析帧号
                    var frameStr = segments[0].Replace("Frame", "").Trim();
                    if (!int.TryParse(frameStr, out var frame))
                        continue;
                    
                    // 解析触发类型
                    var triggerType = segments[1].Trim();
                    
                    // 解析基础效果ID
                    if (!int.TryParse(segments[2].Trim(), out var baseEffectId))
                        continue;
                    
                    // 应用等级映射获取实际效果值
                    var effectResult = GetEffectValue(baseEffectId, skillLevel);
                    
                    if (effectResult.EffectData != null)
                    {
                        results.Add(new TriggerFrameEffect
                        {
                            Frame = frame,
                            TriggerType = triggerType,
                            EffectId = effectResult.EffectId,
                            EffectValue = effectResult.Value
                        });
                        
                        ASLogger.Instance.Debug($"SkillConfigManager: Parsed trigger Frame{frame}:{triggerType} " +
                            $"→ Effect {effectResult.EffectId} (value={effectResult.Value})");
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"SkillConfigManager: Failed to get effect for base {baseEffectId} level {skillLevel}");
                    }
                }
            }
            
            return results;
        }
    }
}

