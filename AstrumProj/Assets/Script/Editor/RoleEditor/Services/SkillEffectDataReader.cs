using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 技能效果数据读取服务
    /// 从 SkillEffectTable.csv 读取效果配置
    /// </summary>
    public static class SkillEffectDataReader
    {
        private const string LOG_PREFIX = "[SkillEffectDataReader]";
        
        // 缓存
        private static List<SkillEffectTableData> _cachedEffects = null;
        private static Dictionary<int, SkillEffectTableData> _cachedEffectsById = null;
        
        /// <summary>
        /// 读取所有技能效果配置
        /// </summary>
        public static List<SkillEffectTableData> ReadAllSkillEffects()
        {
            if (_cachedEffects != null)
                return _cachedEffects;
            
            try
            {
                var config = SkillEffectTableData.GetTableConfig();
                _cachedEffects = LubanCSVReader.ReadTable<SkillEffectTableData>(config);
                
                Debug.Log($"{LOG_PREFIX} 读取到 {_cachedEffects.Count} 个技能效果配置");
                
                return _cachedEffects;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} 读取技能效果配置失败: {ex.Message}");
                return new List<SkillEffectTableData>();
            }
        }
        
        /// <summary>
        /// 按效果类型分组
        /// </summary>
        public static Dictionary<int, List<SkillEffectTableData>> GroupByEffectType()
        {
            var allEffects = ReadAllSkillEffects();
            return allEffects.GroupBy(e => e.EffectType)
                            .ToDictionary(g => g.Key, g => g.ToList());
        }
        
        /// <summary>
        /// 获取单个效果配置
        /// </summary>
        public static SkillEffectTableData GetSkillEffect(int effectId)
        {
            // 构建索引（首次调用时）
            if (_cachedEffectsById == null)
            {
                var allEffects = ReadAllSkillEffects();
                _cachedEffectsById = allEffects.ToDictionary(e => e.SkillEffectId);
            }
            
            return _cachedEffectsById.TryGetValue(effectId, out var effect) ? effect : null;
        }
        
        /// <summary>
        /// 搜索效果（按名称或ID）
        /// </summary>
        public static List<SkillEffectTableData> SearchEffects(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return ReadAllSkillEffects();
            
            var allEffects = ReadAllSkillEffects();
            keyword = keyword.ToLower();
            
            return allEffects.Where(e => 
                e.SkillEffectId.ToString().Contains(keyword) ||
                GetEffectTypeName(e.EffectType).Contains(keyword)
            ).ToList();
        }
        
        /// <summary>
        /// 按效果类型筛选
        /// </summary>
        public static List<SkillEffectTableData> FilterByEffectType(int effectType)
        {
            if (effectType == 0)
                return ReadAllSkillEffects();
            
            var allEffects = ReadAllSkillEffects();
            return allEffects.Where(e => e.EffectType == effectType).ToList();
        }
        
        /// <summary>
        /// 获取所有效果类型列表
        /// </summary>
        public static List<int> GetAllEffectTypes()
        {
            var allEffects = ReadAllSkillEffects();
            return allEffects.Select(e => e.EffectType).Distinct().OrderBy(t => t).ToList();
        }
        
        /// <summary>
        /// 清除缓存
        /// </summary>
        public static void ClearCache()
        {
            _cachedEffects = null;
            _cachedEffectsById = null;
            Debug.Log($"{LOG_PREFIX} 缓存已清除");
        }
        
        /// <summary>
        /// 获取效果类型名称
        /// </summary>
        public static string GetEffectTypeName(int effectType)
        {
            return effectType switch
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
        public static string GetTargetTypeName(int targetType)
        {
            return targetType switch
            {
                1 => "敌人",
                2 => "友军",
                3 => "自身",
                4 => "全体",
                _ => "未知"
            };
        }
        
        /// <summary>
        /// 获取效果类型图标
        /// </summary>
        public static string GetEffectTypeIcon(int effectType)
        {
            return effectType switch
            {
                1 => "⚔️", // 伤害
                2 => "💚", // 治疗
                3 => "💨", // 击退
                4 => "✨", // Buff
                5 => "🔻", // Debuff
                _ => "❓"  // 未知
            };
        }
    }
}

