# SkillEffect Parser Design 技能效果解析设计

> 📖 **关联需求**: 战斗系统技能效果数据解耦（2025-11-07）  
> 🔗 **上游文档**: [Skill-Effect-Runtime 技能效果运行时](../技能系统/Skill-Effect-Runtime 技能效果运行时.md)  
> 🧩 **关联配置**: `AstrumConfig/Tables/Datas/Skill/#SkillEffectTable.csv`

## 概述

- 现有 `SkillEffectTable` 使用定制列(`effectValue`,`damageType`,`scalingStat`...)，处理器需了解表结构才能解析参数。
- 新方案引入 `EffectType` + `IntParams` + `StringParams` 通用参数槽，每种效果类型通过独立解析器解释自身参数。
- 解析器产出的领域模型由 `SkillEffectManager` 分发给现有 `IEffectHandler`，避免运行时逻辑与 CSV 字段耦合。
- Luban 仍负责生成 `cfg.Skill.SkillEffectTable`，新增列通过数组语法声明，兼容现有工具链与编辑器。

## 架构设计

```
Luban CSV → SkillEffectTable(bytes)
        ↓ (Luban 生成)
cfg.Skill.SkillEffectTable  ←─ SkillEffectConfigLoader
        ↓ (抽象)
SkillEffectRawData
        ↓ (EffectType 查表)
IEffectParser.Parse()
        ↓
ISkillEffect (领域模型)
        ↓
SkillEffectManager → IEffectHandler
```

**职责分层**
- **SkillEffectConfigLoader**: 读取 `cfg.Skill.SkillEffectTable`，封装为 `SkillEffectRawData`（含 EffectType、IntParams、StringParams）。
- **IEffectParser**: 针对单一 `EffectType` 的解析器，负责将原始参数转换为运行时模型。
- **SkillEffectParserRegistry**: 维护 `{EffectType → IEffectParser}` 映射，支持编辑器与运行时共享。
- **ISkillEffect**: 解析后得到的不可变数据载体，供 `SkillEffectManager` 分发。
- **SkillEffectManager**: 按 EffectType 查找解析器，缓存结果，并将 `ISkillEffect` 注入既有 `IEffectHandler` 管线。

## 配置表结构

| 列名 | 类型声明 (`##type`) | 说明 |
|------|-------------------|------|
| `skillEffectId` | `int` | 主键，保持不变 |
| `effectType` | `string` | 语义化类型键，例如 `Damage`,`Heal`,`Knockback` |
| `targetType` | `int` | 目标筛选/派发仍保留现有数值 |
| `effectDuration` | `float` | 保留时长字段，供持续性效果使用 |
| `effectRange` | `float` | 保留范围字段（米） |
| `castTime` | `float` | 施法前摇/延迟 |
| `intParams` | `(array#sep=|,int)` | 任意整数参数按顺序解读；示例：`1500|1|100` |
| `stringParams` | `(array#sep=|,string)` | 补充字符串/路径/公式；示例：`Fire|DamageType:Physical` |
| `visualEffectId` | `int` | 保留视觉表现引用 |
| `soundEffectId` | `int` | 保留音效引用 |

> ⚠️ `effectValue`,`damageType`,`scalingStat`,`scalingRatio`,`effectParams` 等旧列将在 CSV 中迁移至 `intParams`/`stringParams`，同时更新 `##desc` 描述确保表意准确。

### 参数序列化规范
- 数组分隔符使用竖线 `|`，避免与 CSV 逗号冲突，兼容 Luban `array#sep` 语法。
- 空数组写作空字符串（`intParams` 或 `stringParams` 单元留空）。
- `stringParams` 建议使用 `key:value` 格式供解析器识别可选参数；解析器需处理缺省键。

## 解析器接口

```csharp
public interface IEffectParser
{
    string EffectType { get; }
    ISkillEffect Parse(SkillEffectRawData data);
}

public sealed class DamageEffectParser : IEffectParser
{
    public string EffectType => "Damage";

    public ISkillEffect Parse(SkillEffectRawData data)
    {
        int baseDamage = data.IntParams[0];
        int damageType = data.IntParams.Length > 1 ? data.IntParams[1] : 0;
        float ratio = data.IntParams.Length > 2 ? data.IntParams[2] / 100f : 1f;
        string element = data.StringParams.Length > 0 ? data.StringParams[0] : "None";

        return new DamageEffect(baseDamage, damageType, ratio, element);
    }
}
```

**关键约束**
- `SkillEffectParserRegistry` 在启动时注册解析器，若缺少对应 `EffectType`，需记录错误并阻止技能触发。
- 解析器负责校验必填参数（例如 `Damage` 至少需要一个 `baseDamage`）。
- 编辑器工具 (`SkillEffectDataReader`) 复用同一解析流程，确保可视化面板与运行时一致。

## 运行时流程调整

1. `SkillEffectManager` 查询 `cfg.Skill.SkillEffectTable` → 获取 `SkillEffectRawData`。
2. 按 `effectType` 调用注册解析器并缓存 `ISkillEffect` 结果。
3. 将 `ISkillEffect` 与施法上下文打包成 `SkillEffectData`（保留 `CasterId`,`TargetId`）。
4. 根据 `effectType` 映射到现有 `IEffectHandler`：
   - `Damage` → `DamageEffectHandler`
   - `Knockback` → `KnockbackEffectHandler`
   - `Buff` → `BuffEffectHandler`
5. 处理器只关注领域模型，不再直接访问 CSV 字段。

## 关键决策与取舍
- **问题**: 旧版配置列固定化，新增效果需改动多张表/代码。
- **备选**: 继续扩展列；或以 JSON 存储参数。
- **选择**: 引入解析器 + 多参数槽结构，既保持结构化 CSV，又允许效果自定义解析逻辑。
- **影响**: Luban 表结构需迁移；运行时新增解析层；编辑器需更新以展示参数数组。

## 相关文档
- [Skill-Effect-Runtime 技能效果运行时](../技能系统/Skill-Effect-Runtime 技能效果运行时.md)
- [Damage-Calculation 伤害计算](../数值系统/Damage-Calculation 伤害计算.md)
- `AstrumConfig/Tables/Datas/Skill/#SkillEffectTable.csv`

---

*文档版本：v0.1*  
*创建时间：2025-11-07*  
*最后更新：2025-11-07*  
*状态：实现中*  
*Owner*: Combat System Team  
*变更摘要*: 定义技能效果解析器化方案与配置迁移规范

