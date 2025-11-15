# 账号存档系统设计

> 📖 **版本**: v1.0 | **最后更新**: 2025-01-27  
> 🎯 **适用范围**: 客户端与服务器账号存档逻辑  
> 👥 **面向读者**: 开发人员  
> ✅ **目标**: 实现客户端单人存档与服务器账号存档的分离管理，支持 ParrelSync 多实例测试

## TL;DR

- **客户端单人存档**：本地存储，按 ParrelSync 实例区分路径
- **服务器账号存档**：按账号ID持久化，支持登录时同步
- **存档路径分离**：单人存档与账号存档使用不同目录和命名规则
- **ParrelSync 支持**：检测克隆实例，为不同实例分配独立存档路径

## 概述

完善客户端与服务器的账号存档逻辑，实现：
1. 客户端单人模式存档独立存储（本地）
2. 服务器端按账号ID存储存档（持久化）
3. 支持 ParrelSync 多实例测试，不同实例使用独立存档

## 架构设计

### 存档类型

#### 1. 单人存档（Local Save）
- **存储位置**：客户端本地文件系统
- **用途**：单机模式的游戏进度
- **特点**：不与服务器同步，仅本地有效
- **路径规则**：`{persistentDataPath}/LocalSaves/{instanceId}/PlayerProgressData.dat`

#### 2. 账号存档（Account Save）
- **存储位置**：服务器端文件系统/数据库
- **用途**：多人模式的游戏进度，与账号绑定
- **特点**：跨设备同步，服务器权威
- **路径规则**：`{serverDataPath}/AccountSaves/{userId}/PlayerProgressData.dat`

### 核心组件

#### SaveSystem（客户端）

**职责**：管理存档路径和文件操作

**设计要点**：
- 检测 ParrelSync 克隆实例
- 根据存档类型（单人/账号）选择不同路径
- 提供统一的加载/保存接口

#### AccountSaveManager（服务器端）

**职责**：管理账号存档的持久化

**设计要点**：
- 按账号ID组织存档文件
- 支持存档的加载、保存、删除
- 提供账号存档同步接口

#### PlayerDataManager（客户端）

**职责**：统一管理玩家数据，区分单人/账号存档

**设计要点**：
- 根据游戏模式选择存档类型
- 单机模式使用本地存档
- 联机模式使用账号存档（需与服务器同步）

## 实现细节

### ParrelSync 实例检测

```csharp
public static class ParrelSyncHelper
{
    /// <summary>
    /// 检测是否是 ParrelSync 克隆实例
    /// </summary>
    public static bool IsClone()
    {
        #if UNITY_EDITOR
        return ParrelSync.ClonesManager.IsClone();
        #else
        return false;
        #endif
    }
    
    /// <summary>
    /// 获取当前实例的唯一标识符
    /// </summary>
    public static string GetInstanceId()
    {
        #if UNITY_EDITOR
        if (IsClone())
        {
            return ParrelSync.ClonesManager.GetArgument();
        }
        #endif
        return "Main";
    }
}
```

### 客户端存档路径管理

```csharp
public static class SaveSystem
{
    /// <summary>
    /// 存档类型
    /// </summary>
    public enum SaveType
    {
        Local,      // 单人存档
        Account      // 账号存档（暂存，需同步到服务器）
    }
    
    /// <summary>
    /// 获取单人存档路径
    /// </summary>
    private static string GetLocalSavePath()
    {
        var instanceId = ParrelSyncHelper.GetInstanceId();
        var saveDir = Path.Combine(Application.persistentDataPath, "LocalSaves", instanceId);
        Directory.CreateDirectory(saveDir);
        return Path.Combine(saveDir, "PlayerProgressData.dat");
    }
    
    /// <summary>
    /// 获取账号存档路径（客户端暂存）
    /// </summary>
    private static string GetAccountSavePath(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
        }
        
        var instanceId = ParrelSyncHelper.GetInstanceId();
        var saveDir = Path.Combine(Application.persistentDataPath, "AccountSaves", instanceId, userId);
        Directory.CreateDirectory(saveDir);
        return Path.Combine(saveDir, "PlayerProgressData.dat");
    }
    
    /// <summary>
    /// 加载玩家进度数据
    /// </summary>
    public static PlayerProgressData LoadPlayerProgressData(SaveType saveType, string userId = null)
    {
        string path = saveType == SaveType.Local 
            ? GetLocalSavePath() 
            : GetAccountSavePath(userId);
            
        if (!File.Exists(path))
        {
            ASLogger.Instance.Info($"SaveSystem: 存档文件不存在 - {path}");
            return null;
        }
        
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            var data = MemoryPackSerializer.Deserialize<PlayerProgressData>(bytes);
            ASLogger.Instance.Info($"SaveSystem: 成功加载玩家进度数据 - {path}");
            return data;
        }
        catch (System.Exception ex)
        {
            ASLogger.Instance.Error($"SaveSystem: 加载玩家进度数据失败 - {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 保存玩家进度数据
    /// </summary>
    public static void SavePlayerProgressData(PlayerProgressData data, SaveType saveType, string userId = null)
    {
        string path = saveType == SaveType.Local 
            ? GetLocalSavePath() 
            : GetAccountSavePath(userId);
            
        try
        {
            byte[] bytes = MemoryPackSerializer.Serialize(data);
            File.WriteAllBytes(path, bytes);
            ASLogger.Instance.Info($"SaveSystem: 成功保存玩家进度数据 - {path}");
        }
        catch (System.Exception ex)
        {
            ASLogger.Instance.Error($"SaveSystem: 保存玩家进度数据失败 - {ex.Message}");
        }
    }
}
```

### 服务器端账号存档管理

```csharp
namespace AstrumServer.Data
{
    /// <summary>
    /// 账号存档管理器 - 管理服务器端账号存档的持久化
    /// </summary>
    public class AccountSaveManager
    {
        private readonly string _saveDataPath;
        
        public AccountSaveManager(string saveDataPath = null)
        {
            _saveDataPath = saveDataPath ?? Path.Combine(
                AppContext.BaseDirectory, 
                "Data", 
                "AccountSaves"
            );
            Directory.CreateDirectory(_saveDataPath);
        }
        
        /// <summary>
        /// 获取账号存档路径
        /// </summary>
        private string GetAccountSavePath(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
            }
            
            var userDir = Path.Combine(_saveDataPath, userId);
            Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "PlayerProgressData.dat");
        }
        
        /// <summary>
        /// 加载账号存档
        /// </summary>
        public PlayerProgressData LoadAccountSave(string userId)
        {
            var path = GetAccountSavePath(userId);
            
            if (!File.Exists(path))
            {
                ASLogger.Instance.Info($"AccountSaveManager: 账号存档不存在 - UserId: {userId}");
                return null;
            }
            
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var data = MemoryPackSerializer.Deserialize<PlayerProgressData>(bytes);
                ASLogger.Instance.Info($"AccountSaveManager: 成功加载账号存档 - UserId: {userId}");
                return data;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 加载账号存档失败 - UserId: {userId}, Error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 保存账号存档
        /// </summary>
        public bool SaveAccountSave(string userId, PlayerProgressData data)
        {
            var path = GetAccountSavePath(userId);
            
            try
            {
                byte[] bytes = MemoryPackSerializer.Serialize(data);
                File.WriteAllBytes(path, bytes);
                ASLogger.Instance.Info($"AccountSaveManager: 成功保存账号存档 - UserId: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 保存账号存档失败 - UserId: {userId}, Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 删除账号存档
        /// </summary>
        public bool DeleteAccountSave(string userId)
        {
            var path = GetAccountSavePath(userId);
            
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    ASLogger.Instance.Info($"AccountSaveManager: 成功删除账号存档 - UserId: {userId}");
                }
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 删除账号存档失败 - UserId: {userId}, Error: {ex.Message}");
                return false;
            }
        }
    }
}
```

### 客户端 PlayerDataManager 改造

```csharp
public class PlayerDataManager : Singleton<PlayerDataManager>
{
    private PlayerProgressData _progressData;
    private SaveSystem.SaveType _currentSaveType = SaveSystem.SaveType.Local;
    private string _currentUserId = null;
    
    /// <summary>
    /// 初始化管理器
    /// </summary>
    public void Initialize(SaveSystem.SaveType saveType = SaveSystem.SaveType.Local, string userId = null)
    {
        _currentSaveType = saveType;
        _currentUserId = userId;
        ASLogger.Instance.Info($"PlayerDataManager: 初始化 - SaveType: {saveType}, UserId: {userId}");
        LoadProgressData();
    }
    
    /// <summary>
    /// 加载玩家进度数据
    /// </summary>
    public void LoadProgressData()
    {
        _progressData = SaveSystem.LoadPlayerProgressData(_currentSaveType, _currentUserId);
        if (_progressData == null)
        {
            _progressData = CreateDefaultProgressData();
            ASLogger.Instance.Info("PlayerDataManager: 创建默认进度数据");
        }
        else
        {
            EnsureDataIntegrity(_progressData);
            ASLogger.Instance.Info($"PlayerDataManager: 加载进度数据 - 等级 {_progressData.Level}, 经验 {_progressData.Exp}");
        }
    }
    
    /// <summary>
    /// 保存玩家进度数据
    /// </summary>
    public void SaveProgressData(Entity entity = null)
    {
        if (entity != null)
        {
            CaptureProgressFromEntity(entity);
        }

        if (_progressData == null)
        {
            ASLogger.Instance.Warning("PlayerDataManager: 进度数据为空，无法保存");
            return;
        }

        EnsureDataIntegrity(_progressData);
        SaveSystem.SavePlayerProgressData(_progressData, _currentSaveType, _currentUserId);
    }
    
    // ... 其他方法保持不变 ...
}
```

### 服务器端登录流程集成

```csharp
// 在 GameServer.HandleLoginRequest 中
private void HandleLoginRequest(Session client, LoginRequest request)
{
    try
    {
        ASLogger.Instance.Info($"客户端 {client.Id} 请求登录，显示名称: {request.DisplayName}");
        
        // 为用户分配ID（或从持久化存储加载）
        var userInfo = _userManager.AssignUserId(client.Id.ToString(), request.DisplayName);
        
        // 加载账号存档
        var accountSaveManager = new AccountSaveManager();
        var accountSave = accountSaveManager.LoadAccountSave(userInfo.Id);
        
        // 如果存在存档，可以在响应中返回（或通过单独的消息）
        // 这里先简单处理，后续可以通过 LoadAccountSaveRequest 单独请求
        
        // 发送登录成功响应
        var response = LoginResponse.Create();
        response.Success = true;
        response.Message = "登录成功";
        response.User = userInfo;
        response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        _networkManager.SendMessage(client.Id.ToString(), response);
        ASLogger.Instance.Info($"客户端 {client.Id} 登录成功，用户ID: {userInfo.Id}");
    }
    catch (Exception ex)
    {
        // ... 错误处理 ...
    }
}
```

### 账号存档同步协议

需要新增协议消息：

```protobuf
// LoadAccountSaveRequest - 客户端请求加载账号存档
message LoadAccountSaveRequest {
}

// LoadAccountSaveResponse - 服务器返回账号存档
message LoadAccountSaveResponse {
  bool success = 1;
  string message = 2;
  bytes saveData = 3;  // PlayerProgressData 序列化后的数据
}

// SaveAccountSaveRequest - 客户端请求保存账号存档
message SaveAccountSaveRequest {
  bytes saveData = 1;  // PlayerProgressData 序列化后的数据
}

// SaveAccountSaveResponse - 服务器返回保存结果
message SaveAccountSaveResponse {
  bool success = 1;
  string message = 2;
}
```

## 存档路径结构

### 客户端路径结构

```
{Application.persistentDataPath}/
├── LocalSaves/                    # 单人存档目录
│   ├── Main/                      # 主实例
│   │   └── PlayerProgressData.dat
│   ├── Clone_1/                   # ParrelSync 克隆实例1
│   │   └── PlayerProgressData.dat
│   └── Clone_2/                   # ParrelSync 克隆实例2
│       └── PlayerProgressData.dat
└── AccountSaves/                  # 账号存档目录（客户端暂存）
    ├── Main/                      # 主实例
    │   ├── user_xxx/
    │   │   └── PlayerProgressData.dat
    │   └── user_yyy/
    │       └── PlayerProgressData.dat
    └── Clone_1/                   # ParrelSync 克隆实例1
        └── user_zzz/
            └── PlayerProgressData.dat
```

### 服务器端路径结构

```
{ServerDataPath}/
└── AccountSaves/                  # 账号存档目录
    ├── user_xxx/                  # 账号ID
    │   └── PlayerProgressData.dat
    ├── user_yyy/
    │   └── PlayerProgressData.dat
    └── user_zzz/
        └── PlayerProgressData.dat
```

## 使用流程

### 单机模式存档流程

1. **初始化**：`PlayerDataManager.Instance.Initialize(SaveSystem.SaveType.Local)`
2. **加载存档**：自动从 `LocalSaves/{instanceId}/` 加载
3. **保存存档**：保存到 `LocalSaves/{instanceId}/`

### 联机模式存档流程

1. **登录**：客户端连接服务器并登录
2. **初始化**：`PlayerDataManager.Instance.Initialize(SaveSystem.SaveType.Account, userId)`
3. **请求加载**：客户端发送 `LoadAccountSaveRequest`
4. **服务器响应**：服务器返回账号存档数据
5. **应用存档**：客户端应用存档数据到实体
6. **游戏过程中**：定期保存到本地暂存（`AccountSaves/{instanceId}/{userId}/`）
7. **同步到服务器**：关键节点（关卡完成、退出游戏）发送 `SaveAccountSaveRequest` 同步到服务器

## 关键决策与取舍

- **问题**：如何区分单人存档和账号存档？
- **备选**：
  1. 使用不同的目录结构（选择）
  2. 使用文件命名区分
  3. 使用统一的存档文件，内部标记类型
- **选择**：使用不同的目录结构，清晰分离，便于管理
- **影响**：需要修改 `SaveSystem` 和 `PlayerDataManager` 的接口

- **问题**：ParrelSync 多实例如何区分存档？
- **备选**：
  1. 使用 ParrelSync 提供的实例ID（选择）
  2. 使用端口号区分
  3. 手动配置实例标识
- **选择**：使用 ParrelSync 的 `GetArgument()` 获取实例ID
- **影响**：需要在客户端代码中集成 ParrelSync 检测逻辑

- **问题**：账号存档何时同步到服务器？
- **备选**：
  1. 实时同步（每次保存都同步）
  2. 定期同步（定时同步）
  3. 关键节点同步（选择）
- **选择**：关键节点同步（登录、关卡完成、退出游戏），减少服务器压力
- **影响**：需要实现同步协议和错误处理机制

---

**相关文档**:
- [存档数值方案](存档数值方案.md)
- [存档系统开发进展](存档系统-Progress 开发进展.md)

---

*文档版本：v1.0*  
*创建时间：2025-01-27*  
*最后更新：2025-01-27*  
*状态：设计完成*  
*Owner*: Lavender  
*变更摘要*: 创建账号存档系统设计方案，支持客户端单人存档与服务器账号存档分离，集成 ParrelSync 多实例支持

