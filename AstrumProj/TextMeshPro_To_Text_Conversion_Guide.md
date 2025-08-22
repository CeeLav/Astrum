# TextMeshPro转Text转换工具使用指南

## 功能概述

UIGenerator新增了TextMeshPro转Text转换工具，可以将UI预制体中的所有TextMeshPro组件自动转换为Unity内置的Text组件，同时保留原有的文本内容、字体大小、颜色、对齐方式等属性。

## 功能特点

### ✅ 保留的属性
- **文本内容**: 完整保留所有文本
- **字体大小**: 自动转换为整数像素值
- **颜色**: 保留原始颜色设置
- **对齐方式**: 智能转换TextMeshPro对齐到Text对齐
- **富文本支持**: 保留富文本设置
- **射线检测**: 保留raycastTarget设置

### 🔧 转换功能
- **批量转换**: 一次性转换预制体中所有TextMeshPro组件
- **撤销支持**: 支持Unity撤销操作
- **错误处理**: 完善的错误处理和用户提示
- **状态反馈**: 实时显示转换进度和结果

## 使用方法

### 1. 打开UIGenerator
- 在Unity编辑器中，选择菜单 `Tools > UI Generator`
- 或使用快捷键打开UIGenerator窗口

### 2. 选择UI预制体
- 在UIGenerator窗口左侧文件树中选择要转换的UI预制体
- 确保预制体包含TextMeshPro组件

### 3. 执行转换
- 在右侧功能操作区域找到"转换工具"部分
- 点击"TextMeshPro → Text 转换"按钮
- 确认转换操作（会显示转换详情和警告）

### 4. 查看结果
- 转换完成后会显示转换的组件数量
- 在日志区域可以查看详细的转换信息
- 预制体会自动保存修改

## 对齐方式转换对照表

| TextMeshPro对齐 | Text对齐 |
|----------------|----------|
| TopLeft | UpperLeft |
| Top | UpperCenter |
| TopRight | UpperRight |
| Left | MiddleLeft |
| Center | MiddleCenter |
| Right | MiddleRight |
| BottomLeft | LowerLeft |
| Bottom | LowerCenter |
| BottomRight | LowerRight |

## 注意事项

### ⚠️ 重要提醒
1. **不可撤销**: 转换操作会永久移除TextMeshPro组件，请确保已备份重要文件
2. **字体设置**: 转换后会使用Unity默认字体（LegacyRuntime），可能需要手动调整
3. **自动检测**: 工具会自动检测TextMeshPro包是否安装，无需手动配置

### 📋 使用前准备
- 备份重要的UI预制体文件
- 在测试环境中先验证转换效果
- 确保UI预制体中包含TextMeshPro组件

### 🔍 转换后检查
- 检查文本显示是否正常
- 验证字体大小是否合适
- 确认对齐方式是否正确
- 测试富文本功能是否正常

## 错误处理

### 常见问题
1. **"未找到TextMeshPro组件"**: 预制体中没有TextMeshPro组件或未安装TextMeshPro包
2. **"请先选择UI预制体"**: 需要先选择要转换的预制体文件
3. **转换失败**: 检查预制体是否损坏或包含不支持的组件

### 解决方案
- 安装TextMeshPro包: `Window > Package Manager > TextMeshPro > Install`
- 确保选择的文件是.prefab格式的UI预制体
- 检查Unity Console中的详细错误信息
- 如果不需要TextMeshPro功能，可以忽略"未找到TextMeshPro组件"的提示

## 技术实现

### 转换流程
1. 使用反射动态获取TextMeshProUGUI类型
2. 扫描预制体中的所有TextMeshPro组件
3. 使用反射提取每个组件的属性（文本、字体大小、颜色等）
4. 移除TextMeshPro组件
5. 添加Text组件并设置相同属性
6. 转换对齐方式枚举值
7. 设置默认字体资源（LegacyRuntime.ttf）

### 代码结构
- `ConvertTextMeshProToText()`: 主转换方法
- `GetTextMeshProComponents()`: 使用反射获取TextMeshPro组件
- `ConvertSingleTextMeshProToText()`: 单个组件转换（使用反射）
- `GetPropertyValue<T>()`: 反射获取属性值的通用方法
- `ConvertTextAlignment()`: 对齐方式转换（使用反射）
- 使用反射技术，无需TextMeshPro包依赖即可编译和运行

## 版本兼容性

- **Unity版本**: 支持Unity 2019.4及以上版本
- **TextMeshPro**: 自动检测，如果未安装会显示友好提示
- **平台支持**: 支持所有Unity支持的平台
- **依赖要求**: 无需TextMeshPro包依赖，使用反射技术实现

## 更新日志

### v2.0.1 (2025-01-22)
- **字体修复**: 修复Unity新版本中Arial.ttf不再可用的问题
- 使用LegacyRuntime.ttf作为默认字体
- 提高与Unity 2022+版本的兼容性

### v2.0.0 (2025-01-22)
- **重大更新**: 移除TextMeshPro包依赖
- 使用反射技术动态访问TextMeshPro组件
- 自动检测TextMeshPro包是否安装
- 改进错误处理和用户提示
- 更好的兼容性和稳定性

### v1.0.0 (2025-01-22)
- 初始版本发布
- 支持TextMeshPro到Text的完整转换
- 包含属性保留、对齐转换、错误处理等功能
- 使用条件编译支持，需要TextMeshPro包依赖

---

*如有问题或建议，请联系开发团队。*
