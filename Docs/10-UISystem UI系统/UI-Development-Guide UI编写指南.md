# UI编写指南

> 📖 **版本**: v1.0 | **最后更新**: 2025-10-11

本文档介绍如何在Astrum项目中编写UI业务逻辑代码，包括生命周期回调、事件处理、数据绑定等最佳实践。

## 前置要求

- 已完成UI Prefab创建和代码生成
- 已阅读 [UI系统总览](UI-System-Overview%20UI系统总览.md)
- 已阅读 [UI创建指南](UI-Creation-Guide%20UI创建指南.md)
- 熟悉C#编程

## 代码结构

### Partial类分离设计

UI代码采用Partial类分离，分为两个文件：

```
LoginView.designer.cs           (自动生成，不可修改)
├── UI References               - UI元素引用字段
├── Initialize()                - 初始化方法
├── InitializeUIElements()      - 初始化UI元素引用
└── Show() / Hide()             - 基本显示/隐藏方法

LoginView.cs                    (手动编写，业务逻辑)
├── OnInitialize()              - 初始化回调（虚方法）
├── OnShow() / OnHide()         - 显示/隐藏回调（虚方法）
├── Event Handlers              - 事件处理方法
├── Business Logic              - 业务逻辑方法
└── Helper Methods              - 辅助方法
```

### 文件职责

**Designer类（自动生成）**:
- 包含所有UI元素的引用字段
- 提供初始化方法
- 提供基本的Show/Hide方法
- **不要修改此文件**，重新生成会覆盖

**Logic类（手动编写）**:
- 实现UI的业务逻辑
- 处理用户交互事件
- 管理UI数据和状态
- **可以自由修改**，重新生成不会覆盖

## 生命周期

### UI生命周期回调

```csharp
public partial class LoginView
{
    // 1. 初始化回调 - UI第一次创建时调用（仅一次）
    protected virtual void OnInitialize()
    {
        // 绑定事件
        // 初始化数据
        // 设置默认状态
    }
    
    // 2. 显示回调 - UI每次显示时调用
    protected virtual void OnShow()
    {
        // 更新UI数据
        // 重置UI状态
        // 播放动画
    }
    
    // 3. 隐藏回调 - UI每次隐藏时调用
    protected virtual void OnHide()
    {
        // 清理临时数据
        // 停止动画
        // 取消订阅事件（如果需要）
    }
}
```

### 生命周期顺序

```
Prefab加载 → GameObject实例化 → UIRefs.Awake
   ↓
UIRefs收集引用 → 实例化UI逻辑类 → Initialize()
   ↓
InitializeUIElements() → OnInitialize() ← 【在此绑定事件】
   ↓
UIManager.ShowUI() → Show() → OnShow() ← 【在此更新数据】
   ↓
... UI显示中 ...
   ↓
UIManager.HideUI() → OnHide() → Hide() ← 【在此清理资源】
```

## 编写业务逻辑

### 1. 实现OnInitialize

`OnInitialize` 在UI第一次创建时调用，用于初始化设置：

```csharp
protected virtual void OnInitialize()
{
    // 1. 绑定按钮事件
    if (loginButtonButton != null)
    {
        loginButtonButton.onClick.AddListener(OnLoginButtonClicked);
    }
    
    if (registerButtonButton != null)
    {
        registerButtonButton.onClick.AddListener(OnRegisterButtonClicked);
    }
    
    // 2. 绑定输入框事件
    if (usernameInputInputField != null)
    {
        usernameInputInputField.onValueChanged.AddListener(OnUsernameChanged);
        usernameInputInputField.onEndEdit.AddListener(OnUsernameEndEdit);
    }
    
    // 3. 订阅系统事件
    SubscribeToNetworkEvents();
    
    // 4. 初始化UI状态
    UpdateLoginButtonState(false);
    SetStatusText("请输入用户名和密码");
}
```

**OnInitialize中应该做的**:
- ✅ 绑定UI事件（Button.onClick等）
- ✅ 订阅系统事件
- ✅ 初始化默认UI状态
- ✅ 设置默认值

**OnInitialize中不应该做的**:
- ❌ 访问可能未初始化的外部系统
- ❌ 执行耗时操作
- ❌ 进行网络请求

### 2. 实现OnShow

`OnShow` 在UI每次显示时调用，用于更新UI数据：

```csharp
protected virtual void OnShow()
{
    // 1. 更新UI显示数据
    UpdateUserInfo();
    
    // 2. 重置UI状态
    ResetInputFields();
    
    // 3. 刷新列表数据
    RefreshRoomList();
    
    // 4. 播放显示动画
    PlayShowAnimation();
    
    // 5. 重新订阅事件（如果在OnHide中取消订阅）
    // SubscribeToEvents();
}
```

**OnShow中应该做的**:
- ✅ 更新动态数据
- ✅ 重置UI状态
- ✅ 播放显示动画
- ✅ 刷新列表内容

**OnShow中不应该做的**:
- ❌ 绑定事件（应该在OnInitialize中）
- ❌ 执行一次性初始化（应该在OnInitialize中）

### 3. 实现OnHide

`OnHide` 在UI每次隐藏时调用，用于清理资源：

```csharp
protected virtual void OnHide()
{
    // 1. 停止动画
    StopAllAnimations();
    
    // 2. 清理临时数据
    ClearTempData();
    
    // 3. 取消订阅事件（如果需要）
    // UnsubscribeFromEvents();
    
    // 4. 重置状态
    ResetState();
}
```

**OnHide中应该做的**:
- ✅ 停止动画
- ✅ 清理临时数据
- ✅ 重置状态

**OnHide中不应该做的**:
- ❌ 销毁UI（由UIManager管理）
- ❌ 取消OnInitialize中绑定的事件（除非特殊需求）

## 事件处理

### UI事件绑定

#### Button点击事件

```csharp
protected virtual void OnInitialize()
{
    // 绑定按钮点击事件
    if (loginButtonButton != null)
    {
        loginButtonButton.onClick.AddListener(OnLoginButtonClicked);
    }
}

// 事件处理方法
private void OnLoginButtonClicked()
{
    // 验证输入
    if (!ValidateInput())
    {
        ShowErrorMessage("请输入用户名和密码");
        return;
    }
    
    // 执行登录逻辑
    PerformLogin();
}
```

#### InputField输入事件

```csharp
protected virtual void OnInitialize()
{
    if (usernameInputInputField != null)
    {
        // 输入值改变事件
        usernameInputInputField.onValueChanged.AddListener(OnUsernameChanged);
        
        // 输入结束事件
        usernameInputInputField.onEndEdit.AddListener(OnUsernameEndEdit);
    }
}

private void OnUsernameChanged(string value)
{
    // 实时验证输入
    ValidateUsername(value);
}

private void OnUsernameEndEdit(string value)
{
    // 输入完成后的处理
    ProcessUsername(value);
}
```

#### Toggle开关事件

```csharp
protected virtual void OnInitialize()
{
    if (rememberToggleToggle != null)
    {
        rememberToggleToggle.onValueChanged.AddListener(OnRememberToggleChanged);
    }
}

private void OnRememberToggleChanged(bool isOn)
{
    // 处理开关状态变化
    PlayerPrefs.SetInt("RememberUser", isOn ? 1 : 0);
}
```

#### Slider滑动事件

```csharp
protected virtual void OnInitialize()
{
    if (volumeSliderSlider != null)
    {
        volumeSliderSlider.onValueChanged.AddListener(OnVolumeChanged);
    }
}

private void OnVolumeChanged(float value)
{
    // 更新音量
    AudioManager.Instance.SetVolume(value);
    
    // 更新显示文本
    if (volumeTextText != null)
    {
        volumeTextText.text = $"{(int)(value * 100)}%";
    }
}
```

#### Dropdown下拉框事件

```csharp
protected virtual void OnInitialize()
{
    if (qualityDropdownDropdown != null)
    {
        qualityDropdownDropdown.onValueChanged.AddListener(OnQualityChanged);
    }
}

private void OnQualityChanged(int index)
{
    // 设置画质等级
    QualitySettings.SetQualityLevel(index);
}
```

### 系统事件订阅

订阅外部系统的事件（如网络事件、游戏事件）：

```csharp
protected virtual void OnInitialize()
{
    // 订阅网络事件
    SubscribeToNetworkEvents();
}

private void SubscribeToNetworkEvents()
{
    var networkManager = GameApplication.Instance?.NetworkManager;
    if (networkManager != null)
    {
        networkManager.OnConnected += OnNetworkConnected;
        networkManager.OnDisconnected += OnNetworkDisconnected;
        networkManager.OnLoginResponse += OnLoginResponse;
    }
}

private void UnsubscribeFromNetworkEvents()
{
    var networkManager = GameApplication.Instance?.NetworkManager;
    if (networkManager != null)
    {
        networkManager.OnConnected -= OnNetworkConnected;
        networkManager.OnDisconnected -= OnNetworkDisconnected;
        networkManager.OnLoginResponse -= OnLoginResponse;
    }
}

// 网络事件处理
private void OnNetworkConnected()
{
    SetStatusText("已连接到服务器");
    UpdateLoginButtonState(true);
}

private void OnNetworkDisconnected()
{
    SetStatusText("连接断开");
    UpdateLoginButtonState(false);
}

private void OnLoginResponse(LoginResponse response)
{
    if (response.Success)
    {
        SetStatusText("登录成功");
        // 切换到下一个UI
        UIManager.Instance.ShowUI("MainMenu");
    }
    else
    {
        SetStatusText($"登录失败: {response.Message}");
    }
}
```

**事件订阅最佳实践**:
- 在OnInitialize中订阅
- 在OnHide或组件销毁时取消订阅
- 检查null引用
- 使用try-catch保护

## 数据管理

### 访问Manager

通过GameApplication单例访问各种Manager：

```csharp
// 网络管理器
var networkManager = GameApplication.Instance?.NetworkManager;
if (networkManager != null)
{
    networkManager.Send(request);
}

// UI管理器
var uiManager = GameApplication.Instance?.UIManager;
if (uiManager != null)
{
    uiManager.ShowUI("Settings");
}

// 配置管理器
var configManager = GameApplication.Instance?.ConfigManager;
if (configManager != null)
{
    var config = configManager.GetConfig<CharacterConfig>(id);
}

// 音频管理器
var audioManager = GameApplication.Instance?.AudioManager;
if (audioManager != null)
{
    audioManager.PlaySFX("ButtonClick");
}
```

### 数据绑定

将数据绑定到UI元素：

```csharp
// 简单数据绑定
private void UpdateUserInfo(UserInfo userInfo)
{
    if (userInfo == null) return;
    
    if (usernameTextText != null)
    {
        usernameTextText.text = userInfo.Username;
    }
    
    if (levelTextText != null)
    {
        levelTextText.text = $"Lv.{userInfo.Level}";
    }
    
    if (avatarImageImage != null)
    {
        LoadAvatar(userInfo.AvatarUrl);
    }
}

// 列表数据绑定
private void UpdateRoomList(List<RoomInfo> rooms)
{
    if (roomListScrollRect == null) return;
    
    // 清空现有列表
    ClearRoomList();
    
    // 创建列表项
    foreach (var room in rooms)
    {
        var roomItem = CreateRoomItem(room);
        roomItem.transform.SetParent(roomListScrollRect.content, false);
    }
}
```

### 本地数据存储

使用PlayerPrefs或其他存储机制保存用户设置：

```csharp
// 保存设置
private void SaveSettings()
{
    PlayerPrefs.SetInt("Quality", qualityDropdownDropdown.value);
    PlayerPrefs.SetFloat("Volume", volumeSliderSlider.value);
    PlayerPrefs.SetInt("Fullscreen", fullscreenToggleToggle.isOn ? 1 : 0);
    PlayerPrefs.Save();
}

// 加载设置
private void LoadSettings()
{
    if (qualityDropdownDropdown != null)
    {
        qualityDropdownDropdown.value = PlayerPrefs.GetInt("Quality", 2);
    }
    
    if (volumeSliderSlider != null)
    {
        volumeSliderSlider.value = PlayerPrefs.GetFloat("Volume", 0.8f);
    }
    
    if (fullscreenToggleToggle != null)
    {
        fullscreenToggleToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    }
}
```

## UI更新方法

### 更新文本

```csharp
// 简单更新
private void SetStatusText(string text)
{
    if (statusTextText != null)
    {
        statusTextText.text = text;
    }
}

// 格式化更新
private void UpdateScoreText(int score)
{
    if (scoreTextText != null)
    {
        scoreTextText.text = $"分数: {score:N0}";
    }
}

// 富文本更新
private void SetColoredText(string text, Color color)
{
    if (messageTextText != null)
    {
        messageTextText.text = text;
        messageTextText.color = color;
    }
}
```

### 更新图片

```csharp
// 设置Sprite
private void SetImage(Sprite sprite)
{
    if (iconImageImage != null)
    {
        iconImageImage.sprite = sprite;
    }
}

// 设置颜色
private void SetImageColor(Color color)
{
    if (iconImageImage != null)
    {
        iconImageImage.color = color;
    }
}

// 异步加载图片
private async void LoadAvatar(string url)
{
    if (avatarImageImage == null) return;
    
    try
    {
        var texture = await LoadTextureFromUrl(url);
        var sprite = Sprite.Create(texture, 
            new Rect(0, 0, texture.width, texture.height), 
            new Vector2(0.5f, 0.5f));
        avatarImageImage.sprite = sprite;
    }
    catch (Exception ex)
    {
        Debug.LogError($"加载头像失败: {ex.Message}");
    }
}
```

### 更新按钮状态

```csharp
// 启用/禁用按钮
private void SetButtonInteractable(bool interactable)
{
    if (loginButtonButton != null)
    {
        loginButtonButton.interactable = interactable;
    }
}

// 更新按钮文本
private void SetButtonText(string text)
{
    if (loginButtonButton != null)
    {
        var buttonText = loginButtonButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }
}

// 更新多个按钮状态
private void UpdateButtonsState(bool isLoggedIn)
{
    if (loginButtonButton != null)
    {
        loginButtonButton.interactable = !isLoggedIn;
    }
    
    if (logoutButtonButton != null)
    {
        logoutButtonButton.interactable = isLoggedIn;
    }
}
```

## 完整示例

### 示例1：登录界面

```csharp
using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.UI.Core;
using Astrum.Client.Managers;
using Astrum.Client.Core;
using Astrum.Network.Generated;

namespace Astrum.Client.UI.Generated
{
    public partial class LoginView
    {
        #region Fields
        
        private bool isConnecting = false;
        private string serverAddress = "127.0.0.1";
        private int serverPort = 8888;
        
        #endregion
        
        #region Lifecycle Callbacks
        
        protected virtual void OnInitialize()
        {
            // 绑定UI事件
            BindUIEvents();
            
            // 订阅系统事件
            SubscribeToNetworkEvents();
            
            // 初始化UI状态
            InitializeUIState();
        }
        
        protected virtual void OnShow()
        {
            // 更新连接状态
            UpdateConnectionStatus();
        }
        
        protected virtual void OnHide()
        {
            // 取消订阅网络事件
            UnsubscribeFromNetworkEvents();
        }
        
        #endregion
        
        #region UI Event Binding
        
        private void BindUIEvents()
        {
            if (connectButtonButton != null)
            {
                connectButtonButton.onClick.AddListener(OnConnectButtonClicked);
            }
            
            if (usernameInputInputField != null)
            {
                usernameInputInputField.onValueChanged.AddListener(OnUsernameChanged);
            }
        }
        
        #endregion
        
        #region Network Event Subscription
        
        private void SubscribeToNetworkEvents()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager != null)
            {
                networkManager.OnConnected += OnNetworkConnected;
                networkManager.OnDisconnected += OnNetworkDisconnected;
                networkManager.OnLoginResponse += OnLoginResponse;
            }
        }
        
        private void UnsubscribeFromNetworkEvents()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager != null)
            {
                networkManager.OnConnected -= OnNetworkConnected;
                networkManager.OnDisconnected -= OnNetworkDisconnected;
                networkManager.OnLoginResponse -= OnLoginResponse;
            }
        }
        
        #endregion
        
        #region UI Event Handlers
        
        private void OnConnectButtonClicked()
        {
            if (isConnecting)
            {
                Debug.Log("正在连接中...");
                return;
            }
            
            ConnectToServer();
        }
        
        private void OnUsernameChanged(string value)
        {
            // 验证用户名输入
            ValidateUsername(value);
        }
        
        #endregion
        
        #region Network Event Handlers
        
        private void OnNetworkConnected()
        {
            isConnecting = false;
            UpdateConnectionStatus("已连接");
            SetConnectButtonInteractable(false);
        }
        
        private void OnNetworkDisconnected()
        {
            isConnecting = false;
            UpdateConnectionStatus("连接断开");
            SetConnectButtonInteractable(true);
        }
        
        private void OnLoginResponse(LoginResponse response)
        {
            if (response.Success)
            {
                UpdateConnectionStatus("登录成功");
                // 切换到主菜单
                UIManager.Instance.ShowUI("MainMenu");
            }
            else
            {
                UpdateConnectionStatus($"登录失败: {response.Message}");
            }
        }
        
        #endregion
        
        #region Business Logic
        
        private async void ConnectToServer()
        {
            try
            {
                isConnecting = true;
                UpdateConnectionStatus("连接中...");
                SetConnectButtonInteractable(false);
                
                var networkManager = GameApplication.Instance?.NetworkManager;
                if (networkManager == null)
                {
                    throw new Exception("NetworkManager不存在");
                }
                
                await networkManager.ConnectAsync(serverAddress, serverPort);
                
                // 发送连接请求
                var connectRequest = ConnectRequest.Create();
                networkManager.Send(connectRequest);
            }
            catch (Exception ex)
            {
                Debug.LogError($"连接失败: {ex.Message}");
                UpdateConnectionStatus($"连接失败: {ex.Message}");
                SetConnectButtonInteractable(true);
                isConnecting = false;
            }
        }
        
        private bool ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }
            
            if (username.Length < 3)
            {
                return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region UI Update Methods
        
        private void InitializeUIState()
        {
            UpdateConnectionStatus("未连接");
            SetButtonText("连接服务器");
        }
        
        private void UpdateConnectionStatus(string status = null)
        {
            if (statusTextText != null)
            {
                if (status != null)
                {
                    statusTextText.text = $"状态: {status}";
                }
                else
                {
                    var networkManager = GameApplication.Instance?.NetworkManager;
                    if (networkManager != null && networkManager.IsConnected())
                    {
                        statusTextText.text = "状态: 已连接";
                    }
                    else
                    {
                        statusTextText.text = "状态: 未连接";
                    }
                }
            }
        }
        
        private void SetButtonText(string text)
        {
            if (connectButtonButton != null)
            {
                var buttonText = connectButtonButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = text;
                }
            }
        }
        
        private void SetConnectButtonInteractable(bool interactable)
        {
            if (connectButtonButton != null)
            {
                connectButtonButton.interactable = interactable;
            }
        }
        
        #endregion
    }
}
```

## 最佳实践

### 代码组织

1. **使用Region分区**
   ```csharp
   #region Fields
   #region Lifecycle Callbacks
   #region UI Event Binding
   #region UI Event Handlers
   #region Business Logic
   #region UI Update Methods
   #region Helper Methods
   ```

2. **方法命名规范**
   - 事件处理方法：`OnXxxClicked`, `OnXxxChanged`
   - 业务逻辑方法：`PerformXxx`, `ProcessXxx`, `ValidateXxx`
   - UI更新方法：`UpdateXxx`, `SetXxx`, `RefreshXxx`

3. **代码复用**
   - 提取公共UI更新逻辑到辅助方法
   - 使用继承或组合复用UI逻辑

### 性能优化

1. **避免频繁的UI更新**
   ```csharp
   // 不好的做法
   void Update()
   {
       scoreTextText.text = GetScore().ToString();
   }
   
   // 好的做法
   private int lastScore = -1;
   void Update()
   {
       int currentScore = GetScore();
       if (currentScore != lastScore)
       {
           scoreTextText.text = currentScore.ToString();
           lastScore = currentScore;
       }
   }
   ```

2. **缓存组件引用**
   - Designer类已经缓存了所有UI组件引用
   - 避免重复调用GetComponent

3. **使用对象池**
   - 对于列表项等频繁创建/销毁的UI，使用对象池

### 错误处理

1. **Null检查**
   ```csharp
   if (loginButtonButton != null)
   {
       loginButtonButton.onClick.AddListener(OnLoginButtonClicked);
   }
   ```

2. **Try-Catch保护**
   ```csharp
   try
   {
       PerformCriticalOperation();
   }
   catch (Exception ex)
   {
       Debug.LogError($"操作失败: {ex.Message}");
       ShowErrorMessage("操作失败，请重试");
   }
   ```

3. **异步操作处理**
   ```csharp
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
       }
   }
   ```

## 相关文档

- [UI系统总览](UI-System-Overview%20UI系统总览.md) - 了解UI系统架构
- [UI创建指南](UI-Creation-Guide%20UI创建指南.md) - 创建UI Prefab
- [UI运行时使用](UI-Runtime-Usage%20UI运行时使用.md) - UIManager使用
- [UI开发规范](UI-Conventions%20UI开发规范.md) - 命名和代码规范

---

**版本历史**:
- v1.0 (2025-10-11) - 初始版本

