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

| 枚举 | 说明 | 推荐映射 |
|------|------|----------|
| `TargetSelector` | 目标筛选 | `0=Self`, `1=Enemy`, `2=Ally`, `3=AreaAll` |
| `DamageType` | 伤害类型 | `0=None`, `1=Physical`, `2=Magical`, `3=True` |
| `ScalingStat` | 缩放属性 | `0=None`, `1=ATK`, `2=DEF`, `3=HPMax`, `4=AP` |
| `StatusType` | 状态ID | 对应 `StatusTable` 中的枚举/整型ID |
| `DirectionMode` | 位移/击退方向 | `0=Forward`, `1=Backward`, `2=Outward`, `3=Inward` |

> ⚠️ 枚举值需同步至解析器与代码中定义的 `enum`，避免魔法数字失配。

## 🔧 效果配置

### Damage（伤害）

**IntParams 顺序**
1. `TargetSelector` (int)
2. `BaseCoefficient` (int, *1000，1500=150%)
3. `ScalingStat` (int)
4. `ScalingRatio` (int, *1000)
5. `VisualEffectId` (int)
6. `SoundEffectId` (int)

**StringParams** *(可选)*
- `DamageType:<enum>` → 例：`DamageType:1`
- `CastTime:<float>` → 例：`CastTime:0.5`
- `Variance:<float>` → 随机浮动比例，如 `Variance:0.1`

**示例**
```csv
4001,Damage,1|1500|1|1500|5001|6001,DamageType:1|CastTime:0.0
```

### Heal（治疗）

**IntParams**
1. `TargetSelector`
2. `BaseCoefficient` (*1000)
3. `ScalingStat`
4. `ScalingRatio` (*1000)
5. `VisualEffectId`
6. `SoundEffectId`

**StringParams**
- `HealType:<enum>` (如 0=Instant,1=HoT)
- `DurationMs:<int>` （HoT 时长）

### Knockback（击退）

**IntParams**
1. `TargetSelector`
2. `DistanceMm` (int)
3. `DurationMs` (int)
4. `VisualEffectId`
5. `SoundEffectId`

**StringParams**
- `Direction:<enum>` (`Direction:0` 表示 Forward)
- `Curve:<enum>` (速度曲线)

### Status（状态附加）

**IntParams**
1. `TargetSelector`
2. `DurationMs`
3. `StatusType` (引用状态ID)
4. `MaxStacks`
5. `VisualEffectId`
6. `SoundEffectId`

**StringParams**
- `ApplicationMode:<enum>` (叠加方式)
- `IntervalMs:<int>`（持续伤害/治疗间隔）

### Teleport（瞬移/位移）

**IntParams**
1. `TargetSelector`
2. `OffsetMm`
3. `CastDelayMs`
4. `VisualEffectId`
5. `SoundEffectId`

**StringParams**
- `Direction:<enum>`
- `Phase:<enum>` （触发阶段：BeforeHit / AfterHit）

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

