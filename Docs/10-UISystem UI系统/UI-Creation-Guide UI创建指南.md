# UI创建指南

> 📖 **版本**: v1.0 | **最后更新**: 2025-10-11

本文档介绍如何在Astrum项目中创建新的UI界面，包括在Unity中设计UI Prefab和使用UI Generator生成代码。

## 前置要求

- 熟悉Unity编辑器基本操作
- 了解Unity UGUI系统
- 已阅读 [UI系统总览](UI-System-Overview%20UI系统总览.md)

## 创建流程概览

```
1. 在Unity中创建UI Prefab
   └─► 设计UI布局和元素

2. 使用UI Generator生成代码
   └─► 自动生成C#代码和UIRefs组件

3. 编译并验证
   └─► 检查生成的代码是否正确

4. 编写业务逻辑
   └─► 在Logic类中实现UI功能

5. 运行时测试
   └─► 通过UIManager显示UI并测试
```

## 第一步：创建UI Prefab

### 1.1 创建Canvas（如果场景中没有）

如果你的场景中还没有Canvas，需要先创建一个：

1. 在Hierarchy窗口右键 → `UI` → `Canvas`
2. Unity会自动创建Canvas和EventSystem
3. 配置Canvas组件：
   - **Render Mode**: Screen Space - Overlay（屏幕空间覆盖）
   - **Canvas Scaler**: 根据需要配置UI缩放模式
   - **Graphic Raycaster**: 用于处理UI事件

### 1.2 创建UI根节点

1. 在Canvas下创建一个空GameObject作为UI的根节点
2. 右键Canvas → `Create Empty`
3. 命名为你的UI名称，例如：`Login`、`MainMenu`、`Settings`

**命名规范**:
- 使用PascalCase（大驼峰）命名
- 名称应简洁明了，表达UI的用途
- 避免使用特殊字符和空格

### 1.3 添加UI元素

在根节点下添加各种UI元素：

#### 常用UI元素

**Panel（面板）**:
```
右键根节点 → UI → Panel
用途：背景面板、容器
```

**Image（图片）**:
```
右键根节点 → UI → Image
用途：显示图片、图标、背景
```

**Text（文本）**:
```
右键根节点 → UI → Text - TextMeshPro
用途：显示文字内容
注意：推荐使用TextMeshPro而不是传统的Text
```

**Button（按钮）**:
```
右键根节点 → UI → Button - TextMeshPro
用途：可点击的按钮
自动包含：Image（背景）+ Text（文字）
```

**Input Field（输入框）**:
```
右键根节点 → UI → Input Field - TextMeshPro
用途：用户文本输入
```

**Slider（滑动条）**:
```
右键根节点 → UI → Slider
用途：调节数值（音量、亮度等）
```

**Toggle（开关）**:
```
右键根节点 → UI → Toggle
用途：开关选项
```

**Dropdown（下拉框）**:
```
右键根节点 → UI → Dropdown - TextMeshPro
用途：选择列表
```

**Scroll View（滚动视图）**:
```
右键根节点 → UI → Scroll View
用途：显示可滚动的内容列表
```

### 1.4 组织UI层级

合理的UI层级结构能提高可维护性：

```
Login (根节点)
├── Background (背景)
│   └── BackgroundImage
├── Header (头部区域)
│   ├── Logo
│   └── Title
├── Content (内容区域)
│   ├── UsernamePanel
│   │   ├── UsernameLabel
│   │   └── UsernameInput
│   └── PasswordPanel
│       ├── PasswordLabel
│       └── PasswordInput
├── Footer (底部区域)
│   ├── LoginButton
│   │   ├── ButtonBackground
│   │   └── ButtonText
│   └── RegisterButton
│       ├── ButtonBackground
│       └── ButtonText
└── StatusBar (状态栏)
    └── StatusText
```

**层级组织原则**:
1. **功能分组**: 相关的UI元素放在同一个容器下
2. **命名清晰**: 节点名称应表达其用途
3. **避免过深**: 层级深度建议不超过5层
4. **使用容器**: 使用空GameObject或Panel作为容器组织元素

### 1.5 配置UI元素属性

#### RectTransform设置

每个UI元素都有RectTransform组件，需要正确配置：

- **Anchors（锚点）**: 定义UI元素相对于父节点的对齐方式
- **Pivot（轴心）**: 定义UI元素的旋转和缩放中心
- **Position**: 相对于锚点的位置
- **Size**: UI元素的尺寸

**常用锚点设置**:
- 左上角：Anchors (0, 1)
- 居中：Anchors (0.5, 0.5)
- 拉伸填充：Anchors Min (0, 0), Max (1, 1)

#### 组件属性配置

**Image组件**:
- Source Image: 选择sprite图片
- Color: 设置颜色和透明度
- Material: 设置材质（如果需要）
- Raycast Target: 是否接收射线检测（影响性能）

**Text组件（TextMeshPro）**:
- Text: 默认文本内容
- Font Asset: 字体资源
- Font Size: 字体大小
- Color: 文字颜色
- Alignment: 对齐方式
- Wrapping: 自动换行

**Button组件**:
- Interactable: 是否可交互
- Transition: 状态过渡效果（Color Tint / Sprite Swap）
- Navigation: 导航设置

### 1.6 保存为Prefab

1. 将设计好的UI根节点从Hierarchy拖拽到Project窗口
2. 保存位置：`Assets/ArtRes/UI/`
3. 命名规范：与根节点名称一致，例如 `Login.prefab`

**注意事项**:
- 确保Prefab保存在正确的目录下
- Prefab名称与UI名称一致
- 保存后可以从场景中删除该实例

## 第二步：使用UI Generator生成代码

### 2.1 打开UI Generator

在Unity编辑器中：

1. 点击菜单 `Tools` → `UI Generator`
2. UI Generator窗口将会打开

### 2.2 选择Prefab

在UI Generator窗口中：

1. 点击 `选择Prefab` 按钮
2. 在弹出的文件选择对话框中选择你的UI Prefab
3. 路径示例：`Assets/ArtRes/UI/Login.prefab`

### 2.3 配置生成选项

UI Generator会自动读取Prefab信息并显示：

- **UI名称**: 默认为Prefab文件名（可修改）
- **命名空间**: `Astrum.Client.UI.Generated`（默认）
- **输出路径**: `Assets/Script/AstrumClient/UI/Generated`（默认）

**可选配置**:
- 是否生成注释（推荐开启）
- 是否使用Region分区（推荐开启）
- 是否使用Partial类（推荐开启）

### 2.4 生成代码

1. 检查配置信息是否正确
2. 点击 `生成UI代码` 按钮
3. 等待生成完成（通常只需几秒）
4. 查看Console窗口的生成日志

### 2.5 生成结果

生成成功后，会产生以下文件：

```
Assets/Script/AstrumClient/UI/Generated/
├── LoginView.designer.cs    (自动生成，不要修改)
└── LoginView.cs              (业务逻辑，可以修改)
```

同时，Prefab会被自动修改：
```
Login.prefab
└── 添加了 UIRefs 组件到根节点
```

## 第三步：验证生成的代码

### 3.1 检查Designer类

打开 `LoginView.designer.cs` 文件，检查：

```csharp
// <auto-generated>
// 此文件由UI生成器自动生成，请勿手动修改
// </auto-generated>

namespace Astrum.Client.UI.Generated
{
    public partial class LoginView
    {
        #region UI References
        
        // 所有UI元素的引用
        private Button loginButtonButton;
        private Text usernameInputText;
        // ...
        
        #endregion
        
        #region Initialization
        
        public void Initialize(UIRefs refs)
        {
            uiRefs = refs;
            InitializeUIElements();
            OnInitialize();
        }
        
        private void InitializeUIElements()
        {
            // 初始化所有UI元素引用
            loginButtonButton = uiRefs.GetComponent<Button>("Login/Footer/LoginButton");
            // ...
        }
        
        #endregion
        
        #region Basic Methods
        
        public virtual void Show() { ... }
        public virtual void Hide() { ... }
        
        #endregion
    }
}
```

**检查要点**:
- ✅ 所有UI元素是否都有对应的引用字段
- ✅ 引用路径是否正确
- ✅ 组件类型是否正确（Button、Text、Image等）
- ✅ 命名是否符合规范

### 3.2 检查Logic类

打开 `LoginView.cs` 文件，检查：

```csharp
// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

namespace Astrum.Client.UI.Generated
{
    public partial class LoginView
    {
        #region Virtual Methods
        
        protected virtual void OnInitialize()
        {
            // TODO: 在此处编写初始化逻辑
        }
        
        protected virtual void OnShow()
        {
            // TODO: 在此处编写显示时的逻辑
        }
        
        protected virtual void OnHide()
        {
            // TODO: 在此处编写隐藏时的逻辑
        }
        
        #endregion
    }
}
```

**检查要点**:
- ✅ 文件已创建
- ✅ 包含生命周期回调方法
- ✅ Partial类声明正确

### 3.3 检查UIRefs组件

在Unity中打开Prefab，检查：

1. 根节点是否有 `UIRefs` 组件
2. UIRefs组件的配置：
   - UI Class Name: `LoginView`
   - UI Namespace: `Astrum.Client.UI.Generated`
   - UI Ref Items: 包含所有UI元素的引用信息

### 3.4 编译检查

在Unity编辑器中：

1. 等待Unity自动编译
2. 检查Console窗口是否有编译错误
3. 如果有错误，根据错误信息修复

**常见编译错误**:
- 命名空间错误：检查using语句
- 类型不匹配：检查UI元素类型
- 引用缺失：重新生成代码

## 第四步：更新Prefab结构后的重新生成

当你修改了UI Prefab的结构（添加/删除/修改UI元素）后，需要重新生成代码：

### 4.1 修改Prefab

1. 在Unity中打开Prefab进行编辑
2. 添加、删除或修改UI元素
3. 保存Prefab

### 4.2 重新生成代码

1. 打开UI Generator
2. 选择修改后的Prefab
3. 点击 `生成UI代码`
4. UI Generator会：
   - ✅ 覆盖更新 `LoginView.designer.cs`
   - ✅ **保留** `LoginView.cs` 中的业务逻辑
   - ✅ 更新Prefab上的UIRefs组件

### 4.3 更新业务逻辑

在 `LoginView.cs` 中更新对新UI元素的引用和逻辑。

## 完整示例：创建一个登录界面

### 示例1：简单登录界面

#### Unity中的UI结构

```
Login (RectTransform)
├── Background (Image)
│   └── Color: 半透明黑色
├── Panel (Panel)
│   ├── Title (TextMeshProUGUI)
│   │   └── Text: "欢迎登录"
│   ├── UsernameInput (TMP_InputField)
│   │   └── Placeholder: "请输入用户名"
│   ├── PasswordInput (TMP_InputField)
│   │   └── Placeholder: "请输入密码"
│   └── LoginButton (Button)
│       └── Text: "登录"
└── StatusText (TextMeshProUGUI)
    └── Text: "未连接"
```

#### 生成的Designer类（部分）

```csharp
public partial class LoginView
{
    #region UI References
    
    private Image backgroundImage;
    private Text titleText;
    private TMP_InputField usernameInputInputField;
    private TMP_InputField passwordInputInputField;
    private Button loginButtonButton;
    private Text statusTextText;
    
    #endregion
    
    private void InitializeUIElements()
    {
        backgroundImage = uiRefs.GetComponent<Image>("Login/Background");
        titleText = uiRefs.GetComponent<Text>("Login/Panel/Title");
        usernameInputInputField = uiRefs.GetComponent<TMP_InputField>("Login/Panel/UsernameInput");
        passwordInputInputField = uiRefs.GetComponent<TMP_InputField>("Login/Panel/PasswordInput");
        loginButtonButton = uiRefs.GetComponent<Button>("Login/Panel/LoginButton");
        statusTextText = uiRefs.GetComponent<Text>("Login/StatusText");
    }
}
```

## 最佳实践

### UI设计最佳实践

1. **保持层级简洁**
   - 避免过深的嵌套
   - 使用容器组织相关元素
   - 合理使用Layout Group

2. **命名规范**
   - 使用描述性的名称
   - 遵循PascalCase命名
   - 避免使用特殊字符

3. **性能考虑**
   - 关闭不需要交互元素的Raycast Target
   - 合理使用Canvas
   - 避免过多的UI元素

4. **可维护性**
   - 功能分组
   - 清晰的层级结构
   - 添加注释（在GameObject名称中）

### 代码生成最佳实践

1. **定期重新生成**
   - UI结构变化后立即重新生成
   - 确保Designer类与Prefab同步

2. **保护业务逻辑**
   - 所有业务逻辑写在Logic类中
   - 不要修改Designer类

3. **版本控制**
   - Designer类和Logic类都应纳入版本控制
   - Prefab和UIRefs组件也应纳入版本控制

## 常见问题

### Q1: 生成的代码找不到某个UI元素的引用？

**原因**: UI元素可能没有被正确识别

**解决方法**:
1. 检查UI元素是否有对应的UI组件（Button、Text等）
2. 检查UI元素的命名是否包含特殊字符
3. 重新生成代码

### Q2: UIRefs组件的引用列表为空？

**原因**: UIRefs组件配置错误或Prefab结构有问题

**解决方法**:
1. 删除UIRefs组件
2. 重新运行UI Generator
3. 检查生成日志

### Q3: 重新生成后业务逻辑丢失？

**原因**: 可能修改了Designer类

**解决方法**:
1. Designer类会被覆盖，业务逻辑应该写在Logic类中
2. 使用版本控制恢复误删的Logic类
3. 从备份中恢复代码

### Q4: 编译错误：找不到某个类型？

**原因**: 缺少必要的using语句

**解决方法**:
1. 检查生成代码的using语句
2. 添加必要的命名空间引用：
   ```csharp
   using UnityEngine.UI;
   using TMPro;
   using Astrum.Client.UI.Core;
   ```

### Q5: UI元素在运行时为null？

**原因**: UIRefs初始化失败或路径错误

**解决方法**:
1. 检查UIRefs组件是否正确配置
2. 检查UI元素路径是否正确
3. 在Awake中添加日志检查初始化状态

## 相关文档

- [UI系统总览](UI-System-Overview%20UI系统总览.md) - 了解UI系统架构
- [UI编写指南](UI-Development-Guide%20UI编写指南.md) - 编写UI业务逻辑
- [UI运行时使用](UI-Runtime-Usage%20UI运行时使用.md) - 运行时管理UI
- [UI开发规范](UI-Conventions%20UI开发规范.md) - 命名规范和最佳实践

---

**版本历史**:
- v1.0 (2025-10-11) - 初始版本

