# AstrumConfig

Astrum项目的配置和协议数据目录。

## 目录结构

```
AstrumConfig/
├── Proto/              # Protocol Buffer协议文件
│   ├── connectionstatus_C_4000.proto  # 连接状态协议
│   ├── game_S_3000.proto              # 游戏服务端协议
│   ├── gamemessages_C_2000.proto      # 游戏消息协议
│   └── networkcommon_C_1000.proto     # 网络通用协议
├── Tables/             # Luban表格配置文件
│   ├── Datas/          # 数据表格文件
│   ├── Defines/        # 定义文件
│   ├── output/         # 生成输出目录
│   ├── gen.bat         # 全量生成脚本
│   ├── gen.sh          # Linux/Mac生成脚本
│   ├── gen_client.bat  # 客户端生成脚本
│   └── luban.conf      # Luban配置文件
└── README.md           # 本文件
```

## Proto文件命名规范

Proto文件必须按照以下格式命名：`name_C_1.proto`

- `name`: 协议名称（如：test, game, user等）
- `C`: 使用范围标识
  - `C`: 仅客户端使用
  - `S`: 仅服务端使用  
  - `CS`: 客户端和服务端都使用
- `1`: 起始opcode编号

## 协议文件格式

### 基本结构

```protobuf
// 协议说明
// ResponseType: ResponseMessageName

message RequestMessage
{
    int32 id = 1; // 字段说明
    string data = 2; // 字段说明
}

message ResponseMessage
{
    bool success = 1; // 字段说明
    string message = 2; // 字段说明
}
```

### 特殊注释

- `// ResponseType: MessageName` - 指定响应消息类型
- `// no dispose` - 在消息结束的`}`后添加，表示不自动生成Dispose方法

### 支持的数据类型

- 基本类型：`int32`, `int64`, `uint32`, `uint64`, `bool`, `string`, `bytes`
- 集合类型：`repeated Type`, `map<KeyType, ValueType>`
- 嵌套消息：`message Type`

## 代码生成

Proto文件通过Proto2CS工具自动生成C#代码：

1. 运行Proto2CS工具
2. 自动生成对应的C#消息类
3. 生成的代码包含：
   - MemoryPack序列化属性
   - 对象池管理方法
   - Dispose方法
   - Opcode常量定义

## 注意事项

- 保持协议版本兼容性
- 字段编号不要随意修改
- 添加新字段时使用新的编号
- 删除字段时保留编号，标记为deprecated
- 定期更新协议文档

## Luban业务表格配置

### 概述

Luban 是一个强大、易用、优雅、稳定的游戏配置解决方案，用于生成和管理游戏数据表。本节重点介绍如何配置业务表格，而不是基础部署。


### 生成命令

#### 全量生成（服务端+客户端）
```bash
# Windows
gen.bat

# Linux/Mac
./gen.sh
```

#### 客户端生成
```bash
# Windows
gen_client.bat
```

### 业务表格配置方法

#### Excel表格格式规范

业务表格必须遵循特定的格式规范，使用特殊的行来定义字段信息：

| 行类型 | 说明 | 示例 |
|--------|------|------|
| `##var` | 字段名称定义行 | `id`, `name`, `level`, `hp` |
| `##type` | 字段类型定义行 | `int`, `string`, `float`, `bool` |
| `##group` | 导出分组定义行 | `c`, `s`, `c,s` |
| `##desc` | 字段描述行（可选） | `角色ID`, `角色名称` |
| 数据行 | 实际数据，从第4行开始 | `1001`, `玩家1`, `10`, `100.5` |

#### 完整的Excel表格示例

**装备配置表 (equip.xlsx)**:

| ##var    | id   | name      | level | attack | defense | type    | desc        |
|----------|------|-----------|-------|--------|---------|---------|-------------|
| ##type   | int  | string    | int   | float  | int     | enum    | string      |
| ##group  | c,s  | c         | c,s   | c,s    | c,s     | c,s     | c           |
| ##desc   | 装备ID| 装备名称  | 等级  | 攻击力  | 防御力   | 装备类型   | 描述        |
|          | 1001 | 新手剑    | 1     | 10.5   | 5       | weapon  | 新手专用武器 |
|          | 1002 | 铁剑      | 5     | 25.0   | 12      | weapon  | 普通铁制武器 |
|          | 2001 | 布衣      | 1     | 0.0    | 8       | armor   | 基础防具    |

#### 数据类型说明

| 类型 | 说明 | 示例 |
|------|------|------|
| `int` | 整数 | `100`, `-50` |
| `long` | 长整数 | `1000000000` |
| `float` | 浮点数 | `10.5`, `3.14` |
| `double` | 双精度浮点数 | `10.123456` |
| `bool` | 布尔值 | `true`, `false` |
| `string` | 字符串 | `"hello"`, `"装备名称"` |
| `enum` | 枚举类型 | 需要在 `__enums__.xlsx` 中定义 |
| `bean` | 复合类型 | 需要在 `__beans__.xlsx` 中定义 |
| `array<int>` | 整数数组 | `[1,2,3,4]` |
| `array<string>` | 字符串数组 | `["a","b","c"]` |
| `array,类型#sep=分隔符` | 带分隔符数组 | `array,int#sep==` |

**数组类型详解**：

Luban不支持`int[]`语法，必须使用`array,类型`格式：

- **基本数组语法**：`array,int` - 整数数组
- **带分隔符数组**：`array,int#sep=分隔符` - 指定分隔符的数组
- **常用分隔符**：
  - 逗号分隔：`array,int#sep=,` → 数据格式：`"1001,1002,1003"`
  - 等号分隔：`array,int#sep==` → 数据格式：`1001==1002==1003`
  - 竖线分隔：`array,string#sep=|` → 数据格式：`"a|b|c"`
| `map<int,string>` | 键值对 | `{1:"a",2:"b"}` |

#### 导出分组说明

| 分组 | 说明 | 用途 |
|------|------|------|
| `c` | 客户端 | 仅客户端使用 |
| `s` | 服务端 | 仅服务端使用 |
| `c,s` | 客户端+服务端 | 两端都使用 |
| `e` | 编辑器 | 编辑器工具使用 |

### 表格注册

#### 方式一：自动导入（推荐方式）

使用文件名前缀 `#` 创建表格，无需在 `__tables__.xlsx` 中注册：

**文件名格式：** `#表格名.xlsx`

**示例：**
- `#EntityBase.xlsx` - 自动生成 `Entity.TbEntityBase` 类
- `#Action.xlsx` - 自动生成 `Action.TbAction` 类  
- `#EntityModel.xlsx` - 自动生成 `EntityModel.TbEntityModel` 类

**优势：**
- 无需手动维护 `__tables__.xlsx` 文件
- 减少配置错误
- 简化表格管理流程
- 支持嵌套目录结构

**创建新表格步骤：**
1. 在 `Datas/` 目录下创建子文件夹（如 `Entity/`）
2. 创建以 `#` 开头的 Excel 文件（如 `#EntityBase.xlsx`）
3. 按照标准格式填写表格内容（##var、##type、##group、##desc 行）
4. 运行生成脚本即可自动导入表格

#### 方式二：手动注册（传统方式）

创建业务表格后，需要在 `__tables__.xlsx` 中注册：

| ##var | full_name | value_type | define_from_excel | input |
|-------|-----------|------------|-------------------|-------|
|       | demo.TbEquip | Equip | true | equip.xlsx |

- `full_name`: 表的完整名称，格式为 `命名空间.表名`
- `value_type`: 记录类型名称
- `define_from_excel`: 设置为 `true` 表示从Excel定义
- `input`: Excel文件名

#### 数据结构定义

如果业务表格使用了复杂的数据类型，需要在相应的定义文件中声明：

**__beans__.xlsx** (定义复合类型):
| ##var | name | fields |
|-------|------|--------|
|       | EquipData | id:int;name:string;stats:map<int,float> |

**__enums__.xlsx** (定义枚举类型):
| ##var | name | items |
|-------|------|-------|
|       | EquipType | weapon:1;armor:2;accessory:3 |

### 输出目录结构

```
Tables/output/
├── Client/              # 客户端数据
│   ├── cfg.bytes       # 二进制数据文件
│   └── cfg.json        # JSON数据文件
└── Server/              # 服务端数据
    ├── cfg.bytes
    └── cfg.json
```

生成的代码会输出到：`AstrumProj/Assets/Script/Generated/Table/`

### 常见业务场景配置示例

#### 1. 角色等级配置表

**level_config.xlsx**:
| ##var    | level | exp_required | hp_base | mp_base | unlock_features |
|----------|-------|--------------|---------|---------|-----------------|
| ##type   | int   | long         | int     | int     | array<string>   |
| ##group  | c,s   | c,s          | c,s     | c,s     | c               |
| ##desc   | 等级  | 所需经验     | 基础血量| 基础蓝量| 解锁功能        |
|          | 1     | 0            | 100     | 50      | ["skill1"]      |
|          | 2     | 100          | 120     | 60      | ["skill2"]      |
|          | 3     | 250          | 150     | 75      | ["skill3","pet"] |

**数组类型配置示例**：

**技能动作ID数组配置 (skill_actions.xlsx)**:
| ##var    | id   | name      | skillActionIds |
|----------|------|-----------|----------------|
| ##type   | int  | string    | array,int#sep== |
| ##group  | c,s  | c         | c,s            |
| ##desc   | 技能ID| 技能名称  | 技能动作ID列表  |
|          | 2001 | 冲刺斩    | 3001==3002     |
|          | 2002 | 防御反击  | 3003           |
|          | 2003 | 多重射击  | 3004==3005     |

**注意**：使用`array,int#sep==`类型时，数据格式为`3001==3002`，而不是`[3001,3002]`。

#### 2. 道具配置表

**item_config.xlsx**:
| ##var    | id   | name      | type    | stack_max | sell_price | effects    |
|----------|------|-----------|---------|-----------|------------|------------|
| ##type   | int  | string    | enum    | int       | int        | map<int,int>|
| ##group  | c,s  | c         | c,s     | c,s       | c,s        | c,s        |
| ##desc   | 道具ID| 道具名称  | 道具类型| 最大堆叠  | 出售价格   | 效果属性    |
|          | 1001 | 生命药水  | potion  | 99        | 10         | {1:100}    |
|          | 1002 | 魔法药水  | potion  | 99        | 15         | {2:50}     |
|          | 2001 | 铁剑      | weapon  | 1         | 100        | {3:20}     |

对应的枚举定义：
**__enums__.xlsx**:
| ##var | name | items |
|-------|------|-------|
|       | ItemType | weapon:1;potion:2;material:3 |

#### 3. 技能配置表

**skill_config.xlsx**:
| ##var    | id   | name      | level | damage | cooldown | target_type | effects    |
|----------|------|-----------|-------|--------|----------|-------------|------------|
| ##type   | int  | string    | int   | float  | float    | enum        | bean       |
| ##group  | c,s  | c         | c,s   | c,s    | c,s      | c,s         | c,s        |
| ##desc   | 技能ID| 技能名称  | 等级  | 伤害值  | 冷却时间  | 目标类型    | 技能效果    |
|          | 1001 | 火球术    | 1     | 50.0   | 3.0      | enemy       | {type:1,value:50} |
|          | 1002 | 治疗术    | 1     | 30.0   | 5.0      | ally        | {type:2,value:30} |

#### 4. 实体配置表（使用自动导入）

**#EntityBase.xlsx**:
| ##var    | entityId | modelId | idleAction | walkAction | runAction | jumpAction | birthAction | deathAction |
|----------|----------|---------|------------|------------|-----------|------------|-------------|-------------|
| ##type   | int      | int     | string     | string     | string    | string     | string      | string      |
| ##group  |          |         |            |            |           |            |             |             |
| ##desc   | 实体ID   | 模型ID  | 静止动作   | 走路动作   | 跑步动作  | 跳跃动作   | 出生动作    | 死亡动作    |
|          | 1001     | 2001    | idle_01    | walk_01    | run_01    | jump_01    | birth_01    | death_01    |
|          | 1002     | 2002    | idle_02    | walk_02    | run_02    | jump_02    | birth_02    | death_02    |
|          | 1003     | 2003    | idle_03    | walk_03    | run_03    | jump_03    | birth_03    | death_03    |

**注意：** 使用 `#` 前缀的文件名会自动导入，无需在 `__tables__.xlsx` 中注册。

对应的Bean定义：
**__beans__.xlsx**:
| ##var | name | fields |
|-------|------|--------|
|       | SkillEffect | type:int;value:float |

对应的枚举定义：
**__enums__.xlsx**:
| ##var | name | items |
|-------|------|-------|
|       | TargetType | enemy:1;ally:2;self:3 |

### 使用示例

#### 创建新的业务表格

1. **创建Excel文件**：在 `Datas/` 目录下创建新的Excel文件
2. **定义表格结构**：按照格式规范填写 `##var`、`##type`、`##group` 行
3. **填写数据**：从第4行开始填写实际数据
4. **注册表格**：在 `__tables__.xlsx` 中添加新表注册信息
5. **定义类型**：如有需要，在 `__enums__.xlsx` 或 `__beans__.xlsx` 中定义复杂类型
6. **生成代码**：运行生成脚本生成代码和数据文件

#### 在项目中使用生成的代码

```csharp
// 获取装备配置
var equipTable = cfg.Tables.TbEquip;
var sword = equipTable[1001]; // 获取ID为1001的装备
Console.WriteLine($"装备名称: {sword.Name}, 攻击力: {sword.Attack}");

// 遍历所有装备
foreach (var equip in equipTable.DataList)
{
    if (equip.Level >= 5)
    {
        Console.WriteLine($"高级装备: {equip.Name}");
    }
}
```

### 注意事项和最佳实践

#### 表格配置注意事项

1. **Excel格式要求**：
   - 必须严格按照 `##var`、`##type`、`##group` 行的格式填写
   - 字段名称不能包含特殊字符，建议使用英文
   - 数据类型必须准确，错误会导致生成失败
   - 导出分组要正确设置，避免客户端包含服务端数据

2. **数据类型使用**：
   - 优先使用基础类型（int, string, float, bool）
   - 复杂结构使用 `bean` 类型，在 `__beans__.xlsx` 中定义
   - 固定选项使用 `enum` 类型，在 `__enums__.xlsx` 中定义
   - 数组和映射类型要谨慎使用，确保数据格式正确

3. **表格注册**：
   - 每个业务表格都必须在 `__tables__.xlsx` 中注册
   - `full_name` 使用点分隔的命名空间格式
   - `input` 文件名要与实际Excel文件名完全一致

4. **数据完整性**：
   - 主键字段（通常是id）不能重复
   - 必填字段不能为空
   - 引用其他表的字段要确保引用的记录存在

#### 最佳实践

1. **命名规范**：
   - 表格文件使用下划线分隔：`item_config.xlsx`
   - 字段名称使用驼峰命名：`itemId`, `itemName`
   - 枚举值使用小写：`weapon`, `armor`, `accessory`

2. **分组策略**：
   - 客户端显示相关数据使用 `c` 分组
   - 服务端逻辑相关数据使用 `s` 分组
   - 两端都需要的数据使用 `c,s` 分组
   - 敏感数据（如价格、掉落概率）仅服务端使用

3. **性能优化**：
   - 避免过大的表格，考虑分表策略
   - 合理使用数组和映射类型，避免过度嵌套
   - 定期清理无用的配置数据

4. **版本管理**：
   - 修改表格后必须重新生成代码和数据
   - 保持 `luban.conf` 配置与实际文件结构一致
   - 生成的代码不要手动修改，会被覆盖
   - 数据文件路径要确保正确，避免运行时找不到文件
   - 重要配置变更要记录变更日志

#### 常见问题解决

1. **生成失败**：
   - 检查Excel格式是否正确
   - 确认数据类型是否支持
   - 验证表格注册信息是否正确
   - **数组类型错误**：Luban不支持`int[]`语法，应使用`array,int`或`array,int#sep=分隔符`

2. **运行时错误**：
   - 检查数据文件路径是否正确
   - 确认客户端和服务端使用相同的数据文件
   - 验证字段访问权限（分组设置）

3. **数据不一致**：
   - 确保客户端和服务端使用相同版本的配置数据
   - 检查导出分组设置是否正确
   - 验证表格注册信息是否完整
