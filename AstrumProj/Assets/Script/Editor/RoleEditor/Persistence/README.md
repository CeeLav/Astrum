# 通用Luban CSV读写框架

> 📖 **项目路径**: `AstrumProj/Assets/Script/Editor/RoleEditor/Persistence/`  
> 🔧 **依赖**: CsvHelper 33.1.0  
> ✅ **状态**: 已测试通过

## 概述

这是一个通用的CSV表格读写框架，专门用于处理Luban格式的CSV表。使用 **CsvHelper** 库进行CSV解析和生成，不需要为每张表都写重复的读写代码，只需要定义数据映射即可。

### 核心特性

✅ **通用性** - 一套代码支持所有Luban表  
✅ **类型安全** - 强类型映射，编译期检查  
✅ **空值处理** - 自动处理CSV中的空字段  
✅ **自动备份** - 写入前自动备份原文件  
✅ **易扩展** - 添加新表只需定义映射类

## 核心组件

### 1. LubanCSVReader（通用读取器）
使用 **CsvHelper** 读取任何Luban格式的CSV表，通过反射和 TableFieldAttribute 自动解析并填充数据对象。

### 2. LubanCSVWriter（通用写入器）
使用 **CsvHelper** 将数据对象序列化并写入Luban格式的CSV表，自动处理格式化和引号转义。

### 3. TableFieldAttribute（字段映射特性）
用于标记数据类中的字段与CSV列的映射关系。

### 4. LubanTableConfig（表配置）
定义表的结构，包括文件路径、表头信息等。

## 使用方法

### 步骤1：创建数据映射类

```csharp
using Astrum.Editor.RoleEditor.Persistence.Core;

public class YourTableData
{
    [TableField(0, "FieldName1")]
    public int Field1 { get; set; }
    
    [TableField(1, "FieldName2")]
    public string Field2 { get; set; }
    
    [TableField(2, "FieldName3")]
    public float Field3 { get; set; }
    
    // 获取表配置
    public static LubanTableConfig GetTableConfig()
    {
        return new LubanTableConfig
        {
            FilePath = "AstrumConfig/Tables/Datas/YourFolder/#YourTable.csv",
            HeaderLines = 4,
            HasEmptyFirstColumn = true,
            Header = new TableHeader
            {
                VarNames = new List<string> { "field1", "field2", "field3" },
                Types = new List<string> { "int", "string", "float" },
                Groups = new List<string> { "", "", "" },
                Descriptions = new List<string> { "字段1描述", "字段2描述", "字段3描述" }
            }
        };
    }
}
```

### 步骤2：读取数据

```csharp
var config = YourTableData.GetTableConfig();
var dataList = LubanCSVReader.ReadTable<YourTableData>(config);

foreach (var data in dataList)
{
    Debug.Log($"Field1: {data.Field1}, Field2: {data.Field2}");
}
```

### 步骤3：写入数据

```csharp
var config = YourTableData.GetTableConfig();
var dataList = new List<YourTableData>
{
    new YourTableData { Field1 = 1, Field2 = "Test", Field3 = 1.5f }
};

bool success = LubanCSVWriter.WriteTable(config, dataList, enableBackup: true);
```

## 特性说明

### TableFieldAttribute

- **Index**: 列索引（从0开始）
- **Name**: 字段名称（可选，用于调试）
- **Ignore**: 是否忽略此字段

```csharp
[TableField(0, "EntityId")]           // 第0列，名称为EntityId
public int EntityId { get; set; }

[TableField(5, Ignore = true)]         // 第5列，但忽略不处理
public int IgnoredField { get; set; }
```

### LubanTableConfig

- **FilePath**: CSV文件路径（相对于项目根目录）
- **HeaderLines**: 表头行数（默认4行：##var, ##type, ##group, ##desc）
- **HasEmptyFirstColumn**: 是否首列为空（Luban格式默认true）
- **Header**: 表头定义（用于写入时生成表头）

## 示例：CharacterData读写

CharacterDataReader和CharacterDataWriter展示了如何使用通用框架：

1. 定义EntityTableData和RoleTableData映射类
2. 使用LubanCSVReader读取两张表
3. 合并数据为CharacterEditorData
4. 编辑后拆分为EntityTableData和RoleTableData
5. 使用LubanCSVWriter写回两张表

## 支持的数据类型

- int
- float
- double
- bool
- string
- Enum（自动转换）

## 技术特点

### 使用 CsvHelper 的优势
1. **成熟可靠** - 久经考验的开源CSV库
2. **功能强大** - 自动处理各种边界情况（引号、逗号、换行符等）
3. **性能优化** - 比手写解析更快更稳定
4. **易于调试** - 完善的文档和社区支持

### Luban 格式适配
框架专门适配了Luban表格式的特殊性：
- **跳过前4行元数据**：##var, ##type, ##group, ##desc
- **首列空白处理**：自动处理每行数据首列的空白
- **动态ClassMap**：通过反射和TableFieldAttribute动态生成CsvHelper映射

## 注意事项

1. **列索引从0开始**，但Luban表通常首列为空，所以实际CSV列号要+1
2. **表头必须正确**，包括变量名、类型、分组、描述
3. **自动备份**：写入时会自动备份原文件（保留最近5个备份）
4. **CSV转义**：CsvHelper自动处理逗号、引号、换行符的转义
5. **依赖CsvHelper**：需要在项目中添加CsvHelper库

## 扩展新表

要添加新表的读写支持：

1. 在`Mappings/`目录创建新的数据映射类
2. 使用`[TableField]`标记字段
3. 实现`GetTableConfig()`静态方法
4. 使用`LubanCSVReader/Writer`读写数据

无需修改通用框架代码！

## 测试

使用菜单 `Tools/Role & Skill Editor/Test/` 测试读写功能：
- **Test CSV Read**: 测试读取功能
- **Test CSV Write**: 测试写入功能
- **Test Round Trip**: 测试完整的读→写→读流程

### 测试结果示例

```
[LubanCSVReader] Successfully loaded 3 records from AstrumConfig/Tables/Datas/Entity/#EntityBaseTable.csv
[LubanCSVReader] Successfully loaded 5 records from AstrumConfig/Tables/Datas/Role/#RoleBaseTable.csv
[RoleEditor] Successfully loaded 5 roles
```

## 实际应用

### 角色编辑器中的使用

角色编辑器使用此框架读写 **EntityBaseTable** 和 **RoleBaseTable**：

```csharp
// RoleDataReader.cs - 读取并合并两张表
var entityDataList = LubanCSVReader.ReadTable<EntityTableData>(EntityTableData.GetTableConfig());
var roleDataList = LubanCSVReader.ReadTable<RoleTableData>(RoleTableData.GetTableConfig());

// 合并为 RoleEditorData
var mergedData = MergeData(entityDataList, roleDataList);

// RoleDataWriter.cs - 拆分并写入两张表
LubanCSVWriter.WriteTable(EntityTableData.GetTableConfig(), entityDataList);
LubanCSVWriter.WriteTable(RoleTableData.GetTableConfig(), roleDataList);
```

## 关键技术点

### 1. 空值处理方案

Luban表中经常有空字段（如 `JumpAction` 为空），框架使用**自定义TypeConverter**处理：

```csharp
// NullableInt32Converter
public override object ConvertFromString(string text, ...)
{
    if (string.IsNullOrWhiteSpace(text))
        return 0;  // 空值转为默认值
    
    return base.ConvertFromString(text, row, memberMapData);
}
```

全局注册：
```csharp
csv.Context.TypeConverterCache.AddConverter<int>(new NullableInt32Converter());
```

### 2. 路径解析

框架自动处理项目路径：
```
Application.dataPath    = D:\Astrum\AstrumProj\Assets
向上一级               = D:\Astrum\AstrumProj
向上两级（项目根）      = D:\Astrum

配置路径: "AstrumConfig/Tables/Datas/..."
完整路径: "D:\Astrum\AstrumConfig/Tables/Datas/..."
```

### 3. 动态ClassMap生成

框架通过反射自动生成CsvHelper的ClassMap：

```csharp
// 扫描 TableField 特性
[TableField(0, "EntityId")]
public int EntityId { get; set; }

// 自动生成
var memberMap = classMap.Map(type, property);
memberMap.Index(columnIndex);
```

