using System;
using System.Collections.Generic;
using UnityEngine;

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
        
        /// <summary>效果ID</summary>
        public int EffectId = 0;
        
        /// <summary>触发类型</summary>
        public string TriggerType = "Direct"; // Direct, Collision, Condition
        
        /// <summary>碰撞盒信息（仅Collision类型使用）</summary>
        public string CollisionInfo = ""; // 格式：Box:5x2x1, Sphere:3.0, Capsule:2x5, Point
        
        // === 效果详情（从配置表读取，缓存用于显示） ===
        
        [HideInInspector]
        public string EffectName = "";
        
        [HideInInspector]
        public int EffectType = 0; // 1=伤害, 2=治疗, 3=击退, 4=Buff, 5=Debuff
        
        [HideInInspector]
        public float EffectValue = 0f;
        
        [HideInInspector]
        public float EffectRange = 0f;
        
        [HideInInspector]
        public int TargetType = 0;
        
        [HideInInspector]
        public float EffectDuration = 0f;
        
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
                EffectId = 0,
                TriggerType = "Direct",
                CollisionInfo = "",
                EffectName = "",
                EffectType = 0,
                EffectValue = 0f,
                EffectRange = 0f,
                TargetType = 0,
                EffectDuration = 0f,
                CollisionShapeType = "",
                CollisionShapeSize = ""
            };
        }
        
        /// <summary>
        /// 从配置表创建（带效果详情）
        /// </summary>
        public static SkillEffectEventData CreateFromTable(int effectId, string triggerType = "Direct", string collisionInfo = "")
        {
            var data = new SkillEffectEventData
            {
                EffectId = effectId,
                TriggerType = triggerType,
                CollisionInfo = collisionInfo
            };
            
            data.RefreshFromTable();
            data.ParseCollisionInfo();
            
            return data;
        }
        
        /// <summary>
        /// 克隆数据
        /// </summary>
        public SkillEffectEventData Clone()
        {
            return new SkillEffectEventData
            {
                EffectId = this.EffectId,
                TriggerType = this.TriggerType,
                CollisionInfo = this.CollisionInfo,
                EffectName = this.EffectName,
                EffectType = this.EffectType,
                EffectValue = this.EffectValue,
                EffectRange = this.EffectRange,
                TargetType = this.TargetType,
                EffectDuration = this.EffectDuration,
                CollisionShapeType = this.CollisionShapeType,
                CollisionShapeSize = this.CollisionShapeSize
            };
        }
        
        // === 辅助方法 ===
        
        /// <summary>
        /// 从配置表刷新效果详情
        /// </summary>
        public void RefreshFromTable()
        {
            if (EffectId <= 0)
            {
                ClearEffectDetails();
                return;
            }
            
            try
            {
                var effectConfig = Services.SkillEffectDataReader.GetSkillEffect(EffectId);
                       if (effectConfig != null)
                       {
                           // 根据效果类型和数值生成友好的名称
                           EffectName = GenerateEffectName(effectConfig);
                           EffectType = effectConfig.EffectType;
                           EffectValue = effectConfig.EffectValue;
                           EffectRange = effectConfig.EffectRange;
                           TargetType = effectConfig.TargetType;
                           EffectDuration = effectConfig.EffectDuration;
                       }
                else
                {
                    Debug.LogWarning($"[SkillEffectEventData] 效果ID {EffectId} 在配置表中不存在");
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
            EffectType = 0;
            EffectValue = 0f;
            EffectRange = 0f;
            TargetType = 0;
            EffectDuration = 0f;
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
                   string typeName = config.EffectType switch
                   {
                       1 => "伤害",
                       2 => "治疗", 
                       3 => "击退",
                       4 => "Buff",
                       5 => "Debuff",
                       _ => "效果"
                   };
                   
                   // 格式：类型 + 数值
                   if (config.EffectValue > 0)
                   {
                       return $"{typeName} {config.EffectValue}";
                   }
                   
                   return $"{typeName}_{config.SkillEffectId}";
               }
               
               /// <summary>
               /// 获取显示名称
               /// </summary>
               public string GetDisplayName()
               {
                   if (EffectId == 0) return "[未设置效果]";
                   
                   return !string.IsNullOrEmpty(EffectName) ? EffectName : $"效果_{EffectId}";
               }
        
        /// <summary>
        /// 获取详细信息文本
        /// </summary>
        public string GetDetailText()
        {
            if (EffectId == 0) return "未设置效果";
            
            string text = $"{GetDisplayName()}\n";
            text += $"类型: {GetEffectTypeName()}\n";
            text += $"数值: {EffectValue}\n";
            text += $"范围: {EffectRange}m\n";
            text += $"目标: {GetTargetTypeName()}\n";
            
            if (EffectDuration > 0)
            {
                text += $"持续: {EffectDuration}秒\n";
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
            return EffectType switch
            {
                1 => "伤害",
                2 => "治疗",
                3 => "击退",
                4 => "Buff",
                5 => "Debuff",
                _ => "未知"
            };
        }
        
        /// <summary>
        /// 获取目标类型名称
        /// </summary>
        public string GetTargetTypeName()
        {
            return TargetType switch
            {
                1 => "敌人",
                2 => "友军",
                3 => "自身",
                4 => "全体",
                _ => "未知"
            };
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
            return EffectType switch
            {
                1 => new Color(1f, 0.3f, 0.3f),    // 伤害 - 红色
                2 => new Color(0.3f, 1f, 0.3f),    // 治疗 - 绿色
                3 => new Color(1f, 0.7f, 0.2f),    // 击退 - 橙色
                4 => new Color(0.4f, 0.7f, 1f),    // Buff - 蓝色
                5 => new Color(0.8f, 0.4f, 1f),    // Debuff - 紫色
                _ => Color.gray                     // 未知 - 灰色
            };
        }
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            
            if (EffectId <= 0)
            {
                errors.Add("效果ID必须大于0");
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

