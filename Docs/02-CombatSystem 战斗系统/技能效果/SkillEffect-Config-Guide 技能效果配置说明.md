# SkillEffect 配置说明

## 📋 概述

技能效果配置统一存放在 `AstrumConfig/Tables/Datas/Skill/#SkillEffectTable.csv`，表结构已简化为四列：

```csv
##var,skillEffectId,effectType,intParams,stringParams
##type,int,string,"(array#sep=|,int)","(array#sep=|,string)"
```

**配置原则**
- 以枚举/整型驱动逻辑，尽量避免字符串比较。
- `intParams` 负责所有数值和枚举索引，推荐按固定顺序编码。
- `stringParams` 仅在必须表达文本或公式时使用，格式为 `key:value`。

## 🏗️ 架构

### 核心设计理念
- **解耦**：每种效果类型由独立解析器解释 `intParams` / `stringParams`。
- **可拓展**：新增效果类型只需定义解析器与参数约定，无需改动表结构。
- **一致性**：编辑器、运行时共享同一解析规则，确保行为一致。

### 配置分类
```
SkillEffectTable
 ├─ Damage
 ├─ Heal
 ├─ Knockback
 ├─ Status
 ├─ Teleport
 └─ CustomEffect (预留)
```

## 📊 SkillEffectTable 详细说明

### 公共字段

| 列名 | 类型 | 说明 |
|------|------|------|
| `skillEffectId` | `int` | 效果唯一ID，与技能动作触发帧关联 |
| `effectType` | `string` | 解析器键：`Damage`,`Heal`,`Knockback`,`Status`,`Teleport` 等 |
| `intParams` | `int[]` | 竖线 `|` 分隔的整型序列，详见各效果约定 |
| `stringParams` | `string[]` | 竖线分隔的字符串序列，`key:value` 形式，可为空 |

### 枚举约定

| 枚举 | 中文说明 | 推荐映射 |
|------|----------|----------|
| `TargetSelector` | 目标筛选方式 | `0=自身`, `1=敌人`, `2=友军`, `3=区域内全部` |
| `DamageType` | 伤害/元素类型 | 建议统一映射：`0=无`, `1=物理`, `2=魔法`, `3=火`, `4=冰`, `5=雷`, `6=毒`, `7=真实` |
| `ScalingStat` | 缩放属性 | `0=无`, `1=攻击`, `2=防御`, `3=生命上限`, `4=法强` |
| `HealMode` | 治疗方式 | `0=瞬发`, `1=持续` |
| `StatusType` | 状态枚举ID | 对应 `StatusTable` 中定义的状态 |
| `DirectionMode` | 位移/击退方向 | `0=向前`, `1=向后`, `2=远离施法者`, `3=靠近施法者`, `4=瞬移点` |
| `CurveType` | 速度曲线 | `0=线性`, `1=减速`, `2=加速`, `3=自定义` |
| `StatusApplicationMode` | 状态叠加模式 | `0=刷新时长`, `1=叠加层数` |

> ⚠️ 枚举值需同步至解析器与代码中定义的 `enum`，避免魔法数字失配。

## 🔧 效果配置

### Damage（伤害）

**IntParams 顺序**
1. `TargetSelector`（目标筛选枚举）
2. `DamageType`（伤害/元素枚举）
3. `BaseCoefficient`（基础倍率×1000，例如1500=150%）
4. `ScalingStat`（属性缩放枚举）
5. `ScalingRatio`（属性缩放倍率×1000）
6. `VisualEffectId`（视觉特效ID）
7. `SoundEffectId`（音效ID）

**StringParams** *(可选)*
- 默认留空；如需文本标签可写 `Note:BossOnly`
- 若需配置公式，可写 `Formula:ATK*1.5+50`

**示例**
```csv
4001,Damage,1|1|0|1500|1|1500|0|2000|5001|6001,
```

### Heal（治疗）

**IntParams**
1. `TargetSelector`（目标筛选）
2. `HealMode`（治疗方式枚举）
3. `BaseCoefficient`（基础治疗量×1000）
4. `ScalingStat`（属性缩放枚举）
5. `ScalingRatio`（缩放倍率×1000）
6. `VisualEffectId`（视觉特效）
7. `SoundEffectId`（音效）

**StringParams**
- 默认留空；可用于备注：`Note:TriggerOnCritical`

### Knockback（击退）

**IntParams**
1. `TargetSelector`（目标筛选）
2. `DistanceMm`（击退距离，毫米）
3. `DurationMs`（击退持续时长，毫秒）
4. `DirectionMode`（方向枚举）
5. `CurveType`（速度曲线枚举）
6. `VisualEffectId`（视觉特效）
7. `SoundEffectId`（音效）

**StringParams**
- 默认留空；可用于备注或效果标签

### Status（状态附加）

**IntParams**
1. `TargetSelector`（目标筛选）
2. `StatusType`（状态类型ID）
3. `DurationMs`（持续时间，毫秒）
4. `MaxStacks`（最大叠加层数）
5. `ApplicationMode`（叠加模式枚举，0=刷新时长，1=叠加层）
6. `VisualEffectId`（视觉特效）
7. `SoundEffectId`（音效）

**StringParams**
- 默认留空；若有备注可写 `Note:BossOnly`

### Teleport（瞬移/位移）

**IntParams**
1. `TargetSelector`（目标筛选）
2. `OffsetMm`（位移距离，毫米，可正负）
3. `DirectionMode`（方向枚举）
4. `VisualEffectId`（视觉特效）
5. `SoundEffectId`（音效）

**StringParams**
- 默认留空；若需特殊阶段可使用 `Phase:AfterHit`（极少数情况）

### 自定义效果（示例）

> 新增类型需创建解析器并记录参数顺序。建议：
- 在 `intParams` 头两位写入目标与核心数值。
- 保持 5~6 个槽位，便于统一解析模板。

## 注意事项

⚠️ **保持枚举同步**：枚举值改动需同步更新解析器、枚举定义、配置表。

⚠️ **参数校验**：解析器应验证 `intParams` 长度，缺失参数直接报错，防止运行时崩溃。

⚠️ **浮点精度**：所有倍率以 *1000 存储，避免 CSV 浮点误差。

⚠️ **字符串使用最少化**：只有无法离散化为枚举的配置才写入 `stringParams`。

---

*文档版本：v0.1*  
*创建时间：2025-11-07*  
*最后更新：2025-11-07*  
*状态：草稿*  
*Owner*: Combat System Team  
*变更摘要*: 定义技能效果配置参数格式与枚举约定

