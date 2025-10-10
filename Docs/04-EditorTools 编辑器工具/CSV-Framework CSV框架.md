# Luban CSV框架 - 快速参考

> 📖 完整文档：[Luban_CSV框架使用指南.md](./Luban_CSV框架使用指南.md)

## 一分钟上手

### 1️⃣ 定义映射类

```csharp
using Astrum.Editor.RoleEditor.Persistence.Core;

public class YourTableData
{
    [TableField(0)] public int Id { get; set; }
    [TableField(1)] public string Name { get; set; }
    [TableField(2)] public float Value { get; set; }
    
    public static LubanTableConfig GetTableConfig()
    {
        return new LubanTableConfig
        {
            FilePath = "AstrumConfig/Tables/Datas/YourFolder/#YourTable.csv",
            HeaderLines = 4,
            HasEmptyFirstColumn = true,
            Header = new TableHeader
            {
                VarNames = new List<string> { "id", "name", "value" },
                Types = new List<string> { "int", "string", "float" },
                Groups = new List<string> { "", "", "" },
                Descriptions = new List<string> { "ID", "名称", "数值" }
            }
        };
    }
}
```

### 2️⃣ 读取数据

```csharp
var config = YourTableData.GetTableConfig();
var dataList = LubanCSVReader.ReadTable<YourTableData>(config);
```

### 3️⃣ 写入数据

```csharp
var config = YourTableData.GetTableConfig();
LubanCSVWriter.WriteTable(config, dataList);
```

## 常用功能

### 空值处理
```csharp
// CSV中的空字段自动转为默认值
"" → int = 0
"" → float = 0f
"" → string = ""
```

### 数组字段
```csharp
// Luban数组格式
"(array#sep==),1,2,3"

// 需要自定义解析
[TableField(5)]
public List<int> ArrayField { get; set; }
```

### 自动备份
```csharp
// 写入时自动备份（保留5个版本）
LubanCSVWriter.WriteTable(config, data, enableBackup: true);

// 备份文件命名
#YourTable_backup_20251008_143022.csv
```

## 测试菜单

```
Tools/Role & Skill Editor/Test/
├── Test CSV Read      - 测试读取
├── Test CSV Write     - 测试写入
└── Test Round Trip    - 测试完整流程
```

## 常见问题

**Q: 空值导致转换失败？**  
A: 框架已配置 `NullableConverter`，空值自动转为0

**Q: 找不到CSV文件？**  
A: 检查 FilePath 是否正确（相对于 Astrum 根目录）

**Q: 列索引对不上？**  
A: 记住Luban首列为空，框架会自动+1

## 实际应用

### 角色编辑器示例
```csharp
// 读取两张表
var entities = LubanCSVReader.ReadTable<EntityTableData>(...);
var roles = LubanCSVReader.ReadTable<RoleTableData>(...);

// 合并业务数据
var roleEditorData = MergeData(entities, roles);

// 编辑后写回
LubanCSVWriter.WriteTable(..., entityList);
LubanCSVWriter.WriteTable(..., roleList);
```

---

💡 **提示**: 框架使用 CsvHelper + 反射 + 自定义TypeConverter，完美支持Luban格式！

