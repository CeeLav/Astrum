## ADDED Requirements

### Requirement: 实体运行时检查器窗口

系统 SHALL 提供一个 Unity Editor 窗口，用于在运行时查看实体的组件数据、Capability 状态和消息日志。

#### Scenario: 打开检查器窗口

- **WHEN** 用户在 Unity Editor 菜单中选择 `Astrum/Editor 编辑器/Entity Runtime Inspector`
- **THEN** 打开一个新的编辑器窗口，标题为 "Entity Runtime Inspector"
- **AND** 窗口显示实体选择区域、组件数据区域、Capability 状态区域和消息日志区域

#### Scenario: 窗口布局

- **WHEN** 检查器窗口打开
- **THEN** 窗口分为四个主要区域：
  - 实体选择区域（顶部）
  - 组件数据区域（可滚动）
  - Capability 状态区域（可滚动）
  - 消息日志区域（可滚动，底部）

### Requirement: 实体选择功能

系统 SHALL 允许用户通过实体ID输入或从场景中选择实体。

#### Scenario: 通过ID选择实体

- **WHEN** 用户在实体ID输入框中输入有效的实体ID（如 "1001"）
- **AND** 点击"查找"按钮或按回车键
- **THEN** 系统从当前 World 中查找该实体
- **AND** 如果实体存在，显示实体的基本信息（名称、配置ID、创建时间等）
- **AND** 更新组件数据、Capability 状态和消息日志的显示
- **AND** 如果实体不存在，显示友好的错误提示（如 "实体不存在"）

#### Scenario: 实体不存在时的处理

- **WHEN** 用户输入不存在的实体ID
- **THEN** 显示错误提示："实体 {entityId} 不存在"
- **AND** 组件数据、Capability 状态和消息日志区域显示为空或提示"请选择实体"

#### Scenario: 无效输入处理

- **WHEN** 用户输入非数字的实体ID（如 "abc"）
- **THEN** 显示错误提示："请输入有效的实体ID"
- **AND** 不执行查找操作

### Requirement: 组件数据展示

系统 SHALL 以可读格式展示实体上所有组件的字段值。

#### Scenario: 显示组件列表

- **WHEN** 用户选择了有效的实体
- **THEN** 系统列出该实体上挂载的所有组件
- **AND** 每个组件显示为可折叠的标题栏，显示组件类型名称
- **AND** 点击标题栏可以展开/折叠组件的字段详情

#### Scenario: 显示组件字段

- **WHEN** 用户展开某个组件
- **THEN** 显示该组件的所有公共字段和属性
- **AND** 每个字段显示为 "字段名: 值" 的格式
- **AND** 复杂类型（如 Vector3、Dictionary）以可读格式显示

#### Scenario: 组件数据为空

- **WHEN** 实体上没有任何组件
- **THEN** 组件数据区域显示提示："该实体没有挂载任何组件"

### Requirement: Capability 状态展示

系统 SHALL 显示实体上所有 Capability 的激活状态、优先级、标签和持续时间信息。

#### Scenario: 显示 Capability 列表

- **WHEN** 用户选择了有效的实体
- **THEN** 系统列出该实体上所有已注册的 Capability
- **AND** 每个 Capability 显示为可折叠的标题栏，显示 Capability 类型名称
- **AND** 标题栏显示激活状态（激活/禁用）的视觉指示（如颜色或图标）

#### Scenario: 显示 Capability 详细信息

- **WHEN** 用户展开某个 Capability
- **THEN** 显示以下信息：
  - 激活状态（IsActive: true/false）
  - 优先级（Priority）
  - 标签列表（Tags）
  - 激活持续时间（ActiveDuration，如果 TrackActiveDuration 为 true）
  - 禁用持续时间（DeactiveDuration，如果 TrackDeactiveDuration 为 true）

#### Scenario: 显示被禁用的 Tag

- **WHEN** 用户选择了有效的实体
- **AND** 该实体的 DisabledTags 字典不为空
- **THEN** 在 Capability 状态区域下方显示"被禁用的 Tag"部分
- **AND** 列出所有被禁用的 Tag
- **AND** 对于每个被禁用的 Tag，显示禁用发起者的实体ID列表（如 "UserInputMovement (禁用者: 1001, 1002)"）

#### Scenario: 无禁用 Tag 时的显示

- **WHEN** 实体的 DisabledTags 字典为空或不存在
- **THEN** 显示提示："被禁用的 Tag: 无"

#### Scenario: Capability 未激活

- **WHEN** 某个 Capability 的 IsActive 为 false
- **THEN** 该 Capability 的标题栏显示"禁用"状态
- **AND** 显示禁用持续时间（如果 TrackDeactiveDuration 为 true）

#### Scenario: Capability 不存在

- **WHEN** 实体上没有任何 Capability
- **THEN** Capability 状态区域显示提示："该实体没有注册任何 Capability"

### Requirement: 消息日志展示

系统 SHALL 实时显示发送给该实体的逻辑层消息（EntityEvent）和视图层消息（ViewEvent）。

#### Scenario: 显示逻辑层消息列表

- **WHEN** 用户选择了有效的实体
- **AND** 该实体的 EventQueue 中有消息
- **THEN** 消息日志区域显示"逻辑层消息队列"部分
- **AND** 列出所有逻辑层消息
- **AND** 每条消息显示帧号（Frame）、消息类型和消息内容
- **AND** 消息按帧号顺序排列（最新的在底部）

#### Scenario: 显示视图层消息列表

- **WHEN** 用户选择了有效的实体
- **AND** 该实体的 ViewEventQueue 中有消息
- **THEN** 消息日志区域显示"视图层消息队列"部分
- **AND** 列出所有视图层消息
- **AND** 每条消息显示时间戳、消息类型和消息内容
- **AND** 消息按时间顺序排列（最新的在底部）

#### Scenario: 逻辑层消息格式

- **WHEN** 显示逻辑层消息
- **THEN** 每条消息显示为以下格式：
  - 帧号（如 "[Frame: 100]"）
  - 消息类型名称（如 "HitEvent"）
  - 消息内容摘要（如 "TargetId=2001, Damage=100"）

#### Scenario: 视图层消息格式

- **WHEN** 显示视图层消息
- **THEN** 每条消息显示为以下格式：
  - 时间戳（如 "[20:00:05.123]"）
  - 消息类型名称（如 "VFXTriggerEvent"）
  - 消息内容摘要（如 "ActionId=1001"）

#### Scenario: 消息日志限制

- **WHEN** 消息日志超过 100 条
- **THEN** 自动移除最旧的消息，保持最多 100 条
- **AND** 提供滚动条以便查看历史消息

#### Scenario: 清空消息日志

- **WHEN** 用户点击"清空日志"按钮
- **THEN** 清空当前显示的消息日志
- **AND** 不影响实体的 ViewEventQueue（只清空显示，不清空队列）

#### Scenario: 无消息时的显示

- **WHEN** 实体的 EventQueue 和 ViewEventQueue 都为空或不存在
- **THEN** 逻辑层消息队列区域显示提示："暂无消息"
- **AND** 视图层消息队列区域显示提示："暂无消息"

### Requirement: 自动刷新功能

系统 SHALL 支持自动刷新实体信息，可配置刷新间隔。

#### Scenario: 启用自动刷新

- **WHEN** 用户勾选"自动刷新"复选框
- **THEN** 系统按照配置的刷新间隔（默认 0.5 秒）自动更新显示
- **AND** 每次刷新时重新获取实体数据并更新所有区域

#### Scenario: 禁用自动刷新

- **WHEN** 用户取消勾选"自动刷新"复选框
- **THEN** 停止自动刷新
- **AND** 用户可以通过点击"刷新"按钮手动刷新

#### Scenario: 配置刷新间隔

- **WHEN** 用户在刷新间隔输入框中输入新的间隔值（如 "1.0"）
- **THEN** 系统使用新的刷新间隔进行自动刷新
- **AND** 刷新间隔最小值为 0.1 秒，最大值为 5.0 秒

### Requirement: 手动刷新功能

系统 SHALL 提供手动刷新按钮，允许用户随时更新显示。

#### Scenario: 手动刷新

- **WHEN** 用户点击"刷新"按钮
- **THEN** 立即重新获取当前选中实体的数据
- **AND** 更新组件数据、Capability 状态和消息日志的显示
- **AND** 如果实体已被销毁，显示错误提示："实体已被销毁"

### Requirement: 运行时访问检查

系统 SHALL 在访问运行时数据时进行安全检查，处理各种异常情况。

#### Scenario: World 不可用

- **WHEN** 系统尝试访问 World 但 World 不存在或未初始化
- **THEN** 显示友好提示："World 未初始化，请先运行游戏"
- **AND** 所有数据区域显示为空或提示信息

#### Scenario: 实体被销毁

- **WHEN** 用户选择了实体后，该实体在运行时被销毁
- **THEN** 在下次刷新时显示错误提示："实体已被销毁"
- **AND** 清空所有数据区域的显示

#### Scenario: 访问异常处理

- **WHEN** 在访问实体数据时发生异常（如空引用、类型转换错误等）
- **THEN** 捕获异常并显示错误提示："访问实体数据时发生错误: {异常信息}"
- **AND** 不导致 Editor 崩溃或窗口关闭

