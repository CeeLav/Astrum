using System;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Stats;
using Astrum.CommonBase;
using cfg.Skill;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 伤害计算模块 - 使用完整属性系统和确定性计算
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// 计算伤害（完整版：接入属性系统 + 确定性计算）
        /// </summary>
        /// <param name="caster">施法者实体</param>
        /// <param name="target">目标实体</param>
        /// <param name="effectConfig">效果配置</param>
        /// <param name="currentFrame">当前逻辑帧号（用于确定性随机）</param>
        /// <returns>伤害结果</returns>
        public static DamageResult Calculate(Entity caster, Entity target, SkillEffectTable effectConfig, int currentFrame)
        {
            // 1. 获取施法者和目标的派生属性
            var casterDerived = caster.GetComponent<DerivedStatsComponent>();
            var targetDerived = target.GetComponent<DerivedStatsComponent>();
            var targetState = target.GetComponent<StateComponent>();
            
            // 2. 检查是否可以受伤
            if (targetState != null && !targetState.CanTakeDamage())
            {
                ASLogger.Instance.Debug($"[DamageCalc] Target {target.UniqueId} is immune to damage");
                return new DamageResult { FinalDamage = FP.Zero, IsCritical = false };
            }
            
            // 3. 获取属性值（定点数）
            FP casterAttack = casterDerived?.Get(StatType.ATK) ?? (FP)100;
            FP casterAccuracy = casterDerived?.Get(StatType.ACCURACY) ?? (FP)0.95;
            FP casterCritRate = casterDerived?.Get(StatType.CRIT_RATE) ?? (FP)0.05;
            FP casterCritDamage = casterDerived?.Get(StatType.CRIT_DMG) ?? (FP)2.0;
            
            FP targetDefense = targetDerived?.Get(StatType.DEF) ?? (FP)20;
            FP targetEvasion = targetDerived?.Get(StatType.EVASION) ?? (FP)0.05;
            FP targetBlockRate = targetDerived?.Get(StatType.BLOCK_RATE) ?? (FP)0.1;
            FP targetBlockValue = targetDerived?.Get(StatType.BLOCK_VALUE) ?? (FP)50;
            
            //ASLogger.Instance.Debug($"[DamageCalc] Caster ATK={casterAttack}, CRIT={casterCritRate}, Target DEF={targetDefense}");
            //ASLogger.Instance.Debug($"[DamageCalc] Frame={currentFrame}, CasterId={caster.UniqueId}, TargetId={target.UniqueId}");
            
            Stats.DamageType damageType = effectConfig.GetDamageTypeEnum();

            // 4. 计算基础伤害
            FP baseDamage = CalculateBaseDamage(casterAttack, effectConfig);
            // 属性缩放
            int scalingStatCode = effectConfig.GetIntParam(3, 0);
            int scalingRatio = effectConfig.GetIntParam(4, 0);
            if (scalingStatCode > 0 && scalingRatio != 0)
            {
                FP scalingValue = GetScalingStatValue(casterDerived, scalingStatCode);
                if (scalingValue > FP.Zero)
                {
                    FP scalingMultiplier = (FP)scalingRatio / (FP)1000;
                    baseDamage += scalingValue * scalingMultiplier;
                }
            }
            ASLogger.Instance.Debug($"[DamageCalc] Base damage: {(float)baseDamage:F2}");
            /*
            // 5. 命中判定
            int seed1 = GenerateSeed(currentFrame, caster.UniqueId, target.UniqueId, 1);
            ASLogger.Instance.Debug($"[DamageCalc] Hit seed: {seed1}");
            if (!CheckHit(casterAccuracy, targetEvasion, seed1))
            {
                ASLogger.Instance.Debug($"[DamageCalc] ❌ MISS! (Accuracy={casterAccuracy}, Evasion={targetEvasion})");
                return new DamageResult { FinalDamage = FP.Zero, IsCritical = false, IsMiss = true };
            }
            
            // 6. 格挡判定
            int seed2 = GenerateSeed(currentFrame, caster.UniqueId, target.UniqueId, 2);
            ASLogger.Instance.Debug($"[DamageCalc] Block seed: {seed2}");
            bool isBlocked = CheckBlock(targetBlockRate, seed2);
            if (isBlocked)
            {
                FP beforeBlock = baseDamage;
                baseDamage = TSMath.Max(FP.Zero, baseDamage - targetBlockValue);
                ASLogger.Instance.Debug($"[DamageCalc] 🛡️ BLOCKED! {(float)beforeBlock:F2} - {(float)targetBlockValue:F2} = {(float)baseDamage:F2}");
            }
            
            // 7. 暴击判定
            int seed3 = GenerateSeed(currentFrame, caster.UniqueId, target.UniqueId, 3);
            ASLogger.Instance.Debug($"[DamageCalc] Crit seed: {seed3}");
            bool isCritical = CheckCritical(casterCritRate, seed3);
            if (isCritical)
            {
                FP beforeCrit = baseDamage;
                baseDamage *= casterCritDamage;
                ASLogger.Instance.Debug($"[DamageCalc] 💥 CRITICAL HIT! {(float)beforeCrit:F2} × {(float)casterCritDamage:F2} = {(float)baseDamage:F2}");
            }*/
            
            // 8. 应用防御减免
            FP afterDefense = ApplyDefense(baseDamage, targetDefense, damageType);
            ASLogger.Instance.Debug($"[DamageCalc] After defense: {(float)baseDamage:F2} → {(float)afterDefense:F2}");
            
            // 9. 应用抗性
            FP afterResistance = ApplyResistance(afterDefense, targetDerived, damageType);
            ASLogger.Instance.Debug($"[DamageCalc] After resistance: {(float)afterDefense:F2} → {(float)afterResistance:F2}");
            
            // 10. 应用随机浮动（注意：使用确定性随机）
            //int seed4 = GenerateSeed(currentFrame, caster.UniqueId, target.UniqueId, 4);
            //ASLogger.Instance.Debug($"[DamageCalc] Variance seed: {seed4}");
            FP finalDamage = afterResistance;//ApplyDeterministicVariance(afterResistance, seed4);
            
            // 11. 确保非负
            finalDamage = TSMath.Max(FP.Zero, finalDamage);
            
            //ASLogger.Instance.Debug($"[DamageCalc] RESULT - Final damage: {(float)finalDamage:F2} (Crit: {isCritical}, Block: {isBlocked})");
            
            return new DamageResult
            {
                FinalDamage = finalDamage,
                IsCritical = false,
                IsBlocked = false,
                IsMiss = false,
                DamageType = damageType
            };
        }
        
        // ========== 伤害计算辅助方法 ==========
        
        /// <summary>
        /// 计算基础伤害
        /// </summary>
        private static FP CalculateBaseDamage(FP attack, SkillEffectTable effectConfig)
        {
            // effectValue 通常是百分比*1000（如1500表示150%攻击力）
            int baseCoefficient = effectConfig.GetIntParam(2);
            FP ratio = (FP)baseCoefficient / (FP)1000;
            return attack * ratio;
        }

        private static FP GetScalingStatValue(DerivedStatsComponent casterDerived, int code)
        {
            if (casterDerived == null)
                return FP.Zero;

            return code switch
            {
                1 => casterDerived.Get(StatType.ATK),
                2 => casterDerived.Get(StatType.DEF),
                3 => casterDerived.Get(StatType.HP),
                4 => casterDerived.Get(StatType.MAX_MANA),
                _ => FP.Zero
            };
        }
        
        /// <summary>
        /// 命中判定（确定性）
        /// </summary>
        private static bool CheckHit(FP accuracy, FP evasion, int randomSeed)
        {
            // 最终命中概率 = 命中率 - 闪避率
            FP hitChance = accuracy - evasion;
            
            // 极限约束 [10%, 100%]
            hitChance = TSMath.Clamp(hitChance, (FP)0.1, FP.One);
            
            FP roll = GenerateDeterministicRandom(randomSeed);
            return roll < hitChance;
        }
        
        /// <summary>
        /// 格挡判定（确定性）
        /// </summary>
        private static bool CheckBlock(FP blockRate, int randomSeed)
        {
            FP roll = GenerateDeterministicRandom(randomSeed);
            return roll < blockRate;
        }
        
        /// <summary>
        /// 暴击判定（确定性）
        /// </summary>
        private static bool CheckCritical(FP critRate, int randomSeed)
        {
            FP roll = GenerateDeterministicRandom(randomSeed);
            return roll < critRate;
        }
        
        /// <summary>
        /// 应用防御减免
        /// </summary>
        private static FP ApplyDefense(FP baseDamage, FP defense, Stats.DamageType damageType)
        {
            // 真实伤害无视防御
            if (damageType == Stats.DamageType.True)
                return baseDamage;
            
            // 减伤公式：减伤百分比 = 防御 / (防御 + 100)
            FP damageReduction = defense / (defense + (FP)100);
            return baseDamage * (FP.One - damageReduction);
        }
        
        /// <summary>
        /// 应用抗性减免
        /// </summary>
        private static FP ApplyResistance(FP damage, DerivedStatsComponent targetDerived, Stats.DamageType damageType)
        {
            if (targetDerived == null)
                return damage;
            
            FP resistance = FP.Zero;
            
            switch (damageType)
            {
                case Stats.DamageType.Physical:
                    resistance = targetDerived.Get(StatType.PHYSICAL_RES);
                    break;
                case Stats.DamageType.Magical:
                    resistance = targetDerived.Get(StatType.MAGICAL_RES);
                    break;
                case Stats.DamageType.True:
                    return damage; // 真实伤害无视抗性
            }
            
            return damage * (FP.One - resistance);
        }
        
        /// <summary>
        /// 应用随机浮动（±5%，确定性）
        /// </summary>
        private static FP ApplyDeterministicVariance(FP damage, int randomSeed)
        {
            FP variance = GenerateDeterministicRandom(randomSeed) * (FP)0.1 + (FP)0.95; // [0.95, 1.05]
            return damage * variance;
        }
        
        /// <summary>
        /// 生成确定性随机种子
        /// </summary>
        /// <param name="frame">当前逻辑帧号</param>
        /// <param name="casterId">施法者ID</param>
        /// <param name="targetId">目标ID</param>
        /// <param name="sequence">序列号（同一帧多次随机判定时递增）</param>
        /// <returns>确定性种子</returns>
        private static int GenerateSeed(int frame, long casterId, long targetId, int sequence)
        {
            // 确保所有客户端生成相同种子
            return HashCode.Combine(frame, casterId, targetId, sequence);
        }
        
        /// <summary>
        /// 生成确定性随机数 [0, 1)（使用简单哈希算法）
        /// </summary>
        private static FP GenerateDeterministicRandom(int seed)
        {
            // LCG (Linear Congruential Generator) 算法
            // 确保相同的种子产生相同的结果
            uint state = (uint)seed;
            state = state * 1664525u + 1013904223u;
            
            // 将 uint 映射到 [0, 1)
            FP result = (FP)(state & 0x7FFFFFFF) / (FP)0x7FFFFFFF;
            return result;
        }
    }
}
