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
- **客户端实例ID持久化**：每个实例生成并持久化唯一的客户端ID，**直接作为账号ID使用**，统一账号和存档标识

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
- **路径规则**：`{persistentDataPath}/LocalSaves/{clientInstanceId}/PlayerProgressData.dat`

#### 2. 账号存档（Account Save）
- **存储位置**：服务器端文件系统/数据库
- **用途**：多人模式的游戏进度，与账号绑定
- **特点**：跨设备同步，服务器权威
- **路径规则**：`{serverDataPath}/AccountSaves/{clientInstanceId}/PlayerProgressData.dat`
- **统一标识**：客户端实例ID直接作为账号ID，无需映射

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

### 客户端实例ID管理

#### ClientInstanceIdManager（客户端）

**职责**：生成并持久化客户端实例ID，用于稳定识别客户端实例

**设计要点**：
- 基于 ParrelSync 实例ID生成唯一标识
- 持久化到本地文件，确保每次启动使用相同ID
- 支持多实例，每个实例有独立的ID
- **直接作为账号ID使用**，统一账号和存档标识

```csharp
namespace Astrum.Client.Data
{
    /// <summary>
    /// 客户端实例ID管理器 - 管理客户端实例的唯一标识符
    /// </summary>
    public static class ClientInstanceIdManager
    {
        private static string _cachedInstanceId;
        private static string InstanceIdFilePath => 
            Path.Combine(Application.persistentDataPath, "ClientInstanceId.dat");
        
        /// <summary>
        /// 获取或生成客户端实例ID
        /// </summary>
        public static string GetOrCreateInstanceId()
        {
            if (!string.IsNullOrEmpty(_cachedInstanceId))
            {
                return _cachedInstanceId;
            }
            
            // 尝试从文件加载
            if (File.Exists(InstanceIdFilePath))
            {
                try
                {
                    _cachedInstanceId = File.ReadAllText(InstanceIdFilePath).Trim();
                    if (!string.IsNullOrEmpty(_cachedInstanceId))
                    {
                        ASLogger.Instance.Info($"ClientInstanceIdManager: 加载已存在的实例ID - {_cachedInstanceId}");
                        return _cachedInstanceId;
                    }
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Warning($"ClientInstanceIdManager: 加载实例ID失败 - {ex.Message}");
                }
            }
            
            // 生成新的实例ID
            _cachedInstanceId = GenerateInstanceId();
            
            // 保存到文件
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(InstanceIdFilePath));
                File.WriteAllText(InstanceIdFilePath, _cachedInstanceId);
                ASLogger.Instance.Info($"ClientInstanceIdManager: 生成并保存新的实例ID - {_cachedInstanceId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ClientInstanceIdManager: 保存实例ID失败 - {ex.Message}");
            }
            
            return _cachedInstanceId;
        }
        
        /// <summary>
        /// 生成实例ID
        /// </summary>
        private static string GenerateInstanceId()
        {
            var instanceId = ParrelSyncHelper.GetInstanceId();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = UnityEngine.Random.Range(1000, 9999);
            
            // 格式：client_{instanceId}_{timestamp}_{random}
            return $"client_{instanceId}_{timestamp}_{random}";
        }
        
        /// <summary>
        /// 清除缓存的实例ID（用于测试）
        /// </summary>
        public static void ClearCache()
        {
            _cachedInstanceId = null;
        }
    }
}
```

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
        var clientInstanceId = ClientInstanceIdManager.GetOrCreateInstanceId();
        var saveDir = Path.Combine(Application.persistentDataPath, "LocalSaves", clientInstanceId);
        Directory.CreateDirectory(saveDir);
        return Path.Combine(saveDir, "PlayerProgressData.dat");
    }
    
    /// <summary>
    /// 获取账号存档路径（客户端暂存）
    /// </summary>
    private static string GetAccountSavePath(string clientInstanceId = null)
    {
        // 如果没有提供，使用当前实例的客户端ID
        if (string.IsNullOrEmpty(clientInstanceId))
        {
            clientInstanceId = ClientInstanceIdManager.GetOrCreateInstanceId();
        }
        
        var saveDir = Path.Combine(Application.persistentDataPath, "AccountSaves", clientInstanceId);
        Directory.CreateDirectory(saveDir);
        return Path.Combine(saveDir, "PlayerProgressData.dat");
    }
    
    /// <summary>
    /// 加载玩家进度数据
    /// </summary>
    public static PlayerProgressData LoadPlayerProgressData(SaveType saveType, string clientInstanceId = null)
    {
        string path = saveType == SaveType.Local 
            ? GetLocalSavePath() 
            : GetAccountSavePath(clientInstanceId);
            
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
    public static void SavePlayerProgressData(PlayerProgressData data, SaveType saveType, string clientInstanceId = null)
    {
        string path = saveType == SaveType.Local 
            ? GetLocalSavePath() 
            : GetAccountSavePath(clientInstanceId);
            
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
        /// 获取账号存档路径（使用客户端实例ID）
        /// </summary>
        private string GetAccountSavePath(string clientInstanceId)
        {
            if (string.IsNullOrEmpty(clientInstanceId))
            {
                throw new ArgumentException("ClientInstanceId cannot be null or empty", nameof(clientInstanceId));
            }
            
            var userDir = Path.Combine(_saveDataPath, clientInstanceId);
            Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "PlayerProgressData.dat");
        }
        
        /// <summary>
        /// 加载账号存档（使用客户端实例ID）
        /// </summary>
        public PlayerProgressData LoadAccountSave(string clientInstanceId)
        {
            var path = GetAccountSavePath(clientInstanceId);
            
            if (!File.Exists(path))
            {
                ASLogger.Instance.Info($"AccountSaveManager: 账号存档不存在 - ClientInstanceId: {clientInstanceId}");
                return null;
            }
            
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var data = MemoryPackSerializer.Deserialize<PlayerProgressData>(bytes);
                ASLogger.Instance.Info($"AccountSaveManager: 成功加载账号存档 - ClientInstanceId: {clientInstanceId}");
                return data;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 加载账号存档失败 - ClientInstanceId: {clientInstanceId}, Error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 保存账号存档（使用客户端实例ID）
        /// </summary>
        public bool SaveAccountSave(string clientInstanceId, PlayerProgressData data)
        {
            var path = GetAccountSavePath(clientInstanceId);
            
            try
            {
                byte[] bytes = MemoryPackSerializer.Serialize(data);
                File.WriteAllBytes(path, bytes);
                ASLogger.Instance.Info($"AccountSaveManager: 成功保存账号存档 - ClientInstanceId: {clientInstanceId}");
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 保存账号存档失败 - ClientInstanceId: {clientInstanceId}, Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 删除账号存档（使用客户端实例ID）
        /// </summary>
        public bool DeleteAccountSave(string clientInstanceId)
        {
            var path = GetAccountSavePath(clientInstanceId);
            
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    ASLogger.Instance.Info($"AccountSaveManager: 成功删除账号存档 - ClientInstanceId: {clientInstanceId}");
                }
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 删除账号存档失败 - ClientInstanceId: {clientInstanceId}, Error: {ex.Message}");
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
        private string _currentClientInstanceId = null;
        
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize(SaveSystem.SaveType saveType = SaveSystem.SaveType.Local, string clientInstanceId = null)
        {
            _currentSaveType = saveType;
            _currentClientInstanceId = clientInstanceId ?? ClientInstanceIdManager.GetOrCreateInstanceId();
            ASLogger.Instance.Info($"PlayerDataManager: 初始化 - SaveType: {saveType}, ClientInstanceId: {_currentClientInstanceId}");
            LoadProgressData();
        }
        
        /// <summary>
        /// 加载玩家进度数据
        /// </summary>
        public void LoadProgressData()
        {
            _progressData = SaveSystem.LoadPlayerProgressData(_currentSaveType, _currentClientInstanceId);
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
        SaveSystem.SavePlayerProgressData(_progressData, _currentSaveType, _currentClientInstanceId);
    }
    
    // ... 其他方法保持不变 ...
}
```

### 服务器端登录流程集成

#### UserManager 改造

服务器端需要根据客户端实例ID查找或创建账号：

```csharp
namespace AstrumServer.Managers
{
    public class UserManager
    {
        // 注意：不再需要客户端ID到账号ID的映射，直接使用客户端实例ID作为账号ID
        
        public UserManager()
        {
        }
        
        /// <summary>
        /// 根据客户端实例ID获取或创建账号（客户端实例ID直接作为账号ID）
        /// </summary>
        public UserInfo GetOrCreateUserByClientId(string clientInstanceId, string sessionId, string displayName)
        {
            // 直接使用客户端实例ID作为账号ID
            var userId = clientInstanceId;
            
            // 检查账号是否已存在
            if (_users.TryGetValue(userId, out var existingUser))
            {
                // 更新Session映射
                _sessionToUser[sessionId] = userId;
                _userToSession[userId] = sessionId;
                existingUser.LastLoginAt = TimeInfo.Instance.ClientNow();
                
                ASLogger.Instance.Info($"客户端实例 {clientInstanceId} 登录，使用已有账号: {userId}");
                return existingUser;
            }
            
            // 创建新账号（使用客户端实例ID作为账号ID）
            var userInfo = UserInfo.Create();
            userInfo.Id = userId;  // 直接使用客户端实例ID
            userInfo.DisplayName = displayName;
            userInfo.LastLoginAt = TimeInfo.Instance.ClientNow();
            userInfo.CurrentRoomId = "";
            
            // 添加到管理器
            _users[userId] = userInfo;
            _sessionToUser[sessionId] = userId;
            _userToSession[userId] = sessionId;
            
            ASLogger.Instance.Info($"为客户端实例 {clientInstanceId} 创建新账号: {userId}");
            return userInfo;
        }
        
        // ... 其他方法保持不变 ...
    }
}
```

#### 登录请求改造

```csharp
// 在 GameServer.HandleLoginRequest 中
private void HandleLoginRequest(Session client, LoginRequest request)
{
    try
    {
        // 获取客户端实例ID（如果请求中包含）
        var clientInstanceId = request.ClientInstanceId;
        if (string.IsNullOrEmpty(clientInstanceId))
        {
            // 兼容旧版本：使用Session ID作为临时标识
            clientInstanceId = $"temp_{client.Id}";
            ASLogger.Instance.Warning($"客户端未提供实例ID，使用临时标识: {clientInstanceId}");
        }
        
        ASLogger.Instance.Info($"客户端 {client.Id} 请求登录，实例ID: {clientInstanceId}, 显示名称: {request.DisplayName}");
        
        // 根据客户端实例ID获取或创建账号（客户端实例ID直接作为账号ID）
        var userInfo = _userManager.GetOrCreateUserByClientId(
            clientInstanceId, 
            client.Id.ToString(), 
            request.DisplayName ?? $"Player_{client.Id}"
        );
        
        // 加载账号存档（使用客户端实例ID）
        var accountSaveManager = new AccountSaveManager();
        var accountSave = accountSaveManager.LoadAccountSave(clientInstanceId);
        
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

#### 客户端登录请求改造

```csharp
// 在 UserManager.AutoLoginAsync 中
public async Task<bool> AutoLoginAsync()
{
    // ... 前面的代码 ...
    
    // 创建登录请求
    var loginRequest = LoginRequest.Create();
    loginRequest.DisplayName = $"Player_{UnityEngine.Random.Range(1000, 9999)}";
    loginRequest.ClientInstanceId = ClientInstanceIdManager.GetOrCreateInstanceId(); // 新增
    
    // ... 后面的代码 ...
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
├── ClientInstanceId.dat           # 客户端实例ID（持久化）
├── LocalSaves/                    # 单人存档目录
│   ├── client_Main_xxx_yyy/       # 主实例的客户端实例ID
│   │   └── PlayerProgressData.dat
│   ├── client_Clone_1_xxx_yyy/    # 克隆实例1的客户端实例ID
│   │   └── PlayerProgressData.dat
│   └── client_Clone_2_xxx_yyy/    # 克隆实例2的客户端实例ID
│       └── PlayerProgressData.dat
└── AccountSaves/                  # 账号存档目录（客户端暂存）
    ├── client_Main_xxx_yyy/       # 使用客户端实例ID作为账号ID
    │   └── PlayerProgressData.dat
    ├── client_Clone_1_xxx_yyy/
    │   └── PlayerProgressData.dat
    └── client_Clone_2_xxx_yyy/
        └── PlayerProgressData.dat
```

### 服务器端路径结构

```
{ServerDataPath}/
└── AccountSaves/                  # 账号存档目录
    ├── client_Main_xxx_yyy/       # 使用客户端实例ID作为账号ID
    │   └── PlayerProgressData.dat
    ├── client_Clone_1_xxx_yyy/
    │   └── PlayerProgressData.dat
    └── client_Clone_2_xxx_yyy/
        └── PlayerProgressData.dat
```

**统一标识说明**：
- 客户端实例ID格式：`client_{instanceId}_{timestamp}_{random}`
- 客户端实例ID = 账号ID（直接使用，无需映射）
- 所有存档路径统一使用客户端实例ID

## 使用流程

### 单机模式存档流程

1. **初始化**：`PlayerDataManager.Instance.Initialize(SaveSystem.SaveType.Local)`
2. **加载存档**：自动从 `LocalSaves/{clientInstanceId}/` 加载
3. **保存存档**：保存到 `LocalSaves/{clientInstanceId}/`

### 联机模式存档流程

1. **登录**：客户端连接服务器并登录，发送客户端实例ID
2. **初始化**：`PlayerDataManager.Instance.Initialize(SaveSystem.SaveType.Account)`（自动使用当前实例的客户端ID）
3. **请求加载**：客户端发送 `LoadAccountSaveRequest`（使用客户端实例ID）
4. **服务器响应**：服务器返回账号存档数据（基于客户端实例ID查找）
5. **应用存档**：客户端应用存档数据到实体
6. **游戏过程中**：定期保存到本地暂存（`AccountSaves/{clientInstanceId}/`）
7. **同步到服务器**：关键节点（关卡完成、退出游戏）发送 `SaveAccountSaveRequest` 同步到服务器（使用客户端实例ID）

## 关键决策与取舍

- **问题**：如何统一客户端实例账号和存档地址？
- **备选**：
  1. 客户端实例ID直接作为账号ID，统一所有路径（选择）
  2. 使用映射表关联客户端ID和账号ID
  3. 分别管理客户端ID和账号ID
- **选择**：客户端实例ID直接作为账号ID，简化设计，统一标识
- **影响**：简化了服务器端逻辑，无需映射表，账号和存档路径完全统一

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
- **选择**：使用 ParrelSync 的 `GetArgument()` 获取实例ID，结合时间戳和随机数生成唯一客户端实例ID
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

*文档版本：v1.2*  
*创建时间：2025-01-27*  
*最后更新：2025-01-27*  
*状态：设计完成*  
*Owner*: Lavender  
*变更摘要*: 统一客户端实例账号和存档地址，客户端实例ID直接作为账号ID使用，简化设计

