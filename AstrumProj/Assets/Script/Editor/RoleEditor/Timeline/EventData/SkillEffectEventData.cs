using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.Editor.RoleEditor.Services;
using System.Linq;

namespace Astrum.Editor.RoleEditor.Timeline.EventData
{
    /// <summary>
    /// 技能效果事件数据
    /// 用于时间轴的技能效果轨道
    /// </summary>
    [Serializable]
    public class SkillEffectEventData
    {
        // === 核心数据 ===
        
        /// <summary>效果ID列表（支持多个效果）</summary>
        public List<int> EffectIds = new List<int>();
        
        /// <summary>触发类型</summary>
        public string TriggerType = "Direct"; // Direct, Collision, Condition
        
        /// <summary>碰撞盒信息（仅Collision类型使用）</summary>
        public string CollisionInfo = ""; // 格式：Box:5x2x1, Sphere:3.0, Capsule:2x5, Point
        
        // === 效果详情（从配置表读取，缓存用于显示） ===
        
        [HideInInspector]
        public string EffectName = "";
        
        [HideInInspector]
        public string EffectTypeKey = string.Empty;
        
        [HideInInspector]
        public float PrimaryValue = 0f;
        
        [HideInInspector]
        public int TargetSelector = 0;
        
        [HideInInspector]
        public List<int> IntParamsSnapshot = new List<int>();
        
        [HideInInspector]
        public List<string> StringParamsSnapshot = new List<string>();
        
        // === 碰撞盒解析结果 ===
        
        [HideInInspector]
        public string CollisionShapeType = ""; // Box, Sphere, Capsule, Point
        
        [HideInInspector]
        public string CollisionShapeSize = ""; // "5x2x1" 或 "3.0"
        
        // === 工厂方法 ===
        
        /// <summary>
        /// 创建默认数据
        /// </summary>
        public static SkillEffectEventData CreateDefault()
        {
            return new SkillEffectEventData
            {
                EffectIds = new List<int>(),
                TriggerType = "Direct",
                CollisionInfo = "",
                EffectName = "",
                EffectTypeKey = string.Empty,
                PrimaryValue = 0f,
                TargetSelector = 0,
                IntParamsSnapshot = new List<int>(),
                StringParamsSnapshot = new List<string>(),
                CollisionShapeType = "",
                CollisionShapeSize = ""
            };
        }
        
        /// <summary>
        /// 从配置表创建（带效果详情）
        /// </summary>
        public static SkillEffectEventData CreateFromTable(List<int> effectIds, string triggerType = "Direct", string collisionInfo = "")
        {
            var data = new SkillEffectEventData
            {
                EffectIds = effectIds ?? new List<int>(),
                TriggerType = triggerType,
                CollisionInfo = collisionInfo
            };
            
            data.RefreshFromTable();
            data.ParseCollisionInfo();
            
            return data;
        }
        
        /// <summary>
        /// 从配置表创建（单个效果ID，兼容旧代码）
        /// </summary>
        public static SkillEffectEventData CreateFromTable(int effectId, string triggerType = "Direct", string collisionInfo = "")
        {
            return CreateFromTable(new List<int> { effectId }, triggerType, collisionInfo);
        }
        
        /// <summary>
        /// 克隆数据
        /// </summary>
        public SkillEffectEventData Clone()
        {
            return new SkillEffectEventData
            {
                EffectIds = new List<int>(this.EffectIds),
                TriggerType = this.TriggerType,
                CollisionInfo = this.CollisionInfo,
                EffectName = this.EffectName,
                EffectTypeKey = this.EffectTypeKey,
                PrimaryValue = this.PrimaryValue,
                TargetSelector = this.TargetSelector,
                IntParamsSnapshot = new List<int>(this.IntParamsSnapshot ?? new List<int>()),
                StringParamsSnapshot = new List<string>(this.StringParamsSnapshot ?? new List<string>()),
                CollisionShapeType = this.CollisionShapeType,
                CollisionShapeSize = this.CollisionShapeSize
            };
        }
        
        // === 辅助方法 ===
        
        /// <summary>
        /// 从配置表刷新效果详情（使用第一个效果ID）
        /// </summary>
        public void RefreshFromTable()
        {
            if (EffectIds == null || EffectIds.Count == 0 || EffectIds[0] <= 0)
            {
                ClearEffectDetails();
                return;
            }
            
            try
            {
                // 使用第一个效果ID来显示详情
                int primaryEffectId = EffectIds[0];
                var effectConfig = Services.SkillEffectDataReader.GetSkillEffect(primaryEffectId);
                if (effectConfig != null)
                {
                    if (EffectIds.Count > 1)
                    {
                        EffectName = GenerateEffectName(effectConfig) + $" +{EffectIds.Count - 1}";
                    }
                    else
                    {
                        EffectName = GenerateEffectName(effectConfig);
                    }

                    EffectTypeKey = effectConfig.EffectType ?? string.Empty;
                    IntParamsSnapshot = new List<int>(effectConfig.IntParams ?? new List<int>());
                    StringParamsSnapshot = new List<string>(effectConfig.StringParams ?? new List<string>());
                    TargetSelector = IntParamsSnapshot.Count > 0 ? IntParamsSnapshot[0] : 0;
                    PrimaryValue = ComputePrimaryValue(effectConfig);
                }
                else
                {
                    Debug.LogWarning($"[SkillEffectEventData] 效果ID {primaryEffectId} 在配置表中不存在");
                    ClearEffectDetails();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillEffectEventData] 刷新效果详情失败: {ex.Message}");
                ClearEffectDetails();
            }
        }
        
        /// <summary>
        /// 清空效果详情
        /// </summary>
        private void ClearEffectDetails()
        {
            EffectName = "";
            EffectTypeKey = string.Empty;
            PrimaryValue = 0f;
            TargetSelector = 0;
            IntParamsSnapshot = new List<int>();
            StringParamsSnapshot = new List<string>();
        }
        
        /// <summary>
        /// 解析碰撞盒信息
        /// </summary>
        public void ParseCollisionInfo()
        {
            if (string.IsNullOrEmpty(CollisionInfo))
            {
                CollisionShapeType = "";
                CollisionShapeSize = "";
                return;
            }
            
            // "Box:5x2x1" → Type="Box", Size="5x2x1"
            string[] parts = CollisionInfo.Split(':');
            if (parts.Length >= 1)
            {
                CollisionShapeType = parts[0].Trim();
                CollisionShapeSize = parts.Length > 1 ? parts[1].Trim() : "";
            }
        }
        
               /// <summary>
               /// 根据效果配置生成友好的名称
               /// </summary>
        private static string GenerateEffectName(Persistence.Mappings.SkillEffectTableData config)
        {
            string typeName = SkillEffectDataReader.GetEffectTypeDisplayName(config.EffectType);
            float primary = ComputePrimaryValue(config);

            if (primary > 0f)
            {
                if (config.EffectType != null && config.EffectType.Equals("knockback", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{typeName} {primary:0.##}m";
                }

                if (config.EffectType != null && (config.EffectType.Equals("damage", StringComparison.OrdinalIgnoreCase) || config.EffectType.Equals("heal", StringComparison.OrdinalIgnoreCase)))
                {
                    return $"{typeName} {primary:0.#}%";
                }

                return $"{typeName} {primary:0.##}";
            }

            return $"{typeName}_{config.SkillEffectId}";
        }
        
        private static float ComputePrimaryValue(Persistence.Mappings.SkillEffectTableData config)
        {
            var ints = config.IntParams ?? new List<int>();
            switch ((config.EffectType ?? string.Empty).ToLower())
            {
                case "damage":
                case "heal":
                    return ints.Count > 2 ? ints[2] / 10f : 0f;
                case "knockback":
                    return ints.Count > 1 ? ints[1] / 1000f : 0f;
                case "status":
                    return ints.Count > 2 ? ints[2] / 1000f : 0f;
                case "teleport":
                    return ints.Count > 1 ? ints[1] / 1000f : 0f;
                default:
                    return 0f;
            }
        }
               
               /// <summary>
               /// 获取显示名称
               /// </summary>
               public string GetDisplayName()
               {
                   if (EffectIds == null || EffectIds.Count == 0 || EffectIds[0] == 0) return "[未设置效果]";
                   
                   int primaryEffectId = EffectIds[0];
                   return !string.IsNullOrEmpty(EffectName) ? EffectName : $"效果_{primaryEffectId}";
               }
        
        /// <summary>
        /// 获取详细信息文本
        /// </summary>
        public string GetDetailText()
        {
            if (EffectIds == null || EffectIds.Count == 0 || EffectIds[0] == 0) return "未设置效果";
            
            string text = $"{GetDisplayName()}\n";
            text += $"类型: {GetEffectTypeName()}\n";
            text += $"主值: {FormatPrimaryValue()}\n";
            text += $"目标: {GetTargetTypeName()}\n";

            if (IntParamsSnapshot != null && IntParamsSnapshot.Count > 0)
            {
                text += $"参数(Int): {string.Join("|", IntParamsSnapshot)}\n";
            }

            if (StringParamsSnapshot != null && StringParamsSnapshot.Count > 0)
            {
                text += $"参数(Str): {string.Join(" | ", StringParamsSnapshot)}\n";
            }
            
            if (!string.IsNullOrEmpty(CollisionInfo))
            {
                text += $"碰撞盒: {CollisionShapeType} ({CollisionShapeSize})";
            }
            
            return text;
        }
        
        /// <summary>
        /// 获取效果类型名称
        /// </summary>
        public string GetEffectTypeName()
        {
            return SkillEffectDataReader.GetEffectTypeDisplayName(EffectTypeKey);
        }
        
        /// <summary>
        /// 获取目标类型名称
        /// </summary>
        public string GetTargetTypeName()
        {
            return TargetSelector switch
            {
                0 => "自身",
                1 => "敌人",
                2 => "友军",
                3 => "区域",
                _ => "未知"
            };
        }

        private string FormatPrimaryValue()
        {
            if (PrimaryValue <= 0f)
                return "--";

            switch ((EffectTypeKey ?? string.Empty).ToLower())
            {
                case "damage":
                case "heal":
                    return $"{PrimaryValue:0.#}%";
                case "knockback":
                case "teleport":
                    return $"{PrimaryValue:0.##}m";
                case "status":
                    return $"{PrimaryValue:0.##}s";
                default:
                    return PrimaryValue.ToString("0.##");
            }
        }
        
        /// <summary>
        /// 获取触发类型图标
        /// </summary>
        public string GetTriggerIcon()
        {
            return TriggerType switch
            {
                "Direct" => "→",
                "Collision" => "💥",
                "Condition" => "❓",
                _ => "?"
            };
        }
        
        /// <summary>
        /// 获取效果类型颜色
        /// </summary>
        public Color GetEffectTypeColor()
        {
            switch ((EffectTypeKey ?? string.Empty).ToLower())
            {
                case "damage":
                    return new Color(1f, 0.3f, 0.3f);
                case "heal":
                    return new Color(0.3f, 1f, 0.3f);
                case "knockback":
                    return new Color(1f, 0.7f, 0.2f);
                case "buff":
                    return new Color(0.4f, 0.7f, 1f);
                case "debuff":
                    return new Color(0.8f, 0.4f, 1f);
                case "status":
                    return new Color(0.9f, 0.6f, 0.2f);
                case "teleport":
                    return new Color(0.4f, 0.9f, 0.9f);
                default:
                    return Color.gray;
            }
        }
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            
            if (EffectIds == null || EffectIds.Count == 0)
            {
                errors.Add("至少需要一个效果ID");
            }
            else
            {
                foreach (var effectId in EffectIds)
                {
                    if (effectId <= 0)
                    {
                        errors.Add($"效果ID {effectId} 无效（必须大于0）");
                    }
                }
            }
            
            if (string.IsNullOrEmpty(TriggerType))
            {
                errors.Add("触发类型不能为空");
            }
            
            if (TriggerType == "Collision" && string.IsNullOrEmpty(CollisionInfo))
            {
                errors.Add("碰撞触发必须指定碰撞盒信息");
            }
            
            // 验证碰撞盒格式
            if (!string.IsNullOrEmpty(CollisionInfo))
            {
                if (!ValidateCollisionInfoFormat(CollisionInfo))
                {
                    errors.Add($"碰撞盒格式错误: {CollisionInfo}");
                }
            }
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// 验证碰撞盒格式
        /// </summary>
        private bool ValidateCollisionInfoFormat(string collisionInfo)
        {
            if (string.IsNullOrEmpty(collisionInfo)) return true;
            
            string[] parts = collisionInfo.Split(':');
            if (parts.Length < 1) return false;
            
            string shapeType = parts[0].Trim().ToLower();
            
            switch (shapeType)
            {
                case "box":
                    // Box:5x2x1
                    if (parts.Length < 2) return false;
                    string[] boxSize = parts[1].Split('x', 'X', '×');
                    return boxSize.Length == 3;
                
                case "sphere":
                    // Sphere:3.0
                    return parts.Length >= 2;
                
                case "capsule":
                    // Capsule:2x5
                    if (parts.Length < 2) return false;
                    string[] capsuleSize = parts[1].Split('x', 'X', '×');
                    return capsuleSize.Length == 2;
                
                case "point":
                    // Point
                    return true;
                
                default:
                    return false;
            }
        }
    }
}

