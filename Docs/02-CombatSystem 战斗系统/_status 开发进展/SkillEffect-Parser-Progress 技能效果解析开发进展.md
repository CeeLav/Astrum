# 技能效果解析层开发进展

> 📊 **当前版本**: v0.1.0 (准备开发)  
> 📅 **最后更新**: 2025-11-07  
> 👤 **负责人**: Combat System Team

## TL;DR（四象限）
- **状态/进度**：完成配置与文档方案，代码实现尚未开始
- **已达成**：统一 SkillEffectTable 四列结构；完成解析器设计文档 & 配置指南
- **风险/阻塞**：运行时代码仍依赖旧列；枚举常量需与配置同步；编辑器工具待更新
- **下一步**：实现解析器注册表与新版数据模型，落地 Damage/Knockback 解析

## 版本历史

### v0.1.0 - Parser 初始化方案 (2025-11-07)
**状态**: 📝 策划案完成

**完成内容**
- [x] 技能效果配置重构为 `skillEffectId|effectType|intParams|stringParams`
- [x] 技术设计文档：`SkillEffect-Parser-Design 技能效果解析设计.md`
- [x] 配置使用指南：`SkillEffect-Config-Guide 技能效果配置说明.md`
- [x] 重写 `#SkillEffectTable.csv` 数据并清空 `stringParams`
- [x] 明确枚举映射与参数顺序

**待完成**
- [ ] Luban Schema & 生成代码更新
- [ ] 运行时解析器接口与实现
- [ ] 编辑器读取逻辑 (`SkillEffectDataReader` 等)
- [ ] 单元测试覆盖

**预计工时**: 5 天 (不含测试)

---

## 当前阶段

**阶段名称**: Phase 0 - 解析框架落地

**完成度**: 40%

**下一步计划**
1. 更新 Luban `cfg.Skill.SkillEffectTable` 模型（含数组字段）
2. 实现 `SkillEffectRawData` & `SkillEffectParserRegistry`
3. 落地 `DamageEffectParser` 与 `KnockbackEffectParser`
4. 调整 `SkillEffectManager` / `IEffectHandler` 适配解析结果
5. 编写配置加载与解析单元测试

---

## 依赖状态
- ✅ 配置表与文档已完成重构
- ⚠️ Luban 模型：需更新 `Tables/Datas/Skill/SkillEffectTable.luban` & 生成代码
- ⚠️ 运行时代码：仍使用旧字段 (`EffectValue`, `DamageType` 等)
- ⚠️ 编辑器：`SkillEffectDataReader`、选择窗口需要重写解析逻辑

---

## 重点任务

| 序号 | 任务 | Owner | 状态 | 备注 |
|------|------|-------|------|------|
| T1 | 更新 Luban schema 与自动生成代码 | Dev | 待开始 | 调整 `SkillEffectTableData` 映射 |
| T2 | 定义 `SkillEffectRawData` 与解析器接口 | Dev | 待开始 | 支撑运行时与编辑器共享 |
| T3 | 实现 `DamageEffectParser`、`KnockbackEffectParser` | Dev | 待开始 | 校验参数长度 + 缓存 |
| T4 | 修改 `SkillEffectManager` 流程接入解析结果 | Dev | 待开始 | 替代旧字段访问 |
| T5 | 调整编辑器 (`SkillEffectDataReader` / UI) | Dev | 待开始 | 确保显示新参数结构 |
| T6 | 单元测试 & 配置样例校验 | QA | 待开始 | 覆盖解析器与运行时流程 |

---

## 风险与阻塞
- **解析器与枚举同步风险**：需要统一定义 `DamageType` 等枚举值，防止运行时代码与 CSV 不一致。
- **旧逻辑兼容成本**：`DamageCalculator`、`KnockbackEffectHandler` 等仍依赖旧字段，需集中改造。
- **编辑器体验退化**：未更新读取逻辑前，角色编辑器可能显示空数据。

---

## 相关链接
- [SkillEffect-Parser-Design 技能效果解析设计](../技能效果/SkillEffect-Parser-Design 技能效果解析设计.md)
- [SkillEffect-Config-Guide 技能效果配置说明](../技能效果/SkillEffect-Config-Guide 技能效果配置说明.md)
- `AstrumConfig/Tables/Datas/Skill/#SkillEffectTable.csv`

---

*文档版本：v0.1.0*  
*创建时间：2025-11-07*  
*最后更新：2025-11-07*  
*状态：准备开发*  
*Owner*: Combat System Team  
*变更摘要*: 建立技能效果解析层开发进展跟踪，整理 Phase 0 任务清单

