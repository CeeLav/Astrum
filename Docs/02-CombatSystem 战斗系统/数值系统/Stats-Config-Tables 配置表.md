# 配置表设计

> 📋 数值系统的配置表设计与扩展：RoleBaseTable, RoleGrowthTable, BuffTable
>
> **版本**: v1.0  
> **更新**: 2025-10-14

---

## 一、配置表数值规则

### 1.1 核心原则

**配置表全部使用 `int` 类型，避免float的不确定性**

### 1.2 配置规则总表

| 属性类型 | 配置示例 | 运行时值 | 转换规则 | 说明 |
|---------|---------|---------|---------|------|
| 攻击/防御/生命 | `80` | `FP(80)` | 直接转换 | 整数属性 |
| 速度 | `10500` | `FP(10.5)` | 除以1000 | 小数属性 |
| 暴击率5% | `50` | `FP(0.05)` | 除以1000 | 百分比属性 |
| 暴击伤害200% | `2000` | `FP(2.0)` | 除以1000 | 百分比倍率 |
| 命中率95% | `950` | `FP(0.95)` | 除以1000 | 百分比属性 |
| 抗性10% | `100` | `FP(0.1)` | 除以1000 | 百分比属性 |
| 回复速度5.0/秒 | `5000` | `FP(5.0)` | 除以1000 | 小数属性 |

### 1.3 记忆法则

- ✅ **整数属性**：直接配置（攻击、防御、生命、法力上限等）
- ✅ **小数属性**：**扩大1000倍**存储（速度、回复速度等）
- ✅ **百分比属性**：**扩大1000倍**（暴击率、抗性等，5%存为50）
- ✅ **运行时读取**：需要除以1000的属性，代码中判断

### 1.4 代码转换示例

```csharp
// 配置表字段类型
public class RoleBaseTable
{
    public int BaseAttack;     // 整数属性
    public int BaseSpeed;      // 小数属性*1000
    public int BaseCritRate;   // 百分比*1000
}

// 运行时转换
BaseStats.Set(StatType.ATK, (FP)roleConfig.BaseAttack);                    // 直接转换
BaseStats.Set(StatType.SPD, (FP)roleConfig.BaseSpeed / (FP)1000);         // 除以1000
BaseStats.Set(StatType.CRIT_RATE, (FP)roleConfig.BaseCritRate / (FP)1000); // 除以1000
```

### 1.5 优势

- ✅ 配置表全是int，易于编辑和校验
- ✅ 避免float精度问题
- ✅ CSV文件更清晰（50 vs 0.05）
- ✅ 运行时一次性转换为FP
- ✅ 支持帧同步确定性

---

## 二、RoleBaseTable（角色基础表）

### 2.1 现有字段评估

**当前字段**：
```csv
id, name, description, roleType,
baseAttack, baseDefense, baseHealth, baseSpeed,
attackGrowth, defenseGrowth, healthGrowth, speedGrowth,
lightAttackSkillId, heavyAttackSkillId, skill1Id, skill2Id
```

**评估**：
- ✅ 基础四维足够（攻防血速）
- ✅ 成长值已定义
- ❌ **缺少高级属性**（暴击率、暴击伤害、命中、闪避等）

### 2.2 扩展字段设计

**新增字段**（高级属性）：

```csv
##var,id,name,...,baseAttack,baseDefense,baseHealth,baseSpeed,baseCritRate,baseCritDamage,baseAccuracy,baseEvasion,baseBlockRate,baseBlockValue,physicalRes,magicalRes,baseMaxMana,manaRegen,healthRegen
##type,int,string,...,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int
##desc,角色ID,角色名称,...,基础攻击力,基础防御力,基础生命值,基础移动速度*1000,基础暴击率*1000,基础暴击伤害*1000,基础命中率*1000,基础闪避率*1000,基础格挡率*1000,基础格挡值,物理抗性*1000,魔法抗性*1000,基础法力上限,法力回复*1000,生命回复*1000
```

### 2.3 示例数据

#### 骑士（平衡型）
```csv
1001,骑士,...,80,80,1000,10000,50,2000,950,50,150,60,100,0,100,5000,2000
```
**解释**：
- 攻击80、防御80、生命1000（整数）
- 速度10.0（10000/1000）
- 暴击率5%（50/1000=0.05）
- 暴击伤害200%（2000/1000=2.0）
- 命中率95%（950/1000=0.95）
- 闪避率5%（50/1000=0.05）
- 格挡率15%（150/1000=0.15）
- 格挡值60（整数）
- 物理抗性10%（100/1000=0.1）
- 魔法抗性0%（0/1000=0）
- 法力上限100（整数）
- 法力回复5.0/秒（5000/1000）
- 生命回复2.0/秒（2000/1000）

#### 刺客（高暴击低防御）
```csv
1005,刺客,...,70,50,600,7000,250,2500,980,200,50,30,0,0,80,4000,1000
```
**解释**：
- 暴击率25%（250/1000）
- 暴击伤害250%（2500/1000）
- 闪避率20%（200/1000）
- 速度7.0（7000/1000）

#### 法师（高魔抗低物抗）
```csv
1003,法师,...,50,40,700,4500,100,2200,900,80,80,40,0,300,200,10000,1500
```
**解释**：
- 魔法抗性30%（300/1000）
- 速度4.5（4500/1000）
- 法力上限200

### 2.4 属性字段详解

| 字段名 | 类型 | 配置示例 | 运行时值 | 说明 |
|-------|------|---------|---------|------|
| baseAttack | int | 80 | FP(80) | 基础攻击力（整数） |
| baseDefense | int | 80 | FP(80) | 基础防御力（整数） |
| baseHealth | int | 1000 | FP(1000) | 基础生命值（整数） |
| baseSpeed | int | 10000 | FP(10.0) | 基础速度*1000 |
| baseCritRate | int | 50 | FP(0.05) | 暴击率*1000（5%） |
| baseCritDamage | int | 2000 | FP(2.0) | 暴击伤害*1000（200%） |
| baseAccuracy | int | 950 | FP(0.95) | 命中率*1000（95%） |
| baseEvasion | int | 50 | FP(0.05) | 闪避率*1000（5%） |
| baseBlockRate | int | 150 | FP(0.15) | 格挡率*1000（15%） |
| baseBlockValue | int | 60 | FP(60) | 格挡值（整数） |
| physicalRes | int | 100 | FP(0.1) | 物理抗性*1000（10%） |
| magicalRes | int | 300 | FP(0.3) | 魔法抗性*1000（30%） |
| baseMaxMana | int | 100 | FP(100) | 法力上限（整数） |
| manaRegen | int | 5000 | FP(5.0) | 法力回复*1000（5.0/秒） |
| healthRegen | int | 2000 | FP(2.0) | 生命回复*1000（2.0/秒） |

---

## 三、RoleGrowthTable（角色成长表）

### 3.1 现有字段评估

**当前字段**：
```csv
id, roleId, level, requiredExp,
lightAttackBonus, heavyAttackBonus, defenseBonus, healthBonus, speedBonus,
unlockSkillId, skillPoint
```

**评估**：
- ✅ 基础成长已定义
- ✅ 经验需求已定义
- ❌ **lightAttackBonus/heavyAttackBonus 语义不清**，应该统一为攻击力加成

### 3.2 优化字段设计

```csv
##var,id,roleId,level,requiredExp,attackBonus,defenseBonus,healthBonus,speedBonus,critRateBonus,critDamageBonus,unlockSkillId,skillPoint
##type,int,int,int,int,int,int,int,int,int,int,int,int
##desc,ID,角色ID,等级,升级所需经验,攻击力加成,防御力加成,生命值加成,速度加成*1000,暴击率加成*1000,暴击伤害加成*1000,解锁技能ID,技能点
```

### 3.3 示例数据

#### 骑士 Lv2
```csv
2,1001,2,1000,8,8,100,100,2,50,0,1
```
**解释**：
- 攻击 +8
- 防御 +8
- 生命 +100
- 速度 +0.1（100/1000）
- 暴击率 +0.2%（2/1000）
- 暴击伤害 +5%（50/1000=0.05）

#### 刺客 Lv2（高速度和暴击成长）
```csv
12,1005,2,1000,7,5,60,200,5,100,0,1
```
**解释**：
- 攻击 +7
- 速度 +0.2（200/1000）
- 暴击率 +0.5%（5/1000）
- 暴击伤害 +10%（100/1000）

### 3.4 成长曲线建议

**攻击力成长**：
```
战士型：每级 +8-10
敏捷型：每级 +6-8
法师型：每级 +5-7
```

**生命值成长**：
```
战士型：每级 +100-120
敏捷型：每级 +60-80
法师型：每级 +70-90
```

---

## 四、BuffTable（Buff配置表）

### 4.1 表结构设计

**用途**：定义所有Buff/Debuff的效果和数值

```csv
##var,buffId,buffName,buffType,duration,stackable,maxStack,modifiers,tickDamage,tickInterval
##type,int,string,int,int,bool,int,string,int,int
##desc,BuffID,Buff名称,类型(1=Buff/2=Debuff),持续帧数,可叠加,最大层数,属性修饰器,持续伤害*1000,触发间隔帧数
```

### 4.2 字段说明

| 字段名 | 类型 | 说明 | 示例 |
|-------|------|------|------|
| buffId | int | Buff唯一ID | 5001 |
| buffName | string | Buff名称 | 力量祝福 |
| buffType | int | 类型（1=增益Buff, 2=减益Debuff） | 1 |
| duration | int | 持续帧数（300帧 = 15秒 @ 20FPS） | 600 |
| stackable | bool | 是否可叠加 | true |
| maxStack | int | 最大叠加层数 | 3 |
| modifiers | string | 属性修饰器字符串（格式见下） | ATK:Percent:200;SPD:Flat:1000 |
| tickDamage | int | 持续伤害*1000（如10.0伤害存为10000） | 10000 |
| tickInterval | int | 伤害间隔帧数 | 30 |

### 4.3 修饰器字符串格式

**格式**：
```
属性:类型:数值;属性:类型:数值;...
注意：数值全部为int
```

**示例**：
```
"ATK:Percent:200;SPD:Flat:1000;CRIT_RATE:Flat:50"

解析为：
  - ATK +20% (Percent, 200/1000=0.2)
  - SPD +1.0 (Flat, 1000/1000=1.0)
  - CRIT_RATE +5% (Flat, 50/1000=0.05)
```

**规则**：
- **Percent类型**：总是除以1000
- **Flat类型**：根据属性类型判断
  - 整数属性（ATK/DEF/HP等）：直接使用
  - 小数属性（SPD/CRIT_RATE等）：除以1000

### 4.4 示例数据

#### 增益Buff

```csv
# 力量祝福
5001,力量祝福,1,600,true,3,ATK:Percent:200;SPD:Flat:1000,0,0

# 极速
5002,极速,1,300,false,1,SPD:Percent:500,0,0

# 护盾（特殊处理）
5003,护盾,1,180,false,1,无,0,0

# 狂暴
5004,狂暴,1,600,true,5,ATK:Percent:100;CRIT_RATE:Flat:50,0,0
```

**解释**：
- `5001` - 力量祝福：ATK +20%（200/1000=0.2），SPD +1.0（1000/1000），可叠加3层
- `5002` - 极速：SPD +50%（500/1000=0.5），不可叠加
- `5004` - 狂暴：ATK +10%（100/1000=0.1），CRIT_RATE +5%（50/1000=0.05），可叠加5层

#### 减益Debuff

```csv
# 燃烧
6001,燃烧,2,300,false,1,PHYSICAL_RES:Percent:-100,10000,30

# 冰冻
6002,冰冻,2,120,false,1,SPD:Percent:-800,0,0

# 虚弱
6003,虚弱,2,600,true,3,ATK:Percent:-150,0,0

# 破甲
6004,破甲,2,300,false,1,DEF:Percent:-300,0,0
```

**解释**：
- `6001` - 燃烧：物理抗性-10%（-100/1000=-0.1），每30帧造成10.0伤害（10000/1000=10.0）
- `6002` - 冰冻：速度-80%（-800/1000=-0.8）
- `6003` - 虚弱：攻击-15%（-150/1000=-0.15），可叠加3层
- `6004` - 破甲：防御-30%（-300/1000=-0.3）

---

## 五、SkillEffectTable（技能效果表）

### 5.1 新字段结构

```csv
##var,skillEffectId,effectType,intParams,stringParams
##type,int,string,"(array#sep=|,int)","(array#sep=|,string)"
```

| 字段 | 说明 |
|------|------|
| `skillEffectId` | 效果主键 |
| `effectType` | 语义型字符串（如 `Damage`,`Knockback`,`Status`）|
| `intParams` | 整数参数数组，所有数值/资源ID按解析器约定顺序编码 |
| `stringParams` | 字符串参数数组，惯用 `key:value` 形式承载语义数据 |

### 5.2 效果类型参数约定

| `effectType` | `intParams` 约定 | `stringParams` 约定 |
|--------------|------------------|----------------------|
| `Damage` | `targetType|baseCoefficient|scalingStat|scalingRatio|visualEffectId|soundEffectId` | `DamageType:Physical`、`Element:Fire`、`CastTime:0.5` |
| `Heal` | `targetType|baseCoefficient|scalingStat|scalingRatio|visualEffectId|soundEffectId` | `HealType:Direct`、`CastTime:0.3` |
| `Knockback` | `targetType|distanceMm|durationMs|visualEffectId|soundEffectId` | `Direction:Forward`、`Curve:EaseOut` |
| `Status` | `targetType|durationMs|maxStacks|visualEffectId|soundEffectId` | `Status:Freeze`、`Radius:6.0` |
| `Teleport` | `targetType|offsetMm|castDelayMs|visualEffectId|soundEffectId` | `Direction:Forward`、`Phase:AfterHit` |

> 解析器需在加载时验证参数长度，缺失必填项直接报错，避免运行时不确定性。

### 5.3 示例数据

```csv
# 物理伤害技能（150%攻击力，附加1.5倍暴击放大）
4001,Damage,1|1500|1|1500|5001|6001,DamageType:Physical|Element:Physical

# 火焰击退（距离5米，方向朝外）
4016,Knockback,1|5000|300|5016|6016,Direction:Outward|Element:Fire

# 冻结状态（持续3秒）
4012,Status,1|3000|3|5012|6012,Status:Freeze|Immunity:true
```

---

## 六、数值平衡参考

### 6.1 基础数值建议

#### 战士型（骑士、重锤者）
```
攻击：80-100
防御：80-120
生命：1000-1200
速度：3.5-10
暴击率：5-10%
```

#### 敏捷型（刺客、弓手）
```
攻击：60-80
防御：50-60
生命：600-800
速度：6-10
暴击率：20-30%
```

#### 法师型（法师）
```
攻击：50-60
防御：40-50
生命：700-800
速度：4.5-6
暴击率：10-15%
魔法抗性：20-30%
```

### 6.2 Buff加成建议

```
普通Buff：+10-20%属性
强力Buff：+30-50%属性
终极Buff：+100%属性（短时间）
```

### 6.3 技能伤害倍率建议

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

## 七、配置表实现清单

### Phase 1：扩展现有配置表

1. **扩展 RoleBaseTable**
   - 添加高级属性字段（暴击率、暴击伤害、命中、闪避、格挡、抗性等）
   - 更新现有角色数据（骑士、法师、重锤者等）
   - 确保所有数值字段都是int类型

2. **优化 RoleGrowthTable**
   - 统一成长字段命名（attackBonus代替lightAttackBonus/heavyAttackBonus）
   - 添加暴击成长字段（critRateBonus、critDamageBonus）
   - 更新现有成长数据

### Phase 2：创建新配置表

3. **创建 BuffTable**
   - 定义Buff配置结构
   - 添加示例Buff数据（力量祝福、极速、燃烧、冰冻等）
   - 测试修饰器字符串解析

4. **优化 SkillEffectTable**
   - 切换至 `intParams` / `stringParams` 通用参数结构
   - 更新现有技能效果数据并补齐解析器所需参数
   - 补充测试覆盖，确保无效参数能在加载阶段被拒绝

### Phase 3：数据填充

5. **填充角色基础数据**
   - 为现有5个角色填充完整属性
   - 确保职业定位明确（战士/刺客/法师等）
   - 平衡测试

6. **填充成长数据**
   - 为每个角色填充1-100级成长数据
   - 设置合理的经验需求曲线
   - 技能解锁等级设置

7. **填充Buff数据**
   - 创建10-20个常用Buff/Debuff
   - 确保修饰器格式正确
   - 测试叠加逻辑

---

## 八、配置表使用示例

### 读取RoleBaseTable

```csharp
var roleConfig = ConfigManager.Instance.Tables.TbRoleBaseTable.Get(1001);

// 整数属性：直接转换
BaseStats.Set(StatType.ATK, (FP)roleConfig.BaseAttack);
BaseStats.Set(StatType.DEF, (FP)roleConfig.BaseDefense);
BaseStats.Set(StatType.HP, (FP)roleConfig.BaseHealth);

// 小数属性：除以1000
BaseStats.Set(StatType.SPD, (FP)roleConfig.BaseSpeed / (FP)1000);

// 百分比属性：除以1000
BaseStats.Set(StatType.CRIT_RATE, (FP)roleConfig.BaseCritRate / (FP)1000);
BaseStats.Set(StatType.CRIT_DMG, (FP)roleConfig.BaseCritDamage / (FP)1000);
```

### 读取BuffTable

```csharp
var buffConfig = ConfigManager.Instance.Tables.TbBuffTable.Get(5001);

// 解析修饰器字符串："ATK:Percent:200;SPD:Flat:1000"
var modifiers = ParseBuffModifiers(buffConfig.Modifiers);

foreach (var mod in modifiers)
{
    // ATK: Percent, 200/1000=0.2
    // SPD: Flat, 1000/1000=1.0
    derivedStats.AddModifier(mod.StatType, mod.Modifier);
}
```

---

**创建日期**: 2025-10-14  
**作者**: Astrum开发团队  
**状态**: 📝 设计完成，待实现

