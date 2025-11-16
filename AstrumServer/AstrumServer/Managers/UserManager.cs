using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Astrum.Generated;
using Astrum.CommonBase;

namespace AstrumServer.Managers
{
    /// <summary>
    /// 用户管理器 - 管理所有在线用户
    /// </summary>
    public class UserManager
    {
        private readonly ConcurrentDictionary<string, UserInfo> _users = new();
        private readonly ConcurrentDictionary<string, string> _sessionToUser = new(); // sessionId -> userId
        private readonly ConcurrentDictionary<string, string> _userToSession = new(); // userId -> sessionId

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

        /// <summary>
        /// 用户连接时分配ID（旧接口，保持向后兼容）
        /// </summary>
        [Obsolete("使用 GetOrCreateUserByClientId 替代，直接使用客户端实例ID作为账号ID")]
        public UserInfo AssignUserId(string sessionId, string displayName)
        {
            try
            {
                // 生成唯一用户ID（临时兼容方案）
                var userId = GenerateUserId();
                
                var userInfo = UserInfo.Create();
                userInfo.Id = userId;
                userInfo.DisplayName = displayName;
                userInfo.LastLoginAt = TimeInfo.Instance.ClientNow();
                userInfo.CurrentRoomId = "";

                // 添加到管理器中
                _users[userId] = userInfo;
                _sessionToUser[sessionId] = userId;
                _userToSession[userId] = sessionId;

                ASLogger.Instance.Info($"为用户分配ID: {userId} (DisplayName: {displayName}, Session: {sessionId})");

                return userInfo;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"为用户分配ID时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// 用户断开连接
        /// </summary>
        public void RemoveUser(string sessionId)
        {
            try
            {
                if (_sessionToUser.TryRemove(sessionId, out var userId))
                {
                    if (_users.TryRemove(userId, out var userInfo))
                    {
                        _userToSession.TryRemove(userId, out _);
                        
                        // 如果用户在房间中，需要离开房间
                        if (!string.IsNullOrEmpty(userInfo.CurrentRoomId))
                        {
                            ASLogger.Instance.Info($"用户 {userId} 断开连接，当前在房间 {userInfo.CurrentRoomId}");
                        }
                        
                        ASLogger.Instance.Info($"用户断开连接: {userId} (DisplayName: {userInfo.DisplayName})");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"移除用户时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 只移除 Session 映射，保留用户信息（用于断线重连）
        /// </summary>
        public void RemoveSessionMapping(string sessionId)
        {
            try
            {
                if (_sessionToUser.TryRemove(sessionId, out var userId))
                {
                    _userToSession.TryRemove(userId, out _);
                    ASLogger.Instance.Info($"移除 Session 映射: {sessionId} -> {userId}，保留用户信息以便重连");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"移除 Session 映射时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 根据Session ID获取用户信息
        /// </summary>
        public UserInfo? GetUserBySessionId(string sessionId)
        {
            if (_sessionToUser.TryGetValue(sessionId, out var userId))
            {
                _users.TryGetValue(userId, out var userInfo);
                return userInfo;
            }
            return null;
        }

        /// <summary>
        /// 根据用户ID获取用户信息
        /// </summary>
        public UserInfo? GetUserById(string userId)
        {
            _users.TryGetValue(userId, out var userInfo);
            return userInfo;
        }

        /// <summary>
        /// 获取所有在线用户
        /// </summary>
        public List<UserInfo> GetAllUsers()
        {
            return _users.Values.ToList();
        }

        /// <summary>
        /// 获取在线用户数量
        /// </summary>
        public int GetOnlineUserCount()
        {
            return _users.Count;
        }

        /// <summary>
        /// 更新用户房间信息
        /// </summary>
        public void UpdateUserRoom(string userId, string roomId)
        {
            if (_users.TryGetValue(userId, out var userInfo))
            {
                userInfo.CurrentRoomId = roomId;
                ASLogger.Instance.Debug($"更新用户 {userId} 的房间信息: {roomId}");
            }
        }

        /// <summary>
        /// 检查用户是否在线
        /// </summary>
        public bool IsUserOnline(string userId)
        {
            return _users.ContainsKey(userId);
        }

        /// <summary>
        /// 获取用户的Session ID
        /// </summary>
        public string? GetSessionIdByUserId(string userId)
        {
            _userToSession.TryGetValue(userId, out var sessionId);
            return sessionId;
        }

        /// <summary>
        /// 生成唯一用户ID（仅用于向后兼容，新代码应使用客户端实例ID）
        /// </summary>
        private string GenerateUserId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = new Random().Next(1000, 9999);
            return $"user_{timestamp}_{random}";
        }

        /// <summary>
        /// 清理断开的用户
        /// </summary>
        public void CleanupDisconnectedUsers()
        {
            var disconnectedSessions = new List<string>();
            
            foreach (var kvp in _sessionToUser)
            {
                var sessionId = kvp.Key;
                var userId = kvp.Value;
                
                // 这里可以添加心跳检测逻辑
                // 暂时简单处理，实际项目中应该有更复杂的断线检测
            }

            foreach (var sessionId in disconnectedSessions)
            {
                RemoveUser(sessionId);
            }
        }
    }
} 