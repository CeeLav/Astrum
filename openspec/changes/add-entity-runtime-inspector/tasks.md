## 1. 项目结构搭建

- [x] 1.1 创建目录结构 `AstrumProj/Assets/Script/Editor/EntityRuntimeInspector/`
- [x] 1.2 创建主窗口类文件 `EntityRuntimeInspectorWindow.cs`
- [x] 1.3 创建 UI 模块文件 `EntityInspectorModules.cs`（可选，或直接在窗口类中实现）- 已直接在窗口类中实现

## 2. 基础窗口实现

- [x] 2.1 实现 `EntityRuntimeInspectorWindow` 类，继承 `EditorWindow`
- [x] 2.2 添加 `[MenuItem]` 属性，创建菜单项 `Astrum/Editor 编辑器/Entity Runtime Inspector`
- [x] 2.3 实现 `ShowWindow()` 静态方法
- [x] 2.4 实现 `OnEnable()` 和 `OnDisable()` 生命周期方法
- [x] 2.5 实现基础 `OnGUI()` 方法，显示窗口标题

## 3. 实体选择功能

- [x] 3.1 添加实体ID输入框 UI
- [x] 3.2 实现 `OnEntityIdInputChanged()` 方法，处理输入变化
- [x] 3.3 实现 `RefreshEntity()` 方法，从 World 获取实体
- [x] 3.4 实现 `GetWorld()` 方法，获取运行时 World 实例（通过 GameMode 或 Room）
- [x] 3.5 添加实体基本信息显示（名称、配置ID、创建时间）
- [x] 3.6 添加错误提示显示（实体不存在、无效输入等）

## 4. 组件数据展示模块

- [x] 4.1 实现 `DrawComponentView()` 方法
- [x] 4.2 遍历 `Entity.Components` 字典，显示所有组件
- [x] 4.3 实现组件折叠/展开功能（使用 `EditorGUILayout.Foldout`）
- [x] 4.4 实现反射缓存机制（`GetCachedFields()` 方法）
- [x] 4.5 实现字段值格式化显示（处理基本类型、Vector3、Dictionary 等）
- [x] 4.6 添加空组件提示（当实体没有组件时）

## 5. Capability 状态展示模块

- [x] 5.1 实现 `DrawCapabilityView()` 方法
- [x] 5.2 遍历 `Entity.CapabilityStates` 字典，显示所有 Capability
- [x] 5.3 从 `CapabilitySystem` 获取 Capability 实例（通过 TypeId，使用反射）
- [x] 5.4 实现 Capability 折叠/展开功能
- [x] 5.5 显示激活状态（IsActive），使用视觉指示（颜色或图标）
- [x] 5.6 显示优先级（Priority）
- [x] 5.7 显示标签列表（Tags）
- [x] 5.8 显示激活/禁用持续时间（ActiveDuration/DeactiveDuration）
- [x] 5.9 实现 `DrawDisabledTags()` 方法，显示被禁用的 Tag
- [x] 5.11 遍历 `Entity.DisabledTags` 字典，显示所有被禁用的 Tag
- [x] 5.12 对于每个被禁用的 Tag，显示禁用发起者的实体ID列表
- [x] 5.13 添加空禁用 Tag 提示（当 DisabledTags 为空时）
- [x] 5.14 添加空 Capability 提示（当实体没有 Capability 时）

## 6. 消息日志展示模块

- [x] 6.1 实现 `DrawMessageLog()` 方法
- [x] 6.2 实现 `UpdateMessageLog()` 方法，从 `Entity.EventQueue` 和 `Entity.ViewEventQueue` 复制消息
- [x] 6.3 实现逻辑层消息列表显示（使用 `EditorGUILayout.BeginScrollView`）
- [x] 6.4 实现逻辑层消息格式化显示（帧号 + 类型 + 内容）
- [x] 6.5 实现视图层消息列表显示（使用 `EditorGUILayout.BeginScrollView`）
- [x] 6.6 实现视图层消息格式化显示（时间戳 + 类型 + 内容）
- [x] 6.7 实现消息日志限制（最多 100 条，自动移除旧消息，分别限制逻辑层和视图层）
- [x] 6.8 添加"清空日志"按钮（清空两个队列的显示）
- [x] 6.9 添加无消息提示（当两个队列都为空时）

## 7. 刷新机制

- [x] 7.1 实现手动刷新按钮
- [x] 7.2 实现 `RefreshAll()` 方法，刷新所有数据区域
- [x] 7.3 实现自动刷新复选框
- [x] 7.4 实现刷新间隔输入框
- [x] 7.5 使用 `EditorApplication.update` 实现自动刷新回调
- [x] 7.6 实现刷新节流机制（避免过于频繁的刷新）
- [x] 7.7 在 `OnDisable()` 中取消自动刷新订阅

## 8. 错误处理和安全性

- [x] 8.1 在 `GetWorld()` 中添加空值检查
- [x] 8.2 在 `RefreshEntity()` 中添加异常处理（try-catch）
- [x] 8.3 在组件数据展示中添加异常处理
- [x] 8.4 在 Capability 状态展示中添加异常处理
- [x] 8.5 在消息日志展示中添加异常处理
- [x] 8.6 添加实体销毁检查（在刷新时检查 `Entity.IsDestroyed`）

## 9. UI 优化和美化

- [x] 9.1 优化窗口布局，使用合适的间距和分组
- [x] 9.2 添加工具栏区域（刷新按钮、自动刷新选项等）
- [x] 9.3 使用 `EditorStyles` 优化文本显示（标题、标签等）
- [x] 9.4 实现滚动视图，确保内容过多时可以滚动
- [x] 9.5 添加区域分隔线，提高可读性（使用 EditorGUILayout.Space）
- [x] 9.6 优化折叠/展开的视觉反馈

## 10. 测试和验证

- [ ] 10.1 测试窗口打开和关闭功能
- [ ] 10.2 测试实体ID输入功能（有效ID、无效ID、不存在ID）
- [ ] 10.3 测试组件数据展示（有组件、无组件、复杂类型）
- [ ] 10.4 测试 Capability 状态展示（激活、禁用、无 Capability）
- [ ] 10.5 测试禁用 Tag 展示（有禁用 Tag、无禁用 Tag）
- [ ] 10.6 测试逻辑层消息日志展示（有消息、无消息、消息过多）
- [ ] 10.7 测试视图层消息日志展示（有消息、无消息、消息过多）
- [ ] 10.6 测试自动刷新功能（启用、禁用、修改间隔）
- [ ] 10.7 测试手动刷新功能
- [ ] 10.8 测试错误处理（World 不可用、实体被销毁、访问异常）
- [ ] 10.9 测试长时间运行（确保无内存泄漏）

## 11. 文档和清理

- [x] 11.1 添加代码注释（XML 文档注释）
- [x] 11.2 检查代码风格是否符合项目规范
- [x] 11.3 移除调试日志和临时代码
- [x] 11.4 确保所有 TODO 注释已处理

