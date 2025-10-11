# UI运行时使用

> 📖 **版本**: v1.0 | **最后更新**: 2025-10-11

本文档介绍如何在运行时使用UIManager管理UI界面，包括显示、隐藏、销毁UI，以及各种使用场景。

## 前置要求

- 已创建并生成UI代码
- 已阅读 [UI系统总览](UI-System-Overview%20UI系统总览.md)
- 了解UIManager的基本概念

## UIManager概述

`UIManager` 是UI系统的核心管理器，负责UI的完整生命周期管理。

### 主要功能

- 🔹 **UI加载**: 从Prefab加载UI并实例化
- 🔹 **UI缓存**: 已创建的UI自动缓存，避免重复加载
- 🔹 **UI显示**: 显示指定的UI界面
- 🔹 **UI隐藏**: 隐藏指定的UI界面
- 🔹 **UI销毁**: 销毁UI并清除缓存
- 🔹 **UI查询**: 查询UI的存在性和显示状态

### 访问UIManager

UIManager是单例模式，通过以下方式访问：

```csharp
// 方式1：直接访问单例
UIManager.Instance.ShowUI("Login");

// 方式2：通过GameApplication访问（推荐）
var uiManager = GameApplication.Instance?.UIManager;
if (uiManager != null)
{
    uiManager.ShowUI("Login");
}
```

## 核心API

### ShowUI - 显示UI

```csharp
public void ShowUI(string uiName)
```

显示指定名称的UI。如果UI尚未创建，会自动加载并实例化；如果已存在，直接显示。

**参数**:
- `uiName`: UI名称（不含扩展名），对应Prefab文件名

**示例**:
```csharp
// 显示登录界面
UIManager.Instance.ShowUI("Login");

// 显示房间列表
UIManager.Instance.ShowUI("RoomList");

// 显示设置界面
UIManager.Instance.ShowUI("Settings");
```

**行为**:
1. 检查UI是否已缓存
2. 如果未缓存，从`Assets/ArtRes/UI/{uiName}.prefab`加载
3. 实例化GameObject
4. UIRefs自动初始化（Awake时）
5. 调用UI的Show()方法
6. GameObject设置为Active

### HideUI - 隐藏UI

```csharp
public void HideUI(string uiName)
```

隐藏指定名称的UI（不销毁，保留在缓存中）。

**参数**:
- `uiName`: UI名称

**示例**:
```csharp
// 隐藏登录界面
UIManager.Instance.HideUI("Login");

// 隐藏房间列表
UIManager.Instance.HideUI("RoomList");
```

**行为**:
1. 调用UI的Hide()方法（如果有）
2. GameObject设置为Inactive
3. UI保留在缓存中，下次ShowUI时快速显示

### DestroyUI - 销毁UI

```csharp
public void DestroyUI(string uiName)
```

销毁指定名称的UI并从缓存中移除。

**参数**:
- `uiName`: UI名称

**示例**:
```csharp
// 销毁登录界面
UIManager.Instance.DestroyUI("Login");
```

**行为**:
1. 从缓存中移除
2. 销毁GameObject
3. 释放相关资源
4. 下次ShowUI时需要重新加载

**使用场景**:
- 确定不再需要的UI
- 内存优化，释放不常用的UI

### GetUI - 获取UI GameObject

```csharp
public GameObject GetUI(string uiName)
```

获取指定名称的UI GameObject（如果存在）。

**参数**:
- `uiName`: UI名称

**返回值**:
- `GameObject`: UI的GameObject，如果不存在返回null

**示例**:
```csharp
// 获取登录界面GameObject
var loginUI = UIManager.Instance.GetUI("Login");
if (loginUI != null)
{
    // 访问UI组件
    var uiRefs = loginUI.GetComponent<UIRefs>();
    // ...
}
```

### HasUI - 检查UI是否存在

```csharp
public bool HasUI(string uiName)
```

检查指定名称的UI是否已创建（在缓存中）。

**参数**:
- `uiName`: UI名称

**返回值**:
- `bool`: 如果UI已创建返回true，否则返回false

**示例**:
```csharp
// 检查登录界面是否已创建
if (UIManager.Instance.HasUI("Login"))
{
    // UI已存在
    UIManager.Instance.ShowUI("Login");
}
else
{
    // UI不存在，首次显示会自动创建
    UIManager.Instance.ShowUI("Login");
}
```

### IsUIVisible - 检查UI是否显示

```csharp
public bool IsUIVisible(string uiName)
```

检查指定名称的UI是否当前正在显示。

**参数**:
- `uiName`: UI名称

**返回值**:
- `bool`: 如果UI正在显示返回true，否则返回false

**示例**:
```csharp
// 检查登录界面是否显示
if (UIManager.Instance.IsUIVisible("Login"))
{
    // 登录界面正在显示
    UIManager.Instance.HideUI("Login");
}
else
{
    // 登录界面未显示
    UIManager.Instance.ShowUI("Login");
}

// 切换UI显示状态
if (UIManager.Instance.IsUIVisible("Settings"))
{
    UIManager.Instance.HideUI("Settings");
}
else
{
    UIManager.Instance.ShowUI("Settings");
}
```

## 使用场景

### 场景1：游戏启动流程

```csharp
public class GameApplication : MonoBehaviour
{
    private void Start()
    {
        // 初始化所有管理器
        InitializeManagers();
        
        // 显示登录界面
        ShowLoginUI();
    }
    
    private void ShowLoginUI()
    {
        try
        {
            Debug.Log("显示登录UI");
            UIManager.Instance.ShowUI("Login");
        }
        catch (Exception ex)
        {
            Debug.LogError($"显示登录UI失败: {ex.Message}");
        }
    }
}
```

### 场景2：UI之间切换

```csharp
// 从登录界面切换到主菜单
public class LoginView
{
    private void OnLoginSuccess()
    {
        // 隐藏登录界面
        UIManager.Instance.HideUI("Login");
        
        // 显示主菜单
        UIManager.Instance.ShowUI("MainMenu");
    }
}

// 从主菜单返回登录界面
public class MainMenuView
{
    private void OnLogoutClicked()
    {
        // 隐藏主菜单
        UIManager.Instance.HideUI("MainMenu");
        
        // 显示登录界面
        UIManager.Instance.ShowUI("Login");
    }
}
```

### 场景3：弹出窗口

```csharp
// 显示弹出窗口
public void ShowConfirmDialog(string message, Action onConfirm, Action onCancel)
{
    // 显示确认对话框
    UIManager.Instance.ShowUI("ConfirmDialog");
    
    // 获取对话框实例并配置
    var dialog = UIManager.Instance.GetUI("ConfirmDialog");
    if (dialog != null)
    {
        var uiRefs = dialog.GetComponent<UIRefs>();
        var confirmDialog = uiRefs.GetUIInstance() as ConfirmDialogView;
        if (confirmDialog != null)
        {
            confirmDialog.SetMessage(message);
            confirmDialog.SetCallbacks(onConfirm, onCancel);
        }
    }
}

// 关闭弹出窗口
public void CloseConfirmDialog()
{
    UIManager.Instance.HideUI("ConfirmDialog");
}
```

### 场景4：UI栈管理（自定义扩展）

虽然UIManager本身不提供UI栈功能，但可以在业务层实现：

```csharp
public class UIStackManager
{
    private Stack<string> uiStack = new Stack<string>();
    
    // 推入新UI
    public void PushUI(string uiName)
    {
        // 隐藏当前UI
        if (uiStack.Count > 0)
        {
            UIManager.Instance.HideUI(uiStack.Peek());
        }
        
        // 推入新UI并显示
        uiStack.Push(uiName);
        UIManager.Instance.ShowUI(uiName);
    }
    
    // 弹出当前UI，返回上一级
    public void PopUI()
    {
        if (uiStack.Count == 0) return;
        
        // 隐藏并销毁当前UI
        var currentUI = uiStack.Pop();
        UIManager.Instance.HideUI(currentUI);
        
        // 显示上一级UI
        if (uiStack.Count > 0)
        {
            UIManager.Instance.ShowUI(uiStack.Peek());
        }
    }
    
    // 清空UI栈
    public void ClearStack()
    {
        while (uiStack.Count > 0)
        {
            var ui = uiStack.Pop();
            UIManager.Instance.HideUI(ui);
        }
    }
}

// 使用示例
var uiStackManager = new UIStackManager();

// 导航到房间列表
uiStackManager.PushUI("RoomList");

// 导航到房间详情
uiStackManager.PushUI("RoomDetail");

// 返回房间列表
uiStackManager.PopUI();
```

### 场景5：UI预加载

```csharp
// 预加载常用UI，避免首次显示时的加载延迟
public class UIPreloader
{
    public void PreloadCommonUI()
    {
        // 预加载并立即隐藏
        PreloadUI("MainMenu");
        PreloadUI("Settings");
        PreloadUI("LoadingScreen");
    }
    
    private void PreloadUI(string uiName)
    {
        // 显示（会触发加载和初始化）
        UIManager.Instance.ShowUI(uiName);
        
        // 立即隐藏
        UIManager.Instance.HideUI(uiName);
        
        Debug.Log($"预加载UI完成: {uiName}");
    }
}

// 在游戏启动时调用
void Start()
{
    var preloader = new UIPreloader();
    preloader.PreloadCommonUI();
}
```

### 场景6：加载界面

```csharp
public class LoadingManager
{
    // 显示加载界面
    public void ShowLoading(string message = "加载中...")
    {
        UIManager.Instance.ShowUI("Loading");
        
        var loadingUI = UIManager.Instance.GetUI("Loading");
        if (loadingUI != null)
        {
            var uiRefs = loadingUI.GetComponent<UIRefs>();
            var loadingView = uiRefs.GetUIInstance() as LoadingView;
            if (loadingView != null)
            {
                loadingView.SetMessage(message);
            }
        }
    }
    
    // 更新加载进度
    public void UpdateProgress(float progress)
    {
        var loadingUI = UIManager.Instance.GetUI("Loading");
        if (loadingUI != null)
        {
            var uiRefs = loadingUI.GetComponent<UIRefs>();
            var loadingView = uiRefs.GetUIInstance() as LoadingView;
            if (loadingView != null)
            {
                loadingView.UpdateProgress(progress);
            }
        }
    }
    
    // 隐藏加载界面
    public void HideLoading()
    {
        UIManager.Instance.HideUI("Loading");
    }
}

// 使用示例
var loadingManager = new LoadingManager();

// 显示加载界面
loadingManager.ShowLoading("正在加载资源...");

// 执行异步加载
await LoadResourcesAsync((progress) => {
    loadingManager.UpdateProgress(progress);
});

// 隐藏加载界面
loadingManager.HideLoading();
```

### 场景7：模态对话框

```csharp
public class DialogManager
{
    // 显示消息对话框
    public void ShowMessageDialog(string title, string message, Action onClose = null)
    {
        UIManager.Instance.ShowUI("MessageDialog");
        
        var dialogUI = UIManager.Instance.GetUI("MessageDialog");
        if (dialogUI != null)
        {
            var uiRefs = dialogUI.GetComponent<UIRefs>();
            var dialog = uiRefs.GetUIInstance() as MessageDialogView;
            if (dialog != null)
            {
                dialog.SetContent(title, message);
                dialog.SetCloseCallback(onClose);
            }
        }
    }
    
    // 显示确认对话框
    public void ShowConfirmDialog(string title, string message, 
        Action onConfirm, Action onCancel = null)
    {
        UIManager.Instance.ShowUI("ConfirmDialog");
        
        var dialogUI = UIManager.Instance.GetUI("ConfirmDialog");
        if (dialogUI != null)
        {
            var uiRefs = dialogUI.GetComponent<UIRefs>();
            var dialog = uiRefs.GetUIInstance() as ConfirmDialogView;
            if (dialog != null)
            {
                dialog.SetContent(title, message);
                dialog.SetCallbacks(onConfirm, onCancel);
            }
        }
    }
}

// 使用示例
var dialogManager = new DialogManager();

// 显示消息
dialogManager.ShowMessageDialog("提示", "操作完成！");

// 显示确认对话框
dialogManager.ShowConfirmDialog(
    "确认", 
    "是否退出游戏？", 
    () => Application.Quit(),  // 确认
    () => Debug.Log("取消退出")  // 取消
);
```

## 高级用法

### 访问UI实例

通过UIRefs获取UI逻辑类实例：

```csharp
// 获取UI GameObject
var loginUI = UIManager.Instance.GetUI("Login");
if (loginUI != null)
{
    // 获取UIRefs组件
    var uiRefs = loginUI.GetComponent<UIRefs>();
    if (uiRefs != null)
    {
        // 获取UI逻辑类实例
        var loginView = uiRefs.GetUIInstance();
        
        // 或使用泛型方法（如果UIRefs支持）
        // var loginView = uiRefs.GetUIInstance<LoginView>();
        
        // 调用UI方法
        if (loginView != null)
        {
            var type = loginView.GetType();
            var method = type.GetMethod("SetServerInfo");
            method?.Invoke(loginView, new object[] { "127.0.0.1", 8888 });
        }
    }
}
```

### 动态创建UI

```csharp
// 动态创建UI实例（不通过UIManager）
public GameObject CreateUI(string prefabName, Transform parent)
{
    var prefabPath = $"Assets/ArtRes/UI/{prefabName}.prefab";
    
    #if UNITY_EDITOR
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    #else
    var prefab = Resources.Load<GameObject>($"UI/{prefabName}");
    #endif
    
    if (prefab == null)
    {
        Debug.LogError($"找不到UI Prefab: {prefabPath}");
        return null;
    }
    
    var uiObject = GameObject.Instantiate(prefab, parent);
    uiObject.name = prefabName;
    
    return uiObject;
}
```

### UI事件通信

```csharp
// 使用事件在UI之间通信
public class UIEventBus
{
    public static event Action<string> OnUIEvent;
    
    public static void Publish(string eventName)
    {
        OnUIEvent?.Invoke(eventName);
    }
    
    public static void Subscribe(Action<string> handler)
    {
        OnUIEvent += handler;
    }
    
    public static void Unsubscribe(Action<string> handler)
    {
        OnUIEvent -= handler;
    }
}

// UI A 发送事件
UIEventBus.Publish("UserLoggedIn");

// UI B 接收事件
protected virtual void OnInitialize()
{
    UIEventBus.Subscribe(OnUIEvent);
}

private void OnUIEvent(string eventName)
{
    if (eventName == "UserLoggedIn")
    {
        // 处理用户登录事件
        UpdateUserInfo();
    }
}

protected virtual void OnHide()
{
    UIEventBus.Unsubscribe(OnUIEvent);
}
```

## 性能优化

### UI缓存策略

```csharp
// 常驻UI：始终保持在缓存中
private readonly HashSet<string> persistentUI = new HashSet<string>
{
    "HUD",          // 游戏HUD
    "MainMenu",     // 主菜单
    "Loading"       // 加载界面
};

// 临时UI：使用后可以销毁
private readonly HashSet<string> temporaryUI = new HashSet<string>
{
    "Settings",     // 设置界面
    "About",        // 关于界面
    "Credits"       // 制作人员
};

// 根据策略管理UI
public void ManageUICache()
{
    // 销毁不常用的临时UI
    foreach (var uiName in temporaryUI)
    {
        if (UIManager.Instance.HasUI(uiName) && 
            !UIManager.Instance.IsUIVisible(uiName))
        {
            // 超过一定时间未使用，销毁
            UIManager.Instance.DestroyUI(uiName);
        }
    }
}
```

### UI预加载和异步加载

```csharp
// 异步预加载UI
public async Task PreloadUIAsync(string uiName)
{
    await Task.Run(() =>
    {
        // 模拟异步加载
        System.Threading.Thread.Sleep(100);
    });
    
    // 在主线程中显示UI
    UIManager.Instance.ShowUI(uiName);
    UIManager.Instance.HideUI(uiName);
}

// 批量预加载
public async Task PreloadMultipleUIAsync(params string[] uiNames)
{
    var tasks = new List<Task>();
    foreach (var uiName in uiNames)
    {
        tasks.Add(PreloadUIAsync(uiName));
    }
    await Task.WhenAll(tasks);
}
```

## 最佳实践

### 1. UI管理

- ✅ 使用UIManager统一管理所有UI
- ✅ 不要手动实例化UI Prefab（除非特殊需求）
- ✅ 常用UI保持在缓存中，不常用UI及时销毁
- ✅ UI切换使用HideUI而不是DestroyUI（除非确定不再需要）

### 2. 错误处理

```csharp
// 安全的UI显示
public void SafeShowUI(string uiName)
{
    try
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager未初始化");
            return;
        }
        
        UIManager.Instance.ShowUI(uiName);
    }
    catch (Exception ex)
    {
        Debug.LogError($"显示UI失败: {uiName}, 错误: {ex.Message}");
    }
}
```

### 3. UI生命周期

- ✅ 在OnInitialize中绑定事件
- ✅ 在OnShow中更新数据
- ✅ 在OnHide中清理临时资源
- ✅ 避免在Update中频繁更新UI

### 4. 内存管理

- ✅ 及时销毁不再使用的UI
- ✅ 大型UI考虑延迟加载
- ✅ 使用对象池管理列表项等频繁创建的UI元素

## 常见问题

### Q1: UI显示后看不到？

**可能原因**:
1. Canvas层级问题
2. UI被其他UI遮挡
3. Canvas Sorting Order设置不正确

**解决方法**:
```csharp
// 检查UI是否真的显示
if (UIManager.Instance.IsUIVisible("Login"))
{
    var loginUI = UIManager.Instance.GetUI("Login");
    Debug.Log($"Login UI Active: {loginUI.activeInHierarchy}");
    
    // 检查Canvas设置
    var canvas = loginUI.GetComponent<Canvas>();
    if (canvas != null)
    {
        Debug.Log($"Canvas Sorting Order: {canvas.sortingOrder}");
    }
}
```

### Q2: UI重复创建？

**原因**: 多次调用ShowUI

**解决方法**:
```csharp
// 检查UI是否已显示
if (!UIManager.Instance.IsUIVisible("Login"))
{
    UIManager.Instance.ShowUI("Login");
}
```

### Q3: 切换UI时出现闪烁？

**原因**: 先隐藏后显示导致的短暂空白

**解决方法**:
```csharp
// 先显示新UI，再隐藏旧UI
UIManager.Instance.ShowUI("NewUI");
UIManager.Instance.HideUI("OldUI");
```

## 相关文档

- [UI系统总览](UI-System-Overview%20UI系统总览.md) - 了解UI系统架构
- [UI创建指南](UI-Creation-Guide%20UI创建指南.md) - 创建UI
- [UI编写指南](UI-Development-Guide%20UI编写指南.md) - 编写UI逻辑
- [UI开发规范](UI-Conventions%20UI开发规范.md) - 开发规范

---

**版本历史**:
- v1.0 (2025-10-11) - 初始版本

