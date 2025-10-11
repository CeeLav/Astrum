# 技能编辑器 (Skill Editor)

> 📖 **版本**: v1.0.0 (阶段2A - 核心功能)  
> 🔧 **依赖**: Odin Inspector, CsvHelper  
> ✅ **状态**: 可用（基础编辑功能完成）

---

## 🚀 快速开始

### 打开编辑器
```
Unity菜单 > Tools > Role & Skill Editor > Skill Editor
```

### 基本操作
1. **选择技能** - 左侧列表点击选择
2. **编辑属性** - 右侧面板修改（Odin自动UI）
3. **保存修改** - 顶部"保存"按钮
4. **验证数据** - 顶部"验证"按钮

---

## ✨ 核心功能

### 已实现功能 ✅

**技能管理**：
- ✅ 新建技能（自动生成ID，从2000开始）
- ✅ 复制技能（克隆所有数据）
- ✅ 删除技能（带确认提示）
- ✅ 搜索技能（按ID、名称、描述）
- ✅ 类型筛选（全部/攻击/控制/位移/Buff）

**技能编辑**：
- ✅ 技能基本信息（ID、名称、描述、类型）
- ✅ 等级配置（学习等级、最大等级）
- ✅ 冷却与消耗（展示用数据）
- ✅ 图标ID配置

**技能动作（简化视图）**：
- ✅ 显示动作列表
- ✅ 显示动作名称（从ActionTable动态查询）
- ✅ 编辑基础参数（Cost/Cooldown）
- ✅ 显示触发帧数量
- ⚠️ **触发帧详细编辑** → 预留给阶段2B（独立编辑器）

**数据验证**：
- ✅ 技能ID范围验证（2000-9999）
- ✅ 必填字段验证
- ✅ **ActionTable对应关系验证**（重要）
- ✅ 触发帧格式验证
- ✅ 未保存提醒

---

## 📋 界面布局

```
┌──────────────────────────────────────────────────────────┐
│  [保存] [刷新] [验证]                     状态栏          │
├─────────────┬────────────────────────────────────────────┤
│             │                                            │
│  技能列表     │         技能详情面板（Odin自动UI）          │
│  (250px)    │                                            │
│             │  ┌─────────────────────────────────────┐  │
│  搜索框      │  │ 技能基本信息                        │  │
│  -------   │  │  - 技能ID (只读)                   │  │
│  类型: [全部]│  │  - 技能名称                        │  │
│             │  │  - 技能描述                        │  │
│  [新建]      │  │  - 技能类型 [攻击/控制/位移/Buff]  │  │
│  [复制]      │  └─────────────────────────────────────┘  │
│  [删除]      │                                            │
│             │  ┌─────────────────────────────────────┐  │
│  ⚔ [2001]   │  │ 技能配置                            │  │
│  冲刺斩*     │  │  - 学习所需等级                    │  │
│  动作数: 2   │  │  - 技能最大等级                    │  │
│             │  │  - 展示冷却时间                    │  │
│  🔒 [2002]  │  │  - 展示法力消耗                    │  │
│  冰冻术      │  │  - 图标ID                          │  │
│  动作数: 1   │  └─────────────────────────────────────┘  │
│             │                                            │
│  💨 [2003]  │  ┌─────────────────────────────────────┐  │
│  闪现       │  │ 技能动作（简化视图）                 │  │
│  动作数: 1   │  │  动作 3001: 冲刺                   │  │
│             │  │    - 实际消耗: 15                  │  │
│             │  │    - 实际冷却: 270帧               │  │
│             │  │    ℹ 触发帧: 2 个                   │  │
│             │  │                                     │  │
│             │  │  动作 3002: 斩击                   │  │
│             │  │    - 实际消耗: 20                  │  │
│             │  │    - 实际冷却: 180帧               │  │
│             │  │    ℹ 触发帧: 1 个                   │  │
│             │  └─────────────────────────────────────┘  │
│             │                                            │
└─────────────┴────────────────────────────────────────────┘
技能总数: 15  未保存: 1  当前: [2001] 冲刺斩
```

---

## 📊 数据结构

### 技能数据（SkillEditorData）
```csharp
SkillEditorData
├── 技能基本信息
│   ├── SkillId (int, 只读)
│   ├── SkillName (string)
│   ├── SkillDescription (string)
│   └── SkillType (enum: 攻击/控制/位移/Buff)
├── 技能配置
│   ├── RequiredLevel (int)
│   ├── MaxLevel (int)
│   ├── DisplayCooldown (float, 秒)
│   ├── DisplayCost (int)
│   └── IconId (int)
└── 技能动作列表
    └── SkillActions (List<SkillActionData>)
```

### 技能动作数据（SkillActionData）
```csharp
SkillActionData
├── ActionId (int, 必须对应ActionTable)
├── SkillId (int, 所属技能)
├── ActionName (string, 只读, 动态查询)
├── AttackBoxInfo (string)
├── ActualCost (int, 实际消耗)
├── ActualCooldown (int, 实际冷却帧数)
└── TriggerFrames (List<TriggerFrameInfo>)
    └── Frame, TriggerType, EffectId
```

---

## ⚠️ 重要约束

### 1. SkillActionTable 与 ActionTable 一一对应

**规则**：
- 每个 SkillAction 的 `ActionId` **必须**在 `ActionTable` 中存在
- `ActionTable` 中的 `actionType` 应为 `"skill"`
- **不允许重复的 ActionId**（一个动作只能属于一个技能）

**验证器会检查**：
```csharp
✅ ActionId 在 ActionTable 中存在
✅ ActionType = "skill"
✅ 无重复的 ActionId
```

### 2. 技能ID范围

- **技能ID范围**: 2000 - 9999
- **新建技能**: 自动从2000开始递增
- **验证失败**: 会阻止保存

### 3. 触发帧配置

**格式**: `"Frame5:Collision:4001,Frame10:Direct:4002"`
- **Frame**: 帧号（非负整数）
- **TriggerType**: `Collision` / `Direct` / `Condition`
- **EffectId**: 对应 SkillEffectTable 的ID

---

## 🔧 常见操作

### 创建新技能
1. 点击左侧"新建"按钮
2. 自动生成唯一ID（从2000开始）
3. 填写技能基本信息
4. 添加技能动作（参考下方）
5. 保存

### 添加技能动作
1. 在右侧"技能动作"面板
2. 点击列表底部 `[+]` 按钮
3. 输入 `ActionId`（必须在ActionTable中存在）
4. 配置 Cost 和 Cooldown
5. **触发帧配置** → 暂时在CSV中手动编辑，或等待阶段2B

### 复制技能
1. 选中要复制的技能
2. 点击"复制"按钮
3. 自动生成新ID
4. 修改名称和属性
5. 保存

### 验证数据
1. 点击顶部"验证"按钮
2. 查看验证结果
3. 修复错误
4. 重新验证

---

## ⚙️ 配置文件路径

```
SkillTable:       AstrumConfig/Tables/Datas/Skill/#SkillTable.csv
SkillActionTable: AstrumConfig/Tables/Datas/Skill/#SkillActionTable.csv
ActionTable:      AstrumConfig/Tables/Datas/Entity/#ActionTable.csv
```

**自动备份**：每次保存前自动备份，保留最近5个版本

---

## 🚧 已知限制（阶段2A）

### 触发帧编辑限制
- ⚠️ **不支持可视化编辑**：需要在CSV中手动编辑
- ⚠️ **不支持时间轴视图**：只显示数量
- 🔜 **解决方案**：阶段2B - 独立的 SkillAction 编辑器

### 碰撞盒编辑限制
- ⚠️ **纯文本输入**：格式 `"Box1:5x2x1,Box2:3x3x1"`
- ⚠️ **无可视化**：无法实时预览
- 🔜 **解决方案**：阶段2B - 碰撞盒可视化编辑器

### 预览功能
- ⚠️ **暂无预览**：无法预览技能动画和效果
- 🔜 **解决方案**：阶段2B - 集成 RolePreviewModule

---

## 📝 使用建议

### 推荐工作流程

1. **先在 ActionTable 中创建动作**
   - 配置动画路径
   - 设置 actionType = "skill"

2. **在技能编辑器中创建技能**
   - 填写基本信息
   - 添加动作引用

3. **在CSV中配置触发帧**（临时方案）
   - 打开 `#SkillActionTable.csv`
   - 编辑 `triggerFrames` 字段
   - 格式: `"Frame5:Collision:4001,Frame10:Direct:4002"`

4. **刷新并验证**
   - 点击"刷新"重新加载
   - 点击"验证"检查数据完整性

---

## 🐛 常见问题

**Q: 保存时提示验证失败？**  
A: 检查以下几点：
- ActionId 是否在 ActionTable 中存在
- 技能名称是否为空
- 是否有重复的 ActionId
- 触发帧格式是否正确

**Q: ActionName 显示为 "Action_XXXX"？**  
A: 说明该 ActionId 在 ActionTable 中不存在，需要先创建

**Q: 如何编辑触发帧？**  
A: 阶段2A 暂不支持，请在CSV中手动编辑，或等待阶段2B

**Q: 技能动作数量为0？**  
A: 点击"技能动作"列表底部的 `[+]` 按钮添加动作

---

## 🔜 未来计划（阶段2B）

**SkillAction 编辑器**（独立窗口）：
- 🔜 触发帧时间轴可视化
- 🔜 碰撞盒可视化编辑器
- 🔜 效果关联编辑
- 🔜 动画预览
- 🔜 批量编辑支持

**预计开发时间**: 3-4小时

---

## 📚 相关文档

- [角色编辑器](./README.md)
- [Luban CSV框架](../../../AstrumConfig/Doc/Luban_CSV框架使用指南.md)
- [技能系统策划案](../../../AstrumConfig/Doc/技能系统策划案.md)

---

**最后更新**: 2025-10-08  
**版本**: v1.0.0 (阶段2A)  
**开发者**: Astrum Team

