# Input 输入配置表

## 概述

本目录包含输入系统的所有配置表，用于定义从物理输入到动作指令的完整映射链路。

## 配置表列表

### 1. #InputBindingTable.csv - 输入绑定配置表

**功能**：定义物理按键/鼠标到逻辑动作的映射

**用途**：
- 配置按键绑定
- 支持主按键和备用按键
- 支持鼠标按键
- 用于按键重映射功能

**示例**：
```csv
Attack,攻击,Space,None,0,Button,Combat
```
表示"攻击"动作可以通过 Space 键或鼠标左键触发。

---

### 2. #LSInputFieldMappingTable.csv - LSInput字段映射表

**功能**：定义逻辑动作如何组装为LSInput字段

**用途**：
- 将多个逻辑动作组合为轴值（如WASD组合为移动向量）
- 将单个按钮映射为布尔字段
- 配置相机方向转换

**示例**：
```csv
MoveX,CompositeAxis,"MoveLeft,MoveRight",false,左右移动合成X轴
```
表示 MoveLeft 和 MoveRight 动作组合为 LSInput.MoveX 字段。

---

### 3. #ActionCommandMappingTable.csv - ActionCommand映射表

**功能**：定义LSInput如何转换为ActionCommand

**用途**：
- 配置命令触发条件
- 设置命令有效帧数
- 配置命令优先级

**示例**：
```csv
attack,Attack,True,3,10,普通攻击
```
表示当 LSInput.Attack 为 true 时，生成有效期3帧的 "attack" 命令。

---

### 4. #InputContextTable.csv - 输入上下文配置表

**功能**：定义不同场景下的输入模式

**用途**：
- 打开UI时禁用战斗输入
- 对话时只允许交互键
- 过场动画时禁用所有输入

**示例**：
```csv
UI,UI界面,"Interact,OpenMenu",Attack|Skill1|Skill2|Dash,100,true
```
表示UI模式下只启用交互和菜单键，禁用战斗相关输入。

## 数据流

```
物理输入 (KeyCode.W)
    ↓ [InputBindingTable]
逻辑动作 (MoveForward)
    ↓ [LSInputFieldMappingTable + 相机方向]
帧同步输入 (LSInput.MoveY)
    ↓ [ActionCommandMappingTable]
动作指令 (ActionCommand "move")
    ↓ [Action系统]
游戏行为 (角色移动)
```

## 使用说明

### 添加新输入

1. 在 `#InputBindingTable.csv` 添加按键绑定
2. 在 `#LSInputFieldMappingTable.csv` 配置LSInput映射（如需新字段则修改proto）
3. 在 `#ActionCommandMappingTable.csv` 配置命令映射
4. 运行Luban代码生成工具

### 修改按键绑定

直接编辑 `#InputBindingTable.csv`，无需修改代码。

### 添加输入上下文

在 `#InputContextTable.csv` 添加新行，代码中使用：
```csharp
InputContextManager.Instance.PushContext("UI");
InputContextManager.Instance.PopContext();
```

## 注意事项

1. **文件名格式**：所有CSV文件必须以 `#` 开头
2. **编码格式**：使用 UTF-8 编码
3. **分隔符**：使用逗号分隔，字段内逗号用引号包裹
4. **空值表示**：使用 `None` 或空字符串
5. **布尔值**：使用 `true`/`false`
6. **注释行**：以 `##` 开头的行为字段定义

## 相关文档

- [Input-System 输入系统架构设计](../../../../Docs/02-CombatSystem%20战斗系统/输入系统/Input-System%20输入系统架构设计.md)
- [Luban CSV框架使用指南](../../../Doc/Luban_CSV框架使用指南.md)

## 版本历史

- **v1.0** (2025-11-09): 初始版本，包含基础输入配置

