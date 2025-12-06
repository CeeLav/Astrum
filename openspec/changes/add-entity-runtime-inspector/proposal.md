# Change: 添加实体运行时检查器编辑器窗口

**状态**: 🟡 提案中

## Why

在开发和调试 ECC（Entity-Component-Capability）系统时，开发人员需要能够实时查看实体的状态信息，包括：

1. **组件数据可视化** - 查看实体上挂载的所有组件及其数据
2. **Capability 激活状态** - 了解哪些 Capability 当前处于激活状态，哪些被禁用
3. **消息日志** - 查看其他模块发送给该实体的消息（ViewEventQueue）

目前缺乏一个统一的运行时调试工具来查看这些信息，开发人员需要：
- 在代码中添加断点或日志
- 使用 Unity Inspector 查看序列化数据（但无法看到运行时状态）
- 手动遍历 World.Entities 字典查找实体

这导致调试效率低下，特别是在处理复杂的 ECC 交互时。

## What Changes

### 新增功能

- **ADDED**: `EntityRuntimeInspectorWindow` - 新的 Unity Editor 窗口，用于运行时查看实体信息
- **ADDED**: 实体选择机制 - 支持通过实体ID输入或从场景中选择实体
- **ADDED**: 组件数据展示 - 以可读格式展示实体上所有组件的字段值
- **ADDED**: Capability 状态展示 - 显示所有 Capability 的激活状态、优先级、标签等信息
- **ADDED**: 消息日志区 - 实时显示发送给该实体的 ViewEvent 消息

### 技术实现

- 使用 Unity `EditorWindow` 基类创建自定义编辑器窗口
- 运行时访问 `World.GetEntity()` 获取实体
- 使用反射或序列化机制展示组件数据
- 集成到 Unity Editor 菜单：`Astrum/Editor 编辑器/Entity Runtime Inspector`

## Impact

### 影响的规范

- `editor-tools` (新增) - 添加运行时实体检查器功能

### 影响的代码

**新增文件**:
- `AstrumProj/Assets/Script/Editor/EntityRuntimeInspector/EntityRuntimeInspectorWindow.cs` - 主窗口类
- `AstrumProj/Assets/Script/Editor/EntityRuntimeInspector/EntityInspectorModules.cs` - UI 模块（组件展示、Capability 展示、消息日志）

**修改文件**:
- 无（纯新增功能，不修改现有代码）

### 兼容性

- ✅ 完全向后兼容 - 纯新增功能
- ✅ 仅在 Editor 模式下可用 - 不影响运行时性能
- ✅ 可选功能 - 不影响现有工作流

### 风险

1. **性能影响** - 如果频繁刷新可能导致 Editor 卡顿
   - 缓解：使用节流刷新机制，限制更新频率
2. **反射开销** - 使用反射获取组件数据可能有性能开销
   - 缓解：缓存反射结果，仅在数据变化时更新
3. **运行时访问** - 需要确保在运行时能安全访问 World 和 Entity
   - 缓解：添加空值检查和错误处理

## Dependencies

- 无外部依赖
- 需要运行时 World 实例可用（通过 Room.MainWorld 或类似机制）

## Success Criteria

- [ ] 编辑器窗口可以正常打开和关闭
- [ ] 可以通过实体ID输入框选择实体
- [ ] 组件数据正确显示（所有字段值可读）
- [ ] Capability 激活状态正确显示（激活/禁用、优先级、标签）
- [ ] 消息日志实时更新（显示 ViewEventQueue 中的消息）
- [ ] 窗口在运行时自动刷新（可配置刷新频率）
- [ ] 实体不存在时显示友好错误提示
- [ ] 窗口布局清晰，信息易于阅读

## References

- Unity EditorWindow 文档：https://docs.unity3d.com/ScriptReference/EditorWindow.html
- 现有编辑器窗口参考：`AstrumProj/Assets/Script/Editor/RoleEditor/Windows/RoleEditorWindow.cs`
- ECC 架构文档：`Docs/05-CoreArchitecture 核心架构/ECC-System ECC结构说明.md`

