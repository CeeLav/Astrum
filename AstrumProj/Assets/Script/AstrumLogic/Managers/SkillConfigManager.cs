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
            
            // 6. 解析触发帧（关键：应用技能等级，同时解析碰撞形状）
            skillActionInfo.TriggerEffects = ParseTriggerFrames(
                skillActionInfo.TriggerFrames,
                skillActionInfo.AttackBoxInfo,
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
        /// 解析触发帧信息（应用等级映射，并解析碰撞形状）
        /// 格式：Frame5:Collision:10000,Frame8:Direct:10100
        /// </summary>
        public List<TriggerFrameInfo> ParseTriggerFrames(string triggerFramesStr, string attackBoxInfo, int skillLevel)
        {
            var results = new List<TriggerFrameInfo>();
            
            if (string.IsNullOrEmpty(triggerFramesStr))
                return results;
            
            // 1. 预先解析 AttackBoxInfo，得到碰撞形状列表
            List<Physics.CollisionShape> collisionShapes = new List<Physics.CollisionShape>();
            if (!string.IsNullOrEmpty(attackBoxInfo))
            {
                collisionShapes = Physics.CollisionDataParser.Parse(attackBoxInfo);
                if (collisionShapes == null)
                {
                    ASLogger.Instance.Warning($"Failed to parse AttackBoxInfo: {attackBoxInfo}");
                    collisionShapes = new List<Physics.CollisionShape>();
                }
            }
            
            // 2. 解析触发帧字符串
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
                    
                    // 应用等级映射获取实际效果ID
                    var effectResult = GetEffectValue(baseEffectId, skillLevel);
                    
                    if (effectResult.EffectData != null)
                    {
                        // 3. 构造 TriggerFrameInfo
                        TriggerFrameInfo triggerInfo = new TriggerFrameInfo
                        {
                            Frame = frame,
                            TriggerType = triggerType,
                            EffectId = effectResult.EffectId
                        };
                        
                        // 4. 如果是 Collision 类型，附加碰撞形状
                        if (triggerType.Equals("Collision", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (collisionShapes.Count > 0)
                            {
                                // 使用第一个碰撞形状（简化实现）
                                triggerInfo.CollisionShape = collisionShapes[0];
                            }
                            else
                            {
                                ASLogger.Instance.Warning($"Collision trigger at frame {frame} but no collision shape available");
                            }
                        }
                        
                        // 5. 如果是 Condition 类型，解析条件（可选）
                        if (triggerType.Equals("Condition", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (segments.Length >= 4)
                            {
                                triggerInfo.Condition = ParseCondition(segments[3]);
                            }
                        }
                        
                        results.Add(triggerInfo);
                        
                        ASLogger.Instance.Debug($"Parsed trigger Frame{frame}:{triggerType} → Effect {effectResult.EffectId}");
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"Failed to get effect for base {baseEffectId} level {skillLevel}");
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 解析触发条件字符串
        /// 格式：EnergyMin=50,Tag=Charged
        /// </summary>
        private TriggerCondition ParseCondition(string conditionStr)
        {
            var condition = new TriggerCondition();
            
            var parts = conditionStr.Split(',');
            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length != 2) continue;
                
                var key = kv[0].Trim();
                var value = kv[1].Trim();
                
                switch (key.ToLower())
                {
                    case "energymin":
                        if (float.TryParse(value, out var energy))
                            condition.EnergyMin = energy;
                        break;
                        
                    case "tag":
                        condition.RequiredTag = value;
                        break;
                }
            }
            
            return condition;
        }
    }
}

