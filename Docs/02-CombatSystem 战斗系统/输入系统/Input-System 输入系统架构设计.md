# Input-System 输入系统架构设计

## 文档信息

- **创建日期**：2025-11-09
- **版本**：v1.0
- **状态**：设计阶段

## 一、背景与问题分析

### 1.1 当前问题

当前 `InputManager` 存在以下问题：

1. **职责混乱**：同时负责原始输入采集、LSInput组装、相机方向转换等多个职责
2. **硬编码严重**：按键映射、LSInput字段、ActionCommand映射都硬编码在代码中
3. **扩展性差**：添加新输入需要修改多处代码（proto、InputManager、ActionCapability）
4. **未使用代码**：`InputState`、事件系统、输入历史等功能完全未使用
5. **缺少抽象**：没有逻辑动作层的抽象，物理按键直接映射到LSInput

### 1.2 现有三层架构

当前输入系统实际上包含三个层次：

```
第一层：物理输入层 (KeyCode/Mouse)
    ↓ 硬编码在 InputManager.CollectLSInput()
第二层：帧同步输入层 (LSInput)
    ↓ 硬编码在 ActionCapability.SyncInputCommands()
第三层：动作指令层 (ActionCommand)
```

**数据流示例**：
- 玩家按下 `W` 键 → `LSInput.MoveY` 设置为正值 → 生成 `"move"` 命令 → 触发移动动作
- 玩家按下 `Space` 键 → `LSInput.Attack = true` → 生成 `"attack"` 和 `"normalattack"` 命令 → 触发攻击动作

## 二、设计目标

### 2.1 核心目标

1. **职责分离**：每层只负责单一职责，清晰的数据流
2. **配置驱动**：使用CSV配置表管理所有映射关系
3. **易于扩展**：添加新输入无需修改代码，只需修改配置表
4. **统一体系**：与项目现有的Luban配置表体系一致
5. **保持简单**：不引入Unity新InputSystem，保持老输入系统的简洁性

### 2.2 设计原则

- **单一职责**：每个类/层只做一件事
- **开闭原则**：对扩展开放，对修改关闭
- **依赖倒置**：依赖抽象接口，不依赖具体实现
- **配置优先**：优先使用配置表，避免硬编码

## 三、架构设计

### 3.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                    物理输入层 (Raw Input)                      │
│  - UnityEngine.Input.GetKey(KeyCode)                         │
│  - UnityEngine.Input.GetMouseButton()                        │
└─────────────────────┬───────────────────────────────────────┘
                      │ ConfigurableInputProvider
                      │ (读取 InputBindingTable.csv)
                      ↓
┌─────────────────────────────────────────────────────────────┐
│                   逻辑动作层 (Logical Action)                  │
│  - Attack, Skill1, Skill2                                    │
│  - MoveForward, MoveBackward, MoveLeft, MoveRight           │
└─────────────────────┬───────────────────────────────────────┘
                      │ LSInputAssembler
                      │ (读取 LSInputFieldMappingTable.csv)
                      │ + 相机方向转换
                      ↓
┌─────────────────────────────────────────────────────────────┐
│                 帧同步输入层 (LSInput - Proto)                 │
│  - MoveX, MoveY (Q31.32 定点数)                              │
│  - Attack, Skill1, Skill2 (bool)                            │
│  - PlayerId, Frame, Timestamp                               │
└─────────────────────┬───────────────────────────────────────┘
                      │ ActionCapability.SyncInputCommands()
                      │ (读取 ActionCommandMappingTable.csv)
                      ↓
┌─────────────────────────────────────────────────────────────┐
│                 动作指令层 (ActionCommand)                     │
│  - "move", "attack", "normalattack"                         │
│  - "skill1", "skill2"                                       │
│  - ValidFrames (有效帧数)                                     │
└─────────────────────┬───────────────────────────────────────┘
                      │ ActionCapability.SelectActionFromCandidates()
                      ↓
┌─────────────────────────────────────────────────────────────┐
│                    动作系统 (Action System)                    │
│  - ActionInfo.Commands 匹配                                  │
│  - 触发具体动作（移动、攻击、技能等）                            │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 层级职责

#### 第一层：物理输入层 → 逻辑动作层

**职责**：将物理按键/鼠标输入转换为逻辑动作

**配置表**：`InputBindingTable.csv`

**实现类**：
- `IRawInputProvider` - 输入提供者接口
- `ConfigurableInputProvider` - 基于配置表的实现
- `InputContextManager` - 输入上下文管理（可选）

**数据流**：
```
KeyCode.W → "MoveForward" 逻辑动作
KeyCode.Space → "Attack" 逻辑动作
MouseButton.Left → "Attack" 逻辑动作
```

#### 第二层：逻辑动作层 → 帧同步输入层

**职责**：将逻辑动作组装为标准化的LSInput，包含相机方向转换

**配置表**：`LSInputFieldMappingTable.csv`

**实现类**：
- `LSInputAssembler` - 静态组装器

**数据流**：
```
"MoveForward" + "MoveLeft" + 相机方向 
  → LSInput.MoveX/MoveY (定点数)

"Attack" 
  → LSInput.Attack = true
```

#### 第三层：帧同步输入层 → 动作指令层

**职责**：将LSInput转换为带有效期的ActionCommand

**配置表**：`ActionCommandMappingTable.csv`

**实现类**：
- `ActionCapability.SyncInputCommands()` - 修改为配置驱动

**数据流**：
```
LSInput.Attack = true 
  → ActionCommand("attack", validFrames=3)
  → ActionCommand("normalattack", validFrames=3)

LSInput.MoveX != 0 || MoveY != 0 
  → ActionCommand("move", validFrames=1)
```

## 四、CSV配置表设计

### 4.1 InputBindingTable.csv - 输入绑定配置表

**功能**：定义物理按键到逻辑动作的映射

**字段说明**：

| 字段名 | 类型 | 说明 | 示例 |
|--------|------|------|------|
| ActionId | string | 逻辑动作ID（唯一标识） | Attack, MoveForward |
| DisplayName | string | 显示名称（用于UI） | 攻击, 向前移动 |
| PrimaryKey | string | 主按键（KeyCode名称） | Space, W |
| AlternativeKey | string | 备用按键 | None, UpArrow |
| MouseButton | int | 鼠标按键（-1=不使用, 0=左键, 1=右键, 2=中键） | 0, -1 |
| InputType | string | 输入类型（Button/Axis） | Button, Axis |
| Category | string | 分类（Movement/Combat/UI） | Combat, Movement |

**示例数据**：见 `InputBindingTable.csv`

**使用场景**：
- 游戏设置界面的按键重映射
- 不同输入方案切换（如WASD vs 方向键）
- 多语言显示名称

### 4.2 LSInputFieldMappingTable.csv - LSInput字段映射表

**功能**：定义逻辑动作如何组装为LSInput字段

**字段说明**：

| 字段名 | 类型 | 说明 | 示例 |
|--------|------|------|------|
| LSInputField | string | LSInput字段名 | Attack, MoveX |
| MappingType | string | 映射类型（DirectButton/CompositeAxis/CustomLogic） | DirectButton |
| SourceActions | string | 源动作ID列表（逗号分隔） | Attack, MoveLeft,MoveRight |
| RequireNonZero | bool | 是否要求非零值 | false |
| Description | string | 说明 | 直接映射Attack按钮 |

**映射类型说明**：
- `DirectButton`：直接映射单个按钮到布尔字段
- `CompositeAxis`：组合多个按钮为轴值（如WASD组合为移动向量）
- `CustomLogic`：需要自定义逻辑处理（预留）

**示例数据**：见 `LSInputFieldMappingTable.csv`

### 4.3 ActionCommandMappingTable.csv - ActionCommand映射表

**功能**：定义LSInput如何转换为ActionCommand

**字段说明**：

| 字段名 | 类型 | 说明 | 示例 |
|--------|------|------|------|
| CommandName | string | 命令名称 | attack, move, skill1 |
| LSInputField | string | LSInput字段名（\|分隔表示OR关系） | Attack, MoveX\|MoveY |
| TriggerCondition | string | 触发条件（NonZero/True/Always） | True, NonZero |
| ValidFrames | int | 有效帧数 | 3, 1 |
| Priority | int | 优先级（用于冲突解决） | 10, 20 |
| Description | string | 说明 | 普通攻击 |

**触发条件说明**：
- `True`：布尔字段为true时触发
- `NonZero`：数值字段非零时触发（用于MoveX/MoveY）
- `Always`：总是触发（预留）

**示例数据**：见 `ActionCommandMappingTable.csv`

### 4.4 InputContextTable.csv - 输入上下文配置表（可选）

**功能**：定义不同场景下的输入模式（如战斗模式、UI模式、对话模式）

**字段说明**：

| 字段名 | 类型 | 说明 | 示例 |
|--------|------|------|------|
| ContextId | string | 上下文ID | Combat, UI, Dialogue |
| DisplayName | string | 显示名称 | 战斗模式, UI界面 |
| EnabledActions | string | 启用的动作列表（逗号分隔，空=全部） | Attack,Skill1,MoveForward |
| DisabledActions | string | 禁用的动作列表（\|分隔） | Attack\|Skill1 |
| Priority | int | 优先级（高优先级覆盖低优先级） | 0, 100 |
| BlockLowerContexts | bool | 是否阻断低优先级上下文 | false, true |

**使用场景**：
- 打开UI界面时禁用战斗输入
- 对话时只允许交互键
- 过场动画时禁用所有输入

**示例数据**：见 `InputContextTable.csv`

## 五、代码实现方案

### 5.1 核心接口定义

#### IRawInputProvider - 输入提供者接口

```csharp
namespace Astrum.Client.Input
{
    /// <summary>
    /// 原始输入提供者接口
    /// </summary>
    public interface IRawInputProvider
    {
        /// <summary>
        /// 获取按钮状态
        /// </summary>
        /// <param name="actionId">逻辑动作ID</param>
        /// <returns>是否按下</returns>
        bool GetButton(string actionId);
        
        /// <summary>
        /// 获取轴值
        /// </summary>
        /// <param name="axisId">轴ID</param>
        /// <returns>轴值 (-1.0 ~ 1.0)</returns>
        float GetAxis(string axisId);
        
        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        Vector2 GetMousePosition();
        
        /// <summary>
        /// 设置启用的动作列表（用于输入上下文）
        /// </summary>
        void SetEnabledActions(HashSet<string> enabledActions);
    }
}
```

#### ConfigurableInputProvider - 配置驱动的实现

```csharp
namespace Astrum.Client.Input
{
    /// <summary>
    /// 基于配置表的输入提供者
    /// </summary>
    public class ConfigurableInputProvider : IRawInputProvider
    {
        private Dictionary<string, InputBindingTable> _bindings;
        private HashSet<string> _enabledActions;
        
        public ConfigurableInputProvider()
        {
            LoadBindings();
            _enabledActions = null; // null表示全部启用
        }
        
        private void LoadBindings()
        {
            _bindings = new Dictionary<string, InputBindingTable>();
            
            // 从配置表加载
            var tables = ConfigManager.Instance.Tables;
            foreach (var binding in tables.InputBindingTable.DataList)
            {
                _bindings[binding.ActionId] = binding;
            }
        }
        
        public bool GetButton(string actionId)
        {
            // 检查是否启用
            if (_enabledActions != null && !_enabledActions.Contains(actionId))
                return false;
                
            if (!_bindings.TryGetValue(actionId, out var binding))
                return false;
            
            // 检查主按键
            if (!string.IsNullOrEmpty(binding.PrimaryKey) && binding.PrimaryKey != "None")
            {
                if (Enum.TryParse<KeyCode>(binding.PrimaryKey, out var key))
                {
                    if (UnityEngine.Input.GetKey(key))
                        return true;
                }
            }
            
            // 检查备用按键
            if (!string.IsNullOrEmpty(binding.AlternativeKey) && binding.AlternativeKey != "None")
            {
                if (Enum.TryParse<KeyCode>(binding.AlternativeKey, out var key))
                {
                    if (UnityEngine.Input.GetKey(key))
                        return true;
                }
            }
            
            // 检查鼠标按键
            if (binding.MouseButton >= 0)
            {
                if (UnityEngine.Input.GetMouseButton(binding.MouseButton))
                    return true;
            }
            
            return false;
        }
        
        public float GetAxis(string axisId)
        {
            float value = 0f;
            
            // 根据轴ID查找对应的正负按键
            if (axisId == "MoveHorizontal")
            {
                if (GetButton("MoveLeft")) value -= 1f;
                if (GetButton("MoveRight")) value += 1f;
            }
            else if (axisId == "MoveVertical")
            {
                if (GetButton("MoveBackward")) value -= 1f;
                if (GetButton("MoveForward")) value += 1f;
            }
            
            return Mathf.Clamp(value, -1f, 1f);
        }
        
        public Vector2 GetMousePosition()
        {
            return UnityEngine.Input.mousePosition;
        }
        
        public void SetEnabledActions(HashSet<string> enabledActions)
        {
            _enabledActions = enabledActions;
        }
    }
}
```

### 5.2 LSInputAssembler - LSInput组装器

```csharp
namespace Astrum.Client.Input
{
    /// <summary>
    /// LSInput组装器 - 将逻辑动作转换为帧同步输入
    /// </summary>
    public static class LSInputAssembler
    {
        private static Dictionary<string, LSInputFieldMappingTable> _fieldMappings;
        private static bool _initialized = false;
        
        /// <summary>
        /// 初始化（加载配置表）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            _fieldMappings = new Dictionary<string, LSInputFieldMappingTable>();
            var tables = ConfigManager.Instance.Tables;
            
            foreach (var mapping in tables.LSInputFieldMappingTable.DataList)
            {
                _fieldMappings[mapping.LSInputField] = mapping;
            }
            
            _initialized = true;
        }
        
        /// <summary>
        /// 从原始输入组装LSInput
        /// </summary>
        /// <param name="provider">输入提供者</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="camera">相机（用于方向转换）</param>
        /// <returns>组装好的LSInput</returns>
        public static LSInput AssembleFromRawInput(
            IRawInputProvider provider,
            long playerId,
            Camera camera = null)
        {
            if (!_initialized) Initialize();
            
            var input = new LSInput { PlayerId = playerId };
            
            // 处理移动输入（需要相机方向转换）
            AssembleMovement(input, provider, camera);
            
            // 处理按钮输入
            AssembleButtons(input, provider);
            
            return input;
        }
        
        /// <summary>
        /// 组装移动输入
        /// </summary>
        private static void AssembleMovement(LSInput input, IRawInputProvider provider, Camera camera)
        {
            // 获取原始轴输入
            float horizontal = provider.GetAxis("MoveHorizontal");
            float vertical = provider.GetAxis("MoveVertical");
            
            // 如果没有输入，直接返回
            if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
            {
                input.MoveX = 0;
                input.MoveY = 0;
                return;
            }
            
            // 归一化输入
            Vector2 rawInput = new Vector2(horizontal, vertical);
            if (rawInput.magnitude > 1f)
                rawInput.Normalize();
            
            // 转换到世界空间（考虑相机方向）
            Vector2 worldMove = TransformToWorldSpace(rawInput, camera);
            
            // 转换为定点数
            input.MoveX = ToFixedPoint(worldMove.x);
            input.MoveY = ToFixedPoint(worldMove.y);
        }
        
        /// <summary>
        /// 组装按钮输入
        /// </summary>
        private static void AssembleButtons(LSInput input, IRawInputProvider provider)
        {
            // 根据配置表映射
            foreach (var kvp in _fieldMappings)
            {
                string fieldName = kvp.Key;
                var mapping = kvp.Value;
                
                if (mapping.MappingType == "DirectButton")
                {
                    bool value = provider.GetButton(mapping.SourceActions);
                    SetBoolField(input, fieldName, value);
                }
            }
        }
        
        /// <summary>
        /// 转换到世界空间（考虑相机方向）
        /// </summary>
        private static Vector2 TransformToWorldSpace(Vector2 input, Camera cam)
        {
            if (cam == null)
                return input;
            
            // 获取相机方向（忽略Y轴）
            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            // 计算世界空间移动向量
            Vector3 worldMove = forward * input.y + right * input.x;
            
            return new Vector2(worldMove.x, worldMove.z);
        }
        
        /// <summary>
        /// 转换为Q31.32定点数
        /// </summary>
        private static long ToFixedPoint(float value)
        {
            return (long)(value * (double)(1L << 32));
        }
        
        /// <summary>
        /// 设置布尔字段
        /// </summary>
        private static void SetBoolField(LSInput input, string fieldName, bool value)
        {
            switch (fieldName)
            {
                case "Attack": input.Attack = value; break;
                case "Skill1": input.Skill1 = value; break;
                case "Skill2": input.Skill2 = value; break;
                // 扩展新字段时在此添加
            }
        }
    }
}
```

### 5.3 InputManager 重构

```csharp
namespace Astrum.Client.Managers
{
    /// <summary>
    /// 输入管理器 - 负责调度输入采集和LSInput组装
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        private IRawInputProvider _inputProvider;
        
        /// <summary>
        /// 初始化输入管理器
        /// </summary>
        public void Initialize()
        {
            // 创建配置驱动的输入提供者
            _inputProvider = new ConfigurableInputProvider();
            
            // 初始化LSInput组装器
            LSInputAssembler.Initialize();
            
            ASLogger.Instance.Info("InputManager: 输入管理器初始化完成", "Input.Manager");
        }
        
        /// <summary>
        /// 更新输入管理器
        /// </summary>
        public void Update()
        {
            var currentGameMode = GameDirector.Instance?.CurrentGameMode;
            if (currentGameMode?.MainRoom == null)
            {
                return;
            }
            
            // 组装LSInput
            var input = LSInputAssembler.AssembleFromRawInput(
                _inputProvider,
                currentGameMode.PlayerId,
                CameraManager.Instance?.MainCamera
            );
            
            // 提交到帧同步系统
            currentGameMode.MainRoom.LSController.SetPlayerInput(currentGameMode.PlayerId, input);
        }
        
        /// <summary>
        /// 关闭输入管理器
        /// </summary>
        public void Shutdown()
        {
            ASLogger.Instance.Info("InputManager: 关闭输入管理器", "Input.Manager");
            _inputProvider = null;
        }
    }
}
```

### 5.4 ActionCapability 修改

```csharp
namespace Astrum.LogicCore.Capabilities
{
    public partial class ActionCapability
    {
        private List<ActionCommandMappingTable> _commandMappings;
        
        public override void OnActivate(Entity entity)
        {
            base.OnActivate(entity);
            LoadCommandMappings();
        }
        
        /// <summary>
        /// 加载命令映射配置
        /// </summary>
        private void LoadCommandMappings()
        {
            var tables = ConfigManager.Instance.Tables;
            _commandMappings = tables.ActionCommandMappingTable.DataList.ToList();
        }
        
        /// <summary>
        /// 同步输入命令（修改为配置驱动）
        /// </summary>
        private void SyncInputCommands(Entity entity, ActionComponent actionComponent)
        {
            if (actionComponent == null) return;
            
            var inputComponent = GetComponent<LSInputComponent>(entity);
            if (inputComponent == null || inputComponent.CurrentInput == null)
            {
                actionComponent.InputCommands.Clear();
                return;
            }
            
            var currentInput = inputComponent.CurrentInput;
            var commands = actionComponent.InputCommands ?? new List<ActionCommand>();
            actionComponent.InputCommands = commands;
            
            // 递减现有命令的剩余帧数，并移除已过期的命令
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                var cmd = commands[i];
                if (cmd == null)
                {
                    commands.RemoveAt(i);
                    continue;
                }
                
                if (cmd.ValidFrames > 0)
                    cmd.ValidFrames -= 1;
                    
                if (cmd.ValidFrames <= 0)
                    commands.RemoveAt(i);
            }
            
            // 根据配置表同步命令
            foreach (var mapping in _commandMappings)
            {
                if (ShouldAddCommand(currentInput, mapping))
                {
                    AddOrRefreshCommand(commands, mapping.CommandName, mapping.ValidFrames);
                }
            }
        }
        
        /// <summary>
        /// 判断是否应该添加命令
        /// </summary>
        private bool ShouldAddCommand(LSInput input, ActionCommandMappingTable mapping)
        {
            var fields = mapping.LSInputField.Split('|');
            
            foreach (var field in fields)
            {
                string fieldName = field.Trim();
                
                switch (mapping.TriggerCondition)
                {
                    case "True":
                        if (GetBoolFieldValue(input, fieldName))
                            return true;
                        break;
                        
                    case "NonZero":
                        if (IsFieldNonZero(input, fieldName))
                            return true;
                        break;
                        
                    case "Always":
                        return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取布尔字段值
        /// </summary>
        private bool GetBoolFieldValue(LSInput input, string fieldName)
        {
            switch (fieldName)
            {
                case "Attack": return input.Attack;
                case "Skill1": return input.Skill1;
                case "Skill2": return input.Skill2;
                default: return false;
            }
        }
        
        /// <summary>
        /// 判断字段是否非零
        /// </summary>
        private bool IsFieldNonZero(LSInput input, string fieldName)
        {
            switch (fieldName)
            {
                case "MoveX": return input.MoveX != 0;
                case "MoveY": return input.MoveY != 0;
                default: return false;
            }
        }
        
        // AddOrRefreshCommand 方法保持不变
        private static void AddOrRefreshCommand(List<ActionCommand> commands, string name, int validFrames)
        {
            if (validFrames <= 0 || commands == null)
                return;
            
            foreach (var cmd in commands)
            {
                if (cmd != null && string.Equals(cmd.CommandName, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (cmd.ValidFrames < validFrames)
                        cmd.ValidFrames = validFrames;
                    return;
                }
            }
            
            commands.Add(new ActionCommand(name, validFrames));
        }
    }
}
```

### 5.5 InputContextManager - 输入上下文管理器（可选）

```csharp
namespace Astrum.Client.Input
{
    /// <summary>
    /// 输入上下文管理器 - 管理不同场景下的输入模式
    /// </summary>
    public class InputContextManager : Singleton<InputContextManager>
    {
        private Stack<InputContextTable> _contextStack = new Stack<InputContextTable>();
        private HashSet<string> _currentEnabledActions = new HashSet<string>();
        private ConfigurableInputProvider _inputProvider;
        
        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(ConfigurableInputProvider inputProvider)
        {
            _inputProvider = inputProvider;
            RefreshEnabledActions();
        }
        
        /// <summary>
        /// 压入上下文
        /// </summary>
        public void PushContext(string contextId)
        {
            var tables = ConfigManager.Instance.Tables;
            var context = tables.InputContextTable.DataMap.GetValueOrDefault(contextId);
            
            if (context != null)
            {
                _contextStack.Push(context);
                RefreshEnabledActions();
                
                ASLogger.Instance.Debug($"InputContext: 压入上下文 {contextId}", "Input.Context");
            }
        }
        
        /// <summary>
        /// 弹出上下文
        /// </summary>
        public void PopContext()
        {
            if (_contextStack.Count > 0)
            {
                var context = _contextStack.Pop();
                RefreshEnabledActions();
                
                ASLogger.Instance.Debug($"InputContext: 弹出上下文 {context.ContextId}", "Input.Context");
            }
        }
        
        /// <summary>
        /// 清空上下文栈
        /// </summary>
        public void ClearContexts()
        {
            _contextStack.Clear();
            RefreshEnabledActions();
        }
        
        /// <summary>
        /// 刷新启用的动作列表
        /// </summary>
        private void RefreshEnabledActions()
        {
            _currentEnabledActions.Clear();
            
            if (_contextStack.Count == 0)
            {
                // 默认启用所有动作
                var tables = ConfigManager.Instance.Tables;
                foreach (var binding in tables.InputBindingTable.DataList)
                {
                    _currentEnabledActions.Add(binding.ActionId);
                }
            }
            else
            {
                // 从栈底到栈顶处理上下文
                var contexts = _contextStack.Reverse().ToList();
                
                foreach (var context in contexts)
                {
                    if (context.BlockLowerContexts)
                    {
                        _currentEnabledActions.Clear();
                    }
                    
                    // 添加启用的动作
                    if (!string.IsNullOrEmpty(context.EnabledActions))
                    {
                        foreach (var action in context.EnabledActions.Split(','))
                        {
                            _currentEnabledActions.Add(action.Trim());
                        }
                    }
                    
                    // 移除禁用的动作
                    if (!string.IsNullOrEmpty(context.DisabledActions))
                    {
                        foreach (var action in context.DisabledActions.Split('|'))
                        {
                            _currentEnabledActions.Remove(action.Trim());
                        }
                    }
                }
            }
            
            // 应用到输入提供者
            _inputProvider?.SetEnabledActions(_currentEnabledActions);
        }
    }
}
```

## 六、扩展性说明

### 6.1 添加新输入的流程

**场景1：添加新按键动作（如跳跃）**

1. 在 `InputBindingTable.csv` 添加一行：
   ```csv
   Jump,跳跃,Space,None,-1,Button,Movement
   ```

2. 修改 `gamemessages_C_2000.proto` 添加字段：
   ```protobuf
   message LSInput {
       // ... 现有字段 ...
       bool Jump = 10;  // 跳跃输入
   }
   ```

3. 运行协议生成工具重新生成代码

4. 在 `LSInputFieldMappingTable.csv` 添加映射：
   ```csv
   Jump,DirectButton,Jump,false,直接映射Jump按钮
   ```

5. 在 `ActionCommandMappingTable.csv` 添加命令映射：
   ```csv
   jump,Jump,True,2,5,跳跃
   ```

6. 在 `LSInputAssembler.SetBoolField()` 添加case：
   ```csharp
   case "Jump": input.Jump = value; break;
   ```

7. 在 `ActionCapability.GetBoolFieldValue()` 添加case：
   ```csharp
   case "Jump": return input.Jump;
   ```

8. 完成！无需修改其他代码

**场景2：修改现有按键绑定**

只需修改 `InputBindingTable.csv`，无需修改任何代码：
```csv
# 将攻击从Space改为鼠标左键
Attack,攻击,None,None,0,Button,Combat
```

**场景3：添加新输入上下文**

在 `InputContextTable.csv` 添加一行：
```csv
Shop,商店界面,"Interact",Attack|Skill1|Skill2|MoveForward|MoveBackward|MoveLeft|MoveRight,80,true
```

代码中使用：
```csharp
// 打开商店时
InputContextManager.Instance.PushContext("Shop");

// 关闭商店时
InputContextManager.Instance.PopContext();
```

### 6.2 支持新输入设备

**添加手柄支持**：

1. 创建 `GamepadInputProvider : IRawInputProvider`
2. 在 `InputBindingTable.csv` 添加手柄按键配置
3. 在 `InputManager` 中切换Provider

**添加触屏支持**：

1. 创建 `TouchInputProvider : IRawInputProvider`
2. 实现虚拟摇杆逻辑
3. 无需修改LSInput和ActionCommand层

### 6.3 运行时按键重映射

```csharp
// 修改配置表数据
var tables = ConfigManager.Instance.Tables;
var attackBinding = tables.InputBindingTable.DataMap["Attack"];
attackBinding.PrimaryKey = "Mouse0";  // 改为鼠标左键

// 重新加载InputProvider
_inputProvider = new ConfigurableInputProvider();
```

## 七、实施步骤

### 阶段1：准备工作（1天）

1. 创建CSV配置表文件
2. 配置Luban生成规则
3. 生成配置表代码

### 阶段2：核心实现（2-3天）

1. 实现 `IRawInputProvider` 接口
2. 实现 `ConfigurableInputProvider`
3. 实现 `LSInputAssembler`
4. 重构 `InputManager`

### 阶段3：集成测试（1-2天）

1. 修改 `ActionCapability.SyncInputCommands()`
2. 测试现有输入功能
3. 验证配置表驱动

### 阶段4：扩展功能（可选，1-2天）

1. 实现 `InputContextManager`
2. 添加运行时按键重映射UI
3. 添加输入调试工具

### 阶段5：文档和优化（1天）

1. 编写使用文档
2. 性能优化
3. 代码审查

**总计**：5-9天

## 八、注意事项

### 8.1 性能考虑

1. **配置表缓存**：配置表在初始化时加载一次，运行时不重复加载
2. **字典查找**：使用Dictionary进行O(1)查找，避免遍历
3. **字符串比较**：使用switch而非反射，减少性能开销
4. **避免GC**：复用LSInput对象，避免每帧new

### 8.2 兼容性

1. **保持proto不变**：现有LSInput字段保持不变，确保网络兼容
2. **渐进式迁移**：可以先迁移InputManager，保持ActionCapability不变
3. **回退方案**：保留旧代码注释，便于回退

### 8.3 调试支持

建议添加调试工具：

```csharp
// 输入调试器
public class InputDebugger
{
    public static void LogCurrentInput(IRawInputProvider provider)
    {
        var tables = ConfigManager.Instance.Tables;
        foreach (var binding in tables.InputBindingTable.DataList)
        {
            bool pressed = provider.GetButton(binding.ActionId);
            if (pressed)
            {
                Debug.Log($"[Input] {binding.ActionId} ({binding.DisplayName}) is pressed");
            }
        }
    }
}
```

### 8.4 版本管理

1. **CSV文件**：纳入Git版本管理，便于追踪修改
2. **配置迁移**：如果修改表结构，提供迁移脚本
3. **向后兼容**：新增字段使用默认值，保持旧配置可用

## 九、未来扩展方向

### 9.1 短期扩展

1. **输入录制与回放**：记录LSInput序列，用于测试和演示
2. **输入宏**：支持组合键和连招
3. **输入提示UI**：根据配置表动态显示按键提示

### 9.2 长期扩展

1. **AI输入模拟**：实现AIInputProvider，用于机器人测试
2. **网络输入同步**：支持观战者输入同步
3. **输入分析**：统计玩家输入习惯，优化游戏设计

## 十、参考资料

### 10.1 相关文档

- [Action-System 动作系统](../技能系统/Action-System%20动作系统.md)
- [Luban CSV框架使用指南](../../../AstrumConfig/Doc/Luban_CSV框架使用指南.md)
- [帧同步系统设计](../../03-NetworkSystem%20网络系统/帧同步系统设计.md)（如果存在）

### 10.2 配置表文件

- `AstrumConfig/Tables/Datas/Input/InputBindingTable.csv`
- `AstrumConfig/Tables/Datas/Input/LSInputFieldMappingTable.csv`
- `AstrumConfig/Tables/Datas/Input/ActionCommandMappingTable.csv`
- `AstrumConfig/Tables/Datas/Input/InputContextTable.csv`

### 10.3 代码文件

- `AstrumProj/Assets/Script/AstrumClient/Managers/InputManager.cs`
- `AstrumProj/Assets/Script/AstrumClient/Input/IRawInputProvider.cs`（新建）
- `AstrumProj/Assets/Script/AstrumClient/Input/ConfigurableInputProvider.cs`（新建）
- `AstrumProj/Assets/Script/AstrumClient/Input/LSInputAssembler.cs`（新建）
- `AstrumProj/Assets/Script/AstrumLogic/Capabilities/ActionCapability.cs`（修改）

---

**文档状态**：✅ 设计完成，待实施

**下一步**：创建CSV配置表文件并配置Luban生成规则

