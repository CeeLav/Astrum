using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Physics;
using Astrum.CommonBase;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 技能配置管理器 - 从Luban表组装技能数据（单例）
    /// </summary>
    public class SkillConfig : Singleton<SkillConfig>
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
            }
            
            return skillInfo;
        }
        
        /// <summary>
        /// 构造技能信息（内部方法）
        /// </summary>
        private SkillInfo? BuildSkillInfo(int skillId, int level)
        {
            var configManager = TableConfig.Instance;
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
            
            //ASLogger.Instance.Info($"SkillConfigManager: Built SkillInfo for skill {skillId} ({skillInfo.Name}) level {level}");
            return skillInfo;
        }
        
        // ========== 技能动作构造 ==========
        
        /// <summary>
        /// 构造技能动作信息（根据技能等级）
        /// </summary>
        private SkillActionInfo? BuildSkillActionInfo(int actionId, int skillLevel)
        {
            var configManager = TableConfig.Instance;
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
            ActionConfig.Instance.PopulateBaseActionFields(skillActionInfo, actionTable);
            
            // 5. 填充技能专属字段
            ActionConfig.Instance.PopulateSkillActionFields(skillActionInfo, skillActionTable);
            
            // 6. 解析触发帧（关键：应用技能等级，同时解析碰撞形状）
            skillActionInfo.TriggerEffects = ParseTriggerFrames(
                skillActionInfo.TriggerFrames,
                skillLevel
            );
            
            
            
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
            var configManager = TableConfig.Instance;
            if (!configManager.IsInitialized)
            {
                ASLogger.Instance.Error("SkillConfigManager.GetEffectValue: ConfigManager not initialized");
                return new EffectValueResult { Value = 0f };
            }
            
            // 计算实际效果ID：BaseEffectId + Level - 1
            int actualEffectId = baseEffectId + level - 1;
            
            // 精确查找对应等级的效果
            var effect = configManager.Tables.TbSkillEffectTable.GetOrDefault(actualEffectId);
            if (effect != null)
            {
                return new EffectValueResult
                {
                    EffectId = actualEffectId,
                    Value = ExtractPrimaryValue(effect),
                    IsInterpolated = false,
                    EffectData = effect
                };
            }
            
            // 如果找不到对应等级的效果，回退使用基础效果
            var baseEffect = configManager.Tables.TbSkillEffectTable.GetOrDefault(baseEffectId);
            if (baseEffect != null)
            {
                ASLogger.Instance.Warning($"SkillConfigManager.GetEffectValue: Effect {actualEffectId} not found, " +
                    $"falling back to base effect {baseEffectId}");
                return new EffectValueResult
                {
                    EffectId = baseEffectId,
                    Value = ExtractPrimaryValue(baseEffect),
                    IsInterpolated = false,
                    EffectData = baseEffect
                };
            }
            
            // TODO: 后续阶段实现插值逻辑
            ASLogger.Instance.Error($"SkillConfigManager.GetEffectValue: Neither effect {actualEffectId} " +
                $"nor base effect {baseEffectId} found!");
            
            return new EffectValueResult { Value = 0f };
        }

        private float ExtractPrimaryValue(cfg.Skill.SkillEffectTable effect)
        {
            if (effect == null)
                return 0f;

            var type = effect.EffectType?.ToLower();
            switch (type)
            {
                case "damage":
                case "heal":
                    return effect.GetIntParam(2, 0) / 10f;
                case "knockback":
                    return effect.GetIntParam(1, 0) / 1000f;
                case "status":
                    return effect.GetIntParam(2, 0) / 1000f;
                case "teleport":
                    return effect.GetIntParam(1, 0) / 1000f;
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// 获取技能效果数据
        /// </summary>
        public cfg.Skill.SkillEffectTable? GetSkillEffect(int effectId)
        {
            var configManager = TableConfig.Instance;
            if (!configManager.IsInitialized) return null;
            
            return configManager.Tables.TbSkillEffectTable.Get(effectId);
        }
        
        // ========== 触发帧解析 ==========
        
        /// <summary>
        /// 解析触发帧信息（支持 JSON 格式和旧字符串格式）
        /// JSON 格式：{"frame":10,"type":"SkillEffect","triggerType":"Collision",...}
        /// 旧格式：Frame5:Collision(Box:5x2x1):10000,Frame8:Direct:10100
        /// </summary>
        public List<TriggerFrameInfo> ParseTriggerFrames(string triggerFramesStr, int skillLevel)
        {
            var results = new List<TriggerFrameInfo>();
            
            if (string.IsNullOrEmpty(triggerFramesStr))
                return results;
            
            // 检测是否为 JSON 格式（以 [ 或 { 开头）
            string trimmed = triggerFramesStr.Trim();
            if (trimmed.StartsWith("[") || trimmed.StartsWith("{"))
            {
                return ParseTriggerFramesFromJSON(triggerFramesStr, skillLevel);
            }
            
            // 否则使用旧格式解析
            return ParseTriggerFramesFromString(triggerFramesStr, skillLevel);
        }
        
        /// <summary>
        /// 从 JSON 格式解析触发帧信息
        /// </summary>
        private List<TriggerFrameInfo> ParseTriggerFramesFromJSON(string jsonStr, int skillLevel)
        {
            var results = new List<TriggerFrameInfo>();
            
            try
            {
                // 定义 JSON 数据结构（与编辑器端 TriggerFrameData 对应）
                var jsonData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonStr);
                if (jsonData == null)
                    return results;
                
                foreach (var item in jsonData)
                {
                    // 辅助方法：查找键（不区分大小写）
                    object GetValue(string key)
                    {
                        // 先尝试精确匹配
                        if (item.ContainsKey(key))
                            return item[key];
                        // 尝试首字母大写（PascalCase）
                        string pascalKey = char.ToUpper(key[0]) + key.Substring(1);
                        if (item.ContainsKey(pascalKey))
                            return item[pascalKey];
                        return null;
                    }
                    
                    // 获取类型
                    string type = GetValue(TriggerFrameJsonKeys.Type)?.ToString() ?? TriggerFrameJsonKeys.TypeSkillEffect;
                    
                    // 解析帧范围
                    int? frame = null;
                    int? startFrame = null;
                    int? endFrame = null;
                    
                    var frameObj = GetValue(TriggerFrameJsonKeys.Frame);
                    if (frameObj != null && int.TryParse(frameObj.ToString(), out int f))
                        frame = f;
                    
                    var startFrameObj = GetValue(TriggerFrameJsonKeys.StartFrame);
                    if (startFrameObj != null && int.TryParse(startFrameObj.ToString(), out int sf))
                        startFrame = sf;
                    
                    var endFrameObj = GetValue(TriggerFrameJsonKeys.EndFrame);
                    if (endFrameObj != null && int.TryParse(endFrameObj.ToString(), out int ef))
                        endFrame = ef;
                    
                    // 根据类型处理
                    if (type == TriggerFrameJsonKeys.TypeSkillEffect)
                    {
                        // SkillEffect 类型
                        string triggerType = GetValue(TriggerFrameJsonKeys.TriggerType)?.ToString() ?? "";
                        string socketName = GetValue(TriggerFrameJsonKeys.SocketName)?.ToString() ?? string.Empty;
                        
                        // 解析效果ID列表（支持单个或多个）
                        List<int> effectIds = new List<int>();
                        
                        // 首先尝试解析 effectIds 数组
                        var effectIdsObj = GetValue(TriggerFrameJsonKeys.EffectIds);
                        if (effectIdsObj is Newtonsoft.Json.Linq.JArray effectIdsArray)
                        {
                            foreach (var element in effectIdsArray)
                            {
                                if (int.TryParse(element.ToString(), out int id) && id > 0)
                                {
                                    effectIds.Add(id);
                                }
                            }
                        }
                        // 兼容旧数据：如果没有 effectIds，尝试解析单个 effectId
                        else
                        {
#pragma warning disable CS0618 // 类型或成员已过时
                            var effectIdObj = GetValue(TriggerFrameJsonKeys.EffectId);
#pragma warning restore CS0618
                            if (effectIdObj != null && int.TryParse(effectIdObj.ToString(), out int singleId) && singleId > 0)
                            {
                                effectIds.Add(singleId);
                            }
                        }
                        
                        if (effectIds.Count == 0)
                        {
                            ASLogger.Instance.Warning("SkillEffect trigger missing effectIds");
                            continue;
                        }
                        
                        // 应用等级映射到每个效果ID
                        List<int> mappedEffectIdsList = new List<int>();
                        foreach (var baseEffectId in effectIds)
                        {
                            var effectResult = GetEffectValue(baseEffectId, skillLevel);
                            if (effectResult.EffectData != null)
                            {
                                mappedEffectIdsList.Add(effectResult.EffectId);
                            }
                            else
                            {
                                ASLogger.Instance.Warning($"Failed to get effect for base {baseEffectId} level {skillLevel}");
                            }
                        }
                        
                        if (mappedEffectIdsList.Count == 0)
                        {
                            ASLogger.Instance.Warning("No valid effects after level mapping");
                            continue;
                        }
                        
                        // 转换为数组（MemoryPack 要求）
                        int[] mappedEffectIds = mappedEffectIdsList.ToArray();
                        
                        // 解析碰撞信息
                        CollisionShape? collisionShape = null;
                        string collisionInfo = GetValue(TriggerFrameJsonKeys.CollisionInfo)?.ToString() ?? "";
                        
                        if (triggerType.Equals(TriggerFrameJsonKeys.TriggerTypeCollision, System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(collisionInfo))
                        {
                            var parsed = SkillSystem.CollisionInfoParser.Parse(collisionInfo);
                            if (parsed.HasValue)
                                collisionShape = parsed.Value;
                        }
                        
                        // 解析条件
                        TriggerCondition? condition = null;
                        if (triggerType.Equals(TriggerFrameJsonKeys.TriggerTypeCondition, System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(collisionInfo))
                        {
                            condition = ParseCondition(collisionInfo);
                        }
                        
                        // 创建触发帧信息（支持单帧和多帧范围）
                        if (frame.HasValue)
                        {
                            // 单帧
                            results.Add(new TriggerFrameInfo
                            {
                                Frame = frame.Value,
                                Type = TriggerFrameJsonKeys.TypeSkillEffect,
                                TriggerType = triggerType,
                                EffectIds = mappedEffectIds,
                                CollisionShape = collisionShape,
                                Condition = condition,
                                SocketName = socketName
                            });
                        }
                        else if (startFrame.HasValue && endFrame.HasValue)
                        {
                            // 多帧范围：展开为单帧列表
                            for (int frameNum = startFrame.Value; frameNum <= endFrame.Value; frameNum++)
                            {
                                results.Add(new TriggerFrameInfo
                                {
                                    StartFrame = startFrame,
                                    EndFrame = endFrame,
                                    Frame = frameNum, // 当前帧号
                                    Type = TriggerFrameJsonKeys.TypeSkillEffect,
                                    TriggerType = triggerType,
                                    EffectIds = mappedEffectIds,
                                    CollisionShape = collisionShape,
                                    Condition = condition,
                                    SocketName = socketName
                                });
                            }
                        }
                    }
                    else if (type == TriggerFrameJsonKeys.TypeVFX)
                    {
                        // VFX 类型
                        string resourcePath = GetValue(TriggerFrameJsonKeys.ResourcePath)?.ToString() ?? string.Empty;
                        string socketName = GetValue(TriggerFrameJsonKeys.SocketName)?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(resourcePath))
                        {
                            ASLogger.Instance.Warning("VFX trigger missing resourcePath");
                            continue;
                        }
                        
                        // 解析位置偏移（支持 Vector3 对象格式）
                        float[] positionOffset = null;
                        var posObj = GetValue(TriggerFrameJsonKeys.PositionOffset);
                        if (posObj != null)
                        {
                            // 尝试解析为数组
                            if (posObj is Newtonsoft.Json.Linq.JArray posArray && posArray.Count >= 3)
                            {
                                positionOffset = new float[3];
                                if (float.TryParse(posArray[0].ToString(), out float x))
                                    positionOffset[0] = x;
                                if (float.TryParse(posArray[1].ToString(), out float y))
                                    positionOffset[1] = y;
                                if (float.TryParse(posArray[2].ToString(), out float z))
                                    positionOffset[2] = z;
                            }
                            // 尝试解析为 Vector3 对象（Unity 序列化格式）
                            else if (posObj is Newtonsoft.Json.Linq.JObject posJObj)
                            {
                                positionOffset = new float[3];
                                if (posJObj["x"] != null && float.TryParse(posJObj["x"].ToString(), out float x))
                                    positionOffset[0] = x;
                                if (posJObj["y"] != null && float.TryParse(posJObj["y"].ToString(), out float y))
                                    positionOffset[1] = y;
                                if (posJObj["z"] != null && float.TryParse(posJObj["z"].ToString(), out float z))
                                    positionOffset[2] = z;
                            }
                        }
                        
                        // 解析旋转（支持 Vector3 对象格式）
                        float[] rotation = null;
                        var rotObj = GetValue(TriggerFrameJsonKeys.Rotation);
                        if (rotObj != null)
                        {
                            // 尝试解析为数组
                            if (rotObj is Newtonsoft.Json.Linq.JArray rotArray && rotArray.Count >= 3)
                            {
                                rotation = new float[3];
                                if (float.TryParse(rotArray[0].ToString(), out float x))
                                    rotation[0] = x;
                                if (float.TryParse(rotArray[1].ToString(), out float y))
                                    rotation[1] = y;
                                if (float.TryParse(rotArray[2].ToString(), out float z))
                                    rotation[2] = z;
                            }
                            // 尝试解析为 Vector3 对象（Unity 序列化格式）
                            else if (rotObj is Newtonsoft.Json.Linq.JObject rotJObj)
                            {
                                rotation = new float[3];
                                if (rotJObj["x"] != null && float.TryParse(rotJObj["x"].ToString(), out float x))
                                    rotation[0] = x;
                                if (rotJObj["y"] != null && float.TryParse(rotJObj["y"].ToString(), out float y))
                                    rotation[1] = y;
                                if (rotJObj["z"] != null && float.TryParse(rotJObj["z"].ToString(), out float z))
                                    rotation[2] = z;
                            }
                        }
                        
                        // 解析其他参数
                        float scale = 1.0f;
                        var scaleObj = GetValue(TriggerFrameJsonKeys.Scale);
                        if (scaleObj != null)
                            float.TryParse(scaleObj.ToString(), out scale);
                        
                        float playbackSpeed = 1.0f;
                        var playbackSpeedObj = GetValue(TriggerFrameJsonKeys.PlaybackSpeed);
                        if (playbackSpeedObj != null)
                            float.TryParse(playbackSpeedObj.ToString(), out playbackSpeed);
                        
                        bool followCharacter = true;
                        var followCharObj = GetValue(TriggerFrameJsonKeys.FollowCharacter);
                        if (followCharObj != null)
                            bool.TryParse(followCharObj.ToString(), out followCharacter);
                        
                        bool loop = false;
                        var loopObj = GetValue(TriggerFrameJsonKeys.Loop);
                        if (loopObj != null)
                            bool.TryParse(loopObj.ToString(), out loop);
                        
                        // 创建触发帧信息（支持单帧和多帧范围）
                        if (frame.HasValue)
                        {
                            // 单帧
                            results.Add(new TriggerFrameInfo
                            {
                                Frame = frame.Value,
                                Type = TriggerFrameJsonKeys.TypeVFX,
                                VFXResourcePath = resourcePath,
                                VFXPositionOffset = positionOffset,
                                VFXRotation = rotation,
                                VFXScale = scale,
                                VFXPlaybackSpeed = playbackSpeed,
                                VFXFollowCharacter = followCharacter,
                                VFXLoop = loop,
                                SocketName = socketName
                            });
                        }
                        else if (startFrame.HasValue && endFrame.HasValue)
                        {
                            // 多帧范围：展开为单帧列表（只在开始帧触发）
                            for (int frameNum = startFrame.Value; frameNum <= endFrame.Value; frameNum++)
                            {
                                results.Add(new TriggerFrameInfo
                                {
                                    StartFrame = startFrame,
                                    EndFrame = endFrame,
                                    Frame = frameNum, // 当前帧号
                                    Type = TriggerFrameJsonKeys.TypeVFX,
                                    VFXResourcePath = resourcePath,
                                    VFXPositionOffset = positionOffset,
                                    VFXRotation = rotation,
                                    VFXScale = scale,
                                    VFXPlaybackSpeed = playbackSpeed,
                                    VFXFollowCharacter = followCharacter,
                                    VFXLoop = loop,
                                    SocketName = socketName
                                });
                            }
                        }
                    }
                    // SFX 类型暂时跳过（后续实现）
                }
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"Failed to parse trigger frames from JSON: {ex.Message}\nJSON: {jsonStr}");
            }
            
            return results;
        }
        
        /// <summary>
        /// 从旧字符串格式解析触发帧信息（兼容旧数据）
        /// </summary>
        private List<TriggerFrameInfo> ParseTriggerFramesFromString(string triggerFramesStr, int skillLevel)
        {
            var results = new List<TriggerFrameInfo>();
            
            // 智能分割触发帧字符串（忽略括号内的逗号）
            var parts = SplitIgnoringParentheses(triggerFramesStr, ',');
            foreach (var part in parts)
            {
                string trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                
                // 智能解析三部分：Frame / Trigger+Collision / EffectID
                // 考虑碰撞盒信息中可能包含冒号和@符号
                int firstColonIndex = trimmed.IndexOf(':');
                if (firstColonIndex < 0)
                {
                    ASLogger.Instance.Warning($"Invalid trigger frame format (missing colon): {trimmed}");
                    continue;
                }
                
                // 找到右括号位置（如果有）
                int lastParenIndex = trimmed.LastIndexOf(')');
                
                // 在右括号之后查找最后一个冒号
                int searchStartIndex = lastParenIndex > 0 ? lastParenIndex : firstColonIndex + 1;
                int lastColonIndex = trimmed.IndexOf(':', searchStartIndex);
                
                if (lastColonIndex < 0)
                {
                    ASLogger.Instance.Warning($"Invalid trigger frame format (missing effect ID separator): {trimmed}");
                    continue;
                }
                
                // 分割三部分
                string framePart = trimmed.Substring(0, firstColonIndex).Trim().Replace("Frame", "").Trim();
                string triggerPart = trimmed.Substring(firstColonIndex + 1, lastColonIndex - firstColonIndex - 1).Trim();
                string effectIdPart = trimmed.Substring(lastColonIndex + 1).Trim();
                
                // 解析帧范围
                // 支持格式：Frame5 或 Frame5-10
                int startFrame, endFrame;
                
                if (framePart.Contains("-"))
                {
                    // 多帧格式：Frame5-10
                    string[] frameRange = framePart.Split('-');
                    if (frameRange.Length != 2 || 
                        !int.TryParse(frameRange[0].Trim(), out startFrame) ||
                        !int.TryParse(frameRange[1].Trim(), out endFrame))
                    {
                        ASLogger.Instance.Warning($"Failed to parse frame range: {framePart}");
                        continue;
                    }
                    
                    if (startFrame > endFrame)
                    {
                        ASLogger.Instance.Warning($"Start frame ({startFrame}) > end frame ({endFrame}): {framePart}");
                        continue;
                    }
                }
                else
                {
                    // 单帧格式：Frame5
                    if (!int.TryParse(framePart, out startFrame))
                    {
                        ASLogger.Instance.Warning($"Failed to parse frame number: {framePart}");
                        continue;
                    }
                    endFrame = startFrame;
                }
                
                // 解析触发类型和碰撞盒信息
                string triggerType = triggerPart;
                string collisionInfo = "";
                
                // 检查是否包含碰撞盒信息 (格式：Collision(Box:5x2x1@0,1,0))
                if (triggerPart.Contains("(") && triggerPart.Contains(")"))
                {
                    int startIndex = triggerPart.IndexOf('(');
                    int endIndex = triggerPart.IndexOf(')');
                    
                    if (startIndex >= 0 && endIndex > startIndex)
                    {
                        triggerType = triggerPart.Substring(0, startIndex).Trim();
                        collisionInfo = triggerPart.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                    }
                }
                
                // 解析基础效果ID
                if (!int.TryParse(effectIdPart, out var baseEffectId))
                {
                    ASLogger.Instance.Warning($"Failed to parse effect ID: {effectIdPart}");
                    continue;
                }
                    
                    // 应用等级映射获取实际效果ID
                    var effectResult = GetEffectValue(baseEffectId, skillLevel);
                    
                if (effectResult.EffectData != null)
                {
                    // 多帧展开为单帧列表
                    for (int frame = startFrame; frame <= endFrame; frame++)
                    {
                        // 构造 TriggerFrameInfo
                        TriggerFrameInfo triggerInfo = new TriggerFrameInfo
                        {
                            Frame = frame,
                            Type = TriggerFrameJsonKeys.TypeSkillEffect,
                            TriggerType = triggerType,
                            EffectIds = new int[] { effectResult.EffectId }
                        };
                        
                        // 如果是 Collision 类型，从内联的碰撞盒信息解析 CollisionShape
                        if (triggerType.Equals("Collision", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrEmpty(collisionInfo))
                            {
                                var collisionShape = SkillSystem.CollisionInfoParser.Parse(collisionInfo);
                                if (collisionShape.HasValue)
                                {
                                    triggerInfo.CollisionShape = collisionShape.Value;
                                }
                                else
                                {
                                    ASLogger.Instance.Warning($"Failed to parse collision info: {collisionInfo} at frame {frame}");
                                }
                            }
                            else
                            {
                                ASLogger.Instance.Warning($"Collision trigger at frame {frame} but no collision info provided");
                            }
                        }
                        
                        // 如果是 Condition 类型，解析条件（可选）
                        if (triggerType.Equals("Condition", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrEmpty(collisionInfo))
                            {
                                triggerInfo.Condition = ParseCondition(collisionInfo);
                            }
                        }
                        
                        results.Add(triggerInfo);
                    }
                    
                    // 日志（显示帧范围）
                    string frameRange = startFrame == endFrame ? $"Frame{startFrame}" : $"Frame{startFrame}-{endFrame}";
                }
                else
                {
                    ASLogger.Instance.Warning($"Failed to get effect for base {baseEffectId} level {skillLevel}");
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
        
        /// <summary>
        /// 智能分割字符串，忽略括号内的分隔符
        /// </summary>
        private static string[] SplitIgnoringParentheses(string input, char separator)
        {
            var result = new System.Collections.Generic.List<string>();
            var current = new System.Text.StringBuilder();
            int parenthesesDepth = 0;
            
            foreach (char c in input)
            {
                if (c == '(')
                {
                    parenthesesDepth++;
                    current.Append(c);
                }
                else if (c == ')')
                {
                    parenthesesDepth--;
                    current.Append(c);
                }
                else if (c == separator && parenthesesDepth == 0)
                {
                    // 只在括号外的分隔符才分割
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString().Trim());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            
            // 添加最后一个片段
            if (current.Length > 0)
            {
                result.Add(current.ToString().Trim());
            }
            
            return result.ToArray();
        }
    }
}

