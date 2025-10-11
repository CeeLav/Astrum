# UI开发规范

> 📖 **版本**: v1.0 | **最后更新**: 2025-10-11

本文档定义Astrum项目中UI开发的命名规范、目录结构、代码风格和最佳实践。

## 命名规范

### UI Prefab命名

**规则**: PascalCase（大驼峰命名）

```
✅ 正确示例:
Login.prefab
MainMenu.prefab
RoomList.prefab
RoomDetail.prefab
UserSettings.prefab
ConfirmDialog.prefab

❌ 错误示例:
login.prefab          (全小写)
main_menu.prefab      (下划线)
roomList.prefab       (camelCase)
Room List.prefab      (包含空格)
```

### UI GameObject命名

**规则**: PascalCase，描述性命名

```
✅ 正确示例:
LoginButton
UsernameInput
ConnectionStatusText
RoomListScrollView
ConfirmDialog
BackgroundImage

❌ 错误示例:
button1               (无意义名称)
input_field           (下划线)
txt                   (缩写)
Image (1)             (默认名称)
```

### UI类命名

**规则**: UI名称 + "View"后缀，PascalCase

```
✅ 正确示例:
LoginView.cs
MainMenuView.cs
RoomListView.cs
ConfirmDialogView.cs

❌ 错误示例:
Login.cs              (缺少View后缀)
LoginUI.cs            (使用UI后缀)
login_view.cs         (下划线)
```

### UI元素引用字段命名

**规则**: 元素名称 + 组件类型（camelCase）

UI Generator自动生成的引用字段遵循此规范：

```csharp
✅ 正确示例:
private Button loginButtonButton;
private Text usernameTextText;
private Image backgroundImageImage;
private InputField usernameInputInputField;

格式: {元素名称}{组件类型}
```

### 方法命名

**事件处理方法**: `On` + 元素名称 + 事件类型

```csharp
✅ 正确示例:
private void OnLoginButtonClicked()
private void OnUsernameInputChanged(string value)
private void OnSettingsToggleValueChanged(bool isOn)
private void OnNetworkConnected()

❌ 错误示例:
private void LoginClick()           (缺少On前缀)
private void login_button_click()   (下划线)
private void HandleLogin()          (不清晰)
```

**业务逻辑方法**: 动词 + 名词

```csharp
✅ 正确示例:
private void PerformLogin()
private void ValidateInput()
private void UpdateUserInfo()
private void LoadRoomList()
private void SendLoginRequest()

❌ 错误示例:
private void Login()                (过于简单)
private void DoIt()                 (无意义)
private void Process()              (不清晰)
```

**UI更新方法**: `Update` / `Set` / `Refresh` + 目标

```csharp
✅ 正确示例:
private void UpdateConnectionStatus(string status)
private void SetButtonInteractable(bool interactable)
private void RefreshRoomList()
private void SetStatusText(string text)

❌ 错误示例:
private void ChangeStatus()         (不清晰)
private void Refresh()              (过于泛化)
```

## 目录结构规范

### 标准目录结构

```
AstrumProj/
├── Assets/
│   ├── ArtRes/
│   │   └── UI/                                  # UI Prefab目录
│   │       ├── Login.prefab
│   │       ├── MainMenu.prefab
│   │       ├── RoomList.prefab
│   │       └── ...
│   │
│   └── Script/
│       ├── AstrumClient/
│       │   ├── Managers/
│       │   │   └── UIManager.cs                 # UI管理器
│       │   │
│       │   └── UI/
│       │       ├── Core/                        # 核心组件
│       │       │   ├── UIRefs.cs
│       │       │   └── UIPanel.cs
│       │       │
│       │       └── Generated/                   # 生成的UI代码
│       │           ├── LoginView.cs
│       │           ├── LoginView.designer.cs
│       │           ├── MainMenuView.cs
│       │           ├── MainMenuView.designer.cs
│       │           └── ...
│       │
│       └── Editor/
│           └── UIGenerator/                     # UI生成器
│               ├── Core/
│               ├── Generators/
│               ├── Windows/
│               └── README.md
```

### 目录组织原则

1. **UI Prefab**: 统一放在 `Assets/ArtRes/UI/` 目录
2. **UI代码**: 统一放在 `Assets/Script/AstrumClient/UI/Generated/` 目录
3. **UI核心组件**: 放在 `Assets/Script/AstrumClient/UI/Core/` 目录
4. **编辑器工具**: 放在 `Assets/Script/Editor/UIGenerator/` 目录

### 文件组织

**相关文件放在一起**:
```
Generated/
├── LoginView.cs                 # 逻辑类
├── LoginView.designer.cs        # 设计器类
├── MainMenuView.cs
├── MainMenuView.designer.cs
└── ...
```

**不要创建过深的子目录**:
```
❌ 避免:
Generated/
└── Login/
    └── Views/
        └── Components/
            └── LoginView.cs

✅ 推荐:
Generated/
└── LoginView.cs
```

## 代码风格规范

### Region分区

使用Region组织代码，提高可读性：

```csharp
public partial class LoginView
{
    #region Fields
    
    private bool isConnecting = false;
    
    #endregion
    
    #region Lifecycle Callbacks
    
    protected virtual void OnInitialize() { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    
    #endregion
    
    #region UI Event Binding
    
    private void BindUIEvents() { }
    
    #endregion
    
    #region UI Event Handlers
    
    private void OnLoginButtonClicked() { }
    
    #endregion
    
    #region Network Event Subscription
    
    private void SubscribeToNetworkEvents() { }
    private void UnsubscribeFromNetworkEvents() { }
    
    #endregion
    
    #region Network Event Handlers
    
    private void OnNetworkConnected() { }
    
    #endregion
    
    #region Business Logic
    
    private void PerformLogin() { }
    private bool ValidateInput() { return true; }
    
    #endregion
    
    #region UI Update Methods
    
    private void UpdateConnectionStatus(string status) { }
    private void SetButtonInteractable(bool interactable) { }
    
    #endregion
    
    #region Helper Methods
    
    private string FormatTime(float seconds) { return ""; }
    
    #endregion
}
```

### 注释规范

**类注释**:
```csharp
/// <summary>
/// 登录界面
/// 用于用户登录和连接服务器
/// </summary>
public partial class LoginView
{
}
```

**方法注释**:
```csharp
/// <summary>
/// 连接到服务器
/// </summary>
/// <param name="address">服务器地址</param>
/// <param name="port">服务器端口</param>
private async void ConnectToServer(string address, int port)
{
}
```

**字段注释**:
```csharp
// 是否正在连接中
private bool isConnecting = false;

// 服务器地址
private string serverAddress = "127.0.0.1";
```

### 代码格式

**缩进**: 使用4个空格

**大括号**: K&R风格（左大括号不换行）
```csharp
✅ 正确:
if (condition) {
    // code
}

❌ 错误:
if (condition)
{
    // code
}
```

**命名空间**: 使用文件作用域命名空间（C# 10+）或传统方式
```csharp
// C# 10+ 文件作用域命名空间（推荐）
namespace Astrum.Client.UI.Generated;

public partial class LoginView
{
}

// 或传统方式
namespace Astrum.Client.UI.Generated
{
    public partial class LoginView
    {
    }
}
```

**using语句顺序**:
```csharp
// 1. System命名空间
using System;
using System.Collections.Generic;

// 2. Unity命名空间
using UnityEngine;
using UnityEngine.UI;

// 3. 第三方库
using TMPro;

// 4. 项目命名空间
using Astrum.Client.Core;
using Astrum.Client.Managers;
using Astrum.Client.UI.Core;
using Astrum.Network.Generated;
```

### 访问修饰符

**字段**: 使用private，除非需要序列化
```csharp
private bool isConnecting;
[SerializeField] private float delay;
```

**方法**:
- 生命周期回调: `protected virtual`
- 事件处理: `private`
- 业务逻辑: `private` 或 `public`（如果需要外部访问）
- 辅助方法: `private`

```csharp
protected virtual void OnInitialize() { }
private void OnLoginButtonClicked() { }
public void PerformLogin() { }
private bool ValidateInput() { return true; }
```

## UI层级结构规范

### 标准UI层级

```
UIRoot (Canvas)
└── [UI Name] (RectTransform)
    ├── Background (Panel/Image)
    ├── Header (Empty GameObject)
    │   ├── Title
    │   └── CloseButton
    ├── Content (Empty GameObject or Panel)
    │   ├── Section1
    │   └── Section2
    ├── Footer (Empty GameObject)
    │   ├── ConfirmButton
    │   └── CancelButton
    └── Overlay (for dialogs/popups)
```

### 层级组织原则

1. **功能分组**: 相关UI元素放在同一个容器下
   ```
   UserInfoPanel
   ├── AvatarImage
   ├── UsernameText
   └── LevelText
   ```

2. **避免过深嵌套**: 层级深度建议不超过5层
   ```
   ✅ 推荐: 3-4层
   Login
   └── Content
       └── UsernamePanel
           └── UsernameInput

   ❌ 避免: 6层以上
   Login
   └── Container
       └── Panel
           └── Group
               └── Section
                   └── Field
                       └── Input
   ```

3. **使用描述性容器**: 容器命名应表达其包含的内容
   ```
   ✅ 正确:
   ButtonContainer
   InputFieldsPanel
   ItemListScrollView

   ❌ 错误:
   Container1
   Panel
   GameObject
   ```

## 性能规范

### UI组件使用

**Raycast Target**: 只在需要交互的元素上启用
```
✅ 需要启用:
- Button
- Toggle
- InputField
- Slider
- 可点击的Image

❌ 应该禁用:
- 纯装饰性Image
- 背景Image
- 静态Text
- 不交互的UI元素
```

**Canvas**: 合理拆分Canvas
```
✅ 推荐:
- 静态UI使用一个Canvas
- 动态更新的UI使用独立Canvas
- 弹出窗口使用独立Canvas

❌ 避免:
- 所有UI放在同一个Canvas
- 频繁修改整个Canvas
```

### 代码性能

**避免频繁的UI更新**:
```csharp
❌ 错误:
void Update()
{
    scoreText.text = GetScore().ToString();
}

✅ 正确:
private int lastScore = -1;
void Update()
{
    int currentScore = GetScore();
    if (currentScore != lastScore)
    {
        scoreText.text = currentScore.ToString();
        lastScore = currentScore;
    }
}
```

**缓存组件引用**:
```csharp
❌ 错误:
void Update()
{
    GetComponent<Text>().text = "...";
}

✅ 正确:
private Text cachedText;
void Start()
{
    cachedText = GetComponent<Text>();
}
void Update()
{
    cachedText.text = "...";
}
```

## 错误处理规范

### Null检查

**总是检查Null**:
```csharp
✅ 正确:
if (loginButtonButton != null)
{
    loginButtonButton.onClick.AddListener(OnLoginButtonClicked);
}

❌ 错误:
loginButtonButton.onClick.AddListener(OnLoginButtonClicked); // 可能抛出NullReferenceException
```

### Try-Catch使用

**关键操作使用Try-Catch**:
```csharp
✅ 正确:
private async void LoadData()
{
    try
    {
        var data = await DataService.LoadAsync();
        UpdateUI(data);
    }
    catch (Exception ex)
    {
        Debug.LogError($"加载数据失败: {ex.Message}");
        ShowErrorMessage("加载失败");
    }
}
```

### 日志记录

**使用ASLogger**:
```csharp
✅ 正确:
ASLogger.Instance.Info("UI初始化完成");
ASLogger.Instance.Error($"UI加载失败: {ex.Message}");

❌ 避免过度使用:
Debug.Log("Button clicked");  // 不必要的日志
```

## 版本控制规范

### Git Ignore

Designer类和Logic类都应该纳入版本控制：
```
✅ 应该提交:
- LoginView.cs
- LoginView.designer.cs
- Login.prefab
- Login.prefab.meta

❌ 不应提交:
- 临时文件
- 编辑器生成的缓存
```

### Commit Message

UI相关的提交信息格式：
```
✅ 推荐格式:
[UI] 添加登录界面
[UI] 修复房间列表显示问题
[UI] 优化设置界面布局
[UI Generator] 支持TextMeshPro组件

❌ 避免:
Update UI
Fix bug
Modified files
```

## 测试规范

### UI测试检查清单

**功能测试**:
- [ ] UI能正确显示和隐藏
- [ ] 所有按钮功能正常
- [ ] 输入框能正确输入和验证
- [ ] 事件绑定正确
- [ ] 数据绑定正确

**性能测试**:
- [ ] UI加载时间在可接受范围内
- [ ] 没有不必要的UI更新
- [ ] Raycast Target设置合理
- [ ] 没有内存泄漏

**兼容性测试**:
- [ ] 不同分辨率下显示正常
- [ ] 不同平台下显示正常
- [ ] UI适配正确

## 最佳实践总结

### 必须遵守

1. ✅ 使用UIManager管理所有UI
2. ✅ UI Prefab放在 `Assets/ArtRes/UI/` 目录
3. ✅ UI代码放在 `Assets/Script/AstrumClient/UI/Generated/` 目录
4. ✅ 使用PascalCase命名UI Prefab和类
5. ✅ 使用UI Generator生成代码
6. ✅ 不要修改 `.designer.cs` 文件
7. ✅ 在OnInitialize中绑定事件
8. ✅ 检查Null引用
9. ✅ 使用Region组织代码
10. ✅ 关闭不必要元素的Raycast Target

### 推荐遵守

1. ✅ 使用描述性的命名
2. ✅ 添加必要的注释
3. ✅ 避免过深的层级嵌套
4. ✅ 合理使用Canvas分离
5. ✅ 缓存频繁访问的组件引用
6. ✅ 避免在Update中频繁更新UI
7. ✅ 使用Try-Catch保护关键操作
8. ✅ 及时销毁不再使用的UI
9. ✅ 预加载常用UI
10. ✅ 使用对象池管理列表项

### 避免的做法

1. ❌ 不要修改Designer类
2. ❌ 不要手动实例化UI Prefab（使用UIManager）
3. ❌ 不要在Update中频繁修改UI
4. ❌ 不要使用无意义的命名
5. ❌ 不要创建过深的层级结构
6. ❌ 不要在所有UI元素上启用Raycast Target
7. ❌ 不要忽略Null检查
8. ❌ 不要在Designer类中编写业务逻辑
9. ❌ 不要使用全局变量管理UI状态
10. ❌ 不要忽略内存管理

## 代码审查清单

在提交UI代码之前，请检查以下项目：

**命名规范**:
- [ ] UI Prefab使用PascalCase
- [ ] UI类使用View后缀
- [ ] 方法命名清晰准确
- [ ] 变量命名有意义

**代码质量**:
- [ ] 没有修改Designer类
- [ ] 使用Region分区
- [ ] 添加必要注释
- [ ] 没有硬编码的魔法数字
- [ ] 异常处理完善

**性能**:
- [ ] Raycast Target设置合理
- [ ] 没有不必要的UI更新
- [ ] 缓存了组件引用
- [ ] 没有内存泄漏

**功能**:
- [ ] 所有功能正常工作
- [ ] 事件绑定正确
- [ ] 数据显示正确
- [ ] 错误处理完善

## 相关文档

- [UI系统总览](UI-System-Overview%20UI系统总览.md) - 了解UI系统架构
- [UI创建指南](UI-Creation-Guide%20UI创建指南.md) - 创建UI
- [UI编写指南](UI-Development-Guide%20UI编写指南.md) - 编写UI逻辑
- [UI运行时使用](UI-Runtime-Usage%20UI运行时使用.md) - 运行时管理UI

---

**版本历史**:
- v1.0 (2025-10-11) - 初始版本

