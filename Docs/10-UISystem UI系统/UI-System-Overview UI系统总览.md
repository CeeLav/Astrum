# UI系统总览

> 📖 **版本**: v1.0 | **最后更新**: 2025-10-11

## 概述

Astrum项目的UI系统是一个基于Unity UGUI的模块化UI框架，采用自动化代码生成和分离式设计，实现了UI视图与业务逻辑的清晰分离。

## 核心特性

- ✅ **自动化代码生成** - 从Prefab自动生成C#代码
- ✅ **Partial类分离** - Designer类（自动生成）与Logic类（手动编写）分离
- ✅ **UIRefs组件** - 自动管理UI元素引用
- ✅ **统一管理** - UIManager负责UI生命周期管理
- ✅ **缓存机制** - 已创建的UI自动缓存，避免重复加载
- ✅ **编辑器工具** - 集成的UI Generator编辑器工具

## 架构设计

### 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                      GameApplication                         │
│                     (应用程序主控制器)                          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ 管理
                     ▼
         ┌───────────────────────┐
         │      UIManager        │  ◄─── 统一的UI管理器
         │   (UI生命周期管理)      │
         └───────┬───────────────┘
                 │
                 │ 加载/缓存
                 ▼
    ┌────────────────────────────┐
    │      UI Prefab (Asset)      │  ◄─── Unity UI Prefab
    │    + UIRefs Component       │       (包含UIRefs组件)
    └────────────────────────────┘
                 │
                 │ 实例化
                 ▼
    ┌────────────────────────────┐
    │   UI Instance (Runtime)     │
    │    ┌──────────────────┐    │
    │    │   UIRefs         │◄───┼─── 运行时引用管理
    │    │  (引用收集)       │    │
    │    └────┬─────────────┘    │
    │         │ 初始化             │
    │         ▼                    │
    │    ┌──────────────────┐    │
    │    │  LoginView类      │◄───┼─── C# UI逻辑类
    │    │ (业务逻辑代码)     │    │
    │    └──────────────────┘    │
    └────────────────────────────┘
```

### 代码结构

```
UI代码采用Partial类分离设计：

LoginView.cs
├── LoginView.designer.cs    (自动生成，不可修改)
│   ├── UI References         - UI元素引用字段
│   ├── Initialize()          - 初始化方法
│   └── Show()/Hide()         - 基本显示/隐藏方法
│
└── LoginView.cs              (手动编写，业务逻辑)
    ├── OnInitialize()        - 初始化回调
    ├── OnShow()/OnHide()     - 显示/隐藏回调
    ├── Event Handlers        - 事件处理方法
    └── Business Logic        - 业务逻辑方法
```

## 核心组件

### 1. UIManager

**职责**: UI的统一管理器，负责UI的创建、显示、隐藏、销毁和缓存

**主要功能**:
- `ShowUI(string uiName)` - 显示UI（自动加载或从缓存获取）
- `HideUI(string uiName)` - 隐藏UI
- `DestroyUI(string uiName)` - 销毁UI并移除缓存
- `GetUI(string uiName)` - 获取UI GameObject
- `HasUI(string uiName)` - 检查UI是否已创建
- `IsUIVisible(string uiName)` - 检查UI是否显示

**路径**: `Assets/Script/AstrumClient/Managers/UIManager.cs`

**使用示例**:
```csharp
// 显示登录界面
UIManager.Instance.ShowUI("Login");

// 隐藏登录界面
UIManager.Instance.HideUI("Login");

// 检查是否显示
if (UIManager.Instance.IsUIVisible("Login"))
{
    // 已显示
}
```

### 2. UIRefs

**职责**: UI元素引用管理组件，自动收集子节点的UI组件引用

**主要功能**:
- 自动收集所有子节点的UI组件（Button、Text、Image等）
- 实例化对应的UI逻辑类
- 提供GetComponent方法获取UI元素引用
- 在Awake时自动初始化

**路径**: `Assets/Script/AstrumClient/UI/Core/UIRefs.cs`

**特点**:
- 每个UI Prefab的根节点都需要有UIRefs组件
- UIRefs在UI Generator生成代码时自动添加
- 使用路径字符串（如"Login/ButtonContainer/ConnectButton"）索引UI元素

### 3. UI Generator

**职责**: 编辑器工具，从Prefab自动生成UI代码

**主要功能**:
- 分析Prefab结构，提取所有UI组件
- 生成Partial类代码（Designer + Logic）
- 自动添加UIRefs组件到Prefab
- 支持增量更新（Logic类不会被覆盖）

**路径**: `Assets/Script/Editor/UIGenerator/`

**使用方式**: Unity编辑器菜单 `Tools > UI Generator`

### 4. UI逻辑类

**职责**: UI的业务逻辑实现

**特点**:
- 采用Partial类设计，分为Designer部分和Logic部分
- Designer部分自动生成，包含UI引用和基础方法
- Logic部分手动编写，包含业务逻辑
- 支持生命周期回调：OnInitialize、OnShow、OnHide

**路径**: `Assets/Script/AstrumClient/UI/Generated/`

## UI生命周期

```
┌──────────────┐
│  Prefab加载  │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ GameObject   │
│ 实例化        │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ UIRefs.Awake │ ◄── 自动执行
│ ├─ 收集引用   │
│ └─ 实例化UI类 │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Initialize   │ ◄── UIRefs调用
│ ├─ 初始化引用 │
│ └─ OnInitialize() │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Show()     │ ◄── UIManager调用
│ ├─ SetActive │
│ └─ OnShow()  │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  UI显示中     │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Hide()     │ ◄── UIManager调用
│ ├─ OnHide()  │
│ └─ SetActive │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Destroy()    │ ◄── UIManager调用（可选）
└──────────────┘
```

## 工作流程

### 开发流程

```
1. 设计UI
   └─► 在Unity中创建UI Prefab
        └─► 添加UI元素（Button、Text、Image等）

2. 生成代码
   └─► 使用UI Generator生成代码
        ├─► 生成 XXXView.designer.cs (自动生成)
        ├─► 生成 XXXView.cs (业务逻辑)
        └─► 添加 UIRefs 组件到Prefab

3. 编写逻辑
   └─► 在 XXXView.cs 中编写业务逻辑
        ├─► 实现 OnInitialize()
        ├─► 实现 OnShow() / OnHide()
        ├─► 编写事件处理方法
        └─► 编写业务逻辑方法

4. 运行时使用
   └─► 通过 UIManager 管理UI
        ├─► UIManager.ShowUI("XXX")
        ├─► UIManager.HideUI("XXX")
        └─► UIManager.DestroyUI("XXX")
```

### 更新流程

```
当Prefab结构发生变化时：

1. 修改Unity中的Prefab
   └─► 添加/删除/修改UI元素

2. 重新生成代码
   └─► 使用UI Generator重新生成
        ├─► Designer类会被覆盖更新
        ├─► Logic类不会被覆盖
        └─► UIRefs组件会被更新

3. 更新业务逻辑（如需要）
   └─► 在Logic类中使用新的UI元素
```

## 目录结构

```
AstrumProj/
├── Assets/
│   ├── ArtRes/
│   │   └── UI/                              # UI Prefab存放目录
│   │       ├── Login.prefab                 # 登录界面
│   │       ├── RoomList.prefab              # 房间列表
│   │       └── RoomDetail.prefab            # 房间详情
│   │
│   └── Script/
│       ├── AstrumClient/
│       │   ├── Managers/
│       │   │   └── UIManager.cs             # UI管理器
│       │   │
│       │   └── UI/
│       │       ├── Core/
│       │       │   ├── UIRefs.cs            # UI引用管理组件
│       │       │   └── UIPanel.cs           # UI面板基类（可选）
│       │       │
│       │       └── Generated/               # 生成的UI代码
│       │           ├── LoginView.cs         # 登录界面逻辑
│       │           ├── LoginView.designer.cs
│       │           ├── RoomListView.cs
│       │           ├── RoomListView.designer.cs
│       │           └── ...
│       │
│       └── Editor/
│           └── UIGenerator/                 # UI代码生成器
│               ├── Core/
│               │   ├── UIGenerator.cs       # 核心生成器
│               │   └── UIGeneratorConfig.cs # 配置
│               ├── Generators/
│               │   ├── CSharpCodeGenerator.cs
│               │   └── UIRefsGenerator.cs
│               ├── Windows/
│               │   └── UIGeneratorWindow.cs # 编辑器窗口
│               └── README.md                # UI Generator说明
```

## 配置说明

### UIGeneratorConfig

UI Generator的配置类，定义代码生成的各项设置：

```csharp
// Prefab搜索路径
public static readonly string PREFAB_SEARCH_PATH = "Assets/";

// 生成的代码输出路径
public static readonly string CODE_OUTPUT_PATH = "Assets/Script/AstrumClient/UI/Generated";

// 代码生成设置
public CodeGenerationSettings CodeSettings = new CodeGenerationSettings
{
    Namespace = "Astrum.Client.UI.Generated",     // 命名空间
    BaseClassName = "UIBase",                      // 基类名称（当前未使用）
    GenerateComments = true,                       // 生成注释
    GenerateRegions = true,                        // 生成Region
    UsePartialClass = true,                        // 使用Partial类分离
    DesignerFileSuffix = ".designer.cs",          // Designer文件后缀
    LogicFileSuffix = ".cs"                        // Logic文件后缀
};
```

### UIManager配置

```csharp
// UI Prefab路径
private string uiPrefabPath = "Assets/ArtRes/UI/";
```

## 支持的UI组件

UI Generator支持以下Unity UGUI组件的自动识别和引用生成：

- **基础组件**
  - `RectTransform` - 矩形变换
  - `Canvas` - 画布
  - `CanvasGroup` - 画布组

- **视觉组件**
  - `Image` - 图片
  - `RawImage` - 原始图片
  - `Text` - 文本（UGUI）
  - `TextMeshProUGUI` - 文本（TextMeshPro）

- **交互组件**
  - `Button` - 按钮
  - `Toggle` - 开关
  - `Slider` - 滑动条
  - `Scrollbar` - 滚动条
  - `Dropdown` - 下拉框
  - `InputField` - 输入框
  - `ScrollRect` - 滚动视图

- **布局组件**
  - `LayoutGroup` - 布局组
  - `GridLayoutGroup` - 网格布局
  - `HorizontalLayoutGroup` - 水平布局
  - `VerticalLayoutGroup` - 垂直布局

## 相关文档

- [UI创建指南](UI-Creation-Guide%20UI创建指南.md) - 如何创建UI Prefab和生成代码
- [UI编写指南](UI-Development-Guide%20UI编写指南.md) - 如何编写UI业务逻辑
- [UI运行时使用](UI-Runtime-Usage%20UI运行时使用.md) - UIManager的使用方法
- [UI开发规范](UI-Conventions%20UI开发规范.md) - 命名规范和最佳实践

## 技术优势

### 1. 自动化代码生成
- **减少手动工作**: 无需手动编写UI元素引用代码
- **减少错误**: 自动生成的代码经过验证，减少拼写错误
- **提高效率**: 快速从设计到代码实现

### 2. Partial类分离
- **保护业务逻辑**: Logic类不会被重新生成覆盖
- **清晰分离**: Designer类和Logic类职责明确
- **易于维护**: 业务逻辑与UI结构分离

### 3. UIRefs组件
- **自动化管理**: 无需手动拖拽引用
- **路径索引**: 使用字符串路径访问UI元素
- **类型安全**: GetComponent方法提供类型安全的访问

### 4. 统一管理
- **缓存机制**: 避免重复加载，提高性能
- **生命周期管理**: 统一的创建、显示、隐藏、销毁流程
- **易于扩展**: 可以方便地添加UI栈、历史记录等功能

## 注意事项

1. **Prefab命名**: Prefab文件名应与生成的类名一致
2. **UIRefs组件**: 每个UI Prefab根节点必须有UIRefs组件
3. **路径规范**: UI Prefab必须放在 `Assets/ArtRes/UI/` 目录下
4. **代码保护**: 不要修改 `.designer.cs` 文件，所有业务逻辑写在 `.cs` 文件中
5. **增量更新**: 重新生成代码时，Logic类不会被覆盖
6. **编辑器模式**: 在Editor模式下使用AssetDatabase加载，运行时使用Resources加载（需优化）

---

**版本历史**:
- v1.0 (2025-10-11) - 初始版本

