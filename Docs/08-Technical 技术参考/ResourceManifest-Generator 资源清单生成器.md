# ResourceManifestGenerator 使用说明

## 问题背景

在使用YooAsset的EditorSimulateMode模式运行游戏时，每次启动都会调用`EditorSimulateModeHelper.SimulateBuild()`方法来生成资源清单。如果项目资源量巨大或者本地电脑CPU性能较弱，都会造成生成时间很久，导致游戏启动卡顿。

## 解决方案

通过编写Editor脚本，在Unity编辑器中手动生成资源清单，避免运行时卡顿。

## 功能特性

### 1. 生成资源清单
- **菜单路径**: `Astrum/资源管理/生成资源清单`
- **功能**: 手动调用`EditorSimulateModeHelper.SimulateBuild("DefaultPackage")`生成资源清单
- **特点**: 
  - 显示进度条和生成时间
  - 详细的日志输出
  - 自动刷新资源窗口
  - 错误处理和异常捕获

### 2. 清理资源清单
- **菜单路径**: `Astrum/资源管理/清理资源清单`
- **功能**: 删除已生成的资源清单目录
- **特点**:
  - 安全确认对话框
  - 自动刷新资源窗口

### 3. 打开资源清单目录
- **菜单路径**: `Astrum/资源管理/打开资源清单目录`
- **功能**: 在文件管理器中打开资源清单目录
- **特点**:
  - 自动检测当前平台
  - 支持多种平台（Windows、Android、iOS等）

## 使用方法

### 首次使用
1. 在Unity编辑器中，点击菜单 `Astrum/资源管理/生成资源清单`
2. 等待生成完成（会显示进度条和耗时）
3. 生成成功后，可以正常启动游戏，不会再卡顿

### 资源更新后
当项目中的资源发生变化时：
1. 再次点击 `Astrum/资源管理/生成资源清单`
2. 重新生成最新的资源清单
3. 启动游戏使用最新的资源清单

### 清理操作
如果需要清理旧的资源清单：
1. 点击 `Astrum/资源管理/清理资源清单`
2. 确认删除操作
3. 下次启动游戏时会重新生成

## 技术实现

### 代码修改
在`ResourceManager.cs`中，已经将原来的动态生成改为使用预生成的资源清单：

```csharp
// 原来的代码（会导致卡顿）
//var buildResult = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");

// 修改后的代码（使用预生成的清单）
var packageRoot = "D:\\Develop\\Projects\\Astrum\\AstrumProj\\Bundles\\StandaloneWindows64\\DefaultPackage\\Simulate";
```

### 文件结构
生成的资源清单会保存在以下目录：
```
AstrumProj/Bundles/[平台名称]/DefaultPackage/Simulate/
├── DefaultPackage_Simulate.json    # 资源清单文件
├── DefaultPackage_Simulate.hash    # 哈希文件
└── DefaultPackage_Simulate.bytes   # 资源数据文件
```

### 平台支持
- **Windows**: `StandaloneWindows64`
- **Android**: `Android`
- **iOS**: `iOS`
- **macOS**: `StandaloneOSX`
- **WebGL**: `WebGL`

## 注意事项

1. **YooAsset依赖**: 此功能需要项目启用YooAsset（`#if YOO_ASSET_2`）
2. **资源更新**: 当项目资源发生变化时，需要重新生成资源清单
3. **平台切换**: 切换目标平台后，需要重新生成对应平台的资源清单
4. **文件路径**: 确保ResourceManager.cs中的硬编码路径与实际项目路径匹配

## 故障排除

### 常见问题

1. **菜单项不显示**
   - 检查是否启用了YooAsset
   - 确认Editor脚本编译成功

2. **生成失败**
   - 查看Console窗口的错误信息
   - 检查项目资源是否有问题
   - 确认YooAsset配置正确

3. **路径错误**
   - 检查ResourceManager.cs中的硬编码路径
   - 确认Bundles目录存在

4. **权限问题**
   - 确保Unity编辑器有写入权限
   - 关闭可能占用文件的程序

### 日志信息
生成过程中会在Console窗口输出详细日志：
- 开始时间和结束时间
- 生成耗时
- 文件数量和大小
- 错误信息（如有）

## 更新历史

- **v1.0** (2025-01-14): 初始版本，支持基本的资源清单生成功能


