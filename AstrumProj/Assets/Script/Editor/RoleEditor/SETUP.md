# 角色编辑器设置说明

## 依赖检查

### 必需的依赖

1. **CsvHelper** ✅ 已安装
   - 路径: `Assets/Packages/CsvHelper.33.1.0/lib/netstandard2.1/CsvHelper.dll`
   - 用途: CSV文件读写

2. **Odin Inspector** ✅ 已安装（从 Art 目录导入）
   - 用途: 强大的编辑器UI特性

## 首次使用步骤

### 1. 刷新 Unity 项目

在 Unity 编辑器中：
- 点击 `Assets > Refresh` 或按 `Ctrl+R`
- 等待Unity重新编译和生成.csproj文件

### 2. 验证编译

检查 Console 窗口是否有编译错误：
- 应该没有关于 `RoleEditor` 命名空间的错误
- 可能会有一些警告（可以忽略）

### 3. 运行测试

在Unity菜单中运行：
```
Tools/Role & Skill Editor/Test/Test CSV Read
```

如果能看到角色数据输出，说明设置成功！

## 文件结构

```
Assets/Script/Editor/
├── Common/                              ✅ 通用编辑器工具
│   ├── AstrumEditorUtility.cs          ✅ 工具类（避免与Unity.EditorUtility冲突）
│   └── Editor.Common.asmdef             ✅ 程序集定义
│
└── RoleEditor/                          ✅ 角色编辑器
    ├── RoleEditor.asmdef                ✅ 程序集定义（引用Common和CsvHelper）
    ├── Core/
    │   └── EditorConfig.cs              ✅ 配置常量
    ├── Data/
    │   ├── RoleEditorData.cs            ✅ 数据模型（使用Odin特性）
    │   └── RoleDataValidator.cs         ✅ 数据验证
    ├── Persistence/
    │   ├── Core/
    │   │   ├── LubanCSVReader.cs        ✅ 通用读取器（CsvHelper）
    │   │   ├── LubanCSVWriter.cs        ✅ 通用写入器（CsvHelper）
    │   │   ├── LubanTableConfig.cs      ✅ 表配置
    │   │   └── TableFieldAttribute.cs   ✅ 字段映射特性
    │   ├── Mappings/
    │   │   ├── EntityTableData.cs       ✅ EntityBaseTable映射
    │   │   └── RoleTableData.cs         ✅ RoleBaseTable映射
    │   ├── RoleDataReader.cs            ✅ 角色数据读取器
    │   ├── RoleDataWriter.cs            ✅ 角色数据写入器
    │   └── README.md                    ✅ 使用文档
    └── Test/
        └── CSVTest.cs                   ✅ 测试菜单
```

## 关键改进

### 1. 命名修正
- ❌ `EditorUtility` (与Unity冲突)
- ✅ `AstrumEditorUtility` (无冲突)

### 2. 数据模型统一
- ❌ `CharacterEditorData`
- ✅ `RoleEditorData`

### 3. 使用CsvHelper
- ✅ 成熟的CSV处理库
- ✅ 自动处理边界情况
- ✅ 性能更好

## 常见问题

### Q: 找不到 CsvHelper 类型
A: 检查 `RoleEditor.asmdef` 是否包含 `"precompiledReferences": ["CsvHelper.dll"]`

### Q: 找不到 Odin 特性
A: 确保已从 `Art/Odin Inspector and Serializer 3.3.1.13.unitypackage` 导入 Odin

### Q: 编译错误：找不到 CharacterDataValidator.cs
A: 在Unity中刷新项目（Ctrl+R），Unity会更新.csproj文件

## 下一步

完成设置后，可以开始：
1. 测试CSV读写功能
2. 创建角色编辑器窗口
3. 实现预览模块

