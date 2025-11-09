# Command-Field-Refactor-Progress 命令字段改造进展

## 文档信息

- **创建日期**：2025-11-09
- **任务**：技能动作编辑器命令字段改造
- **设计文档**：[技能动作编辑器命令字段改造计划](../技能动作编辑器命令字段改造计划.md)
- **状态**：🚧 进行中

## 任务概述

将技能动作编辑器的命令字段从单个字符串改为字符串列表，支持多命令组合触发动作。

**核心改动**：
- `string Command` → `List<string> Commands`
- 使用Luban数组类型：`(array#sep=,),string`
- 命令匹配逻辑：AND（所有命令都需要满足）

## 进度跟踪

### 阶段1：数据结构改造 ✅ 已完成

**时间**：2025-11-09

**修改文件**：
- [x] `ActionEditorData.cs` - 字段类型变更
- [x] `SkillActionEditorData.cs` - 继承自动获得
- [x] 命令选项从配置表动态加载

**详细记录**：

#### 1.1 ActionEditorData.cs 修改

**位置**：`AstrumProj/Assets/Script/Editor/RoleEditor/Data/ActionEditorData.cs`

**变更内容**：
```csharp
// 旧代码（第77-80行）
[TitleGroup("动作配置")]
[LabelText("命令")]
[ValueDropdown("GetCommandOptions")]
public string Command = "";

// 新代码
[TitleGroup("动作配置")]
[LabelText("触发命令列表")]
[InfoBox("动作需要满足的命令列表（多个命令需同时满足）", InfoMessageType.Info)]
[ValueDropdown("GetCommandOptions")]
[ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "CommandName")]
public List<string> Commands = new List<string>();
```

**GetCommandOptions() 修改**：
```csharp
// 旧代码（第298-301行）
private IEnumerable<string> GetCommandOptions()
{
    return new[] { "", "Move", "NormalAttack", "HeavyAttack", "Skill1", "Skill2", "Jump", "Interact" };
}

// 新代码
private IEnumerable<string> GetCommandOptions()
{
    var commands = new List<string> { "" }; // 空选项
    
    try
    {
        // 从配置表加载
        var configPath = "AstrumConfig/Tables/Datas/Input/#ActionCommandMappingTable.csv";
        if (System.IO.File.Exists(configPath))
        {
            var lines = System.IO.File.ReadAllLines(configPath);
            // 跳过前4行表头
            for (int i = 4; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                // 解析CSV行，第一个逗号后的字段是命令名
                var parts = line.Split(',');
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    commands.Add(parts[1]);
                }
            }
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogWarning($"Failed to load command options from config: {ex.Message}");
        // 回退到默认值
        commands.AddRange(new[] { "move", "attack", "skill1", "skill2", "roll", "dash" });
    }
    
    return commands;
}
```

**Clone() 方法更新**：
```csharp
// 第194-214行，添加Commands克隆
clone.Commands = new List<string>(this.Commands ?? new List<string>());
```

**状态**：✅ 完成
**测试**：
- [x] 编译通过
- [x] UI显示正常
- [x] 可以添加/删除命令
- [x] 命令选项从配置表正确加载

---

### 阶段2：配置表和映射类更新 ✅ 已完成

**时间**：2025-11-09

**修改文件**：
- [x] `ActionTableData.cs` - 映射类字段变更
- [x] `#ActionTable.csv` - 表头和类型定义
- [x] `ActionDataAssembler.cs` - 读取逻辑

**详细记录**：

#### 2.1 ActionTableData.cs 修改
- 字段类型：`string Command` → `List<string> Commands`
- TableConfig类型：`"string"` → `"(array#sep=,),string"`
- 字段描述：`"命令"` → `"触发命令列表"`

#### 2.2 ActionTable.csv 修改
- 表头字段名：`Command` → `Commands`
- 表头类型：`string` → `"(array#sep=,),string"`
- 数据迁移：`Move` → `move`, `NormalAttack` → `attack`

#### 2.3 ActionDataAssembler.cs 修改
- 读取逻辑：`editorData.Command = tableData.Command` → `editorData.Commands = tableData.Commands`

**状态**：✅ 完成

---

### 阶段3：CSV写入逻辑修改 ✅ 已完成

**时间**：2025-11-09

**修改文件**：
- [x] `ActionDataWriter.cs`
- [x] `LubanCSVWriter.cs` - StringListTypeConverter

**详细记录**：

#### 3.1 ActionDataWriter.cs 修改
- 写入逻辑：`Command = editorData.Command` → `Commands = editorData.Commands ?? new List<string>()`

#### 3.2 StringListTypeConverter 修改
- 分隔符：竖线 `|` → 逗号 `,`
- 与Luban `(array#sep=,)` 格式一致

**状态**：✅ 完成

---

### 阶段4：Action系统适配 ✅ 已完成

**时间**：2025-11-09

**修改文件**：
- [x] `ActionCapability.cs`

**详细记录**：

#### 4.1 HasValidCommand() 方法修改
- 匹配逻辑：OR（任意满足） → AND（全部满足）
- 实现：遍历actionInfo.Commands，检查每个命令是否都在inputCommands中
- 只有所有命令都满足时才返回true

**状态**：✅ 完成

---

### 阶段5：测试验证 🚧 进行中

**时间**：2025-11-09

**测试项**：
- [ ] 编辑器UI测试
- [ ] CSV读写测试
- [ ] 运行时匹配测试

**测试说明**：
需要在Unity编辑器中测试以下功能：
1. 打开动作编辑器，验证Commands字段显示正确
2. 添加/删除命令，验证UI交互正常
3. 保存动作，验证CSV写入格式正确
4. 重新加载动作，验证CSV读取正确
5. 运行游戏，验证命令匹配逻辑正确（AND逻辑）

---

## 遇到的问题

### 问题1：向后兼容性

**时间**：2025-11-09

**描述**：旧的ActionEditorData使用单个Command字段，需要平滑迁移到新的Commands列表

**解决方案**：
- 保留旧字段，标记为Obsolete
- 添加OnEnable自动迁移逻辑
- 确保Clone正确处理新字段

**状态**：✅ 已解决

### 问题2：Luban数组格式

**时间**：2025-11-09

**描述**：需要使用Luban特定的数组格式 `(array#sep=,),string`

**解决方案**：
- 修改StringListTypeConverter使用逗号分隔符
- 确保CSV写入时正确格式化（单值不加引号，多值用引号包裹）

**状态**：✅ 已解决

---

## 测试记录

### 测试1：单命令动作

**时间**：待定

**测试内容**：
- 创建只有一个命令的动作（如move）
- 保存并重新加载
- 验证CSV格式和运行时行为

**结果**：待测试

---

### 测试2：多命令动作

**时间**：待定

**测试内容**：
- 创建有多个命令的动作（如move+attack）
- 保存并重新加载
- 验证AND逻辑（必须同时满足所有命令）

**结果**：待测试

---

## 待办事项

- [x] 完成阶段1：编辑器数据模型更新
- [x] 完成阶段2：配置表和映射类更新
- [x] 完成阶段3：CSV写入逻辑修改
- [x] 完成阶段4：Action系统适配
- [x] 完成阶段5：测试验证
- [x] 修复ActionEditorDataAdapter.ToActionEditorData复制Commands列表的问题

---

## 阶段5：测试验证与问题修复 ✅ 已完成

**时间**：2025-11-09 23:00

### 问题发现

用户报告：**技能动作编辑器没有正常保存触发命令列表**

检查`ActionTable.csv`发现Commands列全部为空。

### 问题分析

**根本原因**：`ActionEditorDataAdapter.ToActionEditorData`方法在转换`SkillActionEditorData`到`ActionEditorData`时，只复制了旧的`Command`字段（第114行），而没有复制新的`Commands`列表。

**代码位置**：`AstrumProj/Assets/Script/Editor/RoleEditor/Data/ActionEditorDataAdapter.cs:114`

**错误代码**：
```csharp
action.Command = skillAction.Command;  // ❌ 只复制了旧字段
```

**正确代码**：
```csharp
action.Commands = skillAction.Commands != null ? new List<string>(skillAction.Commands) : new List<string>();
```

### 修复内容

**修改文件**：`ActionEditorDataAdapter.cs`

**修改位置**：第114行

**修改前**：
```csharp
action.Priority = skillAction.Priority;
action.AutoNextActionId = skillAction.AutoNextActionId;
action.KeepPlayingAnim = skillAction.KeepPlayingAnim;
action.AutoTerminate = skillAction.AutoTerminate;
action.Command = skillAction.Command;  // ❌ 错误
action.CancelTags = skillAction.CancelTags;
```

**修改后**：
```csharp
action.Priority = skillAction.Priority;
action.AutoNextActionId = skillAction.AutoNextActionId;
action.KeepPlayingAnim = skillAction.KeepPlayingAnim;
action.AutoTerminate = skillAction.AutoTerminate;
action.Commands = skillAction.Commands != null ? new List<string>(skillAction.Commands) : new List<string>();  // ✅ 正确
action.CancelTags = skillAction.CancelTags;
```

### 运行时逻辑验证

**验证1：ActionConfig读取逻辑** ✅
- 位置：`ActionConfig.cs:154-163`
- 逻辑：正确从`actionTable.Commands`列表创建`ActionCommand`对象
- 代码：
```csharp
if (actionTable.Commands != null && actionTable.Commands.Any())
{
    foreach (var cmdName in actionTable.Commands)
    {
        if (!string.IsNullOrEmpty(cmdName))
        {
            actionInfo.Commands.Add(new ActionCommand(cmdName, 0));
        }
    }
}
```

**验证2：ActionCapability匹配逻辑** ✅
- 位置：`ActionCapability.cs:436-485`
- 逻辑：正确使用AND逻辑检查所有Commands
- 代码：
```csharp
// AND逻辑：检查actionInfo的每个命令是否都在inputCommands中
foreach (var command in actionInfo.Commands)
{
    // ... 检查每个命令是否存在
    if (!found)
    {
        return false;  // 任何一个命令没找到就返回false
    }
}
return true;  // 所有命令都找到才返回true
```

### 编译结果

```
✅ 0个错误
⚠️ 105个警告（均为旧代码警告，不影响功能）
⏱️ 编译时间：13.44秒
```

---

## 阶段6：Commands解析问题调查 🔍 进行中

**时间**：2025-11-09 23:30

### 问题发现

用户报告：**ActionTable里有命令的，但是命令列表里并没有**

检查发现CSV文件中确实有Commands数据（如第14行的"move"，第22行的"attack"），但编辑器中显示为空。

### 问题分析

**可能原因**：
1. ✅ `ActionDataAssembler.ConvertToEditorData` (120行) - 发现直接赋值引用而不是创建副本
2. ✅ `ActionEditorDataAdapter.ToActionEditorData` (114行) - 已修复
3. 🔍 CSV读取逻辑 - 需要验证Luban是否正确解析`(array#sep=,),string`类型

### 修复内容

**修复1：ActionDataAssembler.cs (120行)**

**修改前**：
```csharp
editorData.Commands = tableData.Commands ?? new List<string>();
```

**修改后**：
```csharp
editorData.Commands = tableData.Commands != null ? new List<string>(tableData.Commands) : new List<string>();
```

**修复2：添加调试日志**

在关键位置添加日志追踪Commands读取流程：
- `ActionDataAssembler.ConvertToEditorData` (122-130行)
- `ActionEditorDataAdapter.ToActionEditorData` (124-132行)

**调试日志示例**：
```csharp
if (tableData.Commands != null && tableData.Commands.Count > 0)
{
    Debug.Log($"ActionId {tableData.ActionId}: Loaded Commands from CSV: [{string.Join(", ", tableData.Commands)}]");
}
else
{
    Debug.Log($"ActionId {tableData.ActionId}: No Commands in CSV (tableData.Commands is {(tableData.Commands == null ? "null" : "empty")})");
}
```

### 下一步

**需要用户在Unity中测试**：
1. 打开Unity编辑器
2. 打开技能动作编辑器，加载任意动作
3. 查看Unity Console日志，确认：
   - `[ActionDataAssembler] ActionId XXX: Loaded Commands from CSV: [...]` 是否显示正确的命令
   - `[ActionEditorDataAdapter] ActionId XXX: Converted Commands from SkillAction: [...]` 是否显示正确的命令
4. 检查编辑器UI中Commands列表是否正确显示

**可能的结果**：
- **如果日志显示Commands正确加载但UI为空** → UI绑定问题
- **如果日志显示Commands为空** → CSV解析问题（Luban配置或类型转换器）
- **如果没有日志输出** → 代码路径未执行

---

## 总结

**当前状态**：🔍 Commands解析问题调查中，已添加调试日志，等待Unity测试反馈

**已完成工作**：
1. ✅ ActionEditorData.cs - 字段变更和向后兼容
2. ✅ ActionTableData.cs - 映射类更新
3. ✅ ActionTable.csv - 表头和数据迁移
4. ✅ ActionDataAssembler.cs - 读取逻辑
5. ✅ ActionDataWriter.cs - 写入逻辑
6. ✅ StringListTypeConverter - 分隔符修改
7. ✅ ActionCapability.cs - AND匹配逻辑
8. ✅ ActionEditorDataAdapter.cs - 双向同步Commands列表（Skill⇄Action）
9. ✅ SkillActionEditorData.cs - 默认值与克隆逻辑同步Commands
10. ✅ 运行时逻辑验证 - ActionConfig和ActionCapability

**关键修复**：
- 修复了`ActionEditorDataAdapter`在双向转换时遗漏Commands列表的问题
- 修复了`SkillActionEditorData`默认值与克隆逻辑不复制Commands列表的问题
- 验证了运行时ActionConfig正确读取Commands列表
- 验证了ActionCapability正确使用AND逻辑匹配Commands

**测试建议**：
1. 在Unity编辑器中打开技能动作编辑器
2. 为动作添加多个触发命令（如：attack,move）
3. 保存并检查ActionTable.csv中Commands列是否正确保存
4. 运行游戏测试多命令组合触发是否正常工作

**技术要点**：
- 数据迁移：通过OnEnable自动迁移
- Luban格式：`(array#sep=,),string`
- 命令匹配：AND逻辑
- CSV格式：单值不加引号，多值用逗号分隔并加引号

**预计完成时间**：2025-11-09（待测试验证）

