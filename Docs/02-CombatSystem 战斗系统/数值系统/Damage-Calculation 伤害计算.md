# 伤害计算公式

> ⚔️ 完整的伤害计算系统：基础伤害、防御、抗性、暴击、命中、格挡
>
> **版本**: v1.0  
> **更新**: 2025-10-14

---

## 一、完整伤害计算流程

```csharp
public static DamageResult Calculate(Entity caster, Entity target, SkillEffectTable effectConfig, int randomSeed)
{
    // 1. 获取施法者和目标的派生属性
    var casterDerived = caster.GetComponent<DerivedStatsComponent>();
    var targetDerived = target.GetComponent<DerivedStatsComponent>();
    var targetState = target.GetComponent<StateComponent>();
    
    // 2. 检查是否可以受伤
    if (!targetState.CanTakeDamage())
        return new DamageResult { FinalDamage = FP.Zero, IsCritical = false };
    
    // 3. 获取属性值（定点数）
    FP casterAttack = casterDerived.Get(StatType.ATK);
    FP casterAccuracy = casterDerived.Get(StatType.ACCURACY);
    FP casterCritRate = casterDerived.Get(StatType.CRIT_RATE);
    FP casterCritDamage = casterDerived.Get(StatType.CRIT_DMG);
    
    FP targetDefense = targetDerived.Get(StatType.DEF);
    FP targetEvasion = targetDerived.Get(StatType.EVASION);
    FP targetBlockRate = targetDerived.Get(StatType.BLOCK_RATE);
    FP targetBlockValue = targetDerived.Get(StatType.BLOCK_VALUE);
    
    // 4. 计算基础伤害
    FP baseDamage = CalculateBaseDamage(casterAttack, effectConfig);
    
    // 5. 命中判定
    if (!CheckHit(casterAccuracy, targetEvasion, randomSeed))
        return new DamageResult { FinalDamage = FP.Zero, IsCritical = false, IsMiss = true };
    
    // 6. 格挡判定
    bool isBlocked = CheckBlock(targetBlockRate, randomSeed);
    if (isBlocked)
    {
        baseDamage = FPMath.Max(FP.Zero, baseDamage - targetBlockValue);
    }
    
    // 7. 暴击判定
    bool isCritical = CheckCritical(casterCritRate, randomSeed);
    if (isCritical)
    {
        baseDamage *= casterCritDamage;
    }
    
    // 8. 应用防御减免
    FP afterDefense = ApplyDefense(baseDamage, targetDefense, effectConfig.DamageType);
    
    // 9. 应用抗性
    FP afterResistance = ApplyResistance(afterDefense, targetDerived, effectConfig.DamageType);
    
    // 10. 应用随机浮动（注意：使用确定性随机）
    FP finalDamage = ApplyDeterministicVariance(afterResistance, randomSeed);
    
    // 11. 确保非负
    finalDamage = FPMath.Max(FP.Zero, finalDamage);
    
    return new DamageResult
    {
        FinalDamage = finalDamage,
        IsCritical = isCritical,
        IsBlocked = isBlocked,
        DamageType = effectConfig.DamageType
    };
}
```

---

## 二、基础伤害计算

### 2.1 基础公式

```csharp
private static FP CalculateBaseDamage(FP attack, SkillEffectTable effectConfig)
{
    // effectValue 通常是百分比（如150表示150%攻击力）
    FP ratio = (FP)effectConfig.EffectValue / (FP)100;
    return attack * ratio;
}
```

### 2.2 示例

```
攻击力 = FP(100)
技能倍率 = FP(1.5)（配置表中 effectValue = 150）
基础伤害 = FP(100) × FP(1.5) = FP(150)
```

### 2.3 技能倍率建议

**普通攻击**：
```
轻击：80-120%攻击力
重击：150-200%攻击力
```

**技能伤害**：
```
小技能：150-250%攻击力
中技能：300-500%攻击力
大招：800-1500%攻击力
```

---

## 三、防御减免公式

### 3.1 物理/魔法伤害

```csharp
private static FP ApplyDefense(FP baseDamage, FP defense, DamageType damageType)
{
    // 真实伤害无视防御
    if (damageType == DamageType.True)
        return baseDamage;
    
    // 减伤公式：减伤百分比 = 防御 / (防御 + 100)
    FP damageReduction = defense / (defense + (FP)100);
    return baseDamage * (FP.One - damageReduction);
}
```

### 3.2 防御收益曲线

| 防御值 | 减伤百分比 |
|-------|-----------|
| 25 | ~20% |
| 50 | ~33% |
| 100 | ~50% |
| 200 | ~67% |
| 400 | ~80% |

### 3.3 示例

```
基础伤害 = FP(150)
防御力 = FP(50)
减伤 = FP(50) / (FP(50) + FP(100)) = FP(0.333) (33.3%)
最终伤害 = FP(150) × (FP.One - FP(0.333)) = FP(100)
```

### 3.4 真实伤害

```
真实伤害 = 基础伤害（无视防御）
```

---

## 四、抗性减免

### 4.1 抗性公式

```csharp
private static FP ApplyResistance(FP damage, DerivedStatsComponent targetDerived, DamageType damageType)
{
    FP resistance = FP.Zero;
    
    switch (damageType)
    {
        case DamageType.Physical:
            resistance = targetDerived.Get(StatType.PHYSICAL_RES);
            break;
        case DamageType.Magical:
            resistance = targetDerived.Get(StatType.MAGICAL_RES);
            break;
        case DamageType.True:
            return damage; // 真实伤害无视抗性
    }
    
    return damage * (FP.One - resistance);
}
```

### 4.2 示例

```
伤害 = FP(100)
物理抗性 = FP(0.2)（20%）
最终伤害 = FP(100) × (FP.One - FP(0.2)) = FP(80)
```

### 4.3 抗性来源

- Buff/Debuff
- 装备
- 被动技能
- 种族特性

---

## 五、暴击计算

### 5.1 暴击判定

```csharp
private static bool CheckCritical(FP critRate, int randomSeed)
{
    // 使用确定性随机（基于种子）
    TSRandom random = new TSRandom(randomSeed);
    FP roll = random.NextFP(); // 返回 [0, 1) 的定点数
    return roll < critRate;
}
```

### 5.2 暴击伤害

```
暴击伤害 = 基础伤害 × 暴击伤害倍率

示例：
基础伤害 = FP(150)
暴击率 = FP(0.25)（25%）
暴击伤害倍率 = FP(2.5)（250%）
随机值 = FP(0.18)（基于种子确定性生成）

→ 0.18 < 0.25 → 暴击成功！
→ 暴击伤害 = FP(150) × FP(2.5) = FP(375)
```

### 5.3 确定性保证

- ✅ 使用 `TSRandom`（TrueSync的确定性随机）
- ✅ 基于统一的种子（如帧号+实体ID）
- ✅ 所有客户端生成相同的随机数序列

```csharp
// 种子生成策略
private static int GenerateSeed(int frameNumber, long casterId, long targetId)
{
    // 确保所有客户端生成相同种子
    return HashCode.Combine(frameNumber, casterId, targetId);
}

// 用法：
int seed = GenerateSeed(currentFrame, caster.UniqueId, target.UniqueId);
bool isCrit = CheckCritical(critRate, seed);
```

---

## 六、命中/闪避判定

### 6.1 命中公式

```csharp
private static bool CheckHit(FP accuracy, FP evasion, int randomSeed)
{
    // 最终命中概率 = 命中率 - 闪避率
    FP hitChance = accuracy - evasion;
    
    // 极限约束
    hitChance = FPMath.Clamp(hitChance, (FP)0.1, FP.One); // [10%, 100%]
    
    TSRandom random = new TSRandom(randomSeed);
    FP roll = random.NextFP();
    return roll < hitChance;
}
```

### 6.2 示例

```
施法者命中率 = FP(0.95)（95%）
目标闪避率 = FP(0.15)（15%）
最终命中率 = FP(0.95) - FP(0.15) = FP(0.80)（80%）
随机值 = FP(0.65)（确定性生成）

→ 0.65 < 0.80 → 命中成功！
```

### 6.3 极限约束

- **最低命中率**：10%（即使目标闪避100%）
- **最高命中率**：100%（无法超过）

---

## 七、格挡判定

### 7.1 格挡公式

```csharp
private static bool CheckBlock(FP blockRate, int randomSeed)
{
    TSRandom random = new TSRandom(randomSeed);
    FP roll = random.NextFP();
    return roll < blockRate;
}
```

### 7.2 示例

```
原伤害 = FP(150)
格挡率 = FP(0.30)（30%）
格挡值 = FP(80)
随机值 = FP(0.22)

→ 0.22 < 0.30 → 格挡成功！
→ 格挡后伤害 = FPMath.Max(FP.Zero, FP(150) - FP(80)) = FP(70)
```

---

## 八、完整伤害计算示例

### 场景：骑士攻击法师

**施法者（骑士 Lv5）**：
```
BaseStats:
  ATK = 80 + 8×4 = 112（基础80，等级加成8/级）
  CRIT_RATE = 0.05
  CRIT_DMG = 2.0
  ACCURACY = 0.95

Buffs:
  [力量祝福] ATK +20% (Percent)
  [狂战士] CRIT_DMG ×1.5 (FinalMultiplier)

DerivedStats:
  FinalStats.Get(StatType.ATK) = 112 × 1.2 = 134.4
  FinalStats.Get(StatType.CRIT_RATE) = 0.05
  FinalStats.Get(StatType.CRIT_DMG) = 2.0 × 1.5 = 3.0
  FinalStats.Get(StatType.ACCURACY) = 0.95
```

**目标（法师 Lv3）**：
```
BaseStats:
  DEF = 40 + 4×2 = 48
  EVASION = 0.10
  BLOCK_RATE = 0.05
  BLOCK_VALUE = 30
  PHYSICAL_RES = 0.0
  MAGICAL_RES = 0.3

DerivedStats:
  FinalStats.Get(StatType.DEF) = 48
  FinalStats.Get(StatType.EVASION) = 0.10
  FinalStats.Get(StatType.BLOCK_RATE) = 0.05
  FinalStats.Get(StatType.BLOCK_VALUE) = 30
```

**技能效果配置**：
```
SkillEffectId: 4001
EffectType: 1（伤害）
EffectValue: 150（150%攻击力）
DamageType: 1（物理）
```

**计算流程**：

```
[1] 基础伤害计算
    = FinalAttack × (EffectValue / 100)
    = 134.4 × 1.5
    = 201.6

[2] 命中判定
    命中概率 = FinalAccuracy - FinalEvasion
             = 0.95 - 0.10 = 0.85（85%）
    随机数 = 0.42 → 命中成功！

[3] 格挡判定
    格挡概率 = FinalBlockRate = 0.05（5%）
    随机数 = 0.82 → 格挡失败

[4] 暴击判定
    暴击概率 = FinalCritRate = 0.05（5%）
    随机数 = 0.03 → 暴击成功！💥
    暴击伤害 = 201.6 × 3.0 = 604.8

[5] 防御减免
    减伤百分比 = Defense / (Defense + 100)
                = 48 / 148 = 0.324（32.4%）
    伤害 = 604.8 × (1 - 0.324) = 408.8

[6] 抗性减免（物理抗性）
    抗性 = FinalPhysicalResistance = 0.0
    伤害 = 408.8 × (1 - 0.0) = 408.8

[7] 随机浮动（±5%）
    随机数 = 0.62 → 浮动 = 0.97
    最终伤害 = 408.8 × 0.97 = 396.5

[8] 结果
    ✅ 命中
    ✅ 暴击
    ❌ 格挡失败
    最终伤害：396.5
```

---

## 九、确定性计算技术

### 9.1 为什么需要确定性

**问题**：帧同步需要确定性，但浮点数不确定
- ✅ float在不同CPU/编译器下可能产生不同结果
- ✅ System.Random不确定
- ✅ UnityEngine.Random不确定
- ✅ DateTime.Now不同步

**解决方案（本系统采用）**：全面使用定点数（FP）

### 9.2 定点数（TrueSync.FP）

```csharp
using TrueSync;

// 所有属性值使用FP
public class Stats
{
    private Dictionary<StatType, FP> _values;
    public FP Get(StatType type) => _values.TryGetValue(type, out var v) ? v : FP.Zero;
}

// 所有计算使用FP
FP baseDamage = casterAttack * ratio;
FP finalDamage = baseDamage * (FP.One - damageReduction);
```

### 9.3 确定性随机数

```csharp
// 使用TrueSync的TSRandom，基于种子生成
int seed = GenerateSeed(frameNumber, casterId, targetId);
TSRandom random = new TSRandom(seed);
FP randomValue = random.NextFP(); // [0, 1)

// 所有客户端使用相同种子，生成相同随机序列
```

### 9.4 种子生成策略

```csharp
private static int GenerateSeed(int frame, long casterId, long targetId)
{
    // 确保所有客户端生成相同种子
    return HashCode.Combine(frame, casterId, targetId);
}

// 用法：
int seed = GenerateSeed(currentFrame, caster.UniqueId, target.UniqueId);
bool isCrit = CheckCritical(critRate, seed);
```

### 9.5 确定性保证清单

| 项目 | 解决方案 |
|------|---------|
| 数值存储 | 使用 `TrueSync.FP`（64位定点数） |
| 数学运算 | 使用 `FPMath` 工具类 |
| 随机判定 | 使用 `TSRandom` + 确定性种子 |
| 时间同步 | 使用逻辑帧号，不使用 `DateTime.Now` |

### 9.6 注意事项

**禁止事项**：
- ❌ 禁止使用 `float`、`double` 存储运行时数值
- ❌ 禁止使用 `System.Random`（不确定）
- ❌ 禁止使用 `UnityEngine.Random`（不确定）
- ❌ 禁止使用 `DateTime.Now`（不同步）

**正确做法**：
- ✅ 运行时数值全部使用 `FP`
- ✅ 随机数使用 `TSRandom`
- ✅ 时间使用逻辑帧号
- ✅ UI显示时转换：`(float)fpValue`

---

## 十、DamageResult 数据结构

```csharp
/// <summary>
/// 伤害结算结果
/// </summary>
public class DamageResult
{
    /// <summary>最终伤害（定点数）</summary>
    public FP FinalDamage { get; set; }
    
    /// <summary>是否暴击</summary>
    public bool IsCritical { get; set; }
    
    /// <summary>是否格挡</summary>
    public bool IsBlocked { get; set; }
    
    /// <summary>是否未命中</summary>
    public bool IsMiss { get; set; }
    
    /// <summary>伤害类型</summary>
    public DamageType DamageType { get; set; }
}

/// <summary>
/// 伤害类型
/// </summary>
public enum DamageType
{
    Physical = 1,  // 物理伤害
    Magical = 2,   // 魔法伤害
    True = 3       // 真实伤害
}
```

---

## 十一、使用示例

### 完整伤害结算流程

```csharp
// 施放技能，触发伤害效果
var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);

// 生成确定性随机种子
int seed = GenerateSeed(currentFrame, attackerEntity.UniqueId, targetEntity.UniqueId);

var damageResult = DamageCalculator.Calculate(
    caster: attackerEntity,
    target: targetEntity,
    effectConfig: effectConfig,
    randomSeed: seed
);

if (damageResult.IsMiss)
{
    // 未命中，显示MISS
    ShowMissText(targetEntity);
    return;
}

// 应用伤害
var targetDynamic = targetEntity.GetComponent<DynamicStatsComponent>();
var targetDerived = targetEntity.GetComponent<DerivedStatsComponent>();

FP actualDamage = targetDynamic.TakeDamage(damageResult.FinalDamage, targetDerived);

// 伤害反馈（转为float用于UI显示）
float displayDamage = (float)actualDamage;

if (damageResult.IsCritical)
{
    ShowCriticalDamageText(displayDamage); // 💥暴击
}
else if (damageResult.IsBlocked)
{
    ShowBlockedDamageText(displayDamage); // 🛡️格挡
}
else
{
    ShowNormalDamageText(displayDamage);
}

// 检查死亡
if (targetDynamic.Get(DynamicResourceType.CURRENT_HP) <= FP.Zero)
{
    var stateComp = targetEntity.GetComponent<StateComponent>();
    stateComp.Set(StateType.DEAD, true);
    OnEntityDied(targetEntity);
}
```

---

**创建日期**: 2025-10-14  
**作者**: Astrum开发团队  
**状态**: 📝 设计完成，待实现

