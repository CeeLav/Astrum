using System;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;
using cfg.Skill;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 伤害计算模块
    /// 负责根据配置和属性计算最终伤害
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// 计算伤害
        /// </summary>
        /// <param name="caster">施法者实体</param>
        /// <param name="target">目标实体</param>
        /// <param name="effectConfig">效果配置</param>
        /// <returns>伤害结果</returns>
        public static DamageResult Calculate(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            // 获取施法者和目标的属性（简化版）
            // TODO: 接入完整的属性系统
            float casterAttack = GetEntityAttack(caster);
            float targetDefense = GetEntityDefense(target);
            float casterCritRate = GetEntityCritRate(caster);
            float casterCritDamage = GetEntityCritDamage(caster);
            
            ASLogger.Instance.Debug($"[DamageCalc] Input - CasterATK: {casterAttack}, TargetDEF: {targetDefense}, " +
                $"CritRate: {casterCritRate:P1}, CritDmg: {casterCritDamage:F2}x, EffectValue: {effectConfig.EffectValue}");
            
            // 1. 计算基础伤害
            float baseDamage = CalculateBaseDamage(casterAttack, effectConfig);
            ASLogger.Instance.Debug($"[DamageCalc] Base damage: {baseDamage:F2}");
            
            // 2. 暴击判定
            bool isCritical = CheckCritical(casterCritRate);
            if (isCritical)
            {
                float beforeCrit = baseDamage;
                baseDamage *= casterCritDamage;
                ASLogger.Instance.Info($"[DamageCalc] 💥 CRITICAL HIT! {beforeCrit:F2} × {casterCritDamage:F2} = {baseDamage:F2}");
            }
            
            // 3. 应用防御减免
            float afterDefense = ApplyDefense(baseDamage, targetDefense);
            ASLogger.Instance.Debug($"[DamageCalc] After defense: {baseDamage:F2} → {afterDefense:F2} (DEF: {targetDefense})");
            float finalDamage = afterDefense;
            
            // 4. 应用属性克制（简化版）
            finalDamage = ApplyElementalModifier(finalDamage, caster, target, effectConfig);
            
            // 5. 应用随机浮动
            finalDamage = ApplyRandomVariance(finalDamage);
            
            // 6. 确保伤害非负
            finalDamage = Math.Max(0, finalDamage);
            
            ASLogger.Instance.Info($"[DamageCalc] RESULT - Final damage: {finalDamage:F2} (Critical: {isCritical})");
            
            // 7. 构造结果
            return new DamageResult
            {
                FinalDamage = finalDamage,
                IsCritical = isCritical,
                DamageType = ParseDamageType(effectConfig.EffectParams)
            };
        }
        
        /// <summary>
        /// 计算基础伤害
        /// </summary>
        private static float CalculateBaseDamage(float casterAttack, SkillEffectTable effectConfig)
        {
            // EffectValue 通常是百分比（如150表示150%攻击力）
            float ratio = effectConfig.EffectValue / 100f;
            return casterAttack * ratio;
        }
        
        /// <summary>
        /// 暴击判定
        /// </summary>
        private static bool CheckCritical(float critRate)
        {
            // 使用随机数判定是否暴击
            var random = new Random();
            return random.NextDouble() < critRate;
        }
        
        /// <summary>
        /// 应用防御减免
        /// </summary>
        private static float ApplyDefense(float baseDamage, float defense)
        {
            // 简单的防御公式：减伤百分比 = 防御 / (防御 + 100)
            // 防御20时约17%减伤，防御100时约50%减伤
            float damageReduction = defense / (defense + 100f);
            return baseDamage * (1f - damageReduction);
        }
        
        /// <summary>
        /// 应用属性克制
        /// </summary>
        private static float ApplyElementalModifier(float damage, Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            // TODO: 实现属性克制逻辑
            // 解析伤害类型
            // var damageType = ParseDamageType(effectConfig.EffectParams);
            
            // 根据属性克制关系调整伤害
            // float multiplier = GetElementalMultiplier(casterElement, targetElement);
            // return damage * multiplier;
            
            // 当前简化：不做克制
            return damage;
        }
        
        /// <summary>
        /// 应用随机浮动（±5%）
        /// </summary>
        private static float ApplyRandomVariance(float damage)
        {
            var random = new Random();
            float variance = (float)(random.NextDouble() * 0.1 + 0.95); // [0.95, 1.05]
            return damage * variance;
        }
        
        /// <summary>
        /// 解析伤害类型
        /// </summary>
        private static DamageType ParseDamageType(string effectParams)
        {
            if (string.IsNullOrEmpty(effectParams))
                return DamageType.Physical;
            
            // 解析格式："DamageType:Physical" 或 "DamageType:Magical"
            if (effectParams.Contains("Physical"))
                return DamageType.Physical;
            else if (effectParams.Contains("Magical"))
                return DamageType.Magical;
            else if (effectParams.Contains("True"))
                return DamageType.True;
            
            return DamageType.Physical;
        }
        
        // ========== 属性获取（简化版）==========
        
        /// <summary>
        /// 获取实体攻击力（简化版）
        /// </summary>
        private static float GetEntityAttack(Entity entity)
        {
            // TODO: 从属性系统获取
            // var statComp = entity.GetComponent<StatComponent>();
            // return statComp?.Attack ?? 10f;
            
            // 临时简化：固定返回
            return 100f;
        }
        
        /// <summary>
        /// 获取实体防御力（简化版）
        /// </summary>
        private static float GetEntityDefense(Entity entity)
        {
            // TODO: 从属性系统获取
            return 20f;
        }
        
        /// <summary>
        /// 获取实体暴击率（简化版）
        /// </summary>
        private static float GetEntityCritRate(Entity entity)
        {
            // TODO: 从属性系统获取
            return 0.2f; // 20%暴击率
        }
        
        /// <summary>
        /// 获取实体暴击伤害（简化版）
        /// </summary>
        private static float GetEntityCritDamage(Entity entity)
        {
            // TODO: 从属性系统获取
            return 2.0f; // 暴击2倍伤害
        }
    }
}

