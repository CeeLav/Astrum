# ASLogger 分类管理系统

## 概述

ASLogger分类管理系统是一个强大的Unity日志管理工具，允许开发者按分类管理日志，并在运行时动态启用或禁用特定的日志输出。

## 主要特性

### 1. 双重控制机制
- **分类级别控制**：可以启用/禁用整个分类的日志
- **单个日志控制**：可以精确控制每个具体的日志语句
- **隔离设置**：分类和单个日志的启用/禁用设置相互独立

### 2. 稳定的日志标识
- **主标识**：基于消息内容和方法名，不依赖行号
- **备用标识**：基于行号，用于匹配
- **自动匹配**：支持多种匹配策略，确保日志正确识别

### 3. 直观的编辑器界面
- **分类树形结构**：优化的视觉层次，更大的点击区域，选中状态指示
- **日志列表**：清晰的日志条目显示，状态指示器，分隔线
- **实时预览**：点击分类后预览该分类下的所有日志
- **搜索过滤**：支持按内容搜索和按级别过滤
- **工具栏**：集成常用操作按钮，减少界面混乱

## 使用方法

### 1. 打开管理器窗口

在Unity编辑器中，选择菜单：`Tools > ASLogger Manager`

### 2. 基本操作

#### 分类操作
- 点击分类前的复选框来启用/禁用整个分类
- 禁用分类会自动禁用该分类下的所有日志
- 启用分类会保持日志的原有状态

#### 单个日志操作
- 点击日志条目右侧的复选框来启用/禁用单个日志
- 启用单个日志会自动启用其所属分类
- 禁用单个日志不会影响分类状态

#### 批量操作
- **全部启用**：启用所有分类和日志
- **全部禁用**：禁用所有分类和日志

### 3. 运行时控制

#### 代码中的控制
```csharp
// 禁用Network分类
ASLogger.SetCategoryEnabled("Network", false);

// 禁用特定日志
ASLogger.SetLogEnabled("logId", false);

// 设置分类最小级别
ASLogger.SetCategoryMinLevel("Network", LogLevel.Warning);

// 检查分类状态
bool isEnabled = ASLogger.IsCategoryEnabled("Network");

// 获取统计信息
var stats = ASLogger.GetGlobalStatistics();
```

#### 编辑器窗口同步
- 编辑器窗口的修改会实时同步到运行时
- 运行时修改会反映在编辑器窗口中
- 支持动态修改配置，无需重启应用
- **刷新扫描**：重新扫描项目中的日志
- **清空日志**：清空当前显示的日志列表

### 3. 代码中使用

#### 基本用法（保持向后兼容）
```csharp
ASLogger.Instance.Info("用户登录成功");
ASLogger.Instance.Debug("网络连接建立");
ASLogger.Instance.Warning("房间已满");
ASLogger.Instance.Error("连接失败");
```

#### 带分类的用法
```csharp
// 网络相关日志
ASLogger.Instance.Info("网络连接成功", "Network.Connection");
ASLogger.Instance.Debug("发送消息", "Network.Message");
ASLogger.Instance.Error("网络错误", "Network.Error");

// UI相关日志
ASLogger.Instance.Info("UI初始化完成", "UI.Initialization");
ASLogger.Instance.Debug("按钮点击", "UI.Interaction");
ASLogger.Instance.Warning("UI警告", "UI.Warning");

// 游戏逻辑相关日志
ASLogger.Instance.Info("玩家移动", "GameLogic.Player.Movement");
ASLogger.Instance.Debug("技能释放", "GameLogic.Player.Skill");
ASLogger.Instance.Info("房间状态更新", "GameLogic.Room.Update");
```

#### 带ID的用法（用于精确控制）
```csharp
ASLogger.Instance.Info("用户登录成功", "Authentication", "login_success");
ASLogger.Instance.Debug("发送消息", "Network.Message", "send_message");
ASLogger.Instance.Error("连接失败", "Network.Error", "connection_failed");
```

### 4. 分类命名建议

#### 按模块分类
- `Network` - 网络相关
- `UI` - 用户界面相关
- `GameLogic` - 游戏逻辑相关
- `Audio` - 音频相关
- `Input` - 输入相关

#### 按功能分类
- `Authentication` - 身份验证
- `RoomManagement` - 房间管理
- `PlayerMovement` - 玩家移动
- `SkillSystem` - 技能系统

#### 层级分类
- `Network.Connection` - 网络连接
- `Network.Message` - 网络消息
- `Network.Error` - 网络错误
- `UI.Login` - 登录界面
- `UI.RoomList` - 房间列表
- `UI.RoomDetail` - 房间详情

## 技术架构

### 文件结构

#### CommonBase程序集（运行时代码）
- `LogCategory.cs` - 日志分类数据结构
- `LogEntry.cs` - 日志条目数据结构
- `LogManagerConfig.cs` - 配置ScriptableObject
- `LogFilter.cs` - 日志过滤逻辑
- `ASLogger.cs` - 增强的日志系统

#### Editor程序集（编辑器代码）
- `ASLoggerManagerWindow.cs` - 主编辑器窗口
- `LogScanner.cs` - 日志扫描器
- `ASLoggerTest.cs` - 测试脚本

### 核心组件

#### LogCategory
- 管理分类的层级结构
- 统计分类下的日志数量
- 提供批量操作功能

#### LogEntry
- 存储日志的详细信息
- 生成稳定的唯一标识
- 支持消息内容清理和匹配

#### LogFilter
- 实现日志过滤逻辑
- 支持分类和单个日志的过滤
- 提供运行时控制接口

#### ASLogger
- 增强的日志记录功能
- 支持分类和ID参数
- 集成过滤系统

## 配置说明

### LogManagerConfig
- **scanPaths**：扫描路径列表
- **excludePaths**：排除路径列表
- **globalMinLevel**：全局最小日志级别
- **enableCategoryFilter**：是否启用分类过滤
- **enableLogFilter**：是否启用单个日志过滤

### 默认配置
- 扫描路径：`Assets/Script`
- 排除路径：`Assets/Script/Generated`、`Assets/Script/Editor`
- 全局最小级别：`Debug`
- 过滤功能：全部启用

## 测试功能

### ASLoggerTest组件
1. 在场景中创建空GameObject
2. 添加`ASLoggerTest`组件
3. 在Inspector中点击测试按钮

### 测试功能
- **运行一次性测试**：生成各种分类的测试日志
- **测试分类过滤**：测试不同分类的日志输出
- **测试日志级别**：测试不同级别的日志输出

## 注意事项

1. **向后兼容**：现有代码无需修改，继续使用原有API
2. **性能影响**：过滤检查在日志记录前进行，对性能影响最小
3. **配置持久化**：设置会自动保存到ScriptableObject
4. **代码重构**：日志标识基于消息内容，不依赖行号，支持代码重构
5. **程序集分离**：运行时代码在CommonBase，编辑器代码在Editor程序集

## 扩展功能

### 自定义日志处理器
可以创建自定义的ILogHandler实现，集成到ASLogger系统中。

### 自定义分类规则
可以修改LogScanner中的正则表达式，支持自定义的日志格式。

### 自定义过滤逻辑
可以扩展LogFilter类，实现更复杂的过滤规则。

## 故障排除

### 常见问题

1. **日志不显示**
   - 检查分类是否启用
   - 检查单个日志是否启用
   - 检查日志级别设置

2. **分类不显示**
   - 运行"刷新扫描"功能
   - 检查扫描路径设置
   - 检查排除路径设置

3. **设置不保存**
   - 确保LogManagerConfig已保存
   - 检查Unity的AssetDatabase刷新

### 调试建议

1. 使用ASLoggerTest组件进行测试
2. 查看Unity Console中的日志输出
3. 检查LogManagerConfig的Inspector面板
4. 使用Unity的Debug.Log输出调试信息

## UI改进说明

### 分类树优化
- **更大的点击区域**：每个分类项都有20px高度的点击区域
- **视觉层次**：使用缩进和分隔线清晰显示层级关系
- **选中状态**：选中的分类有蓝色背景和左侧指示器
- **状态反馈**：禁用的分类显示为灰色，启用的显示为正常颜色
- **统计信息**：右侧显示启用/总日志数量

### 日志列表优化
- **清晰的条目**：每个日志条目有固定的行高和分隔线
- **级别标签**：彩色背景的日志级别标签，易于识别
- **状态指示器**：右侧显示"启用"/"禁用"状态
- **详细信息**：文件路径、方法名等信息清晰显示
- **更大的复选框**：25px宽度的复选框，更容易点击

### 工具栏集成
- **分类树工具栏**：集成"全部启用"/"全部禁用"按钮
- **日志列表工具栏**：集成搜索、过滤、统计信息
- **底部工具栏**：保留常用操作，移除重复按钮

### 视觉改进
- **深色主题**：使用Unity Editor的深色主题，避免白色背景
- **颜色编码**：不同状态使用不同颜色区分，适配深色背景
- **分隔线**：使用细线分隔不同区域，颜色适配深色主题
- **对齐**：所有元素都精确对齐，看起来更专业
- **文字对比度**：白色文字在深色背景上提供良好的可读性

## 更新日志

### v1.2.0
- 重构架构，将运行时控制功能整合到ASLogger中
- 移除不必要的RuntimeLogController类
- 编辑器窗口可以直接通知ASLogger修改配置
- 添加静态配置管理方法，支持运行时动态控制
- 优化代码结构，减少依赖关系

### v1.1.1
- 修复深色主题适配问题，移除白色背景
- 优化文字颜色对比度，提升可读性
- 调整分隔线颜色，适配深色主题
- 统一使用Unity Editor深色主题风格

### v1.1.0
- 优化分类树UI，提供更大的点击区域
- 改进日志列表显示，添加状态指示器
- 集成工具栏，减少界面混乱
- 添加选中状态指示和视觉反馈
- 优化整体布局和视觉层次

### v1.0.0
- 初始版本发布
- 支持分类和单个日志的启用/禁用
- 提供直观的编辑器界面
- 实现稳定的日志标识系统
- 支持运行时动态控制
