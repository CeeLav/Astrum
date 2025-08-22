# UI Generator 使用说明

## 概述

UI Generator 是一个基于现有Prefab的Unity UI代码生成工具，可以从现有的UI Prefab自动生成对应的C#代码类和UIRefs组件。

## 功能特性

- 🏗️ 基于现有Prefab生成代码
- 📝 自动生成对应的C# UI逻辑类
- 🔗 集成UIRefs组件，自动管理UI引用
- 🎯 支持所有Unity UI组件
- 🔧 编辑器集成，操作简单
- 📱 自动提取UI结构信息

## 快速开始

### 1. 打开UI Generator

在Unity编辑器中，选择菜单 `Tools > UI Generator` 打开生成器窗口。

### 2. 创建UI Prefab

在Unity编辑器中手动创建UI Prefab，或者使用现有的Prefab。

### 3. 生成UI代码

1. 在UI Generator窗口中点击"选择Prefab"按钮
2. 选择你的UI Prefab文件
3. 设置UI名称（可选，默认使用Prefab文件名）
4. 点击"生成UI代码"按钮
5. 查看生成结果

## 工作流程

### 1. 创建UI Prefab

在Unity编辑器中创建UI Prefab，包含所需的UI组件和布局。

### 2. 选择Prefab

在UI Generator窗口中选择要生成代码的Prefab。

### 3. 自动分析

工具会自动分析Prefab结构，提取所有UI组件信息。

### 4. 生成代码

生成对应的C# UI逻辑类和UIRefs组件。

## 生成的代码结构

### UI类

生成的UI类是一个普通的C#类（不是MonoBehaviour），包含：

- 所有UI元素的引用
- 事件绑定方法
- UI逻辑方法
- 显示/隐藏方法

### UIRefs组件

每个生成的Prefab都会自动添加UIRefs组件，负责：

- 收集所有子节点的引用
- 实例化对应的UI逻辑类
- 提供引用访问接口

## 使用示例

### 1. 在场景中使用

```csharp
// 获取UIRefs组件
var uiRefs = GetComponent<UIRefs>();

// 获取UI实例
var uiInstance = uiRefs.GetUIInstance();

// 调用UI方法
var uiType = uiRefs.GetUIType();
var showMethod = uiType.GetMethod("Show");
showMethod?.Invoke(uiInstance, null);
```

### 2. 动态创建UI

```csharp
// 实例化Prefab
var prefab = Resources.Load<GameObject>("UI/MyUI");
var uiObject = Instantiate(prefab);

// 获取UIRefs组件
var uiRefs = uiObject.GetComponent<UIRefs>();
```

## 高级功能

### 自定义组件

可以通过修改 `ComponentFactory.cs` 来添加对新组件的支持。

### 自定义属性

可以通过修改 `PrefabGenerator.cs` 来添加对新属性的支持。

### 模板系统

可以创建UI模板，快速生成常用的UI结构。

## 注意事项

1. **JSON格式**: 确保JSON格式正确，可以使用在线JSON验证工具
2. **组件名称**: 组件名称必须与Unity中的实际组件名称一致
3. **路径设置**: 确保输出路径存在且有写入权限
4. **命名规范**: UI名称和节点名称应遵循C#命名规范
5. **性能考虑**: 复杂的UI结构可能影响生成性能

## 故障排除

### 常见问题

1. **组件创建失败**: 检查组件名称是否正确
2. **属性设置失败**: 检查属性值类型是否匹配
3. **生成失败**: 查看控制台错误信息
4. **引用丢失**: 检查UIRefs组件配置

### 调试技巧

1. 启用详细日志输出
2. 检查JSON配置文件格式
3. 验证组件和属性支持
4. 查看生成的代码和Prefab

## 更新日志

### v1.0.0
- 基础UI生成功能
- JSON配置支持
- UIRefs组件集成
- 编辑器窗口集成
- 多种UI组件支持

## 技术支持

如有问题或建议，请查看项目文档或联系开发团队。
